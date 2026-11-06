using Duzhe.Chapters;
using Duzhe.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Duzhe.UI
{
    /// C09 start menu with four pages: Home (title / continue / entry
    /// buttons), Chapters (completed chapters only), Gallery (unlocked
    /// farewells) and Settings (volumes). The page is chosen through
    /// GameSession.PendingMenuTab so "return to the chapter list" after a
    /// replay lands on the right page.
    public class MainMenuPanel : MonoBehaviour
    {
        private GameObject panel;
        private GameObject homePage;
        private GameObject chaptersPage;
        private GameObject galleryPage;
        private GameObject settingsPage;
        private Transform chapterRows;
        private Transform galleryRows;
        private Text progressText;
        private Slider masterSlider;
        private Slider musicSlider;
        private Slider sfxSlider;
        private Text windowLabel;
        private Text resolutionLabel;
        private MenuTab currentPage = MenuTab.Home;
        private bool built;
        private GameObject aiNotice;

        private void Start()
        {
            Build();
            Refresh();
        }

        private void Build()
        {
            if (built) return;
            built = true;

            panel = new GameObject("MainMenuPanel");
            panel.transform.SetParent(transform, false);
            DuzheUiFactory.MakeFullStretch(panel);
            Sprite menuArt = Resources.Load<Sprite>("Art/Backgrounds/BG00_MainMenu");
            if (menuArt != null)
            {
                UnityEngine.UI.Image background = DuzheUiFactory.AddFullStretchImage(
                    panel.transform, "MenuArt", Color.white, false);
                background.sprite = menuArt;
                UnityEngine.UI.AspectRatioFitter fit = background.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
                fit.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = menuArt.rect.width / menuArt.rect.height;
            }
            DuzheUiFactory.AddFullStretchImage(panel.transform, "Backdrop", new Color32(0, 0, 0, 36), false);

            BuildHomePage();
            BuildChaptersPage();
            BuildGalleryPage();
            BuildSettingsPage();
            BuildAiNotice();
            panel.SetActive(false);
        }

        private void BuildHomePage()
        {
            homePage = new GameObject("MenuHomePage");
            homePage.transform.SetParent(panel.transform, false);
            RectTransform homeRect = DuzheUiFactory.MakeFullStretch(homePage);
            homeRect.anchorMin = homeRect.anchorMax = new Vector2(0.5f, 0.5f);
            homeRect.sizeDelta = new Vector2(1280f, 720f);
            homeRect.anchoredPosition = new Vector2(-260f, 45f);

            DuzheUiFactory.AddLabel(homePage.transform, "Title",
                new Vector2(340f, -200f), new Vector2(600f, 80f), 72,
                new Color32(238, 232, 218, 255), TextAnchor.MiddleCenter).text =
                UiText.Get("game.title", "渡者");
            DuzheUiFactory.AddLabel(homePage.transform, "Subtitle",
                new Vector2(340f, -286f), new Vector2(600f, 34f), 24,
                new Color32(212, 211, 200, 255), TextAnchor.MiddleCenter).text =
                UiText.Get("menu.subtitle", "把没说完的话，好好说完。");

            DuzheUiFactory.AddButton(homePage.transform, "Start",
                new Vector2(540f, -360f), new Vector2(200f, 52f), UiText.Get("menu.start", "开始故事"), 26,
                () => StartGame());
            DuzheUiFactory.AddButton(homePage.transform, "MenuChapters",
                new Vector2(540f, -424f), new Vector2(200f, 52f), "章节目录", 24,
                () => SetPage(MenuTab.Chapters));
            DuzheUiFactory.AddButton(homePage.transform, "MenuGallery",
                new Vector2(540f, -488f), new Vector2(200f, 52f), "回忆馆", 24,
                () => SetPage(MenuTab.Gallery));
            DuzheUiFactory.AddButton(homePage.transform, "MenuSettings",
                new Vector2(540f, -552f), new Vector2(200f, 52f), "设置", 24,
                () => SetPage(MenuTab.Settings));
            DuzheUiFactory.AddButton(homePage.transform, "Quit",
                new Vector2(540f, -616f), new Vector2(200f, 52f), UiText.Get("menu.quit", "退出游戏"), 24,
                () =>
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                });

            progressText = DuzheUiFactory.AddLabel(homePage.transform, "Progress",
                new Vector2(240f, -660f), new Vector2(800f, 30f), 20,
                new Color32(150, 144, 132, 255), TextAnchor.MiddleCenter);
        }

        private void BuildAiNotice()
        {
            aiNotice = new GameObject("AiProductionNotice");
            aiNotice.transform.SetParent(panel.transform, false);
            RectTransform rt = aiNotice.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.56f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.offsetMin = new Vector2(0f, 20f);
            rt.offsetMax = new Vector2(-24f, 184f);
            UnityEngine.UI.Image background = aiNotice.AddComponent<UnityEngine.UI.Image>();
            background.color = new Color32(9, 19, 28, 218);
            background.raycastTarget = false;
            Text title = DuzheUiFactory.AddLabel(rt, "AiNoticeTitle", new Vector2(16f, -12f),
                new Vector2(480f, 24f), 18, new Color32(238, 232, 218, 255), TextAnchor.UpperLeft);
            title.text = "AI 制作说明";
            Text text = DuzheUiFactory.AddLabel(rt, "AiNoticeBody", new Vector2(16f, -42f),
                new Vector2(480f, 112f), 16, new Color32(223, 225, 222, 255), TextAnchor.UpperLeft);
            text.rectTransform.anchorMin = new Vector2(0f, 1f);
            text.rectTransform.anchorMax = new Vector2(1f, 1f);
            text.rectTransform.sizeDelta = new Vector2(-32f, 112f);
            text.rectTransform.anchoredPosition = new Vector2(16f, -42f);
            text.text = "本作的剧情文本、美术、音乐、音效及程序代码使用生成式 AI 辅助制作，经开发者筛选、调整与整合。\n"
                + "使用工具：ChatGPT、Kimi、DeepSeek、团结 Codely。\n"
                + "内容在开发阶段生成，游玩时不实时调用 AI。";
            title.raycastTarget = text.raycastTarget = false;
        }

        private void BuildChaptersPage()
        {
            chaptersPage = new GameObject("MenuChaptersPage");
            chaptersPage.transform.SetParent(panel.transform, false);
            DuzheUiFactory.MakeFullStretch(chaptersPage);
            DuzheUiFactory.AddLabel(chaptersPage.transform, "ChaptersTitle",
                new Vector2(340f, -110f), new Vector2(600f, 48f), 36,
                new Color32(238, 232, 218, 255), TextAnchor.MiddleCenter).text = "章节目录";
            chaptersPage.transform.GetChild(chaptersPage.transform.childCount - 1);
            chapterRows = NewRowsContainer(chaptersPage.transform, "ChapterRows");
            DuzheUiFactory.AddButton(chaptersPage.transform, "ChaptersBack",
                new Vector2(540f, -640f), new Vector2(200f, 48f), "返回", 24,
                () => SetPage(MenuTab.Home));
            chaptersPage.SetActive(false);
        }

        private void BuildGalleryPage()
        {
            galleryPage = new GameObject("MenuGalleryPage");
            galleryPage.transform.SetParent(panel.transform, false);
            DuzheUiFactory.MakeFullStretch(galleryPage);
            DuzheUiFactory.AddLabel(galleryPage.transform, "GalleryTitle",
                new Vector2(340f, -110f), new Vector2(600f, 48f), 36,
                new Color32(238, 232, 218, 255), TextAnchor.MiddleCenter).text = "回忆馆";
            galleryRows = NewRowsContainer(galleryPage.transform, "GalleryRows");
            DuzheUiFactory.AddButton(galleryPage.transform, "GalleryBack",
                new Vector2(540f, -640f), new Vector2(200f, 48f), "返回", 24,
                () => SetPage(MenuTab.Home));
            galleryPage.SetActive(false);
        }

        private void BuildSettingsPage()
        {
            settingsPage = new GameObject("MenuSettingsPage");
            settingsPage.transform.SetParent(panel.transform, false);
            DuzheUiFactory.MakeFullStretch(settingsPage);
            DuzheUiFactory.AddLabel(settingsPage.transform, "SettingsTitle",
                new Vector2(340f, -150f), new Vector2(600f, 48f), 36,
                new Color32(238, 232, 218, 255), TextAnchor.MiddleCenter).text = "设置";

            GameSession session = GameSession.Instance;
            SaveService save = session != null ? session.Save : null;

            DuzheUiFactory.AddLabel(settingsPage.transform, "MenuVolMaster",
                new Vector2(420f, -260f), new Vector2(120f, 30f), 22,
                new Color32(180, 174, 162, 255), TextAnchor.MiddleLeft).text = "主音量";
            masterSlider = DuzheUiFactory.AddSlider(settingsPage.transform, "MenuVolMasterSlider",
                new Vector2(560f, -268f), new Vector2(300f, 20f),
                save != null ? save.MasterVolume : 1f, v => OnVolumeChanged(0, v));

            DuzheUiFactory.AddLabel(settingsPage.transform, "MenuVolMusic",
                new Vector2(420f, -330f), new Vector2(120f, 30f), 22,
                new Color32(180, 174, 162, 255), TextAnchor.MiddleLeft).text = "音乐";
            musicSlider = DuzheUiFactory.AddSlider(settingsPage.transform, "MenuVolMusicSlider",
                new Vector2(560f, -338f), new Vector2(300f, 20f),
                save != null ? save.MusicVolume : 1f, v => OnVolumeChanged(1, v));

            DuzheUiFactory.AddLabel(settingsPage.transform, "MenuVolSfx",
                new Vector2(420f, -400f), new Vector2(120f, 30f), 22,
                new Color32(180, 174, 162, 255), TextAnchor.MiddleLeft).text = "音效";
            sfxSlider = DuzheUiFactory.AddSlider(settingsPage.transform, "MenuVolSfxSlider",
                new Vector2(560f, -408f), new Vector2(300f, 20f),
                save != null ? save.SfxVolume : 1f, v => OnVolumeChanged(2, v));

            // C12: window mode + resolution (desktop build). Values live in
            // PlayerPrefs, never in the save file.
            DuzheUiFactory.AddLabel(settingsPage.transform, "MenuWindowSection",
                new Vector2(420f, -462f), new Vector2(200f, 30f), 22,
                new Color32(180, 174, 162, 255), TextAnchor.MiddleLeft).text = "窗口";
            windowLabel = DuzheUiFactory.AddLabel(settingsPage.transform, "MenuWindowValue",
                new Vector2(560f, -462f), new Vector2(180f, 30f), 22,
                new Color32(238, 232, 218, 255), TextAnchor.MiddleLeft);
            DuzheUiFactory.AddButton(settingsPage.transform, "MenuWindowToggle",
                new Vector2(760f, -458f), new Vector2(180f, 40f), "切换全屏", 20,
                () =>
                {
                    DisplaySettings.ToggleFullscreen();
                    SyncDisplayLabels();
                });

            DuzheUiFactory.AddLabel(settingsPage.transform, "MenuResolutionSection",
                new Vector2(420f, -522f), new Vector2(200f, 30f), 22,
                new Color32(180, 174, 162, 255), TextAnchor.MiddleLeft).text = "分辨率";
            resolutionLabel = DuzheUiFactory.AddLabel(settingsPage.transform, "MenuResolutionValue",
                new Vector2(560f, -522f), new Vector2(180f, 30f), 22,
                new Color32(238, 232, 218, 255), TextAnchor.MiddleLeft);
            DuzheUiFactory.AddButton(settingsPage.transform, "MenuResolutionPrev",
                new Vector2(760f, -518f), new Vector2(44f, 40f), "◀", 20,
                () =>
                {
                    DisplaySettings.CycleResolution(-1);
                    SyncDisplayLabels();
                });
            DuzheUiFactory.AddButton(settingsPage.transform, "MenuResolutionNext",
                new Vector2(816f, -518f), new Vector2(44f, 40f), "▶", 20,
                () =>
                {
                    DisplaySettings.CycleResolution(1);
                    SyncDisplayLabels();
                });

            DuzheUiFactory.AddButton(settingsPage.transform, "SettingsBack",
                new Vector2(540f, -600f), new Vector2(200f, 48f), "返回", 24,
                () => SetPage(MenuTab.Home));
            settingsPage.SetActive(false);
            SyncDisplayLabels();
        }

        /// Refreshes the two display rows from DisplaySettings.
        private void SyncDisplayLabels()
        {
            if (windowLabel != null) windowLabel.text = DisplaySettings.WindowLabel();
            if (resolutionLabel != null) resolutionLabel.text = DisplaySettings.ResolutionLabel();
        }

        private static Transform NewRowsContainer(Transform parent, string name)
        {
            GameObject rows = new GameObject(name);
            RectTransform rt = rows.AddComponent<RectTransform>();
            rows.transform.SetParent(parent, false);
            DuzheUiFactory.MakeFullStretch(rows);
            return rows.transform;
        }

        public void Refresh()
        {
            Build();
            GameSession session = GameSession.Instance;
            bool isMenu = session != null && session.Mode == Core.GameMode.MainMenu;
            Debug.Log("[Duzhe] MainMenu refresh isMenu=" + isMenu);
            panel.SetActive(isMenu);
            if (!isMenu) return;

            SetPage(session.PendingMenuTab);
            RefreshProgress(session);
        }

        private void SetPage(MenuTab tab)
        {
            if (homePage == null) return;
            currentPage = tab;
            if (aiNotice != null) aiNotice.SetActive(tab == MenuTab.Home);
            homePage.SetActive(tab == MenuTab.Home);
            chaptersPage.SetActive(tab == MenuTab.Chapters);
            galleryPage.SetActive(tab == MenuTab.Gallery);
            settingsPage.SetActive(tab == MenuTab.Settings);
            if (tab == MenuTab.Chapters) BuildChapterRows();
            if (tab == MenuTab.Gallery) BuildGalleryRows();
            if (tab == MenuTab.Settings) { SyncSettingsSliders(); SyncDisplayLabels(); }
        }

        /// C12: ESC steps back one page (回忆馆/章节目录/设置 -> 首页) instead
        /// of doing nothing. The pause overlay never opens in menu mode, so
        /// this press has exactly one reader.
        private void Update()
        {
            if (panel == null || !panel.activeSelf) return;
            Core.InputRouter router = Core.InputRouter.Instance;
            if (router == null || !router.EscapePressed) return;
            if (currentPage != MenuTab.Home) SetPage(MenuTab.Home);
        }

        private void BuildChapterRows()
        {
            ClearRows(chapterRows);
            GameSession session = GameSession.Instance;
            if (session == null || session.Save == null) return;
            float rowY = 210f;
            int count = 0;
            foreach (string id in ChapterCatalog.FormalChapterIds)
            {
                if (!session.Save.HasCompletedChapter(id)) continue;
                ChapterConfig cfg = ChapterCatalog.GetChapter(id);
                if (cfg == null) continue;
                string chapterId = id;
                DuzheUiFactory.AddButton(chapterRows, "MenuChapter_" + chapterId,
                    new Vector2(440f, -rowY), new Vector2(400f, 52f), cfg.displayName, 26,
                    () => SelectChapter(chapterId));
                rowY += 62f;
                count++;
            }
            if (count == 0)
            {
                DuzheUiFactory.AddLabel(chapterRows, "MenuChapterEmpty",
                    new Vector2(340f, -220f), new Vector2(600f, 40f), 24,
                    new Color32(180, 174, 162, 255), TextAnchor.MiddleCenter).text =
                    UiText.Get("chapters.empty", "通关后，可以在这里重玩章节。");
            }
        }

        private void BuildGalleryRows()
        {
            ClearRows(galleryRows);
            GameSession session = GameSession.Instance;
            if (session == null || session.Save == null) return;
            float rowY = 210f;
            int count = 0;
            foreach (GalleryController.GalleryEntry entry in GalleryController.Entries)
            {
                if (!GalleryController.CanView(session.Save, entry.presentationId)) continue;
                string id = entry.presentationId;
                DuzheUiFactory.AddButton(galleryRows, "MenuGallery_" + id,
                    new Vector2(380f, -rowY), new Vector2(520f, 52f),
                    GalleryController.TitleOf(id), 24,
                    () => PlayGallery(id));
                rowY += 62f;
                count++;
            }
            if (count == 0)
            {
                DuzheUiFactory.AddLabel(galleryRows, "MenuGalleryEmpty",
                    new Vector2(340f, -210f), new Vector2(600f, 40f), 24,
                    new Color32(180, 174, 162, 255), TextAnchor.MiddleCenter).text =
                    UiText.Get("gallery.empty", "通关后，这里会收进一段段回忆。");
            }
        }

        private static void ClearRows(Transform rows)
        {
            if (rows == null) return;
            for (int i = rows.childCount - 1; i >= 0; i--)
            {
                GameObject child = rows.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        private void SelectChapter(string chapterId)
        {
            GameSession session = GameSession.Instance;
            if (session == null || session.Chapters == null || session.Save == null) return;
            if (!session.Save.HasCompletedChapter(chapterId))
            {
                Debug.LogWarning("[Duzhe] Menu chapter replay refused: not completed: " + chapterId);
                return;
            }
            if (session.Transitions != null && session.Transitions.IsTransitioning) return;
            panel.SetActive(false);
            session.Chapters.StartChapter(chapterId, Core.GameMode.ChapterReplay);
        }

        private void PlayGallery(string presentationId)
        {
            GameSession session = GameSession.Instance;
            if (session == null || session.Gallery == null) return;
            session.Gallery.PlayFromMenu(presentationId);
        }

        private void RefreshProgress(GameSession session)
        {
            if (progressText == null) return;
            int formalDone = 0;
            bool devRouteDone = false;
            if (session.Save != null && session.Save.Data != null &&
                session.Save.Data.completedChapterIds != null)
            {
                foreach (string id in session.Save.Data.completedChapterIds)
                {
                    if (ChapterCatalog.IsFormalChapter(id)) formalDone++;
                    else if (id == "CH01_DEV") devRouteDone = true;
                }
            }

            if (formalDone > 0)
            {
                string doneList = CompletedChapterNumbers(session);
                int registered = ChapterCatalog.RegisteredFormalChapterCount();
                if (formalDone >= registered && registered > 0)
                {
                    // C12: no "已通关" wording anywhere in the menus; the list
                    // itself only ever shows completed chapters.
                    progressText.text = UiText.Get("chapters.progress", "进度")
                        + "：" + doneList + " ／ 共 " + registered +
                        " 章（本版全部完成；开始故事将重玩第 1 章）";
                }
                else
                {
                    progressText.text = UiText.Get("chapters.progress", "进度")
                        + "：" + doneList + " ／ 共 " + registered + " 章";
                }
            }
            else if (devRouteDone)
            {
                progressText.text = "开发测试路线已完成（Placeholder，不计入正式五关）";
            }
            else
            {
                progressText.text = UiText.Get("chapters.empty", "通关后，可以在这里重玩章节。")
                    + "　章节内进度不保存。";
            }
        }

        private static string CompletedChapterNumbers(GameSession session)
        {
            if (session == null || session.Save == null || session.Save.Data == null)
                return "";
            var sb = new System.Text.StringBuilder();
            foreach (string id in ChapterCatalog.FormalChapterIds)
            {
                if (!ChapterCatalog.IsFormalChapter(id)) continue;
                if (!session.Save.HasCompletedChapter(id)) continue;
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(id.StartsWith("CH") ? id.Substring(2) : id);
            }
            return sb.ToString();
        }

        private void StartGame()
        {
            GameSession session = GameSession.Instance;
            if (session == null || session.Chapters == null || session.Transitions == null) return;
            if (session.Transitions.IsTransitioning) return; // double click guard
            // "Continue" semantics: first unfinished chapter of the chain.
            // When everything is done CH01 replays (keeps every unlock and
            // never rewrites progress).
            string chapterId = session.Chapters.GetFirstUnfinishedChapterId();
            bool alreadyDone = session.Save != null && session.Save.HasCompletedChapter(chapterId);
            Core.GameMode mode = alreadyDone ? Core.GameMode.ChapterReplay : Core.GameMode.MainRun;
            if (session.Audio != null) session.Audio.PlaySfx("SFX01_UIConfirm");
            panel.SetActive(false);
            session.Chapters.StartChapter(chapterId, mode);
        }

        private void OnVolumeChanged(int which, float value)
        {
            GameSession s = GameSession.Instance;
            if (s == null || s.Save == null || s.Audio == null) return;
            s.Save.SetVolumes(
                which == 0 ? value : s.Save.MasterVolume,
                which == 1 ? value : s.Save.MusicVolume,
                which == 2 ? value : s.Save.SfxVolume);
            s.Audio.ApplyVolumes(s.Save);
        }

        private void SyncSettingsSliders()
        {
            GameSession s = GameSession.Instance;
            if (s == null || s.Save == null) return;
            if (masterSlider != null) masterSlider.SetValueWithoutNotify(s.Save.MasterVolume);
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(s.Save.MusicVolume);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(s.Save.SfxVolume);
        }

        private void OnDisable()
        {
            // Leaving the menu persists the volumes once (settings page).
            GameSession s = GameSession.Instance;
            if (s != null && s.Save != null) s.Save.SaveVolumes();
        }
    }
}
