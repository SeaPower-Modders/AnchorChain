using BepInEx;
using System.Reflection;
using UnityEngine;

namespace AnchorChain.Preloader;


// The preloader is installed by hand, so players rarely update it (https://github.com/SeaPower-Modders/AnchorChain/issues/21).
// It is therefore kept a thin stub: find AnchorChain.dll and hand over to it. It references no game types, so a game update has
// nothing in it to break; everything that depends on the game (when the mod folders are listed, the load order) lives in
// AnchorChain.dll, which reaches players through the Workshop.
[BepInPlugin("io.github.seapower_modders.anchorchain_preloader", "AnchorChain Preloader", "2.0.0")]
public class AnchorChainPreloader: BaseUnityPlugin
{
	private const string LoaderFileName = "AnchorChain.dll";
	private const string LoaderTypeName = "AnchorChain.AnchorChainLoader";
	private const string SeaPowerAppId = "1286220";
	private const string UserSettingsFileName = "usersettings.ini";
	private const string LoadOrderSection = "LoadOrder";
	private static readonly string[] IgnoredStreamingAssets = ["original", "user", "EntityScenes"];

	private void Awake()
	{
		Logger.LogInfo("AnchorChain Preloader started!");

		string asmPath = FindLoader();
		if (asmPath is null) {
			Logger.LogError($"Could not find AnchorChain chainloader");
			return;
		}

		try {
			Assembly loaded = Assembly.LoadFile(asmPath);
			Logger.LogInfo("Loaded assembly " + loaded.FullName);

			Type chainLoader = loaded.GetType(LoaderTypeName);
			if (chainLoader is null) { Logger.LogError($"AnchorChain .dll at {asmPath} missing ChainLoader"); return; }

			((IPluginLoader) Activator.CreateInstance(chainLoader)).LoadPlugins();
		}
		catch (Exception e) {
			Logger.LogError($"Failed to initialize AnchorChain with error: {e}");
		}
	}

	// Looks in the folders the game takes mods from, straight on disk: the game's own list of them is not ready this early on every
	// game version (https://github.com/SeaPower-Modders/AnchorChain/issues/19). Only copies in mod folders that are switched on in the
	// game's Mod Manager count; if there are several, the newest one wins.
	private string FindLoader()
	{
		string best = null;
		Version bestVersion = null;
		HashSet<string> enabled = EnabledModFolders();
		bool skippedDisabled = false;

		foreach (string modFolder in ModFolders()) {
			string[] dllFiles;
			try {
				dllFiles = Directory.GetFiles(modFolder, LoaderFileName, SearchOption.AllDirectories);
			}
			catch (Exception e) {
				Logger.LogWarning($"Could not search {modFolder}: {e.Message}");
				continue;
			}
			if (dllFiles.Length == 0) { continue; }

			if (enabled is not null && !enabled.Contains(Path.GetFileName(modFolder))) {
				Logger.LogInfo($"Skipping AnchorChain in {modFolder}: switched off in the Mod Manager");
				skippedDisabled = true;
				continue;
			}

			foreach (string path in dllFiles) {
				// GetFiles also matches longer extensions like ".dll_old" on Windows.
				if (!Path.GetFileName(path).Equals(LoaderFileName, StringComparison.OrdinalIgnoreCase)) { continue; }

				Version version;
				try {
					version = AssemblyName.GetAssemblyName(path).Version;
				}
				catch (Exception) {
					continue;
				}

				if (bestVersion is null || version > bestVersion) {
					best = path;
					bestVersion = version;
				}
			}
		}

		if (best is not null) { Logger.LogInfo($"Using AnchorChain {bestVersion} from {best}"); }
		else if (skippedDisabled) { Logger.LogInfo("AnchorChain is switched off in the Mod Manager; not starting it"); }
		return best;
	}

	// The names of the mod folders switched on in the game's Mod Manager (a Workshop item's folder is named after its id), read from
	// the file the game keeps them in, the way FileManager.GetSearchDirectories reads it on 0.8.7 and the globe build alike. A folder
	// the file does not list is switched off. Null if the file or its list cannot be read: then every copy counts.
	private HashSet<string> EnabledModFolders()
	{
		Dictionary<string, string> loadOrder;
		try {
			loadOrder = ReadIniSection(Path.Combine(Application.persistentDataPath, UserSettingsFileName), LoadOrderSection);
		}
		catch (Exception e) {
			Logger.LogWarning($"Could not read the Mod Manager settings, so switched-off mods are not skipped: {e.Message}");
			return null;
		}
		if (loadOrder is null || !loadOrder.TryGetValue("NumberOfModFiles", out string countText) || !int.TryParse(countText, out int count)) {
			Logger.LogWarning("No Mod Manager list found, so switched-off mods are not skipped");
			return null;
		}

		HashSet<string> enabled = new(StringComparer.Ordinal);
		for (int i = 1; i <= count; i++) {
			if (!loadOrder.TryGetValue($"Mod{i}Directory", out string entry)) { continue; }

			// "<folder name>,<switched on>,<Workshop id, 0 for a local folder>"
			string[] parts = entry.Split(',');
			bool on = true;
			if (parts.Length > 1) { bool.TryParse(parts[1], out on); }
			if (!on) { continue; }

			string name = parts[0].Trim();
			if (name.Length > 0) { enabled.Add(name); }
			if (parts.Length > 2 && ulong.TryParse(parts[2].Trim(), out ulong steamId) && steamId != 0) { enabled.Add(steamId.ToString()); }
		}
		return enabled;
	}

	// The keys and values of one section of an ini file; null if the file or the section does not exist.
	private static Dictionary<string, string> ReadIniSection(string path, string section)
	{
		if (!File.Exists(path)) { return null; }

		Dictionary<string, string> values = null;
		foreach (string rawLine in File.ReadAllLines(path)) {
			string line = rawLine.Trim();
			if (line.StartsWith("[") && line.EndsWith("]")) {
				if (values is not null) { break; }
				if (line.Substring(1, line.Length - 2).Trim() == section) { values = new Dictionary<string, string>(); }
				continue;
			}

			int equals = line.IndexOf('=');
			if (values is null || equals <= 0) { continue; }
			values[line.Substring(0, equals).Trim()] = line.Substring(equals + 1).Trim();
		}
		return values;
	}

	// The game's mod folders: the sub-folders of StreamingAssets and one folder per Steam Workshop item.
	private static IEnumerable<string> ModFolders()
	{
		if (Directory.Exists(Application.streamingAssetsPath)) {
			foreach (string dir in Directory.GetDirectories(Application.streamingAssetsPath)) {
				// The game's own data, skipped by the game too when it lists the mod folders.
				if (IgnoredStreamingAssets.Contains(Path.GetFileName(dir))) { continue; }
				yield return dir;
			}
		}

		// <library>\steamapps\common\Sea Power -> <library>\steamapps\workshop\content\1286220\<item id>
		DirectoryInfo steamApps = Directory.GetParent(Paths.GameRootPath)?.Parent;
		if (steamApps is not null) {
			string workshop = Path.Combine(steamApps.FullName, "workshop", "content", SeaPowerAppId);
			if (Directory.Exists(workshop)) {
				foreach (string item in Directory.GetDirectories(workshop)) { yield return item; }
			}
		}
	}
}

// AnchorChain.dll implements this; keep it unchanged so that every preloader works with every AnchorChain.dll.
public interface IPluginLoader
{
	public void LoadPlugins();
}
