using System.Diagnostics.CodeAnalysis;
using BepInEx;
using SeaPower;
using System.Reflection;

namespace AnchorChain;


[BepInPlugin("io.github.seapower_modders.anchorchain", "AnchorChain", "1.1.0")]
public class AnchorChainLoader : BaseUnityPlugin, Preloader.IPluginLoader
{
	private static readonly List<DirectoryInfo> _allDirectories = new();
	private static string _configPath = string.Empty;
	private static readonly HashSet<string> ReservedSectionKeys = ["AnchorChain.State", "AnchorChain.ResetValues"];
	private static bool _initialized;

	public void LoadPlugins()
	{
		if (!PluginDirectories.IsSelected(typeof(AnchorChainLoader).Assembly.Location, FileManager.Instance.Directories)) {
			Logger.LogInfo("AnchorChain's directory is not selected; skipping initialization.");
			return;
		}
		if (_initialized) return;
		_initialized = true;
		try {
			LoadSelectedPlugins();
		}
		catch (Exception error) {
			PluginRuntime.Failed = true;
			Logger.LogError($"AnchorChain load aborted: {error}");
		}
		try {
			ModMenuIntegration.Install();
		}
		catch (Exception error) {
			Logger.LogError($"Could not install AnchorChain mod-menu controls: {error}");
		}
	}

	private void LoadSelectedPlugins()
	{
		var directories = FileManager.Instance.Directories.ToArray();
		var selected = PluginDirectories.Selected(directories);
		_allDirectories.Clear();
		_allDirectories.AddRange(selected);
		_configPath = Path.Join(Globals._streamingAssetsPath.FullName, "ACConfigs");
		Directory.CreateDirectory(_configPath);
		_allDirectories.Add(new DirectoryInfo(_configPath));

		Dictionary<string, (ACPlugin Metadata, Type Type)> recognized = new();
		List<ACPlugin> preferred = new();
		HashSet<string> seenFiles = new(StringComparer.OrdinalIgnoreCase);

		// The top menu entry has highest priority. Run it last unless dependencies say otherwise.
		foreach (DirectoryInfo directory in selected.Reverse()) {
			foreach (string path in PluginDirectories.DllFiles(directory, directories)) {
				if (!seenFiles.Add(path) || PluginDirectories.IsLoader(path)) continue;
				try {
					Assembly assembly = Assembly.LoadFile(path);
					foreach (Type type in assembly.GetExportedTypes()
						.Where(type => !type.IsAbstract && !type.ContainsGenericParameters && typeof(IAnchorChainMod).IsAssignableFrom(type))
						.OrderBy(type => type.FullName, StringComparer.Ordinal)) {
						ACPlugin metadata = type.GetCustomAttribute<ACPlugin>();
						if (metadata is null) continue;
						if (recognized.ContainsKey(metadata.GUID)) {
							Logger.LogWarning($"Skipping duplicate plugin {metadata.GUID} at {path}; first discovered definition wins.");
							continue;
						}
						ACConfig config = type.GetCustomAttribute<ACConfig>();
						if (config is not null && !LoadConfigs(metadata, config, new IniHandler(), new IniHandler())) continue;
						metadata.Dependencies = new(type.GetCustomAttributes<ACDependency>());
						metadata.Incompatibilities = new(type.GetCustomAttributes<ACIncompatibility>());
						// ACDependency checks presence/version; only Before/After constrain initialization order.
						metadata.Dependencies.UnionWith(metadata.Before.Select(guid => new ACDependency(guid, null, null)));
						metadata.Dependencies.UnionWith(metadata.After.Select(guid => new ACDependency(guid, null, null)));
						recognized.Add(metadata.GUID, (metadata, type));
						preferred.Add(metadata);
					}
				}
				catch (BadImageFormatException) {
					// Native DLLs can accompany mods. They are not managed plugins.
				}
				catch (Exception error) {
					PluginRuntime.Failed = true;
					Logger.LogWarning($"Error inspecting {path}: {error}");
				}
			}
		}

		// Prune in rounds, so removing a dependency also removes its dependants.
		while (true) {
			List<ACPlugin> remove = new();
			foreach (ACPlugin metadata in preferred.Where(plugin => recognized.ContainsKey(plugin.GUID))) {
				ACDependency missing = metadata.Dependencies.FirstOrDefault(dependency =>
					!recognized.TryGetValue(dependency.GUID, out var target) || !dependency.Contains(target.Metadata.Version));
				ACIncompatibility conflict = metadata.Incompatibilities.FirstOrDefault(item => recognized.ContainsKey(item.GUID));
				if (missing is null && conflict is null) continue;
				Logger.LogWarning($"Skipping {metadata.Name} ({metadata.GUID}): " +
					(missing is not null ? $"missing or mis-versioned dependency {missing.GUID}." : $"incompatible with {conflict.GUID}."));
				remove.Add(metadata);
			}
			if (remove.Count == 0) break;
			foreach (ACPlugin metadata in remove) recognized.Remove(metadata.GUID);
		}

		preferred.RemoveAll(plugin => !recognized.ContainsKey(plugin.GUID));
		foreach (ACPlugin metadata in preferred)
			foreach (string successor in metadata.Before)
				recognized[successor].Metadata.After.Add(metadata.GUID);

		// Sort the entire graph before calling constructors or entry points. Cycles cannot half-load a chain.
		IReadOnlyList<ACPlugin> ordered = PluginLoadOrder.Sort(preferred);
		HashSet<string> loaded = new(StringComparer.Ordinal);
		foreach (ACPlugin metadata in ordered) {
			if (!metadata.After.IsSubsetOf(loaded)) {
				Logger.LogWarning($"Skipping {metadata.GUID}: a prerequisite failed to initialize.");
				continue;
			}
			try {
				PluginRuntime.Start(recognized[metadata.GUID].Type);
				loaded.Add(metadata.GUID);
				Logger.LogInfo($"Loaded plugin {metadata.Name} ({metadata.GUID})");
			}
			catch (Exception error) {
				Logger.LogError($"Error loading {metadata.GUID}: {error}");
			}
		}
		Logger.LogInfo($"AnchorChain loaded {loaded.Count} of {ordered.Count} eligible plugins.");
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
			if (defaultIni.doesSectionExist("AnchorChain.ResetValues")) {
				foreach ((string section, string keys) in defaultIni.GetSectionKeyValues("AnchorChain.ResetValues")) {
					foreach (string key in keys.Split(",")) {
						userIni.writeValue(section, key, defaultIni.readValue(section, key, ""));
					}
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
public class ACConfig(bool required = true) : Attribute
{
	public bool Required { get; } = required;
}


public interface IAnchorChainMod
{
	public void TriggerEntryPoint();
}
