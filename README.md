# AnchorChain

### [Read the user guide and documentation here.](https://seapower-modders.github.io/AnchorChain/)

## Build and release automation

The **Build and release** workflow builds both DLLs on pushes to `main` and when run manually on `main`. Each build uploads `ACPreloader.zip` with the Windows x64 BepInEx installer and `dev.zip` with both AnchorChain DLLs. Pushing a tag such as `V1.2.0` publishes those same ZIPs to GitHub Releases. The tag must match the project version and point to a commit on `main`.

Before the first run, create a `build-references` Actions environment restricted to `main` and version tags. In a private repository, attach `seapower-references.zip` to a release. Put these DLLs from the same supported Sea Power installation's `Sea Power_Data/Managed` folder at the ZIP root:

- `Seapower-Scripts.dll`
- `Noesis.NoesisGUI.dll`
- `UnityEngine.dll`
- `UnityEngine.CoreModule.dll`
- `UnityEngine.ParticleSystemModule.dll`

Set the environment variables `ANCHORCHAIN_REFERENCES_REPOSITORY` to `owner/repo` and `ANCHORCHAIN_REFERENCES_TAG` to that release tag. Set the `ANCHORCHAIN_REFERENCES_TOKEN` secret to a token with read access to Contents in the private reference repository. The reference DLLs are used only for compilation. For a local build, copy those same files into `Dll/` and run `dotnet build AnchorChain/AnchorChain.csproj -c Release`.

# Included Software

This program's release files contain the umodified program BepInEx, licensed under LGPL v2.1. The source code is available here: https://github.com/BepInEx/BepInEx.
