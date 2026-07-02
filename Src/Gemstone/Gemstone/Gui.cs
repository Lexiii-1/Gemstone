using System.Collections;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using Gemstone.Mods.Cosmetx;
using Gemstone.patches;
using GorillaNetworking;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Gemstone.Gemstone;

internal class Gui : MonoBehaviour
{
    private const int       maxEmotePages       = 15;
    private       Vector2   adminScrollPosition = Vector2.zero;
    private       Texture2D buttonActiveTex;
    private       Texture2D buttonHoverTex;
    private       Texture2D buttonNormalTex;
    private       GUIStyle  buttonStyle;
    private       Rect      connectionWindowRect = new(20, 20, 250, 160);

    private int currentGuiTab;

    private int      emotePage            = 0;
    private Vector2  emotesScrollPosition = Vector2.zero;
    private GUIStyle headerStyle;
    private bool     inPlayerSubmenu;
    private GUIStyle labelStyle;

    private bool menuVisible = true;

    private Vector2   modScrollPosition    = Vector2.zero;
    private Rect      modsWindowRect       = new(280, 20, 320, 480);
    private Vector2   playerScrollPosition = Vector2.zero;
    private string    roomToJoin           = "";
    private Player    selectedPlayer;
    private Vector2   soundboardScrollPosition = Vector2.zero;
    private bool      stylesInitialized;
    private Texture2D toggleOffTex;
    private Texture2D toggleOnTex;
    private GUIStyle  toggleStyle;
    private Texture2D windowBackgroundTex;

    private GUIStyle windowStyle;

    private void Update()
    {
        if (UnityInput.Current.GetKeyDown(KeyCode.F11))
            menuVisible = !menuVisible;
    }

    private void OnGUI()
    {
        if (!menuVisible)
            return;

        InitializeStyles();

        Color originalBackgroundColor = GUI.backgroundColor;
        Color originalContentColor    = GUI.contentColor;
        Color originalColor           = GUI.color;

        connectionWindowRect = GUI.Window(
                0,
                connectionWindowRect,
                DrawConnectionWindow,
                "",
                windowStyle
        );

        modsWindowRect = GUI.Window(
                1,
                modsWindowRect,
                DrawModsWindow,
                "",
                windowStyle
        );

        GUI.backgroundColor = originalBackgroundColor;
        GUI.contentColor    = originalContentColor;
        GUI.color           = originalColor;
    }

    private void InitializeStyles()
    {
        if (stylesInitialized) return;

        windowBackgroundTex = CreateSolidColorTexture(new Color(0.08f, 0.08f, 0.12f, 0.92f));
        buttonNormalTex     = CreateSolidColorTexture(new Color(0.15f, 0.15f, 0.22f, 1f));
        buttonHoverTex      = CreateSolidColorTexture(new Color(0.22f, 0.22f, 0.32f, 1f));
        buttonActiveTex     = CreateSolidColorTexture(new Color(0.28f, 0.28f, 0.42f, 1f));
        toggleOnTex         = CreateSolidColorTexture(new Color(0.3f,  0.75f, 0.4f,  1.0f));
        toggleOffTex        = CreateSolidColorTexture(new Color(0.25f, 0.25f, 0.3f,  1.0f));

        windowStyle                     = new GUIStyle(GUI.skin.window);
        windowStyle.normal.background   = windowBackgroundTex;
        windowStyle.onNormal.background = windowBackgroundTex;
        windowStyle.border              = new RectOffset(4,  4,  4,  4);
        windowStyle.padding             = new RectOffset(12, 12, 15, 12);

        buttonStyle                   = new GUIStyle(GUI.skin.button);
        buttonStyle.normal.background = buttonNormalTex;
        buttonStyle.hover.background  = buttonHoverTex;
        buttonStyle.active.background = buttonActiveTex;
        buttonStyle.normal.textColor  = Color.white;
        buttonStyle.hover.textColor   = new Color(0.9f, 0.9f, 1f, 1f);
        buttonStyle.fontStyle         = FontStyle.Bold;
        buttonStyle.alignment         = TextAnchor.MiddleCenter;

        toggleStyle                     = new GUIStyle(GUI.skin.toggle);
        toggleStyle.normal.background   = toggleOffTex;
        toggleStyle.hover.background    = buttonHoverTex;
        toggleStyle.onNormal.background = toggleOnTex;
        toggleStyle.onHover.background  = toggleOnTex;
        toggleStyle.normal.textColor    = Color.white;
        toggleStyle.onNormal.textColor  = Color.white;
        toggleStyle.fontStyle           = FontStyle.Normal;
        toggleStyle.alignment           = TextAnchor.MiddleLeft;
        toggleStyle.padding             = new RectOffset(8, 4, 2, 2);

        labelStyle                  = new GUIStyle(GUI.skin.label);
        labelStyle.normal.textColor = new Color(0.85f, 0.85f, 0.9f, 1f);
        labelStyle.fontStyle        = FontStyle.Normal;

        headerStyle                  = new GUIStyle(GUI.skin.label);
        headerStyle.normal.textColor = new Color(0.4f, 0.7f, 1f, 1f);
        headerStyle.fontStyle        = FontStyle.Bold;
        headerStyle.fontSize         = 13;

        stylesInitialized = true;
    }

    private Texture2D CreateSolidColorTexture(Color color)
    {
        Texture2D texture = new(2, 2);
        Color[]   colors  = new Color[4];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = color;

        texture.SetPixels(colors);
        texture.Apply();

        return texture;
    }

    private void DrawConnectionWindow(int windowID)
    {
        GUI.DragWindow(new Rect(0, 0, 250, 25));
        GUILayout.Space(5);

        AddButton("Disconnect", () =>
                                {
                                    if (PhotonNetwork.InRoom)
                                        PhotonNetwork.Disconnect();
                                });

        GUILayout.Space(6);

        AddButton("Quit", () => { Application.Quit(); });
    }

