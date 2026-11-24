using BepInEx;
using SeaPower;
using System.Reflection;

namespace AnchorChain;


[BepInPlugin("io.github.seapower_modders.anchorchain", "AnchorChain", "1.2.1")]
public class AnchorChainLoader : BaseUnityPlugin, Preloader.IPluginLoader
{
	private static string _configPath = string.Empty;
	private static readonly HashSet<string> ReservedSectionKeys = ["AnchorChain.State", "AnchorChain.ResetValues"];
	private static bool _initialized;

	public void LoadPlugins()
	{
		var directories = FileManager.Instance.Directories.ToArray();
		string loaderPath = typeof(AnchorChainLoader).Assembly.Location;
		var owner = directories.Where(dir => IsWithin(loaderPath, dir.DirectoryInfo.FullName))
			.OrderByDescending(dir => dir.DirectoryInfo.FullName.Length).FirstOrDefault();
		if (owner is null || !IsActive(owner)) {
			Logger.LogInfo("AnchorChain's directory is not selected; skipping initialization.");
			return;
		}
		if (_initialized) return;
		_initialized = true;
		try {
			LoadSelectedPlugins(directories, loaderPath);
		}
		catch (Exception error) {
			Logger.LogError($"AnchorChain load aborted: {error}");
		}
	}

	private void LoadSelectedPlugins(SearchDirectory[] directories, string loaderPath)
	{
		_configPath = Path.Join(Globals._streamingAssetsPath.FullName, "ACConfigs");
		Directory.CreateDirectory(_configPath);

		Dictionary<string, (ACPlugin Metadata, Type Type)> recognized = new();
		List<ACPlugin> preferred = new();

		// Discover from the bottom of the menu. File lookup still follows the menu from the top.
		var paths = SelectedDirectories(directories).Reverse()
			.SelectMany(directory => DllFiles(directory, directories));
		foreach (string path in paths) {
			if (string.Equals(path, loaderPath, StringComparison.OrdinalIgnoreCase)) continue;
			try {
				var types = Assembly.LoadFile(path).GetExportedTypes()
					.Where(type => !type.IsAbstract && !type.ContainsGenericParameters && typeof(IAnchorChainMod).IsAssignableFrom(type))
					.OrderBy(type => type.FullName, StringComparer.Ordinal);
				foreach (Type type in types) {
					var metadata = type.GetCustomAttribute<ACPlugin>();
					if (metadata is null) continue;
					if (recognized.ContainsKey(metadata.GUID)) {
						Logger.LogWarning($"Skipping duplicate plugin {metadata.GUID} at {path}; first discovered definition wins.");
						continue;
					}
					var config = type.GetCustomAttribute<ACConfig>();
					if (config is not null && !LoadConfigs(metadata, config, new IniHandler(), new IniHandler())) continue;

					metadata.Dependencies = new(type.GetCustomAttributes<ACDependency>());
					metadata.Dependencies.UnionWith(metadata.Before.Concat(metadata.After)
						.Select(guid => new ACDependency(guid, null, null)));
					metadata.Incompatibilities = new(type.GetCustomAttributes<ACIncompatibility>());
					recognized.Add(metadata.GUID, (metadata, type));
					preferred.Add(metadata);
				}
			}
			catch (BadImageFormatException) {
				// Mods can ship native DLLs.
			}
			catch (Exception error) {
				Logger.LogWarning($"Error inspecting {path}: {error}");
			}
		}

		// Prune in rounds, so removing a dependency also removes its dependants.
		while (true) {
			List<ACPlugin> remove = new();
			foreach (ACPlugin metadata in preferred) {
				ACDependency? missing = metadata.Dependencies.FirstOrDefault(dependency =>
					!recognized.TryGetValue(dependency.GUID, out var target) || !dependency.Contains(target.Metadata.Version));
				ACIncompatibility? conflict = metadata.Incompatibilities.FirstOrDefault(item => recognized.ContainsKey(item.GUID));
				if (missing is null && conflict is null) continue;
				string reason = missing is not null ? $"missing or mis-versioned dependency {missing.GUID}"
					: $"incompatible with {conflict!.GUID}";
				Logger.LogWarning($"Skipping {metadata.Name} ({metadata.GUID}): {reason}.");
				remove.Add(metadata);
			}
			if (remove.Count == 0) break;
			foreach (ACPlugin metadata in remove) recognized.Remove(metadata.GUID);
			preferred.RemoveAll(plugin => !recognized.ContainsKey(plugin.GUID));
		}

		foreach (ACPlugin metadata in preferred) {
			foreach (string successor in metadata.Before) {
				recognized[successor].Metadata.After.Add(metadata.GUID);
			}
		}

		var ordered = ResolveLoadOrder(preferred);
		HashSet<string> loaded = new(StringComparer.Ordinal);
		foreach (ACPlugin metadata in ordered) {
			if (!metadata.After.IsSubsetOf(loaded)) {
				Logger.LogWarning($"Skipping {metadata.GUID}: a prerequisite failed to initialize.");
				continue;
			}
			try {
				var plugin = (IAnchorChainMod)Activator.CreateInstance(recognized[metadata.GUID].Type)!;
				plugin.TriggerEntryPoint();
				loaded.Add(metadata.GUID);
				Logger.LogInfo($"Loaded plugin {metadata.Name} ({metadata.GUID})");
			}
			catch (Exception error) {
				Logger.LogError($"Error loading {metadata.GUID}: {error}");
			}
		}
		Logger.LogInfo($"AnchorChain loaded {loaded.Count} of {ordered.Count} eligible plugins.");
	}

