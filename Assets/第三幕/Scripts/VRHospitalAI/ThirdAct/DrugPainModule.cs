using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;

namespace VRHospitalAI.ThirdAct
{
    /// <summary>
    /// 上篇·药物镇痛模块控制器。
    /// 覆盖策划文档 3.4 上篇全部内容：
    ///   1. 麻醉医生讲解（4张知识卡片）
    ///   2. 体位示范（观看虚拟孕妇"虾米状"配合体位）
    ///   3. 体位摆放选择题（3题）
    ///   4. 宫缩期沟通演练
    /// 全部使用 VR 手柄输入，PC 调试用键盘回退。
    /// </summary>
    public class DrugPainModule : MonoBehaviour
    {
        [Header("引用")]
        public NPCBehaviour anesthesiologistNPC;
        public CameraFade cameraFade;
        public ScoreManager scoreManager;
        public ThirdActHUD hud;

        [Header("讲解 UI")]
        public GameObject explanationPanel;
        public Text explanationTitle;
        public Text explanationBody;
        public Button btnNextCard;
        public Button btnCloseCard;
        public Button btnStartVideo;

        [Header("全局确认按钮（屏幕底部常驻）")]
        public Button confirmButton;

        [Header("体位示范")]
        public GameObject positionDemoPanel;
        public Text positionDemoTitle;
        public Text positionDemoBody;
        public Button btnNextToQuiz;
        public UnityEngine.Video.VideoPlayer positionVideo;  // 体位示范视频
        public UnityEngine.UI.RawImage positionVideoRawImage;  // 视频显示组件

        [Header("体位选择题")]
        public GameObject positionQuizPanel;
        public Text questionText;
        public Button[] positionQuizButtons = new Button[3];
        public Text feedbackText;

        [Header("宫缩沟通演练")]
        public GameObject contractionPanel;
        public Button callNurseButton;
        public Button[] voiceCards = new Button[4];
        public Text contractionIntensityText;
        public Image contractionBar;

        // ── 状态标志 ────────────────────────────────────────
        private int currentQuestionIndex;
        private int correctAnswerCount;
        private bool isReadyConfirmShowing;   // 是否处于"确 定"确认状态
        private bool isShowingCards;          // 是否正在显示讲解卡片
        private bool isShowingDemo;           // 是否正在显示体位示范
        private bool isModuleComplete;        // 模块是否已完成
        private Coroutine contractionCoroutine;

        public event Action OnModuleComplete;
        public event Action OnDrugTalkComplete;  // 上篇完成（进入宫缩演练）

        public bool IsShowingCards => isShowingCards;

        private bool buttonsSetup;  // 防止重复连线

        void Start()
        {
            if (cameraFade == null) cameraFade = FindObjectOfType<CameraFade>();
            if (scoreManager == null) scoreManager = FindObjectOfType<ScoreManager>();
            if (hud == null) hud = FindObjectOfType<ThirdActHUD>();
            HideAll();
            // 自动加载体位示范视频
            if (positionVideo == null)
            {
                var videoGO = new GameObject("PositionVideoPlayer");
                videoGO.transform.SetParent(transform, false);
                positionVideo = videoGO.AddComponent<UnityEngine.Video.VideoPlayer>();
                // 使用 RenderTexture 模式，视频会渲染到 RawImage
                positionVideo.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture;
                positionVideo.source = UnityEngine.Video.VideoSource.Url;
                positionVideo.url = "file:///" + UnityEngine.Application.dataPath + "/Resources/Videos/position_demo.mp4";
                positionVideo.isLooping = false;
                positionVideo.waitForFirstFrame = true;
                positionVideo.errorReceived += OnVideoError;

                // 创建 RenderTexture
                var renderTexture = new RenderTexture(640, 368, 24);
                positionVideo.targetTexture = renderTexture;

                // 创建显示视频的 Canvas 和 RawImage
                CreateVideoDisplayCanvas(renderTexture);

                Debug.Log("[DrugPain] 体位示范视频已创建，等待 StartPositionDemo 时播放");
            }
        }

