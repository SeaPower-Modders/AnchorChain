using AnchorChain;
using UnityEngine;

namespace ExamplePlugin
{
    /// <summary>
    /// This plugin implicitly requires Plugin 2, and will load before Plugin 2 is loaded.
    /// </summary>
    [ACPlugin("your-name.plugin-1", "Plugin 1", "1.0", ["your-name.plugin-2"])]
    public class Plugin1 : MonoBehaviour, IModInterface
    {
        void IModInterface.TriggerEntryPoint()
        {
            Debug.Log("Plugin 1 loaded");
        }
    }

    /// <summary>
    /// This plugin implicitly requires Plugin 3, and will load only after Plugin 3 is loaded.
    /// </summary>
    [ACPlugin("your-name.plugin-2", "Plugin 2", "0.2.1", [], ["your-name.plugin-3"])]
    public class Plugin2 : MonoBehaviour, IModInterface
    {
        void IModInterface.TriggerEntryPoint()
        {
            Debug.Log("Plugin 2 loaded");
        }
    }

    /// <summary>
    /// This plugin is not specified to load before or after any plugins, and will be loaded in the first pass of plugin loading as a result.
    /// </summary>
    [ACPlugin("your-name.plugin-3", "Plugin 3", "2.3")]
    public class Plugin3 : MonoBehaviour, IModInterface
    {
        void IModInterface.TriggerEntryPoint()
        {
            Debug.Log("Plugin 3 loaded");
        }
    }

    /// <summary>
    /// This plugin requires Plugin 1 at a minimum version of 0.3.0, and a maximum version of 1.0. If Plugin 1 is not present or is misversioned, it will not load.
    /// It will also load in the first pass of plugin loading for the same reason as Plugin 3.
    /// </summary>
    [ACPlugin("your-name.plugin-4", "Plugin 4", "1.3.2")]
    [ACDependency("your-name.plugin-1", "0.3.0", "1.0")]
    public class Plugin4 : MonoBehaviour, IModInterface
    {
        void IModInterface.TriggerEntryPoint()
        {
            Debug.Log("Plugin 4 loaded");
        }
    }
}
