<link-summary>The documentation hub for Anchor Chain mods</link-summary>
<show-structure for="chapter, procedure" depth="2"/>
# Modder Documentation

## What *Really* is Anchor Chain?

In short, Anchor Chain is the Sea Power community's newest (and only) 
<tooltip term="Chainloader">chainloader</tooltip>.
While BepInEx can load plugins from its `Sea Power/BepInEx/plugins` folder, it is very inflexible about extending this capability to other folders.
Since Sea Power mods are saved within the `Sea Power/Sea Power_Data/StreamingAssets` folder, this poses a problem for distributing mods to users.
Namely, most users won't feel comfortable or want to bother digging within the game's folder.
This is where Anchor Chain comes in.
Anchor Chain is a single BepInEx plugin that loads other mods into Sea Power, allowing for modders to distribute their mods via the Steam Workshop as long as the user has it installed.


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
