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
	// game version (https://github.com/SeaPower-Modders/AnchorChain/issues/19). If there are several copies, the newest one wins.
	private string FindLoader()
	{
		string best = null;
		Version bestVersion = null;

		foreach (string root in ModRoots()) {
			string[] dllFiles;
			try {
				dllFiles = Directory.GetFiles(root, LoaderFileName, SearchOption.AllDirectories);
			}
			catch (Exception e) {
				Logger.LogWarning($"Could not search {root}: {e.Message}");
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
		return best;
	}

	// The game's local mod folders (sub-folders of StreamingAssets) and its Steam Workshop items.
	private static IEnumerable<string> ModRoots()
	{
		if (Directory.Exists(Application.streamingAssetsPath)) {
			foreach (string dir in Directory.GetDirectories(Application.streamingAssetsPath)) {
				// The game's own data, skipped by the game too when it lists the mod folders.
				if (IgnoredStreamingAssets.Contains(Path.GetFileName(dir))) { continue; }
				yield return dir;
			}
		}

		// <library>\steamapps\common\Sea Power -> <library>\steamapps\workshop\content\1286220
		DirectoryInfo steamApps = Directory.GetParent(Paths.GameRootPath)?.Parent;
		if (steamApps is not null) {
			string workshop = Path.Combine(steamApps.FullName, "workshop", "content", SeaPowerAppId);
			if (Directory.Exists(workshop)) { yield return workshop; }
		}
	}
}

// AnchorChain.dll implements this; keep it unchanged so that every preloader works with every AnchorChain.dll.
public interface IPluginLoader
{
	public void LoadPlugins();
}
