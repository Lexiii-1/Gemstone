using System.Collections;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using Gemstone.Console;
using Gemstone.Mods.Cosmetx;
using GorillaLocomotion;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Gemstone.Gemstone;

[BepInPlugin(Constants.GUID, Constants.Name, Constants.Version)]
public class Main : BaseUnityPlugin
{
    public static int Pages;

    private static Coroutine activeSoundCoroutine;
    private static AudioClip currentlyPlayingClip;
    public static  Coroutine beesCoroutine;
    public         bool      isMenuCreated;

    public int selectedButtonIndex;

    public GameObject       HandMenuCollider;
    public GameObject       HandMenuCollider2;
    public List<GameObject> btnObjs = new();
    public float            globalClickCooldown;
    public int              currentCategoryIndex = -1;
    public int              currentPageIndex;
    public bool             searchActive;
    public bool             searchResultsMode;
    public string           searchText = "";

    private static readonly float[] searchKeyColumns = { -0.0146f, -0.00876f, -0.00292f, 0.00292f, 0.00876f, 0.0146f };
    private static readonly float[] searchKeyRows    = { 0.006f, 0.0028f, -0.0004f, -0.0036f, -0.0068f };

    private static readonly string[][] searchKeyboardViews =
    {
            new[]
            {
                    "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R",
                    "S", "T", "U", "V", "W", "X", "Y", "Z", "DEL", "SPC", "CLR", "GO",
            },
    };

    private int      searchReturnCategory = -1;
    private int      searchReturnPage;
    private Coroutine searchButtonAnimation;
    private bool     searchButtonPopPending;
    private static Material searchIconMaterial;
    private static Texture2D searchIconTexture;

    public           AudioSource audioSource;
    public           bool        IsAdmin;
    private readonly Vector3     menuForwardOffset = new(0.08f, 0f, 0f);

    private readonly List<AudioClip> soundboardClips     = new();
    private readonly List<string>    soundboardFileNames = new();
    private          bool            buttonWasPressed;
    private          AudioClip       cachedClip;
    private          bool            inPlayerSubmenu;
    private          float           joystickScrollCooldown;
    private          bool            lastRigState;
    private          bool            leftJoystickReset = true;
    private          AssetBundle     menuBundle;
    private          GameObject      menuObj;

    private       bool       menuOpen;
    private       GameObject menuPrefab;
    private       Coroutine  rgbCoroutine;
    private       bool       rightJoystickReset = true;
    private       Player     selectedPlayer;
    private       bool       soundReady;
    public static Main       instance       { get; private set; }
    public static bool       IsVRRIGEnabled => VRRig.LocalRig.enabled != null ? VRRig.LocalRig.enabled : false;

    private void Awake()
    {
        LoadMenuAssetBundle();
        instance = this;
        string dirPath = Path.Combine(Paths.GameRootPath, "Gemstone");
        Directory.CreateDirectory(dirPath);

        Harmony harmony = new(Constants.GUID);
        harmony.PatchAll();

        audioSource = gameObject.GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        LoadSound();
        StartCoroutine(LoadSoundboardFiles());
    }

    private void Start()
    {
        Mods.Mods.FetchRemoteMOTD("https://raw.githubusercontent.com/Lexiii-1/Gemstone/refs/heads/main/LiveMSG");
        gameObject.AddComponent<GunLib>();
        gameObject.AddComponent<ModConfig>();
        gameObject.AddComponent<ModBackend>();
        gameObject.AddComponent<Cosmetx>();
        gameObject.AddComponent<InfoNotifs>();
        gameObject.AddComponent<Gui>();
        gameObject.AddComponent<NowPlaying>();
        gameObject.AddComponent<ColoredBoards>();
        gameObject.AddComponent<EmoteManager>();
        GameObject arsobj = new("ARS");
        DontDestroyOnLoad(arsobj);
        arsobj.AddComponent<ARS.ARS>();
        if (NotiLib.Instance == null)
        {
            GameObject notiObj = new("NotiLib");
            DontDestroyOnLoad(notiObj);
            notiObj.AddComponent<NotiLib>();
        }

        Console.Console.LoadConsole();
    }

