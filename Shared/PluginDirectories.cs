using SeaPower;

namespace AnchorChain;

internal static class PluginDirectories
{
    internal static bool IsChainLoader(string path) => Path.GetFileName(path).EndsWith("AnchorChain.dll", StringComparison.OrdinalIgnoreCase);
    internal static bool IsLoader(string path) => Path.GetFileName(path).Contains("AnchorChain", StringComparison.OrdinalIgnoreCase)
        && path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);

    // IsEnabled controls checkbox editability. Locked but checked base directories remain active.
    public static IReadOnlyList<DirectoryInfo> Selected(IEnumerable<SearchDirectory> directories) => directories
        .Where(directory => directory.IsChecked && directory.DirectoryInfo.Exists)
        .Select(directory => directory.DirectoryInfo)
        .GroupBy(directory => directory.FullName, StringComparer.OrdinalIgnoreCase)
        .Select(group => group.First()).ToArray();

    internal static bool IsSelected(string file, IEnumerable<SearchDirectory> directories)
    {
        file = Path.GetFullPath(file);
        // The most specific registered directory owns the DLL, even when an ancestor is checked.
        SearchDirectory owner = directories.Where(directory => file.StartsWith(
                directory.DirectoryInfo.FullName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(directory => directory.DirectoryInfo.FullName.Length).FirstOrDefault();
        return owner is not null && owner.IsChecked && owner.DirectoryInfo.Exists;
    }

    public static IEnumerable<string> DllFiles(DirectoryInfo directory, IEnumerable<SearchDirectory> directories)
    {
        string prefix = directory.FullName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string[] nestedRoots = directories.Select(item => item.DirectoryInfo.FullName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar)
            .Where(path => path.Length > prefix.Length && path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToArray();
        return Directory.EnumerateFiles(directory.FullName, "*.dll", SearchOption.AllDirectories)
            // A checked parent must not rediscover an unchecked child mod or steal its load priority.
            .Where(file => !nestedRoots.Any(root => file.StartsWith(root, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase).ThenBy(file => file, StringComparer.Ordinal);
    }
}
