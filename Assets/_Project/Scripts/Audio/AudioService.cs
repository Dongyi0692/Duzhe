using UnityEngine;

namespace Duzhe.Audio
{
    /// Volume-bus framework: master/music/sfx with persistence handled by
    /// SaveService. C10 wires the delivered R15/R16/R17 audio:
    /// - one music source (looping BGM, ducked while the notes minigame runs),
    /// - one sfx source for one-shots,
    /// - one loop source for continuous cues (rowing / chalk),
    /// all under this single manager (no second audio system).
    public class AudioService : MonoBehaviour
    {
        private AudioSource musicSource;
        private AudioSource sfxSource;
        private AudioSource loopSource;
        private bool gameplayPaused;
        private string currentMusicId;
        private string currentLoopId;
        private float musicDuck = 1f;

        private void Awake()
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            loopSource = gameObject.AddComponent<AudioSource>();
            loopSource.loop = true;
            loopSource.playOnAwake = false;
        }

        // Read-only diagnostics (used by the automated audio checks; no
        // behaviour depends on them).
        public float AppliedMusicVolume { get { return musicSource != null ? musicSource.volume : 0f; } }
        public float AppliedSfxVolume { get { return sfxSource != null ? sfxSource.volume : 0f; } }
        public bool MusicIsPlaying { get { return musicSource != null && musicSource.isPlaying; } }
        public bool SfxIsPlaying { get { return sfxSource != null && sfxSource.isPlaying; } }
        public bool LoopIsPlaying { get { return loopSource != null && loopSource.isPlaying; } }
        public bool GameplayPaused { get { return gameplayPaused; } }
        public string CurrentMusicId { get { return currentMusicId; } }
        public float MusicDuck { get { return musicDuck; } }

        public void LoadVolumesFrom(Core.SaveService save)
        {
            ApplyVolumes(save);
        }

        /// Single place where the bus gains are computed: master*music*duck
        /// for music, master*sfx for effects. Never multiplies twice.
        public void ApplyVolumes(Core.SaveService save)
        {
            if (save == null) return;
            musicSource.volume = Mathf.Clamp01(save.MasterVolume)
                * Mathf.Clamp01(save.MusicVolume) * musicDuck;
            sfxSource.volume = Mathf.Clamp01(save.MasterVolume)
                * Mathf.Clamp01(save.SfxVolume);
            loopSource.volume = sfxSource.volume;
        }

        /// The seven-note performance ducks the music instead of stopping it.
        public void SetNotesActive(bool active)
        {
            musicDuck = active ? 0.25f : 1f;
            Core.GameSession s = Core.GameSession.Instance;
            if (s != null) ApplyVolumes(s.Save);
        }

        /// Freezes/resumes gameplay audio while the pause overlay is up
        /// (timeScale does not affect AudioSources by itself).
        public void SetGameplayPaused(bool paused)
        {
            gameplayPaused = paused;
            if (paused)
            {
                if (musicSource.isPlaying) musicSource.Pause();
                if (sfxSource.isPlaying) sfxSource.Pause();
                if (loopSource.isPlaying) loopSource.Pause();
            }
            else
            {
                if (musicSource.clip != null) musicSource.UnPause();
                if (sfxSource.clip != null) sfxSource.UnPause();
                if (loopSource.clip != null) loopSource.UnPause();
            }
        }

        /// Music ids map to Resources/Audio/Music/<id>.
        public void PlayMusic(string musicAssetId)
        {
            if (string.IsNullOrEmpty(musicAssetId)) return;
            // A previously missing id must be retried (new batch imported).
            if (musicAssetId == currentMusicId && musicSource.clip != null) return;
            AudioClip clip = TryLoadClip("Audio/Music/" + musicAssetId);
            if (clip == null)
            {
                musicSource.Stop();
                musicSource.clip = null;
                currentMusicId = "";
                Debug.Log("[Duzhe] Music asset not imported yet: " + musicAssetId + " (silence).");
                return;
            }
            musicSource.Stop();
            musicSource.clip = clip;
            musicSource.volume = ComputeMusicVolume();
            musicSource.Play();
            if (gameplayPaused) musicSource.Pause();
            currentMusicId = musicAssetId;
        }

        /// Scene-based music routing (MusicCueMap): underworld/river chapters
        /// use RiverMist, the reunion scenes use Reunion, CH05 uses
        /// SelfCrossing.
        public void PlayMusicForScene(string sceneId, string chapterId)
        {
            string musicId;
            if (chapterId == "CH05")
            {
                musicId = "BGM03_SelfCrossing";
            }
            else if (sceneId == Core.DuzheIds.SceneFarmCourtyard ||
                     sceneId == Core.DuzheIds.ScenePark ||
                     sceneId == Core.DuzheIds.SceneRecordingStudio ||
                     sceneId == Core.DuzheIds.SceneClassroom)
            {
                musicId = "BGM02_Reunion";
            }
            else
            {
                musicId = "BGM01_RiverMist";
            }
            PlayMusic(musicId);
        }

        public void PlaySfx(string sfxAssetId)
        {
            AudioClip clip = TryLoadClip("Audio/SFX/" + sfxAssetId);
            if (clip == null)
            {
                Debug.Log("[Duzhe] SFX asset not imported yet: " + sfxAssetId + " (silence).");
                return;
            }
            PlayGameplayClip(clip);
        }

        /// One-shot on the sfx bus. The source volume already contains the
        /// bus gain, so PlayOneShot must NOT pass the same value again.
        public void PlayGameplayClip(AudioClip clip)
        {
            if (clip == null || gameplayPaused || sfxSource == null) return;
            sfxSource.PlayOneShot(clip, 1f);
        }

        /// Continuous cue (rowing / chalk). Starting the same id again is a
        /// no-op; a different id replaces it. Never called per frame.
        public void PlaySfxLoop(string sfxAssetId)
        {
            if (string.IsNullOrEmpty(sfxAssetId)) return;
            if (sfxAssetId == currentLoopId && loopSource.isPlaying) return;
            AudioClip clip = TryLoadClip("Audio/SFX/" + sfxAssetId);
            if (clip == null)
            {
                Debug.Log("[Duzhe] Loop SFX asset not imported yet: " + sfxAssetId + " (silence).");
                return;
            }
            loopSource.Stop();
            loopSource.clip = clip;
            loopSource.volume = sfxSource.volume;
            loopSource.Play();
            if (gameplayPaused) loopSource.Pause();
            currentLoopId = sfxAssetId;
        }

        public void StopSfxLoop(string sfxAssetId)
        {
            if (string.IsNullOrEmpty(sfxAssetId)) return;
            if (sfxAssetId != currentLoopId) return;
            loopSource.Stop();
            loopSource.clip = null;
            currentLoopId = "";
        }

        public void StopMusic()
        {
            currentMusicId = null;
            musicSource.Stop();
            musicSource.clip = null;
        }

        private float ComputeMusicVolume()
        {
            Core.SaveService save = Core.GameSession.Instance != null ? Core.GameSession.Instance.Save : null;
            if (save == null) return musicDuck;
            return Mathf.Clamp01(save.MasterVolume) * Mathf.Clamp01(save.MusicVolume) * musicDuck;
        }

        private static AudioClip TryLoadClip(string resourcesPath)
        {
            return Resources.Load<AudioClip>(resourcesPath);
        }
    }
}
