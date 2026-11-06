using Duzhe.Core;
using UnityEngine;

namespace Duzhe.Chapters
{
    /// Hides an object when a matching chapter event fires (e.g. the cane
    /// station disappears for the rest of the run once MG01_Cane completes).
    /// One-way: it never re-activates the object on its own.
    public class HideOnChapterEvent : MonoBehaviour
    {
        public GameEventType eventType = GameEventType.MinigameCompleted;
        [Tooltip("Only react when the event sourceId matches (empty = any).")]
        public string sourceId = "";
        [Tooltip("Only react when the event chapterId matches (empty = any).")]
        public string chapterId = "";

        private void OnEnable()
        {
            GameSession session = GameSession.Instance;
            if (session != null) session.OnGameEvent += HandleEvent;
        }

        private void OnDisable()
        {
            GameSession session = GameSession.Instance;
            if (session != null) session.OnGameEvent -= HandleEvent;
        }

        private void HandleEvent(GameEvent e)
        {
            if (e.type != eventType) return;
            if (!string.IsNullOrEmpty(sourceId) && e.sourceId != sourceId) return;
            if (!string.IsNullOrEmpty(chapterId) && e.chapterId != chapterId) return;
            gameObject.SetActive(false);
        }
    }
}
