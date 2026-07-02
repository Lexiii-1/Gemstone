using BepInEx.Configuration;
using Gemstone.Mods.Cosmetx;
using Gemstone.patches;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Gemstone.Gemstone;

public class ModBackend : MonoBehaviour
{
    private void Awake() => GemstoneMenuBackend.Rebuild();

    private void Update() => GemstoneMenuBackend.TickEnabledMods();
}

public static class GemstoneMenuBackend
{
    public const int ButtonsPerPage = 4;

    private static readonly List<ModCategory> categories = new();

    private static bool hasBuilt;

    public static IReadOnlyList<ModCategory> Categories
    {
        get
        {
            EnsureBuilt();

            return categories;
        }
    }

    public static void Rebuild()
    {
        categories.Clear();

        ModConfig config = ModConfig.instance;

        if (config == null)
        {
            hasBuilt = false;

            return;
        }

        AddCategory(
                0,
                "Movement",
                false,
                Toggle(config.SpeedBoostEnabled,  Mods.Mods.SpeedBoost),
                Toggle(config.FlyEnabled,         Mods.Mods.Fly, nameKey: "Fly (A)"),
                Toggle(config.IsPlatformsEnabled, Mods.Mods.Platforms, nameKey: "Platforms (LG, RG)"),
                Toggle(config.IsJoystickFly,      Mods.Mods.JoystickFly, nameKey: "Joystick Fly (LJ, RJ)"),
                Toggle(config.LongArmsEnabled,    Mods.Mods.LongArms, Mods.Mods.UnLongArms),
                Toggle(config.IsGroundHelper,     Mods.Mods.GroundHelper, nameKey: "Ground Helper (LG + A)"),
                Toggle(config.IsAmplifiedMonke,   Mods.Mods.AmplifiedMonke),
                Toggle(config.IsNoclipEnabled,    Mods.Mods.Noclip, nameKey: "Noclip (B)"),
                Toggle(config.IsWebSlingers,      Mods.Mods.WebSlingers, nameKey: "Web Slingers (LG, RG)"),
                Toggle(config.IsTPGun,            Mods.Mods.TPGun, nameKey: "Teleport Gun"),
                Toggle(config.IsTagGun,           Mods.Mods.TagGun, Mods.Mods.FixRig, nameKey: "Tag Gun (D?)"),
                Toggle(config.IsTagAll,           Mods.Mods.TagAll, Mods.Mods.FixRig, nameKey: "Tag All (D?)"),
                Toggle(config.IsWasdFly,          Mods.Mods.WasdFly, nameKey: "WASD Fly"),
                Toggle(config.MovementRecorder,   Mods.Mods.MovementRecorder, nameKey: "Movement Recorder (A)"),
                Toggle(config.Dash,               Mods.Mods.Dash, nameKey: "Dash (A, LG)"),
                Toggle(config.IsSizeChanger, Mods.Mods.SizeChanger, Mods.Mods.DisableSizeChanger,
                        nameKey: "Size Changer (RT, A, LG, Admin SS)"),
                Toggle(config.HandTurn, Mods.Mods.HandTurn, nameKey: "Hand Turn (RG)")
        );

        AddCategory(
                1,
                "Utility",
                false,
                Toggle(config.IsGetPIDGun,             Mods.Mods.GetPID, nameKey: "Get PID Gun"),
                Toggle(config.IsMuteGun,               Mods.Mods.MuteGun),
                Toggle(config.IsMuteEveryoneExceptGun, Mods.Mods.MuteEveryoneExceptGun, nameKey: "Mute Others Gun"),
                Toggle(config.IsReportGun,             Mods.Mods.ReportGun),
                Button("Mute All",       Mods.Mods.MuteAll),
                Button("Unmute All",     Mods.Mods.UnmuteAll),
                Button("Ignore Far Tag", () => ExtremelyFarTagPatch.isDetected = false),
                Toggle(config.MenuCustomPropertyEnabled, nameKey: "Show Menu Custom Property"),
                Button("Get PID Self", Mods.Mods.GetPIDSelf)
        );

        AddCategory(
                2,
                "Rig Mods",
                false,
                Toggle(config.IsGhostMonke,     Mods.Mods.GhostMonke,  Mods.Mods.FixRig, nameKey: "Ghost Monke (A)"),
                Toggle(config.IsLockOntoRig,    Mods.Mods.LockOntoRig, Mods.Mods.FixRig),
                Toggle(config.IsHoldRig,        Mods.Mods.HoldRig,     Mods.Mods.FixRig),
                Toggle(config.IsRigGun,         Mods.Mods.RigGun,      Mods.Mods.FixRig),
                Toggle(config.IsFreezeRig,      Mods.Mods.FreezeRig,   Mods.Mods.FixRig, nameKey: "Freeze Rig (B)"),
                Toggle(config.IsUpsideDownHead, Mods.Mods.UpsideDownNeck),
                Toggle(config.IsBackwardsHead,  Mods.Mods.BackwardsHead),
                Toggle(config.IsFunnyRig,       Mods.Mods.MessUpRig,        Mods.Mods.FixRig, nameKey: "Funny Rig"),
                Toggle(config.IsRecroomTorso,   Mods.Mods.RecRoomTorso,     Mods.Mods.FixRig),
                Toggle(config.IsRecroomRig,     Mods.Mods.RecRoomRig,       Mods.Mods.FixRig),
                Toggle(config.FullBodyTracking, Mods.Mods.FullBodyTracking, Mods.Mods.FixRig),
                Toggle(config.IsBees,           TickBees,                   DisableBees),
                Toggle(config.IsCopyRigGun,     Mods.Mods.CopyRigGun,       Mods.Mods.FixRig, nameKey: "Copy Rig"),
                Toggle(config.IsInvisMonke,     Mods.Mods.InvisMonke,       Mods.Mods.FixRig),
                Toggle(config.IsSpazMonke,      Mods.Mods.SpazMonke,        Mods.Mods.FixRig),
                Toggle(config.IsRagdoll,        Mods.Mods.Ragdoll,          Mods.Mods.FixRig, nameKey: "Ragdoll (A)"),
                Toggle(config.IsSpider,         Mods.Mods.Spider,           Mods.Mods.FixRig),
                Toggle(config.InverseSpider,    Mods.Mods.InverseSpider,    Mods.Mods.FixRig),
                Toggle(config.Bean,             Mods.Mods.Bean,             Mods.Mods.FixRig),
                Toggle(config.JoystickRotation, Mods.Mods.JoystickRot, Mods.Mods.FixRig,
                        nameKey: "Joystick Torso Rotation"),
                Toggle(config.IsGhostWalk, TickGhostWalk, Mods.Mods.FixRig)
        );

        AddCategory(
                3,
                "Settings",
                false,
                Toggle(config.IsInvisPlat, nameKey: "Invis Plats"),
                Toggle(config.IsMenuRGB,   nameKey: "Menu RGB"),
                Button("R +", () => IncrementThemeValue(config.R), true),
                Button("G +", () => IncrementThemeValue(config.G), true),
                Button("B +", () => IncrementThemeValue(config.B), true),
                Toggle(config.ShowHandCollider),
                Button("Fly Speed +",      () => ChangeFloat(config.FlySpeedSave,      0.1f,  "0.0")),
                Button("Fly Speed -",      () => ChangeFloat(config.FlySpeedSave,      -0.1f, "0.0")),
                Button("Sling Speed -",    () => ChangeFloat(config.WebSlingSpeedSave, -5f,   "0.0")),
                Button("Sling Speed +",    () => ChangeFloat(config.WebSlingSpeedSave, 5f,    "0.0")),
                Button("Language english", () => SetLanguage(1), true),
                Button("Language spanish", () => SetLanguage(2), true),
                Button("Language german",  () => SetLanguage(3), true),
                Button("Language russian", () => SetLanguage(4), true),
                Button("Language polish",  () => SetLanguage(5), true),
                Button("Wiggly Gun",       () => config.GunType.Value = 1),
                Button("Straight Gun",     () => config.GunType.Value = 2),
                Button("Coil Gun",         () => config.GunType.Value = 3),
                Button("Lighting Gun",     () => config.GunType.Value = 4),
                Button("Vortex Gun",       () => config.GunType.Value = 5),
                Button("DNA Gun",          () => config.GunType.Value = 6),
                Button("Pulse Gun",        () => config.GunType.Value = 7),
                Button("Orbital Gun",      () => config.GunType.Value = 8),
                Button("Static Gun",       () => config.GunType.Value = 9),
                Button("Sine Wave Gun",    () => config.GunType.Value = 10),
                Button("Digital Gun",      () => config.GunType.Value = 11),
                Button("Square Pulse Gun", () => config.GunType.Value = 12),
                Button("Ray Gun",          () => config.GunType.Value = 13),
                Button("Gun Smoothness -", () => ChangeGunSmoothness(-0.05f)),
                Button("Gun Smoothness +", () => ChangeGunSmoothness(0.05f)),
                Toggle(config.PreviewGun, GunLib.LetGun),
                Toggle(config.IsOneHandedMenu),
                Toggle(config.IsJoystickNavigation),
                Button("Default Colors", ResetColors, true),
                Toggle(config.ShowKyleWhileEmoting),
                Toggle(config.EmoteSounds)
        );

        AddCategory(
                4,
                "Important",
                false,
                Button("Reauthenticate", () => MothershipAuthenticator.Instance.BeginLoginFlow()),
                Toggle(config.IsAntiReportEnabled, Mods.Mods.AntiReport),
                Toggle(config.IsBypassAutoMod,     Mods.Mods.BypassAutomod)
        );

        AddCategory(
                5,
                "Fun",
                false,
                Button("Unlock all cosmetics (CS)", () => Cosmetx.instance.ActivateCosmetx()),
                Button("Max Quest Score",           Mods.Mods.MaxQuestScore),
                Toggle(config.IsBraceletSpam, Mods.Mods.BraceletSpam, Mods.Mods.RemoveBracelet,
                        nameKey: "Bracelet Spam (LG, RG, D?)"),
                Toggle(config.IsEnabledBuilderShelf, Mods.Mods.EnableBuilderShelf, Mods.Mods.DisableBuilderShelf),
                Toggle(config.IsAnnoy,               Mods.Mods.Annoy),
                Button("Unlock Forest Guide", () => Cosmetx.instance.UnlockSpecificCosmetic("LMAPY.")),
                Button("Unlock AA Badge",     () => Cosmetx.instance.UnlockSpecificCosmetic("LBANI.")),
                Toggle(config.IsBoop, () => Mods.Mods.Boop())
        );

        AddCategory(
                6,
                "Player List",
                false
        );

        AddCategory(
                7,
                "Soundboard",
                false
        );

        AddCategory(
                8,
                "Sound",
                false,
                Toggle(config.IsJmanSoundSpam,    TickJmanSoundSpam,    nameKey: "Jman SS"),
                Toggle(config.IsCrystalSoundSpam, TickCrystalSoundSpam, nameKey: "Crystal SS")
        );

        AddCategory(
                9,
                "Visual",
                false,
                Toggle(config.IsBoxEsp,   Mods.Mods.BoxESP,      Mods.Mods.CleanupBoxEsp),
                Toggle(config.IsBallEsp,  Mods.Mods.SkeletonESP, Mods.Mods.DisableSkeletonESP),
                Toggle(config.IsNametags, Mods.Mods.NametagsMod, Mods.Mods.DisableNametagsMod)
        );

        AddCategory(
                10,
                "Emotes",
                false,
                Emote("Dance Moves",     "Dance Moves",                "default"),
                Emote("Take The L",      "TakeTheL",                   "takethel"),
                Emote("Reanimated",      "Reanimated",                 "reanimated"),
                Emote("Electro Shuffle", "ElectroShuffle",             "electroshuffle"),
                Emote("Orange Justice",  "OrangeJustice",              "oj"),
                Emote("Ride The Pony",   "RideThePony",                "ridethepony"),
                Emote("Fresh",           "Emote_Fresh",                "fresh"),
                Emote("Electro Swing",   "ElectroSwing",               "swing"),
                Emote("Floss",           "Emote_FlossDance_CMM",       "floss"),
                Emote("Disco Fever",     "DiscoFever",                 "discofever"),
                Emote("Boogie Down",     "BoogieDownLoop",             "boogiedown"),
                Emote("The Robot",       "Emote_RobotDance",           "therobot"),
                Emote("Best Mates",      "BestMates",                  "bestmates"),
                Emote("Paws & Claws",    "Paws&Claws",                 "pawsclaws"),
                Emote("Get Griddy",      "Get Griddy",                 "Emote_Griddles_Music_Loop_01"),
                Emote("Pull Up",         "Pull Up",                    "Gas_Station_Loop"),
                Emote("Popular Vibe",    "Popular Vibe",               "Emote_SpeedDial_Loop"),
                Emote("Lucid Dreams",    "Lucid DreamsLoop",           "Emote_KelpLinen_Music_Loop"),
                Emote("Empty Pockets",   "Empty Out Your PocketsLoop", "eoyp"),
                Emote("What You Want",   "WhatYouWant",                "whatyouwant"),
                Emote("The Renegade",    "The Renegade",               "Emote_Just_Home_Music_Loop"),
                Emote("Jabba Switchway", "Jabba Switchway Loop",       "Emote_January_Bop_Loop"),
                Emote("Infinite Dab",    "InfinidabLoop",              "infinitedab"),
                Emote("Celebrate Me",    "Celebrate Me",               "IP_Emote_Cottontail_Loop"),
                Emote("Billy Bounce",    "BillyBounce",                "billybounce"),
                Emote("Windmill Floss",  "WindmillFloss",              "whirlfloss"),
                Emote("Hype",            "Hype",                       "hype"),
                Emote("Entranced",       "Entranced",                  "entranced"),
                Emote("Laugh It Up",     "LaughItUp",                  "Emote_Laugh_01"),
                Emote("Snoop Walk",      "SnoopWalk",                  "snoopwalk"),
                Emote("Scenario",        "Scenario",                   "scenario"),
                Emote("Night Out",       "Night Out",                  "nightout"),
                Emote("Point And Strut", "pointandstrut",              "pointandstrut"),
                Emote("Moongazer",       "moongazer",                  "moongazer"),
                Emote("Rollie",          "Rollie",                     "Emote_Twist_Daytona_Music_Loop_01"),
                Emote("Heel Click",      "HEEL",                       "heelclickbreakdown"),
                Emote("Switchstep",      "SwitchStep",                 "switchstep"),
                Emote("Freestylin'",     "Freestylin'",                "freestylin"),
                Emote("Go Mufasa",       "Go Mufasa",                  "Emote_Sandwich_Bop_Loop"),
                Emote("Jubi Slide",      "jubislide",                  "Emote_GoodbyeUpbeat_Loop"),
                Emote("Running Man",     "RunningMan",                 "Athena_Emote_Music_RunningMan"),
                Emote("Zany",            "Zany",                       "zany"),
                Emote("Pumpernickel",    "pumpernickel2",              "Athena_Emotes_Music_PumpDance"),
                Emote("Pony Up",         "RideThePony",                "ponyup"),
                Emote("Hula",            "HULA",                       "emote_hula_01"),
                Emote("Never Gonna",     "Never Gonna Loop",           "Emote_NeverGonna_Loop_01"),
                Emote("Say So",          "Say So",                     "Emote_HotPink_Loop_258"),
                Emote("Take It Slow",    "Takeitslow",                 "takeitslow"),
                Emote("Macarena",        "Macarena",                   "Emote_Macaroon_Music_Loop_01"),
                Emote("Cupid's Arrow",   "cupid",                      "cupid"),
                Emote("Gangnam Style",   "gangnam",                    "gangnam"),
                Emote("Slim Shady",      "realslimshady",              "slim"),
                Emote("Party Hips",      "partyhips",                  "partyhips"),
                Emote("Out West",        "outwest",                    "outwest"),
                Emote("My World",        "myworld",                    "Myworld"),
                Emote("Jake Bug",        "Jake",                       "jake"),
                Emote("Miku Beam",       "miku",                       "miku"),
                Button("Jumpstyle",
                        () => EmoteManager.PlayEmoteFromUrl("Hype",
                                "https://github.com/objectgt/stuff/raw/refs/heads/main/jumping.wav", -1f, true)),
                Button("S33k H3lp",
                        () => EmoteManager.PlayEmoteFromUrl("Say So",
                                "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/femtanyl%20-%20S33K%20H3LP.mp3",
                                -1f, true)),
                Button("Stop All Emotes", EmoteManager.StopEmote)
        );

        AddCategory(
                11,
                "Admin",
                true,
                Toggle(config.IsSilKick,       Mods.Mods.silkickgun, nameKey: "Silent Kick Gun"),
                Toggle(config.AdminLaser,      Mods.Mods.AdminLaser),
                Toggle(config.IsTravis,        Mods.Mods.TravisScott,     Mods.Mods.NoTravis),
                Toggle(config.IsTv,            Mods.Mods.SkidTV,          Mods.Mods.NoTv, nameKey: "Tv"),
                Toggle(config.IsPhone,         Mods.Mods.Samsung,         Mods.Mods.NoSamsung),
                Toggle(config.IsTwerkingCarti, Mods.Mods.TwerkingCarti,   Mods.Mods.NoCarti),
                Toggle(config.IsAdminGrab,     Mods.Mods.AdminGrabAll,    nameKey: "Grab All"),
                Toggle(config.IsCoolSword,     Mods.Mods.Sword,           Mods.Mods.NoSword, nameKey: "Roblox Sword"),
                Toggle(config.IsKormakur,      Mods.Mods.KormakurFemboys, Mods.Mods.NoSign,  nameKey: "Femakur Sign"),
                Video("Vid Hell",  "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/GirlHell1999.mp4"),
                Video("Vid OCD",   "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/OCD.mp4"),
                Video("Vid Kitty", "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/Kitty.mp4"),
                Video("Vid AMV",   "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/testvid.mp4"),
                Video("Vid theresabarrier",
                        "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/theresabarrier.mp4"),
                Video("Vid Edit", "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/edit.mp4"),
                Toggle(config.IsCherryBomb, Mods.Mods.CherryBomb, Mods.Mods.NoCherryBomb, nameKey: "Cherry bomb"),
                Video("Vid Zlothy", "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/Zlothy.mov"),
                Video("Vid Barrier Remix",
                        "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/there%20is%20a%20barrier%20remix.mp4"),
                Video("Vid invincible wobbly edit",
                        "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/INVINCIBLEWOBBLYANIMATION.mp4"),
                Video("Vid Punch Mod", "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/punchmod.mp4"),
                Toggle(config.IsBigAssets),
                Video("Grass Skirt Chase",
                        "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/SB%20Music_%20Grass%20Skirt%20Chase%20(check%20desc).mp4"),
                Video("travis skot rel no clickbate",
                        "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/taviskisuit.mp4"),
                Toggle(config.IsVideoPlayer, Mods.Mods.VideoPlayer, Mods.Mods.NoVideoPlayer),
                Button("Reset Video Player", Mods.Mods.ResetVideoPlayer),
                Toggle(config.IsAdminStrangle, Mods.Mods.AdminStrangle),
                Video("Vid Soup mar brobers",
                        "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/soup%20mar%20brobers.mp4"),
                Toggle(config.IsAdminTitan, Mods.Mods.AdminTitan, Mods.Mods.DisableSizeChanger, nameKey: "Titan")
        );

        hasBuilt = true;
    }

