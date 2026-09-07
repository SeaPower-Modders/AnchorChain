# Plugin lifecycle

Every plugin starts through `IAnchorChainMod.TriggerEntryPoint()`. A plugin that
supports reload also implements `IReloadableAnchorChainMod`:

```csharp
public interface IReloadableAnchorChainMod : IAnchorChainMod
{
    Harmony Harmony { get; }
    void Unload();
}
```

Expose the Harmony instance used for your patches, or null if you do not patch
anything. Its ID must belong only to your plugin; it need not match your plugin
GUID. `Unload()` must remove event subscriptions, stop background work, destroy
persistent objects, and reset plugin-owned state.

AnchorChain calls `Unload()` on the original instance, then removes its Harmony
patches with `UnpatchSelf()`. Plugins unload in reverse initialization order.
Cleanup continues if a plugin throws, but another reload is blocked until restart.
A failed entry point also attempts cleanup when the interface is implemented.

Legacy plugins work unchanged, but require a restart when changing DLL mod
selection or order. Reloading reinitializes existing code; it does not unload
assemblies or replace binaries. Always restart after updating a DLL.

## Example

```csharp
[ACPlugin("example.reloadable", "Reloadable example", "1.0")]
public class ReloadableExample : IReloadableAnchorChainMod
{
    public Harmony Harmony { get; } = new("example.reloadable");
    public void TriggerEntryPoint() => Harmony.PatchAll(typeof(ReloadableExample).Assembly);
    public void Unload() { /* Undo non-Harmony state here. */ }
}
```

The entry point must work again after cleanup. Menu reload initializes plugins
before loading the new menu scene, so do not retain objects from the old scene.
Do not use global `Harmony.UnpatchAll()` in cleanup.

The reload feature was inspired by [Lu963's Advanced Chain Loading](https://github.com/Lu963/AnchorChainModified).
