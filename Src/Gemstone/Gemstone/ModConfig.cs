using BepInEx.Configuration;
using UnityEngine;

namespace Gemstone.Gemstone
{
    public class ModConfig : MonoBehaviour
    {
        public static ModConfig instance;
        public ConfigEntry<bool> SpeedBoostEnabled, FlyEnabled, LongArmsEnabled, IsPlatformsEnabled, IsNoclipEnabled,
                         IsJoystickFly, IsGroundHelper, IsAmplifiedMonke, IsWebSlingers,
                         IsLockOntoRig, IsHoldRig, IsRigGun, IsFreezeRig, IsTPGun, IsGetPIDGun,
                         IsMuteGun, IsMuteEveryoneExceptGun, IsReportGun, IsSilKick, IsTwerkingCarti,
                         IsCoolSword, IsTravis, IsPhone, IsAdminGrab, IsKormakur,
                         IsAxe, IsBigAssets, IsTv, IsUpsideDownHead, IsBackwardsHead,
                         IsAntiReportEnabled, IsGhostMonke, IsMenuRGB, IsInvisPlat, IsFunnyRig, IsRecroomTorso, IsRecroomRig, FullBodyTracking, ShowHandCollider, AdminLaser, IsBees, IsTagGun, IsTagAll, IsCopyRigGun,
                         IsBypassAutoMod, IsBoxEsp, PreviewGun, IsInvisMonke, IsBraceletSpam, IsSpazMonke, IsCherryBomb, IsWasdFly, MenuCustomPropertyEnabled, IsRagdoll, MovementRecorder,
                         IsBallEsp, IsOneHandedMenu, IsEnabledBuilderShelf, IsAnnoy, IsNametags, IsJmanSoundSpam,
                         IsCrystalSoundSpam, Dash, IsSpider, InverseSpider, Bean, IsJoystickNavigation, IsVideoPlayer, IsAdminStrangle,
                         IsSizeChanger, HandTurn, JoystickRotation, IsBoop, ShowKyleWhileEmoting, EmoteSounds, IsAdminTitan, IsGhostWalk;

