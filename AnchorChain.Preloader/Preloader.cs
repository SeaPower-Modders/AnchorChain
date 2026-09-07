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

		var directories = FileManager.Instance.Directories.ToArray();
		foreach (var dir in PluginDirectories.Selected(directories)) {
			try {
				string asmPath = PluginDirectories.DllFiles(dir, directories).FirstOrDefault(path =>
					Path.GetFileName(path).Equals("AnchorChain.dll", StringComparison.OrdinalIgnoreCase));
				if (asmPath is null) continue;
				Assembly loaded = Assembly.LoadFile(asmPath);
				Logger.LogInfo("Loaded assembly " + loaded.FullName);

				Type chainLoader = (from x in loaded.GetExportedTypes()
					         where x.FullName != null && x.FullName.Equals("AnchorChain.AnchorChainLoader")
					         select x).FirstOrDefault();

				if (chainLoader is null) { Logger.LogError($"AnchorChain .dll at {asmPath} missing ChainLoader"); continue; }

				((IPluginLoader) Activator.CreateInstance(chainLoader)).LoadPlugins();
				loadedAnchorChain = true;
				// Only the highest-priority selected loader may initialize plugins.
				break;
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
