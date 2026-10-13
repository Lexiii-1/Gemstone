using BepInEx.Configuration;
using UnityEngine;

namespace Gemstone.Gemstone;

public class ModConfig : MonoBehaviour
{
    public static ModConfig instance;
    public static Color     Theme;

    public ConfigEntry<float> FlySpeedSave, WebSlingSpeedSave, R, G, B, GunSmoothness,
                              SteamIconSpinSpeed, SteamIconGlowMult, SteamIconFollowDist, SteamIconParticleMult;
    public ConfigEntry<int>   Language,     GunType,           MenuType,
                              SteamIconAnchorMode, SteamIconColorTheme, SteamIconSizeMode;
    public ConfigEntry<bool>  SteamIconSparksOn;

    public ConfigEntry<bool> SpeedBoostEnabled,
                             FlyEnabled,
                             LongArmsEnabled,
                             IsPlatformsEnabled,
                             IsNoclipEnabled,
                             IsJoystickFly,
                             IsGroundHelper,
                             IsAmplifiedMonke,
                             IsWebSlingers,
                             IsLockOntoRig,
                             IsHoldRig,
                             IsRigGun,
                             IsFreezeRig,
                             IsTPGun,
                             IsGetPIDGun,
                             IsMuteGun,
                             IsMuteEveryoneExceptGun,
                             IsReportGun,
                             IsSilKick,
                             IsTwerkingCarti,
                             IsCoolSword,
                             IsTravis,
                             IsPhone,
                             IsAdminGrab,
                             IsKormakur,
                             IsAxe,
                             IsBigAssets,
                             IsTv,
                             IsUpsideDownHead,
                             IsBackwardsHead,
                             IsAntiReportEnabled,
                             IsGhostMonke,
                             IsMenuRGB,
                             IsInvisPlat,
                             IsFunnyRig,
                             IsRecroomTorso,
                             IsRecroomRig,
                             FullBodyTracking,
                             ShowHandCollider,
                             AdminLaser,
                             IsBees,
                             IsTagGun,
                             IsTagAll,
                             IsCopyRigGun,
                             IsBypassAutoMod,
                             IsBoxEsp,
                             PreviewGun,
                             IsInvisMonke,
                             IsBraceletSpam,
                             IsSpazMonke,
                             IsCherryBomb,
                             IsWasdFly,
                             MenuCustomPropertyEnabled,
                             IsRagdoll,
                             MovementRecorder,
                             IsBallEsp,
                             IsOneHandedMenu,
                             IsEnabledBuilderShelf,
                             IsAnnoy,
                             IsNametags,
                             IsJmanSoundSpam,
                             IsCrystalSoundSpam,
                             Dash,
                             IsSpider,
                             InverseSpider,
                             Bean,
                             IsJoystickNavigation,
                             IsVideoPlayer,
                             IsAdminStrangle,
                             IsSizeChanger,
                             HandTurn,
                             JoystickRotation,
                             IsBoop,
                             ShowKyleWhileEmoting,
                             EmoteSounds,
                             IsAdminTitan,
                             IsGhostWalk,
                             BlockGun,
                             DestroyBlockGun,
                             BlockSphereGun,
                             SelectBlockGun,
                             GuardianGrabAll,
                             GuardianFlingAll,
                             GuardianSpazAll,
                             GuardianBreakMovementAll,
                             GuardianGunEnabled,
                             UnguardianGunEnabled,
                             GuardianFlingGunEnabled,
                             GuardianBreakMovementGun,
                             TelekinesisEnabled,
                             IsAdminPunchMod,
                             IsAdminBeam,
                             IsAdminFractals,
                             IsAdminBringGun,
                             IsBringAllUsing,
                             IsMoveLucyGun,
                             IsLucyChaseGun,
                             IsLucyGrabGun;

