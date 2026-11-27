# AnchorChain

### [Read the user guide and documentation here.](https://seapower-modders.github.io/AnchorChain/)

## Unreleased

Anchor Chain 1.3.1 preserves existing KeelSetup restore records if the switch to Keel fails. Windows CI checks rollback with a locked manifest, an existing manifest, and a fresh install.

## Build and release automation

The **Build and release** workflow compiles both DLLs on every branch push and pull request. Runs on `main` upload `ACPreloader.zip` and `dev.zip`. To publish both ZIPs to GitHub Releases, push a tag such as `V1.2.0` that matches the project version on `main`.

# Included Software

This program's release files contain the umodified program BepInEx, licensed under LGPL v2.1. The source code is available here: https://github.com/BepInEx/BepInEx.
