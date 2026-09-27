using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

namespace VRHospitalAI.ThirdAct
{
    /// <summary>
    /// 下篇·非药物镇痛模块控制器。
    /// 覆盖策划文档 3.4 下篇全部内容：
    ///   1. 拉玛泽呼吸三阶段跟练
    ///   2. 音乐想象疗法
    ///   3. 床旁按摩演示
    ///   4. 卧床位姿卡片
    /// 全部使用 VR 手柄输入，PC 调试用键盘回退。
    /// </summary>
    public class BreathingModule : MonoBehaviour
    {
        [Header("呼吸跟练")]
        public GameObject breathingPanel;
        public Image breatheSphere;
        public Text breatheText;
        public Text matchPercentText;
        public Slider breathPaceSlider;
        public ScoreManager scoreManager;
        public ThirdActHUD hud;
        public Button practiceModeBtn;  // 练习模式按钮：点击增加10%匹配度

        [Header("音乐与想象")]
        public GameObject musicPanel;
        public Text musicTitleText;
        public Button[] musicButtons = new Button[4];
        public Button[] sceneButtons = new Button[4];
        public UnityEngine.Video.VideoPlayer relaxVideo;
        public float musicDuration = 60f;
        public GameObject sceneBgPanel;  // 场景背景色块面板（新增，用于视觉切换）

        [Header("音频")]
        public AudioClip relaxAudioClip;  // 放松音乐片段

        [Header("按摩演示")]
        public GameObject massagePanel;
        public Text massageTitleText;
        public Button[] pressureButtons = new Button[3];
        public Text massageTipText;
        public Text massageDetailText;
        public Button massageConfirmBtn;  // 按摩确认按钮

        [Header("体位卡片")]
        public GameObject positionPanel;
        public Text positionCardTitle;
        public Text positionCardDesc;
        public Button[] positionCardButtons = new Button[5];
        public Button positionConfirmBtn;  // 体位卡片完成按钮

        [Header("转场")]
        public CameraFade cameraFade;

        private int breathingPhase;
        private string selectedMusic;
        private string selectedScene;
        private bool musicTimerActive;
        private AudioSource musicAudioSource;  // 音乐播放器

        public event Action OnModuleComplete;

        private bool buttonsSetup;  // 防止重复连线

        void Start()
        {
            // 自动查找依赖项（保底，防止场景未正确连线时流程卡死）
            if (cameraFade == null) cameraFade = FindObjectOfType<CameraFade>();
            if (scoreManager == null) scoreManager = FindObjectOfType<ScoreManager>();
            if (hud == null) hud = FindObjectOfType<ThirdActHUD>();
            HideAll();
            // 创建音频源
            var audioGO = new GameObject("MusicAudioSource");
            audioGO.transform.SetParent(transform, false);
            musicAudioSource = audioGO.AddComponent<AudioSource>();
            musicAudioSource.playOnAwake = false;
            musicAudioSource.loop = true;
            musicAudioSource.volume = 0.5f;
            // 自动加载测试音频（如果未配置）
            if (relaxAudioClip == null)
            {
                relaxAudioClip = Resources.Load<AudioClip>("Music/relax");
                if (relaxAudioClip != null)
                    Debug.Log("[音乐疗法] 自动加载测试音频: relax.wav (" + relaxAudioClip.length + "秒)");
                else
                    Debug.LogWarning("[音乐疗法] 未找到测试音频，请在 Assets/Resources/Music/ 放置音频文件");
            }
        }

        // 延迟连接按钮：Start 时 UI 面板可能尚未创建
        void Update()
        {
            if (!buttonsSetup && breathingPanel != null)
            {
                SetupMusicButtons();
                SetupMassageButtons();
                SetupPositionCards();
                SetupPracticeModeButton();
                buttonsSetup = true;
            }

            // 放松等待阶段：点击结束按钮或任意处可提前结束（3秒保护期内不触发，避免误点）
            if (relaxPending && Time.time - relaxStart > 3f &&
                (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || skipRelaxPressed))
            {
                Debug.Log("[音乐疗法] 用户主动跳过放松阶段");
                CancelInvoke(nameof(MusicRelaxDone));
                MusicRelaxDone();
            }
            skipRelaxPressed = false;
        }

        private bool skipRelaxPressed;

