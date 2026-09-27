using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class NextSceneWarmLightFadeIn : MonoBehaviour
{
    // ==========================================
    // 【第1段：暖光渐隐】
    // ==========================================
    [Header("=== 暖光滤镜 ===")]
    public Image warmLightFilter;

    [Header("=== 暖光渐隐设置 ===")]
    [Range(0f, 1f)]
    public float startAlpha = 1f;
    [Range(0f, 1f)]
    public float endAlpha = 0f;
    public float fadeOutTime = 2f;
    public bool hideAfterFade = true;

    // ==========================================
    // 【第2段：继续按钮】
    // ==========================================
    [Header("=== 继续按钮 ===")]
    public Button continueButton;

    // ==========================================
    // 【第3段：鼠标拖动调整视角 + 下一个按钮】
    // ==========================================
    [Header("=== 鼠标拖动视角设置 ===")]
    public Transform cameraTransform;
    public float mouseSensitivity = 0.2f;
    public float yawRange = 30f;
    public float pitchRange = 15f;
    public bool allowPitch = true;
    public bool allowYaw = true;
    public TextMeshProUGUI hintText;
    public Button nextButton;

    // ==========================================
    // 【第4段：音乐播放】
    // ==========================================
    [Header("=== 音乐设置 ===")]
    public AudioSource musicSource;

    // ==========================================
    // 【第5段：3个控制按钮】
    // ==========================================
    [Header("=== 暂停音乐按钮 ===")]
    public Button pauseMusicButton;
    public TextMeshProUGUI pauseMusicButtonText;
    public string pauseMusicText = "暂停音乐";
    public string resumeMusicText = "继续音乐";

    [Header("=== 暂停游戏按钮 ===")]
    public Button pauseGameButton;
    public TextMeshProUGUI pauseGameButtonText;
    public string pauseGameText = "暂停游戏";
    public string resumeGameText = "继续游戏";

    [Header("=== 退出按钮 ===")]
    public Button exitButton;

    // ==========================================
    // 【第6段：结尾文本】
    // ==========================================
    [Header("=== 结尾文本 ===")]
    public TextMeshProUGUI endingText;
    public float endingTextDelay = 1f;
    public float endingTextDisplayTime = 3f;

    // ==========================================
    // 【第7段（第4步）：手术进度】
    // ==========================================
    [Header("=== 第4步：静态图片 ===")]
    public Image step4StaticImage;

    [Header("=== 第4步：进度条 ===")]
    public Image step4ProgressBar;
    public float progressStartX = -300f;
    public float progressEndX = 300f;
    public float progressY = 0f;

    [Header("=== 阶段1：消毒完成 ===")]
    public float phase1Duration = 15f;

    [Header("=== 阶段2：麻醉生效 ===")]
    public float phase2Duration = 15f;

    [Header("=== 阶段3：手术进行中 ===")]
    public float phase3Duration = 15f;

    [Header("=== 阶段4：宝宝即将出生 ===")]
    public float phase4Duration = 15f;

    [Header("=== 4个阶段文字（逐个出现）===")]
    public TextMeshProUGUI phaseText1;
    public TextMeshProUGUI phaseText2;
    public TextMeshProUGUI phaseText3;
    public TextMeshProUGUI phaseText4;
    public Color activeTextColor = Color.white;

    [Header("=== 光球呼吸放缩 ===")]
    public Transform lightBall;
    public float minScale = 1.0f;
    public float maxScale = 1.3f;
    public float breathCycle = 3f;

    [Header("=== 进度条结束设置 ===")]
    public bool hideProgressAfterFinish = true;
    public float progressHideDelay = 1f;

    // ==========================================
    // 【第4步：长按鼠标触发文本 + 语音】
    // ==========================================
    [Header("=== 长按鼠标触发设置 ===")]
    public float longPressDuration = 2f;
    public TextMeshProUGUI longPressText;
    public AudioSource longPressVoiceSource;
    public float fallbackTextDuration = 5f;
    public float extraTextHoldTime = 0f;
    public bool useLeftMouseButton = true;

    // ==========================================
    // 【第5步：两段文本 + 按住鼠标右键功能】
    // ==========================================
    [Header("=== 第5步：文本1 ===")]
    public TextMeshProUGUI step5Text1;
    public float step5Text1Duration = 3f;
    public float step5Text1ToText2Delay = 0.5f;

    [Header("=== 第5步：文本2 ===")]
    public TextMeshProUGUI step5Text2;
    public float step5Text2Duration = 3f;
    public float step5Text2ToHintDelay = 0.5f;

    [Header("=== 第5步：提示文本 ===")]
    public TextMeshProUGUI step5HintText;

    [Header("=== 第5步：按住鼠标右键功能 ===")]
    public float step5HoldDuration = 3f;
    public bool step5UseLeftMouseButton = false;
    public bool step5UseVisualFeedback = false;
    public Image step5HoldProgressImage;

    // ==========================================
    // 【第6步：4个先后出现的文本，可单独控制】
    // ==========================================
    [Header("=== 第6步：4个先后出现的文本 ===")]
    [Tooltip("文本1（可勾选是否运行）")]
    public TextMeshProUGUI step6Text1;
    public bool step6Text1Enabled = true;
    public float step6Text1Duration = 3f;

    [Tooltip("文本2（可勾选是否运行）")]
    public TextMeshProUGUI step6Text2;
    public bool step6Text2Enabled = true;
    public float step6Text2Duration = 3f;

    [Tooltip("文本3（可勾选是否运行）")]
    public TextMeshProUGUI step6Text3;
    public bool step6Text3Enabled = true;
    public float step6Text3Duration = 3f;

    [Tooltip("文本4（可勾选是否运行）")]
    public TextMeshProUGUI step6Text4;
    public bool step6Text4Enabled = true;
    public float step6Text4Duration = 3f;

    public float step6TextInterval = 0.5f;

    // ==========================================
    // 【第7步：主菜单（文本按钮 / 答题按钮）】
    // ==========================================
    [Header("=== 第7步：主菜单两个按钮 ===")]
    public Button step7TextButton;
    public Button step7QuizButton;

    // ==========================================
    // 【第7步：文本按钮 → 8个按钮】
    // ==========================================
    [Header("=== 第7步：8个按钮 ===")]
    public Button step7TextBtn1;
    public Button step7TextBtn2;
    public Button step7TextBtn3;
    public Button step7TextBtn4;
    public Button step7TextBtn5;
    public Button step7TextBtn6;
    public Button step7TextBtn7;
    public Button step7NextBtn;

    // ==========================================
    // 【第7步：7个文本（按钮1~7对应）】
    // ==========================================
    [Header("=== 第7步：7个文本 ===")]
    public TextMeshProUGUI step7Content1;
    public TextMeshProUGUI step7Content2;
    public TextMeshProUGUI step7Content3;
    public TextMeshProUGUI step7Content4;
    public TextMeshProUGUI step7Content5;
    public TextMeshProUGUI step7Content6;
    public TextMeshProUGUI step7Content7;

    [Header("=== 第7步：7个文本的显示时长 ===")]
    public float step7Content1Duration = 3f;
    public float step7Content2Duration = 3f;
    public float step7Content3Duration = 3f;
    public float step7Content4Duration = 3f;
    public float step7Content5Duration = 3f;
    public float step7Content6Duration = 3f;
    public float step7Content7Duration = 3f;

    // ==========================================
    // 【第7步：答题 → 3道选择题】
    // ==========================================
    [Header("=== 第7步：3道题 Prompt ===")]
    public TextMeshProUGUI step7QuestionPrompt1;
    public TextMeshProUGUI step7QuestionPrompt2;
    public TextMeshProUGUI step7QuestionPrompt3;

    [Header("=== 第7步：3道题 按钮（每题4个）===")]
    public Button s7q1Button1; public Button s7q1Button2; public Button s7q1Button3; public Button s7q1Button4;
    public Button s7q2Button1; public Button s7q2Button2; public Button s7q2Button3; public Button s7q2Button4;
    public Button s7q3Button1; public Button s7q3Button2; public Button s7q3Button3; public Button s7q3Button4;

    [Header("=== 第7步：3道题 反馈文本 ===")]
    public TextMeshProUGUI s7q1Text1; public TextMeshProUGUI s7q1Text2; public TextMeshProUGUI s7q1Text3; public TextMeshProUGUI s7q1Text4;
    public TextMeshProUGUI s7q2Text1; public TextMeshProUGUI s7q2Text2; public TextMeshProUGUI s7q2Text3; public TextMeshProUGUI s7q2Text4;
    public TextMeshProUGUI s7q3Text1; public TextMeshProUGUI s7q3Text2; public TextMeshProUGUI s7q3Text3; public TextMeshProUGUI s7q3Text4;

    [Header("=== 第7步：答题反馈时长 ===")]
    public float step7AnswerFeedbackDuration = 2f;
    public float step7QuestionInterval = 0.5f;

    // ==========================================
    // 【退出场景设置】
    // ==========================================
    [Header("=== 退出场景设置 ===")]
    public bool useSceneNameForExit = true;
    public string exitSceneName = "UI界面";
    public int exitSceneIndex = -1;

    [Header("=== 调试 ===")]
    public bool showDebugLog = true;

    // 运行时状态
    private bool hasClickedContinue = false;
    private bool hasClickedNext = false;
    private bool isAdjustingCamera = false;
    private bool isMusicPaused = false;
    private bool isGamePaused = false;

    private float baseYaw = 0f;
    private float basePitch = 0f;
    private float currentYaw = 0f;
    private float currentPitch = 0f;

    private float progressTimer = 0f;
    private bool isProgressRunning = false;
    private int currentPhaseIndex = -1;
    private float[] phaseEndTimes = new float[4];
    private float progressTotalDuration = 0f;
    private Vector3 lightBallBaseScale = Vector3.one;
    private float breathTimer = 0f;
    private RectTransform progressBarRect;

    private float mouseHoldTimer = 0f;
    private bool hasTriggeredThisHold = false;
    private Coroutine longPressRoutine;

    private bool step5Running = false;
    private float step5HoldTimer = 0f;

    // 第7步缓存
    private Button[] step7TextMenuButtons;
    private TextMeshProUGUI[] step7QuestionPrompts;
    private Button[][] step7QuestionButtons;
    private TextMeshProUGUI[][] step7QuestionTexts;

    private int step7CurrentQuestionIndex = -1;
    private bool step7WaitingForAnswer = false;
    private int step7ChosenAnswerIndex = -1;

    void Start()
    {
        if (musicSource != null)
        {
            musicSource.playOnAwake = false;
            musicSource.Stop();
        }

        if (longPressVoiceSource != null)
        {
            longPressVoiceSource.playOnAwake = false;
            longPressVoiceSource.Stop();
        }

        Time.timeScale = 1f;
        isGamePaused = false;

        CalculatePhaseTimings();
        BuildStep7Cache();

        if (lightBall != null)
            lightBallBaseScale = lightBall.localScale;

        if (step4ProgressBar != null)
            progressBarRect = step4ProgressBar.rectTransform;

        SetupUI();
        SetupButtons();

        StartCoroutine(MainSequence());
    }

    // ==========================================
    // 初始化UI
    // ==========================================
    void SetupUI()
    {
        if (continueButton != null) continueButton.gameObject.SetActive(false);
        if (hintText != null) hintText.gameObject.SetActive(false);
        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (pauseMusicButton != null) pauseMusicButton.gameObject.SetActive(false);
        if (pauseGameButton != null) pauseGameButton.gameObject.SetActive(false);
        if (exitButton != null) exitButton.gameObject.SetActive(false);
        if (endingText != null) endingText.gameObject.SetActive(false);

        UpdatePauseMusicButtonText();
        UpdatePauseGameButtonText();

        if (step4StaticImage != null) step4StaticImage.gameObject.SetActive(false);
        if (step4ProgressBar != null) step4ProgressBar.gameObject.SetActive(false);
        HideAllPhaseTexts();
        if (lightBall != null) lightBall.gameObject.SetActive(false);
        if (longPressText != null) longPressText.gameObject.SetActive(false);

        // 第5步
        if (step5Text1 != null) step5Text1.gameObject.SetActive(false);
        if (step5Text2 != null) step5Text2.gameObject.SetActive(false);
        if (step5HintText != null) step5HintText.gameObject.SetActive(false);
        if (step5HoldProgressImage != null)
        {
            step5HoldProgressImage.gameObject.SetActive(false);
            step5HoldProgressImage.fillAmount = 0f;
        }

        // 第6步
        HideAllStep6Texts();

        // 第7步
        if (step7TextButton != null) step7TextButton.gameObject.SetActive(false);
        if (step7QuizButton != null) step7QuizButton.gameObject.SetActive(false);

        HideAllStep7TextMenuButtons();
        HideAllStep7Contents();
        HideAllStep7Questions();
        HideAllStep7QuestionTexts();
    }

    // ==========================================
    // 按钮绑定
    // ==========================================
    void SetupButtons()
    {
        if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
        if (nextButton != null) nextButton.onClick.AddListener(OnNextClicked);
        if (pauseMusicButton != null) pauseMusicButton.onClick.AddListener(OnPauseMusicClicked);
        if (pauseGameButton != null) pauseGameButton.onClick.AddListener(OnPauseGameClicked);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);

        // 第7步主菜单
        if (step7TextButton != null)
            step7TextButton.onClick.AddListener(OnStep7TextButtonClicked);

        if (step7QuizButton != null)
            step7QuizButton.onClick.AddListener(OnStep7QuizButtonClicked);

        // 第7步8个按钮
        if (step7TextBtn1 != null) step7TextBtn1.onClick.AddListener(() => OnStep7ContentClicked(0));
        if (step7TextBtn2 != null) step7TextBtn2.onClick.AddListener(() => OnStep7ContentClicked(1));
        if (step7TextBtn3 != null) step7TextBtn3.onClick.AddListener(() => OnStep7ContentClicked(2));
        if (step7TextBtn4 != null) step7TextBtn4.onClick.AddListener(() => OnStep7ContentClicked(3));
        if (step7TextBtn5 != null) step7TextBtn5.onClick.AddListener(() => OnStep7ContentClicked(4));
        if (step7TextBtn6 != null) step7TextBtn6.onClick.AddListener(() => OnStep7ContentClicked(5));
        if (step7TextBtn7 != null) step7TextBtn7.onClick.AddListener(() => OnStep7ContentClicked(6));
        if (step7NextBtn != null) step7NextBtn.onClick.AddListener(OnStep7NextButtonClicked);

        // 第7步答题按钮
        for (int q = 0; q < 3; q++)
        {
            if (step7QuestionButtons[q] == null) continue;

            for (int b = 0; b < step7QuestionButtons[q].Length; b++)
            {
                int capturedQ = q;
                int capturedB = b;

                if (step7QuestionButtons[q][b] != null)
                    step7QuestionButtons[q][b].onClick.AddListener(() => OnStep7AnswerClicked(capturedQ, capturedB));
            }
        }
    }

    // ==========================================
    // 缓存第7步数据
    // ==========================================
    void BuildStep7Cache()
    {
        step7TextMenuButtons = new Button[]
        {
            step7TextBtn1, step7TextBtn2, step7TextBtn3, step7TextBtn4,
            step7TextBtn5, step7TextBtn6, step7TextBtn7, step7NextBtn
        };

        step7QuestionPrompts = new TextMeshProUGUI[3]
        {
            step7QuestionPrompt1, step7QuestionPrompt2, step7QuestionPrompt3
        };

        step7QuestionButtons = new Button[3][];
        step7QuestionButtons[0] = new Button[] { s7q1Button1, s7q1Button2, s7q1Button3, s7q1Button4 };
        step7QuestionButtons[1] = new Button[] { s7q2Button1, s7q2Button2, s7q2Button3, s7q2Button4 };
        step7QuestionButtons[2] = new Button[] { s7q3Button1, s7q3Button2, s7q3Button3, s7q3Button4 };

        step7QuestionTexts = new TextMeshProUGUI[3][];
        step7QuestionTexts[0] = new TextMeshProUGUI[] { s7q1Text1, s7q1Text2, s7q1Text3, s7q1Text4 };
        step7QuestionTexts[1] = new TextMeshProUGUI[] { s7q2Text1, s7q2Text2, s7q2Text3, s7q2Text4 };
        step7QuestionTexts[2] = new TextMeshProUGUI[] { s7q3Text1, s7q3Text2, s7q3Text3, s7q3Text4 };
    }

    // ==========================================
    // 主流程
    // ==========================================
    IEnumerator MainSequence()
    {
        yield return StartCoroutine(WarmLightFadeOutSequence());
        yield return StartCoroutine(WaitForContinueButton());
        yield return StartCoroutine(CameraAdjustmentSequence());

        StartLoopMusic();
        ShowControlButtons();

        yield return StartCoroutine(EndingTextSequence());
        yield return StartCoroutine(SurgeryProgressSequence());
        yield return StartCoroutine(Step5Sequence());
        yield return StartCoroutine(Step6Sequence());

        // 第7步
        yield return StartCoroutine(Step7Sequence());
    }

    // ==========================================
    // Update
    // ==========================================
    void Update()
    {
        if (isProgressRunning)
            HandleStep4LongPress();

        if (step5Running)
            HandleStep5Hold();
    }

    // ==========================================
    // 第4步：长按检测
    // ==========================================
    void HandleStep4LongPress()
    {
        // VR扳机或鼠标长按均可
        bool isMouseHeld = (useLeftMouseButton ? Input.GetMouseButton(0) : Input.GetMouseButton(1))
            || ChuJian.Merge.VRPointerVisual.IsAnyTriggerHeld;

        if (isMouseHeld)
        {
            if (!hasTriggeredThisHold)
            {
                mouseHoldTimer += Time.deltaTime;

                if (mouseHoldTimer >= longPressDuration)
                {
                    TriggerLongPress();
                    hasTriggeredThisHold = true;
                    mouseHoldTimer = 0f;
                }
            }
        }
        else
        {
            mouseHoldTimer = 0f;
            hasTriggeredThisHold = false;
        }
    }

    void TriggerLongPress()
    {
        if (longPressRoutine != null)
        {
            StopCoroutine(longPressRoutine);
            longPressRoutine = null;
        }

        longPressRoutine = StartCoroutine(LongPressRoutine());
    }

    IEnumerator LongPressRoutine()
    {
        if (longPressText != null)
            longPressText.gameObject.SetActive(true);

        bool hasVoice = (longPressVoiceSource != null && longPressVoiceSource.clip != null);

        if (hasVoice)
        {
            longPressVoiceSource.Stop();
            longPressVoiceSource.Play();

            yield return null;

            while (longPressVoiceSource != null && longPressVoiceSource.isPlaying)
                yield return null;

            if (extraTextHoldTime > 0f)
                yield return new WaitForSeconds(extraTextHoldTime);
        }
        else
        {
            yield return new WaitForSeconds(fallbackTextDuration);
        }

        if (longPressText != null)
            longPressText.gameObject.SetActive(false);

        longPressRoutine = null;
    }

    // ==========================================
    // 【第1段】暖光渐隐
    // ==========================================
    IEnumerator WarmLightFadeOutSequence()
    {
        if (warmLightFilter != null)
        {
            warmLightFilter.gameObject.SetActive(true);
            Color filterColor = warmLightFilter.color;
            filterColor.a = startAlpha;
            warmLightFilter.color = filterColor;
        }

        float t = 0f;
        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            float progress = t / fadeOutTime;

            if (warmLightFilter != null)
            {
                Color filterColor = warmLightFilter.color;
                filterColor.a = Mathf.Lerp(startAlpha, endAlpha, progress);
                warmLightFilter.color = filterColor;
            }

            yield return null;
        }

        if (warmLightFilter != null)
        {
            Color filterColor = warmLightFilter.color;
            filterColor.a = endAlpha;
            warmLightFilter.color = filterColor;

            if (hideAfterFade && endAlpha <= 0.01f)
                warmLightFilter.gameObject.SetActive(false);
        }
    }

    // ==========================================
    // 【第2段】继续按钮
    // ==========================================
    IEnumerator WaitForContinueButton()
    {
        hasClickedContinue = false;

        if (continueButton != null)
            continueButton.gameObject.SetActive(true);

        while (!hasClickedContinue)
            yield return null;

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);
    }

    void OnContinueClicked()
    {
        hasClickedContinue = true;
    }

    // ==========================================
    // 【第3段】视角调整
    // ==========================================
    IEnumerator CameraAdjustmentSequence()
    {
        if (hintText != null) hintText.gameObject.SetActive(true);
        if (nextButton != null) nextButton.gameObject.SetActive(true);

        if (cameraTransform != null)
        {
            Vector3 euler = cameraTransform.localEulerAngles;
            baseYaw = euler.y;
            basePitch = euler.x;

            if (baseYaw > 180f) baseYaw -= 360f;
            if (basePitch > 180f) basePitch -= 360f;
        }

        currentYaw = 0f;
        currentPitch = 0f;

        hasClickedNext = false;
        isAdjustingCamera = true;

        while (!hasClickedNext)
        {
            UpdateCameraByMouse();
            yield return null;
        }

        isAdjustingCamera = false;

        if (hintText != null) hintText.gameObject.SetActive(false);
        if (nextButton != null) nextButton.gameObject.SetActive(false);

        yield return new WaitForSeconds(0.5f);
    }

    void UpdateCameraByMouse()
    {
        if (cameraTransform == null) return;

        bool isDragging = Input.GetMouseButton(0) || Input.GetMouseButton(1);

        if (isDragging)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            if (allowYaw)
            {
                currentYaw += mouseX * mouseSensitivity;
                currentYaw = Mathf.Clamp(currentYaw, -yawRange, yawRange);
            }

            if (allowPitch)
            {
                currentPitch -= mouseY * mouseSensitivity;
                currentPitch = Mathf.Clamp(currentPitch, -pitchRange, pitchRange);
            }

            Vector3 euler = cameraTransform.localEulerAngles;
            euler.y = baseYaw + currentYaw;
            euler.x = basePitch + currentPitch;
            cameraTransform.localEulerAngles = euler;
        }
    }

    void OnNextClicked()
    {
        hasClickedNext = true;
    }

    // ==========================================
    // 【第4段】音乐
    // ==========================================
    void StartLoopMusic()
    {
        if (musicSource != null && musicSource.clip != null)
        {
            musicSource.loop = true;
            musicSource.Play();
            isMusicPaused = false;
            UpdatePauseMusicButtonText();
        }
    }

    // ==========================================
    // 【第5段】3个控制按钮
    // ==========================================
    void ShowControlButtons()
    {
        if (pauseMusicButton != null) pauseMusicButton.gameObject.SetActive(true);
        if (pauseGameButton != null) pauseGameButton.gameObject.SetActive(true);
        if (exitButton != null) exitButton.gameObject.SetActive(true);
    }

    void OnPauseMusicClicked()
    {
        if (musicSource == null) return;

        if (isMusicPaused)
        {
            musicSource.UnPause();
            isMusicPaused = false;
        }
        else
        {
            musicSource.Pause();
            isMusicPaused = true;
        }

        UpdatePauseMusicButtonText();
    }

    void UpdatePauseMusicButtonText()
    {
        if (pauseMusicButtonText == null) return;
        pauseMusicButtonText.text = isMusicPaused ? resumeMusicText : pauseMusicText;
    }

    void OnPauseGameClicked()
    {
        if (isGamePaused)
        {
            Time.timeScale = 1f;
            isGamePaused = false;
            ResumeAllAudio();
        }
        else
        {
            Time.timeScale = 0f;
            isGamePaused = true;
            PauseAllAudio();
        }

        UpdatePauseGameButtonText();
        UpdatePauseMusicButtonText();
    }

    void UpdatePauseGameButtonText()
    {
        if (pauseGameButtonText == null) return;
        pauseGameButtonText.text = isGamePaused ? resumeGameText : pauseGameText;
    }

    void PauseAllAudio()
    {
        AudioSource[] allAudio = FindObjectsOfType<AudioSource>();
        foreach (AudioSource audio in allAudio)
        {
            if (audio.isPlaying) audio.Pause();
        }
        isMusicPaused = true;
    }

    void ResumeAllAudio()
    {
        AudioSource[] allAudio = FindObjectsOfType<AudioSource>();
        foreach (AudioSource audio in allAudio)
        {
            audio.UnPause();
        }
        isMusicPaused = false;
    }

    void OnExitClicked()
    {
        Time.timeScale = 1f;

        if (musicSource != null) musicSource.Stop();
        if (longPressVoiceSource != null) longPressVoiceSource.Stop();

        if (useSceneNameForExit && !string.IsNullOrEmpty(exitSceneName))
        {
            string target = exitSceneName;
            // 兼容旧配置里带路径的写法（如 Scenes/UI界面），LoadScene 按名字匹配时不能带路径
            if (target.StartsWith("Scenes/")) target = System.IO.Path.GetFileNameWithoutExtension(target);
            SceneManager.LoadScene(target);
            return;
        }

        // 合并工程：索引在全局场景表里会错位，回退也按名字（幕内回退到 UI界面）
        SceneManager.LoadScene("UI界面");
    }

    // ==========================================
    // 【第6段】结尾文本
    // ==========================================
    IEnumerator EndingTextSequence()
    {
        if (endingText == null) yield break;

        yield return new WaitForSeconds(endingTextDelay);

        endingText.gameObject.SetActive(true);

        yield return new WaitForSeconds(endingTextDisplayTime);

        endingText.gameObject.SetActive(false);
    }

    // ==========================================
    // 【第7段（第4步）】手术进度
    // ==========================================
    IEnumerator SurgeryProgressSequence()
    {
        Time.timeScale = 1f;
        isGamePaused = false;

        CalculatePhaseTimings();

        if (progressTotalDuration <= 0f) yield break;

        if (step4StaticImage != null) step4StaticImage.gameObject.SetActive(true);

        if (step4ProgressBar != null && progressBarRect != null)
        {
            step4ProgressBar.gameObject.SetActive(true);
            Vector2 startPos = progressBarRect.anchoredPosition;
            startPos.x = progressStartX;
            startPos.y = progressY;
            progressBarRect.anchoredPosition = startPos;
        }

        HideAllPhaseTexts();

        if (lightBall != null)
        {
            lightBall.gameObject.SetActive(true);
            lightBall.localScale = lightBallBaseScale * minScale;
        }

        progressTimer = 0f;
        breathTimer = 0f;
        mouseHoldTimer = 0f;
        hasTriggeredThisHold = false;
        currentPhaseIndex = -1;
        isProgressRunning = true;

        UpdatePhase(0);

        while (progressTimer < progressTotalDuration)
        {
            progressTimer += Time.deltaTime;

            float rawProgress = Mathf.Clamp01(progressTimer / progressTotalDuration);

            if (step4ProgressBar != null && progressBarRect != null)
            {
                float currentX = Mathf.Lerp(progressStartX, progressEndX, rawProgress);

                Vector2 pos = progressBarRect.anchoredPosition;
                pos.x = currentX;
                pos.y = progressY;
                progressBarRect.anchoredPosition = pos;
            }

            int newPhase = GetPhaseIndexByTime(progressTimer);
            if (newPhase != currentPhaseIndex)
                UpdatePhase(newPhase);

            UpdateLightBallBreath();

            yield return null;
        }

        if (step4ProgressBar != null && progressBarRect != null)
        {
            Vector2 endPos = progressBarRect.anchoredPosition;
            endPos.x = progressEndX;
            endPos.y = progressY;
            progressBarRect.anchoredPosition = endPos;
        }

        UpdatePhase(3);

        yield return new WaitForSeconds(progressHideDelay);

        isProgressRunning = false;

        if (hideProgressAfterFinish)
        {
            if (step4StaticImage != null) step4StaticImage.gameObject.SetActive(false);
            if (step4ProgressBar != null) step4ProgressBar.gameObject.SetActive(false);
            HideAllPhaseTexts();
            if (lightBall != null) lightBall.gameObject.SetActive(false);
        }
    }

    void CalculatePhaseTimings()
    {
        phaseEndTimes[0] = phase1Duration;
        phaseEndTimes[1] = phaseEndTimes[0] + phase2Duration;
        phaseEndTimes[2] = phaseEndTimes[1] + phase3Duration;
        phaseEndTimes[3] = phaseEndTimes[2] + phase4Duration;

        progressTotalDuration = phaseEndTimes[3];
    }

    int GetPhaseIndexByTime(float time)
    {
        if (time < phaseEndTimes[0]) return 0;
        if (time < phaseEndTimes[1]) return 1;
        if (time < phaseEndTimes[2]) return 2;
        return 3;
    }

    void UpdatePhase(int phaseIndex)
    {
        currentPhaseIndex = phaseIndex;

        HideAllPhaseTexts();

        TextMeshProUGUI currentText = GetPhaseText(phaseIndex);
        if (currentText != null)
        {
            currentText.gameObject.SetActive(true);
            currentText.color = activeTextColor;
        }
    }

    void HideAllPhaseTexts()
    {
        if (phaseText1 != null) phaseText1.gameObject.SetActive(false);
        if (phaseText2 != null) phaseText2.gameObject.SetActive(false);
        if (phaseText3 != null) phaseText3.gameObject.SetActive(false);
        if (phaseText4 != null) phaseText4.gameObject.SetActive(false);
    }

    TextMeshProUGUI GetPhaseText(int phaseIndex)
    {
        switch (phaseIndex)
        {
            case 0: return phaseText1;
            case 1: return phaseText2;
            case 2: return phaseText3;
            case 3: return phaseText4;
            default: return null;
        }
    }

    void UpdateLightBallBreath()
    {
        if (lightBall == null) return;

        breathTimer += Time.deltaTime;

        float t = (Mathf.Sin(breathTimer / breathCycle * Mathf.PI * 2f) + 1f) * 0.5f;
        float scale = Mathf.Lerp(minScale, maxScale, t);

        lightBall.localScale = lightBallBaseScale * scale;
    }

    // ==========================================
    // 【第8段（第5步）】两段文本 + 按住鼠标右键
    // ==========================================
    IEnumerator Step5Sequence()
    {
        if (step5Text1 != null)
        {
            step5Text1.gameObject.SetActive(true);
            yield return new WaitForSeconds(step5Text1Duration);
            step5Text1.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(step5Text1ToText2Delay);

        if (step5Text2 != null)
        {
            step5Text2.gameObject.SetActive(true);
            yield return new WaitForSeconds(step5Text2Duration);
            step5Text2.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(step5Text2ToHintDelay);

        if (step5HintText != null)
            step5HintText.gameObject.SetActive(true);

        step5HoldTimer = 0f;
        step5Running = true;

        if (step5UseVisualFeedback && step5HoldProgressImage != null)
        {
            step5HoldProgressImage.gameObject.SetActive(true);
            step5HoldProgressImage.fillAmount = 0f;
        }

        while (step5Running)
            yield return null;

        if (step5HintText != null)
            step5HintText.gameObject.SetActive(false);

        if (step5HoldProgressImage != null)
            step5HoldProgressImage.gameObject.SetActive(false);
    }

    void HandleStep5Hold()
    {
        // VR扳机或鼠标长按均可（合并工程兼容）
        bool isMouseHeld = (step5UseLeftMouseButton ? Input.GetMouseButton(0) : Input.GetMouseButton(1))
            || Input.GetMouseButton(0) || Input.GetMouseButton(1)
            || ChuJian.Merge.VRPointerVisual.IsAnyTriggerHeld;

        if (isMouseHeld)
        {
            step5HoldTimer += Time.deltaTime;

            if (step5UseVisualFeedback && step5HoldProgressImage != null)
            {
                step5HoldProgressImage.fillAmount = Mathf.Clamp01(step5HoldTimer / step5HoldDuration);
            }

            if (step5HoldTimer >= step5HoldDuration)
            {
                step5HoldTimer = 0f;
                step5Running = false;
            }
        }
        else
        {
            step5HoldTimer = 0f;

            if (step5UseVisualFeedback && step5HoldProgressImage != null)
            {
                step5HoldProgressImage.fillAmount = 0f;
            }
        }
    }

    // ==========================================
    // 【第9段（第6步）】4个先后出现的文本
    // ==========================================
    IEnumerator Step6Sequence()
    {
        if (step6Text1Enabled && step6Text1 != null)
        {
            step6Text1.gameObject.SetActive(true);
            yield return new WaitForSeconds(step6Text1Duration);
            step6Text1.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(step6TextInterval);

        if (step6Text2Enabled && step6Text2 != null)
        {
            step6Text2.gameObject.SetActive(true);
            yield return new WaitForSeconds(step6Text2Duration);
            step6Text2.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(step6TextInterval);

        if (step6Text3Enabled && step6Text3 != null)
        {
            step6Text3.gameObject.SetActive(true);
            yield return new WaitForSeconds(step6Text3Duration);
            step6Text3.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(step6TextInterval);

        if (step6Text4Enabled && step6Text4 != null)
        {
            step6Text4.gameObject.SetActive(true);
            yield return new WaitForSeconds(step6Text4Duration);
            step6Text4.gameObject.SetActive(false);
        }
    }

    void HideAllStep6Texts()
    {
        if (step6Text1 != null) step6Text1.gameObject.SetActive(false);
        if (step6Text2 != null) step6Text2.gameObject.SetActive(false);
        if (step6Text3 != null) step6Text3.gameObject.SetActive(false);
        if (step6Text4 != null) step6Text4.gameObject.SetActive(false);
    }

    // ==========================================
    // 【第10段（第7步）】主菜单 + 8个按钮 + 3道答题
    // ==========================================
    IEnumerator Step7Sequence()
    {
        if (showDebugLog) Debug.Log("第7步开始。");

        // 循环：主菜单 → 文本/答题 → 回主菜单
        while (true)
        {
            yield return StartCoroutine(ShowStep7MainMenu());

            // 等待玩家点击（OnStep7TextButtonClicked / OnStep7QuizButtonClicked 会启动对应协程）

            // 等两个按钮都隐藏（表示玩家点了）
            yield return new WaitUntil(() => 
                (step7TextButton == null || !step7TextButton.gameObject.activeSelf) &&
                (step7QuizButton == null || !step7QuizButton.gameObject.activeSelf)
            );

            // 等待回到主菜单的状态（由子流程控制）
            yield return new WaitUntil(() => 
                (step7TextButton == null || step7TextButton.gameObject.activeSelf) ||
                (step7QuizButton == null || step7QuizButton.gameObject.activeSelf)
            );
        }
    }

    IEnumerator ShowStep7MainMenu()
    {
        // 隐藏所有子界面
        HideAllStep7TextMenuButtons();
        HideAllStep7Contents();
        HideAllStep7Questions();
        HideAllStep7QuestionTexts();

        // 显示主菜单两个按钮
        if (step7TextButton != null) step7TextButton.gameObject.SetActive(true);
        if (step7QuizButton != null) step7QuizButton.gameObject.SetActive(true);

        if (showDebugLog) Debug.Log("第7步：显示主菜单（两个按钮）。");

        yield return null;
    }

    // ---------- 主菜单：文本按钮 ----------
    void OnStep7TextButtonClicked()
    {
        if (showDebugLog) Debug.Log("点击【文本按钮】。");

        if (step7TextButton != null) step7TextButton.gameObject.SetActive(false);
        if (step7QuizButton != null) step7QuizButton.gameObject.SetActive(false);

        ShowStep7TextMenuButtons();
    }

    void ShowStep7TextMenuButtons()
    {
        for (int i = 0; i < step7TextMenuButtons.Length; i++)
        {
            if (step7TextMenuButtons[i] != null)
                step7TextMenuButtons[i].gameObject.SetActive(true);
        }

        if (showDebugLog) Debug.Log("显示 8 个按钮。");
    }

    void HideAllStep7TextMenuButtons()
    {
        for (int i = 0; i < step7TextMenuButtons.Length; i++)
        {
            if (step7TextMenuButtons[i] != null)
                step7TextMenuButtons[i].gameObject.SetActive(false);
        }
    }

    // ---------- 8个按钮中的 1~7 ----------
    void OnStep7ContentClicked(int index)
    {
        if (showDebugLog) Debug.Log($"点击文本按钮 {index + 1}。");

        StartCoroutine(ShowStep7ContentRoutine(index));
    }

    IEnumerator ShowStep7ContentRoutine(int index)
    {
        // 隐藏 8 个按钮
        HideAllStep7TextMenuButtons();

        // 隐藏所有文本
        HideAllStep7Contents();

        // 获取文本和时长
        TextMeshProUGUI content = GetStep7ContentByIndex(index);
        float duration = GetStep7ContentDurationByIndex(index);

        // 显示文本
        if (content != null)
        {
            content.gameObject.SetActive(true);
            yield return new WaitForSeconds(duration);
            content.gameObject.SetActive(false);
        }

        // 回到 8 个按钮
        ShowStep7TextMenuButtons();
    }

    TextMeshProUGUI GetStep7ContentByIndex(int index)
    {
        switch (index)
        {
            case 0: return step7Content1;
            case 1: return step7Content2;
            case 2: return step7Content3;
            case 3: return step7Content4;
            case 4: return step7Content5;
            case 5: return step7Content6;
            case 6: return step7Content7;
            default: return null;
        }
    }

    float GetStep7ContentDurationByIndex(int index)
    {
        switch (index)
        {
            case 0: return step7Content1Duration;
            case 1: return step7Content2Duration;
            case 2: return step7Content3Duration;
            case 3: return step7Content4Duration;
            case 4: return step7Content5Duration;
            case 5: return step7Content6Duration;
            case 6: return step7Content7Duration;
            default: return 3f;
        }
    }

    // ---------- 8个按钮中的按钮8（下一步）----------
    void OnStep7NextButtonClicked()
    {
        if (showDebugLog) Debug.Log("点击按钮8（下一步）→ 回主菜单。");

        // 回主菜单
        StartCoroutine(ShowStep7MainMenu());
    }

    // ---------- 主菜单：答题按钮 ----------
    void OnStep7QuizButtonClicked()
    {
        if (showDebugLog) Debug.Log("点击【答题按钮】。");

        if (step7TextButton != null) step7TextButton.gameObject.SetActive(false);
        if (step7QuizButton != null) step7QuizButton.gameObject.SetActive(false);

        StartCoroutine(Step7QuizSequence());
    }

    IEnumerator Step7QuizSequence()
    {
        for (int q = 0; q < 3; q++)
        {
            if (!HasStep7ValidQuestion(q))
            {
                continue;
            }

            yield return StartCoroutine(AskStep7Question(q));
        }

        if (showDebugLog) Debug.Log("答题结束，回主菜单。");

        // 回主菜单
        yield return StartCoroutine(ShowStep7MainMenu());
    }

    bool HasStep7ValidQuestion(int qIndex)
    {
        if (step7QuestionPrompts != null && step7QuestionPrompts[qIndex] != null) return true;

        if (step7QuestionButtons != null && step7QuestionButtons[qIndex] != null)
        {
            for (int b = 0; b < step7QuestionButtons[qIndex].Length; b++)
            {
                if (step7QuestionButtons[qIndex][b] != null) return true;
            }
        }

        return false;
    }

    IEnumerator AskStep7Question(int qIndex)
    {
        step7CurrentQuestionIndex = qIndex;
        step7WaitingForAnswer = true;
        step7ChosenAnswerIndex = -1;

        if (step7QuestionPrompts[qIndex] != null)
            step7QuestionPrompts[qIndex].gameObject.SetActive(true);

        ShowStep7QuestionButtons(qIndex);

        while (step7WaitingForAnswer)
            yield return null;

        HideStep7QuestionButtons(qIndex);

        if (step7QuestionPrompts[qIndex] != null)
            step7QuestionPrompts[qIndex].gameObject.SetActive(false);

        // 显示反馈
        TextMeshProUGUI feedback = null;
        if (step7QuestionTexts[qIndex] != null &&
            step7ChosenAnswerIndex >= 0 &&
            step7ChosenAnswerIndex < step7QuestionTexts[qIndex].Length)
        {
            feedback = step7QuestionTexts[qIndex][step7ChosenAnswerIndex];
        }

        if (feedback != null)
        {
            feedback.gameObject.SetActive(true);
            yield return new WaitForSeconds(step7AnswerFeedbackDuration);
            feedback.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(step7QuestionInterval);
    }

    void OnStep7AnswerClicked(int qIndex, int bIndex)
    {
        if (!step7WaitingForAnswer) return;
        if (qIndex != step7CurrentQuestionIndex) return;

        step7ChosenAnswerIndex = bIndex;
        step7WaitingForAnswer = false;
    }

    void ShowStep7QuestionButtons(int qIndex)
    {
        if (step7QuestionButtons[qIndex] == null) return;

        for (int b = 0; b < step7QuestionButtons[qIndex].Length; b++)
        {
            if (step7QuestionButtons[qIndex][b] != null)
                step7QuestionButtons[qIndex][b].gameObject.SetActive(true);
        }
    }

    void HideStep7QuestionButtons(int qIndex)
    {
        if (step7QuestionButtons[qIndex] == null) return;

        for (int b = 0; b < step7QuestionButtons[qIndex].Length; b++)
        {
            if (step7QuestionButtons[qIndex][b] != null)
                step7QuestionButtons[qIndex][b].gameObject.SetActive(false);
        }
    }

    void HideAllStep7Questions()
    {
        for (int q = 0; q < 3; q++)
        {
            if (step7QuestionPrompts != null && step7QuestionPrompts[q] != null)
                step7QuestionPrompts[q].gameObject.SetActive(false);

            HideStep7QuestionButtons(q);
        }
    }

    void HideAllStep7QuestionTexts()
    {
        if (step7QuestionTexts == null) return;

        for (int q = 0; q < 3; q++)
        {
            if (step7QuestionTexts[q] == null) continue;

            for (int b = 0; b < step7QuestionTexts[q].Length; b++)
            {
                if (step7QuestionTexts[q][b] != null)
                    step7QuestionTexts[q][b].gameObject.SetActive(false);
            }
        }
    }

    void HideAllStep7Contents()
    {
        if (step7Content1 != null) step7Content1.gameObject.SetActive(false);
        if (step7Content2 != null) step7Content2.gameObject.SetActive(false);
        if (step7Content3 != null) step7Content3.gameObject.SetActive(false);
        if (step7Content4 != null) step7Content4.gameObject.SetActive(false);
        if (step7Content5 != null) step7Content5.gameObject.SetActive(false);
        if (step7Content6 != null) step7Content6.gameObject.SetActive(false);
        if (step7Content7 != null) step7Content7.gameObject.SetActive(false);
    }
}