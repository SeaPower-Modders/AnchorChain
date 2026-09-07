using BepInEx;
using SeaPower;
using System.Reflection;

namespace AnchorChain.Preloader;


[BepInPlugin("io.github.seapower_modders.anchorchain_preloader", "AnchorChain Preloader", "1.0.0")]
public class AnchorChainPreloader: BaseUnityPlugin
{
	private void Awake()
	{
		Logger.LogInfo("AnchorChain Preloader started!");

		bool loadedAnchorChain = false;

		foreach (var dir in FileManager.Instance.Directories.ToList().ConvertAll(dir => dir.DirectoryInfo)) {
			string possiblepath = Path.Combine(dir.FullName);

			string[] dllFiles = Directory.GetFiles(possiblepath, "*.dll", SearchOption.AllDirectories);
			string asmPath = (from x in dllFiles where x.EndsWith("AnchorChain.dll") select x).FirstOrDefault();
			if (asmPath is null) { continue; }

			try {
				Assembly loaded = Assembly.LoadFile(asmPath);
				Logger.LogInfo("Loaded assembly " + loaded.FullName);

				Type chainLoader = (from x in loaded.GetExportedTypes()
					         where x.FullName != null && x.FullName.Equals("AnchorChain.AnchorChainLoader")
					         select x).FirstOrDefault();

				if (chainLoader is null) { Logger.LogError($"AnchorChain .dll at {asmPath} missing ChainLoader"); continue; }

				((IPluginLoader) Activator.CreateInstance(chainLoader)).LoadPlugins();
				loadedAnchorChain = true;
			}
			catch (Exception e) {
				Logger.LogError($"Failed to initialize AnchorChain with error: {e}");
				return;
			}
		}

		if (!loadedAnchorChain) { Logger.LogError($"Could not find AnchorChain chainloader"); }
	}
}

public interface IPluginLoader
{
	public void LoadPlugins();
}