        /// <summary>外部触发跳过放松阶段（供结束按钮调用）</summary>
        public void SkipRelax()
        {
            skipRelaxPressed = true;
        }

        // ── 流程入口 ───────────────────────────────────────────

        public void StartBreathing()
        {
            Debug.Log("[Breathing] StartBreathing 被调用, breathingPanel=" + (breathingPanel != null) + " cameraFade=" + (cameraFade != null) + " scoreManager=" + (scoreManager != null) + " hud=" + (hud != null));
            if (breathingPanel == null)
            {
                Debug.LogError("[Breathing] breathingPanel 是 null！模块 UI 未构建。");
                return;
            }
            if (breatheSphere != null)
            {
                Debug.Log("[Breathing] breatheSphere 字段已赋值，type=" + breatheSphere.type + " fillMethod=" + breatheSphere.fillMethod + " fillOrigin=" + breatheSphere.fillOrigin + " fillAmount=" + breatheSphere.fillAmount);
            }
            else
            {
                Debug.LogError("[Breathing] breatheSphere 是 null！光球未正确连线。");
            }
            HideAll();
            breathingPanel.SetActive(true);
            breathingPhase = 0;
            if (hud != null)
                hud.ShowSubtitle("小安：跟随光球，练习拉玛泽呼吸");
            else
                Debug.LogError("[Breathing] HUD 是 null！");
            StartBreathingPhase(0);
        }

        public void StartMusicRelax()
        {
            Debug.Log("[Breathing] ★ StartMusicRelax 被调用, musicPanel=" + (musicPanel != null));
            if (musicPanel == null)
            {
                Debug.LogError("[Breathing] ★ musicPanel 是 null！无法显示音乐面板");
                return;
            }
            Debug.Log("[Breathing] musicPanel.activeSelf before: " + musicPanel.activeSelf);
            HideAll();
            breathingPanel.SetActive(false);  // 确保呼吸面板关闭
            musicPanel.SetActive(true);
            Debug.Log("[Breathing] musicPanel.activeSelf after: " + musicPanel.activeSelf);
            if (hud != null)
                hud.ShowSubtitle("请选择您喜欢的音乐和场景，放松身心");
        }

        public void StartMassageDemo()
        {
            HideAll();
            breathingPanel.SetActive(false);  // 确保呼吸面板关闭
            massagePanel.SetActive(true);
            if (hud != null)
                hud.ShowSubtitle("小安：选一个按摩力度，缓解腰骶酸痛");
        }

        public void StartPositionCards()
        {
            HideAll();
            breathingPanel.SetActive(false);  // 确保呼吸面板关闭
            positionPanel.SetActive(true);
            ShowPositionCard(0);
            if (hud != null)
                hud.ShowSubtitle("小安：点击了解每种缓解体位");
        }

        // ── 呼吸跟练（三阶段） ─────────────────────────────────
        // 新增：呼吸吻合度算法——根据用户按键时机与光球节奏的匹配程度计算百分比分数

        private float breathMatchScore;  // 当前阶段吻合度得分
        private int breathTotalSamples;  // 采样次数
        private float breathCorrectSamples;  // 正确响应次数（float 以支持半次计分）

        private readonly (string name, string instruction, float cycleTime, int reps, string subtitle)[]
            breathingStages = new[]
        {
            (
                "廓清式呼吸",
                "深吸气 → 缓慢呼出，排空肺部",
                4f, 3,
                "小安：先做几次廓清式呼吸"
            ),
            (
                "胸式呼吸",
                "肩膀轻抬缓放 · 吸2秒 呼2秒",
                4f, 8,
                "小安：宫缩间歇用胸式呼吸放松"
            ),
            (
                "浅快呼吸",
                "像小狗喘气一样浅而快 · 宫缩最强时用",
                2f, 5,
                "小安：宫缩最强时改用浅快呼吸"
            ),
        };

        void StartBreathingPhase(int phase)
        {
            breathingPhase = phase;
            // 重置吻合度计分
            breathMatchScore = 0f;
            breathTotalSamples = 0;
            breathCorrectSamples = 0f;
            var stage = breathingStages[phase];
            if (breatheText != null)
                breatheText.text = $"{stage.name}\n{stage.instruction}";
            if (matchPercentText != null)
                matchPercentText.text = "吻合度: 等待中...";
            StartCoroutine(BreathingLoop(stage.cycleTime, stage.reps));
        }

