using System.IO;
using 产前运动;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class 固定场景生成器
{
    [MenuItem("产前运动/打开固定视角主场景")]
    public static void Build()
    {
        const string path="Assets/产前运动/场景/产前运动互动.unity";
        if(EditorApplication.isPlaying) throw new System.InvalidOperationException("请先退出运行模式");
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty) throw new System.InvalidOperationException("请先保存当前场景");
        if(File.Exists(path)){EditorSceneManager.OpenScene(path);return;}
        var source=EditorSceneManager.OpenScene("Assets/产前运动/场景/历史界面演示.unity");
        EditorSceneManager.SaveScene(source,path,true);
        var scene=EditorSceneManager.OpenScene(path);
        var c=Object.FindObjectOfType<讲解控制器>(); c.openOnStart=true; c.interactionPanel=null;
        c.gameObject.AddComponent<固定视角入口>().lesson=c;
        var canvas=c.dialogue.GetComponentInParent<Canvas>(); var root=canvas.transform;
        canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=canvas.GetComponent<CanvasScaler>();scaler.referenceResolution=new Vector2(1440,900);scaler.matchWidthOrHeight=.5f;
        var cam=Camera.main;cam.name="固定摄像机";
        var player=GameObject.CreatePrimitive(PrimitiveType.Capsule);player.name="玩家占位";player.transform.position=new Vector3(3,1,-10);
        var entry=c.gameObject;entry.name="对话管理器";
        // 保留现有 VideoPlayer 及其资源引用，避免重复创建播放器。
        Place(c.dialogue.transform,.04f,.035f,.96f,.355f);
        Place(c.dialogue.speaker.transform,.025f,.83f,.7f,.97f);c.dialogue.speaker.fontSize=23;
        Place(c.dialogue.body.transform,.025f,.31f,.975f,.81f);c.dialogue.body.fontSize=32;
        Place(c.dialogue.pageLabel.transform,.91f,.83f,.975f,.97f);c.dialogue.pageLabel.fontSize=19;
        c.input.transform.SetParent(c.dialogue.transform,false);Place(c.input.transform,.025f,.055f,.73f,.245f);
        c.input.field.textComponent.fontSize=25;((TMP_Text)c.input.field.placeholder).fontSize=25;
        c.input.send.transform.SetParent(c.dialogue.transform,false);Place(c.input.send.transform,.745f,.055f,.85f,.245f);
        Place(c.dialogue.next.transform,.865f,.055f,.975f,.245f);
        c.dialogue.next.GetComponentInChildren<TMP_Text>().fontSize=24;c.input.send.GetComponentInChildren<TMP_Text>().fontSize=24;
        var menu=c.exerciseButtons[0].transform.parent; Place(menu,.04f,.385f,.285f,.80f);
        for(int i=0;i<5;i++){float y=.815f-i*.158f;Place(c.exerciseButtons[i].transform,.035f,y,.965f,y+.14f);c.exerciseButtons[i].GetComponentInChildren<TMP_Text>().fontSize=20;}
        foreach(var button in new[]{c.safety,c.restart,c.resume})button.transform.SetParent(root,false);
        Place(c.safety.transform,.04f,.815f,.115f,.86f);Place(c.restart.transform,.125f,.815f,.2f,.86f);Place(c.resume.transform,.21f,.815f,.285f,.86f);
        c.status.transform.SetParent(root,false);Place(c.status.transform,.30f,.385f,.58f,.46f);c.status.fontSize=17;
        Place(c.heading.transform,.31f,.81f,.96f,.87f);c.heading.fontSize=27;
        var videoArea=c.video.screen.transform.parent.parent; Place(videoArea,.59f,.47f,.96f,.80f);
        Place(c.video.play.transform,.59f,.405f,.765f,.46f);Place(c.video.replay.transform,.785f,.405f,.96f,.46f);
        c.video.play.GetComponentInChildren<TMP_Text>().fontSize=22;c.video.replay.GetComponentInChildren<TMP_Text>().fontSize=22;
        var nurse=GameObject.Find("护士");nurse.transform.position=new Vector3(-.8f,1,0);
        var label=root.Find("护士标识");Place(label,.4f,.70f,.49f,.76f);
        EditorSceneManager.SaveScene(scene,path);
    }
    private static void Place(Transform t,float x,float y,float r,float top)
    {var rt=(RectTransform)t;rt.anchorMin=new Vector2(x,y);rt.anchorMax=new Vector2(r,top);rt.offsetMin=rt.offsetMax=Vector2.zero;rt.localScale=Vector3.one;}
}
