<link-summary>How to build relationships between AnchorChain plugins</link-summary>
<show-structure for="chapter,procedure" depth="2"/>

# Integrating Multiple Plugins

## Introduction {id="Introduction"}
<link-summary>Integrating Multiple Plugins</link-summary>
AnchorChain provides a few methods to ensure that mods can both be dependent on each other and modify each other.  

All dependencies in AnchorChain are hard dependencies, meaning that even an implication that another mod should exist will be taken to mean that it must.
To make this as clear as possible, anything implying a dependency will be in the 
<a href="Integrating-Multiple-Plugins.md#Implicit_Dependencies">implicit dependencies</a> section.

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
To add <code>other-plugin</code> as a versionless dependency to the NewPlugin class we created in
<a href="Writing-A-Basic-Plugin.md"/>, we can add the line 
<code>[ACDependency("&lt;GUID of plugin&gt;")]</code> as seen below.
<br/>
<code-block lang="C#">
using AnchorChain;
using UnityEngine;

namespace NewPlugin;

<![CDATA[[ACPlugin("io.github.your-url.new-plugin", "New Plugin", "0.1.0")]]]>
<![CDATA[[ACDependency("io.github.other-creator.other-plugin")]]]>
public class NewPlugin : IAnchorChainMod {
    public void TriggerEntryPoint() {
        Debug.Log("Hello Anchor Chain!");
    }
}
</code-block>
</tab>
</tabs>

## Implicit Dependencies {id="Implicit_Dependencies"}
<link-summary>Implicit Dependency Documentation</link-summary>

<note>
This section is meant to document where dependencies are implied in writing code to other ends.
The methods in this section should not be used alone to create dependencies, as they are not explicit.
</note>

### ACPlugin Ordering Dependencies {id="Implicit_Ordering_Dependencies"}
<link-summary>ACPlugin Ordering Dependency Documentation</link-summary>
<a href="Writing-A-Basic-Plugin.md#ACPlugin"><code>ACPlugin</code></a> has two arguments (<code>before</code> and <code>after</code>) which create implicit dependencies.
As mentioned in the <a href="Integrating-Multiple-Plugins.md#Introduction">introduction</a>, all dependencies in AnchorChain are hard dependencies, so your plugin will not load without these implicit dependencies.
These tags are discussed at more length in the <a href="Integrating-Multiple-Plugins.md#Ordering_Dependency_Loading">ordering dependencies</a> section.

## Incompatibilities

### ACIncompatibility

<tabs>
<tab title="The ACIncompatibility Attribute">
The ACIncompatibility
<tooltip term="Attribute">attribute</tooltip> adds explicit incompatibilities to a plugin.
It does not allow for version-specific incompatibility.

<warning><code>guid</code> must never be null. This will cause AnchorChain to fail loading your plugin.</warning>
<br/>
<code-block lang="C#">
public class ACIncompatibility([NotNull] string guid)
</code-block>

<procedure title="Arguments" collapsible="true" type="choices">
<step>
<code>string guid</code>  
The GUID of the incompatibility as specified in its <code>ACPlugin</code> attribute.
</step>
</procedure>
</tab>

<tab title="Example">
To make our example plugin incompatible with the <code>other-plugin</code> plugin, we can add the line
<code>[ACIncompatibility("io.github.other-creator.other-plugin")]</code> as shown below.
<br/>

<code-block lang="C#">
using AnchorChain;
using UnityEngine;

namespace NewPlugin;

<![CDATA[[ACPlugin("io.github.your-url.new-plugin", "New Plugin", "0.1.0")]]]>
<![CDATA[[ACIncompatibility("io.github.other-creator.other-plugin")]]]>
public class NewPlugin : IAnchorChainMod {
    public void TriggerEntryPoint() {
        Debug.Log("Hello Anchor Chain!");
    }
}
</code-block>
</tab>
</tabs>

### ACPlugin Ordering Incompatibilities

<warning>
This section is meant to discuss a pitfall that is a consequence of the plugin ordering system.
Intentionally using this method is highly discouraged, as it leaves little user feedback, is not explicit, and will cause other plugins to fail to load.
</warning>

When plugin load order is resolved, AnchorChain screens for any "ordering loops" formed by two plugins that both want to load before or after the other.
If AnchorChain detects an ordering loop, it will not load either plugin, as the ordering is understood as a hard dependency as discussed 
<a href="Integrating-Multiple-Plugins.md#Implicit_Ordering_Dependencies">here</a>.

## Ordering Dependency Loading {id="Ordering_Dependency_Loading"}
<link-summary>Ordering Dependency Loading Documentation</link-summary>

### ACPlugin Ordering

