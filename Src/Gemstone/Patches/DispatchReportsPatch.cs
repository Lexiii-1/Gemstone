using System;
using HarmonyLib;

namespace Gemstone.patches
{
    [HarmonyPatch(typeof(MonkeAgent), "DispatchReport")]
    public class DispatchReportsPatch
    {
        private static bool Prefix()
        {
            return false;
        }
    }
}
