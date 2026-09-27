using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;
using VRHospitalAI.ThirdAct;
using VRHospitalAI;

namespace VRHospitalAI.ThirdAct
{
/// <summary>
/// 第三幕场景自动搭建器。
/// 运行后自动创建所有 GameObject、UI 面板、3D 环境和组件引用。
/// </summary>
[DefaultExecutionOrder(-1000)]
public class ThirdActAutoBuilder : MonoBehaviour
{
    void Start()
    {
        BuildAll();
        Destroy(gameObject);
    }

    void BuildAll()
    {
        // ── 相机与灯光 ──────────────────────────────────────
        var mainCam = new GameObject("Main Camera");
        mainCam.AddComponent<Camera>();
        mainCam.AddComponent<AudioListener>();
        mainCam.AddComponent<SimpleCameraController>();
        mainCam.transform.position = new Vector3(0, 2.5f, -8);
        mainCam.transform.rotation = Quaternion.identity;

        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.93f, 0.84f);   // 主光：暖色主光源
        light.intensity = 1.05f;
        light.shadows = LightShadows.None;
        lightGO.transform.rotation = Quaternion.Euler(35f, 40f, 0f);

        // 辅光：冷色低强度补光，与主光形成色温差，让形体有体积感
        var fillGO = new GameObject("Fill Light");
        var fill = fillGO.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(0.72f, 0.82f, 1f);
        fill.intensity = 0.30f;
        fill.shadows = LightShadows.None;
        fill.renderMode = LightRenderMode.ForceVertex;  // 补光走顶点光照，性能无损
        fillGO.transform.rotation = Quaternion.Euler(-15f, 215f, 0f);

        // 真实环境光：Flat 暖深灰（原 Skybox 模式无天空盒≈无环境光，暗部死黑）
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.50f, 0.44f, 0.40f);

        // ── 3D 环境 ─────────────────────────────────────────
        Create3DWalls();
        Create3DFloor();

        // 局部重点光：暖色点光提亮产房中心（视觉中心），形成前景/中景/背景层次
        var spotGO = new GameObject("Accent Light");
        var spot = spotGO.AddComponent<Light>();
        spot.type = LightType.Point;
        spot.color = new Color(1f, 0.88f, 0.72f);
        spot.intensity = 0.85f;
        spot.range = 9f;
        spot.transform.position = new Vector3(0f, 2.8f, -5f);

        // ── 核心管理器 ──────────────────────────────────────
        var scoreMgr = new GameObject("ScoreManager");
        scoreMgr.AddComponent<ScoreManager>();

        var fadeGO = new GameObject("FadeCanvas");
        fadeGO.AddComponent<CameraFade>();

        // ── EventSystem（独立，不绑定到任何 Canvas） ──────────
        var eventSystemGO = new GameObject("EventSystem");
        eventSystemGO.AddComponent<EventSystem>();
        eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // ── HUD（含计时器、字幕层） ─────────────────────────
        var hudGO = new GameObject("ThirdActHUD");
        var hud = hudGO.AddComponent<ThirdActHUD>();
        var hudCanvas = CreateCanvas("ThirdActHUD_Canvas", sortingOrder: 2000);
        hud.scorePanel = CreatePanelOnCanvas(hudCanvas, "ScorePanel", new Vector2(-Screen.width / 2 + 100, Screen.height / 2 - 30), new Vector2(180, 40));
        hud.scoreFill = hud.scorePanel.GetComponent<Image>();
        hud.scoreFill.fillMethod = Image.FillMethod.Horizontal;
        hud.scoreFill.fillAmount = 0.6f;
        hud.scoreFill.color = new Color(1f, 0.6f, 0.6f);  // 暖粉色安心值条
        hud.scoreText = AddText(hud.scorePanel, "ScoreText", new Vector2(190, 0), "安心值: 60", 18);

        // 安全提醒文字（默认隐藏）
        hud.reminderText = AddText(hudCanvas.gameObject, "ReminderText", new Vector2(0, Screen.height / 2 - 150), "", 20, ProStyle.Warn);
        hud.reminderText.gameObject.SetActive(false);

        // 步骤提示
        hud.stepHintPanel = CreatePanelOnCanvas(hudCanvas, "StepHintPanel", new Vector2(0, Screen.height / 2 - 100), new Vector2(700, 40));
        hud.stepHintText = AddText(hud.stepHintPanel, "HintText", new Vector2(0, 0), "", 20);

        // 字幕层（底部黑底白字）
        hud.subtitlePanel = CreatePanelOnCanvas(hudCanvas, "SubtitlePanel", new Vector2(0, -Screen.height / 2 + 60), new Vector2(900, 60));
        (hud.subtitlePanel.GetComponent<Image>() as Image).color = new Color(ProStyle.Panel.r, ProStyle.Panel.g, ProStyle.Panel.b, 0.7f);
        hud.subtitleText = AddText(hud.subtitlePanel, "SubtitleText", new Vector2(0, 0), "", 24);  // 与全局刻度 FontBody(24) 对齐

        // 工具栏（D2 合并）：帮助/重播/暂停由全局工具栏（ActFlow 常驻）承担，
        // 本面板改为透明宿主，仅承载流程所需的「确 定」确认按钮——通栏 UI 条不再出现。
        hud.toolbarPanel = CreatePanelOnCanvas(hudCanvas, "Toolbar", new Vector2(0, -Screen.height / 2 + 25), new Vector2(Screen.width, 50));
        (hud.toolbarPanel.GetComponent<Image>() as Image).color = Color.clear;

        // 全局确认按钮（暖粉色）
        var confirmBtnGO = new GameObject("ConfirmBtn");
        confirmBtnGO.transform.SetParent(hud.toolbarPanel.transform, false);
        var confirmRect = confirmBtnGO.AddComponent<RectTransform>();
        confirmRect.anchorMin = new Vector2(0.5f, 0.5f);
        confirmRect.anchorMax = new Vector2(0.5f, 0.5f);
        confirmRect.pivot = new Vector2(0.5f, 0.5f);
        confirmRect.anchoredPosition = new Vector2(300, 0);
        confirmRect.sizeDelta = new Vector2(160, 36);
        var confirmImg = confirmBtnGO.AddComponent<Image>();
        confirmImg.color = ProStyle.ButtonBg;  // 序幕按钮底
        var confirmBtn = confirmBtnGO.AddComponent<Button>();
        var confirmTextGO = new GameObject("Text");
        confirmTextGO.transform.SetParent(confirmBtnGO.transform, false);
        var confirmTextRect = confirmTextGO.AddComponent<RectTransform>();
        confirmTextRect.anchorMin = Vector2.zero;
        confirmTextRect.anchorMax = Vector2.one;
        var confirmTextComp = confirmTextGO.AddComponent<Text>();
        confirmTextComp.text = "确 定";
        confirmTextComp.fontSize = 20;
        confirmTextComp.color = ProStyle.TextMain;
        confirmTextComp.alignment = TextAnchor.MiddleCenter;
        confirmTextComp.font = ProStyle.Font;

