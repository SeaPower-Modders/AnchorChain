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

    internal static void Install()
    {
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
                if (!PluginRuntime.HasChanges(__instance.Directories) && PluginRuntime.RestartReason is null) {
                    original.Execute(null);
                    return;
                }
                string blocker = PluginRuntime.ReloadBlocker(__instance.Directories);
                Show("DLL mods changed", blocker ?? "Save and restart Sea Power, or reload plugins that support cleanup. Unsaved game progress will be lost.",
                    "Save and restart", () => Restart(__instance), blocker is null ? () => Reload(__instance) : null);
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
            PluginRuntime.Unload(menu.Directories);
            FileManager.Instance.SaveDirectories(menu.Directories);
            FileManager.Instance.RefreshSearchDirectories();
            IniHandler.invalidateCache();
            IniHandler.invalidateModifierRegistry();
            // Install plugin patches before the new scene's Awake methods execute.
            AnchorChainLoader.Current.ReloadPlugins();
            if (PluginRuntime.RestartReason is not null) throw new InvalidOperationException(PluginRuntime.RestartReason);
            MenuSystemViewModel.Instance.CurrentWindow = new BlankMenuView();
            PlayerPrefs.SetInt("ApplicationQuitProperly", 1);
            PlayerPrefs.Save();
            SceneManager.LoadScene(0);
        }
        catch {
            PluginRuntime.RequireRestart("Plugin reload did not finish. Restart Sea Power before playing.");
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
