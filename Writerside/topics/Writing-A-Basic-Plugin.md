# Writing A Basic Plugin

## Before You Begin

This guide assumes that you have a fundamental grasp on the C# language and the basic principles of the Unity Game Engine.
If you don't understand the principles of Classes, Namespaces, Interfaces, Inheritance, and Dependencies, then we recommend you learn more C# before beginning (or at least prepare yourself for a challenge).
Additionally, if you don't know what a MonoBehavior or the Unity Debug class is, then we recommend you watch a basic Unity tutorial before returning to this guide.

The plugin which we will guide you to create in this guide is not a particularly practical one, but it will teach you how to interact with AnchorChain.
We won't teach you how to build a mod, just load one via AnchorChain.

## What AnchorChain Needs
As of writing, there is no nuget package for an AnchorChain plugin, so you'll have to do things the old-fashioned way.
Thankfully, AC plugins are fairly simple.
There are two things that define an AnchorChain plugin.
<tabs>

<tab title="The ACPlugin Attribute">
The <code>ACPlugin</code> attribute is how AnchorChain will find and identify your plugin.
It should be applied to any classes that you want to be loaded as a plugin.

<code-block lang="C#">
public class ACPlugin([NotNull] string guid, string name, string version, string[] before, string[] after)
</code-block> 

<deflist collapsible="true">
<def title="GUID">
<code>GUID</code> <b>must never be null</b>. This will cause AnchorChain to not load your plugin. 
We recommend that you use <a href="https://en.wikipedia.org/wiki/Reverse_domain_name_notation">reverse domain notation</a> for <code>GUID</code>. 
For modders, this usually comes in the form of our <a href="https://tomcam.github.io/least-github-pages/github-pages-url.html">github.io url</a>. 
For example, AnchorChain's <code>GUID</code> is <code>io.github.seapower_modders.anchorchain</code>.  
</def>

<def title="Name">
<code>Name</code> is the name of your plugin and is completely arbitrary. 
AnchorChain will only use it in logging and error messages. 
</def>

<def title="Version">
<code>Version</code> must be in a format acceptable by <code lang="C#">System.Version</code>. 
AnchorChain recommends that you adhere to the convention of <a href="https://semver.org/">SemVer</a>. 
</def>

<def title="Before">
<code>Before</code> is an array of plugins that you want your plugin to always load before.
Do not use this option without reason, as if AnchorChain detects a conflict between two mods both wanting to load first, it will abort loading all mods.

The plugins in <code>before</code> should be identified by their GUIDs.
</def>

<def title="After">
<code>After</code> is an array of plugins that you want your plugin to always load after.
Do not use this option without reason, as if AnchorChain detects a conflict between two mods both wanting to load second, it will abort loading all mods.

The plugins in <code>after</code> should be identified by their GUIDs.
</def>

</deflist>
</tab>

<tab title="The IAnchorChainMod Interface">
<code>IAnchorChainMod</code> is a simple interface, only consisting of a single method.

<code-block lang="C#">
public interface IAnchorChainMod { public void TriggerEntryPoint(); }
</code-block> 

<deflist collapsible="true">
<def title="TriggerEntryPoint()">
This is what AnchorChain will call to load your mod. As such, it should contain all setup required for your mod.
</def>
</deflist>
</tab>

</tabs>

## Writing Your Plugin File