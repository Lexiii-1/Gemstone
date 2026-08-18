using System.Collections;
using System.Collections.Generic;
using GorillaLocomotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gemstone.Gemstone;

public class NotiLib : MonoBehaviour
{
    private const float Y_OFFSET = 0.05f;
    private const float PC_Y_OFFSET = 30f;
    public static NotiLib Instance;
    private static readonly List<GameObject> notifications = new();
    private static readonly List<GameObject> pcNotifications = new();

    private static Canvas pcCanvas;

    private static GameObject persistentVrOverlay;
    private static GameObject persistentPcOverlay;
    private static TextMeshPro persistentVrText;
    private static TextMeshProUGUI persistentPcText;

    private static int lastOverlayFrame = -1;

    private void Awake()
    {
        Instance = this;
    }

    private void LateUpdate()
    {
        if (Time.frameCount > lastOverlayFrame + 1)
        {
            if (persistentVrOverlay != null)
            {
                Destroy(persistentVrOverlay);
                persistentVrOverlay = null;
                persistentVrText = null;
            }

            if (persistentPcOverlay != null)
            {
                Destroy(persistentPcOverlay);
                persistentPcOverlay = null;
                persistentPcText = null;
            }
        }
    }

    public static void Overlay(string message)
    {
        if (Instance == null) return;

        lastOverlayFrame = Time.frameCount;

        if (persistentVrOverlay == null)
        {
            persistentVrOverlay = new("PersistentOverlayLabel");
            if (GTPlayer.Instance != null && GTPlayer.Instance.bodyCollider != null)
            {
                persistentVrOverlay.transform.SetParent(GTPlayer.Instance.bodyCollider.transform, false);
            }
            persistentVrOverlay.transform.localScale = Vector3.one * 0.0025f;
            persistentVrOverlay.transform.localPosition = new Vector3(0f, -Y_OFFSET, 0.45f);
            if (GTPlayer.Instance != null && GTPlayer.Instance.headCollider != null)
            {
                persistentVrOverlay.transform.LookAt(GTPlayer.Instance.headCollider.transform.position);
                persistentVrOverlay.transform.Rotate(0f, 180f, 0f);
            }

            persistentVrText = persistentVrOverlay.AddComponent<TextMeshPro>();
            persistentVrText.fontSize = 15f;
            persistentVrText.alignment = TextAlignmentOptions.Center;
            persistentVrText.color = Color.white;
            persistentVrText.enableAutoSizing = true;
            persistentVrText.rectTransform.sizeDelta = new Vector2(500f, 400f);
            persistentVrText.transform.localScale = new Vector3(0.0025f, 0.0025f, 0.0025f);
            if (VRRig.LocalRig != null && VRRig.LocalRig.playerText1 != null)
            {
                persistentVrText.font = VRRig.LocalRig.playerText1.font;
            }

            ApplyOverlayShader(persistentVrText, "PersistentVrOverlay");
        }

        persistentVrText.text = message;

        EnsureCanvasExists();
        if (persistentPcOverlay == null)
        {
            persistentPcOverlay = new("PersistentPCOverlayLabel");
            persistentPcOverlay.transform.SetParent(pcCanvas.transform, false);

            persistentPcText = persistentPcOverlay.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
                persistentPcText.font = TMP_Settings.defaultFontAsset;

            if (VRRig.LocalRig != null && VRRig.LocalRig.playerText1 != null)
            {
                persistentPcText.font = VRRig.LocalRig.playerText1.font;
            }

            persistentPcText.fontSize = 18f;
            persistentPcText.alignment = TextAlignmentOptions.Center;
            persistentPcText.color = Color.white;
            persistentPcText.raycastTarget = false;

            ApplyOverlayShader(persistentPcText, "PersistentPcOverlay");

            RectTransform rect = persistentPcText.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(600f, 60f);
        }

        persistentPcText.text = message;
        UpdatePCZIndices();
    }

    public static void SendNotification(string message, float time)
    {
        if (Instance == null) return;

        GameObject textNotifacation = new("NotificationLabel");
        textNotifacation.transform.SetParent(GTPlayer.Instance.bodyCollider.transform, false);
        textNotifacation.transform.localScale = Vector3.one * 0.0025f;
        notifications.Add(textNotifacation);
        int index = notifications.Count - 1;

        textNotifacation.transform.localPosition = new Vector3(0f, (index + 1) * Y_OFFSET, 0.45f);
        textNotifacation.transform.LookAt(GTPlayer.Instance.headCollider.transform.position);
        textNotifacation.transform.Rotate(0f, 180f, 0f);

        TextMeshPro? text = textNotifacation.AddComponent<TextMeshPro>();
        text.text = message;
        text.font = VRRig.LocalRig.playerText1.font;
        text.fontSize = 15f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.enableAutoSizing = true;
        text.rectTransform.sizeDelta = new Vector2(500f, 400f);
        text.transform.localScale = new Vector3(0.0025f, 0.0025f, 0.0025f);

        ApplyOverlayShader(text, $"VRNotification_{index}");

        EnsureCanvasExists();
        GameObject pcNotification = new("PCNotificationLabel");
        pcNotification.transform.SetParent(pcCanvas.transform, false);
        pcNotifications.Add(pcNotification);
        int pcIndex = pcNotifications.Count - 1;

        TextMeshProUGUI? pcText = pcNotification.AddComponent<TextMeshProUGUI>();

        if (TMP_Settings.defaultFontAsset != null)
            pcText.font = TMP_Settings.defaultFontAsset;

        pcText.text = message;
        pcText.font = VRRig.LocalRig.playerText1.font;
        pcText.fontSize = 18f;
        pcText.alignment = TextAlignmentOptions.Center;
        pcText.color = Color.white;
        pcText.raycastTarget = false;

        ApplyOverlayShader(pcText, $"PCNotification_{pcIndex}");

        RectTransform rect = pcText.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(600f, 60f);

        UpdatePCNotificationPosition(pcNotification, pcIndex);
        UpdatePCZIndices();

        Instance.StartCoroutine(Instance.DestroyAfterTime(textNotifacation, pcNotification, time));
    }

