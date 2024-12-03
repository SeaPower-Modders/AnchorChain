using AnchorChain;
using UnityEngine;

namespace ExamplePlugin
{
    [ACPlugin("test.test", "Hi", "1.0", ["otherother"])]
    [ACDependency("other", "1.0", "2.0")]
    public class EntryPoint : MonoBehaviour, IModInterface
    {
        void IModInterface.TriggerEntryPoint()
        {
            Debug.LogError("example load");
        }
    }
}
