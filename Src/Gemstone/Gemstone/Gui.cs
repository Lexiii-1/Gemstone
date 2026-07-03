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
using System.Collections.Generic;

namespace Gemstone.Gemstone;

internal class Gui : MonoBehaviour
{
    private const int maxEmotePages = 15;
    private Vector2 adminScrollPosition = Vector2.zero;
    private Texture2D buttonActiveTex;
    private Texture2D buttonHoverTex;
    private Texture2D buttonNormalTex;
    private GUIStyle buttonStyle;
    private Rect connectionWindowRect = new(20, 20, 250, 160);

    private int currentGuiTab = -1;

    private int emotePage = 0;
    private Vector2 emotesScrollPosition = Vector2.zero;
    private GUIStyle headerStyle;
    private bool inPlayerSubmenu;
    private GUIStyle labelStyle;

    private bool menuVisible = true;

    private Vector2 modScrollPosition = Vector2.zero;
    private Rect modsWindowRect = new Rect(280, 20, 320, 600);
    private Vector2 playerScrollPosition = Vector2.zero;
    private string roomToJoin = "";
    private Player selectedPlayer;
    private Vector2 soundboardScrollPosition = Vector2.zero;
    private bool stylesInitialized;
    private Texture2D toggleOffTex;
    private Texture2D toggleOnTex;
    private GUIStyle toggleStyle;
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
        Color originalContentColor = GUI.contentColor;
        Color originalColor = GUI.color;

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
        GUI.contentColor = originalContentColor;
        GUI.color = originalColor;
    }

    private void InitializeStyles()
    {
        if (stylesInitialized) return;

        windowBackgroundTex = CreateSolidColorTexture(new Color(0.08f, 0.08f, 0.12f, 0.92f));
        buttonNormalTex = CreateSolidColorTexture(new Color(0.15f, 0.15f, 0.22f, 1f));
        buttonHoverTex = CreateSolidColorTexture(new Color(0.22f, 0.22f, 0.32f, 1f));
        buttonActiveTex = CreateSolidColorTexture(new Color(0.28f, 0.28f, 0.42f, 1f));
        toggleOnTex = CreateSolidColorTexture(new Color(0.3f, 0.75f, 0.4f, 1.0f));
        toggleOffTex = CreateSolidColorTexture(new Color(0.25f, 0.25f, 0.3f, 1.0f));

        windowStyle = new GUIStyle(GUI.skin.window);
        windowStyle.normal.background = windowBackgroundTex;
        windowStyle.onNormal.background = windowBackgroundTex;
        windowStyle.border = new RectOffset(4, 4, 4, 4);
        windowStyle.padding = new RectOffset(12, 12, 15, 12);

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.normal.background = buttonNormalTex;
        buttonStyle.hover.background = buttonHoverTex;
        buttonStyle.active.background = buttonActiveTex;
        buttonStyle.normal.textColor = Color.white;
        buttonStyle.hover.textColor = new Color(0.9f, 0.9f, 1f, 1f);
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.alignment = TextAnchor.MiddleCenter;

        toggleStyle = new GUIStyle(GUI.skin.toggle);
        toggleStyle.normal.background = toggleOffTex;
        toggleStyle.hover.background = buttonHoverTex;
        toggleStyle.onNormal.background = toggleOnTex;
        toggleStyle.onHover.background = toggleOnTex;
        toggleStyle.normal.textColor = Color.white;
        toggleStyle.onNormal.textColor = Color.white;
        toggleStyle.fontStyle = FontStyle.Normal;
        toggleStyle.alignment = TextAnchor.MiddleLeft;
        toggleStyle.padding = new RectOffset(8, 4, 2, 2);

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.normal.textColor = new Color(0.85f, 0.85f, 0.9f, 1f);
        labelStyle.fontStyle = FontStyle.Normal;

        headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.normal.textColor = new Color(0.4f, 0.7f, 1f, 1f);
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.fontSize = 13;

        stylesInitialized = true;
    }

    private Texture2D CreateSolidColorTexture(Color color)
    {
        Texture2D texture = new(2, 2);
        Color[] colors = new Color[4];
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

        bool isAdmin = Main.instance != null && Main.instance.IsAdmin;

        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();

        int i = 0;
        foreach (var category in GemstoneMenuBackend.Categories)
        {
            if (category.AdminOnly && !isAdmin) continue;

            if (i > 0 && i % 4 == 0)
            {
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
            }

            if (GUILayout.Toggle(currentGuiTab == category.Id, category.NameKey, buttonStyle, GUILayout.Height(26)))
            {
                currentGuiTab = category.Id;
            }
            i++;
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.Space(8);

        if (currentGuiTab != -1)
        {
            DrawCategory(currentGuiTab);
        }
    }

    private void DrawCategory(int categoryId)
    {
        modScrollPosition = GUILayout.BeginScrollView(modScrollPosition, GUILayout.Width(300), GUILayout.Height(380));

        bool isAdmin = Main.instance != null && Main.instance.IsAdmin;

        if (categoryId == 7)
        {
            FieldInfo field = typeof(Main).GetField("soundboardClips", BindingFlags.NonPublic | BindingFlags.Instance);
            List<AudioClip> clips = field?.GetValue(Main.instance) as List<AudioClip>;

            if (clips != null && clips.Count > 0)
            {
                foreach (AudioClip clip in clips)
                {
                    if (GUILayout.Button(clip.name, buttonStyle, GUILayout.Height(25)))
                    {
                        Main.ToggleSoundboard(clip);
                        Main.instance.PlayClickSound();
                    }
                    GUILayout.Space(3);
                }
            }
            else
            {
                GUILayout.Label("No sounds found.", labelStyle);
            }
        }
        else
        {
            foreach (ModButton button in GemstoneMenuBackend.GetButtons(categoryId, isAdmin))
            {
                if (button.ToggleEntry != null)
                {
                    bool currentState = button.ToggleEntry.Value;
                    bool newState = GUILayout.Toggle(currentState, $" {button.Name}", toggleStyle, GUILayout.Height(22));
                    if (newState != currentState)
                    {
                        button.Press();
                        if (Main.instance != null)
                        {
                            Main.instance.Config.Save();
                            Main.instance.PlayClickSound();
                        }
                    }
                }
                else
                {
                    if (GUILayout.Button(button.Name, buttonStyle, GUILayout.Height(25)))
                    {
                        button.Press();
                        if (Main.instance != null) Main.instance.PlayClickSound();
                    }
                }
                GUILayout.Space(3);
            }
        }

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
}