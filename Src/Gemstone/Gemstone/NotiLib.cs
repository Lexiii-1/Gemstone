using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gemstone.Gemstone
{
    public class NotiLib : MonoBehaviour
    {
        public static NotiLib Instance;
        private static List<GameObject> notifications = new List<GameObject>();
        private static List<GameObject> pcNotifications = new List<GameObject>();
        private const float Y_OFFSET = 0.05f;
        private const float PC_Y_OFFSET = 30f;

        private static Canvas pcCanvas;

        public static void SendNotification(string message, float time)
        {
            if (Instance == null) return;

            var textNotifacation = new GameObject("NotificationLabel");
            textNotifacation.transform.SetParent(GorillaLocomotion.GTPlayer.Instance.bodyCollider.transform, false);
            textNotifacation.transform.localScale = Vector3.one * 0.0025f;
            notifications.Add(textNotifacation);
            int index = notifications.Count - 1;

            textNotifacation.transform.localPosition = new Vector3(0f, index * Y_OFFSET, 0.45f);
            textNotifacation.transform.LookAt(GorillaLocomotion.GTPlayer.Instance.headCollider.transform.position);
            textNotifacation.transform.Rotate(0f, 180f, 0f);

            var text = textNotifacation.AddComponent<TextMeshPro>();
            text.text = message;
            text.fontSize = 15f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.enableAutoSizing = true;
            text.rectTransform.sizeDelta = new Vector2(500f, 400f);
            text.transform.localScale = new Vector3(0.0025f, 0.0025f, 0.0025f);

            EnsureCanvasExists();

            var pcNotification = new GameObject("PCNotificationLabel");
            pcNotification.transform.SetParent(pcCanvas.transform, false);
            pcNotifications.Add(pcNotification);
            int pcIndex = pcNotifications.Count - 1;

            var pcText = pcNotification.AddComponent<TextMeshProUGUI>();

            if (TMP_Settings.defaultFontAsset != null)
            {
                pcText.font = TMP_Settings.defaultFontAsset;
            }

            pcText.text = message;
            pcText.fontSize = 18f;
            pcText.alignment = TextAlignmentOptions.Center;
            pcText.color = Color.white;
            pcText.raycastTarget = false;

            RectTransform rect = pcText.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(600f, 60f);

            UpdatePCNotificationPosition(pcNotification, pcIndex);

            Instance.StartCoroutine(Instance.DestroyAfterTime(textNotifacation, pcNotification, time));
        }

        private static void EnsureCanvasExists()
        {
            if (pcCanvas != null) return;

            var canvasObj = new GameObject("Gemstone_DesktopNotiCanvas");
            pcCanvas = canvasObj.AddComponent<Canvas>();
            pcCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            pcCanvas.sortingOrder = 999;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            canvasObj.AddComponent<GraphicRaycaster>();

            DontDestroyOnLoad(canvasObj);
        }

        private static void UpdatePCNotificationPosition(GameObject obj, int index)
        {
            if (obj == null) return;

            var rect = obj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = new Vector2(0f, index * PC_Y_OFFSET);
            }
        }

        private IEnumerator DestroyAfterTime(GameObject vrObj, GameObject pcObj, float time) // Deez, I like changing the fucking time on this - Lexi ()
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

        void Awake()
        {
            Instance = this;
        }
    }
}