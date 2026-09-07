using System.Reflection;
using System.Security.Cryptography;
using HarmonyLib;
using SeaPower;

namespace AnchorChain;

// Optional contract. The matching public methods also work without implementing this interface.
public interface IReloadableAnchorChainMod : IAnchorChainMod
{
    Harmony TriggerImprovedEntryPoint();
    void TriggerModUnload();
}

internal static class PluginRuntime
{
    private class LoadedPlugin(string guid, Action unload, Harmony harmony)
    {
        internal string Guid { get; } = guid;
        internal Action Unload { get; } = unload;
        internal Harmony Harmony { get; } = harmony;
        internal bool CanReload => Unload is not null && Harmony is not null;
    }

    private static readonly List<LoadedPlugin> Loaded = new();
    private static readonly Dictionary<string, string> AssemblyHashes = new(StringComparer.OrdinalIgnoreCase);
    private static KeyValuePair<string, string>[] _selection = [];
    private static string _restartReason;
    internal static string RestartReason => _restartReason;

    private static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        using var sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(stream));
    }

    private static KeyValuePair<string, string>[] Snapshot(IEnumerable<SearchDirectory> source)
    {
        var directories = source.ToArray();
        return PluginDirectories.Selected(directories)
            .SelectMany(directory => PluginDirectories.DllFiles(directory, directories))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => new KeyValuePair<string, string>(path, Hash(path))).ToArray();
    }

    internal static void CaptureSelection(IEnumerable<SearchDirectory> directories)
    {
        _selection = Snapshot(directories);
        RememberAssembly(typeof(AnchorChainLoader).Assembly.Location);
        RememberAssembly(typeof(Preloader.IPluginLoader).Assembly.Location);
    }

    internal static void RememberAssembly(string path)
    {
        string hash = Hash(path);
        if (AssemblyHashes.TryGetValue(path, out string previous) && previous != hash)
            throw new InvalidOperationException($"DLL changed on disk; restart required: {path}");
        AssemblyHashes[path] = hash;
    }

    internal static bool HasChanges(IEnumerable<SearchDirectory> directories) => !_selection.SequenceEqual(Snapshot(directories));
    internal static void RequireRestart(string reason) => _restartReason ??= reason;

    internal static string ReloadBlocker(IEnumerable<SearchDirectory> directories)
    {
        if (_restartReason is not null) return _restartReason;
        if (!_selection.Where(item => PluginDirectories.IsLoader(item.Key)).SequenceEqual(Snapshot(directories).Where(item => PluginDirectories.IsLoader(item.Key))))
            return "The selected loader changed. A restart is required.";
        foreach (var assembly in AssemblyHashes)
            if (!File.Exists(assembly.Key) || Hash(assembly.Key) != assembly.Value)
                return $"A loaded DLL changed or was removed: {Path.GetFileName(assembly.Key)}. A restart is required.";
        var legacy = Loaded.Where(plugin => !plugin.CanReload).Select(plugin => plugin.Guid).ToArray();
        return legacy.Length == 0 ? null : "These plugins do not support safe reload: " + string.Join(", ", legacy);
    }

    internal static void Start(ACPlugin metadata, Type type)
    {
        object instance = null;
        Harmony harmony = null;
        Action unload = null;
        try {
            bool ownsPatches = !Harmony.HasAnyPatches(metadata.GUID);
            instance = Activator.CreateInstance(type);
            MethodInfo improved = type.GetMethod("TriggerImprovedEntryPoint", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            MethodInfo cleanup = type.GetMethod("TriggerModUnload", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static, null, Type.EmptyTypes, null);
            if (instance is IReloadableAnchorChainMod reloadable) {
                unload = reloadable.TriggerModUnload;
                harmony = reloadable.TriggerImprovedEntryPoint();
            }
            else if (improved?.ReturnType == typeof(Harmony)) {
                if (cleanup?.ReturnType == typeof(void)) unload = () => cleanup.Invoke(instance, null);
                harmony = (Harmony)improved.Invoke(instance, null);
            }
            else {
                ((IAnchorChainMod)instance).TriggerEntryPoint();
                Loaded.Add(new(metadata.GUID, null, null));
                return;
            }

            // Never claim or remove another plugin's patches, including the loader's own hooks.
            if (!ownsPatches || harmony is null || harmony.Id != metadata.GUID || metadata.GUID.StartsWith("io.github.seapower_modders.anchorchain", StringComparison.Ordinal)) {
                harmony = null;
                RequireRestart($"{metadata.GUID} did not return its own GUID-scoped Harmony instance.");
            }
            Loaded.Add(new(metadata.GUID, unload, harmony));
        }
        catch {
            RequireRestart($"{metadata.GUID} failed during initialization.");
            Cleanup(new(metadata.GUID, unload, harmony));
            throw;
        }
    }

    private static void Cleanup(LoadedPlugin plugin)
    {
        try { plugin.Unload?.Invoke(); }
        catch (Exception error) {
            RequireRestart($"{plugin.Guid} failed to unload.");
            UnityEngine.Debug.LogError($"AnchorChain cleanup failed for {plugin.Guid}: {error}");
        }
        finally {
            try { plugin.Harmony?.UnpatchSelf(); }
            catch (Exception error) {
                RequireRestart($"{plugin.Guid} patches could not be removed.");
                UnityEngine.Debug.LogError($"AnchorChain unpatch failed for {plugin.Guid}: {error}");
            }
        }
    }

    internal static void Unload(IEnumerable<SearchDirectory> directories)
    {
        string blocker = ReloadBlocker(directories);
        if (blocker is not null) throw new InvalidOperationException(blocker);
        for (int index = Loaded.Count - 1; index >= 0; index--) Cleanup(Loaded[index]);
        Loaded.Clear();
        if (_restartReason is not null) throw new InvalidOperationException(_restartReason);
    }
}
