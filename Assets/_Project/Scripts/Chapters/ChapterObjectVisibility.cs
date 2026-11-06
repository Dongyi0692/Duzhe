using Duzhe.Core;
using UnityEngine;

namespace Duzhe.Chapters
{
    /// Route visibility gate for objects that physically coexist in one scene
    /// but belong to different chapter routes (the CH01_DEV development route
    /// and the formal chapters share the same Underworld layout points).
    ///
    /// C09 rewrite: visibility is driven by GameSession.VisualChapterId and is
    /// applied by an explicit RefreshVisibility() sweep (the session scans the
    /// active scene, including INACTIVE objects). A gate never subscribes to
    /// chapter events and never re-derives its default from activeSelf, so:
    /// - a gate hiding its own object can still be woken by the next sweep;
    /// - an actor activated mid-chapter (ritual ghost, cast) is not hidden
    ///   again by a later OnEnable.
    /// `visibleOnChapterEntry` is written by the assembler and is the ONLY
    /// thing a chapter entry restores.
    public class ChapterObjectVisibility : MonoBehaviour
    {
        [Tooltip("Chapter route this object belongs to (e.g. CH01_DEV or CH01).")]
        public string chapterId = "";

        [Tooltip("Whether this object is visible as soon as its chapter starts. Actors that only appear later (ritual ghosts) must be false.")]
        public bool visibleOnChapterEntry = true;

        private bool IsCurrentVisualChapter()
        {
            GameSession s = GameSession.Instance;
            return s != null && !string.IsNullOrEmpty(chapterId) &&
                   s.VisualChapterId == chapterId;
        }

        /// Set once a scene cast shows this object AFTER the chapter entry.
        /// From then on the cast owns the object: a later sweep must not
        /// re-apply `visibleOnChapterEntry` (that used to hide the drummer /
        /// the girl again on the same frame the cast activated them).
        private bool shownAtRuntime;

        private void OnEnable()
        {
            // Only block objects of ANOTHER chapter; never re-hide an actor
            // this chapter just activated.
            if (!IsCurrentVisualChapter()) gameObject.SetActive(false);
        }

        /// Runtime show used by the scene casts (ghosts that appear on arrival).
        public void ShowRuntime()
        {
            shownAtRuntime = true;
            gameObject.SetActive(true);
        }

        /// Applies the entry rule for the current display chapter. Called only
        /// on chapter/scene entry (never per frame).
        public void RefreshVisibility()
        {
            if (shownAtRuntime && IsCurrentVisualChapter())
            {
                if (!gameObject.activeSelf) gameObject.SetActive(true);
                return;
            }
            shownAtRuntime = false;
            bool show = IsCurrentVisualChapter() && visibleOnChapterEntry;
            if (gameObject.activeSelf != show) gameObject.SetActive(show);
        }
    }
}