    public static void TickEnabledMods()
    {
        if (ModConfig.instance == null)
            return;

        EnsureBuilt();

        foreach (ModButton button in categories.SelectMany(category => category.Buttons))
            button.Tick();

        Mods.Mods.UpdateCustomProperties();
        Mods.Mods.CreatePlayerOutline();
    }

    public static IReadOnlyList<ModButton> GetButtons(int categoryId, bool isAdmin)
    {
        EnsureBuilt();

        foreach (ModCategory category in categories.Where(category => category.Id == categoryId))
        {
            if (category.AdminOnly && !isAdmin)
                return [];

            return category.Buttons;
        }

        return Array.Empty<ModButton>();
    }

    public static List<ModButton> GetHomeButtons(bool isAdmin, Action<int> openCategory)
    {
        EnsureBuilt();

        List<ModButton> buttons = new()
        {
                Button("Join Discord", () => Application.OpenURL("https://discord.gg/MJRQDVAZZF")),
        };

        buttons.AddRange(from category in categories where !category.AdminOnly || isAdmin let categoryId = category.Id select Button(category.NameKey, () => openCategory.Invoke(categoryId), true));

        return buttons;
    }

    public static int GetHomeButtonCount(bool isAdmin)
    {
        EnsureBuilt();

        return 1 + categories.Count(category => !category.AdminOnly || isAdmin);
    }

