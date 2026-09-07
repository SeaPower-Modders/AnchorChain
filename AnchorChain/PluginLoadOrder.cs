namespace AnchorChain;

internal static class PluginLoadOrder
{
    // Input order is the user's menu preference. Reconsider it after every scheduled plugin.
    internal static IReadOnlyList<ACPlugin> Sort(IReadOnlyList<ACPlugin> preferred)
    {
        List<ACPlugin> pending = new(preferred);
        List<ACPlugin> sorted = new();
        HashSet<string> scheduled = new(StringComparer.Ordinal);
        while (pending.Count > 0)
        {
            int index = pending.FindIndex(plugin => plugin.After.IsSubsetOf(scheduled));
            if (index < 0) throw new InvalidOperationException(
                "Circular or unresolved plugin load order: " + string.Join(", ", pending.Select(plugin => plugin.GUID)));
            ACPlugin next = pending[index];
            pending.RemoveAt(index);
            sorted.Add(next);
            scheduled.Add(next.GUID);
        }
        return sorted;
    }
}
