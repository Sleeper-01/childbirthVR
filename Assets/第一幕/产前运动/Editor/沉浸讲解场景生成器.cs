using System.IO;
using 产前运动;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

public static class 沉浸讲解场景生成器
{
    const string 主场景 = "Assets/产前运动/场景/产前运动互动.unity";
    static TMP_FontAsset 字体;
    static Color 深色 = new Color32(26, 58, 62, 245), 浅色 = new Color32(236, 247, 242, 255);
    [MenuItem("产前运动/生成三维护士讲解场景")]
    public static void Build()
    {
        if(EditorApplication.isPlaying) throw new System.InvalidOperationException("请先停止运行。");
        if(Object.FindObjectOfType<护士顺序讲解>() != null) { 调整显示(); return; }
        var current=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(current.isDirty) EditorSceneManager.SaveScene(current);
        var source=EditorSceneManager.OpenScene(主场景);
        var backup=AssetDatabase.GenerateUniqueAssetPath("Assets/产前运动/场景/历史固定布局备份.unity");
        if(!EditorSceneManager.SaveScene(source,backup,true)) throw new System.Exception("备份失败，已停止改造。");
        字体=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/产前运动/字体/中文字体.asset");
        var original=AssetDatabase.LoadAssetAtPath<运动讲解配置>("Assets/产前运动/配置/运动讲解配置.asset");
        var config=AssetDatabase.LoadAssetAtPath<运动讲解配置>("Assets/产前运动/配置/护士对白配置.asset");
        if(config==null){config=Object.Instantiate(original);config.name="护士对白配置";填充对白(config);AssetDatabase.CreateAsset(config,"Assets/产前运动/配置/护士对白配置.asset");}
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.name="地板";floor.transform.localScale=new Vector3(2,1,2);
        floor.GetComponent<Renderer>().sharedMaterial=材质("讲解场地材质",new Color(.43f,.48f,.46f));
        var nurse=GameObject.CreatePrimitive(PrimitiveType.Capsule);nurse.name="护士";nurse.transform.position=new Vector3(.9f,1,1.9f);nurse.transform.localScale=new Vector3(.75f,1,.75f);
        nurse.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/产前运动/配置/护士.mat");
        var root=new GameObject("玩家视角根节点");
        var cam=new GameObject("玩家眼睛",typeof(Camera)).GetComponent<Camera>();cam.tag="MainCamera";cam.transform.SetParent(root.transform,false);cam.transform.localPosition=new Vector3(0,1.65f,0);
        cam.fieldOfView=65;cam.nearClipPlane=.05f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.52f,.65f,.71f);cam.cullingMask &= ~(1<<2);
        var player=GameObject.CreatePrimitive(PrimitiveType.Capsule);player.name="玩家胶囊占位";player.transform.SetParent(root.transform,false);player.transform.localPosition=new Vector3(0,.9f,0);player.transform.localScale=new Vector3(.6f,.9f,.6f);player.layer=2;
        var light=new GameObject("主光源",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.transform.eulerAngles=new Vector3(45,-25,0);light.intensity=.8f;
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.45f,.48f,.5f);
        new GameObject("界面事件系统",typeof(EventSystem),typeof(StandaloneInputModule));
        var system=new GameObject("护士讲解管理器");var controller=system.AddComponent<护士顺序讲解>();controller.配置=config;
        controller.问答=system.AddComponent<智能问答服务>();controller.问答.catalog=config;

