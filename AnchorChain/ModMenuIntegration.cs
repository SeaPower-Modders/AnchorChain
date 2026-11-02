using System.Diagnostics;
using System.Reflection;
using System.Windows.Input;
using HarmonyLib;
using SeaPower;
using SeapowerUI;
using SeapowerUI.ViewModels;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnchorChain;

internal static class ModMenuIntegration
{
    private static readonly FieldInfo SaveCommand = AccessTools.Field(typeof(ModMenuViewModel), "<WriteLoadOrderCommand>k__BackingField");
    private static readonly Type MessageType = typeof(ModMenuViewModel).Assembly.GetType("SeapowerUI.ViewModels.MessageBoxViewModel", true);
    private static bool _installed;
    private static string[] _dllPaths = [];
    private static Action _reloadPlugins;

    private static string[] DllPaths(IEnumerable<SearchDirectory> source)
    {
        var directories = source.ToArray();
        return PluginDirectories.Selected(directories)
            .SelectMany(directory => PluginDirectories.DllFiles(directory, directories))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static bool CanReload(string[] paths) => PluginRuntime.CanReload && _dllPaths.Where(PluginDirectories.IsLoader)
        .SequenceEqual(paths.Where(PluginDirectories.IsLoader), StringComparer.OrdinalIgnoreCase);

    internal static void Install(Action reloadPlugins)
    {
        _dllPaths = DllPaths(FileManager.Instance.Directories);
        _reloadPlugins = reloadPlugins;
        if (_installed) return;
        if (SaveCommand is null) throw new MissingFieldException(typeof(ModMenuViewModel).FullName, "WriteLoadOrderCommand");
        new Harmony("io.github.seapower_modders.anchorchain.menu").Patch(
            AccessTools.Constructor(typeof(ModMenuViewModel)),
            postfix: new HarmonyMethod(typeof(ModMenuIntegration), nameof(Attach)));
        _installed = true;
    }

    private static void Attach(ModMenuViewModel __instance)
    {
        if (__instance.WriteLoadOrderCommand is not ICommand original) return;
        SaveCommand.SetValue(__instance, new DelegateCommand(_ => {
            try {
                string[] paths = DllPaths(__instance.Directories);
                if (_dllPaths.SequenceEqual(paths, StringComparer.OrdinalIgnoreCase) && !PluginRuntime.Failed) {
                    original.Execute(null);
                    return;
                }
                bool canReload = CanReload(paths);
                string message = canReload
                    ? "Save and restart, or reload plugins. Always restart after updating a DLL; reload reuses existing code."
                    : "The loader changed, a plugin cannot unload, or loading failed. Save and restart to apply these changes.";
                Show("DLL mods changed", message + "\nUnsaved game progress will be lost.",
                    "Save and restart", () => Restart(__instance), canReload ? () => Reload(__instance) : null);
            }
            catch (Exception error) { ShowError(error); }
        }));
    }

    private static void Show(string header, string message, string actionLabel, Action action, Action reload = null)
    {
        object dialog = Activator.CreateInstance(MessageType);
        void Set(string name, object value) => MessageType.GetProperty(name).SetValue(dialog, value);
        void Close() => WindowHostCommands.CloseWindowCommand.Execute(dialog, ModMenuView.Instance);
        ICommand Command(Action callback) => new DelegateCommand(_ => {
            Close();
            try { callback(); }
            catch (Exception error) { ShowError(error); }
        });
        Set("Header", header);
        Set("Message", message);
        Set("MessageBoxType", MessageBoxType.Warning);
        Set("AcknowledgeCommandLabel", actionLabel);
        Set("AcknowledgeCommand", Command(action));
        if (reload is not null) {
            Set("MiddleCommandLabel", "Reload plugins");
            Set("MiddleCommand", Command(reload));
        }
        Set("CancelCommand", new DelegateCommand(_ => Close()));
        WindowHostCommands.OpenWindowCommand.Execute(dialog, ModMenuView.Instance);
    }

    private static void ShowError(Exception error)
    {
        UnityEngine.Debug.LogError($"AnchorChain mod-menu operation failed: {error}");
        Show("AnchorChain", error.Message + "\nChanges may require a manual restart. See the game log for details.", "OK", () => { });
    }

    private static void Reload(ModMenuViewModel menu)
    {
        try {
            if (!CanReload(DllPaths(menu.Directories))) throw new InvalidOperationException("These plugins require a restart.");
            PluginRuntime.Unload();
            FileManager.Instance.SaveDirectories(menu.Directories);
            FileManager.Instance.RefreshSearchDirectories();
            IniHandler.invalidateCache();
            IniHandler.invalidateModifierRegistry();
            // Install plugin patches before the new scene's Awake methods execute.
            _reloadPlugins();
            if (PluginRuntime.Failed) throw new InvalidOperationException("Plugin loading failed. Restart Sea Power.");
            MenuSystemViewModel.Instance.CurrentWindow = new BlankMenuView();
            PlayerPrefs.SetInt("ApplicationQuitProperly", 1);
            PlayerPrefs.Save();
            SceneManager.LoadScene(0);
        }
        catch {
            PluginRuntime.Failed = true;
            throw;
        }
    }

    private static void Restart(ModMenuViewModel menu)
    {
        FileManager.Instance.SaveDirectories(menu.Directories);
        // Steam ignores launches while the game is still running. Wait for this exact process to exit.
        int processId = Process.GetCurrentProcess().Id;
        string command = $"Wait-Process -Id {processId} -Timeout 60 -ErrorAction SilentlyContinue; " +
            $"if (-not (Get-Process -Id {processId} -ErrorAction SilentlyContinue)) {{ Start-Process 'steam://rungameid/1286220' }}";
        using var helper = Process.Start(new ProcessStartInfo {
            FileName = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe"),
            Arguments = "-NoProfile -NonInteractive -WindowStyle Hidden -EncodedCommand " + Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(command)),
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        });
        if (helper is null) throw new InvalidOperationException("Could not start the restart helper. Restart Sea Power manually.");
        Application.Quit();
    }
}