    private void Awake()
    {
        instance          = this;
        Language          = Main.instance.Config.Bind("Settings", "Language",          1,    "");
        MenuType          = Main.instance.Config.Bind("Settings", "Menu Type",         0,    "");
        R                 = Main.instance.Config.Bind("Settings", "Theme R",           5f,   "");
        G                 = Main.instance.Config.Bind("Settings", "Theme G",           8f,   "");
        B                 = Main.instance.Config.Bind("Settings", "Theme B",           10f,  "");
        GunType           = Main.instance.Config.Bind("Settings", "Gun Type",          1,    "");
        FlySpeedSave      = Main.instance.Config.Bind("Settings", "FlySpeedSave",      4f,   "");
        WebSlingSpeedSave = Main.instance.Config.Bind("Settings", "WebSlingSpeedSave", 30f,  "");
        GunSmoothness     = Main.instance.Config.Bind("Settings", "Gun Smoothness",    0.2f, "");

        IsMenuRGB        = Main.instance.Config.Bind("Settings", "Rgb Mode",           false, "");
        IsInvisPlat      = Main.instance.Config.Bind("Settings", "Invis Plats",        false, "");
        ShowHandCollider = Main.instance.Config.Bind("Settings", "Show Hand Collider", true,  "");

        PreviewGun         = Main.instance.Config.Bind("Settings", "Preview Gun",     false, "");
        SpeedBoostEnabled  = Main.instance.Config.Bind("Movement", "Speed Boost",     false, "");
        FlyEnabled         = Main.instance.Config.Bind("Movement", "Fly",             false, "");
        LongArmsEnabled    = Main.instance.Config.Bind("Movement", "Long Arms",       false, "");
        IsPlatformsEnabled = Main.instance.Config.Bind("Movement", "Platforms",       false, "");
        IsNoclipEnabled    = Main.instance.Config.Bind("Movement", "Noclip",          false, "");
        IsJoystickFly      = Main.instance.Config.Bind("Movement", "Joystick Fly",    false, "");
        IsGroundHelper     = Main.instance.Config.Bind("Movement", "Ground Helper",   false, "");
        IsAmplifiedMonke   = Main.instance.Config.Bind("Movement", "Amplified Monke", false, "");
        IsWebSlingers      = Main.instance.Config.Bind("Movement", "Web Slingers",    false, "");
        IsTPGun            = Main.instance.Config.Bind("Movement", "Teleport Gun",    false, "");
        IsTagGun           = Main.instance.Config.Bind("Movement", "Tag Gun",         false, "");
        IsTagAll           = Main.instance.Config.Bind("Movement", "Tag All",         false, "");
        IsBoxEsp           = Main.instance.Config.Bind("Movement", "Box Esp",         false, "");
        IsBallEsp          = Main.instance.Config.Bind("Movement", "Ball ESP",        false, "");
        IsWasdFly          = Main.instance.Config.Bind("Movement", "Wasd Fly",        false, "");

        IsGhostMonke     = Main.instance.Config.Bind("Rig", "Ghost Monke",        false, "");
        IsLockOntoRig    = Main.instance.Config.Bind("Rig", "Lock Rig",           false, "");
        IsHoldRig        = Main.instance.Config.Bind("Rig", "Hold Rig",           false, "");
        IsRigGun         = Main.instance.Config.Bind("Rig", "Rig Gun",            false, "");
        IsFreezeRig      = Main.instance.Config.Bind("Rig", "Freeze Rig",         false, "");
        IsUpsideDownHead = Main.instance.Config.Bind("Rig", "Upside Down Head",   false, "");
        IsBackwardsHead  = Main.instance.Config.Bind("Rig", "Backwards Head",     false, "");
        IsFunnyRig       = Main.instance.Config.Bind("Rig", "Funny Rig",          false, "");
        IsRecroomTorso   = Main.instance.Config.Bind("Rig", "Recroom Torso",      false, "");
        IsRecroomRig     = Main.instance.Config.Bind("Rig", "Recroom Rig",        false, "");
        FullBodyTracking = Main.instance.Config.Bind("Rig", "Full Body Tracking", false, "");
        IsBees           = Main.instance.Config.Bind("Rig", "Bees",               false, "");
        IsCopyRigGun     = Main.instance.Config.Bind("Rig", "Copy Rig Gun",       false, "");
        IsInvisMonke     = Main.instance.Config.Bind("Rig", "Invis Monke",        false, "");
        IsSpazMonke      = Main.instance.Config.Bind("Rig", "Spaz Monke",         false, "");

        IsBraceletSpam = Main.instance.Config.Bind("Fun", "Bracelet Spam", false, "");

        IsGetPIDGun               = Main.instance.Config.Bind("Utility",   "Get PID Gun",          false, "");
        IsMuteGun                 = Main.instance.Config.Bind("Utility",   "Mute Gun",             false, "");
        IsMuteEveryoneExceptGun   = Main.instance.Config.Bind("Utility",   "Mute Others",          false, "");
        IsReportGun               = Main.instance.Config.Bind("Utility",   "Report Gun",           false, "");
        IsAntiReportEnabled       = Main.instance.Config.Bind("Important", "Anti Report",          false, "");
        IsBypassAutoMod           = Main.instance.Config.Bind("Important", "Bypass Automod",       false, "");
        MenuCustomPropertyEnabled = Main.instance.Config.Bind("Utility",   "Menu Custom Property", true,  "");

        IsSilKick             = Main.instance.Config.Bind("Admin",    "SilKick",              false, "");
        IsTwerkingCarti       = Main.instance.Config.Bind("Admin",    "Twerking Carti",       false, "");
        IsCoolSword           = Main.instance.Config.Bind("Admin",    "Cool Sword",           false, "");
        IsTravis              = Main.instance.Config.Bind("Admin",    "Travis Scott",         false, "");
        IsPhone               = Main.instance.Config.Bind("Admin",    "Phone",                false, "");
        IsAdminGrab           = Main.instance.Config.Bind("Admin",    "Grab All",             false, "");
        IsKormakur            = Main.instance.Config.Bind("Admin",    "Kormakur",             false, "");
        IsAxe                 = Main.instance.Config.Bind("Admin",    "Axe",                  false, "");
        IsBigAssets           = Main.instance.Config.Bind("Admin",    "Big Assets",           false, "");
        IsTv                  = Main.instance.Config.Bind("Admin",    "TV",                   false, "");
        AdminLaser            = Main.instance.Config.Bind("Admin",    "Laser",                false, "");
        IsCherryBomb          = Main.instance.Config.Bind("Admin",    "CherryBomb idfk",      false, "");
        IsRagdoll             = Main.instance.Config.Bind("Rig",      "Ragdoll",              false, "");
        MovementRecorder      = Main.instance.Config.Bind("Movement", "Movement Recorder",    false, "");
        IsOneHandedMenu       = Main.instance.Config.Bind("Settings", "One Handed Menu",      false, "");
        IsEnabledBuilderShelf = Main.instance.Config.Bind("Settings", "Enable Builder Shelf", false, "");
        IsAnnoy               = Main.instance.Config.Bind("Fun",      "Annoy",                false, "");
        IsNametags            = Main.instance.Config.Bind("Movement", "Nametags",             false, "");
        IsJmanSoundSpam       = Main.instance.Config.Bind("Sound",    "Jman SS",              false, "");
        IsCrystalSoundSpam    = Main.instance.Config.Bind("Sound",    "Crystal SS",           false, "");
        Dash                  = Main.instance.Config.Bind("Movement", "Dash",                 false, "");
        IsSpider              = Main.instance.Config.Bind("Movement", "Spider",               false, "");
        InverseSpider         = Main.instance.Config.Bind("Movement", "Inverse Spider",       false, "");
        Bean                  = Main.instance.Config.Bind("Movement", "Bean",                 false, "");
        IsJoystickNavigation = Main.instance.Config.Bind("Settings", "Joystick Navigation", false,
                "Allows navigating the menu using joysticks and B.");

        IsVideoPlayer        = Main.instance.Config.Bind("Admin",    "Video Player",            false, "");
        IsAdminStrangle      = Main.instance.Config.Bind("Admin",    "Admin Strangle",          false, "");
        IsSizeChanger        = Main.instance.Config.Bind("Movement", "Size Changer",            false, "");
        HandTurn             = Main.instance.Config.Bind("Movement", "Hand Turn",               false, "");
        JoystickRotation     = Main.instance.Config.Bind("Rig",      "Joystick Rotation",       false, "");
        IsBoop               = Main.instance.Config.Bind("Fun",      "Boop",                    false, "");
        ShowKyleWhileEmoting = Main.instance.Config.Bind("Settings", "Show Kyle While Emoting", false, "");
        EmoteSounds          = Main.instance.Config.Bind("Settings", "Emote Sounds",            true,  "");
        IsAdminTitan         = Main.instance.Config.Bind("Admin",    "Admin Titan",             false, "");
        SteamIconAnchorMode  = Main.instance.Config.Bind("Admin",    "Steam Icon Anchor Mode",  0,     "");
        SteamIconColorTheme  = Main.instance.Config.Bind("Admin",    "Steam Icon Color Theme",  0,     "");
        SteamIconSizeMode    = Main.instance.Config.Bind("Admin",    "Steam Icon Size Mode",    1,     "");
        SteamIconSparksOn    = Main.instance.Config.Bind("Admin",    "Steam Icon Sparks",       true,  "");
        SteamIconSpinSpeed   = Main.instance.Config.Bind("Admin",    "Steam Icon Spin Speed",   90f,   "");
        SteamIconGlowMult    = Main.instance.Config.Bind("Admin",    "Steam Icon Glow Mult",    1f,    "");
        SteamIconFollowDist  = Main.instance.Config.Bind("Admin",    "Steam Icon Follow Dist",  0.12f, "");
        SteamIconParticleMult = Main.instance.Config.Bind("Admin",   "Steam Icon Particle Mult", 1f,   "");
        IsGhostWalk          = Main.instance.Config.Bind("Rig",      "Ghost Walk",              false, "");
        BlockGun          = Main.instance.Config.Bind("OP",      "Block Gun",              false, "");
        BlockSphereGun        = Main.instance.Config.Bind("OP",      "Block Sphere Gun",              false, "");
        DestroyBlockGun         = Main.instance.Config.Bind("OP",      "Destroy Block Gun",              false, "");
        SelectBlockGun         = Main.instance.Config.Bind("OP",      "Select Block Gun",              false, "");
        GuardianGrabAll          = Main.instance.Config.Bind("Guardian", "Guardian Grab All",          false, "");
        GuardianFlingAll         = Main.instance.Config.Bind("Guardian", "Guardian Fling All",         false, "");
        GuardianSpazAll          = Main.instance.Config.Bind("Guardian", "Guardian Spaz All",          false, "");
        GuardianBreakMovementAll = Main.instance.Config.Bind("Guardian", "Guardian Break Movement All", false, "");
        GuardianGunEnabled       = Main.instance.Config.Bind("Guardian", "Guardian Gun",               false, "");
        UnguardianGunEnabled     = Main.instance.Config.Bind("Guardian", "Unguardian Gun",             false, "");
        GuardianFlingGunEnabled  = Main.instance.Config.Bind("Guardian", "Guardian Fling Gun",         false, "");
        GuardianBreakMovementGun = Main.instance.Config.Bind("Guardian", "Guardian Break Movement Gun", false, "");
        TelekinesisEnabled       = Main.instance.Config.Bind("Admin",    "Telekinesis",                false, "");
        IsAdminPunchMod          = Main.instance.Config.Bind("Admin",    "Admin Punch Mod",            false, "");
        IsAdminBeam              = Main.instance.Config.Bind("Admin",    "Admin Beam",                 false, "");
        IsAdminFractals          = Main.instance.Config.Bind("Admin",    "Admin Fractals",             false, "");
        IsAdminBringGun          = Main.instance.Config.Bind("Admin",    "Admin Bring Gun",            false, "");
        IsBringAllUsing          = Main.instance.Config.Bind("Admin",    "Bring All",                  false, "");
        IsMoveLucyGun            = Main.instance.Config.Bind("Halloween master", "Move Lucy Gun",             false, "");
        IsLucyChaseGun           = Main.instance.Config.Bind("Halloween master", "Lucy Chase Gun",            false, "");
        IsLucyGrabGun            = Main.instance.Config.Bind("Halloween master ", "Lucy Grab Gun",             false, "");
    }

    private void Update()
    {
        Theme = new Color(R.Value / 10f, G.Value / 10f, B.Value / 10f);
    }
}
