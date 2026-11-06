using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Reflection;
using Duzhe.Core;
using Duzhe.Chapters;
using Duzhe.Presentation;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Duzhe.EditorTools
{
    public static class OptionalContentPlayTest
    {
        private const string Active = "Duzhe.Optional.Active";
        private const string Phase = "Duzhe.Optional.Phase";
        private const string Failed = "Duzhe.Optional.Failed";
        public static void PrepareAndBuild()
        {
            var report = new StringBuilder();
            if (!ProjectSetupUtility.RunAll(report) || !DevSmokeTest.Run(report))
            {
                Debug.LogError(report.ToString());
                File.WriteAllText("Logs/optional_content_checks.txt", report.ToString());
                EditorApplication.Exit(1); return;
            }
            File.WriteAllText("Logs/optional_content_checks.txt", report.ToString());
            Debug.Log("DZ_OPTIONAL_SMOKE=PASS");
            DuzheBuildTools.BuildWindowsFromCommandLine();
        }
        public static void RunFromCommandLine()
        {
            SessionState.SetBool(Active,true); SessionState.SetInt(Phase,0); SessionState.SetBool(Failed,false);
            Directory.CreateDirectory("Logs/optional_content_play");
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Underworld.unity",OpenSceneMode.Single);
            EditorApplication.delayCall += () => PlayerSettings.runInBackground=true;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying=true;
        }
        [InitializeOnLoadMethod] private static void Resume() { if(SessionState.GetBool(Active,false)) EditorApplication.update += Tick; }
        private static void Tick()
        {
            if(!EditorApplication.isPlaying)
            {
                EditorApplication.update -= Tick; SessionState.SetBool(Active,false);
                PlayerSettings.runInBackground=false;
                EditorApplication.Exit(SessionState.GetBool(Failed,false)?1:0);return;
            }
            if(SessionState.GetInt(Phase,0)==0)
            {
                SessionState.SetInt(Phase,1);
                var go=new GameObject("OptionalContentProbe");UnityEngine.Object.DontDestroyOnLoad(go);go.AddComponent<Driver>();
            }
        }
        public class Driver:MonoBehaviour
        {
            private StringBuilder report=new StringBuilder(); private bool ok=true; private int memoryEvents; private bool preservePause;
            private void Check(bool value,string label) {report.AppendLine((value?"PASS ":"FAIL ")+label);if(!value)ok=false;}
            private IEnumerator Wait(float seconds)
            {
                float t=0;while(t<seconds){
                    GameSession current=GameSession.Instance;
                    if(!preservePause && current!=null && current.Pause!=null && current.Pause.IsOpen) current.Pause.Close();
                    t+=Time.unscaledDeltaTime;yield return null;
                }
                t=0;while(GameSession.Instance!=null && GameSession.Instance.Transitions!=null && GameSession.Instance.Transitions.IsTransitioning && t<20f){t+=Time.unscaledDeltaTime;yield return null;}
                yield return null;
            }
            private void Shot(string file){ScreenCapture.CaptureScreenshot("Logs/optional_content_play/"+file+".png");}
            private IEnumerator Start()
            {
                float timeout=0;while(GameSession.Instance==null && timeout<15){timeout+=Time.unscaledDeltaTime;yield return null;}
                GameSession gs=GameSession.Instance;
                if(gs==null){Check(false,"GameSession available");Finish();yield break;}
                SaveService.TestSaveDirectoryOverride=Path.Combine(Path.GetTempPath(),"DuzheOptional",Guid.NewGuid().ToString("N"));
                gs.Save.Load();
                gs.OnGameEvent += e=> {if(e.type==GameEventType.PresentationFinished && e.sourceId=="PR_CH02_Memory")memoryEvents++;};
                gs.Chapters.StartChapter("CH01",GameMode.MainRun);yield return Wait(1.2f);
                GameObject lamp=GameObject.Find("Visual_CH01_RITUAL");
                Check(lamp!=null && lamp.GetComponent<SpriteRenderer>().sprite.name=="PRP08_StoneLantern","formal stone lantern bound in Underworld");
                gs.Dialogue.ForceStop();
                ChapterConfig farmConfig=ChapterCatalog.GetChapter("CH01");
                int doorIndex=Array.FindIndex(farmConfig.stages,s=>s.stageId=="CH01_Door");
                typeof(ChapterDirector).GetMethod("EnterStage",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(gs.Chapters,new object[]{doorIndex});
                yield return Wait(1.2f);
                var dog=FindObjectOfType<Duzhe.Scenes.CourtyardDog>();
                Check(dog!=null && dog.GetComponent<Collider2D>()==null,"courtyard dog exists and never blocks the door");
                if(dog!=null)
                {
                    gs.SetInputMode(InputMode.Exploring); preservePause=true; gs.SetPaused(true);
                }
                yield return Wait(0.1f);
                if(dog!=null)
                {
                    GameBootstrap.Instance.Player.TeleportTo(dog.transform.position+Vector3.left*1.8f);
                    yield return Wait(0.2f);Check(!dog.HasGreeted,"dog does not greet while paused");
                    preservePause=false; if(gs.Pause!=null && gs.Pause.IsOpen) gs.Pause.Close(); gs.SetPaused(false);gs.SetInputMode(InputMode.Exploring);yield return Wait(0.3f);report.AppendLine("Dog diagnostics: dog="+dog.transform.position+" player="+GameBootstrap.Instance.Player.transform.position+" mode="+gs.InputMode+" dialogue="+gs.Dialogue.IsPlaying+" sprite="+dog.GetComponent<SpriteRenderer>().sprite.name);
                    Check(dog.HasGreeted,"approaching the courtyard dog triggers greeting");Shot("courtyard_dog"); yield return Wait(0.3f);
                }
                gs.SetPaused(false);
                gs.Chapters.StartChapter("CH02",GameMode.MainRun);yield return Wait(1.2f);
                ChapterConfig cfg=ChapterCatalog.GetChapter("CH02");int index=Array.FindIndex(cfg.stages,s=>s.stageId=="CH02_Heart");
                typeof(ChapterDirector).GetMethod("EnterStage",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(gs.Chapters,new object[]{index});
                yield return Wait(1.2f);
                gs.PublishEvent(GameEventType.MinigameCompleted,"CH02","CH02_Heart","MG04_Heart");yield return Wait(0.5f);
                Check(gs.Chapters.CurrentStageId=="CH02_Memory" && gs.Presentations.IsPlaying,"heart completion enters memory before farewell");
                string[] assets={"CG06_LoversMemory01","CG07_LoversMemory02","CG08_LoversMemory03"};
                for(int i=0;i<assets.Length;i++)
                {
                    GameObject cg=GameObject.Find("DZ_PresentationCG");
                    Check(cg!=null && cg.GetComponent<Image>().sprite.name==assets[i],"memory image "+(i+1)+" follows manual dialogue order");Shot("memory0"+(i+1));yield return Wait(0.2f);
                    gs.Dialogue.Advance();yield return Wait(0.15f);gs.Dialogue.Advance();yield return Wait(0.4f);
                }
                Check(memoryEvents==1,"memory completion published exactly once");
                Check(gs.Chapters.CurrentStageId=="CH02_Farewell","memory continues into original farewell stage");
                Check(gs.Save.HasUnlockedPresentation("PR_CH02_Memory"),"memory unlocked after viewing in main run");
                gs.ReturnToMenu(MenuTab.Gallery);yield return Wait(1.2f);
                gs.Save.Data.unlockedPresentationIds.Remove("PR_CH02_Memory");
                Check(!Duzhe.UI.GalleryController.CanView(gs.Save,"PR_CH02_Farewell"),"fresh incomplete save keeps chapter recollection locked");
                gs.Save.Data.completedChapterIds.Add("CH02");
                Check(Duzhe.UI.GalleryController.CanView(gs.Save,"PR_CH02_Farewell"),"completed legacy save can view chapter recollection without resetting");
                typeof(Duzhe.UI.MainMenuPanel).GetMethod("SetPage",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(gs.MainMenu,new object[]{MenuTab.Gallery}); yield return Wait(0.1f);
                Check(GameObject.Find("MenuGallery_PR_CH02_Farewell")!=null && GameObject.Find("MenuGallery_PR_CH02_Memory")==null,"main-menu gallery uses one chapter row for completed legacy saves");
                string before=JsonUtility.ToJson(gs.Save.Data);
                typeof(Duzhe.UI.GalleryController).GetMethod("Play",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(gs.Gallery,new object[]{"PR_CH02_Farewell"});
                yield return Wait(1.2f);
                Check(gs.Mode==GameMode.PresentationReplay && gs.Presentations.IsPlaying,"gallery starts new memory playback");
                gs.Gallery.LeaveReplay();yield return Wait(1.2f);
                Check(gs.Mode==GameMode.MainMenu && gs.PendingMenuTab==MenuTab.Gallery && !PresentationCG.IsShowing,"memory replay exits cleanly to gallery");
                Check(before==JsonUtility.ToJson(gs.Save.Data),"gallery replay does not modify progression");
                SaveService.TestSaveDirectoryOverride=null;Finish();
            }
            private void Finish()
            {
                report.AppendLine(ok?"DZ_OPTIONAL_PLAY=PASS":"DZ_OPTIONAL_PLAY=FAIL");
                File.WriteAllText("Logs/optional_content_playtest.txt",report.ToString());Debug.Log(report.ToString());
                SessionState.SetBool(Failed,!ok); EditorApplication.isPlaying=false;
            }
        }
    }
}