    public static int GetPageCount(int buttonCount)
    {
        if (buttonCount <= 0)
            return 1;

        return Mathf.CeilToInt(buttonCount / (float)ButtonsPerPage);
    }

    public static string GetCategoryName(int categoryId)
    {
        EnsureBuilt();

        foreach (ModCategory category in categories)
            if (category.Id == categoryId)
                return Localization.Get(category.NameKey);

        return Localization.Get("Gemstone");
    }

    private static void EnsureBuilt()
    {
        if (hasBuilt)
            return;

        Rebuild();
    }

    private static void AddCategory(int id, string nameKey, bool adminOnly, params ModButton[] buttons) =>
            categories.Add(new ModCategory(id, nameKey, adminOnly, buttons));

    private static ModButton Toggle(
            ConfigEntry<bool> entry,
            Action            tick      = null,
            Action            onDisable = null,
            Action            onEnable  = null,
            string            nameKey   = null
    ) =>
            ModButton.Toggle(entry, tick, onDisable, onEnable, nameKey);

    private static ModButton Button(string nameKey, Action action, bool refreshAfterPress = false) =>
            ModButton.Action(nameKey, action, refreshAfterPress);

    private static ModButton Emote(string nameKey, string emoteName, string soundName) =>
            Button(nameKey, () => EmoteManager.PlayEmote(emoteName, soundName, -1f, true));

