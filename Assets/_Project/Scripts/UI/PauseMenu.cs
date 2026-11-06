using UnityEngine;
using UnityEngine.UI;

namespace Duzhe.UI
{
    /// ESC pause overlay. While open the game clock is frozen (timeScale=0),
    /// gameplay input is blocked and music is paused; UI stays interactive.
    /// ESC toggles without ever stacking panels, and the key that closes the
    /// menu is consumed so it cannot act on the world (InputRouter consumes
    /// on unpause). Volumes are persisted when the menu closes.
    public class PauseMenu : MonoBehaviour
    {
        private GameObject panel;
        private GameObject confirmPanel;
        private bool built;

        private void Start()
        {
            Build();
        }

        private void Build()
        {
            if (built) return;
            built = true;

            panel = new GameObject("PausePanel");
            panel.transform.SetParent(transform, false);
            DuzheUiFactory.MakeFullStretch(panel);
            DuzheUiFactory.AddFullStretchImage(panel.transform, "Dim", new Color32(0, 0, 0, 150), true);
            // Card top-left is 120 down from the canvas top. The y must be
            // NEGATIVE: anchoredPosition is measured upwards from the
            // top-left anchor, so a positive y drew the card above the screen
            // (its bottom edge landed on the "音乐" row).
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Two extra DEV rows below the back button need a taller card.
            const float cardHeight = 520f;
#else
            const float cardHeight = 480f;
#endif
            DuzheUiFactory.AddPanel(panel.transform, "Card",
                new Vector2(420f, -120f), new Vector2(440f, cardHeight), new Color32(24, 22, 30, 240));

            DuzheUiFactory.AddLabel(panel.transform, "Title",
                new Vector2(420f, -150f), new Vector2(440f, 44f), 34,
                new Color32(238, 232, 218, 255), TextAnchor.MiddleCenter).text =
                UiText.Get("pause.title", "已暂停");

            DuzheUiFactory.AddButton(panel.transform, "Resume",
                new Vector2(460f, -216f), new Vector2(360f, 48f), UiText.Get("button.resume", "继续游戏"), 24,
                () => Close());

            DuzheUiFactory.AddLabel(panel.transform, "VolMaster",
                new Vector2(460f, -290f), new Vector2(120f, 30f), 22,
                new Color32(180, 174, 162, 255), TextAnchor.MiddleLeft).text =
                UiText.Get("volume.master", "主音量");
            DuzheUiFactory.AddSlider(panel.transform, "VolMasterSlider",
                new Vector2(600f, -300f), new Vector2(180f, 20f),
                GameSessionSafe().Save != null ? GameSessionSafe().Save.MasterVolume : 1f,
                v => OnVolumeChanged(0, v));

            DuzheUiFactory.AddLabel(panel.transform, "VolMusic",
                new Vector2(460f, -340f), new Vector2(120f, 30f), 22,
                new Color32(180, 174, 162, 255), TextAnchor.MiddleLeft).text =
                UiText.Get("volume.music", "音乐");
            DuzheUiFactory.AddSlider(panel.transform, "VolMusicSlider",
                new Vector2(600f, -350f), new Vector2(180f, 20f),
                GameSessionSafe().Save != null ? GameSessionSafe().Save.MusicVolume : 1f,
                v => OnVolumeChanged(1, v));

            DuzheUiFactory.AddLabel(panel.transform, "VolSfx",
                new Vector2(460f, -390f), new Vector2(120f, 30f), 22,
                new Color32(180, 174, 162, 255), TextAnchor.MiddleLeft).text =
                UiText.Get("volume.sfx", "音效");
            DuzheUiFactory.AddSlider(panel.transform, "VolSfxSlider",
                new Vector2(600f, -400f), new Vector2(180f, 20f),
                GameSessionSafe().Save != null ? GameSessionSafe().Save.SfxVolume : 1f,
                v => OnVolumeChanged(2, v));

            DuzheUiFactory.AddButton(panel.transform, "BackToMenu",
                new Vector2(460f, -456f), new Vector2(172f, 48f), UiText.Get("button.return_menu", "返回开始界面"), 20,
                () => ShowConfirmLeave());
            // C09: completed chapters can be replayed straight from the pause
            // overlay (starting a replay tears the current run down).
            DuzheUiFactory.AddButton(panel.transform, "PauseChapters",
                new Vector2(648f, -456f), new Vector2(172f, 48f), "章节", 20,
                () =>
                {
                    Core.GameSession s = GameSessionSafe();
                    if (s != null && s.ChapterMenu != null) s.ChapterMenu.Show();
                });

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DuzheUiFactory.AddButton(panel.transform, "DevComplete",
                new Vector2(460f, -516f), new Vector2(172f, 44f), UiText.Get("pause_dev_complete", "DEV:完成当前章"), 18,
                () =>
                {
                    Core.GameSession s = GameSessionSafe();
                    if (s != null && s.Chapters != null) s.Chapters.DevMarkChapterCompleted();
                });
            DuzheUiFactory.AddButton(panel.transform, "DevClear",
                new Vector2(648f, -516f), new Vector2(172f, 44f), UiText.Get("pause_dev_clear", "DEV:清空存档"), 18,
                () =>
                {
                    Core.GameSession s = GameSessionSafe();
                    if (s != null && s.Save != null)
                    {
                        s.Save.ClearAll();
                        if (s.Hud != null) s.Hud.ShowToast(UiText.Get("pause_dev_cleared", "存档已清空"));
                    }
                });
            // DEV chapter picker: until C09 adds the real chapter list, this
            // is the only way to enter a chapter that the save already marks
            // completed (otherwise "start" replays CH01 once everything is
            // done). DEV-only; removed in the C11 release-cleanup pass.
            DuzheUiFactory.AddButton(panel.transform, "DevChapters",
                new Vector2(460f, -576f), new Vector2(172f, 44f), "DEV:选择章节", 18,
                () => ShowChapterPicker());
            // C11: the DEV minigame picker is development-only in shipped builds.
            DuzheUiFactory.AddButton(panel.transform, "DevMinigames",
                new Vector2(648f, -576f), new Vector2(172f, 44f), "DEV:机关测试", 18,
                () =>
                {
                    // Closes the pause overlay first, then opens the DEV
                    // minigame picker (never part of a shipped menu).
                    Close();
                    if (GameSessionSafe() != null && GameSessionSafe().Minigames != null)
                    {
                        GameSessionSafe().Minigames.OpenTester();
                    }
                });
#endif

            // Leave-chapter confirmation (spec section 9/10: warn that
            // in-chapter progress will be lost).
            confirmPanel = new GameObject("PauseConfirmPanel");
            confirmPanel.transform.SetParent(transform, false);
            DuzheUiFactory.MakeFullStretch(confirmPanel);
            DuzheUiFactory.AddFullStretchImage(confirmPanel.transform, "Dim2", new Color32(0, 0, 0, 180), true);
            DuzheUiFactory.AddPanel(confirmPanel.transform, "Card2",
                new Vector2(400f, -260f), new Vector2(480f, 200f), new Color32(24, 22, 30, 240));
            DuzheUiFactory.AddLabel(confirmPanel.transform, "Msg",
                new Vector2(420f, -290f), new Vector2(440f, 90f), 26,
                new Color32(238, 232, 218, 255), TextAnchor.UpperLeft).text =
                UiText.Get("confirm.leave_chapter_title", "离开当前章节？") + "\n" +
                UiText.Get("confirm.leave_chapter_body", "本章未完成的进度将不保留。已通关章节、回忆和音量设置会保留。");
            DuzheUiFactory.AddButton(confirmPanel.transform, "Yes",
                new Vector2(430f, -390f), new Vector2(200f, 48f), UiText.Get("button.confirm", "确认"), 22,
                () => ConfirmLeave());
            DuzheUiFactory.AddButton(confirmPanel.transform, "No",
                new Vector2(650f, -390f), new Vector2(200f, 48f), UiText.Get("button.cancel", "取消"), 22,
                () => HideConfirmLeave());
            confirmPanel.SetActive(false);

            panel.SetActive(false);
        }

