# Plugin lifecycle

Legacy `IAnchorChainMod.TriggerEntryPoint()` plugins still work unchanged. They
require a game restart when changing the selected DLL mods or their order.

Reload is optional. Implement `IReloadableAnchorChainMod`, or expose these public
methods using the convention introduced by [Lu963's Advanced Chain Loading](https://github.com/Lu963/AnchorChainModified):

```csharp
public Harmony TriggerImprovedEntryPoint();
public void TriggerModUnload();
```

The improved entry point replaces the legacy entry point, not supplements it.
It must return a Harmony instance whose ID equals the plugin's `ACPlugin` GUID.
That ID must not already own patches before initialization. Keep all patches under
that one ID. The loader retains the initialized instance and calls its unload
hook before removing only that instance's Harmony patches. Convention-based
static unload hooks also work. Explicit interface implementations are supported.

Unload must remove event subscriptions, stop background work, destroy persistent
objects, release resources, and clear plugin-owned static state. Entry points must
be safe to run again after that cleanup. During menu reload they run before the
new menu scene loads, so do not retain references to objects in the old scene.
Returning a Harmony instance alone does not make a plugin reloadable.

Plugins unload in reverse initialization order. Cleanup exceptions are logged
individually, and cleanup of the other plugins continues. A failed entry point
also attempts its available cleanup hook. Any initialization or cleanup failure
requires a restart before another reload attempt. Never call global
`Harmony.UnpatchAll()` from an unload hook.

Reload does not unload .NET assemblies or replace DLL binaries. AnchorChain checks
SHA-256 fingerprints of discovered assemblies, including the running loader and
preloader. Changed or missing DLLs require a restart. Changing the selected loader
also requires a restart. New plugins can be discovered, but legacy plugins will
prevent subsequent in-process reloads.

## Minimal example

```csharp
using AnchorChain;
using HarmonyLib;

[ACPlugin("example.reloadable", "Reloadable example", "1.0")]
public class ReloadableExample : IReloadableAnchorChainMod
{
    public void TriggerEntryPoint() => TriggerImprovedEntryPoint();
    public Harmony TriggerImprovedEntryPoint()
    {
        Harmony harmony = new("example.reloadable");
        harmony.PatchAll(typeof(ReloadableExample).Assembly);
        return harmony;
    }
    public void TriggerModUnload()
    {
        // Undo this plugin's non-Harmony state here. AnchorChain removes its patches afterward.
    }
}
```

## Manual verification

Use a development installation and disposable plugins. Check that legacy entry
points run once, improved entry points suppress legacy entry points, and unloading
uses the original instances in reverse order. Leave an unrelated Harmony patch
installed and verify it survives reload. Make one cleanup hook throw and verify
that remaining cleanup still runs and another reload is refused.
