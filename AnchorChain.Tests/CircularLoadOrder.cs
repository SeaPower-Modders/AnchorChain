// Uncomment following line to activate CLO tests
///#define CLO
#if CLO

using UnityEngine;


namespace AnchorChain.Tests;


[ACPlugin("io.github.seapower-modders.AnchorChainCLInfo", "CLO Info", "1.0")]
public class CircularLoadOrderInfo : IModInterface
{
	public void TriggerEntryPoint()
	{
		Debug.LogWarning("No plugins should load (including this one)");
	}
}

[ACPlugin("io.github.seapower-modders.AnchorChainCLO1", "CLO 1", "1.0", ["io.github.seapower-modders.AnchorChainCLO2"])]
public class CircularLoadOrder1 : IModInterface
{
	public void TriggerEntryPoint()
	{
		Debug.LogError("CLO 1 Loaded");
	}
}

[ACPlugin("io.github.seapower-modders.AnchorChainCLO2", "CLO 2", "1.0", ["io.github.seapower-modders.AnchorChainCLO3"])]
public class CircularLoadOrder2 : IModInterface
{
	public void TriggerEntryPoint()
	{
		Debug.LogError("CLO 2 Loaded");
	}
}

[ACPlugin("io.github.seapower-modders.AnchorChainCLO3", "CLO 3", "1.0", ["io.github.seapower-modders.AnchorChainCLO1"])]
public class CircularLoadOrder3 : IModInterface
{
	public void TriggerEntryPoint()
	{
		Debug.LogError("CLO 3 Loaded");
	}
}

#endif