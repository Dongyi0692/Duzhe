using System.Collections.Generic;
using Duzhe.Core;
using Duzhe.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Duzhe.Minigames
{
    /// The drag-placement minigame panel for MG03_Stools (3 identical stools
    /// into 3 slots) and MG06_Photo (only the drummer is draggable into the
    /// single gap). Drop inside 24 UI px of a free slot snaps and locks; wrong
    /// placement returns the item to its origin; a pause while dragging
    /// returns the dragged item to its origin and keeps placed items.
    public class DragPlacementMinigame : MinigamePanelBase
    {
        // Generous snap radius (80 UI px ≈ 2.5x the slot half-height): the
        // player only needs to drop the item NEAR the slot, precision is not
        // the challenge of this minigame.
        private const float SnapRadius = 80f;

        private class DraggableObject
        {
            public GameObject go;
            public RectTransform rect;
            public Vector2 origin;
            public int lockedSlot = -1; // -1 = not placed
        }

        private readonly List<DraggableObject> items = new List<DraggableObject>();
        private readonly List<RectTransform> slotOutlines = new List<RectTransform>();
        private readonly bool[] slotOccupied;
        private DraggableObject dragging;
        private Text statusText;

        public DragPlacementMinigame()
        {
            slotOccupied = new bool[8];
        }

        protected override void BuildUi(Transform parent, MinigameConfig cfg)
        {
            // Panel card. NOTE on y signs: the factory anchor/pivot convention
            // puts elements BELOW the top edge at NEGATIVE y (verified against
            // every visible panel). The card background historically sat at
            // +y (mostly off-screen); its content must use negative y.
            float left = 640f - cfg.panelW * 0.5f;
            float top = 360f - cfg.panelH * 0.5f;
            // Card + chrome are centred on their own width; the BOARD art stays
            // at "left" so the delivered 640x360 panels keep their coordinates.
            float cardW = cfg.panelW + 40f;
            float cardLeft = 640f - cardW * 0.5f;
            // The card must actually be on screen (it used to be anchored at
            // +top, i.e. above the screen edge) and cover title -> close.
            DuzheUiFactory.AddPanel(parent, "DragCard",
                new Vector2(cardLeft, -(top - 56f)), new Vector2(cardW, cfg.panelH + 216f),
                new Color32(24, 22, 30, 246));
            DuzheUiFactory.AddLabel(parent, "DragTitle",
                new Vector2(cardLeft, -(top - 48f)),
                new Vector2(cardW, 36f), 28,
                new Color32(238, 232, 218, 255), TextAnchor.MiddleCenter).text =
                MinigameCatalog.Title(cfg);
            // Instructions only for MG03 (MG06 keeps the status line as its
            // hint to avoid overlapping the drummer's start spot).
            if (cfg.minigameId == "MG03_Stools")
            {
                DuzheUiFactory.AddLabel(parent, "DragInstructions",
                    new Vector2(cardLeft, -(top + 16f)),
                    new Vector2(cardW, 28f), 20,
                    new Color32(180, 174, 162, 255), TextAnchor.MiddleCenter).text =
                    UiText.Get("minigame.stools.instructions", "把三把凳子分别拖进空位，松开鼠标放下。");
            }
            statusText = DuzheUiFactory.AddLabel(parent, "DragStatus",
                new Vector2(cardLeft, -(top + cfg.panelH + 56f)),
                new Vector2(cardW, 30f), 20,
                new Color32(180, 174, 162, 255), TextAnchor.MiddleCenter);

            if (cfg.minigameId == "MG03_Stools") BuildStools(parent, cfg, left, top);
            else if (cfg.minigameId == "MG06_Photo") BuildPhoto(parent, cfg, left, top);

            DuzheUiFactory.AddButton(parent, "CloseDrag",
                new Vector2(cardLeft + cardW - 150f, -(top + cfg.panelH + 108f)), new Vector2(150f, 44f),
                UiText.Get("button.close", "关闭"), 20,
                () => { if (MinigameHost.Instance != null) MinigameHost.Instance.Close(); });

            RefreshStatus();
        }

        // -- MG03_Stools -----------------------------------------------------

        private void BuildStools(Transform parent, MinigameConfig cfg, float left, float top)
        {
            // All y values are NEGATIVE (downward from the canvas top), matching
            // the top-left anchor convention of AddPanel/AddButton.
            Sprite stool = Resources.Load<Sprite>("Art/Props/PRP04_Stool");
            if (stool == null) stool = Core.PlaceholderArt.LoadStoolSprite();
            Sprite table = Resources.Load<Sprite>("Art/Props/PRP05_DiningTable");
            float tableTop = top + cfg.panelH * 0.62f;
            // Table marker between the slots (delivered art when present).
            if (table != null)
            {
                DuzheUiFactory.AddSpritePanel(parent, "DiningTable",
                    new Vector2(left + cfg.panelW * 0.5f - 48f, -tableTop),
                    new Vector2(96f, 64f), table);
            }
            else
            {
                DuzheUiFactory.AddPanel(parent, "DiningTable",
                    new Vector2(left + cfg.panelW * 0.5f - 48f, -tableTop),
                    new Vector2(96f, 64f), new Color32(96, 76, 52, 255));
            }

            // Three slot outlines above the table, evenly spaced.
            for (int i = 0; i < cfg.slotCount; i++)
            {
                Vector2 slotPos = new Vector2(
                    left + cfg.panelW * (0.22f + 0.28f * i), -(top + cfg.panelH * 0.38f));
                GameObject slot = new GameObject("StoolSlot_" + i);
                slot.transform.SetParent(parent, false);
                Image outline = slot.AddComponent<Image>();
                outline.color = new Color32(90, 90, 104, 140);
                outline.raycastTarget = false;
                RectTransform rt = outline.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = slotPos;
                rt.sizeDelta = new Vector2(40f, 40f);
                slotOutlines.Add(rt); // screen-space snap uses actual rects
            }

            // Three identical stools to drag, starting below the instructions
            // line (y -252 center: clear of the 196..224 instructions band).
            for (int i = 0; i < cfg.slotCount; i++)
            {
                Vector2 origin = new Vector2(
                    left + cfg.panelW * (0.25f + 0.25f * i), -(top + cfg.panelH * 0.20f));
                CreateDraggable(parent, "Stool_" + i, origin, stool, new Vector2(40f, 40f),
                    new Color32(212, 178, 74, 255), cfg);
            }
        }

        // -- MG06_Photo ------------------------------------------------------

        private void BuildPhoto(Transform parent, MinigameConfig cfg, float left, float top)
        {
            // PRP06 already bakes the three fixed members. Draw the formal
            // board only; adding program bandmate blocks here would duplicate
            // them (the "重影" reported in playtest).
            Sprite board = Resources.Load<Sprite>("Art/Props/PRP06_PhotoFrame");
            if (board != null)
            {
                DuzheUiFactory.AddSpritePanel(parent, "PhotoFrameArt",
                    new Vector2(left, -top), new Vector2(cfg.panelW, cfg.panelH), board);
            }
            else
            {
                DuzheUiFactory.AddPanel(parent, "PhotoFrameFallback",
                    new Vector2(left + 20f, -(top + 40f)), new Vector2(cfg.panelW - 40f, cfg.panelH - 60f),
                    new Color32(70, 62, 54, 255));
            }

            // Match the group photo: only the leftmost drummer position is empty.
            // The fixed members are purple jacket, blue jacket, yellow jacket.
            // Geometry is in the board's top-left coordinates, from the config.
            Color gapColor = new Color32(90, 90, 104, 120);
            slotOutlines.Add(DuzheUiFactory.AddPanel(parent, "DrummerGap",
                new Vector2(left + cfg.photoGapX, -(top + cfg.photoGapY)),
                new Vector2(cfg.photoGapW, cfg.photoGapH), gapColor));

            Sprite drummerSprite = Resources.Load<Sprite>("Art/Characters/CHR07_DrummerSpirit");
            Color drummerColor = drummerSprite != null
                ? Color.white
                : new Color32(212, 178, 74, 255);
            CreateDraggable(parent, "Drummer",
                new Vector2(left + cfg.photoDrummerX + cfg.photoDrummerW * 0.5f,
                    -(top + cfg.photoDrummerY + cfg.photoDrummerH * 0.5f)),
                drummerSprite, new Vector2(cfg.photoDrummerW, cfg.photoDrummerH),
                drummerColor, cfg);
        }

        // -- draggable plumbing ----------------------------------------------

        private void CreateDraggable(Transform parent, string name, Vector2 origin, Sprite sprite,
            Vector2 size, Color color, MinigameConfig cfg)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            if (sprite != null) img.sprite = sprite;
            img.color = color;
            img.preserveAspect = sprite != null;
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = origin;
            rt.sizeDelta = size;

            DraggableObject item = new DraggableObject { go = go, rect = rt, origin = origin };
            var handler = go.AddComponent<DragHandler>();
            handler.owner = this;
            handler.item = item;
            items.Add(item);
        }

        private class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            public DragPlacementMinigame owner;
            public DragPlacementMinigame.DraggableObject item;
            private Vector2 dragOffset; // panel-space offset: item center minus pointer at drag start

            public void OnBeginDrag(PointerEventData e)
            {
                if (owner == null) return;
                if (MinigameHost.IsPausedForPanel()) return; // pause blocks dragging
                if (item.lockedSlot >= 0) return;           // placed items are locked
                owner.dragging = item;
                // Record the pointer->item offset so the item does NOT jump to
                // the pointer on the first drag frame (click must not shift it).
                Vector2 p = owner.ScreenToPanel(e.position);
                dragOffset = new Vector2(item.rect.anchoredPosition.x - p.x,
                                         -item.rect.anchoredPosition.y - p.y);
                // Bring the dragged item to the front so it is always visible
                // while being dragged (spec: dragging must be visible).
                item.go.transform.SetAsLastSibling();
            }

            public void OnDrag(PointerEventData e)
            {
                if (owner == null || owner.dragging != item) return;
                if (MinigameHost.IsPausedForPanel()) return;
                // ScreenToPanel returns top-left-origin coords (y DOWN);
                // anchoredPosition with anchor (0,1) needs y UP, so negate.
                Vector2 p = owner.ScreenToPanel(e.position);
                item.rect.anchoredPosition = new Vector2(p.x + dragOffset.x,
                                                         -(p.y + dragOffset.y));
            }

            public void OnEndDrag(PointerEventData e)
            {
                if (owner == null || owner.dragging != item) return;
                owner.dragging = null;
                owner.TrySnap(item);
            }
        }

        public static bool IsPausedForPanel()
        {
            Core.GameSession s = Core.GameSession.Instance;
            return s != null && s.Paused;
        }

        private Vector2 ScreenToPanel(Vector3 screenPos)
        {
            // Convert screen to the panel card's local UI coordinates via the
            // canvas (canvas is scaled 1280x720 reference).
            GameObject canvas = GameObject.Find(GameBootstrap.UiCanvasName);
            RectTransform canvasRt = canvas != null ? canvas.transform as RectTransform : null;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRt, screenPos, null, out local);
            // Canvas local: pivot center, y up. Convert to panel-card anchored
            // space (top-left origin of the 1280x720 reference):
            return new Vector2(local.x + 640f, 360f - local.y);
        }

        private void TrySnap(DraggableObject item)
        {
            // Screen-space snap: compare the item's actual on-screen center with
            // each slot outline's actual on-screen center (GetWorldCorners
            // returns screen pixels in ScreenSpaceOverlay). Visuals and snap
            // math always agree, no matter the anchor/pivot conventions used
            // when the panels were built.
            Vector3[] c = new Vector3[4];
            item.rect.GetWorldCorners(c);
            Vector2 itemCenter = (c[0] + c[2]) * 0.5f;

            float bestDist = float.MaxValue;
            int bestSlot = -1;
            Vector2 bestCenter = Vector2.zero;
            for (int i = 0; i < slotOutlines.Count; i++)
            {
                if (slotOccupied[i]) continue;
                slotOutlines[i].GetWorldCorners(c);
                Vector2 slotCenter = (c[0] + c[2]) * 0.5f;
                float d = Vector2.Distance(itemCenter, slotCenter);
                if (d < bestDist) { bestDist = d; bestSlot = i; bestCenter = slotCenter; }
            }

            if (bestSlot >= 0 && bestDist <= SnapRadius)
            {
                slotOccupied[bestSlot] = true;
                item.lockedSlot = bestSlot;
                // Put the item's center exactly on the slot's center, converted
                // back into panel-local coords (panel pivot = top-left).
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)PanelGO.transform, bestCenter, null, out local);
                item.rect.anchoredPosition = local;
                Image img = item.go.GetComponent<Image>();
                if (img != null) img.color = new Color(img.color.r, img.color.g, img.color.b, 1f);
                RefreshStatus();
                CheckCompletion();
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[Duzhe] SNAP FAIL item=" + item.go.name +
                " screenCenter=" + itemCenter.ToString("F0") +
                " nearestSlot=" + bestSlot + " dist=" + bestDist.ToString("F1") +
                " radius=" + SnapRadius.ToString("F0"));
