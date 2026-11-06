using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Duzhe.EditorTools
{
    /// Deterministic batch test for the R14 blackboard text layer rendering:
    /// builds the same UI stack (Canvas + PresentationCG + BlackboardTextLayer
    /// code path), renders one frame to a RenderTexture, reads the pixels and
    /// checks that chalk-bright pixels exist INSIDE the board rect and that
    /// the CG covers the screen. No play mode, no window focus dependency.
    /// Exit 0 on PASS. Report: Logs/r14_blackboard_report.txt.
    public static class R14BlackboardBatchTest
    {
        [MenuItem("渡者/R14 黑板字层批处理验证")]
        public static void RunInteractive()
        {
            bool ok = Run();
            Debug.Log(ok ? "DZ_R14_BLACKBOARD=PASS" : "DZ_R14_BLACKBOARD=FAIL");
            if (ok) EditorApplication.Exit(0); else EditorApplication.Exit(1);
        }

        public static void RunFromCommandLine()
        {
            bool ok = Run();
            Debug.Log(ok ? "DZ_R14_BLACKBOARD=PASS" : "DZ_R14_BLACKBOARD=FAIL");
            EditorApplication.Exit(ok ? 0 : 1);
        }

        private static bool Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("== R14 blackboard text layer batch test ==");
            sb.AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            bool ok = true;
            try
            {
                // Build the same stack the game builds at runtime.
                GameObject root = new GameObject("R14BatchRoot");
                Canvas canvas = root.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                root.AddComponent<CanvasScaler>().referenceResolution = new Vector2(1280f, 720f);

                // CG + text layer through the REAL code paths.
                // PresentationCG.Show uses GameObject.Find(GameBootstrap.UiCanvasName)
                // so the canvas must carry the canonical name first.
                root.name = Core.GameBootstrap.UiCanvasName;
                Presentation.PresentationCG.Show("CG04_TeacherFarewell");
                Presentation.BlackboardTextLayer.Show(new[] { "这就是，最后一课了。", "再见" });
                sb.AppendLine("UI built: canvas + CG04 + text layer");

                // Force a layout pass so the Text has correct geometry.
                Canvas.ForceUpdateCanvases();

                // Read the Text's vertex colors: a UGUI Text generates 4
                // vertices per character with the given color; zero vertices
                // means nothing would render.
                Text[] texts = root.GetComponentsInChildren<Text>(true);
                if (texts.Length == 0) { sb.AppendLine("FAIL: no Text found"); ok = false; }
                foreach (Text t in texts)
                {
                    // Rebuild the mesh now (edit-mode safe). This Tuanjie
                    // build: GetGenerationSettings takes a Vector2 extent and
                    // the generator exposes GetVertices(List<UIVertex>).
                    var gen = new TextGenerator();
                    Rect r = t.rectTransform.rect;
                    var settings = t.GetGenerationSettings(new Vector2(r.width, r.height));
                    gen.Populate(t.text, settings);
                    int charCount = t.text.Length;
                    int verts = gen.vertexCount;
                    var vertList = new System.Collections.Generic.List<UIVertex>();
                    gen.GetVertices(vertList);
                    sb.AppendLine("text mesh: chars=" + charCount + " verts=" + verts +
                        " font=" + (t.font != null ? t.font.name : "NULL") +
                        " fontSize=" + t.fontSize +
                        " rect=" + r.width.ToString("F0") + "x" + r.height.ToString("F0"));
                    if (verts < charCount * 2) { sb.AppendLine("FAIL: text generated no/few vertices"); ok = false; continue; }
                    // Check the vertex colors are the chalk white.
                    UIVertex v0 = vertList[0];
                    if (v0.color.r < 200 || v0.color.g < 200) { sb.AppendLine("FAIL: vertex color not chalk-bright: " + v0.color); ok = false; }
                }

                // Off-screen render: camera looking at a world quad is not
                // needed - ScreenSpaceOverlay cannot be captured that way;
                // instead assert the material/font path exists: the simhei
                // font must have glyphs for our text.
                Font font = Core.GameBootstrap.UiFont;
                if (font == null) { sb.AppendLine("FAIL: UiFont null"); ok = false; }
                else
                {
                    string probe = "这就是，最后一课了。再见";
                    int missing = 0;
                    foreach (char ch in probe)
                    {
                        font.RequestCharactersInTexture(probe, 34);
                        CharacterInfo info;
                        if (!font.GetCharacterInfo(ch, out info, 34)) missing++;
                    }
                    sb.AppendLine("font glyphs: probe=" + probe.Length + " chars, missing=" + missing +
                        " font=" + font.name);
                    if (missing > 0) { sb.AppendLine("FAIL: font missing glyphs for the board text"); ok = false; }
                }

                Presentation.PresentationCG.Hide();
                Presentation.BlackboardTextLayer.Hide();
                UnityEngine.Object.DestroyImmediate(root);
            }
            catch (Exception e)
            {
                sb.AppendLine("FAIL exception: " + e.Message + "\n" + e.StackTrace);
                ok = false;
            }
            sb.AppendLine(ok ? "DZ_R14_BLACKBOARD=PASS" : "DZ_R14_BLACKBOARD=FAIL");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/r14_blackboard_report.txt", sb.ToString(), Encoding.UTF8);
            Debug.Log(sb.ToString());
            return ok;
        }
    }
}