    private void Update()
    {
        if (menuOpen && ModConfig.instance != null && ModConfig.instance.IsJoystickNavigation.Value &&
            ControllerInputPoller.instance != null)
        {
            Vector2 joyl    = ControllerInputPoller.instance.leftControllerPrimary2DAxis;
            Vector2 joyr    = ControllerInputPoller.instance.rightControllerPrimary2DAxis;
            bool    trigger = ControllerInputPoller.instance.rightControllerTriggerButton;
            bool    B       = ControllerInputPoller.instance.rightControllerSecondaryButton;

            if (joystickScrollCooldown > 0f)
                joystickScrollCooldown -= Time.deltaTime;

            if (Mathf.Abs(joyl.y) < 0.5f)
                leftJoystickReset = true;

            if (Mathf.Abs(joyr.x) < 0.5f)
                rightJoystickReset = true;

            if (joystickScrollCooldown <= 0f && btnObjs.Count > 0)
            {
                if (joyl.y > 0.5f && leftJoystickReset)
                {
                    selectedButtonIndex--;
                    if (selectedButtonIndex < 0) selectedButtonIndex = btnObjs.Count - 1;
                    joystickScrollCooldown = 0.1f;
                    leftJoystickReset      = false;
                    RefreshMenu();
                }
                else if (joyl.y < -0.5f && leftJoystickReset)
                {
                    selectedButtonIndex++;
                    if (selectedButtonIndex >= btnObjs.Count) selectedButtonIndex = 0;
                    joystickScrollCooldown = 0.1f;
                    leftJoystickReset      = false;
                    RefreshMenu();
                }
            }

            if (joystickScrollCooldown <= 0f)
            {
                if (joyr.x > 0.5f && rightJoystickReset)
                {
                    currentPageIndex       = Mathf.Min(Pages - 1, currentPageIndex + 1);
                    selectedButtonIndex    = 0;
                    joystickScrollCooldown = 0.1f;
                    rightJoystickReset     = false;
                    RefreshMenu();
                }
                else if (joyr.x < -0.5f && rightJoystickReset)
                {
                    currentPageIndex       = Mathf.Max(0, currentPageIndex - 1);
                    selectedButtonIndex    = 0;
                    joystickScrollCooldown = 0.1f;
                    rightJoystickReset     = false;
                    RefreshMenu();
                }
            }

            if (B && globalClickCooldown <= 0f)
            {
                if (searchActive) CloseSearch();
                else SwitchPage(-1, 0);
            }
            else if (trigger && globalClickCooldown <= 0f && selectedButtonIndex >= 0 &&
                     selectedButtonIndex            < btnObjs.Count)
            {
                GameObject? targetBtn = btnObjs[selectedButtonIndex];
                if (targetBtn != null)
                {
                    ButtonCollider? bc = targetBtn.GetComponent<ButtonCollider>();
                    if (bc != null)
                        bc.Press();
                }
            }
        }

        IReadOnlyList<VRRig>? activeRigs = VRRigCache.ActiveRigs;

        if (activeRigs != null)
        foreach (VRRig rig in activeRigs)
            if (rig != null && rig.Creator != null)
            {
                Color gemstoneColorStart = new(0.6f, 0f, 1f);
                Color gemstoneColorEnd   = new(0f, 1f, 1f);

                Color  arsColorStart  = new(1f, 0f, 0f);
                Color  arsColorEnd    = new(0f, 0f, 0f);
                string arsBaseTag     = "[ON ARS]";
                string arsGradientTag = " ";
                for (int i = 0; i < arsBaseTag.Length; i++)
                {
                    float  t            = (float)i / (arsBaseTag.Length - 1);
                    Color  currentColor = Color.Lerp(arsColorStart, arsColorEnd, t);
                    string hexColor     = ColorUtility.ToHtmlStringRGB(currentColor);
                    arsGradientTag += $"<color=#{hexColor}>{arsBaseTag[i]}</color>";
                }

                string gemstoneBaseTag     = "[Gemstone User]";
                string gemstoneGradientTag = " ";
                for (int i = 0; i < gemstoneBaseTag.Length; i++)
                {
                    float  t            = (float)i / (gemstoneBaseTag.Length - 1);
                    Color  currentColor = Color.Lerp(gemstoneColorStart, gemstoneColorEnd, t);
                    string hexColor     = ColorUtility.ToHtmlStringRGB(currentColor);

                    gemstoneGradientTag += $"<color=#{hexColor}>{gemstoneBaseTag[i]}</color>";
                }

                string ownerBaseTag     = "[Gemstone Owner]";
                string ownerGradientTag = " ";
                for (int i = 0; i < ownerBaseTag.Length; i++)
                {
                    float  t            = (float)i / (ownerBaseTag.Length - 1);
                    Color  currentColor = Color.Lerp(gemstoneColorStart, gemstoneColorEnd, t);
                    string hexColor     = ColorUtility.ToHtmlStringRGB(currentColor);

                    ownerGradientTag += $"<color=#{hexColor}>{ownerBaseTag[i]}</color>";
                }

                Color chudColorStart = new(0.5f, 0f, 0f);
                Color chudColorEnd   = new(0.2f, 0.2f, 0.2f);

                string chudBaseTag     = "[Chud Menu User]";
                string chudGradientTag = " ";
                for (int i = 0; i < chudBaseTag.Length; i++)
                {
                    float  t            = (float)i / (chudBaseTag.Length - 1);
                    Color  currentColor = Color.Lerp(chudColorStart, chudColorEnd, t);
                    string hexColor     = ColorUtility.ToHtmlStringRGB(currentColor);

                    chudGradientTag += $"<color=#{hexColor}>{chudBaseTag[i]}</color>";
                }

                string playerColorHex = "FFFFFF";
                if (rig.mainSkin != null && rig.mainSkin.sharedMaterial != null)
                    playerColorHex = ColorUtility.ToHtmlStringRGB(rig.mainSkin.sharedMaterial.color);

                string rawName = rig.Creator.NickName;
                if (string.IsNullOrEmpty(rawName))
                    rawName = "Player";

                string fpsStr = GetFPS(rig);

                string platformStr      = GetPlatform(rig);
                string platformColorHex = "FFFFFF";
                if (string.IsNullOrEmpty(platformStr) || platformStr == "?")
                {
                    platformStr      = "Unknown";
                    platformColorHex = "FFFFFF";
                }
                else
                {
                    string lowerPlatform = platformStr.ToLower();
                    if (lowerPlatform.Contains("link") || lowerPlatform.Contains("oculus quest link"))
                        platformColorHex = "FFFF00";
                    else if (lowerPlatform.Contains("standalone") || lowerPlatform.Contains("quest"))
                        platformColorHex = "87CEEB";
                    else if (lowerPlatform.Contains("steam"))
                        platformColorHex = "0000FF";
                }

                string cosmeticResult = HasSpecialCosmetic(rig);
                string cosmeticTag    = "";
                if (cosmeticResult != "False")
                    cosmeticTag = $" [{cosmeticResult}]";

                string baseFormattedPrefix =
                        $"[{rig.Creator.UserId}] <color=#{playerColorHex}>{rawName}</color> [{fpsStr}] [<color=#{platformColorHex}>{platformStr}</color>]{cosmeticTag}";

                bool isLexi = rig.Creator.UserId != null                                                        &&
                              ServerData.Administrators.TryGetValue(rig.Creator.UserId, out string consoleName) &&
                              consoleName.Equals("Lexi", StringComparison.OrdinalIgnoreCase)
                           || rig.isLocal && PhotonNetwork.LocalPlayer != null &&
                              PhotonNetwork.LocalPlayer.UserId != null                                   &&
                              ServerData.Administrators.TryGetValue(PhotonNetwork.LocalPlayer.UserId,
                                      out string localConsoleName) &&
                              localConsoleName.Equals("Lexi", StringComparison.OrdinalIgnoreCase);

                if (rig.isLocal || rig.Creator.IsLocal)
                {
                    if (rig.playerText1 != null)
                    {
                        if (isLexi)
                            rig.playerText1.text = baseFormattedPrefix + ownerGradientTag;
                        else if (ModConfig.instance != null && ModConfig.instance.MenuCustomPropertyEnabled.Value)
                            rig.playerText1.text = baseFormattedPrefix + gemstoneGradientTag;
                        else
                            rig.playerText1.text = baseFormattedPrefix;
                    }

                    continue;
                }

                if (rig.Creator.GetPlayerRef() != null)
                {
                    Hashtable? properties                = rig.Creator.GetPlayerRef().CustomProperties;
                    bool       hasActiveGemstoneProperty = false;
                    bool       hasActiveChudProperty     = false;
                    if (properties != null && properties.Count > 0)
                        foreach (object? keyObj in properties.Keys)
                            if (keyObj != null)
                            {
                                string key = keyObj.ToString();
                                if (key.Contains("Gemstone."))
                                {
                                    if (properties[keyObj] is bool isGemstone && isGemstone)
                                        hasActiveGemstoneProperty = true;
                                    else if (properties[keyObj] is int intGemstone && intGemstone == 1)
                                        hasActiveGemstoneProperty = true;
                                }
                                else if (key.Contains("Chud"))
                                {
                                    if (properties[keyObj] is bool isChud && isChud)
                                        hasActiveChudProperty = true;
                                    else if (properties[keyObj] is int intChud && intChud == 1)
                                        hasActiveChudProperty = true;
                                }
                            }

                    string finalTags = "";
                    if (isLexi)
                    {
                        finalTags += ownerGradientTag;
                    }
                    else
                    {
                        if (hasActiveGemstoneProperty)
                            finalTags += gemstoneGradientTag;
                    }

                    if (hasActiveChudProperty)
                        finalTags += chudGradientTag;

                    if (ARS.ARS.NeedToReport(rig.Creator.GetPlayerRef()))
                        finalTags += arsGradientTag;

                    if (rig.playerText1 != null)
                        rig.playerText1.text = baseFormattedPrefix + finalTags;
                }
            }

        if (!searchActive)
        {
            if (UnityInput.Current.GetKey(KeyCode.Z))
                ControllerInputPoller.instance.rightControllerPrimaryButton = true;
            if (UnityInput.Current.GetKey(KeyCode.X))
                ControllerInputPoller.instance.rightControllerSecondaryButton = true;
            if (UnityInput.Current.GetKey(KeyCode.C))
                ControllerInputPoller.instance.leftControllerPrimaryButton = true;
            if (UnityInput.Current.GetKey(KeyCode.V))
                ControllerInputPoller.instance.leftControllerSecondaryButton = true;

            if (UnityInput.Current.GetKey(KeyCode.LeftControl))
                ControllerInputPoller.instance.leftControllerTriggerButton = true;

            if (UnityInput.Current.GetKey(KeyCode.LeftAlt)) ControllerInputPoller.instance.leftGrab = true;
            if (UnityInput.Current.GetKey(KeyCode.RightControl))
                ControllerInputPoller.instance.rightControllerTriggerButton = true;

            if (UnityInput.Current.GetKey(KeyCode.RightAlt)) ControllerInputPoller.instance.rightGrab = true;
        }

        string roomName = PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null
                                  ? PhotonNetwork.CurrentRoom.Name
                                  : "Not In Room";

        string playerCount = PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null
                                     ? PhotonNetwork.CurrentRoom.PlayerCount.ToString()
                                     : "0";

        Mods.Mods.UpdateMOTDText(

$"Welcome To Gemstone Version: {Constants.Version}!" + (Constants.Debug ? "\n\n\n\n\n\n\n\nDEBUG BUILD" : ""),

$"Welcome to gemstone! This Menu Mas A Few Fun Mods Made Just For You!\n\n\nIf You Get Banned It Is Not I, The Developers Responsibility. \n\n\nCurrent Room: {roomName}\nPlayers: {playerCount}\n\nLive Message: {Mods.Mods.remoteText}");

        if (globalClickCooldown > 0) globalClickCooldown -= Time.deltaTime;
        bool isButtonPressed                             = ControllerInputPoller.instance.leftControllerSecondaryButton;

        if (isButtonPressed && !buttonWasPressed)
        {
            menuOpen = !menuOpen;

            if (menuOpen)
            {
                searchButtonPopPending = true;
                CreateMenu();
            }
            else
            {
                searchActive      = false;
                searchResultsMode = false;
                searchText        = "";
                DestroyMenu(false);
            }
        }

        buttonWasPressed = isButtonPressed;

        if (menuOpen && searchActive) HandleSearchTyping();

        if (menuOpen)
        {
            bool currentRigState = IsVRRIGEnabled;

            if (menuObj == null || currentRigState != lastRigState)
            {
                DestroyMenu(true);
                CreateMenu();
            }

            lastRigState = currentRigState;
        }

        if (menuObj != null)

            if (menuObj != null)
                if (menuObj != null)
                {
                    Vector3 parentScale = GTPlayer.Instance.LeftHand.controllerTransform.lossyScale;

                    Vector3 desired = new(1.24208f, 13.04792f, 15.86129f);

                    menuObj.transform.localScale = new Vector3(
                            desired.x / parentScale.x,
                            desired.y / parentScale.y,
                            desired.z / parentScale.z
                    );
                }
    }

