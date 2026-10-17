using System.Reflection;
using BepInEx;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gemstone.Gemstone;

internal class Gui : MonoBehaviour
{
    private const float AnimSpeed = 12f;
    private const float HiddenX = -320f;
    private const float VisibleX = 10f;

    private float connectionX = HiddenX;
    private float modsX = HiddenX;
    private int currentGuiTab = -1;
    private bool menuVisible = true;
    private bool stylesInitialized;
    private bool stylesHalloween;
    private Vector2 modScrollPosition = Vector2.zero;
    private string searchQuery = "";

    private Rect connectionWindowRect = new(HiddenX, 20, 250, 220);
    private Rect modsWindowRect = new(HiddenX, 20, 320, 600);

    private GUIStyle windowStyle;
    private GUIStyle buttonStyle;
    private GUIStyle toggleStyle;
    private GUIStyle labelStyle;
    private GUIStyle headerStyle;
    private GUIStyle searchFieldStyle;

    private Texture2D windowBackgroundTex;
    private Texture2D buttonNormalTex;
    private Texture2D buttonHoverTex;
    private Texture2D buttonActiveTex;
    private Texture2D toggleOnTex;
    private Texture2D toggleOffTex;

    private void Update()
    {
        if (UnityInput.Current.GetKeyDown(KeyCode.F11))
        {
            menuVisible = !menuVisible;
        }

        UpdateAnimations();
    }

    private void UpdateAnimations()
    {
        Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        mousePos.y = Screen.height - mousePos.y;

        bool hoverArea = mousePos.x < 600 && mousePos.y < 700;

        connectionX = Mathf.Lerp(connectionX, hoverArea ? VisibleX : HiddenX, Time.deltaTime * AnimSpeed);
        modsX = Mathf.Lerp(modsX, hoverArea ? VisibleX + 260f : HiddenX, Time.deltaTime * AnimSpeed);

        connectionWindowRect.x = connectionX;
        modsWindowRect.x = modsX;
    }

    private void OnGUI()
    {
        if (!menuVisible) return;

        if (stylesInitialized && ModConfig.IsHalloween != stylesHalloween)
            stylesInitialized = false;

        InitializeStyles();

        if (ModConfig.IsHalloween)
            UpdateHalloweenFlicker();

        connectionWindowRect = GUI.Window(0, connectionWindowRect, DrawConnectionWindow, "System", windowStyle);
        modsWindowRect = GUI.Window(1, modsWindowRect, DrawModsWindow, "Menu", windowStyle);
    }

    private void InitializeStyles()
    {
        if (stylesInitialized) return;

        bool halloween = ModConfig.IsHalloween;

        if (halloween)
        {
            windowBackgroundTex = CreateSolidColorTexture(new Color(1f,    0.42f, 0.05f, 0.95f));
            buttonNormalTex = CreateSolidColorTexture(new Color(1f,    0.42f, 0.05f, 1f));
            buttonHoverTex = CreateSolidColorTexture(new Color(0.65f, 0.30f, 0.08f, 1f));
            buttonActiveTex = CreateSolidColorTexture(new Color(0.8f,  0.42f, 0.12f, 1f));
            toggleOnTex = CreateSolidColorTexture(new Color(0.85f, 0.42f, 0.08f, 1.0f));
            toggleOffTex = CreateSolidColorTexture(new Color(0.28f, 0.16f, 0.20f, 1.0f));
        }
        else
        {
            windowBackgroundTex = CreateSolidColorTexture(new Color(0.08f, 0.08f, 0.12f, 0.95f));
            buttonNormalTex = CreateSolidColorTexture(new Color(0.15f, 0.15f, 0.22f, 1f));
            buttonHoverTex = CreateSolidColorTexture(new Color(0.22f, 0.22f, 0.32f, 1f));
            buttonActiveTex = CreateSolidColorTexture(new Color(0.28f, 0.28f, 0.42f, 1f));
            toggleOnTex = CreateSolidColorTexture(new Color(0.3f, 0.75f, 0.4f, 1.0f));
            toggleOffTex = CreateSolidColorTexture(new Color(0.25f, 0.25f, 0.3f, 1.0f));
        }

        windowStyle = new GUIStyle(GUI.skin.window)
        {
            normal = { background = windowBackgroundTex },
            onNormal = { background = windowBackgroundTex },
            border = new RectOffset(10, 10, 10, 10),
            padding = new RectOffset(15, 15, 25, 15)
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            normal = { background = buttonNormalTex, textColor = Color.white },
            hover = { background = buttonHoverTex, textColor = Color.white },
            active = { background = buttonActiveTex, textColor = Color.white },
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };

        toggleStyle = new GUIStyle(GUI.skin.toggle)
        {
            normal = { background = toggleOffTex, textColor = Color.white },
            onNormal = { background = toggleOnTex, textColor = Color.white },
            padding = new RectOffset(20, 5, 2, 2)
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            normal = { textColor = Color.white }
        };

        searchFieldStyle = new GUIStyle(GUI.skin.textField)
        {
            normal = { background = buttonNormalTex, textColor = Color.white },
            focused = { background = buttonHoverTex, textColor = Color.white },
            hover = { background = buttonHoverTex, textColor = Color.white },
            fontStyle = FontStyle.Bold,
            padding = new RectOffset(8, 8, 4, 4)
        };

        stylesHalloween = halloween;
        stylesInitialized = true;
    }

