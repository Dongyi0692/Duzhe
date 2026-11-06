using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Duzhe.EditorTools
{
    public static class C12Verification
    {
        public static void RunFromCommandLine()
        {
            var report = new StringBuilder();
            bool ok = false;
            try
            {
                ok = ProjectSetupUtility.RunAll(report);
                if (ok)
                {
                    ok &= DevSmokeTest.Run(report);
                    ok &= MinigameValidator.Run(report);
                    ok &= RiverSeedValidator.Run(report);
                    Sprite menu = Resources.Load<Sprite>("Art/Backgrounds/BG00_MainMenu");
                    bool menuOk = menu != null && menu.rect.width == 1280 && menu.rect.height == 720;
                    ok &= menuOk;
                    report.AppendLine((menuOk ? "OK " : "FAIL ") + "Main menu background: 1280x720 Sprite.");
                }
            }
            catch (Exception e) { report.AppendLine(e.ToString()); ok = false; }
            report.AppendLine(ok ? "DZ_C12_VERIFY=PASS" : "DZ_C12_VERIFY=FAIL");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/codex_c12_verification.txt", report.ToString(), Encoding.UTF8);
            Debug.Log(report.ToString());
            EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
