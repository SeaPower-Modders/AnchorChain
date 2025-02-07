using System.Diagnostics.CodeAnalysis;
using BepInEx;
using SeaPower;
using System.Reflection;

namespace AnchorChain;


[BepInPlugin("io.github.seapower_modders.anchorchain", "AnchorChain", "1.1.0")]
public class AnchorChainLoader : BaseUnityPlugin, Preloader.IPluginLoader
{
	private static Dictionary<string, HashSet<ACPlugin>> _postLoadsCache = new();
	private static HashSet<DirectoryInfo> _allDirectories = new();
	private static string _configPath = string.Empty;
	private static readonly HashSet<string> ReservedSectionKeys = ["AnchorChain.State", "AnchorChain.ResetValues"];

	public void LoadPlugins()
	{
		Dictionary<string, (ACPlugin, IAnchorChainMod)> recognizedPlugins = new();
		IniHandler userIni = new();
		IniHandler defaultIni = new();

		if (!Setup()) {
			Logger.LogError($"Anchor Chain setup failed, aborting load.");
			return;
		}

		// First pass, registering plugin classes, configs, dependencies, and plugins to preload
		foreach (DirectoryInfo dir in _allDirectories) {
			string possiblePath = Path.Combine(dir.FullName);

			string[] dllFiles = Directory.GetFiles(possiblePath, "*.dll", SearchOption.AllDirectories);

			foreach (string asmPath in dllFiles) {
				try {
					Assembly loaded = Assembly.LoadFile(asmPath);
					Logger.LogInfo("Loaded assembly " + loaded.FullName);

					foreach (Type plugin in (from x in loaded.GetExportedTypes() where x.GetInterfaces().Contains(typeof(IAnchorChainMod)) select x))
					{
						// All valid plugins should be annotated with ACPlugin
						ACPlugin pluginData = (ACPlugin)Attribute.GetCustomAttribute(plugin, typeof(ACPlugin));
						if (pluginData is null) continue;

						ACConfig configData = (ACConfig)Attribute.GetCustomAttribute(plugin, typeof(ACConfig));

						if (configData is not null && !LoadConfigs(pluginData, configData, userIni, defaultIni)) continue;

						// Set dependencies and incompatibilities
						pluginData.Dependencies =
							new(Attribute.GetCustomAttributes(plugin, typeof(ACDependency)).Cast<ACDependency>() ?? []);
						pluginData.Incompatibilities =
							new(Attribute.GetCustomAttributes(plugin, typeof(ACIncompatibility)).Cast<ACIncompatibility>() ?? []);

						// Ensure all preloads and postloads are dependencies
						pluginData.Dependencies.UnionWith(pluginData.Before.Select(x => new ACDependency(x, null, null)));
						pluginData.Dependencies.UnionWith(pluginData.After.Select(x => new ACDependency(x, null, null)));

						if (!recognizedPlugins.TryAdd(pluginData.GUID, (pluginData, (IAnchorChainMod)Activator.CreateInstance(plugin)))) {
							Logger.LogWarning($"Attempted to load a duplicate plugin: {pluginData.Name} ({pluginData.GUID})");
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
			HashSet<ACPlugin> toRemove = new();

			foreach ((ACPlugin pluginData, IAnchorChainMod _) in recognizedPlugins.Values) {
				foreach (ACDependency dependency in pluginData.Dependencies) {
					if (!recognizedPlugins.ContainsKey(dependency.GUID)) {
						Logger.LogWarning($"Skipping mod \"{pluginData.Name}\" ({pluginData.GUID}); missing dependency \"{dependency.GUID}\"");
						toRemove.Add(pluginData);
						break;
					}
					if (!dependency.Contains(recognizedPlugins[dependency.GUID].Item1.Version)) {
						Logger.LogWarning($"Skipping mod \"{pluginData.Name}\" ({pluginData.GUID}); mis-versioned dependency \"{dependency.GUID}\"");
						toRemove.Add(pluginData);
						break;
					}
				}

				foreach (ACIncompatibility incompatibility in pluginData.Incompatibilities) {
					if (recognizedPlugins.ContainsKey(incompatibility.GUID)) {
						Logger.LogWarning($"Skipping mod \"{pluginData.Name}\" ({pluginData.GUID}); detected incompatible mod \"{incompatibility.GUID}\"");
					}
					toRemove.Add(pluginData);
					break;
				}
			}

			if (toRemove.Count == 0) {
				break;
			}

			foreach (ACPlugin pluginData in toRemove) {
				recognizedPlugins.Remove(pluginData.GUID);
			}
		}

		// Cast all preloads to postloads and postloads to preloads
		foreach ((ACPlugin pluginData, IAnchorChainMod _) in recognizedPlugins.Values) {
			foreach (string plugin in pluginData.Before) {
				recognizedPlugins[plugin].Item1.After.Add(pluginData.GUID);
			}

			foreach (string plugin in pluginData.After) {
				recognizedPlugins[plugin].Item1.Before.Add(pluginData.GUID);
			}
		}

		// Screen for circular post loads
		bool circularLoad = false;
		foreach ((ACPlugin pluginData, IAnchorChainMod _) in recognizedPlugins.Values) {
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
			if (currentLevel.Count == 0) { break; }

			bool loadedOne = false;
			HashSet<string> toLoad = new();
			foreach (string guid in currentLevel) {
				(ACPlugin pluginData, IAnchorChainMod plugin) = recognizedPlugins[guid];
				if (pluginData.After.IsSubsetOf(alreadyLoaded)) {
					try {
						plugin.TriggerEntryPoint();
						Logger.LogInfo($"Loaded plugin {pluginData.Name} ({pluginData.GUID})");
						toLoad.UnionWith(pluginData.Before);
						alreadyLoaded.Add(pluginData.GUID);
						loadedOne = true;
					}
					catch (Exception e) {
						Logger.LogError($"Error loading plugin {pluginData.Name} ({pluginData.GUID}): {e}");
					}
				}
			}

			currentLevel.UnionWith(toLoad);
			currentLevel.RemoveWhere(x => alreadyLoaded.Contains(x));

			if (!loadedOne) {
				Logger.LogError($"Aborting chainload; unable to load any more plugins: {alreadyLoaded}, {currentLevel}");
			}
		}

		Logger.LogInfo($"Loaded AnchorChain V{((BepInPlugin)Attribute.GetCustomAttribute(typeof(AnchorChainLoader), typeof(BepInPlugin))).Version}!");
	}


	private bool Setup()
	{
		foreach (var dir in FileManager.Instance.Directories.ToList().ConvertAll(dir => dir.DirectoryInfo)) {
			_allDirectories.Add(dir);
		}

		_configPath = Path.Join(Globals._streamingAssetsPath.FullName, "ACConfigs");

		if (!Directory.Exists(_configPath)) {
			try {
				Directory.CreateDirectory(_configPath);
				_allDirectories.Add(new DirectoryInfo(_configPath));
			} catch (Exception ex) {
				Logger.LogError($"Failed to create ACConfigs: {ex}");
				return false;
			}
		}

		return true;
	}


	private HashSet<ACPlugin> FindAllPostLoads(ACPlugin plugin, Dictionary<string, (ACPlugin, IAnchorChainMod)> recognizedPlugins, HashSet<ACPlugin> prev)
	{
		if (_postLoadsCache.TryGetValue(plugin.GUID, out HashSet<ACPlugin> cachedPostLoads)) {
			return cachedPostLoads;
		}

		if (!prev.Add(plugin)) { return [plugin]; }

		HashSet<ACPlugin> allPostLoads = new();

		foreach (string guid in plugin.After) {
			allPostLoads.UnionWith(FindAllPostLoads(recognizedPlugins[guid].Item1, recognizedPlugins, prev));
		}

		_postLoadsCache[plugin.GUID] = allPostLoads;
		return allPostLoads;
	}


	private bool LoadConfigs(ACPlugin pluginData, ACConfig configData, IniHandler userIni, IniHandler defaultIni)
	{
		string userPath = FindFile(pluginData.GUID + "_user.ini");
		string defaultPath = FindFile(pluginData.GUID + ".ini");

		// Any config-using plugin must have a default config
		if (defaultPath is null) {
			Logger.LogWarning($"Required reference config file missing: {pluginData.Name} ({pluginData.GUID})");
			return false;
		}

		// Make sure user config exists
		if (userPath is null) {
			userPath = Path.Join(_configPath, pluginData.GUID + "_user.ini");
			Logger.LogInfo($"Writing local config file: {pluginData.Name} ({pluginData.GUID})");

			try {
				File.Copy(defaultPath, userPath, true);
			} catch (Exception ex) {
				Logger.LogError($"Failed to create local config file: {pluginData.Name} ({pluginData.GUID}) | {ex.Message}");
				if (configData.Required) {
					return false;
				}
				Logger.LogInfo($"Failed to load config file: {pluginData.Name} ({pluginData.GUID})");
				return true;
			}
		}

		// Open now-ensured configs
		userIni.open(userPath);
		defaultIni.open(defaultPath);

		// Ensure config validity
		foreach ((string sectionName, Dictionary<string, string> section) in defaultIni.Data) {
			if (ReservedSectionKeys.Contains(sectionName)) continue;

			if (!userIni.doesSectionExist(sectionName)) {
				userIni.AddSection(sectionName, new());
				Logger.LogInfo($"Plugin config missing section: {pluginData.Name} ({pluginData.GUID})");
			}

			foreach (string key in section.Keys) {
				if (!userIni.doesKeyExist(sectionName, key)) {
					userIni.writeValue(sectionName, key, section[key]);
					Logger.LogInfo($"Plugin config missing key: {pluginData.Name} ({pluginData.GUID})");
				}
			}
		}

		// Process any config commands
		if (!defaultIni.readValue("AnchorChain.State", "hasReset", false)) {
			foreach ((string section, string keys) in defaultIni.GetSectionKeyValues("AnchorChain.ResetValues")) {
				foreach (string key in keys.Split(",")) {
					userIni.writeValue(section, key, defaultIni.readValue(section, key, ""));
				}
			}
			defaultIni.writeValue("AnchorChain.State", "hasReset", true);
		}

		// Save all config changes to disk
		userIni.saveFile(true);
		defaultIni.saveFile(true);

		return true;
	}


	public static string FindFile(string fileName)
	{
		foreach (DirectoryInfo dir in _allDirectories) {
			string path = Path.Join(dir.FullName, fileName);
			if (File.Exists(path)) return path;
		}

		return null;
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
	[NotNull] public HashSet<ACIncompatibility> Incompatibilities { get; internal set;  } = new();


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
		(MinVersion is null || version >= MinVersion) && (MaxVersion is null || version <= MaxVersion);
}


[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ACIncompatibility([NotNull] string guid) : Attribute
{
	public string GUID { get; } = guid;
}


[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ACConfig(bool required = false) : Attribute
{
	public bool Required { get; } = required;
}


public interface IAnchorChainMod
{
	public void TriggerEntryPoint();
}