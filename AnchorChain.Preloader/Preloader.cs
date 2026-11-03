using BepInEx;
using SeaPower;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace AnchorChain.Preloader;


[BepInPlugin("io.github.seapower_modders.anchorchain_preloader", "AnchorChain Preloader", "1.0.0")]
public class AnchorChainPreloader: BaseUnityPlugin
{
	// Once the game has finished starting up: how long it gets to list the mod folders before we give up.
	private const float WaitForFoldersSeconds = 60f;

	private enum Result { Loaded, NotFound, Failed }

	private void Awake()
	{
		Logger.LogInfo("AnchorChain Preloader started!");

		// Up to Sea Power 0.8.7 the game has listed its mod folders by now. The public beta of 26 Oct 2026 lists them later
		// (https://github.com/SeaPower-Modders/AnchorChain/issues/19), so when nothing is found yet, wait for the game
		// instead of giving up. Where the folders are there, nothing changes: Anchor Chain starts right here, as before.
		if (TryLoad() != Result.NotFound) { return; }

		Logger.LogInfo("The game has not listed its mod folders yet; waiting for it.");
		StartCoroutine(WaitAndLoad());
	}

	private IEnumerator WaitAndLoad()
	{
		// After the start-up load screen: mods started in the middle of it can keep the unit data from loading.
		yield return new WaitUntil(StartupDone);

		float until = Time.realtimeSinceStartup + WaitForFoldersSeconds;
		while (Time.realtimeSinceStartup < until) {
			if (TryLoad() != Result.NotFound) { yield break; }
			yield return new WaitForSecondsRealtime(0.5f);
		}

		Logger.LogError($"Could not find AnchorChain chainloader");
	}

	private bool StartupDone()
	{
		try {
			return StartupDoneCore();
		}
		catch (Exception e) {
			// A game version where these types differ: do not wait on them.
			Logger.LogWarning($"Startup readiness check unavailable; retrying AnchorChain discovery: {e}");
			return true;
		}
	}

	// Apart from the caller, so that a type missing from some game version fails inside the try above, not when the caller is compiled.
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
	private static bool StartupDoneCore() =>
		Singleton<UnitMetadataProvider>.InstanceExists() && Singleton<UnitMetadataProvider>.Instance.UnitData != null &&
		Singleton<LoadScreen>.InstanceExists() && Singleton<LoadScreen>.Instance.LoadText == "Done";

	private Result TryLoad()
	{
		if (!Singleton<FileManager>.InstanceExists()) { return Result.NotFound; }

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
				return Result.Failed;
			}
		}

		return loadedAnchorChain ? Result.Loaded : Result.NotFound;
	}
}

public interface IPluginLoader
{
	public void LoadPlugins();
}
