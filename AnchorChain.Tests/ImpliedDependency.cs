// Uncomment following line to activate SD tests
// #define ID
#if ID

using UnityEngine;


namespace AnchorChain.Tests;

/// <summary>
/// Info plugin for test group
/// </summary>
[ACPlugin("io.github.seapower-modders.AnchorChainIDInfo", "ID Info", "1.0")]
public class ImpliedDependencyInfo : IAnchorChainMod
{
	public void TriggerEntryPoint()
	{
		Debug.LogWarning("ID 2, ID 1, and ID 3 should load in that order");
	}
}

/// <summary>
/// THE implied dependency
/// </summary>
[ACPlugin("io.github.seapower-modders.AnchorChainID1", "ID 1", "1.0")]
public class ImpliedDependency1 : IAnchorChainMod
{
	public void TriggerEntryPoint()
	{
		Debug.Log("ID 1 Loaded");
	}
}

/// <summary>
/// Test preload plugin
/// </summary>
[ACPlugin("io.github.seapower-modders.AnchorChainID2", "ID 2", "1.0", ["io.github.seapower-modders.AnchorChainID1"])]
public class ImpliedDependency2 : IAnchorChainMod
{
	public void TriggerEntryPoint()
	{
		Debug.Log("ID 2 Loaded");
	}
}

/// <summary>
/// Test postload plugin
/// </summary>
[ACPlugin("io.github.seapower-modders.AnchorChainID3", "ID 3", "1.0", [], ["io.github.seapower-modders.AnchorChainID1"])]
public class ImpliedDependency3 : IAnchorChainMod
{
	public void TriggerEntryPoint()
	{
		Debug.Log("ID 3 Loaded");
	}
}

/// <summary>
/// Test missing preload plugin
/// </summary>
[ACPlugin("io.github.seapower-modders.AnchorChainID4", "ID 4", "1.0", ["io.github.seapower-modders.does-not-exist"])]
public class ImpliedDependency4 : IAnchorChainMod
{
	public void TriggerEntryPoint()
	{
		Debug.LogError("ID 4 Loaded");
	}
}

/// <summary>
/// Test missing postload plugin
/// </summary>
[ACPlugin("io.github.seapower-modders.AnchorChainID5", "ID 5", "1.0", [], ["io.github.seapower-modders.does-not-exist"])]
public class ImpliedDependency5 : IAnchorChainMod
{
	public void TriggerEntryPoint()
	{
		Debug.LogError("ID 5 Loaded");
	}
}

#endif