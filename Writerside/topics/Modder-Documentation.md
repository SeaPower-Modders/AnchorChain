<link-summary>The documentation hub for Anchor Chain mods</link-summary>
<show-structure for="chapter, procedure" depth="2"/>
# Modder Documentation

## What *Really* is Anchor Chain?

Anchor Chain is the Sea Power community's oldest (and only) 
<tooltip term="Chainloader">chainloader</tooltip>.
While BepInEx can load plugins from its `Sea Power/BepInEx/plugins` folder, it is very inflexible about extending this capability to other folders.
Since Sea Power mods are saved within the `Sea Power/Sea Power_Data/StreamingAssets` folder or various steam workshop directories, this poses a problem for distributing mods to users.
Namely, most users won't feel comfortable or want to bother digging within the game's folder.
This is where Anchor Chain comes in.

Anchor Chain consists of two parts, the preloader and mod loader.
The preloader is a BepInEx plugin that loads any plugins (Steam Workshop or local) implementing the 
<code>IPluginLoader</code> interface.
The mod loader implements this interface, which then scans for and loads all plugins implementing the 
<code>IAnchorChainMod</code> interface.
This division of labor allows for Steam Workshop distribution and updates to the main body of AnchorChain while still maintaining its functionality.

Because players install the preloader by hand and rarely update it, it is kept as small as possible: it references no game types,
finds `AnchorChain.dll` directly on disk (the game's local mod folders and its Steam Workshop folder; only copies in mod folders
switched on in the Mod Manager, as saved in `usersettings.ini`, count, and the newest version wins) and hands over.
Everything that depends on the game, such as waiting until the game has listed its mod folders, lives in the mod loader,
so game updates are handled by a Workshop update of Anchor Chain alone.


<procedure title="Getting Started With Anchor Chain" type="choices" >
<step>
<a href="Install-Anchor-Chain.md">Installing Anchor Chain</a>
</step>
<step>
<a href="Writing-A-Basic-Plugin.md">Writing your first plugin</a>
</step>
<step>
<a href="Integrating-Multiple-Plugins.md">Integrating multiple plugins</a>
</step>
</procedure>
