using HarmonyLib;

namespace Gemstone.patches
{
    [HarmonyPatch(typeof(VRRig), nameof(VRRig.PackCompetitiveData))]
    internal class FpsPatch
    {
        public static bool enabled = false;
        public static short fps = 0;

        public static bool Prefix(ref short __result)
        {
            if (!enabled)
                return true;
            __result = (short)fps;

            return false;
        }
    }
}