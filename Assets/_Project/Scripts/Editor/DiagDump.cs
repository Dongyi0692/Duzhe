using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Duzhe.EditorTools
{
    /// TEMPORARY diagnostic (one-shot): dumps the Boat sprite state in the
    /// River scene and the engine-view GUIDs of every placeholder art asset,
    /// to pin down the missing-boat report.
    public static class DiagDump
    {
        public static void RunFromCommandLine()
        {
            var sb = new StringBuilder();
            try
            {
                sb.AppendLine("== AssetPathToGUID (engine view) ==");
                string artRoot = "Assets/_Project/Resources/Art";
                string[] files = Directory.GetFiles(artRoot, "*.png", SearchOption.AllDirectories);
                foreach (string f in files)
                {
                    string guid = AssetDatabase.AssetPathToGUID(f);
                    sb.AppendLine(f + " -> " + guid);
                }

                sb.AppendLine("== River.unity Boat state ==");
                Scene river = EditorSceneManager.OpenScene("Assets/_Project/Scenes/River.unity", OpenSceneMode.Single);
                foreach (GameObject go in river.GetRootGameObjects())
                {
                    foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name == "Boat")
                        {
                            SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
                            if (sr == null) sb.AppendLine("Boat: NO SpriteRenderer");
                            else
                            {
                                sb.AppendLine("Boat sprite: " + (sr.sprite != null ? sr.sprite.name : "NULL"));
                                sb.AppendLine("Boat enabled: " + sr.enabled + " GOactive: " + t.gameObject.activeSelf +
                                    " pos: " + t.position.ToString("F2") + " layer: " + t.gameObject.layer +
                                    " sortLayer: " + sr.sortingLayerName + " order: " + sr.sortingOrder);
                            }
                        }
                    }
                }

                sb.AppendLine("== Underworld.unity BoatDock state ==");
                Scene uw = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Underworld.unity", OpenSceneMode.Single);
                foreach (GameObject go in uw.GetRootGameObjects())
                {
                    foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name == "BoatDock_DEV")
                        {
                            SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
                            if (sr == null) sb.AppendLine("BoatDock: NO SpriteRenderer");
                            else sb.AppendLine("BoatDock sprite: " + (sr.sprite != null ? sr.sprite.name : "NULL"));
                        }
                    }
                }

                sb.AppendLine("== LoadAssetAtPath checks ==");
                Sprite boat = AssetDatabase.LoadAssetAtPath<Sprite>(artRoot + "/Props/Boat_Placeholder.png");
                Sprite whirl = AssetDatabase.LoadAssetAtPath<Sprite>(artRoot + "/Props/Whirlpool_Placeholder.png");
                sb.AppendLine("Boat_Placeholder load: " + (boat != null ? "OK " + boat.name : "NULL"));
                sb.AppendLine("Whirlpool_Placeholder load: " + (whirl != null ? "OK " + whirl.name : "NULL"));
            }
            catch (Exception e)
            {
                sb.AppendLine("EXCEPTION: " + e.Message + "\n" + e.StackTrace);
            }
            string text = sb.ToString();
            Debug.Log(text);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/diag_dump.txt", text, Encoding.UTF8);
            EditorApplication.Exit(0);
        }
    }
}
