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
