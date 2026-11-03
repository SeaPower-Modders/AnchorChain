using BepInEx;
using SeaPower;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace AnchorChain.Preloader;


[BepInPlugin("io.github.seapower_modders.anchorchain_preloader", "AnchorChain Preloader", "1.0.0")]
public class AnchorChainPreloader: BaseUnityPlugin
{
	private enum LoadResult { NotFound, Loaded, Failed }

	private void Awake()
	{
		Logger.LogInfo("AnchorChain Preloader started!");
		if (TryLoad() != LoadResult.NotFound) { return; }
		Logger.LogInfo("Waiting for the game to finish starting and list its mod folders.");
		StartCoroutine(WaitAndLoad());
	}

	private IEnumerator WaitAndLoad()
	{
		// Loading plugins during startup can interrupt the game's unit loading.
		yield return new WaitUntil(StartupDone);
		float deadline = Time.realtimeSinceStartup + 30f;
		do {
			if (TryLoad() != LoadResult.NotFound) { yield break; }
			yield return new WaitForSecondsRealtime(0.5f);
		} while (Time.realtimeSinceStartup < deadline);
		Logger.LogError("Could not find AnchorChain chainloader");
	}

	private bool StartupDone()
	{
		try { return StartupDoneCore(); }
		catch (Exception e) {
			Logger.LogWarning($"Startup readiness check unavailable; retrying AnchorChain discovery: {e}");
			return true;
		}
	}

	// Keep missing game APIs inside the caller's try/catch when the runtime compiles this method.
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool StartupDoneCore() =>
		Singleton<UnitMetadataProvider>.InstanceExists() && Singleton<UnitMetadataProvider>.Instance.UnitData != null &&
		Singleton<LoadScreen>.InstanceExists() && Singleton<LoadScreen>.Instance.LoadText == "Done";

	private LoadResult TryLoad()
	{
		if (!Singleton<FileManager>.InstanceExists()) { return LoadResult.NotFound; }
		bool loadedAnchorChain = false;

		foreach (DirectoryInfo dir in FileManager.Instance.Directories.Select(dir => dir.DirectoryInfo).ToArray()) {
			if (dir is null || !dir.Exists) { continue; }

			string[] dllFiles = Directory.GetFiles(dir.FullName, "*.dll", SearchOption.AllDirectories);
			string asmPath = dllFiles.FirstOrDefault(file => file.EndsWith("AnchorChain.dll"));
			if (asmPath is null) { continue; }

			try {
				Assembly loaded = Assembly.LoadFile(asmPath);
				Logger.LogInfo("Loaded assembly " + loaded.FullName);

				Type chainLoader = loaded.GetExportedTypes().FirstOrDefault(type => type.FullName == "AnchorChain.AnchorChainLoader");

				if (chainLoader is null) { Logger.LogError($"AnchorChain .dll at {asmPath} missing ChainLoader"); continue; }

				((IPluginLoader) Activator.CreateInstance(chainLoader)).LoadPlugins();
				loadedAnchorChain = true;
			}
			catch (Exception e) {
				Logger.LogError($"Failed to initialize AnchorChain with error: {e}");
				return LoadResult.Failed;
			}
		}

		return loadedAnchorChain ? LoadResult.Loaded : LoadResult.NotFound;
	}
}

public interface IPluginLoader
{
	public void LoadPlugins();
}