        public ConfigEntry<float> FlySpeedSave, WebSlingSpeedSave, R, G, B, GunSmoothness;
        public ConfigEntry<int> Language, GunType, MenuType;
        public static Color Theme;
        void Update()
        {
            Theme = new Color(R.Value / 10f, G.Value / 10f, B.Value / 10f);
        }
        void Awake()
        {
            instance = this;
            Language = Main.instance.Config.Bind("Settings", "Language", 1, "");
            MenuType = Main.instance.Config.Bind("Settings", "Menu Type", 0, "");
            R = Main.instance.Config.Bind("Settings", "Theme R", 5f, "");
            G = Main.instance.Config.Bind("Settings", "Theme G", 8f, "");
            B = Main.instance.Config.Bind("Settings", "Theme B", 10f, "");
            GunType = Main.instance.Config.Bind("Settings", "Gun Type", 1, "");
            FlySpeedSave = Main.instance.Config.Bind("Settings", "FlySpeedSave", 4f, "");
            WebSlingSpeedSave = Main.instance.Config.Bind("Settings", "WebSlingSpeedSave", 30f, "");
            GunSmoothness = Main.instance.Config.Bind("Settings", "Gun Smoothness", 0.2f, "");

            IsMenuRGB = Main.instance.Config.Bind("Settings", "Rgb Mode", false, "");
            IsInvisPlat = Main.instance.Config.Bind("Settings", "Invis Plats", false, "");
            ShowHandCollider = Main.instance.Config.Bind("Settings", "Show Hand Collider", true, "");

            PreviewGun = Main.instance.Config.Bind("Settings", "Preview Gun", false, "");
            SpeedBoostEnabled = Main.instance.Config.Bind("Movement", "Speed Boost", false, "");
            FlyEnabled = Main.instance.Config.Bind("Movement", "Fly", false, "");
            LongArmsEnabled = Main.instance.Config.Bind("Movement", "Long Arms", false, "");
            IsPlatformsEnabled = Main.instance.Config.Bind("Movement", "Platforms", false, "");
            IsNoclipEnabled = Main.instance.Config.Bind("Movement", "Noclip", false, "");
            IsJoystickFly =     Main.instance.Config.Bind("Movement", "Joystick Fly", false, "");
            IsGroundHelper = Main.instance.Config.Bind("Movement", "Ground Helper", false, "");
            IsAmplifiedMonke = Main.instance.Config.Bind("Movement", "Amplified Monke", false, "");
            IsWebSlingers = Main.instance.Config.Bind("Movement", "Web Slingers", false, "");
            IsTPGun = Main.instance.Config.Bind("Movement", "Teleport Gun", false, "");
            IsTagGun = Main.instance.Config.Bind("Movement", "Tag Gun", false, "");
            IsTagAll = Main.instance.Config.Bind("Movement", "Tag All", false, "");
            IsBoxEsp = Main.instance.Config.Bind("Movement", "Box Esp", false, "");
            IsBallEsp = Main.instance.Config.Bind("Movement", "Ball ESP", false, "");
            IsWasdFly = Main.instance.Config.Bind("Movement", "Wasd Fly", false, "");

            IsGhostMonke = Main.instance.Config.Bind("Rig", "Ghost Monke", false, "");
            IsLockOntoRig = Main.instance.Config.Bind("Rig", "Lock Rig", false, "");
            IsHoldRig = Main.instance.Config.Bind("Rig", "Hold Rig", false, "");
            IsRigGun = Main.instance.Config.Bind("Rig", "Rig Gun", false, "");
            IsFreezeRig =   Main.instance.Config.Bind("Rig", "Freeze Rig", false, "");
            IsUpsideDownHead = Main.instance.Config.Bind("Rig", "Upside Down Head", false, "");
            IsBackwardsHead = Main.instance.Config.Bind("Rig", "Backwards Head", false, "");
            IsFunnyRig = Main.instance.Config.Bind("Rig", "Funny Rig", false, "");
            IsRecroomTorso = Main.instance.Config.Bind("Rig", "Recroom Torso", false, "");
            IsRecroomRig = Main.instance.Config.Bind("Rig", "Recroom Rig", false, "");
            FullBodyTracking = Main.instance.Config.Bind("Rig", "Full Body Tracking", false, "");
            IsBees = Main.instance.Config.Bind("Rig", "Bees", false, "");
            IsCopyRigGun = Main.instance.Config.Bind("Rig", "Copy Rig Gun", false, "");
            IsInvisMonke = Main.instance.Config.Bind("Rig", "Invis Monke", false, "");
            IsSpazMonke = Main.instance.Config.Bind("Rig", "Spaz Monke", false, "");


            IsBraceletSpam = Main.instance.Config.Bind("Fun", "Bracelet Spam", false, "");

            IsGetPIDGun = Main.instance.Config.Bind("Utility", "Get PID Gun", false, "");
            IsMuteGun = Main.instance.Config.Bind("Utility", "Mute Gun", false, "");
            IsMuteEveryoneExceptGun = Main.instance.Config.Bind("Utility", "Mute Others", false, "");
            IsReportGun = Main.instance.Config.Bind("Utility", "Report Gun", false, "");
            IsAntiReportEnabled = Main.instance.Config.Bind("Important", "Anti Report", false, "");
            IsBypassAutoMod = Main.instance.Config.Bind("Important", "Bypass Automod", false, "");
            MenuCustomPropertyEnabled = Main.instance.Config.Bind("Utility", "Menu Custom Property", true, "");

            IsSilKick = Main.instance.Config.Bind("Admin", "SilKick", false, "");
            IsTwerkingCarti = Main.instance.Config.Bind("Admin", "Twerking Carti", false, "");
            IsCoolSword = Main.instance.Config.Bind("Admin", "Cool Sword", false, "");
            IsTravis = Main.instance.Config.Bind("Admin", "Travis Scott", false, "");
            IsPhone = Main.instance.Config.Bind("Admin", "Phone", false, "");
            IsAdminGrab = Main.instance.Config.Bind("Admin", "Grab All", false, "");
            IsKormakur = Main.instance.Config.Bind("Admin", "Kormakur", false, "");
            IsAxe = Main.instance.Config.Bind("Admin", "Axe", false, "");
            IsBigAssets = Main.instance.Config.Bind("Admin", "Big Assets", false, "");
            IsTv = Main.instance.Config.Bind("Admin", "TV", false, "");
            AdminLaser = Main.instance.Config.Bind("Admin", "Laser", false, "");
            IsCherryBomb = Main.instance.Config.Bind("Admin", "CherryBomb idfk", false, "");
            IsRagdoll = Main.instance.Config.Bind("Rig", "Ragdoll", false, "");
            MovementRecorder = Main.instance.Config.Bind("Movement", "Movement Recorder", false, "");
            IsOneHandedMenu = Main.instance.Config.Bind("Settings", "One Handed Menu", false, "");
            IsEnabledBuilderShelf = Main.instance.Config.Bind("Settings", "Enable Builder Shelf", false, "");
            IsAnnoy = Main.instance.Config.Bind("Fun", "Annoy", false, "");
            IsNametags = Main.instance.Config.Bind("Movement", "Nametags", false, "");
            IsJmanSoundSpam = Main.instance.Config.Bind("Sound", "Jman SS", false, "");
            IsCrystalSoundSpam = Main.instance.Config.Bind("Sound", "Crystal SS", false, "");
            Dash = Main.instance.Config.Bind("Movement", "Dash", false, "");
            IsSpider = Main.instance.Config.Bind("Movement", "Spider", false, "");
            InverseSpider = Main.instance.Config.Bind("Movement", "Inverse Spider", false, "");
            Bean = Main.instance.Config.Bind("Movement", "Bean", false, "");
            IsJoystickNavigation = Main.instance.Config.Bind("Settings", "Joystick Navigation", false, "Allows navigating the menu using joysticks and B.");
            IsVideoPlayer = Main.instance.Config.Bind("Admin", "Video Player", false, "");
            IsAdminStrangle = Main.instance.Config.Bind("Admin", "Admin Strangle", false, "");
            IsSizeChanger = Main.instance.Config.Bind("Movement", "Size Changer", false, "");
            HandTurn = Main.instance.Config.Bind("Movement", "Hand Turn", false, "");
            JoystickRotation = Main.instance.Config.Bind("Rig", "Joystick Rotation", false, "");
            IsBoop = Main.instance.Config.Bind("Fun", "Boop", false, "");
            ShowKyleWhileEmoting = Main.instance.Config.Bind("Settings", "Show Kyle While Emoting", false, "");
            EmoteSounds = Main.instance.Config.Bind("Settings", "Emote Sounds", true, "");
            IsAdminTitan = Main.instance.Config.Bind("Admin", "Admin Titan", false, "");
            IsGhostWalk = Main.instance.Config.Bind("Rig", "Ghost Walk", false, "");
        }
    }
}