    private void DrawModsWindow(int windowID)
    {
        GUI.DragWindow(new Rect(0, 0, 320, 25));
        GUILayout.Space(5);

        if (ModConfig.instance == null)
        {
            GUILayout.Label("ModConfig instance missing...", labelStyle);

            return;
        }

        GUILayout.BeginHorizontal();

        bool tab0 = GUILayout.Toggle(currentGuiTab == 0, "Mods", buttonStyle, GUILayout.Height(26));
        if (tab0 && currentGuiTab != 0) currentGuiTab = 0;

        bool tab1 = GUILayout.Toggle(currentGuiTab == 1, "Players", buttonStyle, GUILayout.Height(26));
        if (tab1 && currentGuiTab != 1) currentGuiTab = 1;

        bool tab2 = GUILayout.Toggle(currentGuiTab == 2, "Sounds", buttonStyle, GUILayout.Height(26));
        if (tab2 && currentGuiTab != 2) currentGuiTab = 2;

        bool tab4 = GUILayout.Toggle(currentGuiTab == 4, "Emotes", buttonStyle, GUILayout.Height(26));
        if (tab4 && currentGuiTab != 4) currentGuiTab = 4;

        if (Main.instance != null && Main.instance.IsAdmin)
        {
            bool tab3 = GUILayout.Toggle(currentGuiTab == 3, "Admin", buttonStyle, GUILayout.Height(26));
            if (tab3 && currentGuiTab != 3) currentGuiTab = 3;
        }

        GUILayout.EndHorizontal();
        GUILayout.Space(8);

        switch (currentGuiTab)
        {
            case 0:
                DrawStandardMods();

                break;

            case 1:
                DrawPlayerListMenu();

                break;

            case 2:
                DrawSoundboardMenu();

                break;

            case 4:
                DrawEmotes();

                break;

            case 3:
                if (Main.instance != null && Main.instance.IsAdmin)
                    DrawAdminMenu();
                else
                    currentGuiTab = 0;

                break;
        }
    }