        private Core.GameSession GameSessionSafe()
        {
            return Core.GameSession.Instance;
        }

        public bool IsOpen { get { return panel != null && panel.activeSelf; } }

        public void Open()
        {
            Build();
            if (IsOpen) return; // never stack
            // Always render above any runtime panel (river briefing/retry,
            // minigame panels, ...): the pause overlay is modal.
            panel.transform.SetAsLastSibling();
            panel.SetActive(true);
            HideConfirmLeave();
            Core.GameSession session = GameSessionSafe();
            if (session != null) session.SetPaused(true);
        }

        public void Close()
        {
            Build();
            if (!IsOpen) return;
            panel.SetActive(false);
            HideConfirmLeave();
            Core.GameSession session = GameSessionSafe();
            if (session != null)
            {
                // Persist volumes on menu close (spec: write on confirm/exit).
                if (session.Save != null) session.Save.SaveVolumes();
                session.SetPaused(false);
            }
        }

        private void ShowConfirmLeave()
        {
            if (confirmPanel == null) return;
            confirmPanel.transform.SetAsLastSibling(); // above the pause panel
            confirmPanel.SetActive(true);
        }

        private void HideConfirmLeave()
        {
            if (confirmPanel != null) confirmPanel.SetActive(false);
        }

        // --- DEV chapter picker (removed in the C11 cleanup pass) ---------

        private GameObject chapterPicker;

        private void ShowChapterPicker()
        {
            if (chapterPicker == null) BuildChapterPicker();
            chapterPicker.transform.SetAsLastSibling();
            chapterPicker.SetActive(true);
        }

        private void HideChapterPicker()
        {
            if (chapterPicker != null) chapterPicker.SetActive(false);
        }

