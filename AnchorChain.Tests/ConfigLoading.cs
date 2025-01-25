// Uncomment following line to activate CLO tests
#define Config
#if Config

using UnityEngine;


namespace AnchorChain.Tests;

[ACPlugin("io.github.seapower-modders.AnchorChainConfigInfo", "Config Info", "1.0")]
public class ConfigInfo : IAnchorChainMod
{
	public void TriggerEntryPoint()
	{
		Debug.LogWarning("Config info 01, 02, and 03 should load.");
	}
}


[ACPlugin("io.github.seapower-modders.AnchorChainConfig01", "Config 01", "1.0")]
[ACConfig]
public class Config01 : IAnchorChainMod
{
	public void TriggerEntryPoint()
	{
		Debug.LogWarning("Config info 01 loaded.");
	}
}


[ACPlugin("io.github.seapower-modders.AnchorChainConfig02", "Config 02", "1.0")]
[ACConfig]
public class Config02 : IAnchorChainMod
{
	public void TriggerEntryPoint()
	{
		Debug.LogWarning("Config info 02 loaded.");
	}
}


[ACPlugin("io.github.seapower-modders.AnchorChainConfig03", "Config 03", "1.0")]
[ACConfig(required: true)]
public class Config03 : IAnchorChainMod
{
	public void TriggerEntryPoint()
	{
		Debug.LogWarning("Config info 03 loaded.");
	}
}


[ACPlugin("io.github.seapower-modders.AnchorChainConfigI04", "Config 04", "1.0")]
[ACConfig(required: true)]
public class Config04 : IAnchorChainMod
{
	public void TriggerEntryPoint()
	{
		Debug.LogWarning("Config info 04 loaded.");
	}
}

#endif