    private static void ApplyOverlayShader(TMP_Text tmpComponent, string labelId)
    {
        if (tmpComponent == null)
        {
            Debug.LogWarning($"[NotiLib] ApplyOverlayShader failed: tmpComponent is null for {labelId}");
            return;
        }

        string[] candidateShaders = new string[]
        {
            "TextMeshPro/Distance Field Overlay",
            "TextMeshPro/Mobile/Distance Field Overlay",
            "GUI/Text Shader",
            "Unlit/Transparent",
            "Standard"
        };

        Shader overlayShader = null;
        foreach (string shaderName in candidateShaders)
        {
            overlayShader = Shader.Find(shaderName);
            if (overlayShader != null)
            {
                Debug.Log($"[NotiLib] Successfully found candidate shader '{shaderName}' for [{labelId}].");
                break;
            }
            else
            {
                Debug.Log($"[NotiLib] Candidate shader '{shaderName}' not found for [{labelId}]. Trying next...");
            }
        }

        if (overlayShader != null)
        {
            Material overlayMat = new Material(overlayShader);
            if (tmpComponent.font != null && tmpComponent.font.material != null)
            {
                overlayMat.CopyPropertiesFromMaterial(tmpComponent.font.material);
            }
            overlayMat.shaderKeywords = tmpComponent.fontMaterial.shaderKeywords;
            overlayMat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            overlayMat.renderQueue = 4000;
            tmpComponent.fontSharedMaterial = overlayMat;

            Debug.Log($"[NotiLib] Applied overlay shader to [{labelId}]. Final Material: {overlayMat.name}, Shader: {overlayMat.shader.name}, RenderQueue: {overlayMat.renderQueue}, ZTest: {overlayMat.GetInt("_ZTest")}");
        }
        else
        {
            Debug.LogWarning($"[NotiLib] All candidate shaders failed for [{labelId}]. Forcing ZTest Always directly on existing font material.");
            if (tmpComponent.fontSharedMaterial != null)
            {
                tmpComponent.fontSharedMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
                tmpComponent.fontSharedMaterial.renderQueue = 4000;
            }
        }
    }

    private static void EnsureCanvasExists()
    {
        if (pcCanvas != null) return;

        GameObject canvasObj = new("Gemstone_DesktopNotiCanvas");
        pcCanvas = canvasObj.AddComponent<Canvas>();
        pcCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        pcCanvas.sortingOrder = 999;

        CanvasScaler? scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

        canvasObj.AddComponent<GraphicRaycaster>();

        DontDestroyOnLoad(canvasObj);
    }

    private static void UpdatePCNotificationPosition(GameObject obj, int index)
    {
        if (obj == null) return;

        RectTransform? rect = obj.GetComponent<RectTransform>();
        if (rect != null)
            rect.anchoredPosition = new Vector2(0f, (index + 1) * PC_Y_OFFSET);
    }

    private static void UpdatePCZIndices()
    {
        if (persistentPcOverlay != null)
        {
            persistentPcOverlay.transform.SetAsFirstSibling();
        }

        for (int i = 0; i < pcNotifications.Count; i++)
        {
            if (pcNotifications[i] != null)
            {
                pcNotifications[i].transform.SetAsLastSibling();
            }
        }
    }

    private IEnumerator DestroyAfterTime(GameObject vrObj, GameObject pcObj, float time)
    {
        yield return new WaitForSeconds(time / 1000.0f);

        if (notifications.Contains(vrObj))
        {
            notifications.Remove(vrObj);
            Destroy(vrObj);
        }

        if (pcNotifications.Contains(pcObj))
        {
            pcNotifications.Remove(pcObj);
            Destroy(pcObj);
        }

        FixNotifPos();
        UpdatePCZIndices();
    }

    private static void FixNotifPos()
    {
        if (persistentVrOverlay != null)
        {
            persistentVrOverlay.transform.localPosition = new Vector3(0f, -Y_OFFSET, 0.45f);
        }

        for (int i = 0; i < notifications.Count; i++)
        {
            if (notifications[i] == null) continue;

            notifications[i].transform.localPosition = new Vector3(0f, (i + 1) * Y_OFFSET, 0.45f);
        }

        if (persistentPcOverlay != null)
        {
            RectTransform persistentRect = persistentPcOverlay.GetComponent<RectTransform>();
            if (persistentRect != null)
            {
                persistentRect.anchoredPosition = new Vector2(0f, -PC_Y_OFFSET);
            }
        }

        for (int i = 0; i < pcNotifications.Count; i++)
        {
            if (pcNotifications[i] == null) continue;

            UpdatePCNotificationPosition(pcNotifications[i], i);
        }
    }
}