    private static ModButton Video(string nameKey, string url) =>
            Button(nameKey, () => Mods.Mods.Video = url);

    private static void TickBees()
    {
        if (Main.beesCoroutine != null)
            return;

        Main.beesCoroutine = Main.instance.StartCoroutine(Mods.Mods.Bees());
    }

    private static void DisableBees()
    {
        if (Main.beesCoroutine != null)
        {
            Main.instance.StopCoroutine(Main.beesCoroutine);
            Main.beesCoroutine = null;
        }

        Mods.Mods.FixRig();
    }

    private static void TickGhostWalk()
    {
        Mods.Mods.GhostWalk(true);
        Mods.Mods.GhostWalk(false);
    }

    private static void TickJmanSoundSpam() =>
            Mods.Mods.SoundSpam(Random.Range(336, 338));

    private static void TickCrystalSoundSpam()
    {
        int[] sounds =
        {
                Random.Range(40,  54),
                Random.Range(214, 221),
        };

        Mods.Mods.SoundSpam(sounds[Random.Range(0, sounds.Length)]);
    }

    private static void IncrementThemeValue(ConfigEntry<float> entry)
    {
        entry.Value = (entry.Value + 1f) % 11f;
        NotiLib.SendNotification(entry.Value.ToString("0"), 2000);
    }

