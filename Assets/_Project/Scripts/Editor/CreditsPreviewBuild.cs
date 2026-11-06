using System.Collections;
using System.IO;
using System.Reflection;
using Duzhe.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Duzhe.EditorTools
{
    public static class CreditsPreviewBuild
    {
        const string Key = "Duzhe.CreditsPreview.Active";
        public static void PreviewOnly(){SessionState.SetBool(Key+".Only",true);RunFromCommandLine();}
        public static void RunFromCommandLine()
        {
            SessionState.SetInt(Key,1);
            SessionState.SetBool(Key+".Background",PlayerSettings.runInBackground);
            PlayerSettings.runInBackground=true;
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity",OpenSceneMode.Single);
            EditorApplication.update+=Tick;
            EditorApplication.isPlaying=true;
        }
        [InitializeOnLoadMethod] static void Resume(){if(SessionState.GetInt(Key,0)>0)EditorApplication.update+=Tick;}
        static void Tick()
        {
            int phase=SessionState.GetInt(Key,0);
            if(EditorApplication.isPlaying && phase==1)
            {
                SessionState.SetInt(Key,2);
                var go=new GameObject("CreditsPreviewDriver");Object.DontDestroyOnLoad(go);go.AddComponent<Driver>();
            }
            else if(!EditorApplication.isPlaying && phase==3)
            {
                EditorApplication.update-=Tick;SessionState.SetInt(Key,0);
                PlayerSettings.runInBackground=SessionState.GetBool(Key+".Background",false);
                if(SessionState.GetBool(Key+".Only",false)){SessionState.SetBool(Key+".Only",false);EditorApplication.Exit(0);}
                else EditorApplication.delayCall+=DuzheBuildTools.BuildWindowsFromCommandLine;
            }
        }
        public class Driver:MonoBehaviour
        {
            IEnumerator Start()
            {
                yield return null;
                while(GameSession.Instance==null || GameSession.Instance.Credits==null)yield return null;
                yield return new WaitForSecondsRealtime(.5f);
                var session=GameSession.Instance;
                session.Chapters.StartChapter("CH05",GameMode.ChapterReplay);
                yield return new WaitForSecondsRealtime(1f);
                session.Presentations.ForceAbort();session.Dialogue.ForceStop();
                var cfg=Duzhe.Chapters.ChapterCatalog.GetChapter("CH05");
                int complete=System.Array.FindIndex(cfg.stages,s=>s.stageId=="CH05_Complete");
                typeof(Duzhe.Chapters.ChapterDirector).GetMethod("EnterStage",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(session.Chapters,new object[]{complete});
                yield return new WaitForSecondsRealtime(1f);
                var credits=session.Credits;
                typeof(Duzhe.UI.CreditsPanel).GetMethod("Build",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(credits,null);
                GameObject panel=credits.transform.Find("CreditsPanel").gameObject;
                panel.SetActive(true);credits.transform.SetAsLastSibling();
                Canvas canvas=GameObject.Find(GameBootstrap.UiCanvasName).GetComponent<Canvas>();
                Camera cam=GameBootstrap.Instance.CameraFollow.GetComponent<Camera>();
                var target=new RenderTexture(1280,720,24);
                var prior=cam.targetTexture;var previous=RenderTexture.active;
                cam.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1f;
                yield return null;
                Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=target;
                Debug.Log("Credits active="+panel.activeInHierarchy+" canvas="+canvas.enabled+" mask="+cam.cullingMask);
                foreach(var label in panel.GetComponentsInChildren<UnityEngine.UI.Text>())
                    Debug.Log("CREDITS_TEXT "+label.name+" "+label.text+" preferred="+label.preferredWidth+" rect="+label.rectTransform.rect.width);
                var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();
                Directory.CreateDirectory("Logs");File.WriteAllBytes("Logs/credits_final_preview.png",tex.EncodeToPNG());
                cam.targetTexture=prior;RenderTexture.active=previous;canvas.renderMode=RenderMode.ScreenSpaceOverlay;
                Destroy(tex);target.Release();Destroy(target);
                Debug.Log("DZ_CREDITS_PREVIEW=PASS");
                SessionState.SetInt(Key,3);EditorApplication.isPlaying=false;
            }
        }
    }
}
