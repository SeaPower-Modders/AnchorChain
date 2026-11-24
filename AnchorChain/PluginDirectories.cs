using SeaPower;

namespace AnchorChain;

internal static class PluginDirectories
{
    private static bool IsWithin(string path, DirectoryInfo directory)
    {
        string relative = Path.GetRelativePath(directory.FullName, path);
        return relative != "." && relative != ".." && !Path.IsPathRooted(relative)
            && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }

    // IsEnabled controls checkbox editability. Locked but checked base directories remain active.
    internal static IEnumerable<DirectoryInfo> Selected(IEnumerable<SearchDirectory> dirs) => dirs
        .Where(dir => dir.IsChecked && dir.DirectoryInfo.Exists)
        .Select(dir => dir.DirectoryInfo.FullName).Distinct(StringComparer.OrdinalIgnoreCase)
        .Select(path => new DirectoryInfo(path));

    internal static bool IsSelected(string file, IEnumerable<SearchDirectory> dirs)
    {
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