    private void DrawStandardMods()
    {
        modScrollPosition = GUILayout.BeginScrollView(modScrollPosition, GUILayout.Width(300), GUILayout.Height(380));

        GUILayout.Label("Movement Mods", headerStyle, GUILayout.Height(20));
        DrawModToggle(Localization.Get("Speed Boost"), ModConfig.instance.SpeedBoostEnabled.Value,
                ModConfig.instance.SpeedBoostEnabled);

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Fly (A)"), ModConfig.instance.FlyEnabled.Value, ModConfig.instance.FlyEnabled);
        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Platforms (LG, RG)"), ModConfig.instance.IsPlatformsEnabled.Value,
                ModConfig.instance.IsPlatformsEnabled);

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Joystick Fly (LJ, RJ)"), ModConfig.instance.IsJoystickFly.Value,
                ModConfig.instance.IsJoystickFly);

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Long Arms"), ModConfig.instance.LongArmsEnabled.Value,
                ModConfig.instance.LongArmsEnabled,  () => Mods.Mods.UnLongArms());

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Ground Helper (LG + A)"), ModConfig.instance.IsGroundHelper.Value,
                ModConfig.instance.IsGroundHelper);

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Amplified Monke"), ModConfig.instance.IsAmplifiedMonke.Value,
                ModConfig.instance.IsAmplifiedMonke);

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Noclip (B)"), ModConfig.instance.IsNoclipEnabled.Value,
                ModConfig.instance.IsNoclipEnabled);

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Web Slingers (LG, RG)"), ModConfig.instance.IsWebSlingers.Value,
                ModConfig.instance.IsWebSlingers);

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Teleport Gun"), ModConfig.instance.IsTPGun.Value, ModConfig.instance.IsTPGun);
        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Tag Gun (D?)"), ModConfig.instance.IsTagGun.Value, ModConfig.instance.IsTagGun,
                () => Mods.Mods.FixRig());

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Tag All (D?)"), ModConfig.instance.IsTagAll.Value, ModConfig.instance.IsTagAll,
                () => Mods.Mods.FixRig());

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("WASD Fly"), ModConfig.instance.IsWasdFly.Value, ModConfig.instance.IsWasdFly);
        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Movement Recorder (A)"), ModConfig.instance.MovementRecorder.Value,
                ModConfig.instance.MovementRecorder);

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Dash (A, LG)"), ModConfig.instance.Dash.Value, ModConfig.instance.Dash);
        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Size Changer"), ModConfig.instance.IsSizeChanger.Value,
                ModConfig.instance.IsSizeChanger,       () => Mods.Mods.DisableSizeChanger());

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Hand Turn (RG)"), ModConfig.instance.HandTurn.Value,
                ModConfig.instance.HandTurn);

        GUILayout.Space(8);
        GUILayout.Label("Utility Mods", headerStyle, GUILayout.Height(20));
        DrawModToggle(Localization.Get("Get PID Gun"), ModConfig.instance.IsGetPIDGun.Value,
                ModConfig.instance.IsGetPIDGun);

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Mute Gun"), ModConfig.instance.IsMuteGun.Value, ModConfig.instance.IsMuteGun);
        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Mute Others Gun"), ModConfig.instance.IsMuteEveryoneExceptGun.Value,
                ModConfig.instance.IsMuteEveryoneExceptGun);

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Report Gun"), ModConfig.instance.IsReportGun.Value,
                ModConfig.instance.IsReportGun);

        GUILayout.Space(3);
        DrawModButton(Localization.Get("Mute All"), () => Mods.Mods.MuteAll());
        GUILayout.Space(3);
        DrawModButton(Localization.Get("Unmute All"), () => Mods.Mods.UnmuteAll());
        GUILayout.Space(3);
        DrawModButton(Localization.Get("Ignore Far Tag"), () => ExtremelyFarTagPatch.isDetected = false);
        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Show Menu Custom Property"), ModConfig.instance.MenuCustomPropertyEnabled.Value,
                ModConfig.instance.MenuCustomPropertyEnabled);

        GUILayout.Space(8);
        GUILayout.Label("Rig Mods", headerStyle, GUILayout.Height(20));
        DrawModToggle(Localization.Get("Ghost Monke (A)"), ModConfig.instance.IsGhostMonke.Value,
                ModConfig.instance.IsGhostMonke,           () => Mods.Mods.FixRig());

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Lock Rig"), ModConfig.instance.IsLockOntoRig.Value,
                ModConfig.instance.IsLockOntoRig,   () => Mods.Mods.FixRig());

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Hold Rig"), ModConfig.instance.IsHoldRig.Value, ModConfig.instance.IsHoldRig,
                () => Mods.Mods.FixRig());

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Rig Gun"), ModConfig.instance.IsRigGun.Value, ModConfig.instance.IsRigGun,
                () => Mods.Mods.FixRig());

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Freeze Rig (B)"), ModConfig.instance.IsFreezeRig.Value,
                ModConfig.instance.IsFreezeRig,           () => Mods.Mods.FixRig());

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Upside Down Head"), ModConfig.instance.IsUpsideDownHead.Value,
                ModConfig.instance.IsUpsideDownHead,        () => Mods.Mods.FixRig());

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Backwards Head"), ModConfig.instance.IsBackwardsHead.Value,
                ModConfig.instance.IsBackwardsHead,       () => Mods.Mods.FixRig());

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("funny rig"), ModConfig.instance.IsFunnyRig.Value, ModConfig.instance.IsFunnyRig,
                () => Mods.Mods.FixRig());

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Recroom Torso"), ModConfig.instance.IsRecroomTorso.Value,
                ModConfig.instance.IsRecroomTorso,       () => Mods.Mods.FixRig());

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Recroom Rig"), ModConfig.instance.IsRecroomRig.Value,
                ModConfig.instance.IsRecroomRig,       () => Mods.Mods.FixRig());

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Full Body Tracking"), ModConfig.instance.FullBodyTracking.Value,
                ModConfig.instance.FullBodyTracking,          () => Mods.Mods.FixRig());

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Bees"), ModConfig.instance.IsBees.Value, ModConfig.instance.IsBees, () =>
        {
            if (Main.beesCoroutine != null)
            {
                Main.instance.StopCoroutine(Main.beesCoroutine);
                Main.beesCoroutine = null;
            }

            Mods.Mods.FixRig();
        });

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Copy Rig"), ModConfig.instance.IsCopyRigGun.Value,
                ModConfig.instance.IsCopyRigGun,    () => Mods.Mods.FixRig());

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Invis Monke"), ModConfig.instance.IsInvisMonke.Value,
                ModConfig.instance.IsInvisMonke,       () => Mods.Mods.FixRig());

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Spaz Monke"), ModConfig.instance.IsSpazMonke.Value,
                ModConfig.instance.IsSpazMonke,       () => Mods.Mods.FixRig());

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Ragdoll (A)"), ModConfig.instance.IsRagdoll.Value, ModConfig.instance.IsRagdoll,
                () => Mods.Mods.FixRig());

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Spider"), ModConfig.instance.IsSpider.Value, ModConfig.instance.IsSpider,
                () => Mods.Mods.FixRig());

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Inverse Spider"), ModConfig.instance.InverseSpider.Value,
                ModConfig.instance.InverseSpider,         () => Mods.Mods.FixRig());

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Bean"), ModConfig.instance.Bean.Value, ModConfig.instance.Bean,
                () => Mods.Mods.FixRig());

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Joystick Torso Rotation"), ModConfig.instance.JoystickRotation.Value,
                ModConfig.instance.JoystickRotation,               () => Mods.Mods.FixRig());

        GUILayout.Space(8);
        GUILayout.Label("Important & Visuals", headerStyle, GUILayout.Height(20));
        DrawModToggle(Localization.Get("Anti Report"), ModConfig.instance.IsAntiReportEnabled.Value,
                ModConfig.instance.IsAntiReportEnabled);

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Bypass Automod"), ModConfig.instance.IsBypassAutoMod.Value,
                ModConfig.instance.IsBypassAutoMod);

        GUILayout.Space(3);
        DrawModButton(Localization.Get("Reauthenticate"), () => MothershipAuthenticator.Instance.BeginLoginFlow());
        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Box ESP"), ModConfig.instance.IsBoxEsp.Value, ModConfig.instance.IsBoxEsp,
                () => Mods.Mods.CleanupBoxEsp());

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Ball ESP"), ModConfig.instance.IsBallEsp.Value, ModConfig.instance.IsBallEsp,
                () => Mods.Mods.DisableSkeletonESP());

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Nametags"), ModConfig.instance.IsNametags.Value, ModConfig.instance.IsNametags,
                () => Mods.Mods.DisableNametagsMod());

        GUILayout.Space(8);
        GUILayout.Label("Fun Mods", headerStyle, GUILayout.Height(20));
        DrawModButton(Localization.Get("Unlock all cosmetics (CS)"), () => Cosmetx.instance.ActivateCosmetx());
        GUILayout.Space(3);
        DrawModButton(Localization.Get("Max Quest Score"), () => Mods.Mods.MaxQuestScore());
        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Bracelet Spam (LG, RG, D?)"), ModConfig.instance.IsBraceletSpam.Value,
                ModConfig.instance.IsBraceletSpam,                    () => Mods.Mods.RemoveBracelet());

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Enable Builder Shelf (SS)"), ModConfig.instance.IsEnabledBuilderShelf.Value,
                ModConfig.instance.IsEnabledBuilderShelf,            () => Mods.Mods.DisableBuilderShelf());

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Annoy"), ModConfig.instance.IsAnnoy.Value, ModConfig.instance.IsAnnoy);
        GUILayout.Space(3);
        DrawModButton(Localization.Get("Unlock Forest Guide"), () => Cosmetx.instance.UnlockSpecificCosmetic("LMAPY."));
        GUILayout.Space(3);
        DrawModButton(Localization.Get("Unlock AA Badge"), () => Cosmetx.instance.UnlockSpecificCosmetic("LBANI."));

        GUILayout.Space(8);
        GUILayout.Label("Configuration & Settings", headerStyle, GUILayout.Height(20));
        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Menu RGB"), ModConfig.instance.IsMenuRGB.Value, ModConfig.instance.IsMenuRGB);
        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Show Hand Collider"), ModConfig.instance.ShowHandCollider.Value,
                ModConfig.instance.ShowHandCollider);

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Preview Gun"), ModConfig.instance.PreviewGun.Value,
                ModConfig.instance.PreviewGun);

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("One Handed Menu"), ModConfig.instance.IsOneHandedMenu.Value,
                ModConfig.instance.IsOneHandedMenu);

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Joystick Navigation"), ModConfig.instance.IsJoystickNavigation.Value,
                ModConfig.instance.IsJoystickNavigation);

        GUILayout.Space(3);
        DrawModButton(Localization.Get("Default Colors"), () =>
                                                          {
                                                              ModConfig.instance.R.Value = 5;
                                                              ModConfig.instance.G.Value = 8;
                                                              ModConfig.instance.B.Value = 10;
                                                          });

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Show Robot Kyle While Emoting"), ModConfig.instance.ShowKyleWhileEmoting.Value,
                ModConfig.instance.ShowKyleWhileEmoting);

        GUILayout.Space(3);
        DrawModToggle(Localization.Get("Emote Sounds"), ModConfig.instance.EmoteSounds.Value,
                ModConfig.instance.EmoteSounds);

        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        DrawModButton("R +", () =>
                             {
                                 ModConfig.instance.R.Value = (ModConfig.instance.R.Value + 1) % 11;
                                 NotiLib.SendNotification(ModConfig.instance.R.Value.ToString(), 2000);
                             });

        DrawModButton("G +", () =>
                             {
                                 ModConfig.instance.G.Value = (ModConfig.instance.G.Value + 1) % 11;
                                 NotiLib.SendNotification(ModConfig.instance.G.Value.ToString(), 2000);
                             });

        DrawModButton("B +", () =>
                             {
                                 ModConfig.instance.B.Value = (ModConfig.instance.B.Value + 1) % 11;
                                 NotiLib.SendNotification(ModConfig.instance.B.Value.ToString(), 2000);
                             });

        GUILayout.EndHorizontal();

        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        DrawModButton(Localization.Get("Fly Speed +"), () =>
                                                       {
                                                           ModConfig.instance.FlySpeedSave.Value += 0.1f;
                                                           NotiLib.SendNotification(
                                                                   ModConfig.instance.FlySpeedSave.Value
                                                                            .ToString("0.0"), 2000);
                                                       });

        DrawModButton(Localization.Get("Fly Speed -"), () =>
                                                       {
                                                           ModConfig.instance.FlySpeedSave.Value -= 0.1f;
                                                           NotiLib.SendNotification(
                                                                   ModConfig.instance.FlySpeedSave.Value
                                                                            .ToString("0.0"), 2000);
                                                       });

        GUILayout.EndHorizontal();

        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        DrawModButton(Localization.Get("Sling Speed +"), () =>
                                                         {
                                                             ModConfig.instance.WebSlingSpeedSave.Value += 5f;
                                                             NotiLib.SendNotification(
                                                                     ModConfig.instance.WebSlingSpeedSave.Value
                                                                              .ToString("0.0"), 2000);
                                                         });

        DrawModButton(Localization.Get("Sling Speed -"), () =>
                                                         {
                                                             ModConfig.instance.WebSlingSpeedSave.Value -= 5f;
                                                             NotiLib.SendNotification(
                                                                     ModConfig.instance.WebSlingSpeedSave.Value
                                                                              .ToString("0.0"), 2000);
                                                         });

        GUILayout.EndHorizontal();

        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        DrawModButton(Localization.Get("Gun Smoothness +"), () =>
                                                            {
                                                                ModConfig.instance.GunSmoothness.Value += 0.05f;
                                                                NotiLib.SendNotification(
                                                                        ModConfig.instance.GunSmoothness.Value.ToString(
                                                                                "0.00"), 2000);
                                                            });

        DrawModButton(Localization.Get("Gun Smoothness -"), () =>
                                                            {
                                                                ModConfig.instance.GunSmoothness.Value = Mathf.Max(0f,
                                                                        ModConfig.instance.GunSmoothness.Value - 0.05f);

                                                                NotiLib.SendNotification(
                                                                        ModConfig.instance.GunSmoothness.Value.ToString(
                                                                                "0.00"), 2000);
                                                            });

        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        GUILayout.Label("Gun Types", headerStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Wiggly",   buttonStyle, GUILayout.Height(22))) ModConfig.instance.GunType.Value = 1;
        if (GUILayout.Button("Straight", buttonStyle, GUILayout.Height(22))) ModConfig.instance.GunType.Value = 2;
        if (GUILayout.Button("Coil",     buttonStyle, GUILayout.Height(22))) ModConfig.instance.GunType.Value = 3;
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Lightning", buttonStyle, GUILayout.Height(22))) ModConfig.instance.GunType.Value = 4;
        if (GUILayout.Button("Vortex",    buttonStyle, GUILayout.Height(22))) ModConfig.instance.GunType.Value = 5;
        if (GUILayout.Button("DNA",       buttonStyle, GUILayout.Height(22))) ModConfig.instance.GunType.Value = 6;
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Pulse",   buttonStyle, GUILayout.Height(22))) ModConfig.instance.GunType.Value = 7;
        if (GUILayout.Button("Orbital", buttonStyle, GUILayout.Height(22))) ModConfig.instance.GunType.Value = 8;
        if (GUILayout.Button("Static",  buttonStyle, GUILayout.Height(22))) ModConfig.instance.GunType.Value = 9;
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Sine Wave", buttonStyle, GUILayout.Height(22))) ModConfig.instance.GunType.Value = 10;
        if (GUILayout.Button("Digital",   buttonStyle, GUILayout.Height(22))) ModConfig.instance.GunType.Value = 11;
        if (GUILayout.Button("Sq Pulse",  buttonStyle, GUILayout.Height(22))) ModConfig.instance.GunType.Value = 12;
        if (GUILayout.Button("Ray Gun",   buttonStyle, GUILayout.Height(22))) ModConfig.instance.GunType.Value = 13;
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        GUILayout.Label("Language Selection", headerStyle);
        string[] languageNames    = { "None", "English", "Español", "Deutsch", "Русский", "Polski", };
        int      currentLangIndex = Mathf.Clamp(ModConfig.instance.Language.Value, 1, 5);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("<", buttonStyle, GUILayout.Width(40), GUILayout.Height(22)))
        {
            currentLangIndex--;
            if (currentLangIndex < 1) currentLangIndex = 5;
            ModConfig.instance.Language.Value = currentLangIndex;
            Main.instance.Config.Save();
        }

        GUILayout.Box(languageNames[currentLangIndex], buttonStyle, GUILayout.ExpandWidth(true), GUILayout.Height(22));
        if (GUILayout.Button(">", buttonStyle, GUILayout.Width(40), GUILayout.Height(22)))
        {
            currentLangIndex++;
            if (currentLangIndex > 5) currentLangIndex = 1;
            ModConfig.instance.Language.Value = currentLangIndex;
            Main.instance.Config.Save();
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        GUILayout.Label("Room Joiner", headerStyle);
        roomToJoin = GUILayout.TextField(roomToJoin, buttonStyle, GUILayout.Height(22));
        GUILayout.Space(4);
        DrawModButton("Join Room", () =>
                                   {
                                       if (!string.IsNullOrEmpty(roomToJoin))
                                           PhotonNetworkController.Instance.AttemptToJoinSpecificRoom(
                                                   roomToJoin.ToUpper(), JoinType.Solo);
                                   });

        GUILayout.Space(8);
        DrawModButton(Localization.Get("Join Discord"), () => Application.OpenURL("https://discord.gg/MJRQDVAZZF"));

        GUILayout.EndScrollView();
    }

    private void DrawPlayerListMenu()
    {
        playerScrollPosition =
                GUILayout.BeginScrollView(playerScrollPosition, GUILayout.Width(300), GUILayout.Height(380));

        if (!inPlayerSubmenu)
        {
            Player[] players = PhotonNetwork.PlayerList;
            if (players == null || players.Length == 0)
                GUILayout.Label("No Players In Room", labelStyle);
            else
                foreach (Player player_ in players)
                {
                    if (player_ == null) continue;
                    DrawModButton(player_.NickName, () =>
                                                    {
                                                        selectedPlayer  = player_;
                                                        inPlayerSubmenu = true;
                                                    });

                    GUILayout.Space(5);
                }
        }
        else
        {
            if (selectedPlayer == null)
            {
                inPlayerSubmenu = false;
                GUILayout.EndScrollView();

                return;
            }

            GUILayout.Label($"Selected: {selectedPlayer.NickName}", headerStyle);
            GUILayout.Space(5);

            DrawModButton(Localization.Get("Back"), () =>
                                                    {
                                                        inPlayerSubmenu = false;
                                                        selectedPlayer  = null;
                                                    });

            GUILayout.Space(5);

            DrawModButton(Localization.Get("Teleport to"), () =>
                                                           {
                                                               if (selectedPlayer != null && Main.instance != null)
                                                                   Main.instance.StartCoroutine(
                                                                           Mods.Mods.TpToPlayer(selectedPlayer.UserId));
                                                           });

            GUILayout.Space(5);

            DrawModButton(Localization.Get("Custom Properties"), () =>
                                                                 {
                                                                     if (selectedPlayer == null) return;

                                                                     string output =
                                                                             $"Player: {selectedPlayer.NickName}\n\nCustom Properties:\n";

                                                                     if (selectedPlayer.CustomProperties != null &&
                                                                         selectedPlayer.CustomProperties.Count > 0)
                                                                         foreach (DictionaryEntry kvp in selectedPlayer
                                                                                .CustomProperties)
                                                                             output += $"{kvp.Key}: {kvp.Value}\n";
                                                                     else
                                                                         output += "None";

                                                                     NotiLib.SendNotification(output, 4000f);
                                                                 });

            GUILayout.Space(5);

            DrawModButton(Localization.Get("Block Player"), () =>
                                                            {
                                                                if (selectedPlayer == null) return;

                                                                string userId = selectedPlayer.UserId;

                                                                foreach (VRRig? rig in VRRigCache.ActiveRigs)
                                                                {
                                                                    if (rig == null || rig.Creator == null) continue;

                                                                    if (rig.Creator.UserId == userId &&
                                                                        rig.Creator.UserId !=
                                                                        PhotonNetwork.LocalPlayer.UserId)
                                                                        rig.gameObject.SetActive(false);
                                                                }

                                                                NotiLib.SendNotification(
                                                                        $"Blocked {selectedPlayer.NickName}", 2000f);
                                                            });
        }

        GUILayout.EndScrollView();
    }

    private void DrawSoundboardMenu()
    {
        soundboardScrollPosition =
                GUILayout.BeginScrollView(soundboardScrollPosition, GUILayout.Width(300), GUILayout.Height(380));

        DrawModToggle(Localization.Get("Jman SS"), ModConfig.instance.IsJmanSoundSpam.Value,
                ModConfig.instance.IsJmanSoundSpam);

        GUILayout.Space(5);
        DrawModToggle(Localization.Get("Crystal SS"), ModConfig.instance.IsCrystalSoundSpam.Value,
                ModConfig.instance.IsCrystalSoundSpam);

        GUILayout.Space(10);

        BindingFlags reflectionFlags       = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
        FieldInfo?   clipsField            = typeof(Main).GetField("soundboardClips",      reflectionFlags);
        FieldInfo?   currentlyPlayingField = typeof(Main).GetField("currentlyPlayingClip", reflectionFlags);

        List<AudioClip> soundboardClips      = clipsField?.GetValue(Main.instance) as List<AudioClip>;
        AudioClip       currentlyPlayingClip = currentlyPlayingField?.GetValue(null) as AudioClip;

        if (soundboardClips == null || soundboardClips.Count == 0)
            GUILayout.Label("No Loaded Clips Found Inside Directory", labelStyle);
        else
            foreach (AudioClip selectedClip in soundboardClips)
            {
                if (selectedClip == null) continue;
                string btnText = currentlyPlayingClip == selectedClip
                                         ? $"[PLAYING] {selectedClip.name}"
                                         : selectedClip.name;

                DrawModButton(btnText, () => { Main.ToggleSoundboard(selectedClip); });
                GUILayout.Space(5);
            }

        GUILayout.EndScrollView();
    }

    private void DrawEmotes()
    {
        emotesScrollPosition =
                GUILayout.BeginScrollView(emotesScrollPosition, GUILayout.Width(300), GUILayout.Height(380));

        GUILayout.Space(5);

        DrawModButton(Localization.Get("Dance Moves"),
                () => EmoteManager.PlayEmote("Dance Moves", "default", -1f, true));

        DrawModButton(Localization.Get("Take The L"), () => EmoteManager.PlayEmote("TakeTheL", "takethel", -1f, true));
        DrawModButton(Localization.Get("Reanimated"),
                () => EmoteManager.PlayEmote("Reanimated", "reanimated", -1f, true));

        DrawModButton(Localization.Get("Electro Shuffle"),
                () => EmoteManager.PlayEmote("ElectroShuffle", "electroshuffle", -1f, true));

        DrawModButton(Localization.Get("Orange Justice"),
                () => EmoteManager.PlayEmote("OrangeJustice", "oj", -1f, true));

        DrawModButton(Localization.Get("Ride The Pony"),
                () => EmoteManager.PlayEmote("RideThePony", "ridethepony", -1f, true));

        DrawModButton(Localization.Get("Fresh"), () => EmoteManager.PlayEmote("Emote_Fresh", "fresh", -1f, true));
        DrawModButton(Localization.Get("Electro Swing"),
                () => EmoteManager.PlayEmote("ElectroSwing", "swing", -1f, true));

        DrawModButton(Localization.Get("Floss"),
                () => EmoteManager.PlayEmote("Emote_FlossDance_CMM", "floss", -1f, true));

        DrawModButton(Localization.Get("Disco Fever"),
                () => EmoteManager.PlayEmote("DiscoFever", "discofever", -1f, true));

        DrawModButton(Localization.Get("Boogie Down"),
                () => EmoteManager.PlayEmote("BoogieDownLoop", "boogiedown", -1f, true));

        DrawModButton(Localization.Get("The Robot"),
                () => EmoteManager.PlayEmote("Emote_RobotDance", "therobot", -1f, true));

        DrawModButton(Localization.Get("Best Mates"),
                () => EmoteManager.PlayEmote("BestMates", "bestmates", -1f, true));

        DrawModButton(Localization.Get("Paws & Claws"),
                () => EmoteManager.PlayEmote("Paws&Claws", "pawsclaws", -1f, true));

        DrawModButton(Localization.Get("Get Griddy"),
                () => EmoteManager.PlayEmote("Get Griddy", "Emote_Griddles_Music_Loop_01", -1f, true));

        DrawModButton(Localization.Get("Pull Up"),
                () => EmoteManager.PlayEmote("Pull Up", "Gas_Station_Loop", -1f, true));

        DrawModButton(Localization.Get("Popular Vibe"),
                () => EmoteManager.PlayEmote("Popular Vibe", "Emote_SpeedDial_Loop", -1f, true));

        DrawModButton(Localization.Get("Lucid Dreams"),
                () => EmoteManager.PlayEmote("Lucid DreamsLoop", "Emote_KelpLinen_Music_Loop", -1f, true));

        DrawModButton(Localization.Get("Empty Pockets"),
                () => EmoteManager.PlayEmote("Empty Out Your PocketsLoop", "eoyp", -1f, true));

        DrawModButton(Localization.Get("What You Want"),
                () => EmoteManager.PlayEmote("WhatYouWant", "whatyouwant", -1f, true));

        DrawModButton(Localization.Get("The Renegade"),
                () => EmoteManager.PlayEmote("The Renegade", "Emote_Just_Home_Music_Loop", -1f, true));

        DrawModButton(Localization.Get("Jabba Switchway"),
                () => EmoteManager.PlayEmote("Jabba Switchway Loop", "Emote_January_Bop_Loop", -1f, true));

        DrawModButton(Localization.Get("Infinite Dab"),
                () => EmoteManager.PlayEmote("InfinidabLoop", "infinitedab", -1f, true));

        DrawModButton(Localization.Get("Celebrate Me"),
                () => EmoteManager.PlayEmote("Celebrate Me", "IP_Emote_Cottontail_Loop", -1f, true));

        DrawModButton(Localization.Get("Billy Bounce"),
                () => EmoteManager.PlayEmote("BillyBounce", "billybounce", -1f, true));

        DrawModButton(Localization.Get("Windmill Floss"),
                () => EmoteManager.PlayEmote("WindmillFloss", "whirlfloss", -1f, true));

        DrawModButton(Localization.Get("Hype"),      () => EmoteManager.PlayEmote("Hype",      "hype",      -1f, true));
        DrawModButton(Localization.Get("Entranced"), () => EmoteManager.PlayEmote("Entranced", "entranced", -1f, true));

        DrawModButton(Localization.Get("Laugh It Up"),
                () => EmoteManager.PlayEmote("LaughItUp", "Emote_Laugh_01", -1f, true));

        DrawModButton(Localization.Get("Snoop Walk"),
                () => EmoteManager.PlayEmote("SnoopWalk", "snoopwalk", -1f, true));

        DrawModButton(Localization.Get("Scenario"),  () => EmoteManager.PlayEmote("Scenario",  "scenario", -1f, true));
        DrawModButton(Localization.Get("Night Out"), () => EmoteManager.PlayEmote("Night Out", "nightout", -1f, true));

        DrawModButton(Localization.Get("Point And Strut"),
                () => EmoteManager.PlayEmote("pointandstrut", "pointandstrut", -1f, true));

        DrawModButton(Localization.Get("Moongazer"), () => EmoteManager.PlayEmote("moongazer", "moongazer", -1f, true));
        DrawModButton(Localization.Get("Rollie"),
                () => EmoteManager.PlayEmote("Rollie", "Emote_Twist_Daytona_Music_Loop_01", -1f, true));

        DrawModButton(Localization.Get("Heel Click"),
                () => EmoteManager.PlayEmote("HEEL", "heelclickbreakdown", -1f, true));

        DrawModButton(Localization.Get("Switchstep"),
                () => EmoteManager.PlayEmote("SwitchStep", "switchstep", -1f, true));

        DrawModButton(Localization.Get("Freestylin'"),
                () => EmoteManager.PlayEmote("Freestylin'", "freestylin", -1f, true));

        DrawModButton(Localization.Get("Go Mufasa"),
                () => EmoteManager.PlayEmote("Go Mufasa", "Emote_Sandwich_Bop_Loop", -1f, true));

        DrawModButton(Localization.Get("Jubi Slide"),
                () => EmoteManager.PlayEmote("jubislide", "Emote_GoodbyeUpbeat_Loop", -1f, true));

        DrawModButton(Localization.Get("Running Man"),
                () => EmoteManager.PlayEmote("RunningMan", "Athena_Emote_Music_RunningMan", -1f, true));

        DrawModButton(Localization.Get("Zany"), () => EmoteManager.PlayEmote("Zany", "zany", -1f, true));
        DrawModButton(Localization.Get("Pumpernickel"),
                () => EmoteManager.PlayEmote("pumpernickel2", "Athena_Emotes_Music_PumpDance", -1f, true));

        DrawModButton(Localization.Get("Pony Up"), () => EmoteManager.PlayEmote("RideThePony", "ponyup", -1f, true));

        DrawModButton(Localization.Get("Hula"), () => EmoteManager.PlayEmote("HULA", "emote_hula_01", -1f, true));
        DrawModButton(Localization.Get("Never Gonna"),
                () => EmoteManager.PlayEmote("Never Gonna Loop", "Emote_NeverGonna_Loop_01", -1f, true));

        DrawModButton(Localization.Get("Say So"),
                () => EmoteManager.PlayEmote("Say So", "Emote_HotPink_Loop_258", -1f, true));

        DrawModButton(Localization.Get("Take It Slow"),
                () => EmoteManager.PlayEmote("Takeitslow", "takeitslow", -1f, true));

        DrawModButton(Localization.Get("Macarena"),
                () => EmoteManager.PlayEmote("Macarena", "Emote_Macaroon_Music_Loop_01", -1f, true));

        DrawModButton(Localization.Get("Cupid's Arrow"), () => EmoteManager.PlayEmote("cupid", "cupid", -1f, true));
        DrawModButton(Localization.Get("Gangnam Style"), () => EmoteManager.PlayEmote("gangnam", "gangnam", -1f, true));
        DrawModButton(Localization.Get("Slim Shady"), () => EmoteManager.PlayEmote("realslimshady", "slim", -1f, true));

        DrawModButton(Localization.Get("Party Hips"),
                () => EmoteManager.PlayEmote("partyhips", "partyhips", -1f, true));

        DrawModButton(Localization.Get("Out West"), () => EmoteManager.PlayEmote("outwest", "outwest", -1f, true));
        DrawModButton(Localization.Get("My World"), () => EmoteManager.PlayEmote("myworld", "Myworld", -1f, true));
        DrawModButton(Localization.Get("Jake Bug"), () => EmoteManager.PlayEmote("Jake",    "jake",    -1f, true));

        DrawModButton(Localization.Get("Miku Beam"), () => EmoteManager.PlayEmote("miku", "miku", -1f, true));
        DrawModButton(Localization.Get("Jumpstyle"),
                () => EmoteManager.PlayEmoteFromUrl("Hype",
                        "https://github.com/objectgt/stuff/raw/refs/heads/main/jumping.wav", -1f, true));

        DrawModButton(Localization.Get("S33k H3lp"),
                () => EmoteManager.PlayEmoteFromUrl("Say So",
                        "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/femtanyl%20-%20S33K%20H3LP.mp3", -1f,
                        true));

        GUILayout.Space(10);
        DrawModButton(Localization.Get("Stop All Emotes"), () => EmoteManager.StopEmote());

        GUILayout.Space(5);
        GUILayout.Label(Localization.Get("all emotes added"), buttonStyle);

        GUILayout.EndScrollView();
    }

    private void DrawAdminMenu()
    {
        adminScrollPosition =
                GUILayout.BeginScrollView(adminScrollPosition, GUILayout.Width(300), GUILayout.Height(380));

        DrawModToggle(Localization.Get("Silent Kick Gun"), ModConfig.instance.IsSilKick.Value,
                ModConfig.instance.IsSilKick);

        GUILayout.Space(5);
        DrawModToggle(Localization.Get("Admin Laser"), ModConfig.instance.AdminLaser.Value,
                ModConfig.instance.AdminLaser);

        GUILayout.Space(5);
        DrawModToggle(Localization.Get("Travis Scott"), ModConfig.instance.IsTravis.Value, ModConfig.instance.IsTravis,
                () => Mods.Mods.NoTravis());

        GUILayout.Space(5);
        DrawModToggle(Localization.Get("Tv"), ModConfig.instance.IsTv.Value, ModConfig.instance.IsTv,
                () => Mods.Mods.NoTv());

        GUILayout.Space(5);
        DrawModToggle(Localization.Get("Phone"), ModConfig.instance.IsPhone.Value, ModConfig.instance.IsPhone,
                () => Mods.Mods.NoSamsung());

        GUILayout.Space(5);
        DrawModToggle(Localization.Get("Twerking Carti"), ModConfig.instance.IsTwerkingCarti.Value,
                ModConfig.instance.IsTwerkingCarti,       () => Mods.Mods.NoCarti());

        GUILayout.Space(5);
        DrawModToggle(Localization.Get("Grab All"), ModConfig.instance.IsAdminGrab.Value,
                ModConfig.instance.IsAdminGrab);

        GUILayout.Space(5);
        DrawModToggle(Localization.Get("Roblox Sword"), ModConfig.instance.IsCoolSword.Value,
                ModConfig.instance.IsCoolSword,         () => Mods.Mods.NoSword());

        GUILayout.Space(5);
        DrawModToggle(Localization.Get("Kormakur Sign"), ModConfig.instance.IsKormakur.Value,
                ModConfig.instance.IsKormakur,           () => Mods.Mods.NoSign());

        GUILayout.Space(10);
        GUILayout.Label("Videos", headerStyle);
        DrawModButton(Localization.Get("Vid Hell"),
                () => Mods.Mods.Video = "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/GirlHell1999.mp4");

        GUILayout.Space(5);
        DrawModButton(Localization.Get("Vid OCD"),
                () => Mods.Mods.Video = "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/OCD.mp4");

        GUILayout.Space(5);
        DrawModButton(Localization.Get("Vid Kitty"),
                () => Mods.Mods.Video = "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/Kitty.mp4");

        GUILayout.Space(5);
        DrawModButton(Localization.Get("Vid AMV"),
                () => Mods.Mods.Video = "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/testvid.mp4");

        GUILayout.Space(5);
        DrawModButton(Localization.Get("Vid theresabarrier"),
                () => Mods.Mods.Video = "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/theresabarrier.mp4");

        GUILayout.Space(5);
        DrawModButton(Localization.Get("Vid Edit"),
                () => Mods.Mods.Video = "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/edit.mp4");

        GUILayout.Space(5);
        DrawModButton(Localization.Get("Vid Zlothy"),
                () => Mods.Mods.Video = "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/Zlothy.mov");

        GUILayout.Space(5);
        DrawModButton(Localization.Get("Vid Barrier Remix"),
                () => Mods.Mods.Video =
                              "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/there%20is%20a%20barrier%20remix.mp4");

        GUILayout.Space(5);
        DrawModButton(Localization.Get("Vid invincible wobbly edit"),
                () => Mods.Mods.Video =
                              "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/INVINCIBLEWOBBLYANIMATION.mp4");

        GUILayout.Space(5);
        DrawModButton(Localization.Get("Vid Punch Mod"),
                () => Mods.Mods.Video = "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/punchmod.mp4");

        GUILayout.Space(5);
        DrawModButton(Localization.Get("Grass Skirt Chase"),
                () => Mods.Mods.Video =
                              "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/SB%20Music_%20Grass%20Skirt%20Chase%20(check%20desc).mp4");

        GUILayout.Space(5);
        DrawModButton(Localization.Get("travis skot rel no clickbate"),
                () => Mods.Mods.Video = "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/taviskisuit.mp4");

        GUILayout.Space(5);
        DrawModButton(Localization.Get("Vid Soup mar brobers"),
                () => Mods.Mods.Video =
                              "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/soup%20mar%20brobers.mp4");

        GUILayout.Space(10);
        DrawModToggle(Localization.Get("Cherry bomb"), ModConfig.instance.IsCherryBomb.Value,
                ModConfig.instance.IsCherryBomb,       () => Mods.Mods.NoCherryBomb());

        GUILayout.Space(5);
        DrawModToggle(Localization.Get("Big Assets"), ModConfig.instance.IsBigAssets.Value,
                ModConfig.instance.IsBigAssets);

        GUILayout.Space(5);
        DrawModToggle(Localization.Get("Video Player"), ModConfig.instance.IsVideoPlayer.Value,
                ModConfig.instance.IsVideoPlayer,       () => Mods.Mods.NoVideoPlayer());

        GUILayout.Space(5);
        DrawModButton(Localization.Get("Reset Video Player"), () => Mods.Mods.ResetVideoPlayer());
        GUILayout.Space(5);
        DrawModToggle(Localization.Get("Admin Strangle"), ModConfig.instance.IsAdminStrangle.Value,
                ModConfig.instance.IsAdminStrangle);

        GUILayout.Space(5);
        DrawModToggle(Localization.Get("Titan"), ModConfig.instance.IsAdminTitan.Value, ModConfig.instance.IsAdminTitan,
                () => Mods.Mods.DisableSizeChanger());

        GUILayout.EndScrollView();
    }

    private void AddButton(string label, Action onClickAction)
    {
        if (GUILayout.Button(label, buttonStyle, GUILayout.Height(30)))
        {
            onClickAction?.Invoke();

            if (Main.instance != null)
                Main.instance.PlayClickSound();
        }
    }

    private void DrawModButton(string title, Action onPressedAction)
    {
        if (GUILayout.Button(title, buttonStyle, GUILayout.Height(25)))
        {
            onPressedAction?.Invoke();

            if (Main.instance != null)
                Main.instance.PlayClickSound();
        }
    }

    private void DrawModToggle(string title, bool enabled, ConfigEntry<bool> configEntry, Action onDisable = null)
    {
        if (configEntry == null) return;

        bool newState = GUILayout.Toggle(enabled, $" {title}", toggleStyle, GUILayout.Height(22));

        if (newState != enabled)
        {
            configEntry.Value = newState;

            if (Main.instance != null)
            {
                Main.instance.Config.Save();
                Main.instance.PlayClickSound();
            }

            if (!newState)
                onDisable?.Invoke();
        }
    }
}