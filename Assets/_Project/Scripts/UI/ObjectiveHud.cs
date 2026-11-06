using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Duzhe.UI
{
    /// Top-of-screen single objective line, a temporary toast line, and the
    /// quest-item icon row (C04: collected items show as small icons and are
    /// collapsed again once handed over). Only one primary action is ever
    /// shown at a time (spec section 5).
    public class ObjectiveHud : MonoBehaviour
    {
        private Text objectiveText;
        private Text toastText;
        private float toastRemaining;

        private readonly List<string> questItemIds = new List<string>();
        private readonly List<Image> questIcons = new List<Image>();

        private void Start()
        {
            BuildUi();
            Hide();
        }

        private void BuildUi()
        {
            if (objectiveText != null) return; // idempotent
            DuzheUiFactory.AddPanel(transform, "ObjectiveBg",
                new Vector2(340f, -8f), new Vector2(600f, 44f), new Color32(14, 12, 18, 190));
            objectiveText = DuzheUiFactory.AddLabel(transform, "ObjectiveLabel",
                new Vector2(350f, -13f), new Vector2(580f, 34f), 26,
                new Color32(238, 232, 218, 255), TextAnchor.MiddleCenter);

            toastText = DuzheUiFactory.AddLabel(transform, "ToastLabel",
                new Vector2(240f, -60f), new Vector2(800f, 34f), 26,
                new Color32(222, 204, 128, 255), TextAnchor.MiddleCenter);
            toastText.text = "";
        }

        public void SetCinematic(bool cinematic)
        {
            BuildUi();
            Transform background = transform.Find("ObjectiveBg");
            if (background != null) background.gameObject.SetActive(!cinematic);
            if (objectiveText != null) objectiveText.gameObject.SetActive(!cinematic);
        }

        public void SetObjective(string text)
        {
            BuildUi();
            SetCinematic(false);
            objectiveText.text = text;
        }

        public void ShowToast(string text)
        {
            BuildUi();
            toastText.text = text;
            toastRemaining = 2.5f;
        }

        /// Adds a quest-item icon at the top-right row (idempotent per id).
        public void ShowQuestItem(string itemId)
        {
            BuildUi();
            if (string.IsNullOrEmpty(itemId) || questItemIds.Contains(itemId)) return;
            questItemIds.Add(itemId);

            GameObject go = new GameObject("QuestItem_" + questItemIds.Count);
            go.transform.SetParent(transform, false);
            Image icon = go.AddComponent<Image>();
            icon.sprite = Core.PlaceholderArt.QuestIconSprite(itemId);
            icon.raycastTarget = false;
            RectTransform rt = icon.rectTransform;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 0.5f);
            // Row grows leftwards from the top-right corner, below the notch.
            rt.anchoredPosition = new Vector2(-16f - (questItemIds.Count - 1) * 48f, -30f);
            rt.sizeDelta = new Vector2(40f, 40f);
            questIcons.Add(icon);
        }

        /// Collapses every quest-item icon (both CH01 items returned).
        public void HideQuestItems()
        {
            foreach (Image icon in questIcons)
            {
                if (icon != null) Destroy(icon.gameObject);
            }
            questIcons.Clear();
            questItemIds.Clear();
        }

        public void Hide()
        {
            BuildUi();
            SetCinematic(false);
            objectiveText.text = "";
            toastText.text = "";
        }

        private void Update()
        {
            if (toastRemaining > 0f)
            {
                toastRemaining -= Time.unscaledDeltaTime;
                if (toastRemaining <= 0f && toastText != null) toastText.text = "";
            }
        }
    }
}
