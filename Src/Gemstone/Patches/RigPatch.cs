using HarmonyLib;
using UnityEngine;

namespace Gemstone.patches;

[HarmonyPatch(typeof(VRRig), "OnDisable")]
internal class GhostPatch : MonoBehaviour
{
    public static bool Prefix(VRRig __instance) => __instance != VRRig.LocalRig;
}

[HarmonyPatch(typeof(VRRigJobManager), "DeregisterVRRig")]
public static class Bullshit
{
    public static bool Prefix(VRRigJobManager __instance) => !(__instance == VRRig.LocalRig);
}

[HarmonyPatch(typeof(VRRig), "PostTick")]
public static class Bullshit2
{
    public static bool Prefix(VRRig __instance) => !__instance.isLocal || __instance.enabled;
}