        IEnumerator BreathingLoop(float cycleTime, int reps)
        {
            Debug.Log("[Breathing] BreathingLoop 开始, phase=" + breathingPhase + " cycleTime=" + cycleTime + " reps=" + reps + " breatheSphere=" + (breatheSphere != null));
            if (breatheSphere != null)
            {
                Debug.Log("[Breathing] breatheSphere初始 fillAmount=" + breatheSphere.fillAmount + " type=" + breatheSphere.type + " fillMethod=" + breatheSphere.fillMethod);
            }
            float inhaleTime = cycleTime * 0.5f;
            float exhaleTime = cycleTime * 0.5f;

            // 记录用户是否在该吸气阶段按下按键
            bool userPressedThisInhale = false;
            bool userPressedThisExhale = false;

            for (int rep = 0; rep < reps; rep++)
            {
                // 吸气阶段
                userPressedThisInhale = false;
                float t = 0f;
                while (t < inhaleTime)
                {
                    t += Time.deltaTime;
                    float fill = t / inhaleTime;
                    if (breatheSphere != null)
                    {
                        breatheSphere.fillAmount = fill;
                        if (rep == 0 && t < 0.5f) // 只在第一次吸气时打印调试日志
                            Debug.Log("[Breathing] 吸气中 fill=" + fill);
                    }
                    if (matchPercentText != null)
                        matchPercentText.text = $"吸气中 (吻合度: {GetMatchPercent()}%)";

                    // 检测用户按下扳机键（表示跟上了吸气节奏）
                    if (!userPressedThisInhale && VRInput.TriggerDown)
                    {
                        userPressedThisInhale = true;
                    }
                    yield return null;
                }
                // 呼气阶段
                userPressedThisExhale = false;
                t = 0f;
                while (t < exhaleTime)
                {
                    t += Time.deltaTime;
                    float fill = 1f - (t / exhaleTime);
                    if (breatheSphere != null)
                    {
                        breatheSphere.fillAmount = fill;
                        if (rep == 0 && t < 0.5f)
                            Debug.Log("[Breathing] 呼气中 fill=" + fill);
                    }
                    if (matchPercentText != null)
                        matchPercentText.text = $"呼气中 (吻合度: {GetMatchPercent()}%)";

                    // 检测用户松开扳机键（表示跟上了呼气节奏）
                    if (!userPressedThisExhale && VRInput.TriggerUp)
                    {
                        userPressedThisExhale = true;
                    }
                    yield return null;
                }

                // 本轮呼吸结束后记录评分
                breathTotalSamples++;
                if (userPressedThisInhale && userPressedThisExhale)
                    breathCorrectSamples++;
                else if (userPressedThisInhale || userPressedThisExhale)
                    breathCorrectSamples += 0.5f;  // 部分匹配算半次

                // 每次正确完成一次呼吸，给一次轻微震动反馈
                VRInput.VibrateBoth(0.2f, 0.05f);
            }

            // 阶段完成，显示最终吻合度
            float finalPercent = breathTotalSamples > 0 ? (breathCorrectSamples / breathTotalSamples) * 100f : 0f;
            breathMatchScore = finalPercent;
            if (matchPercentText != null)
                matchPercentText.text = $"吻合度: {finalPercent:F0}%";
            if (hud != null)
                hud.ShowSubtitle($"阶段 {breathingPhase + 1} 完成！呼吸吻合度 {finalPercent:F0}%，{GetMatchComment(finalPercent)}");
            Debug.Log($"[Breathing] 阶段 {breathingPhase + 1}/{3} 完成，吻合度 {finalPercent:F0}%，下一个 phase={breathingPhase + 1} < 2: {(breathingPhase < 2)}");

            if (breathingPhase < 2)
            {
                // 阶段切换直接进行，不淡入淡出
                StartBreathingPhase(breathingPhase + 1);
            }
            else
            {
                scoreManager?.AddScore(5);
                breathingPanel.SetActive(false);
                if (hud != null)
                {
                    hud.ShowSubtitle("呼吸训练完成！三个阶段都掌握了");
                    hud.ClearSubtitle();
                }
                Debug.Log("[Breathing] 呼吸完成，直接切换到音乐放松");
                StartMusicRelax();
            }
        }