        void CreateVideoDisplayCanvas(RenderTexture renderTexture)
        {
            // 创建 Canvas（顶级）
            var canvasGO = new UnityEngine.GameObject("PositionVideoCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1500;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // 创建视频显示面板（直接在 Canvas 下）
            var videoPanelGO = new UnityEngine.GameObject("PositionVideoPanel");
            videoPanelGO.transform.SetParent(canvasGO.transform, false);

            var rectTransform = videoPanelGO.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0, 80);  // 稍微靠上
            rectTransform.sizeDelta = new Vector2(480, 276);

            // 添加 RawImage 用于显示视频
            var rawImage = videoPanelGO.AddComponent<UnityEngine.UI.RawImage>();
            rawImage.texture = renderTexture;
            positionVideoRawImage = rawImage;

            videoPanelGO.SetActive(false); // 初始隐藏
        }

        void OnVideoError(UnityEngine.Video.VideoPlayer vp, string message)
        {
            Debug.LogError("[DrugPain] 视频播放错误: " + message);
            // 视频加载失败时，跳过视频直接进入演示
            StartPositionDemo();
        }

        // 延迟连接按钮：Start 时 UI 面板可能尚未创建，下一帧检测就绪后再连线
        void Update()
        {
            if (!buttonsSetup)
            {
                if (explanationPanel != null)
                {
                    SetupButtons();
                    buttonsSetup = true;
                }
                return;
            }
        }

        // ── 全局确认按钮逻辑 ─────────────────────────────────

        public void ActivateConfirmButton(string labelText = "确 定")
        {
            if (confirmButton != null)
            {
                confirmButton.interactable = true;
                var btnText = confirmButton.GetComponentInChildren<Text>();
                if (btnText != null) btnText.text = labelText;
            }
        }

        void OnConfirmButtonClicked()
        {
            Debug.Log("[DrugPain] ★ 全局确认按钮被点击！isShowingCards=" + isShowingCards +
                     " isShowingDemo=" + isShowingDemo +
                     " isReadyConfirmShowing=" + isReadyConfirmShowing);

            // 根据当前阶段执行不同操作
            if (isShowingCards && isReadyConfirmShowing)
            {
                // 初始状态：开始讲解
                isReadyConfirmShowing = false;
                if (btnNextCard != null) btnNextCard.gameObject.SetActive(true);
                ShowExplanationCard(0);
            }
            else if (isShowingCards)
            {
                // 讲解进行中：翻到下一张
                NextExplanationCard();
            }
            else if (isShowingDemo)
            {
                // 体位示范进行中：翻到下一张
                ShowPositionDemoCard(currentDemoIndex + 1);
            }
            else
            {
                // 检查是否在选择题阶段
                if (positionQuizPanel != null && positionQuizPanel.activeSelf)
                {
                    Debug.Log("[DrugPain] 确认按钮在选择题阶段点击，跳过（答题按钮处理点击）");
                }
                // 检查是否在宫缩演练阶段
                else if (contractionPanel != null && contractionPanel.activeSelf)
                {
                    Debug.Log("[DrugPain] 确认按钮在宫缩演练阶段点击，跳过");
                }
                else
                {
                    Debug.Log("[DrugPain] 确认按钮点击，但当前无活跃阶段，跳过");
                }
            }

            // 点击后立即重新激活按钮，保证可连续点击
            ActivateConfirmButton("确 定");
        }

        void SetupButtons()
        {
            if (btnStartVideo != null)
            {
                btnStartVideo.onClick.RemoveAllListeners();
                btnStartVideo.onClick.AddListener(OnStartVideoClicked);
            }

            if (btnNextCard != null)
            {
                btnNextCard.onClick.RemoveAllListeners();
                btnNextCard.onClick.AddListener(NextExplanationCard);
            }
            if (btnCloseCard != null)
            {
                btnCloseCard.onClick.RemoveAllListeners();
                btnCloseCard.onClick.AddListener(CloseExplanation);
            }
            if (btnNextToQuiz != null)
            {
                btnNextToQuiz.onClick.RemoveAllListeners();
                btnNextToQuiz.onClick.AddListener(OnDemoNextClick);
            }

            // 全局确认按钮
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(OnConfirmButtonClicked);
            }

