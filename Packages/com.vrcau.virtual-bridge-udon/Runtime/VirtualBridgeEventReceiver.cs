using JetBrains.Annotations;
using UdonSharp;

namespace VirtualBridge
{
    public class VirtualBridgeEventReceiver : UdonSharpBehaviour
    {
        [PublicAPI] public virtual void OnVirtualBridgeDataReceived(string data) {}
    }
}