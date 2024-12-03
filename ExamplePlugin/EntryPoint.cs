using AnchorChain;
using UnityEngine;

namespace ExamplePlugin
{
    [ACPlugin("test.test", "Hi", "1.0", ["test.other"])]
    [ACDependency("test.other", "1.0", "2.0")]
    public class EntryPoint : MonoBehaviour, IModInterface
    {
        void IModInterface.TriggerEntryPoint()
        {
            Debug.LogError("example load");
        }
    }

    [ACPlugin("test.other", "Hi", "1.0", ["test.test"])]
    [ACDependency("test.test", "1.0", "2.0")]
    public class NotEntryPoint : MonoBehaviour, IModInterface
    {
        void IModInterface.TriggerEntryPoint()
        {
            Debug.LogError("example load");
        }
    }
}