        var dialogueCanvas=画布("玩家下方对白",cam,new Vector2(1100,320),.001f);
        dialogueCanvas.transform.SetParent(cam.transform,false);dialogueCanvas.transform.localPosition=new Vector3(0,-.35f,1.2f);
        // Canvas front is local -Z. Align its normal to the eye, without rotating the camera.
        dialogueCanvas.transform.localRotation=Quaternion.LookRotation(new Vector3(0,-.35f,1.2f),Vector3.up);
        var panel=框(dialogueCanvas.transform,"对白背景",0,0,1,1,深色);
        var dialogue=panel.gameObject.AddComponent<对话面板>();controller.对白=dialogue;
        dialogue.speaker=文字(panel,"护士姓名","护士",29,.025f,.82f,.65f,.97f);
        dialogue.body=文字(panel,"当前对白",config.opening,34,.025f,.25f,.975f,.80f);dialogue.body.overflowMode=TextOverflowModes.Page;
        dialogue.pageLabel=文字(panel,"对白页码","1 / 1",20,.87f,.82f,.975f,.97f);dialogue.pageLabel.alignment=TextAlignmentOptions.Right;
        dialogue.next=按钮(panel,"下一句按钮","下一句",.79f,.035f,.975f,.21f);
        controller.下一句文字=dialogue.next.GetComponentInChildren<TMP_Text>();
        controller.提问按钮=按钮(panel,"提问按钮","提问",.585f,.035f,.765f,.21f);
        controller.重看按钮=按钮(panel,"重看按钮","重看本项",.025f,.035f,.23f,.21f);
        var question=框(dialogueCanvas.transform,"提问输入区",0,-.27f,1,-.015f,深色);controller.提问区域=question.gameObject;
        var inputRoot=框(question,"问题输入框",.02f,.16f,.66f,.84f,new Color(.93f,.96f,.94f));
        var field=inputRoot.gameObject.AddComponent<TMP_InputField>();field.lineType=TMP_InputField.LineType.SingleLine;field.characterLimit=1000;
        var viewport=框(inputRoot,"输入裁剪区",.02f,.05f,.98f,.95f,Color.clear);viewport.gameObject.AddComponent<RectMask2D>();viewport.GetComponent<Image>().raycastTarget=false;
        field.textViewport=(RectTransform)viewport;
        field.textComponent=文字(viewport,"输入文字","",27,0,0,1,1);field.textComponent.color=深色;field.fontAsset=字体;
        var placeholder=文字(viewport,"输入提示","输入想向护士了解的问题…",27,0,0,1,1);placeholder.color=new Color(.4f,.48f,.46f);field.placeholder=placeholder;field.targetGraphic=inputRoot.GetComponent<Image>();
        var input=inputRoot.gameObject.AddComponent<文字输入>();input.field=field;input.send=按钮(question,"发送按钮","发送",.68f,.16f,.80f,.84f);controller.输入=input;
        controller.返回按钮=按钮(question,"返回讲解按钮","返回讲解",.82f,.16f,.98f,.84f);