        private void BuildChapterPicker()
        {
            chapterPicker = new GameObject("DevChapterPicker");
            chapterPicker.transform.SetParent(transform, false);
            DuzheUiFactory.MakeFullStretch(chapterPicker);
            DuzheUiFactory.AddFullStretchImage(chapterPicker.transform, "Dim3", new Color32(0, 0, 0, 190), true);
            DuzheUiFactory.AddPanel(chapterPicker.transform, "Card3",
                new Vector2(340f, -80f), new Vector2(600f, 560f), new Color32(24, 22, 30, 245));
            DuzheUiFactory.AddLabel(chapterPicker.transform, "PickerTitle",
                new Vector2(340f, -110f), new Vector2(600f, 40f), 28,
                new Color32(238, 232, 218, 255), TextAnchor.MiddleCenter).text =
                "DEV:选择章节（不计入验收）";

            string[] ids = { "CH01", "CH02", "CH03", "CH04", "CH05" };
            int row = 0;
            foreach (string id in ids)
            {
                if (Chapters.ChapterCatalog.GetChapter(id) == null) continue;
                string chapterId = id; // captured
                // C12: no "已通关/未完成" annotation on chapter rows.
                DuzheUiFactory.AddButton(chapterPicker.transform, "Pick_" + chapterId,
                    new Vector2(380f, -170f - row * 62f), new Vector2(520f, 52f),
                    chapterId + "  " + Chapters.ChapterCatalog.GetChapter(chapterId).displayName, 22,
                    () => StartChapterFromDev(chapterId));
                row++;
            }

            DuzheUiFactory.AddButton(chapterPicker.transform, "PickerClose",
                new Vector2(530f, -170f - row * 62f - 10f), new Vector2(220f, 48f), "取消", 22,
                () => HideChapterPicker());
            chapterPicker.SetActive(false);
        }

        private void StartChapterFromDev(string chapterId)
        {
            Core.GameSession s = GameSessionSafe();
            if (s == null || s.Chapters == null) return;
            HideChapterPicker();
            Close(); // unpause, persist volumes
            // Do not leak run-owned UI from the chapter we are leaving.
            if (s.Minigames != null && s.Minigames.IsOpen) s.Minigames.Close();
            if (s.Presentations != null) s.Presentations.ForceAbort();
            if (s.Credits != null) s.Credits.HideWithoutEvent();
            Presentation.PresentationCG.Hide();
            Presentation.BlackboardTextLayer.Hide();
            if (s.Dialogue != null) s.Dialogue.ForceStop();
            s.Chapters.AbandonCurrentRun();
            bool done = s.Save != null && s.Save.HasCompletedChapter(chapterId);
            s.Chapters.StartChapter(chapterId, done ? Core.GameMode.ChapterReplay : Core.GameMode.MainRun);
        }

        private void ConfirmLeave()
        {
            HideConfirmLeave();
            panel.SetActive(false);
            Core.GameSession session = GameSessionSafe();
            if (session != null)
            {
                if (session.Save != null) session.Save.SaveVolumes();
                session.SetPaused(false);
                // Leave must not leak run-owned UI into the menu: close any
                // open minigame panel, hard-stop the dialogue view and abort
                // a running presentation before the run is abandoned.
                if (session.Minigames != null && session.Minigames.IsOpen) session.Minigames.Close();
                if (session.Presentations != null) session.Presentations.ForceAbort();
                if (session.Credits != null) session.Credits.HideWithoutEvent();
                if (session.ChapterMenu != null) session.ChapterMenu.Hide();
                Presentation.PresentationCG.Hide();
                Presentation.BlackboardTextLayer.Hide();
                if (session.Dialogue != null) session.Dialogue.ForceStop();
                if (session.Chapters != null) session.Chapters.AbandonCurrentRun();
                if (session.Transitions != null) session.Transitions.LoadScene(Core.DuzheIds.SceneMainMenu);
            }
        }

        private void OnVolumeChanged(int which, float value)
        {
            Core.GameSession s = GameSessionSafe();
            if (s == null || s.Save == null || s.Audio == null) return;
            s.Save.SetVolumes(
                which == 0 ? value : s.Save.MasterVolume,
                which == 1 ? value : s.Save.MusicVolume,
                which == 2 ? value : s.Save.SfxVolume);
            s.Audio.ApplyVolumes(s.Save);
        }

        private void Update()
        {
            Core.InputRouter router = Core.InputRouter.Instance;
            if (router == null) return;
            if (!router.EscapePressed) return;
            Core.GameSession session = GameSessionSafe();
            if (session == null) return;

            // C12: a recollection replay has no pause overlay - ESC leaves the
            // memory and goes back to the gallery list.
            if (session.Mode == Core.GameMode.PresentationReplay)
            {
                if (session.Gallery != null) session.Gallery.LeaveReplay();
                else session.ReturnToMenu(Core.MenuTab.Gallery);
                return;
            }
            // While the chapter list is open it owns ESC (it returns to this
            // pause overlay); never close both with one press.
            if (session.ChapterMenu != null && session.ChapterMenu.IsShowing) return;

            if (session.Paused)
            {
                if (confirmPanel != null && confirmPanel.activeSelf) HideConfirmLeave();
                else Close();
            }
            else if (session.Mode != Core.GameMode.MainMenu && session.InputMode != Core.InputMode.Transition)
            {
                Open();
            }
        }
    }
}
