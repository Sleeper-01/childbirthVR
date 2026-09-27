using System.IO;
using 产前运动;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class 历史第一人称生成器
{
    private const string Path = "Assets/产前运动/场景/历史第一人称演示.unity";
    [MenuItem("产前运动/打开历史第一人称演示")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play first.");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty) throw new System.InvalidOperationException("Save current scene first.");
        if (File.Exists(Path)) { EditorSceneManager.OpenScene(Path); return; }
        var source = EditorSceneManager.OpenScene("Assets/产前运动/场景/历史界面演示.unity");
        EditorSceneManager.SaveScene(source, Path, true);
        var scene = EditorSceneManager.OpenScene(Path);
        var lesson = Object.FindObjectOfType<讲解控制器>();
        var nurse = GameObject.Find("护士"); nurse.transform.position = new Vector3(0, 1, 0);
        GameObject.Find("地板").transform.localScale = new Vector3(2, 1, 2);
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/产前运动/配置/护士.mat");
        Cube("北侧边界", new Vector3(0,.35f,10), new Vector3(20,.7f,.25f), material);
        Cube("南侧边界", new Vector3(0,.35f,-10), new Vector3(20,.7f,.25f), material);
        Cube("东侧边界", new Vector3(10,.35f,0), new Vector3(.25f,.7f,20), material);
        Cube("西侧边界", new Vector3(-10,.35f,0), new Vector3(.25f,.7f,20), material);
        var body = new GameObject("玩家占位", typeof(CharacterController), typeof(历史第一人称控制器));
        body.transform.position = new Vector3(0,.03f,-6);
        var motor = body.GetComponent<CharacterController>(); motor.height = 1.8f; motor.radius = .3f; motor.center = new Vector3(0,.9f,0); motor.stepOffset = .2f;
        var mesh = GameObject.CreatePrimitive(PrimitiveType.Capsule); mesh.name = "玩家胶囊"; mesh.transform.SetParent(body.transform,false); mesh.transform.localPosition = new Vector3(0,.9f,0); mesh.transform.localScale = new Vector3(.6f,.9f,.6f); mesh.layer = 2; Object.DestroyImmediate(mesh.GetComponent<Collider>());
        var cam = GameObject.Find("固定摄像机").GetComponent<Camera>(); cam.name = "历史第一人称摄像机";
        cam.transform.SetParent(body.transform,false); cam.transform.localPosition = new Vector3(0,1.65f,0); cam.transform.localRotation = Quaternion.identity;
        cam.orthographic = false; cam.fieldOfView = 70; cam.nearClipPlane = .05f; cam.cullingMask &= ~(1<<2);
        cam.backgroundColor = new Color(.62f,.74f,.81f);
        var walker = body.GetComponent<历史第一人称控制器>(); walker.viewCamera = cam;
        var root = GameObject.Find("交互画布"); var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = cam;
        Object.DestroyImmediate(root.GetComponent<CanvasScaler>());
        var rect = root.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(1000,640); rect.localScale = Vector3.one * .0025f; rect.position = new Vector3(1.9f,1.65f,.9f);
        rect.rotation = Quaternion.LookRotation(rect.position - cam.transform.position, Vector3.up);
        lesson.openOnStart = false; lesson.interactionPanel = root.AddComponent<CanvasGroup>(); lesson.interactionPanel.alpha = 0; lesson.interactionPanel.interactable = lesson.interactionPanel.blocksRaycasts = false;
        foreach(var name in new[]{"标题背景","标题","副标题","说明文字","护士标识"}) { var item=root.transform.Find(name); if(item != null) Object.DestroyImmediate(item.gameObject); }
        Place(lesson.heading.transform, .03f,.91f,.76f,.99f); lesson.heading.fontSize = 30;
        var menu = root.transform.Find("运动选择区"); Place(menu, .02f,.61f,.98f,.9f);
        for(int i=0;i<5;i++)
        {
            int row=i/3, col=i%3;
            Place(lesson.exerciseButtons[i].transform,.015f+col*.33f,.53f-row*.5f,.325f+col*.33f,.97f-row*.5f);
            lesson.exerciseButtons[i].GetComponentInChildren<TMP_Text>().fontSize=23;
        }
        Place(lesson.dialogue.transform,.02f,.16f,.98f,.59f);
        Place(lesson.dialogue.body.transform,.025f,.29f,.975f,.78f); lesson.dialogue.body.fontSize=36;
        lesson.status.fontSize=16;
        Place(lesson.input.transform,.02f,.02f,.80f,.13f); Place(lesson.input.send.transform,.82f,.02f,.98f,.13f);
        var background = new GameObject("面板背景",typeof(RectTransform),typeof(Image)); background.transform.SetParent(root.transform,false); background.transform.SetAsFirstSibling(); Place(background.transform,0,0,1,1); background.GetComponent<Image>().color=new Color(.91f,.95f,.93f,1);
        var exitObj=Object.Instantiate(lesson.dialogue.next.gameObject,root.transform); exitObj.name="结束对话按钮"; Place(exitObj.transform,.77f,.92f,.98f,.99f); exitObj.GetComponentInChildren<TMP_Text>().text="结束对话";

        var screenRoot=new GameObject("历史空间视频屏",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));
        var sc=screenRoot.GetComponent<Canvas>(); sc.renderMode=RenderMode.WorldSpace; sc.worldCamera=cam;
        var sr=screenRoot.GetComponent<RectTransform>(); sr.sizeDelta=new Vector2(960,640); sr.localScale=Vector3.one*.002f; sr.position=new Vector3(-1.8f,1.7f,0);
        var videoArea=root.transform.Find("视频区"); videoArea.SetParent(screenRoot.transform,false); Place(videoArea,.0f,.15625f,1,1);
        lesson.video.play.transform.SetParent(screenRoot.transform,false); Place(lesson.video.play.transform,0,0,.49f,.14f);
        lesson.video.replay.transform.SetParent(screenRoot.transform,false); Place(lesson.video.replay.transform,.51f,0,1,.14f);
        Cube("视频支架",new Vector3(-1.8f,.5f,.1f),new Vector3(.18f,1f,.18f),material);
        Cube("视频底座",new Vector3(-1.8f,.05f,.1f),new Vector3(.9f,.1f,.6f),material);

        var hud=new GameObject("历史探索提示",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler)); hud.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var hs=hud.GetComponent<CanvasScaler>(); hs.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; hs.referenceResolution=new Vector2(1440,900);
        var cross=Object.Instantiate(lesson.heading.gameObject,hud.transform); cross.name="准星"; Place(cross.transform,.48f,.47f,.52f,.53f); var ct=cross.GetComponent<TMP_Text>(); ct.text="+"; ct.alignment=TextAlignmentOptions.Center; ct.fontSize=25;
        var hint=Object.Instantiate(lesson.status.gameObject,hud.transform); hint.name="移动提示"; Place(hint.transform,.1f,.02f,.9f,.09f); var ht=hint.GetComponent<TMP_Text>(); ht.text="WASD 行走 · 鼠标转向 · 靠近护士开始交流"; ht.fontSize=22; ht.alignment=TextAlignmentOptions.Center;
        var label=Object.Instantiate(lesson.heading.gameObject); label.name="护士名称";
        var nc=new GameObject("护士名称画布",typeof(RectTransform),typeof(Canvas)); nc.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace; nc.GetComponent<RectTransform>().sizeDelta=new Vector2(300,80); nc.transform.position=new Vector3(0,2.3f,0); nc.transform.localScale=Vector3.one*.003f;
        label.transform.SetParent(nc.transform,false); Place(label.transform,0,0,1,1); label.GetComponent<TMP_Text>().text="护士"; label.GetComponent<TMP_Text>().alignment=TextAlignmentOptions.Center;
        var proximity=nurse.AddComponent<历史接近触发器>(); proximity.player=walker; proximity.lesson=lesson; proximity.panel=root.transform; proximity.exit=exitObj.GetComponent<Button>(); proximity.crosshair=cross; proximity.hint=ht;
        EditorSceneManager.SaveScene(scene,Path); Selection.activeGameObject=body;
    }
    private static void Place(Transform t,float x,float y,float r,float top)
    {var rt=(RectTransform)t; rt.anchorMin=new Vector2(x,y);rt.anchorMax=new Vector2(r,top);rt.offsetMin=rt.offsetMax=Vector2.zero;rt.localScale=Vector3.one;}
    private static void Cube(string name,Vector3 position,Vector3 size,Material material)
    {var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.position=position;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=material;}
}