        var videoCanvas=画布("视频播放屏",cam,new Vector2(1280,840),.0015f);videoCanvas.transform.position=new Vector3(-.9f,2.05f,2.7f);videoCanvas.transform.rotation=Quaternion.LookRotation(videoCanvas.transform.position-cam.transform.position,Vector3.up);
        var screenArea=框(videoCanvas.transform,"视频边框",0,.143f,1,1,深色);
        var imageObj=new GameObject("视频画面",typeof(RectTransform),typeof(RawImage));imageObj.transform.SetParent(screenArea,false);布局(imageObj.transform,0,0,1,1);
        var image=imageObj.GetComponent<RawImage>();image.raycastTarget=false;
        var texture=AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/产前运动/配置/视频渲染纹理.renderTexture");image.texture=texture;
        var videoObject=new GameObject("视频播放器");var vp=videoObject.AddComponent<VideoPlayer>();vp.playOnAwake=false;vp.audioOutputMode=VideoAudioOutputMode.None;vp.renderMode=VideoRenderMode.RenderTexture;vp.targetTexture=texture;vp.aspectRatio=VideoAspectRatio.FitInside;
        var video=videoObject.AddComponent<视频控制器>();video.player=vp;video.screen=image;
        video.placeholder=文字(screenArea,"视频占位文字","暂无示范视频",36,.05f,.15f,.95f,.85f);video.placeholder.alignment=TextAlignmentOptions.Center;
        video.play=按钮(videoCanvas.transform,"播放暂停按钮","播放",0,0,.49f,.12f);video.replay=按钮(videoCanvas.transform,"重播按钮","重播视频",.51f,0,1,.12f);video.playLabel=video.play.GetComponentInChildren<TMP_Text>();controller.视频=video;
        var stand=GameObject.CreatePrimitive(PrimitiveType.Cube);stand.name="视频支架";stand.transform.position=new Vector3(-.9f,.7f,2.8f);stand.transform.localScale=new Vector3(.15f,1.4f,.15f);stand.GetComponent<Renderer>().sharedMaterial=材质("视频支架材质",new Color(.2f,.3f,.3f));
        var nurseLabel=画布("护士标识",cam,new Vector2(300,80),.002f);nurseLabel.transform.position=new Vector3(.9f,2.25f,1.9f);nurseLabel.transform.rotation=Quaternion.LookRotation(nurseLabel.transform.position-cam.transform.position,Vector3.up);文字(nurseLabel.transform,"护士标识文字","护士",34,0,0,1,1).alignment=TextAlignmentOptions.Center;
        调整显示();
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,主场景);Selection.activeGameObject=root;
    }
    [MenuItem("产前运动/调整对白与视频显示")]
    public static void 调整显示()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("请先停止运行。");
        var c=Object.FindObjectOfType<护士顺序讲解>();
        if(c==null) throw new System.InvalidOperationException("请打开产前运动互动主场景。");
        字体=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/产前运动/字体/中文字体.asset");
        var canvas=c.对白.GetComponentInParent<Canvas>();
        canvas.transform.localScale=Vector3.one*.0012f;
        canvas.transform.localPosition=new Vector3(0,-.35f,1.2f);
        canvas.transform.localRotation=Quaternion.identity;
        c.对白.body.fontSize=38;
        c.对白.speaker.fontSize=32;
        foreach(var t in canvas.GetComponentsInChildren<TMP_Text>(true))
            if(t!=c.对白.body && t!=c.对白.speaker) t.fontSize=Mathf.Max(t.fontSize,28);
        var screen=c.视频.screen.GetComponentInParent<Canvas>();
        screen.transform.rotation=Camera.main.transform.rotation;
        screen.transform.position=new Vector3(-.9f,2.1f,2.7f);
        // 1280 x 720 picture, plus a separate 180 pixel control row.
        ((RectTransform)screen.transform).sizeDelta=new Vector2(1280,900);
        布局(c.视频.screen.transform.parent,0,.2f,1,1);
        布局(c.视频.play.transform,.255f,0,.495f,.11f);
        布局(c.视频.replay.transform,.505f,0,.745f,.11f);
        if(c.上个视频按钮==null) c.上个视频按钮=按钮(screen.transform,"上一个视频按钮","上一个视频",0,0,.245f,.11f);
        if(c.下个视频按钮==null) c.下个视频按钮=按钮(screen.transform,"下一个视频按钮","下一个视频",.755f,0,1,.11f);
        if(c.视频标题==null) c.视频标题=文字(screen.transform,"当前视频名称","1 / 5　"+c.配置.exercises[0].title,32,0,.115f,1,.195f);
        c.视频标题.alignment=TextAlignmentOptions.Center;
        c.视频标题.fontSize=44;
        c.视频.placeholder.fontSize=52;
        foreach(var button in screen.GetComponentsInChildren<Button>(true))
            button.GetComponentInChildren<TMP_Text>(true).fontSize=48;
        EditorUtility.SetDirty(c);
        EditorSceneManager.MarkSceneDirty(c.gameObject.scene);
        EditorSceneManager.SaveScene(c.gameObject.scene);
        温和医疗界面生成器.应用();
    }
    static Canvas 画布(string name,Camera camera,Vector2 size,float scale)
    {var o=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));var c=o.GetComponent<Canvas>();c.renderMode=RenderMode.WorldSpace;c.worldCamera=camera;((RectTransform)o.transform).sizeDelta=size;o.transform.localScale=Vector3.one*scale;return c;}
    static Transform 框(Transform parent,string name,float x,float y,float r,float top,Color color)
    {var o=new GameObject(name,typeof(RectTransform),typeof(Image));o.transform.SetParent(parent,false);布局(o.transform,x,y,r,top);o.GetComponent<Image>().color=color;return o.transform;}
    static void 布局(Transform t,float x,float y,float r,float top)
    {var rt=(RectTransform)t;rt.anchorMin=new Vector2(x,y);rt.anchorMax=new Vector2(r,top);rt.offsetMin=rt.offsetMax=Vector2.zero;rt.localScale=Vector3.one;}
    static TMP_Text 文字(Transform parent,string name,string value,int size,float x,float y,float r,float top)
    {var o=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));o.transform.SetParent(parent,false);布局(o.transform,x,y,r,top);var t=o.GetComponent<TextMeshProUGUI>();t.font=字体;t.fontSize=size;t.color=浅色;t.text=value;t.richText=false;t.raycastTarget=false;t.enableWordWrapping=true;t.alignment=TextAlignmentOptions.Left;return t;}
    static Button 按钮(Transform parent,string name,string value,float x,float y,float r,float top)
    {var t=框(parent,name,x,y,r,top,new Color(.18f,.4f,.4f));var b=t.gameObject.AddComponent<Button>();b.targetGraphic=t.GetComponent<Image>();文字(t,"按钮文字",value,27,.035f,.02f,.965f,.98f).alignment=TextAlignmentOptions.Center;return b;}
    static Material 材质(string name,Color color)
    {var path="Assets/产前运动/配置/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Standard"));m.color=color;m.SetFloat("_Glossiness",.1f);AssetDatabase.CreateAsset(m,path);}return m;}
    static void 填充对白(运动讲解配置 c)
    {
        c.opening="产前适度运动可以增强盆底与核心肌群力量、促进血液循环、缓解腰背酸痛。今天我们一起练几项躺在床上就能完成的运动。";
        c.safety=new[]{"全部运动均为卧位低强度设计，可以在舒适的半卧位下完成。以不觉疲劳为度，每项运动之间安排30秒休息，运动前后各补充适量温水。","练习中如遇宫缩变频、胎动异常、头晕心慌、腹痛或阴道流血流液，应立即停止并呼叫护士。","静脉输液侧手臂不参与上肢伸展动作，可只做健侧或跳过该项。","存在前置胎盘、先兆早产、妊娠期高血压、宫颈机能不全、多胎妊娠等情形，须经产科医生评估同意后方可练习。"};
        c.exercises[0].passages=new[]{"观看示范：先了解凯格尔盆底肌训练。它有助于增强盆底肌力、预防产后漏尿，利于产后恢复与分娩控制。把盆底肌想象成电梯：上行收缩、顶层保持、下行放松。","意象引导：手自然置于腹前，跟随讲解在心里默数。想象电梯逐层上行，然后慢慢下行，体会收缩与放松。","节拍跟练：收缩3秒、放松3秒为一组，跟随这一节奏完成8组收缩与放松。","变式提高：收缩5秒、放松5秒，继续跟练4组。感觉疲劳可以随时暂停或跳过。","呼吸配合：全程自然呼吸，不要憋气。保持自然呼吸，跟随收缩与放松的节奏。","凯格尔训练的讲解结束了。可以重看本项，或休息一下再继续下一项运动。"};
        c.exercises[1].passages=new[]{"接下来是踝泵运动。它有助于促进下肢血液回流，预防卧床期间下肢静脉血栓与水肿。","保持舒适的半卧位，跟随示范节奏，缓慢勾脚尖、绷脚尖，交替进行。","动作舒缓，以不觉疲劳为度。练习中出现异常症状，应立即停止并呼叫护士。","踝泵运动讲解结束。运动之间安排30秒休息，再继续下一项。"};
        c.exercises[2].passages=new[]{"接下来是上肢伸展与握拳，有助于缓解上肢僵硬、促进血液循环。","跟随示范缓慢屈伸上肢、握拳再放开，幅度小而舒缓。可以仅做健侧。","静脉输液侧手臂不参与伸展动作，可只做健侧，或跳过这一项。","上肢伸展与握拳讲解结束。先休息一下，再继续了解下一项运动。"};
        c.exercises[3].passages=new[]{"接下来是卧位骨盆倾斜，有助于增强腹背肌协调，缓解腰背酸痛。","在屈膝状态下，轻轻收腹压床，然后放松。跟随示范理解动作，不要求动作幅度。","以不觉疲劳为度。如出现不适，应立即停止并呼叫护士。","骨盆倾斜讲解结束。休息30秒后，可以继续呼吸练习的讲解。"};
        c.exercises[4].passages=new[]{"最后是拉玛泽呼吸预习。廓清式呼吸有助于放松身心，为宫缩期呼吸镇痛打基础。","跟随示范，配合吸气与呼气的节奏，保持舒适放松。","保持自然呼吸，不要憋气。若出现头晕心慌等不适，立即停止并呼叫护士。","拉玛泽呼吸预习讲解结束。你可以重看本项，或结束今天的讲解。"};
    }
}
