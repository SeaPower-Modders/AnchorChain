#!/usr/bin/env dotnet
#:property PublishAot=false
#:include ../AnchorChain/KeelInstall.cs

using AnchorChain;

using var temp = new TempDirectory();
string manifest = Path.Combine(temp.Path, "keel-setup.txt");
string source = Path.Combine(temp.Path, "stub.dll");
string plugins = Path.Combine(temp.Path, "plugins");
string target = Path.Combine(plugins, "stub.dll");
byte[] original = System.Text.Encoding.Unicode.GetBytes("\uFEFFcreated\tprior.dll\r\n");
File.WriteAllBytes(manifest, original);
File.WriteAllText(source, "new stub");

void Apply(Action<KeelInstall> install) => KeelInstall.Apply(manifest, install, e => throw new Exception("Rollback failed", e));
void Place(KeelInstall files) => files.Place(source, plugins, @"BepInEx\plugins");
void FailAfterCopy(KeelInstall files)
{
	Place(files);
	throw new EnableFailedException();
}

if (OperatingSystem.IsWindows()) {
	// Deny reads but allow deletion, exactly the case where the old rollback lost the manifest.
	bool enteredInstall = false;
	using (File.Open(manifest, FileMode.Open, FileAccess.Write, FileShare.Delete)) {
		try { Apply(_ => enteredInstall = true); }
		catch (IOException) { }
	}
	Require(!enteredInstall && File.ReadAllBytes(manifest).SequenceEqual(original), "unreadable manifest");
}

ExpectInstallFailure();
Require(File.ReadAllBytes(manifest).SequenceEqual(original) && !File.Exists(target), "existing manifest rollback");

File.Delete(manifest);
ExpectInstallFailure();
Require(!File.Exists(manifest) && !File.Exists(target), "new manifest rollback");

File.WriteAllText(target, "old stub");
ExpectInstallFailure();
Require(File.ReadAllText(target) == "old stub" && !File.Exists(manifest), "replaced file rollback");

File.Delete(target);
Apply(Place);
Require(File.ReadAllText(target) == "new stub", "installed file");
Require(File.ReadAllLines(manifest).SequenceEqual(["created\t" + @"BepInEx\plugins\stub.dll"]), "restore record");
Console.WriteLine("Keel install checks passed.");

void ExpectInstallFailure()
{
	try { Apply(FailAfterCopy); }
	catch (EnableFailedException) { return; }
	throw new Exception("Expected the install to fail");
}

static void Require(bool passed, string check)
{
	if (!passed) { throw new Exception("Failed: " + check); }
}

class TempDirectory : IDisposable
{
	public string Path { get; } = Directory.CreateTempSubdirectory("AnchorChain-test-").FullName;
	public void Dispose() => Directory.Delete(Path, recursive: true);
}

class EnableFailedException : Exception;
