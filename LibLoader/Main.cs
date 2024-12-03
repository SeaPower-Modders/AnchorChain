using System.Diagnostics.CodeAnalysis;
using BepInEx;
using SeaPower;
using System.Reflection;

namespace AnchorChain
{


    [BepInPlugin("io.github.seapower-modders.anchorchain", "AnchorChain", "0.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Dictionary<ACPlugin, IModInterface> recognizedPlugins = new();

            // First pass, registering plugin classes, dependencies, and plugins to preload
            foreach (var dir in FileManager.Instance.Directories.ToList().ConvertAll(dir => dir.DirectoryInfo)) {
                string possiblepath = Path.Combine(dir.FullName);

                string[] dllFiles = Directory.GetFiles(possiblepath, "*.dll", SearchOption.AllDirectories);

                foreach (string asmPath in dllFiles) {
                    try {
                        Assembly loaded = Assembly.LoadFile(asmPath);
                        Logger.LogInfo("Loaded assembly " + loaded.FullName);

                        foreach (Type plugin in (from x in loaded.GetExportedTypes()
                                 where x.GetInterfaces().Contains(typeof(IModInterface))
                                 select x)) {
                            // All valid plugins should be annotated with ACPlugin
                            ACPlugin pluginData = (ACPlugin) Attribute.GetCustomAttribute(plugin, typeof(ACPlugin));
                            if (pluginData is null) continue;

                            // Set dependencies
                            pluginData.Dependencies = new(Attribute.GetCustomAttributes(plugin, typeof(ACDependency)).Cast<ACDependency>() ?? []); ;

                            // Ensure all preloads are dependencies
                            pluginData.Dependencies.UnionWith(pluginData.Before.Select(x => new ACDependency(x, null, null)));

                            if (!recognizedPlugins.TryAdd(pluginData, (IModInterface) Activator.CreateInstance(plugin)))
                            {
                                Logger.LogWarning($"Attempted to load a duplicate plugin: {pluginData.Name} ({pluginData.GUID})");
                                continue;
                            }
                        }
                    }
                    catch (Exception ex) {
                        Logger.LogWarning("Error loading assembly at path " + asmPath + ": " + ex);
                    }
                }
            }

            // Ensure all dependencies are met, and remove plugins missing dependencies
            while (true) {
                List<ACPlugin> toRemove = new();

                foreach (ACPlugin pluginData in recognizedPlugins.Keys) {
                    foreach (ACDependency dependency in pluginData.Dependencies) {
                        if (!recognizedPlugins.ContainsKey(new(dependency.GUID, null, null))) {
                            Logger.LogWarning($"Missing dependency {dependency.GUID} for {pluginData.Name} ({pluginData.GUID})");
                            toRemove.Add(pluginData);
                            break;
                        }
                    }
                }

                if (toRemove.Count == 0) {
                    break;
                }

                foreach (ACPlugin pluginData in toRemove) {
                    recognizedPlugins.Remove(pluginData);
                }
            }

            foreach (var plugin in recognizedPlugins.Values) {
                (plugin).TriggerEntryPoint();
            }

            Logger.LogInfo($"Loaded AnchorChain V{( (BepInPlugin) Attribute.GetCustomAttribute(typeof(Plugin), typeof(BepInPlugin)) ).Version}!");
        }

    }


    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ACPlugin([NotNull] string guid, string name, string version, string[] before = null)
        : Attribute, IEquatable<ACPlugin>, IEquatable<string>
    {
        public string GUID { get; } = guid;
        public string Name { get; } = name ?? "";
        public string Version { get; } = version ?? "";
        public HashSet<string> Before { get; } = before is null ? [] : [..before];
        public HashSet<ACDependency> Dependencies { get; internal set; }


        public bool Equals(ACPlugin other) => other is not null && GUID == other.GUID;


        public bool Equals(string other) => other is not null && GUID == other;


        public override int GetHashCode() => GUID.GetHashCode();
    }


    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ACDependency([NotNull] string guid, string min, string max) : Attribute
    {
        public string GUID { get; } = guid;
        public Version MinVersion { get; } = min is null ? null : new Version(min);
        public Version MaxVersion { get; } = max is null ? null : new Version(max);
    }


    public interface IModInterface
    {
        public void TriggerEntryPoint();
    }
}