    private void UpdateHalloweenFlicker()
    {
        if (windowBackgroundTex == null || buttonNormalTex == null) return;

        Color orange = new(1f,    0.42f, 0.05f);
        Color purple = new(0.45f, 0.08f, 0.62f);
        Color dark   = new(0.06f, 0.02f, 0.09f);

        float t       = Time.time * 1.3f;
        float flicker = Mathf.PerlinNoise(Time.time * 4.5f, 0.618f);

        Color color = Color.Lerp(orange, purple, Mathf.Sin(t) * 0.5f + 0.5f);
        color = Color.Lerp(dark, color, 0.55f + flicker * 0.45f);

        Color windowColor = color;
        windowColor.a = 0.95f;
        Color[] windowPixels = { windowColor, windowColor, windowColor, windowColor };
        windowBackgroundTex.SetPixels(windowPixels);
        windowBackgroundTex.Apply();

        Color buttonColor = color;
        buttonColor.a = 1f;
        Color[] buttonPixels = { buttonColor, buttonColor, buttonColor, buttonColor };
        buttonNormalTex.SetPixels(buttonPixels);
        buttonNormalTex.Apply();
    }

    private Texture2D CreateSolidColorTexture(Color color)
    {
        Texture2D texture = new(2, 2);
        Color[] colors = { color, color, color, color };
        texture.SetPixels(colors);
        texture.Apply();

        return texture;
    }

    private void DrawConnectionWindow(int windowID)
    {
        GUI.DragWindow(new Rect(0, 0, 250, 25));

        string fps = VRRig.LocalRig != null ? Main.GetFPS(VRRig.LocalRig) : "0";
        string id  = PhotonNetwork.InRoom && PhotonNetwork.LocalPlayer != null
                             ? PhotonNetwork.LocalPlayer.UserId
                             : "Offline";

        GUILayout.Label("FPS: "        + fps, labelStyle);
        GUILayout.Label("ID: "         + id,  labelStyle);

        if (!string.IsNullOrEmpty(NowPlaying.Track))
            GUILayout.Label("Now: " + NowPlaying.Track, labelStyle);

        GUILayout.Space(5);

        if (GUILayout.Button("Disconnect", buttonStyle))
        {
            PhotonNetwork.Disconnect();
        }

        if (GUILayout.Button("Quit", buttonStyle))
        {
            Application.Quit();
        }
    }

    private void DrawModsWindow(int windowID)
    {
        GUI.DragWindow(new Rect(0, 0, 320, 25));

        bool isAdmin = Main.instance != null && Main.instance.IsAdmin;
        float buttonWidth = (modsWindowRect.width - 50) / 2f;

        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();

        int count = 0;
        foreach (var category in GemstoneMenuBackend.Categories)
        {
            if (category.AdminOnly && !isAdmin) continue;

            if (count > 0 && count % 2 == 0)
            {
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
            }

            if (GUILayout.Toggle(currentGuiTab == category.Id, category.NameKey, buttonStyle, GUILayout.Width(buttonWidth)))
            {
                currentGuiTab = category.Id;
            }

            count++;
        }

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.Space(10);

        if (!string.IsNullOrEmpty(searchQuery))
        {
            DrawSearchResults(isAdmin);
        }
        else if (currentGuiTab != -1)
        {
            DrawCategory(currentGuiTab);
        }

        GUILayout.Space(8);
        GUILayout.BeginHorizontal();

        searchQuery = GUILayout.TextField(searchQuery, searchFieldStyle, GUILayout.Height(24));

        if (GUILayout.Button("X", buttonStyle, GUILayout.Width(30)))
        {
            searchQuery = "";

            if (Main.instance != null) Main.instance.PlayClickSound();
        }

        GUILayout.EndHorizontal();
    }

    private void DrawSearchResults(bool isAdmin)
    {
        modScrollPosition = GUILayout.BeginScrollView(modScrollPosition);

        List<ModButton> results = GemstoneMenuBackend.SearchButtons(searchQuery, isAdmin);

        GUILayout.Label("Results: " + results.Count, labelStyle);

        if (results.Count == 0)
            GUILayout.Label("No matches", labelStyle);

        foreach (ModButton button in results)
            DrawModButton(button);

        GUILayout.EndScrollView();
    }

    private void DrawModButton(ModButton button)
    {
        if (button.ToggleEntry != null)
        {
            bool state = GUILayout.Toggle(button.ToggleEntry.Value, button.Name, toggleStyle);

            if (state != button.ToggleEntry.Value)
            {
                button.Press();

                if (Main.instance != null) Main.instance.PlayClickSound();
            }
        }
        else if (GUILayout.Button(button.Name, buttonStyle))
        {
            button.Press();

            if (Main.instance != null) Main.instance.PlayClickSound();
        }
    }

    private void DrawCategory(int categoryId)
    {
        modScrollPosition = GUILayout.BeginScrollView(modScrollPosition);
        bool isAdmin = Main.instance != null && Main.instance.IsAdmin;

        if (categoryId == 7)
        {
            FieldInfo field = typeof(Main).GetField("soundboardClips", BindingFlags.NonPublic | BindingFlags.Instance);
            List<AudioClip> clips = field?.GetValue(Main.instance) as List<AudioClip>;

            if (clips != null)
            {
                foreach (AudioClip clip in clips)
                {
                    if (GUILayout.Button(clip.name, buttonStyle))
                    {
                        Main.ToggleSoundboard(clip);
                        Main.instance.PlayClickSound();
                    }
                }
            }
        }
        else if (categoryId == 6)
        {
            Player[] players = PhotonNetwork.PlayerList;

            if (players != null)
            {
                foreach (Player player in players)
                {
                    if (GUILayout.Button(player.NickName, buttonStyle))
                    {
                        NotiLib.SendNotification($"Selected: {player.NickName}", 2000f);
                    }
                }
            }
        }
        else
        {
            foreach (ModButton button in GemstoneMenuBackend.GetButtons(categoryId, isAdmin))
                DrawModButton(button);
        }

        GUILayout.EndScrollView();
    }
}
