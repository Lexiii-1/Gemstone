using System.Reflection;
using BepInEx;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

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
    private Vector2 modScrollPosition = Vector2.zero;

    private Rect connectionWindowRect = new(HiddenX, 20, 250, 160);
    private Rect modsWindowRect = new(HiddenX, 20, 320, 600);

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

        connectionWindowRect = GUI.Window(0, connectionWindowRect, DrawConnectionWindow, "");
        modsWindowRect = GUI.Window(1, modsWindowRect, DrawModsWindow, "");
    }

    private void DrawConnectionWindow(int windowID)
    {
        GUI.DragWindow(new Rect(0, 0, 250, 25));

        if (GUILayout.Button("Disconnect"))
        {
            PhotonNetwork.Disconnect();
        }

        if (GUILayout.Button("Quit"))
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

            if (GUILayout.Toggle(currentGuiTab == category.Id, category.NameKey, GUILayout.Width(buttonWidth)))
            {
                currentGuiTab = category.Id;
            }

            count++;
        }

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.Space(10);

        if (currentGuiTab != -1)
        {
            DrawCategory(currentGuiTab);
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
                    if (GUILayout.Button(clip.name))
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
                    if (GUILayout.Button(player.NickName))
                    {
                        NotiLib.SendNotification($"Selected: {player.NickName}", 2000f);
                    }
                }
            }
        }
        else
        {
            foreach (ModButton button in GemstoneMenuBackend.GetButtons(categoryId, isAdmin))
            {
                if (button.ToggleEntry != null)
                {
                    bool state = GUILayout.Toggle(button.ToggleEntry.Value, button.Name);

                    if (state != button.ToggleEntry.Value)
                    {
                        button.Press();
                    }
                }
                else if (GUILayout.Button(button.Name))
                {
                    button.Press();
                }
            }
        }

        GUILayout.EndScrollView();
    }
}