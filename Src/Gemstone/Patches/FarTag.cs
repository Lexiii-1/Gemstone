using Gemstone.Gemstone;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace Gemstone.patches;

[HarmonyPatch(typeof(MonkeAgent), nameof(MonkeAgent.SendReport))]
public static class ExtremelyFarTagPatch
{
    private const           float                     PlayerReportLogCooldown = 1f;
    public static           bool                      isDetected;
    private static readonly Dictionary<string, float> LastLoggedReport = new();

    private static bool Prefix(string susReason, string susId, string susNick)
    {
        bool isMe = susId == PhotonNetwork.LocalPlayer.UserId;

        if (isMe)
        {
            if (susReason == "extremely far tag")
            {
                isDetected = true;
                NotiLib.SendNotification(
                        Localization.Get(
                                "<color=red>[ANTICHEAT]</color> You have been reported for extremely far tag, is it recommended you stop using the tag mods and restart your game."),
                        5000);
            }

            return false;
        }

        if (LastLoggedReport.ContainsKey(susId) && LastLoggedReport[susId] > Time.time)
            return true;

        LastLoggedReport[susId] = Time.time + PlayerReportLogCooldown;

        return true;
    }
}