#endif
            // Wrong placement: back to origin.
            item.rect.anchoredPosition = item.origin;
            RefreshStatus();
        }

        private int PlacedCount()
        {
            int n = 0;
            foreach (DraggableObject item in items)
            {
                if (item.lockedSlot >= 0) n++;
            }
            return n;
        }

        private void RefreshStatus()
        {
            if (statusText == null) return;
            if (Config.minigameId == "MG03_Stools")
            {
                string text = UiText.Get("minigame.stools.progress", "已摆好 {count} / {total}");
                statusText.text = text
                    .Replace("{count}", PlacedCount().ToString())
                    .Replace("{total}", Config.slotCount.ToString());
            }
            else
            {
                statusText.text = UiText.Get("minigame.photo.instructions", "将鼓手拖进合照的空位，松开鼠标放下。");
            }
        }

        private void CheckCompletion()
        {
            // slotCount is the number of PEOPLE on the board (MG06: four),
            // not the number of draggable pieces (MG06: only the drummer).
            int required = RequiredPlacements(Config);
            if (required > 0 && PlacedCount() >= required && Host != null)
            {
                Host.Complete();
            }
        }

        /// How many successful drops finish the board. MG06 keeps slotCount=4
        /// (four people in the photo) but only the drummer is draggable, so a
        /// single correct drop completes it; MG03 needs all three stools.
        public static int RequiredPlacements(MinigameConfig cfg)
        {
            if (cfg == null) return 0;
            if (cfg.minigameId == "MG06_Photo") return 1;
            return cfg.slotCount;
        }

        public override void OnGamePaused()
        {
            // Spec: a pause while dragging returns the UNFINISHED item to its
            // origin; placed items stay locked. Also cancel an active drag.
            dragging = null;
            foreach (DraggableObject item in items)
            {
                if (item.lockedSlot < 0) item.rect.anchoredPosition = item.origin;
            }
        }
    }
}
