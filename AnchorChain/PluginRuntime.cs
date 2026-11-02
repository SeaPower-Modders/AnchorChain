using HarmonyLib;

namespace AnchorChain;

public interface IReloadableAnchorChainMod : IAnchorChainMod
{
    Harmony Harmony { get; }
    void Unload();
}

internal static class PluginRuntime
{
    private static readonly List<IAnchorChainMod> Loaded = [];
    internal static bool Failed { get; set; }
    internal static bool CanReload => !Failed && Loaded.All(plugin => plugin is IReloadableAnchorChainMod);

    internal static void Start(Type type)
    {
        IAnchorChainMod plugin = null;
        try {
            plugin = (IAnchorChainMod)Activator.CreateInstance(type);
            plugin.TriggerEntryPoint();
            Loaded.Add(plugin);
        }
        catch {
            Failed = true;
            if (plugin is IReloadableAnchorChainMod reloadable) Cleanup(reloadable);
            throw;
        }
    }

    private static void Cleanup(IReloadableAnchorChainMod plugin)
    {
        try {
            var harmony = plugin.Harmony;
            try { plugin.Unload(); }
            finally { harmony?.UnpatchSelf(); }
        }
        catch (Exception error) {
            Failed = true;
            UnityEngine.Debug.LogError($"AnchorChain could not unload {plugin.GetType().FullName}: {error}");
        }
    }

    internal static void Unload()
    {
        if (!CanReload) throw new InvalidOperationException("One or more plugins cannot reload. Restart Sea Power.");
        foreach (var plugin in Loaded.Cast<IReloadableAnchorChainMod>().Reverse()) Cleanup(plugin);
        Loaded.Clear();
        if (Failed) throw new InvalidOperationException("Plugin cleanup failed. Restart Sea Power.");
    }
}
