using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Duzhe.Core
{
    /// Persistent save data (TechnicalAndInteractionSpec section 9):
    /// chapter completion, unlocked presentations and audio volumes.
    /// In-chapter precision progress is deliberately NOT saved; "continue"
    /// starts at the first unfinished chapter. Writes go to a temp file then
    /// atomically replace, keeping the previous file as .bak; a corrupt main
    /// file falls back to .bak with a visible warning.
    public class SaveService : MonoBehaviour
    {
        public const int SaveVersion = 1;

        [Serializable]
        public class DuzheSaveData
        {
            public int saveVersion = SaveVersion;
            public List<string> completedChapterIds = new List<string>();
            public List<string> unlockedPresentationIds = new List<string>();
            public float masterVolume = 1f;
            public float musicVolume = 1f;
            public float sfxVolume = 1f;
        }

#if UNITY_EDITOR
        /// Editor-only redirect so automated tests never touch the player's
        /// real save (and its .bak). Null means "use persistentDataPath".
        public static string TestSaveDirectoryOverride;
#endif

        private DuzheSaveData data = new DuzheSaveData();

        public static string SaveDirectoryPath
        {
            get
            {
#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(TestSaveDirectoryOverride)) return TestSaveDirectoryOverride;
#endif
                return Application.persistentDataPath;
            }
        }

        private string MainPath { get { return Path.Combine(SaveDirectoryPath, "duzhe_save.json"); } }
        private string TempPath { get { return MainPath + ".tmp"; } }
        private string BakPath { get { return MainPath + ".bak"; } }

        private void Awake()
        {
            Load();
        }

        public DuzheSaveData Data { get { return data; } }

        public bool HasCompletedChapter(string chapterId)
        {
            return data.completedChapterIds != null && data.completedChapterIds.Contains(chapterId);
        }

        /// Idempotent by contract: completing the same chapter twice must
        /// never duplicate entries.
        public bool MarkChapterCompleted(string chapterId)
        {
            if (string.IsNullOrEmpty(chapterId) || HasCompletedChapter(chapterId)) return false;
            data.completedChapterIds.Add(chapterId);
            Save();
            return true;
        }

        public bool HasUnlockedPresentation(string presentationId)
        {
            return data.unlockedPresentationIds != null && data.unlockedPresentationIds.Contains(presentationId);
        }

        public bool UnlockPresentation(string presentationId)
        {
            if (string.IsNullOrEmpty(presentationId) || HasUnlockedPresentation(presentationId)) return false;
            data.unlockedPresentationIds.Add(presentationId);
            Save();
            return true;
        }

        public void SetVolumes(float master, float music, float sfx)
        {
            data.masterVolume = Mathf.Clamp01(master);
            data.musicVolume = Mathf.Clamp01(music);
            data.sfxVolume = Mathf.Clamp01(sfx);
        }

        public float MasterVolume { get { return data.masterVolume; } }
        public float MusicVolume { get { return data.musicVolume; } }
        public float SfxVolume { get { return data.sfxVolume; } }

        /// Volumes persist when the pause panel is confirmed or closed
        /// (spec: "write on confirm/exit"), not on every slider tick.
        public void SaveVolumes()
        {
            Save();
        }

        public void Load()
        {
            data = ReadFile(MainPath);
            if (data == null)
            {
                if (File.Exists(BakPath))
                {
                    data = ReadFile(BakPath);
                    if (data != null)
                    {
                        Debug.LogWarning("[Duzhe] duzhe_save.json was corrupt; restored the previous backup. Progress may be one save behind.");
                        Save(); // re-establish a healthy main file
                        return;
                    }
                }
                Debug.LogWarning("[Duzhe] No usable save found; starting with a fresh save file.");
                data = new DuzheSaveData();
            }
        }

        public void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(data);
                string dir = SaveDirectoryPath;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                if (File.Exists(TempPath)) File.Delete(TempPath);
                File.WriteAllText(TempPath, json);
                if (File.Exists(MainPath))
                {
                    if (File.Exists(BakPath)) File.Delete(BakPath);
                    File.Copy(MainPath, BakPath);
                }
                if (File.Exists(MainPath)) File.Delete(MainPath);
                File.Move(TempPath, MainPath);
            }
            catch (Exception e)
            {
                Debug.LogError("[Duzhe] Save failed: " + e.Message);
            }
        }

        public void ClearAll()
        {
            data = new DuzheSaveData();
            Save();
            try
            {
                if (File.Exists(BakPath)) File.Delete(BakPath);
            }
            catch (Exception) { }
        }

        private static DuzheSaveData ReadFile(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                string json = File.ReadAllText(path);
                DuzheSaveData d = JsonUtility.FromJson<DuzheSaveData>(json);
                if (d == null || d.saveVersion != SaveVersion)
                {
                    Debug.LogWarning("[Duzhe] Save version mismatch at " + path + "; ignoring file.");
                    return null;
                }
                if (d.completedChapterIds == null) d.completedChapterIds = new List<string>();
                if (d.unlockedPresentationIds == null) d.unlockedPresentationIds = new List<string>();
                return d;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Duzhe] Save read failed at " + path + ": " + e.Message);
                return null;
            }
        }
    }
}
