using Duzhe.Core;
using Duzhe.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Duzhe.Minigames
{
    /// MG02_Door: the door close-up panel. First click on the DOOR opens the
    /// panel (upstream trigger already did the stage gating), then only a
    /// click on the KNOCKER completes the knock and the panel closes by
    /// itself. Clicks elsewhere give no progress; the panel can be closed and
    /// reopened (the trigger stays interactive while its stage is active).
    public class DoorKnockMinigame : MinigamePanelBase
    {
        private Text statusText;
        private GameObject closeupBody;
        private GameObject knockZone;
        private bool knockDone;

        protected override void BuildUi(Transform parent, MinigameConfig cfg)
        {
            float left = 640f - cfg.panelW * 0.5f;
            float top = 360f - cfg.panelH * 0.5f;

            DuzheUiFactory.AddLabel(parent, "DoorTitle",
                new Vector2(640f - cfg.panelW * 0.5f, -(top - 44f)), new Vector2(cfg.panelW, 36f), 28,
                new Color32(238, 232, 218, 255), TextAnchor.MiddleCenter).text =
                MinigameCatalog.Title(cfg);

            // Close-up backdrop: the delivered PRP03 board (640x360) when
            // present, otherwise the procedural door.
            Sprite board = Resources.Load<Sprite>("Art/Props/PRP03_DoorCloseup");
            closeupBody = new GameObject("DoorCloseup");
            closeupBody.transform.SetParent(parent, false);
            Image backdrop = closeupBody.AddComponent<Image>();
            backdrop.color = board != null ? Color.white : new Color32(58, 48, 40, 255);
            if (board != null)
            {
                backdrop.sprite = board;
                backdrop.type = Image.Type.Simple;
                backdrop.raycastTarget = false; // only the knocker is clickable
            }
            RectTransform backdropRt = backdrop.rectTransform;
            backdropRt.anchorMin = new Vector2(0f, 1f);
            backdropRt.anchorMax = new Vector2(0f, 1f);
            backdropRt.pivot = new Vector2(0f, 1f);
            backdropRt.anchoredPosition = new Vector2(left, -top);
            backdropRt.sizeDelta = new Vector2(cfg.panelW, cfg.panelH);

            if (board == null)
            {
                // Procedural fallback door (kept only while the art is absent).
                // y is measured downwards from the board's top edge.
                DuzheUiFactory.AddPanel(closeupBody.transform, "DoorLeaf",
                    new Vector2(220f, -60f), new Vector2(200f, 280f), new Color32(104, 78, 52, 255));
                DuzheUiFactory.AddPanel(closeupBody.transform, "DoorFrame",
                    new Vector2(210f, -50f), new Vector2(220f, 300f), new Color32(70, 52, 36, 255));
                DuzheUiFactory.AddPanel(closeupBody.transform, "DoorLeafInner",
                    new Vector2(232f, -72f), new Vector2(176f, 256f), new Color32(120, 92, 62, 255));
            }

            DuzheUiFactory.AddLabel(closeupBody.transform, "KnockHint",
                new Vector2(320f, -320f), new Vector2(460f, 30f), 22,
                new Color32(222, 204, 128, 255), TextAnchor.MiddleCenter).text =
                UiText.Get("minigame.door.instructions", "点击门环，轻轻敲门。");

            // Knocker hotspot: geometry exported with the panel
            // (PropsHotspots.json) - centre (380,200) in the 640x360 board,
            // i.e. top-left (356,176) size 48x48. Invisible on the art.
            knockZone = new GameObject("KnockerZone");
            knockZone.transform.SetParent(closeupBody.transform, false);
            Image knockerImg = knockZone.AddComponent<Image>();
            knockerImg.color = board != null
                ? new Color(1f, 1f, 1f, 0.02f)
                : new Color32(200, 170, 110, 255);
            RectTransform kz = knockerImg.rectTransform;
            kz.anchorMin = new Vector2(0f, 1f);
            kz.anchorMax = new Vector2(0f, 1f);
            kz.pivot = new Vector2(0f, 1f);
            kz.anchoredPosition = new Vector2(356f, -176f);
            kz.sizeDelta = new Vector2(48f, 48f);
            var knockerHandler = knockZone.AddComponent<DoorKnockZone>();
            knockerHandler.owner = this;

            DuzheUiFactory.AddLabel(parent, "DoorStatus",
                new Vector2(640f - (cfg.panelW + 60f) * 0.5f, -(top + cfg.panelH + 12f)),
                new Vector2(cfg.panelW + 60f, 30f), 20,
                new Color32(180, 174, 162, 255), TextAnchor.MiddleCenter).text =
                UiText.Get("minigame.door.instructions", "点击门环，轻轻敲门。");

            DuzheUiFactory.AddButton(parent, "CloseDoor",
                new Vector2(left + cfg.panelW - 130f, -(top + cfg.panelH + 56f)), new Vector2(130f, 44f),
                UiText.Get("button.close", "关闭"), 20,
                () => { if (MinigameHost.Instance != null) MinigameHost.Instance.Close(); });
        }

        /// Called by the knocker zone handler.
        public void OnKnockerClicked()
        {
            if (knockDone) return;
            knockDone = true;
            Core.GameSession session = Core.GameSession.Instance;
            if (session != null && session.Audio != null) session.Audio.PlaySfx("SFX11_Knock");
            Debug.Log("[Duzhe] Door knocked: " + Config.minigameId);
            if (Host != null) Host.Complete(); // panel closes itself via Close()
        }

        public override void OnGamePaused()
        {
            // No drawable/draggable state to restore; the pause overlay covers
            // the panel and its knocker zone, so nothing can be clicked.
        }

        private class DoorKnockZone : MonoBehaviour, IPointerClickHandler
        {
            public DoorKnockMinigame owner;

            public void OnPointerClick(PointerEventData e)
            {
                if (owner != null) owner.OnKnockerClicked();
            }
        }
    }
}