        // ── 呼吸吻合度评分辅助方法（新增）────────────────────

        float GetMatchPercent()
        {
            if (breathTotalSamples == 0) return 0f;
            return (breathCorrectSamples / breathTotalSamples) * 100f;
        }

        // 练习模式：点击按钮增加10%匹配度
        public void OnPracticeModeClick()
        {
            if (breathTotalSamples == 0)
            {
                // 如果没有采样，直接设置10%
                breathCorrectSamples = 0.1f;
                breathTotalSamples = 1;
            }
            else
            {
                // 增加10%的匹配度（最多到100%）
                float currentPercent = GetMatchPercent();
                float newPercent = Mathf.Min(100f, currentPercent + 10f);
                breathCorrectSamples = (newPercent / 100f) * breathTotalSamples;
            }

            if (matchPercentText != null)
                matchPercentText.text = $"吻合度: {GetMatchPercent():F0}%（练习模式+10%）";

            if (hud != null)
                hud.ShowSubtitle("练习模式：匹配度已增加10%");

            Debug.Log("[Breathing] 练习模式点击，匹配度增加10%");
        }

        string GetMatchComment(float percent)
        {
            if (percent >= 80) return "做得非常好，节奏把握很准确！";
            if (percent >= 60) return "表现不错，继续练习会更熟练";
            if (percent >= 40) return "还有提升空间，注意跟随光球节奏";
            return "别着急，多练习几次就能找到节奏了";
        }

        // ── 音乐与想象疗法 ─────────────────────────────────────

        private readonly string[] musicOptions = new[] { "海浪轻拍", "森林鸟鸣", "钢琴抒情", "自然白噪音" };
        private readonly string[] sceneOptions = new[] { "海边日落", "森林小径", "山间溪流", "花园秋千" };

        void SetupMusicButtons()
        {
            for (int i = 0; i < musicButtons.Length; i++)
            {
                int idx = i;
                musicButtons[i]?.onClick.AddListener(() => OnMusicSelected(idx));
            }
            for (int i = 0; i < sceneButtons.Length; i++)
            {
                int idx = i;
                sceneButtons[i]?.onClick.AddListener(() => OnSceneSelected(idx));
            }
        }

        void OnMusicSelected(int idx)
        {
            if (VRInput.TriggerDown) VRInput.VibrateRight(0.3f, 0.05f);
            selectedMusic = musicOptions[idx];
            if (hud != null)
                hud.ShowSubtitle($"已选「{selectedMusic}」，再选一个场景");
            Debug.Log($"[音乐疗法] 选择: {selectedMusic}");
        }

        void OnSceneSelected(int idx)
        {
            if (VRInput.TriggerDown) VRInput.VibrateRight(0.3f, 0.05f);
            selectedScene = sceneOptions[idx];
            // 切换场景背景色（视觉反馈，对应"画面切换至海边/森林等放松场景"）
            SetSceneBackground(idx);
            if (hud != null)
                hud.ShowSubtitle($"已选「{selectedScene}」，放松这 {musicDuration} 秒");
            Debug.Log($"[音乐疗法] 选择场景: {selectedScene}");
            // 播放放松音乐（重复选择场景时先取消旧的计时，避免完成回调被触发多次）
            CancelInvoke(nameof(MusicRelaxDone));
            relaxPending = true;
            relaxStart = Time.time;
            PlayRelaxMusic();
            ShowSkipButton();
            Invoke(nameof(MusicRelaxDone), musicDuration);
        }

        private Button _skipRelaxBtn;

        void ShowSkipButton()
        {
            if (_skipRelaxBtn != null) { _skipRelaxBtn.gameObject.SetActive(true); return; }
            var go = new GameObject("SkipRelaxButton");
            go.transform.SetParent(musicPanel != null ? musicPanel.transform : transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0, -200);
            rt.sizeDelta = new Vector2(220, 56);
            var img = go.AddComponent<Image>();
            img.color = ProStyle.ButtonBg;
            var txtGo = new GameObject("Label");
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one; txtRt.sizeDelta = Vector2.zero;
            var txt = txtGo.AddComponent<Text>();
            txt.text = "✓ 结束放松";
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontSize = 24;
            txt.color = ProStyle.TextMain;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _skipRelaxBtn = go.AddComponent<Button>();
            _skipRelaxBtn.onClick.AddListener(() => SkipRelax());
        }

