namespace AnchorChain;

/// <summary>Records new files for KeelSetup and restores this install's changes if enabling Keel fails.</summary>
internal class KeelInstall
{
	private readonly Stack<Action> undo = new();
	private readonly string manifest;
	private readonly byte[]? originalManifest;
	private readonly List<string> entries = [];
	private bool wroteManifest;

	private KeelInstall(string manifest)
	{
		this.manifest = manifest;
		try {
			originalManifest = File.ReadAllBytes(manifest);
		}
		catch (FileNotFoundException) {
			return;
		}
		using var reader = new StreamReader(new MemoryStream(originalManifest));
		while (reader.ReadLine() is { } line) { entries.Add(line); }
	}

	internal static void Apply(string manifest, Action<KeelInstall> install, Action<Exception> reportRollbackError)
	{
		var files = new KeelInstall(manifest);
		try {
			install(files);
		}
		catch {
			foreach (Action restore in files.undo) {
				try { restore(); }
				catch (Exception error) { reportRollbackError(error); }
			}
			throw;
		}
	}

	internal void Place(string source, string directory, string manifestDirectory)
	{
		string name = Path.GetFileName(source);
		string target = Path.Combine(directory, name);
		byte[] contents = File.ReadAllBytes(source);
		Directory.CreateDirectory(directory);
		byte[]? previous = File.Exists(target) ? File.ReadAllBytes(target) : null;
		// BepInEx may hold an identical patcher open. It needs no write or rollback.
		if (previous != null && previous.SequenceEqual(contents)) { return; }
		if (previous == null) { RecordCreated(manifestDirectory + @"\" + name); }

		var mode = previous == null ? FileMode.CreateNew : FileMode.Open;
		using var output = File.Open(target, mode, FileAccess.Write, FileShare.Read);
		if (previous == null) { undo.Push(() => File.Delete(target)); }
		else { undo.Push(() => File.WriteAllBytes(target, previous)); }
		output.SetLength(0);
		output.Write(contents, 0, contents.Length);
	}

	private void RecordCreated(string relativePath)
	{
		string entry = "created\t" + relativePath;
		if (entries.Contains(entry)) { return; }
		entries.Add(entry);

		var mode = originalManifest == null && !wroteManifest ? FileMode.CreateNew : FileMode.Open;
		using var stream = File.Open(manifest, mode, FileAccess.Write, FileShare.Read);
		if (!wroteManifest) {
			undo.Push(RestoreManifest);
			wroteManifest = true;
		}
		stream.SetLength(0);
		using var writer = new StreamWriter(stream);
		entries.ForEach(writer.WriteLine);
	}

	private void RestoreManifest()
	{
		if (originalManifest == null) { File.Delete(manifest); }
		else { File.WriteAllBytes(manifest, originalManifest); }
	}
}
