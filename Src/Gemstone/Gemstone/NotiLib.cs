using System.Collections;
using GorillaLocomotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gemstone.Gemstone;

public class NotiLib : MonoBehaviour
{
    private const           float            Y_OFFSET    = 0.05f;
    private const           float            PC_Y_OFFSET = 30f;
    public static           NotiLib          Instance;
    private static readonly List<GameObject> notifications   = new();
    private static readonly List<GameObject> pcNotifications = new();

    private static Canvas pcCanvas;

    private void Awake()
    {
        Instance = this;
    }

    public static void SendNotification(string message, float time)
    {
        if (Instance == null) return;

        GameObject textNotifacation = new("NotificationLabel");
        textNotifacation.transform.SetParent(GTPlayer.Instance.bodyCollider.transform, false);
        textNotifacation.transform.localScale = Vector3.one * 0.0025f;
        notifications.Add(textNotifacation);
        int index = notifications.Count - 1;

        textNotifacation.transform.localPosition = new Vector3(0f, index * Y_OFFSET, 0.45f);
        textNotifacation.transform.LookAt(GTPlayer.Instance.headCollider.transform.position);
        textNotifacation.transform.Rotate(0f, 180f, 0f);

        TextMeshPro? text = textNotifacation.AddComponent<TextMeshPro>();
        text.text                    = message;
        text.font                    = VRRig.LocalRig.playerText1.font;
        text.fontSize                = 15f;
        text.alignment               = TextAlignmentOptions.Center;
        text.color                   = Color.white;
        text.enableAutoSizing        = true;
        text.rectTransform.sizeDelta = new Vector2(500f, 400f);
        text.transform.localScale    = new Vector3(0.0025f, 0.0025f, 0.0025f);

        EnsureCanvasExists();
        GameObject pcNotification = new("PCNotificationLabel");
        pcNotification.transform.SetParent(pcCanvas.transform, false);
        pcNotifications.Add(pcNotification);
        int pcIndex = pcNotifications.Count - 1;

        TextMeshProUGUI? pcText = pcNotification.AddComponent<TextMeshProUGUI>();

        if (TMP_Settings.defaultFontAsset != null)
            pcText.font = TMP_Settings.defaultFontAsset;

        pcText.text          = message;
        pcText.font          = VRRig.LocalRig.playerText1.font;
        pcText.fontSize      = 18f;
        pcText.alignment     = TextAlignmentOptions.Center;
        pcText.color         = Color.white;
        pcText.raycastTarget = false;

        RectTransform rect = pcText.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot     = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(600f, 60f);

        UpdatePCNotificationPosition(pcNotification, pcIndex);

        Instance.StartCoroutine(Instance.DestroyAfterTime(textNotifacation, pcNotification, time));
    }

    private static void EnsureCanvasExists()
    {
        if (pcCanvas != null) return;

        GameObject canvasObj = new("Gemstone_DesktopNotiCanvas");
        pcCanvas              = canvasObj.AddComponent<Canvas>();
        pcCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
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
            rect.anchoredPosition = new Vector2(0f, index * PC_Y_OFFSET);
    }

    private IEnumerator
            DestroyAfterTime(GameObject vrObj, GameObject pcObj,
                             float      time)
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
    }

    private static void FixNotifPos()
    {
        for (int i = 0; i < notifications.Count; i++)
        {
            if (notifications[i] == null) continue;

            notifications[i].transform.localPosition = new Vector3(0f, i * Y_OFFSET, 0.45f);
        }

        for (int i = 0; i < pcNotifications.Count; i++)
        {
            if (pcNotifications[i] == null) continue;

            UpdatePCNotificationPosition(pcNotifications[i], i);
        }
    }
}