        void HideSkipButton()
        {
            if (_skipRelaxBtn != null) _skipRelaxBtn.gameObject.SetActive(false);
        }

        private bool relaxPending;
        private float relaxStart;

        // 场景背景色切换
        private readonly Color[] sceneBgColors = new[]
        {
            new Color(0.2f, 0.5f, 0.9f, 1f),  // 海边日落 - 蓝色
            new Color(0.2f, 0.7f, 0.3f, 1f),  // 森林小径 - 绿色
            new Color(0.7f, 0.7f, 0.7f, 1f),  // 山间溪流 - 淡灰色
            new Color(0.9f, 0.5f, 0.7f, 1f),  // 花园秋千 - 粉色
        };

        void SetSceneBackground(int sceneIdx)
        {
            if (sceneBgPanel == null) return;
            var img = sceneBgPanel.GetComponent<Image>();
            if (img != null)
                img.color = sceneBgColors[sceneIdx];
        }

        void MusicRelaxDone()
        {
            if (!relaxPending) return;  // 防止重复进入
            relaxPending = false;
            HideSkipButton();
            Debug.Log("[Breathing] MusicRelaxDone 被调用");
            StopRelaxMusic();
            scoreManager?.AddScore(2);
            if (musicPanel != null) musicPanel.SetActive(false);
            // 淡出场景背景
            if (sceneBgPanel != null)
            {
                var img = sceneBgPanel.GetComponent<Image>();
                if (img != null)
                    img.color = new Color(0.15f, 0.15f, 0.2f, 0f);
            }
            if (hud != null)
            {
                hud.ShowSubtitle("音乐放松结束，接下来学习按摩");
                hud.ClearSubtitle();
            }
            StartMassageDemo();
        }

        // ── 音乐播放 ─────────────────────────────────────────

        void PlayRelaxMusic()
        {
            if (relaxAudioClip != null && musicAudioSource != null)
            {
                musicAudioSource.clip = relaxAudioClip;
                musicAudioSource.Play();
                Debug.Log("[音乐疗法] 开始播放放松音乐");
            }
            else
            {
                Debug.LogWarning("[音乐疗法] 未配置放松音乐，请挂载 AudioClip 到 BreathingModule 的 relaxAudioClip 字段");
            }
        }

        void StopRelaxMusic()
        {
            if (musicAudioSource != null && musicAudioSource.isPlaying)
            {
                musicAudioSource.Stop();
                Debug.Log("[音乐疗法] 停止播放放松音乐");
            }
        }

        // ── 床旁按摩 ───────────────────────────────────────────

        private readonly string[] pressureLabels = new[] { "轻柔", "适中", "稍重" };
        private readonly string[] pressureTips = new[]
        {
            "轻柔 · 适合敏感部位",
            "适中 · 缓解腰骶酸痛",
            "稍重 · 适合肌肉厚处"
        };

        void SetupMassageButtons()
        {
            for (int i = 0; i < pressureButtons.Length; i++)
            {
                int idx = i;
                pressureButtons[i]?.onClick.AddListener(() => OnPressureSelected(idx));
            }
            massageConfirmBtn?.onClick.AddListener(() => {
                if (massageConfirmBtn.gameObject.activeSelf)
                    AdvanceToPositionCards();
            });
        }

        void SetupPracticeModeButton()
        {
            practiceModeBtn?.onClick.AddListener(() => OnPracticeModeClick());
        }

        void OnPressureSelected(int idx)
        {
            if (VRInput.TriggerDown) VRInput.VibrateRight(0.3f, 0.05f);
            if (massageTipText != null) massageTipText.text = pressureTips[idx];
            if (massageDetailText != null)
                massageDetailText.text = GetMassageDetail(idx);
            if (hud != null)
                hud.ShowSubtitle(pressureTips[idx]);
            scoreManager?.AddScore(1);
            // 显示确认按钮，让用户确认后进入下一环节
            if (massageConfirmBtn != null)
                massageConfirmBtn.gameObject.SetActive(true);
        }

