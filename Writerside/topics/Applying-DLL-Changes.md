# Applying DLL changes

The mod menu keeps Sea Power's normal apply flow when the selected DLL paths,
contents, and ordering are unchanged. When those change, AnchorChain offers
**Save and restart**, **Cancel**, and, when every loaded plugin supports cleanup,
**Reload plugins**. The dialog explains why a restart is required when reload is
unavailable. Applying either operation can discard unsaved game progress.

Cancel does not save or apply the menu selection. Restart saves the selection,
starts a hidden Windows helper, and quits Sea Power. The helper waits up to 60
seconds for that game process to exit before requesting a Steam launch. If the
game does not exit, it does not launch another copy. If the helper cannot start,
the game stays open and displays an error. The menu selection may already be saved.

Reload first checks eligibility, then unloads plugins, saves and refreshes the
directories, clears INI caches, reloads plugins, and loads the menu scene. Failed
reloads require a restart. DLL assemblies cannot be hot-replaced. See
[Plugin lifecycle](Plugin-Lifecycle.md) for the cleanup contract.

This uses Sea Power's existing confirmation dialog. The loader's menu patch stays
installed across plugin reloads. It never globally removes Harmony patches.

## Manual verification

- Apply data-only changes and confirm the native prompt remains.
- Change a DLL mod's checkbox or menu position and confirm the DLL prompt appears.
- Cancel and confirm nothing was applied or saved.
- With a legacy plugin loaded, confirm reload is unavailable and restart is offered.
- With only reloadable plugins, reload twice and check for duplicate hooks or objects.
- Replace a DLL without changing its path and confirm restart is required.
- Disable AnchorChain itself and confirm restart is required. With an old preloader,
  verify the disabled loader logs that it is skipping initialization on the next launch.
- Exercise restart and helper failure in a development installation.

Menu controls and lifecycle conventions were inspired by
[Lu963's Advanced Chain Loading](https://github.com/Lu963/AnchorChainModified).
