<link-summary>How to build relationships between AnchorChain plugins</link-summary>
<show-structure for="chapter,procedure" depth="2"/>

# Integrating Multiple Plugins

## Introduction

AnchorChain provides a few methods to ensure that mods can both be dependent on each other and modify each other.  

All dependencies in AnchorChain are hard dependencies, meaning that even an implication that another mod should exist will be taken to mean that it must.
To make this as clear as possible, anything implying a dependency will be in the 
<a href="Integrating-Multiple-Plugins.md#Implicit Dependencies"/> section.

## Explicit Dependencies

### ACDependency

<tabs>
<tab title="The ACDependency Attribute">

The ACDependency 
<tooltip term="Attribute">attribute</tooltip> is AnchorChain's way of adding explicit dependencies to a plugin.
It also allows for version-specific dependencies with SemVer.

<note>The Steam Workshop does not provide a method for version-specific installation, so versioned dependencies should only be used where absolutely necessary.</note>
<warning><code>guid</code> must never be null. This will cause AnchorChain to fail loading your plugin.</warning>
<br/>

<code-block lang="C#">
public class ACDependency([NotNull] string guid, string minVersion, string maxVersion)
</code-block>

<procedure title="Arguments" collapsible="true" type="choices">
<step>
<code>string guid</code>

The GUID of the dependency as specified in its `ACPlugin` attribute. 
</step>
<step>
<code>string minVersion</code>

The minimum version of the dependency, as specified in its `ACPlugin` attribute. 
If this is set to null, it will be assumed that there is no minimum version.
This must be in a format acceptable by `System.Version`.
</step>
<step>
<code>string maxVersion</code>

The maximum version of the dependency, as specified in its `ACPlugin` attribute.
If this is set to null, it will be assumed that there is no maximum version.
This must be in a format acceptable by `System.Version`.
</step>
</procedure>
</tab>

<tab title="Example">
To add a versionless dependency to the NewPlugin class we created in
<a href="Writing-A-Basic-Plugin.md"/>, we can add the line 
<code>[ACDependency("&lt;GUID of plugin&gt;")]</code> as seen below.
<br/>
<code-block lang="C#">
using AnchorChain;
using UnityEngine;

namespace NewPlugin;

[ACPlugin("io.github.your-url.new-plugin", "New Plugin", "0.1.0")]
[ACDependency("io.github.other-creator.other-plugin")]
public class NewPlugin : IAnchorChainMod {
    public void TriggerEntryPoint() {
        Debug.Log("Hello Anchor Chain!");
    }
}
</code-block>
</tab>
</tabs>

## Implicit Dependencies {id="Implicit Dependencies"}

## Incompatibilities

## Ordering Dependencies