        // 暂停菜单（默认隐藏）
        hud.pauseMenu = CreatePanelOnCanvas(hudCanvas, "PauseMenu", new Vector2(0, 0), new Vector2(Screen.width, Screen.height));
        (hud.pauseMenu.GetComponent<Image>() as Image).color = new Color(0, 0, 0, 0.85f);
        hud.pauseMenu.SetActive(false);
        hud.resumeButton = AddButton(hud.pauseMenu, "ResumeBtn", new Vector2(0, 50), "继续", 24);
        hud.quitButton = AddButton(hud.pauseMenu, "QuitBtn", new Vector2(0, -30), "返回主菜单", 24);

        // 帮助面板（默认隐藏）
        hud.helpPanel = CreatePanelOnCanvas(hudCanvas, "HelpPanel", new Vector2(0, 0), new Vector2(450, 280));
        (hud.helpPanel.GetComponent<Image>() as Image).color = ProStyle.Panel;
        hud.helpContentText = AddText(hud.helpPanel, "HelpText", new Vector2(0, 0), "", 16);
        hud.helpPanel.SetActive(false);

        // ── 控制器 ──────────────────────────────────────────
        var controllerGO = new GameObject("ThirdActController");
        var controller = controllerGO.AddComponent<ThirdActController>();
        controller.scoreManager = FindObjectOfType<ScoreManager>();
        controller.cameraFade = FindObjectOfType<CameraFade>();
        controller.hud = hud;

        // 保存 HUD Canvas 引用，避免 FindObjectOfType 找到错误的 Canvas
        Canvas hudCanvasRef = hudCanvas;

        // ── 药物镇痛模块 ────────────────────────────────────
        var drugGO = new GameObject("DrugPainModule");
        var drug = drugGO.AddComponent<DrugPainModule>();
        drug.scoreManager = FindObjectOfType<ScoreManager>();
        drug.cameraFade = FindObjectOfType<CameraFade>();
        drug.hud = hud;
        drug.confirmButton = confirmBtn;  // 传递全局确认按钮
        BuildDrugPainUI(drug, hudCanvasRef);
        Debug.Log("[第三幕] DrugPainModule 已创建，explanationPanel: " + (drug.explanationPanel != null));
        if (drug.explanationPanel != null)
            Debug.Log("[第三幕] explanationPanel active: " + drug.explanationPanel.activeSelf);

        // ── 呼吸训练模块 ────────────────────────────────────
        var breatheGO = new GameObject("BreathingModule");
        var breathe = breatheGO.AddComponent<BreathingModule>();
        breathe.scoreManager = FindObjectOfType<ScoreManager>();
        breathe.cameraFade = FindObjectOfType<CameraFade>();
        breathe.hud = hud;
        BuildBreathingUI(breathe, hudCanvasRef);

        // ── 镇痛决策模块 ────────────────────────────────────
        var toolboxGO = new GameObject("ToolboxModule");
        var toolbox = toolboxGO.AddComponent<ToolboxModule>();
        toolbox.scoreManager = FindObjectOfType<ScoreManager>();
        toolbox.cameraFade = FindObjectOfType<CameraFade>();
        toolbox.hud = hud;
        BuildToolboxUI(toolbox, hudCanvasRef);

        // ── 连线控制器 ──────────────────────────────────────
        controller.drugModule = drug;
        controller.breathingModule = breathe;
        controller.toolboxModule = toolbox;

        // 按 sortingOrder 调整 Canvas 的 Hierarchy 顺序
        SortCanvasesBySortingOrder();

        // ── 简易医生模型（胶囊） ────────────────────────────
        var doctorGO = new GameObject("Doctor");
        doctorGO.transform.position = new Vector3(0, 0.9f, -6);
        var doctorCapsule = doctorGO.AddComponent<CapsuleCollider>();
        doctorCapsule.direction = 1;
        doctorCapsule.radius = 0.4f;
        doctorCapsule.height = 1.8f;
        var doctorMesh = doctorGO.AddComponent<MeshFilter>();
        var doctorMeshRenderer = doctorGO.AddComponent<MeshRenderer>();
        var doctorMat = new Material(Shader.Find("Standard"));
        doctorMat.color = new Color(1f, 0.88f, 0.82f);  // 暖肤色
        doctorMeshRenderer.sharedMaterial = doctorMat;

        var headGO = new GameObject("Head");
        headGO.transform.SetParent(doctorGO.transform, false);
        headGO.transform.position = new Vector3(0, 0.9f, 0);
        var headSphere = headGO.AddComponent<SphereCollider>();
        headSphere.radius = 0.25f;
        var headMesh = headGO.AddComponent<MeshFilter>();
        var headRenderer = headGO.AddComponent<MeshRenderer>();
        var headMat = new Material(Shader.Find("Standard"));
        headMat.color = new Color(1f, 0.85f, 0.7f);
        headRenderer.sharedMaterial = headMat;

        // ── 麻醉医生 NPC ────────────────────────────────────
        var npcGO = new GameObject("Anesthesiologist");
        npcGO.AddComponent<NPCBehaviour>();
        var npc = npcGO.GetComponent<NPCBehaviour>();
        npc.npcId = "anesthesiologist";
        npc.displayName = "王医生";
        drug.anesthesiologistNPC = npc;

