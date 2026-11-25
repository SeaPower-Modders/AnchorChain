using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using SeaPower;
using SeapowerUI;
using SeapowerUI.ViewModels;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace AnchorChain;

/// <summary>
/// Anchor Chain continues as R.A.T.S. Keel (agreed with Anchor Chain's authors, November 2026). Once per game start, on the
/// main menu, a game message box offers the switch:
/// <list type="bullet">
/// <item>Switch to Keel: subscribes to Keel's Workshop item, waits for Steam to download it, puts Keel's stub into
/// BepInEx\plugins and its takeover patcher into BepInEx\patchers (recorded in keel-setup.txt, so Keel's setup can undo it),
/// then switches Keel on at the top of the game's mod list and offers a restart. A step that fails takes the others back.</item>
/// <item>Not now: nothing changes; the box comes again at the next start.</item>
/// <item>Don't show this again: a check box next to the buttons (the game's own check box style; where the box's layout
/// differs, its middle button stands in as a tick). Ticked, the box stays away until ShowSwitchOffer is set back in Anchor
/// Chain's config file.</item>
/// </list>
/// Anchor Chain itself loads every mod as before at this start. Nothing here can stop that: every step is caught, and a game
/// build whose UI differs only loses the box. Players who have Keel already (its stub is in BepInEx\plugins) never see it.
/// </summary>
internal sealed class KeelBridge : MonoBehaviour
{
	internal const ulong KeelItem = 3835527459;
	private const string StubName = "SeaPowerKeel.Stub.dll";
	private const string StubFolder = "copy-to-BepInEx-plugins";
	private const string PatcherName = "SeaPowerKeel.Patcher.dll";
	private const string WatcherName = "KeelWatcher.exe";
	private const string ManifestName = "keel-setup.txt";
	private const string Header = "MESSAGE  ·  Anchor Chain";
	private const float DownloadSeconds = 300f;
	private const float BrokenSeconds = 15f;     // installed per Steam but no stub this long: a damaged copy, not a slow one
	private const string ConfigName = "io.github.seapower_modders.anchorchain.cfg";
	private const string TickOff = "[ ] Don't show this again";
	private const string TickOn = "[x] Don't show this again";

	private const string OfferText =
		"Anchor Chain continues as R.A.T.S. Keel, made together with Anchor Chain's authors.\n\n" +
		"• Your mods keep working: Keel runs every Anchor Chain mod as it is\n" +
		"• Keel checks each mod against your game version and names the one that breaks\n" +
		"• Switch to Keel: subscribes to it, installs it and restarts the game. Keel's setup (KeelSetup.exe) can undo it\n" +
		"• Not now: Anchor Chain keeps working, and this asks again at the next start\n" +
		"• Don't show this again: tick it below, and this offer stays away";

	private static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("Anchor Chain → Keel");
	private static bool begun;
	// Whether the offer is shown at all. The box's tick writes it; a player sets it back in the config file.
	private static ConfigEntry<bool>? showOffer;

	private object? box;
	private object? context;
	private bool cancelled;

	/// <summary>Called by the loader once its folder is switched on. Never throws.</summary>
	internal static void Begin()
	{
		if (begun) { return; }
		begun = true;
		try {
			if (File.Exists(Path.Combine(Paths.PluginPath, StubName))) {
				Log.LogInfo("R.A.T.S. Keel is installed already: no switch offer.");
				return;
			}
			showOffer = new ConfigFile(Path.Combine(Paths.ConfigPath, ConfigName), true).Bind("Keel", "ShowSwitchOffer", true,
				"Offer the switch to R.A.T.S. Keel on the main menu at every game start. \"Don't show this again\" in that box sets it to false.");
			if (!showOffer.Value) {
				Log.LogInfo($@"The switch offer is switched off (ShowSwitchOffer in BepInEx\config\{ConfigName}).");
				return;
			}
			var host = new GameObject("AnchorChain.KeelBridge");
			DontDestroyOnLoad(host);
			host.AddComponent<KeelBridge>();
		}
		catch (Exception e) {
			Log.LogWarning($"No switch offer: {e.Message}");
		}
	}