        // 按摩手法详细说明（精简为要点式）
        private readonly string[] massageDetails = new[]
        {
            "●  拇指画圈轻揉腰骶部\n●  节奏与宫缩同步\n●  每分钟约 30 次",
            "●  双手交叠贴住腰骶\n●  顺时针中等力度\n●  配合轻声安慰",
            "●  掌根沿脊柱两侧推按\n●  自上而下，力度稍重\n●  避开脊柱正中",
        };

        string GetMassageDetail(int idx) => massageDetails[idx];

        void AdvanceToPositionCards()
        {
            massagePanel.SetActive(false);
            StartPositionCards();
        }

        // ── 卧床位姿卡片 ──────────────────────────────────────
        // 扩展为5张：3种卧位 + 2种站立类方法（以知识卡片形式）

        private readonly (string title, string desc, string subtitle)[] positionCards = new[]
        {
            (
                "左倾卧位",
                "●  左侧卧，右腿弯曲垫枕\n●  改善胎盘血流\n●  适合头晕 / 血压偏低",
                "护士小安：左侧卧，给宝宝更好的血流"
            ),
            (
                "半卧位",
                "●  床头抬高 30-60°\n●  膝盖微屈垫枕\n●  呼吸更顺畅 · 待产首选",
                "护士小安：半卧位，待产时最常用"
            ),
            (
                "屈膝垫枕位",
                "●  平卧屈膝，小腿垫枕\n●  放松腰背肌肉\n●  每 30 分钟换一次边",
                "护士小安：屈膝垫枕，腰背更放松"
            ),
            (
                "分娩球（站立类）",
                "●  坐球弹动或画圈\n●  助胎头下降，缓解腰酸\n●  需家属或护士看护",
                "护士小安：分娩球，帮助加速产程"
            ),
            (
                "自由体位（站立类）",
                "●  站立倚靠，前倾支撑\n●  减轻腰背压力\n●  胎心正常方可尝试",
                "护士小安：自由体位，找最舒服的姿势"
            ),
        };

        private int currentPositionCard = 0;

        void SetupPositionCards()
        {
            int visibleCount = Mathf.Min(positionCardButtons.Length, positionCards.Length);
            for (int i = 0; i < visibleCount; i++)
            {
                int idx = i;
                positionCardButtons[i]?.onClick.AddListener(() => OnPositionCardClick(idx));
            }
            for (int i = visibleCount; i < positionCardButtons.Length; i++)
                positionCardButtons[i]?.gameObject.SetActive(false);
            positionConfirmBtn?.onClick.AddListener(() => {
                if (positionConfirmBtn.gameObject.activeSelf)
                    ShowPositionCard(positionCards.Length);
            });
        }

        void ShowPositionCard(int index)
        {
            if (index >= positionCards.Length)
            {
                positionPanel.SetActive(false);
                if (positionConfirmBtn != null) positionConfirmBtn.gameObject.SetActive(false);
                if (hud != null)
                {
                    hud.ShowSubtitle("体位学习完成！");
                    hud.ClearSubtitle();
                }
                Invoke(nameof(BreathingModuleDone), 1f);
                return;
            }
            currentPositionCard = index;
            var card = positionCards[index];
            if (positionCardTitle != null) positionCardTitle.text = card.title;
            if (positionCardDesc != null) positionCardDesc.text = card.desc;
            if (hud != null) hud.ShowSubtitle(card.subtitle);

            // 最后一张卡片显示"确认完成"按钮
            bool isLast = index >= positionCards.Length - 1;
            if (positionConfirmBtn != null)
                positionConfirmBtn.gameObject.SetActive(isLast);
        }

        void OnPositionCardClick(int idx)
        {
            if (VRInput.TriggerDown) VRInput.VibrateRight(0.3f, 0.05f);
            ShowPositionCard(idx + 1);
            scoreManager?.AddScore(1);
        }

        void BreathingModuleDone()
        {
            Debug.Log("[Breathing] BreathingModuleDone 被调用");
            scoreManager?.AddScore(3);
            if (hud != null)
                hud.ShowSubtitle("非药物镇痛完成！进入法宝选择");
            OnModuleComplete?.Invoke();
        }

        public void HideAll()
        {
            if (breathingPanel) breathingPanel.SetActive(false);
            if (musicPanel) musicPanel.SetActive(false);
            if (massagePanel) massagePanel.SetActive(false);
            if (positionPanel) positionPanel.SetActive(false);
        }
    }
}