        // ── VR 射线交互器 ──────────────────────────────────
        var interactorGO = new GameObject("VRInteractor");
        var interactor = interactorGO.AddComponent<VRInteractor>();
        interactor.rayOrigin = mainCam.transform;
        interactor.maxDistance = 10f;
        var lineRenderer = interactorGO.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.02f;
        lineRenderer.endWidth = 0.02f;
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.cyan, 0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );
        interactor.normalColor = grad;
        interactor.lineRenderer = lineRenderer;

        // 通知 Controller 场景已就绪，启动流程
        controller.InitAndStart();

        Debug.Log("[第三幕] 场景搭建完成！请按 Ctrl+S 保存场景。");
    }

    // ── 公开的静态 UI 构建方法（供 Controller 运行时补全时调用） ────────────

    public static Canvas CreateCanvasAndGet(string name, int sortingOrder = 1000)
    {
        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        canvas.pixelPerfect = true;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    public static void BuildDrugPainUIModular(DrugPainModule drug, Canvas canvas)
    {
        if (canvas == null) return;
        var explPanel = CreatePanelOnCanvasStatic(canvas, "ExplanationPanel", new Vector2(0, 0), new Vector2(900, 450));
        drug.explanationPanel = explPanel;
        drug.explanationTitle = AddTextStatic(explPanel, "Title", new Vector2(0, 160), "title", 28, ProStyle.Warn);
        drug.explanationBody = AddTextStatic(explPanel, "Body", new Vector2(0, 60), "body", 20);
        drug.btnNextCard = AddButtonStatic(explPanel, "NextCard", new Vector2(-200, -160), "下一张", 18);
        drug.btnCloseCard = AddButtonStatic(explPanel, "CloseCard", new Vector2(200, -160), "结束讲解", 18);
        drug.btnStartVideo = AddButtonLargeStatic(explPanel, "StartVideo", new Vector2(0, -190), "确  定", 28);

        // 全局确认按钮（屏幕最底部）
        var confirmBtnGO = new GameObject("ConfirmBtn");
        confirmBtnGO.transform.SetParent(canvas.transform, false);
        var confirmRect = confirmBtnGO.AddComponent<RectTransform>();
        confirmRect.anchorMin = new Vector2(0.5f, 0f);
        confirmRect.anchorMax = new Vector2(0.5f, 0f);
        confirmRect.pivot = new Vector2(0.5f, 0.5f);
        confirmRect.anchoredPosition = new Vector2(0, 20);
        confirmRect.sizeDelta = new Vector2(200, 45);
        var confirmImg = confirmBtnGO.AddComponent<Image>();
        confirmImg.color = ProStyle.ButtonBg;  // 序幕按钮底
        var confirmBtn = confirmBtnGO.AddComponent<Button>();
        var confirmTextGO = new GameObject("Text");
        confirmTextGO.transform.SetParent(confirmBtnGO.transform, false);
        var confirmTextRect = confirmTextGO.AddComponent<RectTransform>();
        confirmTextRect.anchorMin = Vector2.zero;
        confirmTextRect.anchorMax = Vector2.one;
        var confirmTextComp = confirmTextGO.AddComponent<Text>();
        confirmTextComp.text = "确 定";
        confirmTextComp.fontSize = 20;
        confirmTextComp.color = ProStyle.TextMain;
        confirmTextComp.alignment = TextAnchor.MiddleCenter;
        confirmTextComp.font = ProStyle.Font;
        confirmBtn.interactable = false;
        drug.confirmButton = confirmBtn;

        var demoPanel = CreatePanelOnCanvasStatic(canvas, "PositionDemoPanel", new Vector2(0, 0), new Vector2(900, 450));
        drug.positionDemoPanel = demoPanel;
        drug.positionDemoTitle = AddTextStatic(demoPanel, "DemoTitle", new Vector2(0, 160), "title", 28, ProStyle.Warn);
        drug.positionDemoBody = AddTextStatic(demoPanel, "DemoBody", new Vector2(0, 50), "body", 20);
        drug.btnNextToQuiz = AddButtonLargeStatic(demoPanel, "NextToQuiz", new Vector2(0, -190), "知道了，继续答题", 22);

        var quizPanel = CreatePanelOnCanvasStatic(canvas, "PositionQuizPanel", new Vector2(0, 0), new Vector2(900, 450));
        drug.positionQuizPanel = quizPanel;
        drug.questionText = AddTextStatic(quizPanel, "QuestionText", new Vector2(0, 160), "题目", 22, ProStyle.Warn);
        for (int i = 0; i < 3; i++)
            drug.positionQuizButtons[i] = AddButtonStatic(quizPanel, $"Option{i}", new Vector2(0, 60 - i * 80), $"选项{i + 1}", 18);
        drug.feedbackText = AddTextStatic(quizPanel, "FeedbackText", new Vector2(0, -160), "", 18);

        var conPanel = CreatePanelOnCanvasStatic(canvas, "ContractionPanel", new Vector2(0, 0), new Vector2(900, 350));
        drug.contractionPanel = conPanel;
        drug.contractionIntensityText = AddTextStatic(conPanel, "IntensityText", new Vector2(0, 150), "等待宫缩预警...", 20, ProStyle.Danger);
        var barGO = new GameObject("IntensityBar");
        barGO.transform.SetParent(conPanel.transform, false);
        var barRect = barGO.AddComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 0.5f);
        barRect.anchorMax = new Vector2(0.5f, 0.5f);
        barRect.pivot = new Vector2(0.5f, 0.5f);
        barRect.anchoredPosition = new Vector2(0, 70);
        barRect.sizeDelta = new Vector2(500, 20);
        var barImg = barGO.AddComponent<Image>();
        barImg.color = ProStyle.Danger;
        barImg.type = Image.Type.Filled;
        barImg.fillMethod = Image.FillMethod.Horizontal;
        barImg.fillAmount = 0f;
        drug.contractionBar = barImg;
        drug.callNurseButton = AddButtonStatic(conPanel, "CallNurse", new Vector2(0, -30), "呼叫护士", 18);
        string[] cardTexts = { "宫缩来了，有点疼", "我准备好用力了", "我需要镇痛药", "我很紧张，请陪陪我" };
        for (int i = 0; i < 4; i++)
            drug.voiceCards[i] = AddButtonStatic(conPanel, $"VoiceCard{i}", new Vector2(-270 + i * 140, -120), cardTexts[i], 14);
    }

    public static void BuildBreathingUIModular(BreathingModule breathe, Canvas canvas)
    {
        if (canvas == null) return;
        var breathPanel = CreatePanelOnCanvasStatic(canvas, "BreathingPanel", new Vector2(0, 0), new Vector2(800, 600));
        // 呼吸面板背景设为透明，只显示内部的光球、文字、按钮
        var breathPanelImg = breathPanel.GetComponent<Image>();
        if (breathPanelImg != null)
            breathPanelImg.color = new Color(0.1f, 0.1f, 0.1f, 0f);
        breathe.breathingPanel = breathPanel;
        var sphereGO = new GameObject("BreatheSphere");
        sphereGO.transform.SetParent(breathPanel.transform, false);
        var sphereRect = sphereGO.AddComponent<RectTransform>();
        sphereRect.anchorMin = new Vector2(0.5f, 0.5f);
        sphereRect.anchorMax = new Vector2(0.5f, 0.5f);
        sphereRect.pivot = new Vector2(0.5f, 0.5f);
        sphereRect.sizeDelta = new Vector2(200, 200);
        sphereRect.anchoredPosition = Vector2.zero;
        var sphereImg = sphereGO.AddComponent<Image>();
        sphereImg.color = new Color(ProStyle.Accent.r, ProStyle.Accent.g, ProStyle.Accent.b, 0.6f);  // 暖粉色光球
        sphereImg.type = Image.Type.Filled;
        sphereImg.fillMethod = Image.FillMethod.Radial360;
        sphereImg.fillOrigin = 1;  // Top (1) - 从顶部开始填充
        sphereImg.fillAmount = 0.5f;
        breathe.breatheSphere = sphereImg;
        breathe.breatheText = AddTextStatic(breathPanel, "BreatheText", new Vector2(0, -160), "跟随光球呼吸", 28);
        breathe.matchPercentText = AddTextStatic(breathPanel, "MatchText", new Vector2(0, -210), "吻合度: 等待中...", 20, ProStyle.Accent);

        // 练习模式按钮：点击增加10%匹配度
        var practiceBtnGO = new GameObject("PracticeModeBtn");
        practiceBtnGO.transform.SetParent(breathPanel.transform, false);
        var practiceRect = practiceBtnGO.AddComponent<RectTransform>();
        practiceRect.anchorMin = new Vector2(0.5f, 0f);
        practiceRect.anchorMax = new Vector2(0.5f, 0f);
        practiceRect.pivot = new Vector2(0.5f, 0.5f);
        practiceRect.anchoredPosition = new Vector2(0, 15);
        practiceRect.sizeDelta = new Vector2(180, 40);
        var practiceImg = practiceBtnGO.AddComponent<Image>();
        practiceImg.color = ProStyle.ButtonBg;  // 序幕按钮底
        var practiceBtn = practiceBtnGO.AddComponent<Button>();
        var practiceTextGO = new GameObject("Text");
        practiceTextGO.transform.SetParent(practiceBtnGO.transform, false);
        var practiceTextRect = practiceTextGO.AddComponent<RectTransform>();
        practiceTextRect.anchorMin = Vector2.zero;
        practiceTextRect.anchorMax = Vector2.one;
        var practiceTextComp = practiceTextGO.AddComponent<Text>();
        practiceTextComp.text = "练习模式(+10%)";
        practiceTextComp.fontSize = 18;
        practiceTextComp.color = ProStyle.TextMain;
        practiceTextComp.alignment = TextAnchor.MiddleCenter;
        var practiceFont = Resources.Load<Font>("Fonts/simhei");
        if (practiceFont != null) practiceTextComp.font = practiceFont;
        breathe.practiceModeBtn = practiceBtn;

        // 场景背景色块面板（新增）- 放在 Canvas 层级，覆盖全屏
        var sceneBgGO = new GameObject("SceneBgPanel");
        sceneBgGO.transform.SetParent(canvas.transform, false);  // 改为 Canvas 子节点
        var sceneBgRect = sceneBgGO.AddComponent<RectTransform>();
        sceneBgRect.anchorMin = Vector2.zero;
        sceneBgRect.anchorMax = Vector2.one;
        sceneBgRect.offsetMin = Vector2.zero;
        sceneBgRect.offsetMax = Vector2.zero;
        var sceneBgImg = sceneBgGO.AddComponent<Image>();
        sceneBgImg.color = new Color(0.15f, 0.15f, 0.2f, 0f);  // 初始透明
        sceneBgImg.raycastTarget = false;  // 不拦截点击
        breathe.sceneBgPanel = sceneBgGO;

        var musicPanel = CreatePanelOnCanvasStatic(canvas, "MusicPanel", new Vector2(0, 0), new Vector2(750, 500));
        breathe.musicPanel = musicPanel;
        breathe.musicTitleText = AddTextStatic(musicPanel, "MusicTitle", new Vector2(0, 210), "选择音乐与场景", 28, ProStyle.Warn);
        string[] musicNames = { "海浪轻拍", "森林鸟鸣", "钢琴抒情", "自然白噪音" };
        for (int i = 0; i < 4; i++)
            breathe.musicButtons[i] = AddButtonStatic(musicPanel, $"Music{i}", new Vector2(-180, 120 - i * 55), musicNames[i], 16);
        string[] sceneNames = { "海边日落", "森林小径", "山间溪流", "花园秋千" };
        for (int i = 0; i < 4; i++)
            breathe.sceneButtons[i] = AddButtonStatic(musicPanel, $"Scene{i}", new Vector2(60, 120 - i * 55), sceneNames[i], 16);

        var massagePanel = CreatePanelOnCanvasStatic(canvas, "MassagePanel", new Vector2(0, 0), new Vector2(700, 350));
        breathe.massagePanel = massagePanel;
        breathe.massageTitleText = AddTextStatic(massagePanel, "MassageTitle", new Vector2(0, 170), "选择按摩力度", 28, ProStyle.Warn);
        string[] pressureNames = { "轻柔", "适中", "稍重" };
        for (int i = 0; i < 3; i++)
            breathe.pressureButtons[i] = AddButtonStatic(massagePanel, $"Pressure{i}", new Vector2(-150 + i * 150, -80), pressureNames[i], 18);
        breathe.massageTipText = AddTextStatic(massagePanel, "MassageTip", new Vector2(0, -30), "请选择按摩力度", 16);
        breathe.massageDetailText = AddTextStatic(massagePanel, "MassageDetail", new Vector2(0, -110), "", 14, ProStyle.TextMain);
        // 按摩确认按钮
        breathe.massageConfirmBtn = AddButtonStatic(massagePanel, "MassageConfirm", new Vector2(0, -160), "确认，继续", 16);

        var posPanel = CreatePanelOnCanvasStatic(canvas, "PositionPanel", new Vector2(0, 0), new Vector2(850, 380));
        breathe.positionPanel = posPanel;
        breathe.positionCardTitle = AddTextStatic(posPanel, "PosTitle", new Vector2(0, 190), "title", 28, ProStyle.Warn);
        breathe.positionCardDesc = AddTextStatic(posPanel, "PosDesc", new Vector2(0, -10), "desc", 16);
        string[] posNames = { "左倾卧位", "半卧位", "屈膝垫枕位", "分娩球", "自由体位" };
        for (int i = 0; i < 5; i++)
            breathe.positionCardButtons[i] = AddButtonStatic(posPanel, $"PosCard{i}", new Vector2(-250 + i * 120, -160), posNames[i], 14);
        // 体位卡片确认按钮（最后一张卡片显示）
        breathe.positionConfirmBtn = AddButtonStatic(posPanel, "PosConfirm", new Vector2(0, -180), "确认完成", 18);
    }

    public static void BuildToolboxUIModular(ToolboxModule toolbox, Canvas canvas)
    {
        if (canvas == null) return;
        var tbPanel = CreatePanelOnCanvasStatic(canvas, "ToolboxPanel", new Vector2(0, 0), new Vector2(850, 480));
        toolbox.toolboxPanel = tbPanel;
        toolbox.toolTitleText = AddTextStatic(tbPanel, "ToolTitle", new Vector2(0, 200), "选择法宝放入待产包（0/2）", 20, ProStyle.Warn);
        string[] toolNames = { "呼吸卡", "分娩球", "镇痛泵", "按摩手", "音乐盒" };
        for (int i = 0; i < 5; i++)
            toolbox.toolButtons[i] = AddButtonStatic(tbPanel, $"Tool{i}", new Vector2(-260 + i * 130, -150), toolNames[i], 16);

        var bagPanel = CreatePanelOnCanvasStatic(canvas, "BaggagePanel", new Vector2(350, -180), new Vector2(200, 80));
        toolbox.baggagePanel = bagPanel;
        toolbox.baggageCountText = AddTextStatic(bagPanel, "BagCount", new Vector2(0, 0), "0/2", 18, ProStyle.Accent);
        for (int i = 0; i < 2; i++)
        {
            var slot = new GameObject($"BagSlot{i}");
            slot.transform.SetParent(bagPanel.transform, false);
            var slotRect = slot.AddComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = new Vector2(-50 + i * 100, 0);
            slotRect.sizeDelta = new Vector2(60, 60);
            var slotImg = slot.AddComponent<Image>();
            slotImg.color = new Color(ProStyle.PanelHover.r, ProStyle.PanelHover.g, ProStyle.PanelHover.b, 0.5f);
            toolbox.baggageSlots[i] = slot;
        }

        var qPanel = CreatePanelOnCanvasStatic(canvas, "QuestionPanel", new Vector2(0, 0), new Vector2(800, 500));
        toolbox.questionPanel = qPanel;
        toolbox.questionText = AddTextStatic(qPanel, "QuestionText", new Vector2(0, 200), "问题", 22, ProStyle.Warn);
        toolbox.responseText = AddTextStatic(qPanel, "ResponseText", new Vector2(0, -200), "护士回应", 18, ProStyle.Success);
        string[] answers = { "呼吸法我最有把握", "镇痛泵听起来最安心", "音乐和按摩能让我放松", "想都试试多一种选择", "还没想好" };
        for (int i = 0; i < 5; i++)
            toolbox.answerButtons[i] = AddButtonStatic(qPanel, $"Answer{i}", new Vector2(0, -100 - i * 50), answers[i], 16);
        toolbox.SetupAnswerButtons(toolbox.answerButtons);

        // 确认选择按钮（选1件时使用）
        toolbox.confirmButton = AddButtonStatic(tbPanel, "ConfirmBtn", new Vector2(0, -220), "确认选择，继续", 18);
        toolbox.confirmButton.gameObject.SetActive(false);  // 初始隐藏，选1件后显示
    }

    Canvas CreateCanvas(string name, int sortingOrder = 1000)
    {
        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        canvas.pixelPerfect = true;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    // ScreenSpaceOverlay 模式下，渲染顺序由 siblingIndex 决定
    // siblingIndex 越大 = 渲染越晚 = 显示在最上层
    void SortCanvasesBySortingOrder()
    {
        var canvases = FindObjectsOfType<Canvas>()
            .OrderBy(c => c.sortingOrder)
            .ToArray();
        // sortingOrder 低的放前面(siblingIndex=0)，高的放后面(最大siblingIndex)
        for (int i = 0; i < canvases.Length; i++)
        {
            canvases[i].transform.SetSiblingIndex(i);
        }
    }

    GameObject CreatePanelOnCanvas(Canvas canvas, string name, Vector2 pos, Vector2? size = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(canvas.transform, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size ?? new Vector2(200, 50);
        var img = go.AddComponent<Image>();
        ProStyle.MakePanel(img, 0.85f);
        return go;
    }

    GameObject CreatePanel(GameObject parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        var img = go.AddComponent<Image>();
        ProStyle.MakePanel(img, 0.85f);
        return go;
    }

    Text AddText(GameObject parent, string name, Vector2 pos, string content = "", int fontSize = 36, Color? color = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(700, 60);
        var text = go.AddComponent<Text>();
        text.text = content;
        text.fontSize = Mathf.Max(fontSize, 18);
        text.color = color ?? ProStyle.TextMain;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.font = ProStyle.Font;
        return text;
    }

    Button AddButton(GameObject parent, string name, Vector2 pos, string label, int fontSize = 18)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(120, 36);
        var img = go.AddComponent<Image>();
        var btn = go.AddComponent<Button>();
        ProStyle.MakeButton(img, btn);
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        var text = textGO.AddComponent<Text>();
        text.text = label;
        text.fontSize = Mathf.Max(fontSize, 18);
        text.color = ProStyle.TextMain;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.font = ProStyle.Font;
        return btn;
    }

    Button AddButtonLarge(GameObject parent, string name, Vector2 pos, string label, int fontSize = 24)
    {
        var btn = AddButton(parent, name, pos, label, fontSize);
        var rect = btn.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(250, 60);
        var textGO = btn.transform.Find("Text");
        if (textGO != null)
        {
            var text = textGO.GetComponent<Text>();
            if (text != null) text.fontSize = Mathf.Max(fontSize, 14);
        }
        return btn;
    }

    // ── 药物镇痛 UI ───────────────────────────────────────

    void BuildDrugPainUI(DrugPainModule drug, Canvas canvas)
    {
        var explPanel = CreatePanelOnCanvas(canvas, "ExplanationPanel", new Vector2(0, 0), new Vector2(900, 450));
        drug.explanationPanel = explPanel;
        Debug.Log("[第三幕] ExplanationPanel 已创建，activeSelf: " + explPanel.activeSelf + ", position: " + explPanel.transform.position);
        drug.explanationTitle = AddText(explPanel, "Title", new Vector2(0, 160), "title", 28, ProStyle.Warn);
        drug.explanationBody = AddText(explPanel, "Body", new Vector2(0, 60), "body", 20);
        drug.btnNextCard = AddButton(explPanel, "NextCard", new Vector2(-200, -160), "下一张", 18);
        drug.btnCloseCard = AddButton(explPanel, "CloseCard", new Vector2(200, -160), "结束讲解", 18);
        drug.btnStartVideo = AddButtonLarge(explPanel, "StartVideo", new Vector2(0, -190), "确  定", 28);

        // 体位示范面板（新增）
        var demoPanel = CreatePanelOnCanvas(canvas, "PositionDemoPanel", new Vector2(0, 0), new Vector2(900, 450));
        drug.positionDemoPanel = demoPanel;
        drug.positionDemoTitle = AddText(demoPanel, "DemoTitle", new Vector2(0, 160), "title", 28, ProStyle.Warn);
        drug.positionDemoBody = AddText(demoPanel, "DemoBody", new Vector2(0, 50), "body", 20);
        drug.btnNextToQuiz = AddButtonLarge(demoPanel, "NextToQuiz", new Vector2(0, -190), "知道了，继续答题", 22);

        var quizPanel = CreatePanelOnCanvas(canvas, "PositionQuizPanel", new Vector2(0, 0), new Vector2(900, 450));
        drug.positionQuizPanel = quizPanel;
        drug.questionText = AddText(quizPanel, "QuestionText", new Vector2(0, 160), "题目", 22, ProStyle.Warn);
        for (int i = 0; i < 3; i++)
            drug.positionQuizButtons[i] = AddButton(quizPanel, $"Option{i}", new Vector2(0, 60 - i * 80), $"选项{i + 1}", 18);
        drug.feedbackText = AddText(quizPanel, "FeedbackText", new Vector2(0, -160), "", 18);

        var conPanel = CreatePanelOnCanvas(canvas, "ContractionPanel", new Vector2(0, 0), new Vector2(900, 350));
        drug.contractionPanel = conPanel;
        drug.contractionIntensityText = AddText(conPanel, "IntensityText", new Vector2(0, 150), "等待宫缩预警...", 20, ProStyle.Danger);
        var barGO = new GameObject("IntensityBar");
        barGO.transform.SetParent(conPanel.transform, false);
        var barRect = barGO.AddComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 0.5f);
        barRect.anchorMax = new Vector2(0.5f, 0.5f);
        barRect.pivot = new Vector2(0.5f, 0.5f);
        barRect.anchoredPosition = new Vector2(0, 70);
        barRect.sizeDelta = new Vector2(500, 20);
        var barImg = barGO.AddComponent<Image>();
        barImg.color = ProStyle.Danger;
        barImg.type = Image.Type.Filled;
        barImg.fillMethod = Image.FillMethod.Horizontal;
        barImg.fillAmount = 0f;
        drug.contractionBar = barImg;
        drug.callNurseButton = AddButton(conPanel, "CallNurse", new Vector2(0, -30), "呼叫护士", 18);
        string[] cardTexts = { "宫缩来了，有点疼", "我准备好用力了", "我需要镇痛药", "我很紧张，请陪陪我" };
        for (int i = 0; i < 4; i++)
        {
            drug.voiceCards[i] = AddButton(conPanel, $"VoiceCard{i}", new Vector2(-270 + i * 140, -120), cardTexts[i], 14);
        }
    }

    // ── 呼吸训练 UI ───────────────────────────────────────

    void BuildBreathingUI(BreathingModule breathe, Canvas canvas)
    {
        var breathPanel = CreatePanelOnCanvas(canvas, "BreathingPanel", new Vector2(0, 0), new Vector2(800, 600));
        // 呼吸面板背景设为透明，只显示内部的光球、文字、按钮
        var breathPanelImg = breathPanel.GetComponent<Image>();
        if (breathPanelImg != null)
            breathPanelImg.color = new Color(0.1f, 0.1f, 0.1f, 0f);
        breathe.breathingPanel = breathPanel;
        var sphereGO = new GameObject("BreatheSphere");
        sphereGO.transform.SetParent(breathPanel.transform, false);
        var sphereRect = sphereGO.AddComponent<RectTransform>();
        sphereRect.anchorMin = new Vector2(0.5f, 0.5f);
        sphereRect.anchorMax = new Vector2(0.5f, 0.5f);
        sphereRect.pivot = new Vector2(0.5f, 0.5f);
        sphereRect.sizeDelta = new Vector2(200, 200);
        sphereRect.anchoredPosition = Vector2.zero;
        var sphereImg = sphereGO.AddComponent<Image>();
        sphereImg.color = new Color(ProStyle.Accent.r, ProStyle.Accent.g, ProStyle.Accent.b, 0.6f);  // 暖粉色光球
        sphereImg.type = Image.Type.Filled;
        sphereImg.fillMethod = Image.FillMethod.Radial360;
        sphereImg.fillOrigin = 1;  // Top (1) - 从顶部开始填充
        sphereImg.fillAmount = 0.5f;
        breathe.breatheSphere = sphereImg;
        breathe.breatheText = AddText(breathPanel, "BreatheText", new Vector2(0, -160), "跟随光球呼吸", 28);
        breathe.matchPercentText = AddText(breathPanel, "MatchText", new Vector2(0, -210), "吻合度: 等待中...", 20, ProStyle.Accent);

        // 练习模式按钮：点击增加10%匹配度
        var practiceBtnGO = new GameObject("PracticeModeBtn");
        practiceBtnGO.transform.SetParent(breathPanel.transform, false);
        var practiceRect = practiceBtnGO.AddComponent<RectTransform>();
        practiceRect.anchorMin = new Vector2(0.5f, 0f);
        practiceRect.anchorMax = new Vector2(0.5f, 0f);
        practiceRect.pivot = new Vector2(0.5f, 0.5f);
        practiceRect.anchoredPosition = new Vector2(0, 15);
        practiceRect.sizeDelta = new Vector2(180, 40);
        var practiceImg = practiceBtnGO.AddComponent<Image>();
        practiceImg.color = ProStyle.ButtonBg;  // 序幕按钮底
        var practiceBtn = practiceBtnGO.AddComponent<Button>();
        var practiceTextGO = new GameObject("Text");
        practiceTextGO.transform.SetParent(practiceBtnGO.transform, false);
        var practiceTextRect = practiceTextGO.AddComponent<RectTransform>();
        practiceTextRect.anchorMin = Vector2.zero;
        practiceTextRect.anchorMax = Vector2.one;
        var practiceTextComp = practiceTextGO.AddComponent<Text>();
        practiceTextComp.text = "练习模式(+10%)";
        practiceTextComp.fontSize = 18;
        practiceTextComp.color = ProStyle.TextMain;
        practiceTextComp.alignment = TextAnchor.MiddleCenter;
        var practiceFont = Resources.Load<Font>("Fonts/simhei");
        if (practiceFont != null) practiceTextComp.font = practiceFont;
        breathe.practiceModeBtn = practiceBtn;

        // 场景背景色块面板（新增）
        // 场景背景色块面板（新增）- 放在 Canvas 层级，覆盖全屏
        var sceneBgGO = new GameObject("SceneBgPanel");
        sceneBgGO.transform.SetParent(canvas.transform, false);  // 改为 Canvas 子节点
        var sceneBgRect = sceneBgGO.AddComponent<RectTransform>();
        sceneBgRect.anchorMin = Vector2.zero;
        sceneBgRect.anchorMax = Vector2.one;
        sceneBgRect.offsetMin = Vector2.zero;
        sceneBgRect.offsetMax = Vector2.zero;
        var sceneBgImg = sceneBgGO.AddComponent<Image>();
        sceneBgImg.color = new Color(0.15f, 0.15f, 0.2f, 0f);  // 初始透明
        sceneBgImg.raycastTarget = false;  // 不拦截点击
        breathe.sceneBgPanel = sceneBgGO;

        var musicPanel = CreatePanelOnCanvas(canvas, "MusicPanel", new Vector2(0, 0), new Vector2(750, 500));
        breathe.musicPanel = musicPanel;
        breathe.musicTitleText = AddText(musicPanel, "MusicTitle", new Vector2(0, 210), "选择音乐与场景", 28, ProStyle.Warn);
        string[] musicNames = { "海浪轻拍", "森林鸟鸣", "钢琴抒情", "自然白噪音" };
        for (int i = 0; i < 4; i++)
            breathe.musicButtons[i] = AddButton(musicPanel, $"Music{i}", new Vector2(-180, 120 - i * 55), musicNames[i], 16);
        string[] sceneNames = { "海边日落", "森林小径", "山间溪流", "花园秋千" };
        for (int i = 0; i < 4; i++)
            breathe.sceneButtons[i] = AddButton(musicPanel, $"Scene{i}", new Vector2(60, 120 - i * 55), sceneNames[i], 16);

        var massagePanel = CreatePanelOnCanvas(canvas, "MassagePanel", new Vector2(0, 0), new Vector2(700, 350));
        breathe.massagePanel = massagePanel;
        breathe.massageTitleText = AddText(massagePanel, "MassageTitle", new Vector2(0, 170), "选择按摩力度", 28, ProStyle.Warn);
        string[] pressureNames = { "轻柔", "适中", "稍重" };
        for (int i = 0; i < 3; i++)
            breathe.pressureButtons[i] = AddButton(massagePanel, $"Pressure{i}", new Vector2(-150 + i * 150, -80), pressureNames[i], 18);
        breathe.massageTipText = AddText(massagePanel, "MassageTip", new Vector2(0, -30), "请选择按摩力度", 16);
        // 新增：按摩手法详细说明
        breathe.massageDetailText = AddText(massagePanel, "MassageDetail", new Vector2(0, -110), "", 14, ProStyle.TextMain);
        // 按摩确认按钮
        breathe.massageConfirmBtn = AddButton(massagePanel, "MassageConfirm", new Vector2(0, -160), "确认，继续", 16);

        var posPanel = CreatePanelOnCanvas(canvas, "PositionPanel", new Vector2(0, 0), new Vector2(850, 380));
        breathe.positionPanel = posPanel;
        breathe.positionCardTitle = AddText(posPanel, "PosTitle", new Vector2(0, 190), "title", 28, ProStyle.Warn);
        breathe.positionCardDesc = AddText(posPanel, "PosDesc", new Vector2(0, -10), "desc", 16);
        // 5张卡片：3卧位 + 2站立类
        string[] posNames = { "左倾卧位", "半卧位", "屈膝垫枕位", "分娩球", "自由体位" };
        for (int i = 0; i < 5; i++)
            breathe.positionCardButtons[i] = AddButton(posPanel, $"PosCard{i}", new Vector2(-250 + i * 120, -160), posNames[i], 14);
        // 体位卡片确认按钮
        breathe.positionConfirmBtn = AddButton(posPanel, "PosConfirm", new Vector2(0, -180), "确认完成", 18);
    }

    // ── 镇痛决策 UI ───────────────────────────────────────

    void BuildToolboxUI(ToolboxModule toolbox, Canvas canvas)
    {
        var tbPanel = CreatePanelOnCanvas(canvas, "ToolboxPanel", new Vector2(0, 0), new Vector2(850, 480));
        toolbox.toolboxPanel = tbPanel;
        toolbox.toolTitleText = AddText(tbPanel, "ToolTitle", new Vector2(0, 200), "选择法宝放入待产包（0/2）", 20, ProStyle.Warn);
        string[] toolNames = { "呼吸卡", "分娩球", "镇痛泵", "按摩手", "音乐盒" };
        for (int i = 0; i < 5; i++)
            toolbox.toolButtons[i] = AddButton(tbPanel, $"Tool{i}", new Vector2(-260 + i * 130, -150), toolNames[i], 16);

        var bagPanel = CreatePanelOnCanvas(canvas, "BaggagePanel", new Vector2(350, -180), new Vector2(200, 80));
        toolbox.baggagePanel = bagPanel;
        toolbox.baggageCountText = AddText(bagPanel, "BagCount", new Vector2(0, 0), "0/2", 18, ProStyle.Accent);
        for (int i = 0; i < 2; i++)
        {
            var slot = new GameObject($"BagSlot{i}");
            slot.transform.SetParent(bagPanel.transform, false);
            var slotRect = slot.AddComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = new Vector2(-50 + i * 100, 0);
            slotRect.sizeDelta = new Vector2(60, 60);
            var slotImg = slot.AddComponent<Image>();
            slotImg.color = new Color(ProStyle.PanelHover.r, ProStyle.PanelHover.g, ProStyle.PanelHover.b, 0.5f);
            toolbox.baggageSlots[i] = slot;
        }

        var qPanel = CreatePanelOnCanvas(canvas, "QuestionPanel", new Vector2(0, 0), new Vector2(800, 500));
        toolbox.questionPanel = qPanel;
        toolbox.questionText = AddText(qPanel, "QuestionText", new Vector2(0, 200), "问题", 22, ProStyle.Warn);
        toolbox.responseText = AddText(qPanel, "ResponseText", new Vector2(0, -200), "护士回应", 18, ProStyle.Success);
        string[] answers = { "呼吸法我最有把握", "镇痛泵听起来最安心", "音乐和按摩能让我放松", "想都试试多一种选择", "还没想好" };
        for (int i = 0; i < 5; i++)
            toolbox.answerButtons[i] = AddButton(qPanel, $"Answer{i}", new Vector2(0, -100 - i * 50), answers[i], 16);
        toolbox.SetupAnswerButtons(toolbox.answerButtons);

        // 确认选择按钮（选1件时使用）
        toolbox.confirmButton = AddButton(tbPanel, "ConfirmBtn", new Vector2(0, -220), "确认选择，继续", 18);
        toolbox.confirmButton.gameObject.SetActive(false);
    }

    // ── 3D 环境 ───────────────────────────────────────────

    void CreateWall(string name, Vector3 position, Vector2 size, Color color)
    {
        var wallGO = new GameObject(name);
        wallGO.transform.position = position;
        var rect = wallGO.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        var img = wallGO.AddComponent<Image>();
        img.color = color;
        var canvas = wallGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
    }

    void Create3DWalls()
    {
        // 三档明度墙面：远墙最亮（视觉背景）、侧墙次之、近墙最暗——压暗背景竞争、突出房间中央
        var frontWall = CreateWall3D(new Vector3(0, 2.5f, -12f), new Vector2(24f, 5f), new Color(0.52f, 0.47f, 0.43f));
        var backWall = CreateWall3D(new Vector3(0, 2f, 0f), new Vector2(20f, 4f), new Color(0.66f, 0.60f, 0.55f));  // 远墙（视线下方的主背景）
        backWall.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        var leftWall = CreateWall3D(new Vector3(-12f, 2.5f, -6f), new Vector2(14f, 5f), new Color(0.58f, 0.53f, 0.48f));
        leftWall.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        var rightWall = CreateWall3D(new Vector3(12f, 2.5f, -6f), new Vector2(14f, 5f), new Color(0.58f, 0.53f, 0.48f));
        rightWall.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
    }

    GameObject CreateWall3D(Vector3 position, Vector2 size, Color color)
    {
        var wallGO = new GameObject("Wall");
        wallGO.transform.position = position;
        var meshFilter = wallGO.AddComponent<MeshFilter>();
        var meshRenderer = wallGO.AddComponent<MeshRenderer>();
        var mesh = new Mesh();
        mesh.vertices = new Vector3[] {
            new Vector3(-size.x / 2, -size.y / 2, 0),
            new Vector3(size.x / 2, -size.y / 2, 0),
            new Vector3(size.x / 2, size.y / 2, 0),
            new Vector3(-size.x / 2, size.y / 2, 0)
        };
        mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        mesh.uv = new Vector2[] {
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(1, 1), new Vector2(0, 1)
        };
        mesh.RecalculateNormals();
        meshFilter.sharedMesh = mesh;
        var mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.06f);  // 哑光墙面，去掉默认塑料感
        meshRenderer.sharedMaterial = mat;
        return wallGO;
    }

    GameObject Create3DFloor()
    {
        var floorGO = new GameObject("Floor");
        floorGO.transform.position = new Vector3(0, 0, -6);
        floorGO.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        var meshFilter = floorGO.AddComponent<MeshFilter>();
        var meshRenderer = floorGO.AddComponent<MeshRenderer>();
        var mesh = new Mesh();
        float size = 20f;
        mesh.vertices = new Vector3[] {
            new Vector3(-size, 0, -size),
            new Vector3(size, 0, -size),
            new Vector3(size, 0, size),
            new Vector3(-size, 0, size)
        };
        mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        mesh.uv = new Vector2[] {
            new Vector2(0, 0), new Vector2(2, 0),
            new Vector2(2, 2), new Vector2(0, 2)
        };
        mesh.RecalculateNormals();
        meshFilter.sharedMesh = mesh;
        var mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.42f, 0.34f, 0.27f);          // 深暖木色（地板 = 空间最暗面）
        mat.mainTexture = ProStyle.FloorTexture;              // 板缝木纹，消除纯色塑料感
        mat.mainTextureScale = new Vector2(10f, 10f);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.22f);
        meshRenderer.sharedMaterial = mat;
        return floorGO;
    }

    // ── 静态工具方法（供运行时补全调用）────────────────

    public static GameObject CreatePanelOnCanvasStatic(Canvas canvas, string name, Vector2 pos, Vector2? size = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(canvas != null ? canvas.transform : null, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size ?? new Vector2(200, 50);
        var img = go.AddComponent<Image>();
        ProStyle.MakePanel(img, 0.85f);
        return go;
    }

    public static Text AddTextStatic(GameObject parent, string name, Vector2 pos, string content = "", int fontSize = 36, Color? color = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent != null ? parent.transform : null, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(700, 60);
        var text = go.AddComponent<Text>();
        text.text = content;
        text.fontSize = Mathf.Max(fontSize, 18);
        text.color = color ?? ProStyle.TextMain;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.font = ProStyle.Font;
        return text;
    }

    public static Button AddButtonStatic(GameObject parent, string name, Vector2 pos, string label, int fontSize = 18)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent != null ? parent.transform : null, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(120, 36);
        var img = go.AddComponent<Image>();
        var btn = go.AddComponent<Button>();
        ProStyle.MakeButton(img, btn);
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        var text = textGO.AddComponent<Text>();
        text.text = label;
        text.fontSize = Mathf.Max(fontSize, 18);
        text.color = ProStyle.TextMain;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.font = ProStyle.Font;
        return btn;
    }

    public static Button AddButtonLargeStatic(GameObject parent, string name, Vector2 pos, string label, int fontSize = 24)
    {
        var btn = AddButtonStatic(parent, name, pos, label, fontSize);
        var rect = btn.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(250, 60);
        var textGO = btn.transform.Find("Text");
        if (textGO != null)
        {
            var text = textGO.GetComponent<Text>();
            if (text != null) text.fontSize = Mathf.Max(fontSize, 14);
        }
        return btn;
    }
}
}