	private void Start() => StartCoroutine(OfferWhenMenuIsUp());

	private IEnumerator OfferWhenMenuIsUp()
	{
		// The settled main menu: after the load screen and its questions, so the box does not land behind them.
		Log.LogInfo("Waiting for the main menu to offer the switch.");
		float stable = 0f, waited = 0f;
		bool told = false;
		while (stable < 2f) {
			stable = OnMainMenu() ? stable + Time.unscaledDeltaTime : 0f;
			waited += Time.unscaledDeltaTime;
			if (!told && waited > 300f) {
				told = true;
				Log.LogWarning("No main menu after five minutes; still waiting to offer the switch.");
			}
			yield return null;
		}
		Log.LogInfo("Main menu is up.");
		try {
			Show(OfferText, "Switch to Keel", Switch, "Not now", () => Log.LogInfo("Not now: Anchor Chain stays this time."), withTick: true);
			Log.LogInfo("Switch offer shown.");
		}
		catch (Exception e) {
			Log.LogWarning($"The switch offer could not be shown: {e.Message}");
		}
	}

	private static bool OnMainMenu()
	{
		try {
			return MenuUp();
		}
		catch (Exception) {
			return false;   // a game build without these views: no box
		}
	}

	// The start-up load screen is done (the same test as Anchor Chain's preloader and Keel) and the main menu view exists, with no
	// mission or campaign scene loaded. LoadScreenView.Instance cannot tell it: the game sets it once and never clears it (VM test 2026-11-18).
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool MenuUp() =>
		Singleton<UnitMetadataProvider>.InstanceExists() && Singleton<UnitMetadataProvider>.Instance.UnitData != null &&
		Singleton<LoadScreen>.InstanceExists() && Singleton<LoadScreen>.Instance.LoadText == "Done" &&
		MainMenuView.Instance != null &&
		!UnityEngine.SceneManagement.SceneManager.GetSceneByName("mission").isLoaded &&
		!UnityEngine.SceneManagement.SceneManager.GetSceneByName("linear-campaign").isLoaded;

	// ---- the game's message box ----

	// The game's message box class is internal: built and filled by name (the same names on every 0.9 build).
	[MethodImpl(MethodImplOptions.NoInlining)]
	private void Show(string message, string okLabel, Action ok, string? cancelLabel = null, Action? cancel = null, bool withTick = false)
	{
		CloseBox();
		Type type = typeof(MessageBoxType).Assembly.GetType("SeapowerUI.ViewModels.MessageBoxViewModel", true)!;
		object b = Activator.CreateInstance(type, true)!;
		void Set(string name, object? value) => type.GetProperty(name, Any)!.SetValue(b, value);
		Set("MessageBoxType", MessageBoxType.Info);
		Set("Header", Header);
		Set("Message", message);
		Set("AcknowledgeCommandLabel", okLabel);
		Set("AcknowledgeCommand", new DelegateCommand(_ => { CloseBox(); Run(ok); }));
		if (cancelLabel != null) {
			Set("CancelCommandLabel", cancelLabel);
			Set("CancelCommand", new DelegateCommand(_ => { CloseBox(); if (cancel != null) { Run(cancel); } }));
		}
		DelegateCommand? tick = null;
		if (withTick) {
			// The game's message box has three buttons and no check box: the middle one is the tick until a real check box replaces it.
			tick = new DelegateCommand(_ => Run(ToggleTick));
			Set("MiddleCommandLabel", TickLabel());
			Set("MiddleCommand", tick);
		}
		context = MainMenuView.Instance;
		box = b;
		WindowHostCommands.OpenWindowCommand.Execute(b, (Noesis.UIElement)context);
		if (tick != null) { StartCoroutine(AddCheckBox(b, tick)); }
	}

