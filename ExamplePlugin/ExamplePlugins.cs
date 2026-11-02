using AnchorChain;
using UnityEngine;

namespace ExamplePlugin
{
    /// <summary>
    /// This plugin implicitly requires Plugin 2, and will load before Plugin 2 is loaded.
    /// </summary>
    [ACPlugin("your-name.plugin-1", "Plugin 1", "1.0", ["your-name.plugin-2"])]
    public class Plugin1 : MonoBehaviour, IAnchorChainMod
    {
        void IAnchorChainMod.TriggerEntryPoint()
        {
            Debug.Log("Plugin 1 loaded");
        }
    }

    /// <summary>
    /// This plugin implicitly requires Plugin 3, and will load only after Plugin 3 is loaded.
    /// </summary>
    [ACPlugin("your-name.plugin-2", "Plugin 2", "0.2.1", [], ["your-name.plugin-3"])]
    public class Plugin2 : MonoBehaviour, IAnchorChainMod
    {
        void IAnchorChainMod.TriggerEntryPoint()
        {
            Debug.Log("Plugin 2 loaded");
        }
    }

    /// <summary>
    /// This plugin has no ordering constraints and follows the mod menu preference.
    /// </summary>
    [ACPlugin("your-name.plugin-3", "Plugin 3", "2.3")]
    public class Plugin3 : MonoBehaviour, IAnchorChainMod
    {
        void IAnchorChainMod.TriggerEntryPoint()
        {
            Debug.Log("Plugin 3 loaded");
        }
    }

    /// <summary>
    /// This plugin requires Plugin 1 at a minimum version of 0.3.0, and a maximum version of 1.0. If Plugin 1 is not present or is mis-versioned, it will not load.
    /// Presence dependencies do not impose ordering. It follows the menu preference unless another plugin orders it.
    /// </summary>
    [ACPlugin("your-name.plugin-4", "Plugin 4", "1.3.2")]
    [ACDependency("your-name.plugin-1", "0.3.0", "1.0")]
    public class Plugin4 : MonoBehaviour, IAnchorChainMod
    {
        void IAnchorChainMod.TriggerEntryPoint()
        {
            Debug.Log("Plugin 4 loaded");
        }
    }
}
