using HarmonyLib;
using UnityEngine;

namespace Gemstone.patches;

[HarmonyPatch(typeof(VRRig), "OnDisable")]
internal class GhostPatch : MonoBehaviour
{
    public static bool Prefix(VRRig __instance) => __instance != VRRig.LocalRig;
}

[HarmonyPatch(typeof(VRRigJobManager), "DeregisterVRRig")]
public static class DeregisterVRRig
{
    public static bool Prefix(VRRigJobManager __instance) => !(__instance == VRRig.LocalRig);
}

[HarmonyPatch(typeof(VRRig), "PostTick")]
public static class PostTick
{
    public static bool Prefix(VRRig __instance) => !__instance.isLocal || __instance.enabled;
}