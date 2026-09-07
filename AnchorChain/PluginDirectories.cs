using SeaPower;

namespace AnchorChain;

internal static class PluginDirectories
{
    internal static bool IsLoader(string path) => Path.GetFileName(path).Contains("AnchorChain", StringComparison.OrdinalIgnoreCase)
        && path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);

    private static bool IsWithin(string path, DirectoryInfo dir) => path.TrimEnd(Path.DirectorySeparatorChar).StartsWith(
        dir.FullName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    // IsEnabled controls checkbox editability. Locked but checked base directories remain active.
    internal static IReadOnlyList<DirectoryInfo> Selected(IEnumerable<SearchDirectory> dirs) => dirs
        .Where(dir => dir.IsChecked && dir.DirectoryInfo.Exists)
        .Select(dir => dir.DirectoryInfo.FullName).Distinct(StringComparer.OrdinalIgnoreCase)
        .Select(path => new DirectoryInfo(path)).ToArray();

    internal static bool IsSelected(string file, IEnumerable<SearchDirectory> dirs)
    {
        file = Path.GetFullPath(file);
        // The most specific registered directory owns the DLL, even when an ancestor is checked.
        var owner = dirs.Where(dir => IsWithin(file, dir.DirectoryInfo))
            .OrderByDescending(dir => dir.DirectoryInfo.FullName.Length).FirstOrDefault();
        return owner is not null && owner.IsChecked && owner.DirectoryInfo.Exists;
    }

    internal static IEnumerable<string> DllFiles(DirectoryInfo dir, IEnumerable<SearchDirectory> dirs)
    {
        var children = dirs.Select(item => item.DirectoryInfo).Where(child => IsWithin(child.FullName, dir)).ToArray();
        return Directory.EnumerateFiles(dir.FullName, "*.dll", SearchOption.AllDirectories)
            // Registered children use their own selection and priority.
            .Where(file => !children.Any(child => IsWithin(file, child)))
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase).ThenBy(file => file, StringComparer.Ordinal);
    }
}
