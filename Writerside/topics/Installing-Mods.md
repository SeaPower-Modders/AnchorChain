<link-summary>How to install Anchor Chain-based mods</link-summary>

# Installing Mods

## Selected DLL mods

Only checked mod directories contribute AnchorChain plugins. A locked checkbox does
not disable a checked directory. Nested support folders are scanned, but a separately
registered child mod follows its own checkbox and menu position.

AnchorChain also checks its own directory before initializing. This protects users
whose preloader still invokes unchecked copies of AnchorChain. The preloader is
not distributed through the Workshop, so AnchorChain must work with older installed
versions. These checks live entirely in AnchorChain.dll and require no preloader update.

Loader filenames may have a prefix, such as `TestAnchorChain.dll`. Discovery matches
the `AnchorChain.dll` suffix in the existing preloader. Preserve that capitalization
for compatibility with older installs. The DLL must still expose
`AnchorChain.AnchorChainLoader` implementing `IPluginLoader`.
Plugin discovery and reload checks treat any filename containing `AnchorChain`
and ending in `.dll` as a loader binary, also case-insensitively.

## Installing Steam Workshop Mods

AnchorChain was made to make installing mods from the Steam Workshop as simple as possible. 
Subscribe to the mod, enable its checkbox in Sea Power's mod menu, and apply the selection.

## Installing Manually-Installed Mods

Ideally, any manually-installed mod should provide its own guidance for installation.
If this is not the case, we can only provide general guidance on how to get Anchor Chain to load it.

Anchor Chain will look for mods in a few places, but the simplest place is `C:\Program Files (x86)\Steam\steamapps\common\Sea Power\Sea Power_Data\StreamingAssets`.
To get Anchor Chain to load a mod, all the mod's files must be **within a folder** in that directory.
