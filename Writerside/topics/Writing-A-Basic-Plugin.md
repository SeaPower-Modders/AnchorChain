<link-summary>How to write a basic Anchor Chain plugin</link-summary>

# Writing A Basic Plugin

<note>
This guide assumes that you have a fundamental grasp on the C# language and the basic principles of the Unity Game Engine.
If you don't understand the principles of Classes, Namespaces, Interfaces, and Inheritance, then we recommend you learn more C# before beginning (or at least prepare yourself for a challenge).
</note>

## Before You Begin

The plugin which we will guide you to create in this guide will only teach you how to interact with AnchorChain.
This guide isn't about building mods, just loading one via AnchorChain.
If you would like to learn how to build a mod, then we recommend looking to the 
<a href="https://harmony.pardeike.net/">Harmony documentation</a>.

## What Anchor Chain Requires
As of writing, there is no nuget package for an Anchor Chain plugin, so you'll have to do things the old-fashioned way.
Thankfully, AC plugins are fairly simple.
There are two things that define a plugin:
<tabs>

<tab title="The ACPlugin Attribute" id="ACPlugin">
The <code>ACPlugin</code> <tooltip term="Attribute">attribute</tooltip> is how AnchorChain will find and identify your plugin.
It should be applied to any classes that you want to be loaded as a plugin.
<warning><code>guid</code> must never be null. This will cause AnchorChain to fail loading your plugin.</warning>

<br></br>

<code-block lang="C#">
public class ACPlugin([NotNull] string guid, string name, string version, string[] before, string[] after)
</code-block>

<procedure title="Arguments" collapsible="true" type="choices">


<step>
<code>string guid</code>  

AnchorChain's internal name for your plugin.
We recommend that you use <a href="https://en.wikipedia.org/wiki/Reverse_domain_name_notation">reverse domain notation</a> for <code>GUID</code>.
For modders, this usually comes in the form of your <a href="https://tomcam.github.io/least-github-pages/github-pages-url.html">github.io url</a>.
For example, AnchorChain's <code>GUID</code> is <code>io.github.seapower_modders.anchorchain</code>.
</step>

<br></br>

<step>
<code>string name</code> 

The name of your plugin is completely arbitrary. 
AnchorChain will only use it in logging and error messages, and it will always be paired with the plugin's <code>guid</code>.
</step>

<br></br>

<step>
<code>string version</code> 

The version of your plugin.
Must be in a format acceptable by <code lang="C#">System.Version</code>. 
AnchorChain recommends that you adhere to the convention of <a href="https://semver.org/">SemVer</a>.
</step>

<br></br>

<step>
<code>string[] before</code> 

An array of plugins that you want your plugin to always load before.
Do not use this option without reason, as if AnchorChain detects a conflict between two mods both wanting to load first, it will abort loading all mods.

The plugins in <code>before</code> should be identified by their GUIDs.
</step>

<br></br>

<step>
<code>string[] after</code> 

An array of plugins that you want your plugin to always load after.
Do not use this option without reason, as if AnchorChain detects a conflict between two mods both wanting to load second, it will abort loading all mods.

The plugins in <code>after</code> should be identified by their GUIDs.
</step>

</procedure>
</tab>

<tab title="The IAnchorChainMod Interface" id="IACMod">
<code>IAnchorChainMod</code> is a simple interface, only consisting of a single method.
<br></br>
<code-block lang="C#">
public interface IAnchorChainMod { public void TriggerEntryPoint(); }
</code-block> 

<procedure title="TriggerEntryPoint()" collapsible="true">
This is what AnchorChain will call to load your plugin. As such, it should contain all setup required for your plugin.
</procedure>
</tab>

</tabs>

## Writing Your Plugin

<procedure>
<step>
Create a new project in your preferred code editor.
</step>
<step>
Import AnchorChain to your project. 
The dll that you should use is available on our <a href="https://github.com/SeaPower-Modders/AnchorChain/releases" nullable="true">releases page</a> in <code>dev.zip</code>.
</step>
<step>
Create your plugin's namespace. We recommend choosing a unique name to avoid collisions with other mods.
</step>

<br></br>
Your file should be similar to the following:

<code-block lang="C#">
using AnchorChain;

namespace NewPlugin;
</code-block>

<step>
Create a class for your plugin. It should be annotated with <a href="Writing-A-Basic-Plugin.md#ACPlugin"><code>ACPlugin</code></a> 
and implement <a href="Writing-A-Basic-Plugin.md#IACMod"><code>IAnchorChainMod</code></a>.
</step>
<step>
Add your <code>guid</code>, <code>name</code>, and <code>version</code> string to <code>ACPlugin</code>.
</step>
<step>
Create the <code>public void TriggerEntryPoint</code> function.
</step>

<br></br>
Now, your code should look something like this:

<code-block lang="C#">
using AnchorChain;

namespace NewPlugin;

[ACPlugin("io.github.your-url.new-plugin", "New Plugin", "0.1.0")]
public class NewPlugin : IAnchorChainMod {
    public void TriggerEntryPoint() { 
        
    }
}
</code-block>

<step>
All that's left is to add your mod's code to <code>TriggerEntryPoint()</code>. 
For this example, we'll just make it log a message to console using Unity's <code>Debug.Log()</code>.
</step>

Finally, we have our completed plugin.

<code-block lang="C#">
using AnchorChain;
using UnityEngine;

namespace NewPlugin;

[ACPlugin("io.github.your-url.new-plugin", "New Plugin", "0.1.0")]
public class NewPlugin : IAnchorChainMod {
    public void TriggerEntryPoint() {
        Debug.Log("Hello Anchor Chain!");
    }
}
</code-block>
</procedure>

## Load Your Plugin

First, make sure that the Anchor Chain and its preloader are installed in your Sea Power directory.
If that is not the case, follow the instructions 
<a href="Install-Anchor-Chain.md">here</a>.

Anchor Chain is made to make loading plugins easier for users.
Unfortunately, that does not translate into an easier time for modders.
You will need to build your plugin and manually move it into <code><![CDATA[<Sea Power>]]>/Sea Power_Data/StreamingAssets</code>

<warning>
Do not set your IDE's build output directory to this folder. 
IDEs generally assume that they have complete authority over anything in these folders, and will overwrite and delete anything there as such.
They may also output dependencies into output folders, causing Anchor Chain to try (and fail) to load itself.
</warning>

Once your plugin is in <code>StreamingAssets</code>, just boot the game as normal. 
Your plugin will load and log your debug message to <code><![CDATA[<your user directory>]]>/AppData/LocalLow/Triassic Games/Sea Power/Player.log</code>.

Congratulations, you've made and loaded an Anchor Chain plugin!