	// ---- the check box ----

	// The box's view is drawn a frame or two after it opens: look for its tick button for up to a second.
	private IEnumerator AddCheckBox(object shown, DelegateCommand tick)
	{
		for (float until = Time.realtimeSinceStartup + 1f; Time.realtimeSinceStartup < until; ) {
			yield return null;
			if (box != shown) { yield break; }    // closed or replaced meanwhile
			try {
				if (PutCheckBox(tick)) { yield break; }
			}
			catch (Exception e) {
				Log.LogInfo($"No check box in the offer, the tick button stays: {e.Message}");
				yield break;
			}
		}
		Log.LogInfo("No check box in the offer (the box's layout differs): the tick button stays.");
	}

	/// <summary>Puts a check box where the tick button is and hides that button. False while the button is not drawn yet.</summary>
	[MethodImpl(MethodImplOptions.NoInlining)]
	private bool PutCheckBox(DelegateCommand tick)
	{
		Noesis.DependencyObject root = (Noesis.DependencyObject)context!;
		for (Noesis.DependencyObject? up = root; up != null; up = Noesis.VisualTreeHelper.GetParent(up)) { root = up; }
		if (FindButton(root, tick) is not { } button) { return false; }
		if (Noesis.VisualTreeHelper.GetParent(button) is not Noesis.Panel row) { throw new InvalidOperationException("the buttons sit in no panel"); }

		var check = new Noesis.CheckBox {
			Content = "Don't show this again",
			IsChecked = showOffer is { Value: false },
			VerticalAlignment = Noesis.VerticalAlignment.Center,
			Margin = new Noesis.Thickness(0, 0, 16, 0),
		};
		if (button.Foreground != null) { check.Foreground = button.Foreground; }
		check.Click += (_, _) => Run(() => SetShow(check.IsChecked != true));
		row.Children.Insert(row.Children.IndexOf(button), check);
		button.Visibility = Noesis.Visibility.Collapsed;
		Log.LogInfo("Check box added to the offer.");
		return true;
	}

	// Breadth first through the whole UI: the window host is not below the main menu view. Bounded, so a large tree cannot stall a frame.
	private static Noesis.Button? FindButton(Noesis.DependencyObject root, DelegateCommand tick)
	{
		var queue = new Queue<Noesis.DependencyObject>();
		queue.Enqueue(root);
		for (int seen = 0; queue.Count > 0 && seen < 20000; seen++) {
			Noesis.DependencyObject node = queue.Dequeue();
			if (node is Noesis.Button b && ReferenceEquals(b.Command, tick)) { return b; }
			int n = Noesis.VisualTreeHelper.GetChildrenCount(node);
			for (int i = 0; i < n; i++) { queue.Enqueue(Noesis.VisualTreeHelper.GetChild(node, i)); }
		}
		return null;
	}

	private static string TickLabel() => showOffer is { Value: false } ? TickOn : TickOff;

	// Saved at once (BepInEx writes the file when the value is set), so closing the box with its X counts too. The box stays open.
	private void ToggleTick()
	{
		if (showOffer == null) { return; }
		SetShow(!showOffer.Value);
		try {
			box?.GetType().GetProperty("MiddleCommandLabel", Any)?.SetValue(box, TickLabel());
		}
		catch (Exception) {
			// the box is gone; the setting is saved all the same
		}
	}

	private static void SetShow(bool show)
	{
		if (showOffer == null || showOffer.Value == show) { return; }
		showOffer.Value = show;
		Log.LogInfo(show ? "Tick taken off: the offer comes again at the next start." : "Ticked: the offer will not be shown again.");
	}

	private const System.Reflection.BindingFlags Any =
		System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

