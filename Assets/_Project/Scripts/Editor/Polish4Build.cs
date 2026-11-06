using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace Duzhe.EditorTools
{
    public static class Polish4Build
    {
        public static void RunFromCommandLine()
        {
            var report = new StringBuilder();
            bool ok = ProjectSetupUtility.RunAll(report);
            ok &= DevSmokeTest.Run(report);
            ok &= MinigameValidator.Run(report);
            ok &= RiverSeedValidator.Run(report);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/polish4_validation.txt", report.ToString());
            Debug.Log(report.ToString());
            if (!ok) { EditorApplication.Exit(1); return; }
            Debug.Log("DZ_POLISH4=PASS");
            DuzheBuildTools.BuildWindowsFromCommandLine();
        }
    }
}
