using SeaPower;

namespace AnchorChain;

internal static class PluginDirectories
{
    // IsEnabled controls checkbox editability. Locked but checked base directories remain active.
    internal static List<DirectoryInfo> Selected(IEnumerable<SearchDirectory> dirs) => dirs
        .Where(dir => dir.IsChecked && dir.DirectoryInfo.Exists)
        .Select(dir => dir.DirectoryInfo).ToList();

    internal static IEnumerable<string> DllFiles(DirectoryInfo dir) =>
        Directory.EnumerateFiles(dir.FullName, "*.dll", SearchOption.AllDirectories)
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase).ThenBy(file => file, StringComparer.Ordinal);
}