	// Resolve every edge before running any plugin code. Recheck menu priority after each choice.
	private static List<ACPlugin> ResolveLoadOrder(List<ACPlugin> preferred)
	{
		List<ACPlugin> pending = new(preferred);
		List<ACPlugin> ordered = new(preferred.Count);
		HashSet<string> scheduled = new(StringComparer.Ordinal);
		while (pending.Count > 0) {
			int index = pending.FindIndex(plugin => plugin.After.IsSubsetOf(scheduled));
			if (index < 0) {
				throw new InvalidOperationException("Circular or unresolved plugin load order: "
					+ string.Join(", ", pending.Select(plugin => plugin.GUID)));
			}
			var plugin = pending[index];
			pending.RemoveAt(index);
			ordered.Add(plugin);
			scheduled.Add(plugin.GUID);
		}
		return ordered;
	}

	// A locked checkbox can still be checked.
	private static bool IsActive(SearchDirectory dir) => dir.IsChecked && dir.DirectoryInfo.Exists;

	private static IEnumerable<string> SelectedDirectories(IEnumerable<SearchDirectory> directories) => directories
		.Where(IsActive).Select(dir => dir.DirectoryInfo.FullName).Distinct(StringComparer.OrdinalIgnoreCase);

	private static IEnumerable<string> DllFiles(string directory, IEnumerable<SearchDirectory> directories)
	{
		// Each registered child directory has its own selection and priority.
		var children = directories.Select(dir => dir.DirectoryInfo.FullName)
			.Where(child => IsWithin(child, directory)).ToArray();
		return Directory.EnumerateFiles(directory, "*.dll", SearchOption.AllDirectories)
			.Where(file => !children.Any(child => IsWithin(file, child)))
			.OrderBy(file => file, StringComparer.OrdinalIgnoreCase).ThenBy(file => file, StringComparer.Ordinal);
	}

	private static bool IsWithin(string path, string directory)
	{
		string relative = Path.GetRelativePath(directory, path);
		return relative != "." && relative != ".." && !Path.IsPathRooted(relative)
			&& !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal);
	}


	private bool LoadConfigs(ACPlugin pluginData, ACConfig configData, IniHandler userIni, IniHandler defaultIni)
	{
		string? userPath = FindFile(pluginData.GUID + "_user.ini");
		string? defaultPath = FindFile(pluginData.GUID + ".ini");

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


	public static string? FindFile(string fileName) => SelectedDirectories(FileManager.Instance.Directories)
		.Select(dir => Path.Join(dir, fileName))
		.Append(Path.Join(_configPath, fileName))
		.FirstOrDefault(File.Exists);
}


[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ACPlugin(string guid, string? name, string? version, string[]? before = null, string[]? after = null)
	: Attribute, IEquatable<ACPlugin>, IEquatable<string>
{
	public string GUID { get; } = guid;
	public string Name { get; } = name ?? "";
	public Version Version { get; } = new(version ?? "1.0");
	public HashSet<string> Before { get; } = before is null ? [] : [..before];
	public HashSet<string> After { get; } = after is null ? [] : [..after];
	public HashSet<ACDependency> Dependencies { get; internal set; } = new();
	public HashSet<ACIncompatibility> Incompatibilities { get; internal set;  } = new();


	public bool Equals(ACPlugin? other) => other is not null && GUID == other.GUID;


	public bool Equals(string? other) => other is not null && GUID == other;


	public override int GetHashCode() => GUID.GetHashCode();
}


[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ACDependency(string guid, string? min, string? max) : Attribute
{
	public string GUID { get; } = guid;
	public Version? MinVersion { get; } = min is null ? null : new Version(min);
	public Version? MaxVersion { get; } = max is null ? null : new Version(max);


	public bool Contains(Version version) =>
		(MinVersion is null || version >= MinVersion) && (MaxVersion is null || version <= MaxVersion);
}


[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ACIncompatibility(string guid) : Attribute
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