    private static void ChangeFloat(ConfigEntry<float> entry, float amount, string format)
    {
        entry.Value += amount;
        NotiLib.SendNotification(entry.Value.ToString(format), 2000);
    }

    private static void ChangeGunSmoothness(float amount)
    {
        ModConfig.instance.GunSmoothness.Value = Mathf.Max(0f, ModConfig.instance.GunSmoothness.Value + amount);
        NotiLib.SendNotification(ModConfig.instance.GunSmoothness.Value.ToString("0.00"), 2000);
    }

    private static void SetLanguage(int language)
    {
        ModConfig.instance.Language.Value = language;
    }

    private static void ResetColors()
    {
        ModConfig.instance.R.Value = 5f;
        ModConfig.instance.G.Value = 8f;
        ModConfig.instance.B.Value = 10f;
    }
}

public sealed class ModCategory(int id, string nameKey, bool adminOnly, IReadOnlyList<ModButton> buttons)
{
    public int Id { get; } = id;

    public string NameKey { get; } = nameKey;

    public bool AdminOnly { get; } = adminOnly;

    public IReadOnlyList<ModButton> Buttons { get; } = buttons;
}

public sealed class ModButton
{
    private readonly Action       action;
    private readonly Func<string> nameGetter;
    private readonly Action       onDisable;
    private readonly Action       onEnable;
    private readonly Action       tick;

