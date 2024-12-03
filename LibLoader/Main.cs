using System.Diagnostics.CodeAnalysis;
using BepInEx;
using SeaPower;
using System.Reflection;

namespace AnchorChain
{


    [BepInPlugin("io.github.seapower-modders.anchorchain", "AnchorChain", "0.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        private static Dictionary<string, HashSet<ACPlugin>> _postLoadsCache = new();

        private void Awake()
        {
            Dictionary<string, (ACPlugin, IModInterface)> recognizedPlugins = new();

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
                            ACPlugin pluginData = (ACPlugin)Attribute.GetCustomAttribute(plugin, typeof(ACPlugin));
                            if (pluginData is null) continue;

                            // Set dependencies
                            pluginData.Dependencies =
                                new(Attribute.GetCustomAttributes(plugin, typeof(ACDependency)).Cast<ACDependency>() ?? []);

                            // Ensure all preloads and postloads are dependencies
                            pluginData.Dependencies.UnionWith(pluginData.Before.Select(x => new ACDependency(x, null, null)));
                            pluginData.Dependencies.UnionWith(pluginData.After.Select(x => new ACDependency(x, null, null)));

                            if (!recognizedPlugins.TryAdd(pluginData.GUID, (pluginData, (IModInterface)Activator.CreateInstance(plugin)))) {
                                Logger.LogWarning(
                                    $"Attempted to load a duplicate plugin: {pluginData.Name} ({pluginData.GUID})");
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
                        if (!recognizedPlugins.ContainsKey(dependency.GUID)) {
                            Logger.LogWarning(
                                $"Missing dependency \"{dependency.GUID}\" for \"{pluginData.Name}\" ({pluginData.GUID}); it will not be loaded");
                            toRemove.Add(pluginData);
                            break;
                        }
                    }
                }

                if (toRemove.Count == 0) {
                    break;
                }

                foreach (ACPlugin pluginData in toRemove) {
                    recognizedPlugins.Remove(pluginData.GUID);
                }
            }

            // Cast all preloads to postloads
            foreach ((ACPlugin pluginData, IModInterface _) in recognizedPlugins.Values) {
                foreach (string plugin in pluginData.Before) {
                    recognizedPlugins[plugin].Item1.After.Add(pluginData.GUID);
                }
            }

            // Screen for circular post loads
            bool circularLoad = false;
            foreach ((ACPlugin pluginData, IModInterface _) in recognizedPlugins.Values) {
                HashSet<ACPlugin> postLoads = FindAllPostLoads(pluginData, recognizedPlugins, new());
                if (postLoads.Contains(pluginData)) {
                    Logger.LogError($"Aborting chainload; circular load order chain detected: {postLoads}");
                    circularLoad = true;
                }
            }
            if (circularLoad) { return; }

            // Get first "level" of plugins with no preloads
            HashSet<string> currentLevel = (from x in recognizedPlugins.Values where x.Item1.After.Count == 0 select x.Item1.GUID).ToHashSet() ?? new();
            HashSet<string> alreadyLoaded = new();

            while (true) {
                bool loadedOne = false;
                foreach (string guid in currentLevel) {
                    (ACPlugin pluginData, IModInterface plugin) = recognizedPlugins[guid];
                    if (pluginData.After.IsSubsetOf(alreadyLoaded)) {
                        try {
                            plugin.TriggerEntryPoint();
                            Logger.LogInfo($"Loaded plugin {pluginData.Name} ({pluginData.GUID})");
                            currentLevel.UnionWith(pluginData.Before);
                            currentLevel.Remove(pluginData.GUID);
                            loadedOne = true;
                        }
                        catch (Exception e) {
                            // TODO: If no plugins depend on failed load then continue
                            Logger.LogError($"Aborting chainload; error loading plugin {pluginData.Name} ({pluginData.GUID}): {e}");
                            return;
                        }
                    }
                }

                if (currentLevel.Count == 0) { break; }

                if (!loadedOne) {
                    Logger.LogError($"Aborting chainload; unable to load any more plugins: {alreadyLoaded}");
                }
            }

            Logger.LogInfo($"Loaded AnchorChain V{((BepInPlugin)Attribute.GetCustomAttribute(typeof(Plugin), typeof(BepInPlugin))).Version}!");
        }


        private HashSet<ACPlugin> FindAllPostLoads(ACPlugin plugin, Dictionary<string, (ACPlugin, IModInterface)> recognizedPlugins, HashSet<ACPlugin> prev)
        {
            HashSet<ACPlugin> cachedPostLoads = _postLoadsCache[plugin.GUID];
            if (cachedPostLoads != null) {
                return cachedPostLoads;
            }

            if (!prev.Add(plugin)) { return [plugin]; }

            HashSet<ACPlugin> allPostLoads = new();

            foreach (string GUID in plugin.After) {
                allPostLoads.UnionWith(FindAllPostLoads(recognizedPlugins[GUID].Item1, recognizedPlugins, prev));
            }

            _postLoadsCache[plugin.GUID] = allPostLoads;
            return allPostLoads;
        }
    }


    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ACPlugin([NotNull] string guid, string name, string version, string[] before = null, string[] after = null)
        : Attribute, IEquatable<ACPlugin>, IEquatable<string>
    {
        [NotNull] public string GUID { get; } = guid;
        [NotNull] public string Name { get; } = name ?? "";
        [NotNull] public Version Version { get; } = new(version ?? "1.0");
        [NotNull] public HashSet<string> Before { get; } = before is null ? [] : [..before];
        [NotNull] public HashSet<string> After { get; } = after is null ? [] : [..after];
        [NotNull] public HashSet<ACDependency> Dependencies { get; internal set; } = new();


        public static explicit operator ACPlugin(string guid) => new ACPlugin(guid, null, null);


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


        public bool Contains([NotNull] Version version) =>
            (MinVersion is null || version.CompareTo(MinVersion) >= 0) && (MaxVersion is null || version.CompareTo(MaxVersion) <= 0);
    }


    public interface IModInterface
    {
        public void TriggerEntryPoint();
    }
}