    private void LoadMenuAssetBundle()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();

        string resourceName = "Gemstone.EmbedResources.menuobject";

        if (menuBundle == null)
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Logger.LogError("rsrc not found");

                    return;
                }

                using (MemoryStream ms = new())
                {
                    stream.CopyTo(ms);
                    menuBundle = AssetBundle.LoadFromMemory(ms.ToArray());
                }
            }

        if (menuBundle == null)
        {
            Logger.LogError("asset bundle failed to load");

            return;
        }

        menuPrefab = menuBundle.LoadAsset<GameObject>("Assets/MenuObject.prefab");

        if (menuPrefab == null)
            Logger.LogError("prefab not found");
    }

    private void LoadSound()
    {
        try
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            string resourceName = "Gemstone.EmbedResources.buttonpress.ogg";

            using Stream stream = assembly.GetManifestResourceStream(resourceName);

            if (stream == null)
            {
                Logger.LogError("sound rsrc not found");

                return;
            }

            byte[] data = new byte[stream.Length];
            stream.Read(data, 0, data.Length);

            AudioClip? tempClip = WavOrOggToAudioClip(data);

            if (tempClip != null)
            {
                cachedClip = tempClip;
                soundReady = true;
            }
        }
        catch (Exception e)
        {
            Logger.LogError("failed loading embedded sound " + e);
        }
    }

    private AudioClip WavOrOggToAudioClip(byte[] data)
    {
        string tempPath = Path.Combine(Path.GetTempPath(), "Gemstone_click.ogg");
        File.WriteAllBytes(tempPath, data);

        using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + tempPath, AudioType.OGGVORBIS);
        UnityWebRequestAsyncOperation? op = www.SendWebRequest();

        while (!op.isDone) { }

        if (www.result != UnityWebRequest.Result.Success)
        {
            Logger.LogError(www.error);

            return null;
        }

        return DownloadHandlerAudioClip.GetContent(www);
    }

    public void EnableAdminMenu()
    {
        IsAdmin = true;
        if (isMenuCreated) RefreshMenu();
    }

    public static void ToggleSoundboard(AudioClip sound)
    {
        if (currentlyPlayingClip == sound)
        {
            StopActiveSound();

            return;
        }

        if (activeSoundCoroutine != null)
            StopActiveSound();

        activeSoundCoroutine = instance.StartCoroutine(PlaySoundMicrophone(sound));
    }

    private static void StopActiveSound()
    {
        if (activeSoundCoroutine != null)
        {
            instance.StopCoroutine(activeSoundCoroutine);
            activeSoundCoroutine = null;
        }

        currentlyPlayingClip = null;

        Recorder? recorder = NetworkSystem.Instance.VoiceConnection.PrimaryRecorder;
        recorder.SourceType = Recorder.InputSourceType.Microphone;
        recorder.AudioClip  = null;
        recorder.RestartRecording(true);
        recorder.DebugEchoMode = false;
    }

    public static IEnumerator PlaySoundMicrophone(AudioClip sound)
    {
        if (sound == null) yield break;

        currentlyPlayingClip = sound;

        NetworkSystem.Instance.VoiceConnection.PrimaryRecorder.SourceType = Recorder.InputSourceType.AudioClip;
        NetworkSystem.Instance.VoiceConnection.PrimaryRecorder.AudioClip  = sound;
        NetworkSystem.Instance.VoiceConnection.PrimaryRecorder.RestartRecording(true);
        NetworkSystem.Instance.VoiceConnection.PrimaryRecorder.DebugEchoMode = true;

        yield return new WaitForSeconds(sound.length + 0.1f);

        StopActiveSound();

        if (instance.menuOpen && instance.isMenuCreated)
            instance.RefreshMenu();
    }

    private IEnumerator LoadSoundboardFiles()
    {
        soundboardClips.Clear();
        soundboardFileNames.Clear();

        string soundPath = Path.Combine(Paths.GameRootPath, "Gemstone", "Sounds");
        if (!Directory.Exists(soundPath))
        {
            Directory.CreateDirectory(soundPath);

            yield break;
        }

        List<string> files = new();
        files.AddRange(Directory.GetFiles(soundPath, "*.ogg"));
        files.AddRange(Directory.GetFiles(soundPath, "*.mp3"));

        foreach (string file in files)
        {
            AudioType type = file.EndsWith(".mp3") ? AudioType.MPEG : AudioType.OGGVORBIS;

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + file, type))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    clip.name = Path.GetFileNameWithoutExtension(file);
                    soundboardClips.Add(clip);
                    soundboardFileNames.Add(clip.name);
                }
                else
                {
                    Logger.LogError($"Failed to load sound {file}: {www.error}");
                }
            }
        }
    }

    public static string GetPlatform(VRRig Player)
    {
        if (Player == null || Player.Creator == null || Player.Creator.GetPlayerRef() == null)
            return "?";

        int customPropsCount = 0;
        if (Player.Creator.GetPlayerRef().CustomProperties != null)
            customPropsCount = Player.Creator.GetPlayerRef().CustomProperties.Count;

        FieldInfo? cosmeticsField = AccessTools.Field(Player.GetType(), "_playerOwnedCosmetics");

        if (cosmeticsField == null)
            return "?";

        object? cosmeticsObj = cosmeticsField.GetValue(Player);
        string  concat       = "";

        if (cosmeticsObj is HashSet<string> cosmeticSet)
            concat = string.Join("", cosmeticSet);

        if (concat.Contains("S. FIRST LOGIN"))
            return "Steam";

        if (concat.Contains("FIRST LOGIN") || customPropsCount >= 2)
            return "Oculus Quest Link";

        return "Standalone";
    }

    public static string HasSpecialCosmetic(VRRig Player)
    {
        FieldInfo? cosmeticsField = AccessTools.Field(Player.GetType(), "_playerOwnedCosmetics");

        object? cosmeticsObj = cosmeticsField.GetValue(Player);
        string  concat       = "";

        if (cosmeticsObj is HashSet<string> cosmeticSet)
            concat = string.Join("", cosmeticSet);

        if (concat.Contains("LMAPY."))
            return "Forest Guide";

        if (concat.Contains("LBANI."))
            return "Another Axiom Creator";

        if (concat.Contains("LBADE."))
            return "Finger Painter";

        if (concat.Contains("LBAAD."))
            return "Administrator Badge";

        return "False";
    }

    public static string GetFPS(VRRig Player)
    {
        FieldInfo? fieldInfo = AccessTools.Field(Player.GetType(), "fps");

        if (fieldInfo != null)
            return fieldInfo.GetValue(Player)?.ToString() ?? "0";

        return "0";
    }

    public void CreateMenu()
    {
        GTPlayer? player = GTPlayer.Instance;
        isMenuCreated = true;

        if (menuPrefab != null)
        {
            menuObj = Instantiate(menuPrefab);
        }
        else
        {
            menuObj                      = GameObject.CreatePrimitive(PrimitiveType.Cube);
            menuObj.transform.localScale = new Vector3(0.03f, 0.21f, 0.45f);
        }

        if (ModConfig.instance.IsOneHandedMenu.Value)
        {
            if (player.headCollider != null)
            {
                float distanceInFront = 0.5f;

                menuObj.transform.position = player.headCollider.transform.position +
                                             player.headCollider.transform.forward * distanceInFront;

                menuObj.transform.LookAt(player.headCollider.transform.position);

                menuObj.transform.Rotate(-90f, 180f, 90f);

                menuObj.transform.parent = player.headCollider.transform;
                if (!ModConfig.instance.IsJoystickNavigation.Value)
                {
                    HandMenuCollider2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    if (IsVRRIGEnabled)
                    {
                        HandMenuCollider2.transform.parent = GorillaTagger.Instance.leftHandTriggerCollider.transform;
                        HandMenuCollider2.transform.localPosition = Vector3.zero;
                    }
                    else
                    {
                        HandMenuCollider2.transform.parent = GTPlayer.Instance.LeftHand.controllerTransform.transform;
                        HandMenuCollider2.transform.localPosition = Vector3.down * 0.094f;
                    }

                    HandMenuCollider2.layer                = 2;
                    HandMenuCollider2.transform.localScale = new Vector3(0.008f, 0.008f, 0.008f);
                    Destroy(HandMenuCollider2.GetComponent<Rigidbody>());

                    if (ModConfig.instance.ShowHandCollider.Value)
                    {
                        Renderer? rendhand2 = HandMenuCollider2.GetComponent<Renderer>();
                        rendhand2.material.shader = Shader.Find("GUI/Text Shader");
                        rendhand2.material.color  = Color.white;
                    }
                }
            }
            else
            {
                menuObj.transform.parent        = player.LeftHand.controllerTransform;
                menuObj.transform.localPosition = menuForwardOffset;
                menuObj.transform.localRotation = Quaternion.identity;
            }
        }
        else
        {
            menuObj.transform.parent        = player.LeftHand.controllerTransform;
            menuObj.transform.localPosition = menuForwardOffset;
            menuObj.transform.localRotation = Quaternion.identity;
        }

        Transform backBtn    = menuObj.transform.Find("Back");
        Transform forwardBtn = menuObj.transform.Find("Forwards");

        GameObject backObj    = backBtn    != null ? backBtn.gameObject : null;
        GameObject forwardObj = forwardBtn != null ? forwardBtn.gameObject : null;

        Transform Disconnect = menuObj.transform.Find("Disconnect");

        GameObject DisconnectObj = Disconnect != null ? Disconnect.gameObject : null;
        Transform  Home          = menuObj.transform.Find("Home");
        GameObject HomeObj       = Home != null ? Home.gameObject : null;

        SetupNavButton(backObj);
        SetupNavButton(forwardObj);
        SetupNavButton(DisconnectObj);
        SetupNavButton(HomeObj);

        if (backObj != null)
            backObj.GetComponent<ButtonCollider>().OnPressed = () =>
                                                               {
                                                                   currentPageIndex = Mathf.Max(0,
                                                                           currentPageIndex - 1);

                                                                   RefreshMenu();
                                                               };

        if (forwardObj != null)
            forwardObj.GetComponent<ButtonCollider>().OnPressed = () =>
                                                                  {
                                                                      currentPageIndex = Mathf.Min(Pages - 1,
                                                                              currentPageIndex           + 1);

                                                                      RefreshMenu();
                                                                  };

        if (DisconnectObj != null)
        {
            DisconnectObj.GetComponent<ButtonCollider>().OnPressed = PhotonNetwork.Disconnect;
            TextMeshPro? text = DisconnectObj.transform.Find("TextUnchangable").GetComponent<TextMeshPro>();
            text.text = Localization.Get("Disconnect");
        }

        if (HomeObj != null)
            HomeObj.GetComponent<ButtonCollider>().OnPressed = () =>
                                                               {
                                                                   if (searchActive) CloseSearch();
                                                                   else SwitchPage(-1, 0);
                                                               };

        UpdateCurrentPageCount();

        Transform titleTransform = menuObj.transform.Find("Title");

        if (titleTransform != null)
        {
            TextMeshPro? text = titleTransform.GetComponent<TextMeshPro>();
            if (text != null)
            {
                string pageTitle = searchActive
                                           ? "Search: " + (searchText.Length == 0 ? "..." : searchText)
                                           : currentCategoryIndex == -1
                                                   ? Localization.Get("Gemstone")
                                                   : GetCategoryName(currentCategoryIndex);

                string playerTag = PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null
                                           ? " (" + PhotonNetwork.CurrentRoom.PlayerCount + ")"
                                           : "";

                text.text = pageTitle + " [" + (currentPageIndex + 1) + "]" + playerTag;

                text.color = ModConfig.Theme == Color.white ? Color.black : Color.white;

                if (text.fontSharedMaterial != null)
                    text.fontSharedMaterial.shader = Shader.Find("TextMeshPro/Distance Field");
            }
        }
        else
        {
            Debug.LogWarning("Prefab not found");
        }

        Transform BackText = menuObj.transform.Find(":3");
        if (BackText != null)
        {
            TextMeshPro? text = BackText.GetComponent<TextMeshPro>();
            if (text.fontSharedMaterial != null)
                text.fontSharedMaterial.shader = Shader.Find("TextMeshPro/Distance Field");
        }

        if (menuObj.GetComponent<Rigidbody>()) Destroy(menuObj.GetComponent<Rigidbody>());
        if (menuObj.GetComponent<Collider>()) Destroy(menuObj.GetComponent<Collider>());

        Renderer? rend = menuObj.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.shader = Shader.Find("GorillaTag/UberShader");
            rend.material.color  = ModConfig.Theme;
            if (ModConfig.instance.IsMenuRGB.Value) rgbCoroutine = StartCoroutine(RGBTheme(rend));
            else if (ModConfig.IsHalloween) rgbCoroutine = StartCoroutine(HalloweenTheme(rend));
        }

        if (!ModConfig.instance.IsJoystickNavigation.Value)
        {
            HandMenuCollider = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            if (IsVRRIGEnabled)
            {
                HandMenuCollider.transform.parent        = GorillaTagger.Instance.rightHandTriggerCollider.transform;
                HandMenuCollider.transform.localPosition = Vector3.zero;
            }
            else
            {
                HandMenuCollider.transform.parent        = GTPlayer.Instance.RightHand.controllerTransform.transform;
                HandMenuCollider.transform.localPosition = Vector3.down * 0.094f;
            }

            HandMenuCollider.layer                = 2;
            HandMenuCollider.transform.localScale = new Vector3(0.008f, 0.008f, 0.008f);
            Destroy(HandMenuCollider.GetComponent<Rigidbody>());

            if (ModConfig.instance.ShowHandCollider.Value)
            {
                Renderer? rendhand = HandMenuCollider.GetComponent<Renderer>();
                rendhand.material.shader = Shader.Find("GUI/Text Shader");
                rendhand.material.color  = Color.white;
            }
        }

        float       zOffset = 0.005f;
        const float Step    = 0.004f;

        DrawBackendPage(ref zOffset, Step);

        foreach (GameObject btnObj in btnObjs.OfType<GameObject>())
        {
            btnObj.layer                              = 2;
            btnObj.GetComponent<Collider>().isTrigger = true;
        }

        return;

        void SetupNavButton(GameObject obj)
        {
            if (obj == null) return;

            obj.layer = 2;

            Rigidbody? rb = obj.GetComponent<Rigidbody>() ?? obj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity  = false;

            Collider? col                  = obj.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            Renderer? rend = obj.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.shader = Shader.Find("GorillaTag/UberShader");
                rend.material.color  = ModConfig.Theme;

                if (ModConfig.instance.IsMenuRGB.Value)
                    StartCoroutine(RGBTheme(rend));
                else if (ModConfig.IsHalloween)
                    StartCoroutine(HalloweenTheme(rend));
            }

            ButtonCollider? bc = obj.GetComponent<ButtonCollider>() ?? obj.AddComponent<ButtonCollider>();

            bc.OnPressed = null;
        }
    }

    private string GetCategoryName(int index) => GemstoneMenuBackend.GetCategoryName(index);

    private void SwitchPage(int category, int page)
    {
        currentCategoryIndex = category;
        currentPageIndex     = page;
        selectedButtonIndex  = 0;

        if (category != 6)
        {
            inPlayerSubmenu = false;
            selectedPlayer  = null;
        }

        RefreshMenu();
    }

    public void RefreshMenu()
    {
        if (!menuOpen)
            return;

        UpdateCurrentPageCount();
        DestroyMenu(true);
        CreateMenu();
    }

    private void DrawBackendPage(ref float zOffset, float step)
    {
        UpdateCurrentPageCount();

        if (searchActive)
        {
            DrawSearchPage(ref zOffset, step);

            return;
        }

        switch (currentCategoryIndex)
        {
            case -1:
            {
                List<ModButton> buttons =
                        GemstoneMenuBackend.GetHomeButtons(IsAdmin, category => SwitchPage(category, 0));

                DrawButtonPage(buttons, ref zOffset, step);
                DrawSearchToggleButton();

                return;
            }

            case 6:
                DrawPlayerListPage(ref zOffset, step);
                DrawSearchToggleButton();

                return;

            case 7:
                DrawSoundboardPage(ref zOffset, step);
                DrawSearchToggleButton();

                return;

            default:
            {
                IReadOnlyList<ModButton> categoryButtons =
                        GemstoneMenuBackend.GetButtons(currentCategoryIndex, IsAdmin);

                DrawButtonPage(categoryButtons, ref zOffset, step);
                DrawSearchToggleButton();

                break;
            }
        }
    }

    private void DrawSearchPage(ref float zOffset, float step)
    {
        if (searchResultsMode)
        {
            IReadOnlyList<ModButton> results = GemstoneMenuBackend.SearchButtons(searchText, IsAdmin);

            if (results.Count == 0)
                AddButton(zOffset, 0f, 0.2f, searchText.Length == 0 ? "Type letters" : "No results", () => { });
            else
                DrawButtonPage(results, ref zOffset, step);
        }
        else
        {
            DrawSearchKeyboardPage();
        }

        DrawSearchToggleButton();
    }

    private void DrawSearchKeyboardPage()
    {
        int      view = Mathf.Clamp(currentPageIndex, 0, searchKeyboardViews.Length - 1);
        string[] keys = searchKeyboardViews[view];

        for (int i = 0; i < keys.Length; i++)
        {
            string key    = keys[i];
            bool   special = key is "DEL" or "SPC" or "CLR" or "GO";

            AddButton(
                    searchKeyRows[i / searchKeyColumns.Length],
                    searchKeyColumns[i % searchKeyColumns.Length],
                    0.2f,
                    key,
                    () => PressSearchKey(key),
                    special
            );

            ShrinkLastButtonWidth(0.38f);
        }
    }

    private void DrawSearchToggleButton()
    {
        if (!SearchToggleButtonVisible())
            return;

        string label;
        Action action;

        if (!searchActive)
        {
            label  = "Search";
            action = EnterSearch;
        }
        else if (searchResultsMode)
        {
            label = "Keys";

            action = () =>
                     {
                         PlaySearchSound();
                         searchButtonPopPending = true;
                         searchResultsMode   = false;
                         currentPageIndex    = 0;
                         selectedButtonIndex = 0;
                         RefreshMenu();
                     };
        }
        else
        {
            label  = "Close";
            action = CloseSearch;
        }

        AddButton(-0.0116f, 0.0146f, 0.2f, label, action);
        ShrinkLastButtonWidth(0.45f);

        if (!searchActive)
        {
            GameObject searchButton    = btnObjs[^1];
            Transform  textTransform   = searchButton.transform.Find("Text");
            TextMeshPro labelText = textTransform != null ? textTransform.GetComponent<TextMeshPro>() : null;

            if (labelText != null)
                labelText.text = "";

            CreateSearchIcon(searchButton);
        }

        AnimateSearchButton();
    }

    private void CreateSearchIcon(GameObject button)
    {
        if (searchIconTexture == null)
            searchIconTexture = CreateMagnifierTexture();

        if (searchIconMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default") ??
                            Shader.Find("Unlit/Transparent") ??
                            Shader.Find("GUI/Text Shader");

            if (shader == null)
                return;

            searchIconMaterial = new Material(shader);
            searchIconMaterial.mainTexture = searchIconTexture;
            searchIconMaterial.SetTexture("_MainTex", searchIconTexture);
        }

        searchIconMaterial.color = ModConfig.Theme == Color.white ? Color.black : Color.white;

        Transform textChild = button.transform.Find("Text");

        if (textChild == null || textChild.Find("SearchIcon") != null)
            return;

        TextMeshPro text = textChild.GetComponent<TextMeshPro>();

        GameObject icon = GameObject.CreatePrimitive(PrimitiveType.Cube);
        icon.name  = "SearchIcon";
        icon.layer = button.layer;

        Collider iconCollider = icon.GetComponent<Collider>();
        if (iconCollider != null)
        {
            iconCollider.enabled = false;
            Destroy(iconCollider);
        }

        icon.transform.SetParent(textChild, false);
        icon.transform.localPosition  = Vector3.zero;
        icon.transform.localRotation = Quaternion.identity;

        Vector3 textLossy  = textChild.lossyScale;
        float   rectHeight = text != null ? Mathf.Abs(text.rectTransform.rect.height) : 0.03f;

        if (rectHeight <= 0.0001f)
            rectHeight = 0.03f;

        float sizeWorld  = Mathf.Max(0.0001f, textLossy.y * rectHeight * 0.55f);
        float thickWorld = 0.004f;

        icon.transform.localScale = new Vector3(
                textLossy.x > 0f ? sizeWorld / textLossy.x : 0.01f,
                textLossy.y > 0f ? sizeWorld / textLossy.y : 0.01f,
                textLossy.z > 0f ? thickWorld / textLossy.z : 0.001f
        );

        Renderer iconRenderer = icon.GetComponent<Renderer>();
        iconRenderer.sharedMaterial = searchIconMaterial;
    }

    private static Texture2D CreateMagnifierTexture()
    {
        const int size = 96;
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp
        };

        Color clear = new(0f, 0f, 0f, 0f);
        Color solid = new(1f, 1f, 1f, 1f);

        const float lensX     = -0.12f;
        const float lensY     =  0.12f;
        const float lensOuter =  0.52f;
        const float lensThick =  0.17f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = (x + 0.5f) / size * 2f - 1f;
                float py = (y + 0.5f) / size * 2f - 1f;

                float dx = px - lensX;
                float dy = py - lensY;
                float d  = Mathf.Sqrt(dx * dx + dy * dy);

                bool inside = d >= lensOuter - lensThick && d <= lensOuter;

                if (!inside)
                    inside = DistanceToSegment(px, py, 0.26f, -0.26f, 0.78f, -0.78f) <= 0.13f;

                texture.SetPixel(x, y, inside ? solid : clear);
            }
        }

        texture.Apply();
        return texture;
    }

    private static float DistanceToSegment(float px, float py, float x1, float y1, float x2, float y2)
    {
        float abx = x2 - x1;
        float aby = y2 - y1;
        float apx = px - x1;
        float apy = py - y1;
        float ab2 = abx * abx + aby * aby;
        float t   = ab2 > 0f ? Mathf.Clamp01((apx * abx + apy * aby) / ab2) : 0f;
        float cx  = apx - abx * t;
        float cy  = apy - aby * t;
        return Mathf.Sqrt(cx * cx + cy * cy);
    }

    private void PlaySearchSound(float pitch = 1.6f)
    {
        if (soundReady && cachedClip != null) audioSource.PlayOneShot(cachedClip, pitch);
    }

    private void AnimateSearchButton()
    {
        if (btnObjs.Count == 0)
            return;

        GameObject button = btnObjs[^1];
        if (button == null)
            return;

        if (searchButtonAnimation != null)
        {
            StopCoroutine(searchButtonAnimation);
            searchButtonAnimation = null;
        }

        bool playPop = searchButtonPopPending;
        searchButtonPopPending = false;

        searchButtonAnimation = StartCoroutine(SearchButtonPulse(button, playPop));
    }

    private IEnumerator SearchButtonPulse(GameObject button, bool playPop)
    {
        Transform textTransform = button.transform.Find("Text");
        TextMeshPro text = textTransform != null ? textTransform.GetComponent<TextMeshPro>() : null;
        FollowMenu followMenu = button.GetComponent<FollowMenu>();

        Vector3    baseScale      = button.transform.localScale;
        Vector3    baseTextScale  = text != null ? textTransform.localScale : Vector3.one;
        Vector3    basePosition   = followMenu != null ? followMenu.position : Vector3.zero;
        Quaternion baseRotation   = followMenu != null ? followMenu.rotationOffset : Quaternion.identity;

        const float popDuration = 0.35f;
        float time = playPop ? 0f : popDuration;

        while (button != null)
        {
            time += Time.deltaTime;

            float pop = time < popDuration
                                ? EaseOutBack(time / popDuration)
                                : 1f + Mathf.Sin((time - popDuration) * 2.2f) * 0.025f;

            Vector3 scale = baseScale * pop;
            button.transform.localScale = scale;

            if (text != null)
            {
                Vector3 textSize = baseTextScale;

                if (scale.z != 0f) textSize.x = baseTextScale.x * baseScale.z / scale.z;
                if (scale.y != 0f) textSize.y = baseTextScale.y * baseScale.y / scale.y;

                textTransform.localScale = textSize;
                text.color               = Color.HSVToRGB(Mathf.Repeat(time * 0.05f, 1f), 0.35f, 1f);
            }

            if (followMenu != null)
            {
                Vector3 position = basePosition;
                position.z += Mathf.Sin(time * 1.8f) * 0.00025f;
                followMenu.position = position;

                followMenu.rotationOffset = baseRotation *
                                            Quaternion.Euler(Mathf.Sin(time * 1.8f + 1f) * 1.5f, 0f, 0f);
            }

            yield return null;
        }

        searchButtonAnimation = null;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 0.6f;
        const float c3 = c1 + 1f;
        float       u  = t - 1f;

        return 1f + c3 * u * u * u + c1 * u * u;
    }

    private bool SearchToggleButtonVisible() => true;

    private void ShrinkLastButtonWidth(float factor)
    {
        if (btnObjs.Count == 0)
            return;

        GameObject button = btnObjs[^1];
        if (button == null)
            return;

        Vector3 scale = button.transform.localScale;
        scale.z *= factor;
        button.transform.localScale = scale;

        Transform textTransform = button.transform.Find("Text");

        if (textTransform != null)
        {
            Vector3 textSize = textTransform.localScale;
            textSize.x /= factor;
            textTransform.localScale = textSize;
        }
    }

    private void EnterSearch()
    {
        PlaySearchSound();
        searchButtonPopPending = true;
        searchReturnCategory = currentCategoryIndex;
        searchReturnPage     = currentPageIndex;
        searchActive         = true;
        searchResultsMode    = false;
        searchText           = "";
        currentPageIndex     = 0;
        selectedButtonIndex  = 0;

        RefreshMenu();
    }

    private void CloseSearch()
    {
        PlaySearchSound();
        searchButtonPopPending = true;
        searchActive         = false;
        searchResultsMode    = false;
        searchText           = "";
        currentCategoryIndex = searchReturnCategory;
        currentPageIndex     = searchReturnPage;
        selectedButtonIndex  = 0;
        searchReturnCategory = -1;
        searchReturnPage     = 0;

        RefreshMenu();
    }

    private void PressSearchKey(string key)
    {
        switch (key)
        {
            case "DEL":
                if (searchText.Length > 0) searchText = searchText[..^1];

                break;
            case "SPC":
                if (searchText.Length > 0 && searchText.Length < 24) searchText += " ";

                break;
            case "CLR":
                searchText        = "";
                searchResultsMode = false;
                currentPageIndex  = 0;

                break;
            case "GO":
                if (searchText.Length > 0)
                {
                    searchResultsMode = true;
                    currentPageIndex  = 0;
                }

                break;
            default:
                if (searchText.Length < 24) searchText += key.ToLowerInvariant();

                if (searchText.Length > 0)
                {
                    searchResultsMode = true;
                    currentPageIndex  = 0;
                }

                break;
        }

        if (searchResultsMode && searchText.Length == 0)
        {
            searchResultsMode = false;
            currentPageIndex  = 0;
        }

        selectedButtonIndex = 0;
        RefreshMenu();
    }

    private void HandleSearchTyping()
    {
        bool changed = false;

        for (KeyCode key = KeyCode.A; key <= KeyCode.Z; key++)
            if (UnityInput.Current.GetKeyDown(key))
            {
                if (searchText.Length < 24) searchText += (char)('a' + (key - KeyCode.A));

                changed = true;
            }

        for (KeyCode key = KeyCode.Alpha0; key <= KeyCode.Alpha9; key++)
            if (UnityInput.Current.GetKeyDown(key))
            {
                if (searchText.Length < 24) searchText += (char)('0' + (key - KeyCode.Alpha0));

                changed = true;
            }

        if (UnityInput.Current.GetKeyDown(KeyCode.Space) && searchText.Length > 0 && searchText.Length < 24)
        {
            searchText += " ";
            changed     = true;
        }

        if (changed)
        {
            bool wantResults = searchText.Length > 0;

            if (searchResultsMode != wantResults)
            {
                searchResultsMode = wantResults;
                currentPageIndex  = 0;
            }

            PlaySearchSound(1.3f);
            selectedButtonIndex = 0;
            RefreshMenu();
        }

        if (UnityInput.Current.GetKeyDown(KeyCode.Backspace) && searchText.Length > 0)
        {
            searchText = searchText[..^1];

            if (searchResultsMode && searchText.Length == 0)
            {
                searchResultsMode = false;
                currentPageIndex  = 0;
            }

            PlaySearchSound(1.15f);
            selectedButtonIndex = 0;
            RefreshMenu();
        }

        if (UnityInput.Current.GetKeyDown(KeyCode.Return) && !searchResultsMode && searchText.Length > 0)
        {
            searchResultsMode   = true;
            currentPageIndex    = 0;
            selectedButtonIndex = 0;

            RefreshMenu();
        }

        if (UnityInput.Current.GetKeyDown(KeyCode.Escape)) CloseSearch();
    }

    private void DrawButtonPage(IReadOnlyList<ModButton> buttons, ref float zOffset, float step)
    {
        int startIndex = currentPageIndex * GemstoneMenuBackend.ButtonsPerPage;
        int endIndex   = Mathf.Min(startIndex + GemstoneMenuBackend.ButtonsPerPage, buttons.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            ModButton button = buttons[i];

            AddButton(
                    zOffset,
                    0f,
                    0.2f,
                    button.Name,
                    () =>
                    {
                        button.Press();

                        if (button.RefreshAfterPress)
                            RefreshMenu();
                    },
                    button.IsActive
            );

            zOffset -= step;
        }
    }

    private void DrawPlayerListPage(ref float zOffset, float step)
    {
        if (inPlayerSubmenu)
        {
            DrawSelectedPlayerPage(ref zOffset, step);

            return;
        }

        Player[] players = PhotonNetwork.PlayerList;

        if (players == null || players.Length == 0)
        {
            AddButton(zOffset, 0f, 0.2f, Localization.Get("No Players"), () => { });

            return;
        }

        int startIndex = currentPageIndex * GemstoneMenuBackend.ButtonsPerPage;
        int endIndex   = Mathf.Min(startIndex + GemstoneMenuBackend.ButtonsPerPage, players.Length);

        for (int i = startIndex; i < endIndex; i++)
        {
            Player player = players[i];

            AddButton(
                    zOffset,
                    0f,
                    0.2f,
                    player.NickName,
                    () =>
                    {
                        selectedPlayer      = player;
                        inPlayerSubmenu     = true;
                        currentPageIndex    = 0;
                        selectedButtonIndex = 0;
                        RefreshMenu();
                    }
            );

            zOffset -= step;
        }
    }

    private void DrawSelectedPlayerPage(ref float zOffset, float step)
    {
        AddButton(
                zOffset,
                0f,
                0.2f,
                Localization.Get("Back"),
                () =>
                {
                    inPlayerSubmenu     = false;
                    selectedPlayer      = null;
                    selectedButtonIndex = 0;
                    RefreshMenu();
                }
        );

        zOffset -= step;

        AddButton(
                zOffset,
                0f,
                0.2f,
                Localization.Get("Teleport to"),
                () =>
                {
                    if (selectedPlayer == null)
                        return;

                    StartCoroutine(Mods.Mods.TpToPlayer(selectedPlayer.UserId));
                }
        );

        zOffset -= step;

        AddButton(
                zOffset,
                0f,
                0.2f,
                Localization.Get("Custom Properties"),
                () =>
                {
                    if (selectedPlayer == null)
                        return;

                    string output = $"Player: {selectedPlayer.NickName}\n\nCustom Properties:\n";

                    if (selectedPlayer.CustomProperties != null && selectedPlayer.CustomProperties.Count > 0)
                        foreach (DictionaryEntry property in selectedPlayer.CustomProperties)
                            output += $"{property.Key}: {property.Value}\n";
                    else
                        output += "None";

                    NotiLib.SendNotification(output, 4000f);
                }
        );

        zOffset -= step;

        AddButton(
                zOffset,
                0f,
                0.2f,
                Localization.Get("Block Player"),
                () =>
                {
                    if (selectedPlayer == null)
                        return;

                    string userId = selectedPlayer.UserId;

                    foreach (VRRig rig in VRRigCache.ActiveRigs)
                    {
                        if (rig == null || rig.Creator == null)
                            continue;

                        if (rig.Creator.UserId != userId)
                            continue;

                        if (rig.Creator.UserId == PhotonNetwork.LocalPlayer.UserId)
                            continue;

                        rig.gameObject.SetActive(false);
                    }

                    NotiLib.SendNotification($"Blocked {selectedPlayer.NickName}", 2000f);
                }
        );
    }

    private void DrawSoundboardPage(ref float zOffset, float step)
    {
        if (soundboardClips.Count == 0)
        {
            AddButton(zOffset, 0f, 0.2f, Localization.Get("No Sounds Found"), () => { });

            return;
        }

        int startIndex = currentPageIndex * GemstoneMenuBackend.ButtonsPerPage;
        int endIndex   = Mathf.Min(startIndex + GemstoneMenuBackend.ButtonsPerPage, soundboardClips.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            AudioClip selectedClip = soundboardClips[i];
            string buttonName = currentlyPlayingClip == selectedClip
                                        ? $"[PLAYING] {selectedClip.name}"
                                        : selectedClip.name;

            AddButton(
                    zOffset,
                    0f,
                    0.2f,
                    buttonName,
                    () =>
                    {
                        ToggleSoundboard(selectedClip);
                        RefreshMenu();
                    },
                    currentlyPlayingClip == selectedClip
            );

            zOffset -= step;
        }
    }

    private void UpdateCurrentPageCount()
    {
        int buttonCount = GetCurrentButtonCount();

        Pages = searchActive && !searchResultsMode
                        ? searchKeyboardViews.Length
                        : GemstoneMenuBackend.GetPageCount(buttonCount);

        currentPageIndex = Mathf.Clamp(
                currentPageIndex,
                0,
                Mathf.Max(0, Pages - 1)
        );

        int pageButtonCount = GetCurrentPageButtonCount(buttonCount);
        int extraButton     = SearchToggleButtonVisible() ? 1 : 0;

        selectedButtonIndex = Mathf.Clamp(
                selectedButtonIndex,
                0,
                Mathf.Max(0, pageButtonCount - 1 + extraButton)
        );
    }

    private int GetCurrentButtonCount()
    {
        if (searchActive)
        {
            if (searchResultsMode)
                return Mathf.Max(1, GemstoneMenuBackend.SearchButtons(searchText, IsAdmin).Count);

            int view = Mathf.Clamp(currentPageIndex, 0, searchKeyboardViews.Length - 1);

            return searchKeyboardViews[view].Length;
        }

        switch (currentCategoryIndex)
        {
            case -1:
                return GemstoneMenuBackend.GetHomeButtonCount(IsAdmin);

            case 6 when inPlayerSubmenu:
                return 4;

            case 6:
            {
                Player[] players = PhotonNetwork.PlayerList;

                return players == null || players.Length == 0 ? 1 : players.Length;
            }

            case 7:
                return Mathf.Max(1, soundboardClips.Count);

            default:
                return GemstoneMenuBackend.GetButtons(currentCategoryIndex, IsAdmin).Count;
        }
    }

    private int GetCurrentPageButtonCount(int buttonCount)
    {
        if (buttonCount <= 0)
            return 0;

        if (searchActive && !searchResultsMode)
            return buttonCount;

        int startIndex = currentPageIndex * GemstoneMenuBackend.ButtonsPerPage;

        return Mathf.Clamp(
                buttonCount - startIndex,
                0,
                GemstoneMenuBackend.ButtonsPerPage
        );
    }

    public void DestroyMenu(bool refresh)
    {
        if (!refresh) isMenuCreated = false;

        if (menuObj != null)
        {
            if (ModConfig.instance.IsMenuRGB.Value && rgbCoroutine != null)
                StopCoroutine(rgbCoroutine);

            if (!refresh)
            {
                menuObj.transform.SetParent(null, true);

                Rigidbody rb = menuObj.GetComponent<Rigidbody>() ?? menuObj.AddComponent<Rigidbody>();
                rb.isKinematic = false;
                rb.useGravity  = true;
                rb.AddForce(Vector3.down * 0.5f, ForceMode.Impulse);

                GameObject menuToDestroy = menuObj;
                menuObj = null;
                Destroy(menuToDestroy, 5f);
            }
            else
            {
                Destroy(menuObj);
                menuObj = null;
            }
        }

        if (HandMenuCollider != null)
        {
            Destroy(HandMenuCollider);
            HandMenuCollider = null;
        }

        if (HandMenuCollider2 != null)
        {
            Destroy(HandMenuCollider2);
            HandMenuCollider2 = null;
        }

        foreach (GameObject? b in btnObjs)
            if (b != null)
            {
                if (!refresh)
                {
                    b.transform.SetParent(null, true);
                    Rigidbody rb = b.GetComponent<Rigidbody>() ?? b.AddComponent<Rigidbody>();
                    rb.isKinematic = false;
                    rb.useGravity  = true;
                    rb.AddForce(Vector3.down * 0.5f, ForceMode.Impulse);
                    Destroy(b, 5f);
                }
                else
                {
                    Destroy(b);
                }
            }

        btnObjs.Clear();
        Config.Save();
    }

    private void AddToggleButton(ref float z, float step, string name, ConfigEntry<bool> entry, Action onDisable = null,
                                 Action    onEnable = null)
    {
        AddButton(z, 0f, 0.2f, name, () =>
                                     {
                                         entry.Value = !entry.Value;
                                         if (entry.Value) onEnable?.Invoke();
                                         else onDisable?.Invoke();

                                         RefreshMenu();
                                     }, entry.Value);

        z -= step;
    }

    private void AddButton(float z, float y, float scaleY, string name, Action action, bool isActive = false)
    {
        GameObject button = null;

        if (menuBundle != null)
        {
            GameObject buttonPrefab = menuBundle.LoadAsset<GameObject>("Assets/Button.prefab");

            if (buttonPrefab != null)
                button = Instantiate(buttonPrefab);
            else
                Logger.LogError("button prefab not found");
        }

        if (button == null)
        {
            button                      = GameObject.CreatePrimitive(PrimitiveType.Cube);
            button.transform.localScale = new Vector3(0.03f, scaleY, 0.04f);
        }

        FollowMenu followMenu = button.GetComponent<FollowMenu>() ?? button.AddComponent<FollowMenu>();
        followMenu.target         = menuObj.transform;
        followMenu.position       = new Vector3(0.014f, y, z);
        followMenu.rotationOffset = Quaternion.Euler(90f, 0f, 0f);

        Renderer buttonRenderer = button.GetComponent<Renderer>();

        if (buttonRenderer != null)
        {
            buttonRenderer.material.shader = Shader.Find("GorillaTag/UberShader");

            if (ModConfig.instance.IsMenuRGB.Value)
            {
                StartCoroutine(RGBTheme(buttonRenderer, isActive));
            }
            else if (ModConfig.IsHalloween)
            {
                StartCoroutine(HalloweenTheme(buttonRenderer, isActive));
            }
            else if (isActive)
            {
                Color activeColor = ModConfig.Theme * 1.5f;
                activeColor.a                 = 1f;
                buttonRenderer.material.color = activeColor;
            }
            else
            {
                buttonRenderer.material.color = ModConfig.Theme;
            }
        }

        Collider buttonCollider = button.GetComponent<Collider>();

        if (buttonCollider != null)
            buttonCollider.isTrigger = true;

        button.layer = 2;

        ButtonCollider pressCollider = button.GetComponent<ButtonCollider>() ?? button.AddComponent<ButtonCollider>();
        pressCollider.OnPressed = action;

        TextMeshPro text          = null;
        Transform   textTransform = button.transform.Find("Text");

        if (textTransform != null)
            text = textTransform.GetComponent<TextMeshPro>();

        if (text == null)
        {
            GameObject textObject = new("Text");
            textObject.transform.SetParent(button.transform);
            textObject.transform.localPosition = new Vector3(0.55f, 0f, 0f);
            textObject.transform.localRotation = Quaternion.Euler(0f, -90f, -90f);

            text = textObject.AddComponent<TextMeshPro>();
        }

        text.color = ModConfig.Theme == Color.white ? Color.black : Color.white;

        int buttonIndex = btnObjs.Count;

        text.text = ModConfig.instance.IsJoystickNavigation.Value && menuOpen && selectedButtonIndex == buttonIndex
                            ? "[SEL] " + name
                            : name;

        btnObjs.Add(button);
    }

    public void PlayClickSound()
    {
        if (soundReady && cachedClip != null) audioSource.PlayOneShot(cachedClip);
    }

    public IEnumerator RGBTheme(Renderer r, bool isActive = false)
    {
        while (true)
        {
            float speedMultiplier = isActive ? 5f : 2f;
            float t               = Time.time * speedMultiplier;

            r.material.color = new Color(
                    Mathf.Sin(t)      * 0.5f + 0.5f,
                    Mathf.Sin(t + 2f) * 0.5f + 0.5f,
                    Mathf.Sin(t + 4f) * 0.5f + 0.5f
            );

            yield return null;
        }
    }

    public IEnumerator HalloweenTheme(Renderer r, bool isActive = false)
    {
        Color orange = new(1f,    0.42f, 0.05f);
        Color purple = new(0.45f, 0.08f, 0.62f);
        Color dark   = new(0.06f, 0.02f, 0.09f);

        while (r != null)
        {
            float t       = Time.time * (isActive ? 3.2f : 1.3f);
            float flicker = Mathf.PerlinNoise(Time.time * 4.5f, 0.618f);

            Color color = Color.Lerp(orange, purple, Mathf.Sin(t) * 0.5f + 0.5f);
            color = Color.Lerp(dark, color, 0.55f + flicker * 0.45f);
            color.a = 1f;

            r.material.color = color;

            yield return null;
        }
    }

    public class FollowMenu : MonoBehaviour
    {
        public Transform  target;
        public Vector3    position;
        public Quaternion rotationOffset = Quaternion.identity;

        private void LateUpdate()
        {
            if (target)
            {
                transform.position = target.TransformPoint(position);
                transform.rotation = target.rotation * rotationOffset;
            }
        }
    }

    public class ButtonCollider : MonoBehaviour
    {
        public Action OnPressed;

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject != instance.HandMenuCollider && other.gameObject != instance.HandMenuCollider2)
                return;

            if (other.gameObject == instance.HandMenuCollider)
                GorillaTagger.Instance.StartVibration(
                        false,
                        GorillaTagger.Instance.tagHapticStrength / 2f,
                        0.05f
                );
            else
                GorillaTagger.Instance.StartVibration(
                        true,
                        GorillaTagger.Instance.tagHapticStrength / 2f,
                        0.05f
                );

            Press();
        }

        public void Press()
        {
            if (instance.globalClickCooldown > 0)
                return;

            instance.globalClickCooldown = 0.2f;

            instance.PlayClickSound();

            OnPressed?.Invoke();
        }
    }
}