            for (int i = 0; i < positionQuizButtons.Length; i++)
            {
                int idx = i;
                positionQuizButtons[i]?.onClick.AddListener(() => OnPositionQuizClick(idx));
            }
            callNurseButton?.onClick.AddListener(OnCallNurse);
            for (int i = 0; i < voiceCards.Length; i++)
            {
                int idx = i;
                voiceCards[i]?.onClick.AddListener(() => OnVoiceCardClick(idx));
            }
        }

        // ── 流程入口 ────────────────────────────────────────

        public void StartDrugTalk()
        {
            Debug.Log("[DrugPain] StartDrugTalk 被调用");
            HideAll();
            isShowingCards = true;
            isShowingDemo = false;

            if (explanationPanel != null)
                explanationPanel.SetActive(true);
            else
            {
                Debug.LogError("[DrugPain] explanationPanel 是 null！");
                return;
            }

            if (btnStartVideo != null)
            {
                btnStartVideo.gameObject.SetActive(true);
                btnStartVideo.interactable = true;
            }
            if (btnNextCard != null) btnNextCard.gameObject.SetActive(false);
            if (btnCloseCard != null) btnCloseCard.gameObject.SetActive(false);

            currentCardIndex = 0;
            isReadyConfirmShowing = true;
            if (hud != null)
                hud.ShowSubtitle("王医生：接下来由我讲解椎管内镇痛");

            // 显示底部全局确认按钮
            ActivateConfirmButton("确 定");
        }

        // ── 讲解卡片 ────────────────────────────────────────

        private readonly (string title, string body, string subtitle)[] explanationCards = new[]
        {
            (
                "什么是椎管内分娩镇痛？",
                "●  腰椎间置入一根细管\n●  低浓度药物注入椎管\n●  阻断疼痛信号上传大脑\n●  头脑清醒，下半身减痛",
                "王医生：简单说，就是让下半身「减痛」"
            ),
            (
                "镇痛效果与时效",
                "●  10-20 分钟开始起效\n●  效果可维持数小时\n●  宫缩感还在，疼痛明显减轻\n●  分娩结束后拔除导管",
                "王医生：十几分钟起效，痛感明显变轻"
            ),
            (
                "安全性说明",
                "●  药量 ≈ 剖宫产麻醉的 1/10\n●  不入血液、不入羊水\n●  不影响产程进展\n●  全程监测，风险可控",
                "王医生：安全性已经过大量临床验证"
            ),
            (
                "常见误区澄清",
                "●  「伤宝宝」？ → 不会\n●  「不能顺产」？ → 反而助力\n●  「人人能打」？ → 需先评估",
                "王医生：三个常见说法，一次澄清"
            ),
        };

        private int currentCardIndex = 0;

        void ShowExplanationCard(int index)
        {
            if (index >= explanationCards.Length) { CloseExplanation(); return; }
            currentCardIndex = index;
            var card = explanationCards[index];
            if (explanationTitle != null) explanationTitle.text = card.title;
            if (explanationBody != null) explanationBody.text = card.body;
            if (hud != null) hud.ShowSubtitle(card.subtitle);
        }

        public void NextExplanationCard()
        {
            if (!isShowingCards) return;
            ShowExplanationCard(currentCardIndex + 1);
            isReadyConfirmShowing = false;
        }

        void OnStartVideoClicked()
        {
            Debug.Log("[DrugPain] 面板内「确 定」按钮被点击（备用路径）");
            isReadyConfirmShowing = false;
            if (btnStartVideo != null) btnStartVideo.gameObject.SetActive(false);
            if (btnNextCard != null) btnNextCard.gameObject.SetActive(true);
            ShowExplanationCard(0);
        }

        void CloseExplanation()
        {
            if (btnStartVideo != null) btnStartVideo.gameObject.SetActive(false);
            explanationPanel.SetActive(false);
            isShowingCards = false;
            if (hud != null) hud.ClearSubtitle();

            if (isReadyConfirmShowing)
            {
                isReadyConfirmShowing = false;
                return;
            }
            // 讲解结束 → 进入体位示范
            StartPositionDemo();
        }

        // ── 体位示范 ────────────────────────────────────────