    private ModButton(
            string            nameKey,
            ConfigEntry<bool> toggleEntry,
            Action            action,
            Action            tick,
            Action            onDisable,
            Action            onEnable,
            Func<string>      nameGetter,
            bool              refreshAfterPress
    )
    {
        NameKey           = nameKey;
        ToggleEntry       = toggleEntry;
        this.action       = action;
        this.tick         = tick;
        this.onDisable    = onDisable;
        this.onEnable     = onEnable;
        this.nameGetter   = nameGetter;
        RefreshAfterPress = refreshAfterPress;
    }

    public string NameKey { get; }

    public ConfigEntry<bool> ToggleEntry { get; }

    public bool RefreshAfterPress { get; }

    public string Name => nameGetter != null ? nameGetter.Invoke() : Localization.Get(NameKey);

    public bool IsActive
    {
        get
        {
            if (ToggleEntry == null)
                return false;

            return ToggleEntry.Value;
        }
    }

    public static ModButton Toggle(
            ConfigEntry<bool> entry,
            Action            tick              = null,
            Action            onDisable         = null,
            Action            onEnable          = null,
            string            nameKey           = null,
            bool              refreshAfterPress = true
    )
    {
        string resolvedName = nameKey ?? entry.Definition.Key;

        return new ModButton(
                resolvedName,
                entry,
                null,
                tick,
                onDisable,
                onEnable,
                null,
                refreshAfterPress
        );
    }

    public static ModButton Action(string nameKey, Action action, bool refreshAfterPress = false) =>
            new(
                    nameKey,
                    null,
                    action,
                    null,
                    null,
                    null,
                    null,
                    refreshAfterPress
            );

    public void Press()
    {
        if (ToggleEntry == null)
        {
            action?.Invoke();

            return;
        }

        ToggleEntry.Value = !ToggleEntry.Value;

        if (ToggleEntry.Value)
            onEnable?.Invoke();
        else
            onDisable?.Invoke();
    }

    public void Tick()
    {
        if (ToggleEntry == null || tick == null)
            return;

        if (!ToggleEntry.Value)
            return;

        tick.Invoke();
    }
}