	private void CloseBox()
	{
		if (box == null) { return; }
		try {
			CloseCore();
		}
		catch (Exception) {
			// already gone with its view
		}
		box = null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void CloseCore() => WindowHostCommands.CloseWindowCommand.Execute(box, (Noesis.UIElement)context!);

	private void SetText(string message)
	{
		try {
			box?.GetType().GetProperty("Message", Any)?.SetValue(box, message);
		}
		catch (Exception) {
			// the box is gone; the log still says what happens
		}
	}

	private static void Run(Action action)
	{
		try {
			action();
		}
		catch (Exception e) {
			Log.LogError($"Switch step failed: {e}");
		}
	}

	// ---- the switch ----

	private enum Download { NotStarted, Running, Done, Broken }

	private void Switch() => StartCoroutine(SwitchSteps());

	// Download, install, switch on, offer the restart. Each step that fails says why and leaves the game as it was before the click.
	private IEnumerator SwitchSteps()
	{
		Log.LogInfo("Switch to Keel: getting it from the Workshop.");
		cancelled = false;
		subscribedNow = false;
		Show("Getting R.A.T.S. Keel from the Steam Workshop ...", "Cancel", CancelDownload);

		string? folder = null;
		yield return GetKeel(path => folder = path);
		if (cancelled || folder == null) { yield break; }

		Log.LogInfo($"Keel is on disk: {folder}");
		var placed = new Placed();
		try {
			// Files first, the mod list last: the game only loads Keel once its stub is there, so a half switch never lists it.
			Install(folder, placed);
			SwitchOn(folder);
		}
		catch (Exception e) {
			Log.LogError($"Keel could not be installed: {e}");
			placed.Undo();
			Show($"R.A.T.S. Keel is downloaded but could not be installed: {Reason(e)}.\n\n" +
			     "• Nothing was changed: Anchor Chain keeps working\n" +
			     "• Close the game and run KeelSetup.exe from Keel's Workshop folder instead",
			     "Open the Workshop page", OpenPage, "Close");
			yield break;
		}
		Log.LogInfo("Keel is installed and switched on; offering the restart.");
		Show("R.A.T.S. Keel is installed and switched on.\n\n" +
		     "• Restart: the game closes and starts again with Keel\n" +
		     "• Later: Keel takes over the next time you start the game",
		     "Restart", () => StartCoroutine(Restart(folder)), "Later", () => Log.LogInfo("Restart later."));
	}

	private bool subscribedNow;     // this click subscribed to Keel, so a cancel takes the subscription back

	private void CancelDownload()
	{
		cancelled = true;
		Log.LogInfo("Switch cancelled while downloading.");
		if (!subscribedNow) { return; }
		try {
			Unsubscribe();
		}
		catch (Exception e) {
			Log.LogWarning($"Could not take the Keel subscription back: {e.Message}");
		}
	}

	/// <summary>Waits for Steam to have Keel complete; hands over its folder, or null after telling the player why not.</summary>
	private IEnumerator GetKeel(Action<string> found)
	{
		string? failure = null;
		Download state = Download.NotStarted;
		string folder = "";
		try {
			state = State(out folder);
			if (state == Download.NotStarted) {
				Subscribe();
				subscribedNow = true;
			}
		}
		catch (Exception e) {
			failure = e.Message;
		}

		float until = Time.realtimeSinceStartup + DownloadSeconds;
		float brokenSince = -1f;
		while (failure == null && !cancelled && state != Download.Done && Time.realtimeSinceStartup < until) {
			yield return new WaitForSecondsRealtime(0.5f);
			try {
				state = State(out folder);
				if (state == Download.Running) { SetText($"Getting R.A.T.S. Keel from the Steam Workshop ... {Progress()}"); }
				// Steam calls the item installed once its files are written: a folder without the stub a while later is a broken copy.
				if (state != Download.Broken) { brokenSince = -1f; }
				else if (brokenSince < 0f) { brokenSince = Time.realtimeSinceStartup; }
				else if (Time.realtimeSinceStartup - brokenSince > BrokenSeconds) {
					failure = $"its Workshop folder has no {StubName}; Steam's copy looks damaged";
				}
			}
			catch (Exception e) {
				failure = e.Message;
			}
		}
		if (cancelled) { yield break; }
		if (state == Download.Done) {
			found(folder);
			yield break;
		}
		string why = failure ?? (state == Download.NotStarted
			? "Steam did not start the download (is Steam offline?)"
			: $"Steam was still downloading it after {DownloadSeconds / 60f:0} minutes");
		Log.LogWarning($"Keel was not downloaded: {why}.");
		Show($"R.A.T.S. Keel could not be downloaded: {why}.\n\n" +
		     "• Anchor Chain keeps working meanwhile\n" +
		     "• Keel stays subscribed: once Steam has it, close the game and run KeelSetup.exe from its Workshop folder\n" +
		     "• Don't want it? Unsubscribe on its Workshop page",
		     "Open the Workshop page", OpenPage, "Close");
	}

	private static void OpenPage() => Application.OpenURL($"steam://url/CommunityFilePage/{KeelItem}");

	/// <summary>Where Steam is with Keel's item; the folder when it is complete.</summary>
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static Download State(out string folder)
	{
		folder = "";
		uint state = Steam.ItemState();
		if ((state & Steam.Subscribed) == 0) { return Download.NotStarted; }
		if ((state & (Steam.Downloading | Steam.DownloadPending)) != 0) { return Download.Running; }
		if ((state & Steam.Installed) == 0 || (state & Steam.NeedsUpdate) != 0) {
			Steam.Download();   // subscribed but neither there nor coming: ask once more (Steam ignores repeats)
			return Download.Running;
		}
		string? path = Steam.InstallFolder();
		if (path == null || !Directory.Exists(path)) { return Download.Broken; }
		if (!File.Exists(Path.Combine(path, StubFolder, StubName))) { return Download.Broken; }
		folder = path;
		return Download.Done;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void Subscribe()
	{
		if (!SteamManager.Initialized) { throw new InvalidOperationException("Steam is not running"); }
		Steam.Call("SubscribeItem", Steam.Item());
		Steam.Download();
		Log.LogInfo("Subscribed to R.A.T.S. Keel; Steam downloads it.");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void Unsubscribe()
	{
		Steam.Call("UnsubscribeItem", Steam.Item());
		Log.LogInfo("Keel subscription taken back.");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static string Progress()
	{
		object?[] args = { Steam.Item(), 0UL, 0UL };
		return Steam.Call("GetItemDownloadInfo", args) is true && args[2] is ulong total && total > 0 && args[1] is ulong done
			? $"{done * 100 / total} %"
			: "";
	}

	/// <summary>The player's words for a failed install step; the full exception stays in the log.</summary>
	private static string Reason(Exception e) => e switch {
		UnauthorizedAccessException => "Windows did not let the game write into its BepInEx folder",
		IOException => "a file in the BepInEx folder is in use or could not be written",
		_ => e.Message,
	};

	// ---- install, undone on failure ----

	/// <summary>What one install placed, so a failed switch can take it back.</summary>
	private sealed class Placed
	{
		internal readonly List<string> Created = new();
		internal readonly List<(string File, string Backup)> Replaced = new();
		internal string? Manifest;
		internal string[]? ManifestBefore;     // null: the file was not there

		internal void Undo()
		{
			foreach (string file in Created) { Try(() => File.Delete(file)); }
			foreach ((string file, string backup) in Replaced) { Try(() => File.Copy(backup, file, true)); }
			if (Manifest != null) {
				Try(() => {
					if (ManifestBefore == null) { File.Delete(Manifest); }
					else { File.WriteAllLines(Manifest, ManifestBefore); }
				});
			}
			Log.LogInfo("The half install was taken back.");
		}

		private static void Try(Action step)
		{
			try {
				step();
			}
			catch (Exception e) {
				Log.LogWarning($"Could not take a step back: {e.Message}");
			}
		}
	}

	/// <summary>What KeelSetup.exe does when BepInEx is there already: Keel's stub and takeover patcher, recorded for its Restore.</summary>
	private static void Install(string keelFolder, Placed placed)
	{
		string manifest = Path.Combine(Paths.GameRootPath, ManifestName);
		placed.Manifest = manifest;
		placed.ManifestBefore = File.Exists(manifest) ? File.ReadAllLines(manifest) : null;
		List<string> lines = placed.ManifestBefore?.Where(l => l.Length > 0).ToList() ?? new List<string>();
		void Note(string entry)
		{
			if (!lines.Contains(entry)) { lines.Add(entry); }
			File.WriteAllLines(manifest, lines);     // after every step: an interrupted install can still be undone by KeelSetup
		}

		Directory.CreateDirectory(Paths.PluginPath);
		Place(Path.Combine(keelFolder, StubFolder, StubName), Path.Combine(Paths.PluginPath, StubName), @"BepInEx\plugins\", Note, placed);

		string patcherSource = Path.Combine(keelFolder, PatcherName);
		if (File.Exists(patcherSource)) {
			Directory.CreateDirectory(Paths.PatcherPluginPath);
			Place(patcherSource, Path.Combine(Paths.PatcherPluginPath, PatcherName), @"BepInEx\patchers\", Note, placed);
		}
		Log.LogInfo("Keel's stub and takeover patcher are in BepInEx (recorded in keel-setup.txt).");
	}

	// A file already there with the same bytes is left alone: BepInEx holds a loaded patcher open, and copying over it would fail.
	private static void Place(string source, string target, string folderName, Action<string> note, Placed placed)
	{
		if (File.Exists(target)) {
			if (File.ReadAllBytes(target).SequenceEqual(File.ReadAllBytes(source))) { return; }
			string backup = Path.Combine(Path.GetTempPath(), $"AnchorChain-{Guid.NewGuid():N}-{Path.GetFileName(target)}");
			File.Copy(target, backup);
			placed.Replaced.Add((target, backup));
			File.Copy(source, target, true);
			return;
		}
		note("created\t" + folderName + Path.GetFileName(target));
		placed.Created.Add(target);
		File.Copy(source, target);
	}

	/// <summary>Keel switched on in the game's mod list, at its top, and the list saved the way the Mods menu does it.</summary>
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void SwitchOn(string keelFolder)
	{
		FileManager files = Singleton<FileManager>.Instance;
		TrulyObservableCollection<SearchDirectory> list = files.Directories;
		SearchDirectory? keel = list.FirstOrDefault(d => Steam.IdOf(d) == KeelItem);
		bool added = keel == null;
		int wasAt = added ? -1 : list.IndexOf(keel!);
		bool wasOn = keel?.IsChecked ?? false;
		// The top: the game wants an item above every mod that requires it (its Mods menu marks the others red), and Keel holds no
		// game data, so being first changes nothing else. Only members every build since 0.8.7 has.
		if (added) {
			keel = (SearchDirectory)Activator.CreateInstance(typeof(SearchDirectory),
				new DirectoryInfo(keelFolder), true, true, Steam.Item())!;     // isActive, isEditable, Keel's Workshop id
			list.Insert(0, keel);
		}
		else if (wasAt > 0) {
			list.Move(wasAt, 0);
		}
		keel!.IsChecked = true;
		try {
			files.SaveDirectories(list);
			// The game writes this same list back when it closes, so what is saved now stays. Checked on disk all the same.
			if (!SavedOn()) { throw new InvalidOperationException("the game's mod list did not keep Keel"); }
		}
		catch (Exception) {
			// Put the game's own list back too: it is written again at exit, and a Keel left on there would load without its stub.
			keel.IsChecked = wasOn;
			if (added) { list.Remove(keel); }
			else if (wasAt > 0) { list.Move(0, wasAt); }
			files.SaveDirectories(list);
			throw;
		}
		Log.LogInfo("Keel is switched on at the top of the game's mod list.");
	}

	/// <summary>Does usersettings.ini list Keel, switched on?</summary>
	private static bool SavedOn()
	{
		string settings = Path.Combine(Application.persistentDataPath, "usersettings.ini");
		return File.Exists(settings) && File.ReadAllLines(settings).Any(l =>
			l.TrimStart().StartsWith("Mod", StringComparison.Ordinal) && l.Contains(",True," + KeelItem));
	}

	// Keel's watcher waits for the game to close, then starts it again. The game only quits once the watcher is seen running.
	private IEnumerator Restart(string keelFolder)
	{
		string watcher = Path.Combine(keelFolder, WatcherName);
		Process? started = null;
		string? failure = null;
		try {
			if (!File.Exists(watcher)) { throw new FileNotFoundException($"{WatcherName} is not in Keel's folder"); }
			Process self = Process.GetCurrentProcess();
			string exe = self.MainModule?.FileName ?? "";
			started = Process.Start(new ProcessStartInfo(watcher, $"--relaunch {self.Id} \"{exe}\"") { UseShellExecute = false, CreateNoWindow = true });
			if (started == null) { throw new InvalidOperationException($"{WatcherName} did not start"); }
		}
		catch (Exception e) {
			failure = e.Message;
		}
		if (failure == null) {
			yield return new WaitForSecondsRealtime(1f);
			try {
				if (started!.HasExited) { failure = $"{WatcherName} stopped at once"; }
			}
			catch (Exception e) {
				failure = e.Message;
			}
		}
		started?.Dispose();
		if (failure != null) {
			Log.LogWarning($"Could not restart the game by itself: {failure}");
			Show("The game could not restart by itself.\n\n• Close it and start it again: Keel takes over then", "OK", () => { });
			yield break;
		}
		Log.LogInfo("Restarting the game for Keel.");
		Application.Quit();
	}

	// Steamworks.NET is not among Anchor Chain's build references (Dll\ and the references release), so Steam is reached by name:
	// the game loads com.rlabrecque.steamworks.net before any mod, and these names are the same on every build since 0.8.7.
	private static class Steam
	{
		internal const uint Subscribed = 1, Installed = 4, NeedsUpdate = 8, Downloading = 16, DownloadPending = 32;  // EItemState

		private static readonly System.Reflection.Assembly Lib =
			AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "com.rlabrecque.steamworks.net");

		private static Type Type(string name) => Lib.GetType("Steamworks." + name, true)!;

		/// <summary>Keel's Workshop id as a PublishedFileId_t.</summary>
		internal static object Item() => Activator.CreateInstance(Type("PublishedFileId_t"), KeelItem)!;

		/// <summary>A SteamUGC call; out parameters come back in <paramref name="args"/>.</summary>
		internal static object? Call(string method, params object?[] args) =>
			Type("SteamUGC").GetMethods().First(m => m.Name == method && m.GetParameters().Length == args.Length).Invoke(null, args);

		internal static uint ItemState() => (uint)Call("GetItemState", Item())!;

		internal static void Download() => Call("DownloadItem", Item(), true);

		internal static string? InstallFolder()
		{
			object?[] args = { Item(), 0UL, null, 1024u, 0u };
			return Call("GetItemInstallInfo", args) is true ? args[2] as string : null;
		}

		/// <summary>The Workshop id of a folder in the game's mod list (0 for a local one).</summary>
		internal static ulong IdOf(SearchDirectory folder)
		{
			object id = typeof(SearchDirectory).GetField("steamId")!.GetValue(folder)!;
			return (ulong)id.GetType().GetField("m_PublishedFileId")!.GetValue(id)!;
		}
	}
}
