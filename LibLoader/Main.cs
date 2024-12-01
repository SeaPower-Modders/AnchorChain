using BepInEx;
using HarmonyLib;
using SeaPower;
using System.Reflection;
using UnityEngine;

namespace AnchorChain
{


    [BepInPlugin("AC", "AnchorChain", "1.0")]
    public class Plugin : BaseUnityPlugin
    {

        private void Awake()
        {


            // Plugin startup logic
            int ver = 4;
            Logger.LogInfo($"Plugin is loaded! Ver 0.0.0.{ver}");
            Debug.LogWarning($"Plugin is loaded! Ver 0.0.0.{ver}");
            //var harmony = new Harmony("ac.lib.harmony.product");
            //harmony.PatchAll();

            foreach (var dir in FileManager.Instance.Directories.ToList().ConvertAll(dir => dir.DirectoryInfo))
            {
                string possiblepath = Path.Combine(dir.FullName);
                Debug.LogWarning($"Looking for dll in {possiblepath}"); // For example, just print them

                string[] dllFiles = Directory.GetFiles(possiblepath, "*.dll", SearchOption.AllDirectories);

                // Process the found .dll files
                foreach (string asmPath in dllFiles)
                {
                    // Your logic to handle the found DLLs
                    Debug.LogWarning($"loaded dll {asmPath}"); // For example, just print them
                    List<Assembly> _loadedAssemblies = new List<Assembly>();
                    try
                    {
                        Assembly loaded = Assembly.LoadFile(asmPath);
                        Debug.Log("Loaded assembly " + loaded.FullName);
                        _loadedAssemblies.Add(loaded);
                        Type epType = (from x in loaded.GetTypes()
                                       where x.GetInterfaces().Contains(typeof(IModInterface))
                                       select x).FirstOrDefault();
                        IModInterface ep2 = (IModInterface)Activator.CreateInstance(epType);
                        ep2.TriggerEntryPoint();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("Error loading assembly at path " + asmPath + ": " + ex);
                    }
                }
            }



        }

    }

    public interface IModInterface
    {
        public void TriggerEntryPoint();
    }
}