        private readonly (string title, string body, string subtitle)[] positionDemoCards = new[]
        {
            (
                "配合穿刺的正确体位",
                "●  一个稳定的好姿势\n●  让穿刺更快更准\n●  先看演示，再答几道小题",
                "护士小安：看演示，学配合穿刺的姿势"
            ),
            (
                "姿势一：侧卧抱膝",
                "●  侧躺，背部靠床沿\n●  双膝抱向腹部\n●  身体微微前倾",
                "护士小安：第一步——侧卧抱膝"
            ),
            (
                "姿势二：低头弓背",
                "●  低头，下巴贴胸口\n●  背部拱起像虾米\n●  打开椎间隙，方便穿刺",
                "护士小安：第二步——低头弓背"
            ),
            (
                "姿势三：保持稳定",
                "●  穿刺中保持不动\n●  晃动会影响准确性\n●  您只需观看学习即可",
                "护士小安：第三步——保持稳定"
            ),
        };

        private int currentDemoIndex = 0;

        void StartPositionDemo()
        {
            HideAll();
            isShowingDemo = true;
            currentDemoIndex = 0;
            if (hud != null)
                hud.ShowSubtitle("护士小安：请您观看虚拟孕妇的体位示范，了解配合穿刺的正确姿势");
            ActivateConfirmButton("确 定");
            // 先播放视频，视频结束后再显示演示面板
            PlayPositionVideo();
        }

        void PlayPositionVideo()
        {
            if (positionVideo != null)
            {
                // 显示视频面板
                if (positionVideoRawImage != null && positionVideoRawImage.gameObject != null)
                {
                    positionVideoRawImage.gameObject.SetActive(true);
                }
                // 如果视频已结束，重新播放
                if (!positionVideo.isPlaying)
                {
                    positionVideo.Play();
                }
                positionVideo.loopPointReached += OnVideoEnded;
                Debug.Log("[DrugPain] 开始播放体位示范视频");
            }
            else
            {
                Debug.LogWarning("[DrugPain] 未找到体位示范视频组件");
            }
        }

        void OnVideoEnded(UnityEngine.Video.VideoPlayer vp)
        {
            Debug.Log("[DrugPain] 体位示范视频播放完毕");
            positionVideo.loopPointReached -= OnVideoEnded;
            // 隐藏视频面板
            if (positionVideoRawImage != null && positionVideoRawImage.gameObject != null)
            {
                positionVideoRawImage.gameObject.SetActive(false);
            }
            // 视频结束后显示演示面板
            if (isShowingDemo)
            {
                // 视频结束后直接进入第一张卡片，不重复播放视频
                ShowPositionDemoCard(0);
            }
        }

        void ShowPositionDemoCard(int index)
        {
            if (index >= positionDemoCards.Length)
            {
                positionDemoPanel.SetActive(false);
                isShowingDemo = false;
                // 停止视频
                if (positionVideo != null)
                {
                    positionVideo.loopPointReached -= OnVideoEnded;
                    positionVideo.Stop();
                }
                // 隐藏视频面板
                if (positionVideoRawImage != null && positionVideoRawImage.gameObject != null)
                {
                    positionVideoRawImage.gameObject.SetActive(false);
                }
                StartPositionQuiz();
                return;
            }
            // 显示演示面板
            positionDemoPanel.SetActive(true);
            currentDemoIndex = index;
            var card = positionDemoCards[index];
            if (positionDemoTitle != null) positionDemoTitle.text = card.title;
            if (positionDemoBody != null) positionDemoBody.text = card.body;
            if (hud != null) hud.ShowSubtitle(card.subtitle);
        }

        void OnDemoNextClick()
        {
            if (VRInput.TriggerDown) VRInput.VibrateRight(0.3f, 0.05f);
            if (isShowingDemo && positionDemoPanel.activeSelf)
                ShowPositionDemoCard(currentDemoIndex + 1);
            // 不在 demo 阶段时不做任何操作（讲解阶段由 NextExplanationCard 处理）
        }

        // ── 体位选择题 ──────────────────────────────────────

