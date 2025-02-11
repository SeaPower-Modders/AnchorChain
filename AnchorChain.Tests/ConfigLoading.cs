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
		Debug.LogWarning("Config info 01, 03, and 04 should load.");
	}
}


// Test required config existing
[ACPlugin("io.github.seapower-modders.AnchorChainConfig01", "Config 01", "1.0")]
[ACConfig]
public class Config01 : IAnchorChainMod
{
	public void TriggerEntryPoint()
	{
		Debug.LogWarning("Config info 01 loaded.");
	}
}


// Test required config not existing
[ACPlugin("io.github.seapower-modders.AnchorChainConfig02", "Config 02", "1.0")]
[ACConfig]
public class Config02 : IAnchorChainMod
{
	public void TriggerEntryPoint()
	{
		Debug.LogWarning("Config info 02 loaded.");
	}
}


// Test non-required config existing
[ACPlugin("io.github.seapower-modders.AnchorChainConfig03", "Config 03", "1.0")]
[ACConfig(required: false)]
public class Config03 : IAnchorChainMod
{
	public void TriggerEntryPoint()
	{
		Debug.LogWarning("Config info 03 loaded.");
	}
}


// Test non-required config not existing
[ACPlugin("io.github.seapower-modders.AnchorChainConfig04", "Config 04", "1.0")]
[ACConfig(required: false)]
public class Config04 : IAnchorChainMod
{
	public void TriggerEntryPoint()
	{
		Debug.LogWarning("Config info 04 loaded.");
	}
}


// Test config missing section
[ACPlugin("io.github.seapower-modders.AnchorChainConfig05", "Config 05", "1.0")]
[ACConfig()]
public class Config05 : IAnchorChainMod
{
	public void TriggerEntryPoint()
	{
		Debug.LogWarning("Config info 05 loaded.");
	}
}


// Test config missing key
[ACPlugin("io.github.seapower-modders.AnchorChainConfig06", "Config 06", "1.0")]
[ACConfig()]
public class Config06 : IAnchorChainMod
{
	public void TriggerEntryPoint()
	{
		Debug.LogWarning("Config info 06 loaded.");
	}
}


// Test resetting value
[ACPlugin("io.github.seapower-modders.AnchorChainConfig07", "Config 07", "1.0")]
[ACConfig()]
public class Config07 : IAnchorChainMod
{
	public void TriggerEntryPoint()
	{
		Debug.LogWarning("Config info 07 loaded.");
	}
}


#endif