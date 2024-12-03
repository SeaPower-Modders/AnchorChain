using AnchorChain;
using UnityEngine;

namespace ExamplePlugin
{
    [ACPlugin("test.1", "Hi", "1.0", ["test.2"])]
    public class EntryPoint : MonoBehaviour, IModInterface
    {
        void IModInterface.TriggerEntryPoint()
        {
            Debug.LogError("example load");
        }
    }

    [ACPlugin("test.2", "Hi", "1.0", [], ["test.1"])]
    public class NotEntryPoint1 : MonoBehaviour, IModInterface
    {
        void IModInterface.TriggerEntryPoint()
        {
            Debug.LogError("example load");
        }
    }

    [ACPlugin("test.3", "Hi", "1.0", [], ["test.2"])]
    [ACIncompatibility("test.4")]
    public class NotEntryPoint2 : MonoBehaviour, IModInterface
    {
        void IModInterface.TriggerEntryPoint()
        {
            Debug.LogError("example load");
        }
    }

    [ACPlugin("test.4", "Hi", "1.0", [], ["test.1"])]
    public class NotEntryPoint3 : MonoBehaviour, IModInterface
    {
        void IModInterface.TriggerEntryPoint()
        {
            Debug.LogError("example load");
        }
    }
}
