using Duzhe.Core;
using UnityEngine;
namespace Duzhe.Scenes
{
    // An ambient greeting, independent of door puzzles and chapter progress.
    public class CourtyardDog : MonoBehaviour
    {
        public bool HasGreeted { get; private set; }
        private Vector3 restPosition;
        private float greetingTime;
        private void Start() { restPosition = transform.localPosition; }
        private void Update()
        {
            GameSession session = GameSession.Instance;
            if (session == null || session.Paused || session.Chapters == null ||
                session.Chapters.CurrentChapterId != "CH01" ||
                (session.Mode != GameMode.MainRun && session.Mode != GameMode.ChapterReplay)) return;
            if (greetingTime > 0f)
            {
                greetingTime = Mathf.Max(0f, greetingTime - Time.deltaTime);
                transform.localPosition = restPosition + Vector3.up * (Mathf.Abs(Mathf.Sin(greetingTime * 12f)) * 0.08f);
                if (greetingTime == 0f) transform.localPosition = restPosition;
            }
            if (HasGreeted || session.InputMode != InputMode.Exploring || (session.Dialogue != null && session.Dialogue.IsPlaying)) return;
            GameBootstrap boot = GameBootstrap.Instance;
            if (boot == null || boot.Player == null || Vector2.Distance(boot.Player.transform.position, transform.position) > 3.2f) return;
            HasGreeted = true;
            greetingTime = 1f;
            if (session.Audio != null) session.Audio.PlaySfx("SFX13_DogHappy");
        }
    }
}