        public void StartPositionQuiz()
        {
            HideAll();
            positionQuizPanel.SetActive(true);
            currentQuestionIndex = 0;
            correctAnswerCount = 0;
            ShowPositionQuestion(0);
            if (hud != null)
                hud.ShowSubtitle("王医生：几道小题，检验体位要点");
        }

        private readonly (string question, string[] options, int correctIndex, string feedback, string subtitle)[]
            positionQuestions = new[]
        {
            (
                "侧卧时身体应该朝向哪个方向？",
                new[] { "朝向房门", "朝向护士站", "随意即可" },
                1,
                "正确！方便护士观察、医生操作",
                "王医生：侧卧时请面向护士站方向"
            ),
            (
                "抱膝时头部应该？",
                new[] { "尽量贴近膝盖", "抬起看天花板", "随意摆放" },
                0,
                "正确！低头拱背，方便穿刺",
                "王医生：低头能让脊柱弯曲，方便麻醉医生操作"
            ),
            (
                "背部应该呈现什么形状？",
                new[] { "平直", "弓起像虾米", "尽量后仰" },
                1,
                "正确！弓背打开椎间隙，关键一步",
                "王医生：背部弓起来就像一只虾米，这样穿刺更容易"
            ),
        };

        void ShowPositionQuestion(int index)
        {
            if (index >= positionQuestions.Length)
            {
                positionQuizPanel.SetActive(false);
                scoreManager?.AddScore(5);
                if (correctAnswerCount >= positionQuestions.Length)
                {
                    if (feedbackText != null)
                    {
                        feedbackText.text = "✓ 全部答对，姿势掌握了！";
                        feedbackText.color = ProStyle.Success;
                    }
                }
                if (hud != null)
                {
                    hud.ShowSubtitle("体位学习完成！接下来进行宫缩沟通演练");
                    hud.ClearSubtitle();
                }
                Debug.Log("[DrugPain] 上篇完成，触发 OnDrugTalkComplete");
                OnDrugTalkComplete?.Invoke();
                return;
            }
            currentQuestionIndex = index;
            var q = positionQuestions[index];
            if (questionText != null) questionText.text = q.question;
            if (feedbackText != null) feedbackText.text = "";

            // 更新按钮文本为选项内容
            for (int i = 0; i < positionQuizButtons.Length; i++)
            {
                if (i < q.options.Length && positionQuizButtons[i] != null)
                {
                    positionQuizButtons[i].interactable = true;
                    var btnText = positionQuizButtons[i].GetComponentInChildren<Text>();
                    if (btnText != null) btnText.text = q.options[i];
                }
                else if (positionQuizButtons[i] != null)
                {
                    positionQuizButtons[i].interactable = false;
                }
            }
            if (hud != null) hud.ShowSubtitle(q.subtitle);
            Debug.Log("[体位 quiz] 问题 " + index + ": " + q.question);
        }

        void OnPositionQuizClick(int buttonIndex)
        {
            if (VRInput.TriggerDown) VRInput.VibrateRight(0.3f, 0.05f);
            var q = positionQuestions[currentQuestionIndex];
            for (int i = 0; i < positionQuizButtons.Length; i++)
                positionQuizButtons[i].interactable = false;

            if (buttonIndex == q.correctIndex)
            {
                correctAnswerCount++;
                if (feedbackText != null)
                {
                    feedbackText.text = "✓ " + q.feedback;
                    feedbackText.color = ProStyle.Success;
                }
                scoreManager?.AddScore(2);
                Debug.Log("[体位 quiz] 答对! " + q.feedback + " (累计 " + correctAnswerCount + "/" + positionQuestions.Length + ")");
            }
            else
            {
                // 答错：显示错误提示 + 正确答案说明
                if (feedbackText != null)
                {
                    feedbackText.text = "✗ 再想想，正确答案是：" + q.options[q.correctIndex];
                    feedbackText.color = ProStyle.Warn;
                }
                Debug.Log("[体位 quiz] 答错，正确答案是选项 " + (q.correctIndex + 1));
            }

            Invoke(nameof(ShowNextQuestion), 1.5f);
        }

        void ShowNextQuestion()
        {
            ShowPositionQuestion(currentQuestionIndex + 1);
        }

