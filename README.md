# AnchorChain

### [Read the user guide and documentation here.](https://seapower-modders.github.io/AnchorChain/)

## Build and release automation

The **Build and release** workflow builds both DLLs on pushes to `main` and when run manually on `main`. Each build uploads `ACPreloader.zip` with the Windows x64 BepInEx installer and `dev.zip` with both AnchorChain DLLs. Pushing a tag such as `V1.2.0` publishes those same ZIPs to GitHub Releases. The tag must match the project version and point to a commit on `main`.

For CI setup, create the private repository `SeaPower-Modders/AnchorChain-references` and publish a release tagged `0.9.0-261111` with a `seapower-references.zip` asset. Put these DLLs from that Sea Power build's `Sea Power_Data/Managed` folder at the ZIP root:

- `Seapower-Scripts.dll`
- `Noesis.NoesisGUI.dll`
- `UnityEngine.dll`
- `UnityEngine.CoreModule.dll`
- `UnityEngine.ParticleSystemModule.dll`

In AnchorChain, create the `build-references` Actions environment for `main` and version tags. Add one secret, `ANCHORCHAIN_REFERENCES_TOKEN`, with Contents read access to the private repository. The workflow derives the repository name by appending `-references` to the current repository and pins the Sea Power release tag in [the workflow](.github/workflows/build-release.yml).

# Included Software

This program's release files contain the umodified program BepInEx, licensed under LGPL v2.1. The source code is available here: https://github.com/BepInEx/BepInEx.
