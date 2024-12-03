using BepInEx;
using HarmonyLib;
using SeaPower;
using System.Reflection;
using UnityEngine;

namespace AnchorChain
{


    [BepInPlugin("io.github.seapower-modders.anchorchain", "AnchorChain", "0.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {

            //var harmony = new Harmony("ac.lib.harmony.product");
            //harmony.PatchAll();

            foreach (var dir in FileManager.Instance.Directories.ToList().ConvertAll(dir => dir.DirectoryInfo))
            {
                string possiblepath = Path.Combine(dir.FullName);

                string[] dllFiles = Directory.GetFiles(possiblepath, "*.dll", SearchOption.AllDirectories);
                List<Assembly> _loadedAssemblies = new List<Assembly>();

                // Process the found .dll files
                foreach (string asmPath in dllFiles)
                {

                    try
                    {
                        Assembly loaded = Assembly.LoadFile(asmPath);
                        Logger.LogInfo("Loaded assembly " + loaded.FullName);
                        _loadedAssemblies.Add(loaded);
                        Type epType = (from x in loaded.GetTypes()
                                       where x.GetInterfaces().Contains(typeof(IModInterface))
                                       select x).FirstOrDefault();
                        if (epType is null)
                        {
                            continue;
                        }
                        IModInterface ep2 = (IModInterface)Activator.CreateInstance(epType);
                        ep2.TriggerEntryPoint();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning("Error loading assembly at path " + asmPath + ": " + ex);
                    }
                }
            }

            Logger.LogInfo($"Loaded AnchorChain V{( (BepInPlugin) Attribute.GetCustomAttribute(typeof(Plugin), typeof(BepInPlugin)) ).Version}!");
        }

    }

    public interface IModInterface
    {
        public void TriggerEntryPoint();
    }
}
