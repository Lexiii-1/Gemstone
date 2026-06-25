using HarmonyLib;

namespace Gemstone.patches
{
    [HarmonyPatch(typeof(GorillaQuitBox), "OnBoxTriggered")]
    internal class QuitBoxPatch
    {
        public static bool enabled = false;

        public static bool Prefix() => enabled;
    }
}