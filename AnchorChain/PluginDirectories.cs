using SeaPower;

namespace AnchorChain;

internal static class PluginDirectories
{
    private static bool IsWithin(string path, DirectoryInfo dir) => path.TrimEnd(Path.DirectorySeparatorChar).StartsWith(
        dir.FullName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    // IsEnabled controls checkbox editability. Locked but checked base directories remain active.
    internal static List<DirectoryInfo> Selected(IEnumerable<SearchDirectory> dirs) => dirs
        .Where(dir => dir.IsChecked && dir.DirectoryInfo.Exists)
        .Select(dir => dir.DirectoryInfo).ToList();

    internal static bool IsSelected(string file, IEnumerable<SearchDirectory> dirs)
    {
        file = Path.GetFullPath(file);
        // The most specific registered directory owns the DLL, even when an ancestor is checked.
        var owner = dirs.Where(dir => IsWithin(file, dir.DirectoryInfo))
            .OrderByDescending(dir => dir.DirectoryInfo.FullName.Length).FirstOrDefault();
        return owner is not null && owner.IsChecked && owner.DirectoryInfo.Exists;
    }

    internal static IEnumerable<string> DllFiles(DirectoryInfo dir) =>
        Directory.EnumerateFiles(dir.FullName, "*.dll", SearchOption.AllDirectories)
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase).ThenBy(file => file, StringComparer.Ordinal);
}
