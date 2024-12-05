// Uncomment following line to activate SD tests
#define LO
#if LO

using UnityEngine;


namespace AnchorChain.Tests;

/// <summary>
/// Info plugin for test group
/// </summary>
[ACPlugin("io.github.seapower-modders.AnchorChainLOInfo", "LO Info", "1.0")]
public class LoadOrderInfo: IModInterface
{
	public void TriggerEntryPoint()
	{
		Debug.LogWarning("LO plugins should based on the following tree:\nLO 1\tLO 2\tLO 3\n |\t\t |       |\n |<<<<<<<<<<<<<<<|\n |\t\t |\t\t |\nLO 4\tLO 5\tLO 6\n |\t\t |\t\t |\n |<<<<<<<|       |\n |\t\t |       |\nLO 7\tLO 8\tLO 9");
	}
}

[ACPlugin("io.github.seapower-modders.AnchorChainLO1", "LO 1", "1.0", ["io.github.seapower-modders.AnchorChainLO4"])]
public class LoadOrder1: IModInterface
{
	public void TriggerEntryPoint()
	{
		Debug.Log("LO 1 Loaded");
	}
}

[ACPlugin("io.github.seapower-modders.AnchorChainLO2", "LO 2", "1.0", ["io.github.seapower-modders.AnchorChainLO4", "io.github.seapower-modders.AnchorChainLO5"])]
public class LoadOrder2: IModInterface
{
	public void TriggerEntryPoint()
	{
		Debug.Log("LO 2 Loaded");
	}
}

[ACPlugin("io.github.seapower-modders.AnchorChainLO3", "LO 3", "1.0")]
public class LoadOrder3: IModInterface
{
	public void TriggerEntryPoint()
	{
		Debug.Log("LO 3 Loaded");
	}
}

[ACPlugin("io.github.seapower-modders.AnchorChainLO4", "LO 4", "1.0", ["io.github.seapower-modders.AnchorChainLO7"], ["io.github.seapower-modders.AnchorChainLO3"])]
public class LoadOrder4: IModInterface
{
	public void TriggerEntryPoint()
	{
		Debug.Log("LO 4 Loaded");
	}
}

[ACPlugin("io.github.seapower-modders.AnchorChainLO5", "LO 5", "1.0", ["io.github.seapower-modders.AnchorChainLO7", "io.github.seapower-modders.AnchorChainLO8"], ["io.github.seapower-modders.AnchorChainLO2"])]
public class LoadOrder5: IModInterface
{
	public void TriggerEntryPoint()
	{
		Debug.Log("LO 5 Loaded");
	}
}

[ACPlugin("io.github.seapower-modders.AnchorChainLO6", "LO 6", "1.0", ["io.github.seapower-modders.AnchorChainLO9"], ["io.github.seapower-modders.AnchorChainLO3"])]
public class LoadOrder6: IModInterface
{
	public void TriggerEntryPoint()
	{
		Debug.Log("LO 6 Loaded");
	}
}

[ACPlugin("io.github.seapower-modders.AnchorChainLO7", "LO 7", "1.0")]
public class LoadOrder7: IModInterface
{
	public void TriggerEntryPoint()
	{
		Debug.Log("LO 1 Loaded");
	}
}

[ACPlugin("io.github.seapower-modders.AnchorChainLO8", "LO 8", "1.0")]
public class LoadOrder8: IModInterface
{
	public void TriggerEntryPoint()
	{
		Debug.Log("LO 1 Loaded");
	}
}

[ACPlugin("io.github.seapower-modders.AnchorChainLO9", "LO 9", "1.0", [], ["io.github.seapower-modders.AnchorChainLO6"])]
public class LoadOrder9: IModInterface
{
	public void TriggerEntryPoint()
	{
		Debug.Log("LO 1 Loaded");
	}
}

#endif