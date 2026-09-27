using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

public static class 视频素材检查
{
    public static string 状态 = "未开始";
    public static void 开始()
    {
        if (!Application.isPlaying) throw new System.InvalidOperationException("请先运行场景");
        if (GameObject.Find("视频素材临时检查") != null) return;
        Object.FindObjectOfType<产前运动.护士顺序讲解>().StartCoroutine(扫描());
    }
    static IEnumerator 扫描()
    {
        var gameObject=new GameObject("视频素材临时检查");
        var player=gameObject.AddComponent<VideoPlayer>();
        player.playOnAwake=false; player.audioOutputMode=VideoAudioOutputMode.None;
        var target=new RenderTexture(480,270,0);target.Create();
        player.renderMode=VideoRenderMode.RenderTexture;player.targetTexture=target;player.aspectRatio=VideoAspectRatio.FitInside;
        string error=null;player.errorReceived+=(p,e)=>error=e;
        Directory.CreateDirectory("Captures/视频核查");
        foreach(var guid in AssetDatabase.FindAssets("t:VideoClip",new[]{"Assets/Vedio"}))
        {
            var clip=AssetDatabase.LoadAssetAtPath<VideoClip>(AssetDatabase.GUIDToAssetPath(guid));
            状态="检查："+clip.name;error=null;player.clip=clip;player.Prepare();
            float limit=Time.realtimeSinceStartup+30;
            while(!player.isPrepared&&error==null&&Time.realtimeSinceStartup<limit)yield return null;
            if(!player.isPrepared){Debug.LogWarning(状态+" 准备失败");continue;}
            var sheet=new Texture2D(1920,810,TextureFormat.RGB24,false);
            for(int i=0;i<12;i++)
            {
                bool seek=false;VideoPlayer.EventHandler handler=p=>seek=true;player.seekCompleted+=handler;
                player.time=System.Math.Max(.1,(clip.length-.5)*i/11); player.Play();
                limit=Time.realtimeSinceStartup+15;
                while(!seek&&error==null&&Time.realtimeSinceStartup<limit)yield return null;
                yield return new WaitForSecondsRealtime(.4f);
                yield return new WaitForEndOfFrame();player.Pause();player.seekCompleted-=handler;
                var previous=RenderTexture.active;RenderTexture.active=target;
                sheet.ReadPixels(new Rect(0,0,480,270),i%4*480,(2-i/4)*270);RenderTexture.active=previous;
            }
            sheet.Apply();File.WriteAllBytes("Captures/视频核查/"+clip.name+".png",sheet.EncodeToPNG());Object.Destroy(sheet);player.Stop();
        }
        player.targetTexture=null;target.Release();Object.Destroy(target);状态="时间轴预览完成";Object.Destroy(gameObject);
    }
}
