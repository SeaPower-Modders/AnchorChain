using AnchorChain;
using UnityEngine;

namespace ExamplePlugin
{
    public class EntryPoint : MonoBehaviour, IModInterface
    {
        void IModInterface.TriggerEntryPoint()
        {
            Debug.LogError("example load");
        }
    }
}
