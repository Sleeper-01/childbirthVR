using System.IO;
using 产前运动;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using UnityEngine.Video;

public static class 历史演示生成器
{
    private const string Root = "Assets/产前运动";
    private static TMP_FontAsset font;
    private static readonly Color Ink = new Color32(35, 63, 64, 255);
    private static readonly Color Teal = new Color32(35, 105, 104, 255);
    private static readonly Color Paper = new Color32(247, 249, 245, 255);

    [MenuItem("产前运动/打开历史界面演示")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit play mode first.");
        string scenePath = Root + "/场景/历史界面演示.unity";
        if (File.Exists(scenePath)) { EditorSceneManager.OpenScene(scenePath); return; }
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            throw new System.InvalidOperationException("Save the current scene before creating the demo.");
        Directory.CreateDirectory(Root + "/场景"); Directory.CreateDirectory(Root + "/配置");
        AssetDatabase.Refresh();
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/字体/中文字体.asset");
        if (font == null)
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>(Root + "/字体/NotoSansCJKsc-Regular.otf");
            if (source == null) throw new System.InvalidOperationException("Chinese font not imported.");
            font = TMP_FontAsset.CreateFontAsset(source, 40, 5, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            font.name = "Chinese SDF";
            AssetDatabase.CreateAsset(font, Root + "/字体/中文字体.asset");
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, font);
        }
        var data = AssetDatabase.LoadAssetAtPath<运动讲解配置>(Root + "/配置/运动讲解配置.asset");
        if (data == null) { data = MakeCatalog(); AssetDatabase.CreateAsset(data, Root + "/配置/运动讲解配置.asset"); }
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane); ground.name = "地板"; ground.transform.localScale = new Vector3(3, 1, 3);
        var floorMaterial = MaterialAsset("地板材质", new Color32(218, 229, 219, 255));
        ground.GetComponent<Renderer>().sharedMaterial = floorMaterial;
        var nurse = GameObject.CreatePrimitive(PrimitiveType.Capsule); nurse.name = "护士"; nurse.transform.position = new Vector3(-1.5f, 1, 0);
        nurse.GetComponent<Renderer>().sharedMaterial = MaterialAsset("护士", new Color32(63, 139, 133, 255));
        var camera = new GameObject("固定摄像机", typeof(Camera)).GetComponent<Camera>();
        camera.tag = "MainCamera"; camera.transform.position = new Vector3(0, 4, -8); camera.transform.LookAt(new Vector3(0, 1, 0));
        camera.orthographic = true; camera.orthographicSize = 3.5f; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color32(229, 237, 230, 255);
        var light = new GameObject("主光源", typeof(Light)).GetComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.2f; light.transform.eulerAngles = new Vector3(45, -30, 0);
        RenderSettings.ambientLight = new Color(.72f, .76f, .74f);
        new GameObject("界面事件系统", typeof(EventSystem), typeof(StandaloneInputModule));
        var canvasObject = new GameObject("交互画布", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1440, 900); scaler.matchWidthOrHeight = 0.5f;
        var ui = canvasObject.transform;
        Box(ui, "标题背景", new Vector2(0, .89f), Vector2.one, Paper);
        Text(ui, "标题", "产前运动 · 护士陪伴", 32, new Vector2(.035f, .925f), new Vector2(.65f, .98f));
        Text(ui, "副标题", "卧位运动指导   /   文字互动演示", 17, new Vector2(.036f, .892f), new Vector2(.65f, .927f));
        Text(ui, "说明文字", "温和练习，从了解开始", 18, new Vector2(.73f, .92f), new Vector2(.98f, .965f));

        var system = new GameObject("对话管理器");
        var controller = system.AddComponent<讲解控制器>(); controller.catalog = data;
        var service = system.AddComponent<智能问答服务>(); service.catalog = data; controller.service = service;
        var heading = Text(ui, "运动标题", "从一次温和的练习开始", 27, new Vector2(.035f, .805f), new Vector2(.65f, .875f)); controller.heading = heading;
        var sidebar = Box(ui, "运动选择区", new Vector2(.025f, .365f), new Vector2(.24f, .80f), Paper);
        controller.exerciseButtons = new Button[5];
        for (int i = 0; i < 5; i++)
        {
            float y = .80f - i * .17f;
            controller.exerciseButtons[i] = Button(sidebar, "运动 " + (i + 1), "0" + (i + 1) + "   " + data.exercises[i].title, new Vector2(.04f, y), new Vector2(.96f, y + .145f), false);
        }
        Text(ui, "护士标识", "护士", 23, new Vector2(.37f, .69f), new Vector2(.46f, .74f), TextAlignmentOptions.Center);

        var videoRoot = Box(ui, "视频区", new Vector2(.605f, .415f), new Vector2(.975f, .79f), new Color32(32, 57, 58, 255));
        // AspectRatioFitter keeps the actual video image at 16:9 at every Game View size.
        var viewport = new GameObject("视频视口", typeof(RectTransform), typeof(AspectRatioFitter)); viewport.transform.SetParent(videoRoot, false);
        var aspect = viewport.GetComponent<AspectRatioFitter>(); aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent; aspect.aspectRatio = 16f / 9f;
        var videoImage = Box(viewport.transform, "视频画面", Vector2.zero, Vector2.one, Color.white).gameObject;
        Object.DestroyImmediate(videoImage.GetComponent<Image>()); var raw = videoImage.AddComponent<RawImage>();
        var texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(Root + "/配置/视频渲染纹理.renderTexture");
        if (texture == null) { texture = new RenderTexture(1280, 720, 0); texture.name = "视频渲染纹理"; AssetDatabase.CreateAsset(texture, Root + "/配置/视频渲染纹理.renderTexture"); } raw.texture = texture;
        var placeholder = Text(videoRoot, "视频占位提示", "暂无示范视频\n可先阅读护士讲解", 23, new Vector2(.06f, .2f), new Vector2(.94f, .8f), TextAlignmentOptions.Center); placeholder.color = Color.white;
        var player = system.AddComponent<VideoPlayer>(); player.playOnAwake = false; player.audioOutputMode = VideoAudioOutputMode.None; player.renderMode = VideoRenderMode.RenderTexture; player.targetTexture = texture; player.aspectRatio = VideoAspectRatio.FitInside;
        var video = system.AddComponent<视频控制器>(); controller.video = video; video.player = player; video.screen = raw; video.placeholder = placeholder;
        video.play = Button(ui, "播放按钮", "播放", new Vector2(.605f, .355f), new Vector2(.78f, .405f), true);
        video.replay = Button(ui, "重播按钮", "重播视频", new Vector2(.79f, .355f), new Vector2(.975f, .405f), false);
        video.playLabel = video.play.GetComponentInChildren<TMP_Text>(); raw.enabled = false;

        var panel = Box(ui, "底部对话框", new Vector2(.025f, .108f), new Vector2(.975f, .335f), Paper);
        var dialogue = panel.gameObject.AddComponent<对话面板>(); controller.dialogue = dialogue;
        dialogue.speaker = Text(panel, "角色名称", "护士 · 运动讲解", 19, new Vector2(.025f, .77f), new Vector2(.55f, .97f)); dialogue.speaker.color = Teal;
        dialogue.body = Text(panel, "当前对话文字", data.opening, 26, new Vector2(.025f, .27f), new Vector2(.975f, .77f)); dialogue.body.overflowMode = TextOverflowModes.Page;
        dialogue.pageLabel = Text(panel, "页码", "1 / 1", 15, new Vector2(.90f, .77f), new Vector2(.975f, .97f), TextAlignmentOptions.Right);
        controller.status = Text(panel, "状态提示", "请选择运动，或先了解安全提示", 15, new Vector2(.025f, .01f), new Vector2(.40f, .21f));
        controller.safety = Button(panel, "安全提示按钮", "安全提示", new Vector2(.42f, .025f), new Vector2(.55f, .235f), false);
        controller.restart = Button(panel, "重看按钮", "重看本项", new Vector2(.56f, .025f), new Vector2(.69f, .235f), false);
        controller.resume = Button(panel, "继续讲解按钮", "继续讲解", new Vector2(.70f, .025f), new Vector2(.83f, .235f), false);
        dialogue.next = Button(panel, "下一句按钮", "下一句", new Vector2(.84f, .025f), new Vector2(.975f, .235f), true);

        var inputRoot = Box(ui, "文字输入框", new Vector2(.025f, .025f), new Vector2(.83f, .09f), Color.white);
        var field = inputRoot.gameObject.AddComponent<TMP_InputField>(); field.lineType = TMP_InputField.LineType.SingleLine; field.characterLimit = 1000;
        var textArea = new GameObject("输入文字区域", typeof(RectTransform), typeof(RectMask2D)); textArea.transform.SetParent(inputRoot, false); Stretch(textArea.GetComponent<RectTransform>(), new Vector2(.015f, .1f), new Vector2(.985f, .9f));
        field.textViewport = textArea.GetComponent<RectTransform>();
        field.textComponent = Text(textArea.transform, "输入文字", "", 22, Vector2.zero, Vector2.one);
        field.fontAsset = font;
        var hint = Text(textArea.transform, "输入占位提示", "在这里输入问题，按回车或点击发送…", 22, Vector2.zero, Vector2.one); hint.color = new Color(.45f, .52f, .51f); field.placeholder = hint;
        field.targetGraphic = inputRoot.GetComponent<Image>();
        var input = inputRoot.gameObject.AddComponent<文字输入>(); input.field = field;
        input.send = Button(ui, "发送按钮", "发送", new Vector2(.845f, .025f), new Vector2(.975f, .09f), true); controller.input = input;
        AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene, scenePath);
        Selection.activeGameObject = system;
        Debug.Log("Prenatal demo created: " + scenePath);
    }

    private static Material MaterialAsset(string name, Color color)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(Root + "/配置/" + name + ".mat");
        if (existing != null) return existing;
        var material = new Material(Shader.Find("Standard")); material.color = color; material.SetFloat("_Glossiness", .15f);
        AssetDatabase.CreateAsset(material, Root + "/配置/" + name + ".mat"); return material;
    }
    private static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
    { rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero; }
    private static Transform Box(Transform parent, string name, Vector2 min, Vector2 max, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image)); obj.transform.SetParent(parent, false);
        Stretch(obj.GetComponent<RectTransform>(), min, max); obj.GetComponent<Image>().color = color; return obj.transform;
    }
    private static TMP_Text Text(Transform parent, string name, string value, int size, Vector2 min, Vector2 max, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); obj.transform.SetParent(parent, false);
        Stretch(obj.GetComponent<RectTransform>(), min, max);
        var text = obj.GetComponent<TextMeshProUGUI>(); text.font = font; text.fontSize = size; text.color = Ink;
        text.text = value; text.alignment = alignment; text.raycastTarget = false; text.richText = false; text.enableWordWrapping = true;
        return text;
    }
    private static Button Button(Transform parent, string name, string label, Vector2 min, Vector2 max, bool primary)
    {
        var obj = Box(parent, name, min, max, primary ? Teal : new Color32(226, 236, 229, 255));
        var button = obj.gameObject.AddComponent<Button>(); button.targetGraphic = obj.GetComponent<Image>();
        var colors = button.colors; colors.highlightedColor = new Color(.88f, .96f, .93f); colors.pressedColor = new Color(.7f, .85f, .8f); button.colors = colors;
        var labelText = Text(obj, "按钮文字", label, 18, new Vector2(.035f, .05f), new Vector2(.965f, .95f), TextAlignmentOptions.Center);
        labelText.color = primary ? Color.white : Ink; return button;
    }
    public static 运动讲解配置 MakeCatalog()
    {
        var data = ScriptableObject.CreateInstance<运动讲解配置>();
        data.opening = "产前适度运动可以增强盆底与核心肌群力量、促进血液循环、缓解腰背酸痛。今天我们一起练几项躺在床上就能完成的运动。";
        data.safety = new[] {
            "练习前请先确认已获医护人员许可。本演示用于了解动作，阅读进度不代表实际完成训练。全部运动为卧位低强度设计，采取舒适的半卧位，以不觉疲劳为度。",
            "练习中如遇宫缩变频、胎动异常、头晕心慌、腹痛或阴道流血流液，应立即停止并呼叫护士。",
            "静脉输液侧手臂不参与上肢伸展动作，可以只做健侧，或跳过该项。",
            "存在前置胎盘、先兆早产、妊娠期高血压、宫颈机能不全、多胎妊娠等情形，须经产科医生评估同意后方可练习。",
            "每项运动之间安排30秒休息；运动前后各补充适量温水。练习中保持自然呼吸，不要憋气。" };
        data.exercises = new[] {
            new 运动项目 { title = "凯格尔盆底肌训练", passages = new[] {
                "1 观看示范｜凯格尔训练用于增强盆底肌力，帮助预防产后漏尿，并为产后恢复和分娩控制作准备。把盆底肌想象成一部电梯：上行收缩、顶层保持、下行放松。有示范视频时可点击播放。",
                "2 意象引导｜采取舒适的半卧位，手自然放在腹前。想象电梯逐层上升，再慢慢下降，在心中默数节奏。本版以文字说明意象，不检测盆底肌收缩。",
                "3 节拍跟练｜材料中的基础节奏为收缩3秒、放松3秒，重复8组。是否实际练习及练习量，以医护人员对你的指导为准。这里不会计时或统计训练组数。",
                "4 变式提高｜材料中的进阶节奏为收缩5秒、放松5秒，重复4组。如感疲劳就休息或跳过，不勉强增加练习。",
                "5 呼吸配合｜全程自然呼吸，不要憋气。放松肩部，随着自己的呼吸节奏理解收缩与放松。本版没有麦克风或屏气检测。",
                "6 本项结束｜这项讲解已结束。你可以重看本项、向我提问，或选择下一项运动；如正在实际练习，请先休息30秒，以不觉疲劳为度。" } },
            new 运动项目 { title = "踝泵运动", passages = new[] {
                "运动介绍｜踝泵运动通过踝关节活动促进下肢血液回流，帮助减少卧床期间的下肢淤滞与水肿，是材料中预防血栓的辅助活动，并不替代医护安排的预防措施。",
                "动作说明｜保持舒适的半卧位，缓慢勾脚尖，再轻轻绷脚尖，交替进行。有视频时可跟随示范；本版不显示节奏光点，也不自动计数。",
                "注意事项｜动作温和，以不觉疲劳为度。如出现材料中列出的异常症状，立即停止并呼叫护士。",
                "本项结束｜你已看完踝泵运动的讲解。可重看或选择其他运动；实际运动之间安排30秒休息。" } },
            new 运动项目 { title = "上肢伸展与握拳", passages = new[] {
                "运动介绍｜小幅、舒缓的上肢活动有助于缓解僵硬、促进血液循环，可以只做健侧。",
                "动作说明｜在舒适半卧位下缓慢屈伸上肢，轻轻握拳再放开，动作幅度小而舒缓。有示范视频时可点击观看，无需手柄操作。",
                "注意事项｜静脉输液侧手臂不参与上肢伸展，可以只做健侧或跳过本项，不牵拉输液管路。",
                "本项结束｜讲解已完成。你可以提问或继续了解下一项，实际练习以不觉疲劳为度，并安排休息。" } },
            new 运动项目 { title = "卧位骨盆倾斜", passages = new[] {
                "运动介绍｜卧位骨盆倾斜有助于腹背肌协调，缓解腰背酸痛。先确认医护人员允许你进行这项练习。",
                "动作说明｜在舒适的卧位下屈膝，轻轻收腹、压床，再放松。动作轻柔，不追求幅度，可以通过后续添加的视频了解示范。",
                "注意事项｜保持自然呼吸，不要憋气；出现疼痛或不适时停止，呼叫护士。本版没有腹部动作追踪。",
                "本项结束｜你已看完骨盆倾斜讲解。可重看、提问或选择其他运动；实际练习之间休息30秒。" } },
            new 运动项目 { title = "拉玛泽呼吸预习", passages = new[] {
                "运动介绍｜廓清式呼吸练习用于放松身心，为宫缩期的呼吸配合作准备。",
                "动作说明｜保持舒适的半卧位，放松肩部，缓慢吸气，再舒缓呼气。可跟随后续添加的视频了解吸呼节奏。",
                "注意事项｜不要强行憋气或勉强配合节奏；若头晕心慌或出现其他异常，立即停止并呼叫护士。本版不提供光球或震动节拍。",
                "本项结束｜五项运动都可以从左侧选择回看。感谢你一起了解这些方法；实际是否练习、如何调整，请遵从产科医护人员的指导。" } }
        };
        return data;
    }
}
