# AnchorChain

### [Read the user guide and documentation here.](https://seapower-modders.github.io/AnchorChain/)

## Build and release automation

The **Build and release** workflow builds both DLLs on pushes to `main` and when run manually on `main`. Each build uploads `ACPreloader.zip` with the Windows x64 BepInEx installer and `dev.zip` with both AnchorChain DLLs. Pushing a tag such as `V1.2.0` publishes those same ZIPs to GitHub Releases. The tag must match the project version and point to a commit on `main`.

CI downloads the pinned [Sea Power reference bundle](https://github.com/SeaPower-Modders/AnchorChain-references/releases/tag/0.9.0-261111) and checks its SHA-256 before building. No setup is required.

# Included Software

This program's release files contain the umodified program BepInEx, licensed under LGPL v2.1. The source code is available here: https://github.com/BepInEx/BepInEx.