        // ── 宫缩沟通演练 ────────────────────────────────────

        public void StartContractionDialog()
        {
            HideAll();
            contractionPanel.SetActive(true);
            isModuleComplete = false;
            if (hud != null)
                hud.ShowSubtitle("王医生：模拟宫缩来了，练习表达需求");

            contractionIntensityText.text = "等待宫缩预警...";
            contractionBar.fillAmount = 0;
            Invoke(nameof(StartContractionWarning), 3f);
        }

        private readonly string[] voiceCardTexts = new[]
        {
            "宫缩来了，有点疼",
            "我准备好用力了",
            "我需要镇痛药",
            "我很紧张，请陪陪我"
        };

        private readonly string[] nurseResponses = new[]
        {
            "宫缩正常，跟着呼吸来，我陪着你。",
            "现在还不用力，跟着呼吸节奏就好。",
            "镇痛泵正在起效，我来检查一下。",
            "紧张很正常，深呼吸，我一直都在。"
        };

        void StartContractionWarning()
        {
            contractionIntensityText.text = "⚠ 宫缩即将来临！请按呼叫键";
            VRInput.VibrateBoth(0.6f, 0.3f);
            contractionCoroutine = StartCoroutine(ContractionWave());
        }

        IEnumerator ContractionWave()
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 0.05f;
                contractionBar.fillAmount = Mathf.Sin(t * Mathf.PI);
                if (contractionBar.fillAmount > 0.7f && UnityEngine.Random.value < 0.02f)
                    VRInput.VibrateBoth(0.3f, 0.05f);
                yield return null;
            }
            while (contractionBar.fillAmount > 0)
            {
                contractionBar.fillAmount -= Time.deltaTime * 0.5f;
                yield return null;
            }

            // 宫缩波浪自然结束后，如果用户还没操作，直接结束演练
            if (!isModuleComplete)
            {
                contractionIntensityText.text = "宫缩已经过去，您做得很好！";
                VRInput.VibrateRight(0.2f, 0.1f);
                scoreManager?.AddScore(3);
                if (hud != null)
                    hud.ShowSubtitle("宫缩已经过去，您成功完成了演练！");
                Invoke(nameof(ReturnToMenu), 2f);
            }
        }

        void OnCallNurse()
        {
            if (VRInput.TriggerDown) VRInput.VibrateRight(0.3f, 0.05f);
            if (hud != null)
                hud.ShowSubtitle("护士：请选择一张语音卡");
        }

        void OnVoiceCardClick(int index)
        {
            if (VRInput.TriggerDown) VRInput.VibrateRight(0.3f, 0.05f);
            Debug.Log("[宫缩演练] 选择: " + voiceCardTexts[index]);

            // 停止宫缩波浪协程
            if (contractionCoroutine != null)
            {
                StopCoroutine(contractionCoroutine);
                contractionCoroutine = null;
            }
            CancelInvoke(nameof(ReturnToMenu));

            if (hud != null)
                hud.ShowSubtitle(nurseResponses[index]);

            // 医生肯定字幕
            Invoke(nameof(ShowDoctorFeedback), 2.5f);
        }

        void ShowDoctorFeedback()
        {
            if (isModuleComplete) return;
            string doctorFeedback = "王医生：宫缩期保持静止，配合得非常好！\n— 宫缩演练完成 —";
            if (hud != null)
                hud.ShowSubtitle(doctorFeedback);
            scoreManager?.AddScore(2);
            Invoke(nameof(ReturnToMenu), 3f);
        }

        void ReturnToMenu()
        {
            if (isModuleComplete) return;
            isModuleComplete = true;
            if (hud != null) hud.ClearSubtitle();
            Debug.Log("[宫缩演练] 触发 OnModuleComplete");
            OnModuleComplete?.Invoke();
        }

        public void HideAll()
        {
            if (explanationPanel) explanationPanel.SetActive(false);
            if (positionDemoPanel) positionDemoPanel.SetActive(false);
            if (positionQuizPanel) positionQuizPanel.SetActive(false);
            if (contractionPanel) contractionPanel.SetActive(false);
            // 注意：不在这里隐藏确认按钮，按钮常驻工具栏
        }
    }
}
