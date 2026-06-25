using Gemstone.patches;
using GorillaLocomotion;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.XR.OpenXR.Features;

namespace Gemstone.Gemstone
{
    public class RunMods : MonoBehaviour
    {
        void Update()
        {
            if (ModConfig.instance.SpeedBoostEnabled.Value) Mods.Mods.SpeedBoost();
            if (ModConfig.instance.FlyEnabled.Value) Mods.Mods.Fly();
            if (ModConfig.instance.LongArmsEnabled.Value) Mods.Mods.LongArms();
            if (ModConfig.instance.IsPlatformsEnabled.Value) Mods.Mods.Platforms();
            if (ModConfig.instance.IsMuteGun.Value) Mods.Mods.MuteGun();
            if (ModConfig.instance.IsReportGun.Value) Mods.Mods.ReportGun();
            if (ModConfig.instance.IsGhostMonke.Value) Mods.Mods.GhostMonke();
            if (ModConfig.instance.IsBackwardsHead.Value) Mods.Mods.BackwardsHead();
            if (ModConfig.instance.IsUpsideDownHead.Value) Mods.Mods.UpsideDownNeck();
            if (ModConfig.instance.IsNoclipEnabled.Value) Mods.Mods.Noclip();
            if (ModConfig.instance.IsJoystickFly.Value) Mods.Mods.JoystickFly();
            if (ModConfig.instance.IsLockOntoRig.Value) Mods.Mods.LockOntoRig();
            if (ModConfig.instance.IsHoldRig.Value) Mods.Mods.HoldRig();
            if (ModConfig.instance.IsRigGun.Value) Mods.Mods.RigGun();
            if (ModConfig.instance.IsFreezeRig.Value) Mods.Mods.FreezeRig();
            if (ModConfig.instance.IsTPGun.Value) Mods.Mods.TPGun();
            if (ModConfig.instance.IsGetPIDGun.Value) Mods.Mods.GetPID();
            if (ModConfig.instance.IsMuteEveryoneExceptGun.Value) Mods.Mods.MuteEveryoneExceptGun();
            if (ModConfig.instance.IsSilKick.Value) Mods.Mods.silkickgun();
            if (ModConfig.instance.IsAntiReportEnabled.Value) Mods.Mods.AntiReport();
            if (ModConfig.instance.IsTwerkingCarti.Value) Mods.Mods.TwerkingCarti();
            if (ModConfig.instance.IsCoolSword.Value) Mods.Mods.Sword();
            if (ModConfig.instance.IsTravis.Value) Mods.Mods.TravisScott();
            if (ModConfig.instance.IsPhone.Value) Mods.Mods.Samsung();
            if (ModConfig.instance.IsAdminGrab.Value) Mods.Mods.AdminGrabAll();
            if (ModConfig.instance.IsKormakur.Value) Mods.Mods.KormakurFemboys();
            if (ModConfig.instance.IsAxe.Value) Mods.Mods.Axe();
            if (ModConfig.instance.IsTv.Value) Mods.Mods.SkidTV();
            if (ModConfig.instance.IsGroundHelper.Value) Mods.Mods.GroundHelper();
            if (ModConfig.instance.IsAmplifiedMonke.Value) Mods.Mods.AmplifiedMonke();
            if (ModConfig.instance.IsWebSlingers.Value) Mods.Mods.WebSlingers();
            if (ModConfig.instance.IsFunnyRig.Value) Mods.Mods.MessUpRig();
            if (ModConfig.instance.IsRecroomTorso.Value) Mods.Mods.RecRoomTorso();
            if (ModConfig.instance.IsRecroomRig.Value) Mods.Mods.RecRoomRig();
            if (ModConfig.instance.FullBodyTracking.Value) Mods.Mods.FullBodyTracking();
            if (ModConfig.instance.AdminLaser.Value) Mods.Mods.AdminLaser();
            if (ModConfig.instance.IsBees.Value)
            {
                if (Main.beesCoroutine == null)
                    Main.beesCoroutine = Main.instance.StartCoroutine(Mods.Mods.Bees());
            }
            if (ModConfig.instance.IsTagGun.Value) Mods.Mods.TagGun();
            if (ModConfig.instance.IsTagAll.Value) Mods.Mods.TagAll();
            if (ModConfig.instance.IsCopyRigGun.Value) Mods.Mods.CopyRigGun();
            if (ModConfig.instance.IsBypassAutoMod.Value) Mods.Mods.BypassAutomod();
            if (ModConfig.instance.IsBoxEsp.Value) Mods.Mods.BoxESP();
            if (ModConfig.instance.PreviewGun.Value) GunLib.LetGun();
            if (ModConfig.instance.IsInvisMonke.Value) Mods.Mods.InvisMonke();
            if (ModConfig.instance.IsBraceletSpam.Value) Mods.Mods.BraceletSpam();
            if (ModConfig.instance.IsSpazMonke.Value) Mods.Mods.SpazMonke();
            if (ModConfig.instance.IsCherryBomb.Value) Mods.Mods.CherryBomb();
            if (ModConfig.instance.IsRagdoll.Value) Mods.Mods.Ragdoll();
            if (ModConfig.instance.IsWasdFly.Value) Mods.Mods.WasdFly();
            if (ModConfig.instance.MovementRecorder.Value) Mods.Mods.MovementRecorder();
            if (ModConfig.instance.IsBallEsp.Value) Mods.Mods.SkeletonESP();
            if (ModConfig.instance.IsEnabledBuilderShelf.Value) Mods.Mods.EnableBuilderShelf();
            if (ModConfig.instance.IsAnnoy.Value) Mods.Mods.Annoy();
            if (ModConfig.instance.IsNametags.Value) Mods.Mods.NametagsMod();
            if (ModConfig.instance.IsJmanSoundSpam.Value) Mods.Mods.SoundSpam(UnityEngine.Random.Range(336, 338));
            if (ModConfig.instance.IsCrystalSoundSpam.Value)
            {
                int[] sounds = {
                UnityEngine.Random.Range(40,54),
                UnityEngine.Random.Range(214,221)
            };
                Mods.Mods.SoundSpam(sounds[UnityEngine.Random.Range(0, 1)]);
            }
            if (ModConfig.instance.Dash.Value) Mods.Mods.Dash();
            if (ModConfig.instance.IsSpider.Value) Mods.Mods.Spider();
            if (ModConfig.instance.InverseSpider.Value) Mods.Mods.InverseSpider();
            if (ModConfig.instance.Bean.Value) Mods.Mods.Bean();
            if (ModConfig.instance.IsVideoPlayer.Value) Mods.Mods.VideoPlayer();
            if (ModConfig.instance.IsAdminStrangle.Value) Mods.Mods.AdminStrangle();
            if (ModConfig.instance.IsSizeChanger.Value) Mods.Mods.SizeChanger();
            if (ModConfig.instance.HandTurn.Value) Mods.Mods.HandTurn();
            if (ModConfig.instance.JoystickRotation.Value) Mods.Mods.JoystickRot();
            if (ModConfig.instance.IsBoop.Value) Mods.Mods.Boop();
            if (ModConfig.instance.IsAdminTitan.Value) Mods.Mods.AdminTitan();
            if (ModConfig.instance.IsGhostWalk.Value) { Mods.Mods.GhostWalk(true); Mods.Mods.GhostWalk(false); };
            Mods.Mods.UpdateCustomProperties();
            Mods.Mods.CreatePlayerOutline();
        }
    }
}
