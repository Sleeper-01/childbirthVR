using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
namespace VRTour
{
    //随堂小测：护士提问3道选择题
    //射线点击选项：答对安心值增加并播放语音肯定；答错给出讲解
    public class QuizManager : MonoBehaviour
    {
        [Header("小测板锚点（留空自动创建；挪到墙面，Z 轴朝向房间）")]
        public Transform quizBoardAnchor;
        [Header("中文字体资产")]
        public TMP_FontAsset chineseFont;
        [Header("答对语音肯定（Assets/Audio/quiz_correct.mp3）")]
        public AudioClip correctPraiseClip;
        [Header("答错引导语音（Assets/Audio/quiz_wrong.mp3）")]
        public AudioClip wrongFeedbackClip;
        [Header("护士读题语音（Assets/Audio/quiz_q1~3.mp3，用菜单【产程演示/一键绑定语音引用】自动填）")]
        public AudioClip[] questionClips;
        [Header("答对每次增加的安心值")]
        public int scorePerCorrect = 10;
        [Header("答题讲解显示时长（秒），到时自动下一题")]
        public float explainDuration = 9f;
        private class QuestionInfo
        {
            public string question;
            public string[] options = new string[3];
            public int correctIndex;
            public string explanation;
        }
        //三道产程知识题
        private static readonly QuestionInfo[] Questions =
        {
            new QuestionInfo
            {
                question = "第一产程（宫颈扩张期）可以做什么？",
                options = new[] { "适当走动休息，及时进食补充体力", "立刻持续用力屏气", "绝对卧床，禁止进食" },
                correctIndex = 0,
                explanation = "第一产程宫口尚未开全，过早用力会消耗体力并造成宫颈水肿；适当活动、少量进食才能为第二产程蓄力。"
            },
            new QuestionInfo
            {
                question = "第二产程宝宝娩出的关键是什么？",
                options = new[] { "随宫缩节奏屏气用力，听从护士指导", "大声喊叫，不用力配合", "憋气越久越好" },
                correctIndex = 0,
                explanation = "第二产程要跟随宫缩用力、间歇放松休息，听从护士指导能加快产程并减少会阴损伤。"
            },
            new QuestionInfo
            {
                question = "第三产程结束后为什么还要观察2小时？",
                options = new[] { "警惕产后出血，观察宫缩与生命体征", "等待胎盘再次娩出", "没有特殊原因，例行等待" },
                correctIndex = 0,
                explanation = "产后2小时是产后出血高发时段，需持续监测宫缩、阴道流血量与生命体征，确保母婴安全。"
            }
        };
        // ✅ 选项卡配色对齐序幕色板：卡片=PanelHover 深蓝半透明，正确=Success，错误=Danger，悬停=Warn
        private readonly Color cardColor = new Color(0.16f, 0.24f, 0.34f, 0.95f);
        private static readonly Color correctColor = new Color(0.55f, 0.80f, 0.55f, 0.6f);
        private static readonly Color wrongColor = new Color(0.95f, 0.42f, 0.42f, 0.6f);
        private static readonly Color hoverColor = new Color(1f, 0.72f, 0.30f, 0.55f);
        private TextMeshPro titleLabel;
        private TextMeshPro progressLabel;
        private TextMeshPro questionLabel;
        private readonly GameObject[] optionCards = new GameObject[3];
        private readonly TextMeshPro[] optionLabels = new TextMeshPro[3];
        private int[] displayOrder = { 0, 1, 2 };
        private int currentQuestion;
        private int reassurance; //安心值
        private int correctCount;
        private bool locked;     //答题讲解期间锁定输入
        private bool built;
        private bool finished;
        private bool quizStarted; //是否已解锁（拼图→团队集结之后）
        private TeamMeetManager team;
        private Camera mainCamera;
        private OpeningIntro openingIntro;
        private UIManager uiManager;
        private AudioSource audioSource;
        private int hoveredOption = -1;
        void Start()
        {
            mainCamera = Camera.main;
            openingIntro = FindFirstObjectByType<OpeningIntro>();
            uiManager = FindFirstObjectByType<UIManager>();
            team = FindFirstObjectByType<TeamMeetManager>();
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
            if (quizBoardAnchor == null)
            {
                quizBoardAnchor = new GameObject("QuizBoardAnchor").transform;
                quizBoardAnchor.SetParent(transform, false);
            }
            if (chineseFont == null)
            {
                Debug.LogWarning("QuizManager.chineseFont 未指定，中文将无法显示。请重新执行【产程演示/生成医护团队与小测】。", this);
            }
            BuildBoard();
            ShowQuestion(0);
            //小测板先隐藏，等前置流程完成后才出现
            quizBoardAnchor.gameObject.SetActive(false);
            built = true;
        }
        void Update()
        {
            if (!built || finished) return;
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;
            //随堂小测在开场介绍结束且医护团队全部自我介绍展示完毕后才开始（场景无团队功能时不受限）
            if (!quizStarted)
            {
                bool introDone = openingIntro == null || openingIntro.tourCanStart;
                //麻醉师介绍展示完、集结提示播完后，小测才出现
                bool teamDone = team == null || team.IntroSequenceDone;
                if (introDone && teamDone)
                {
                    quizStarted = true;
                    quizBoardAnchor.gameObject.SetActive(true);
                    if (uiManager != null)
                    {
                        uiManager.ShowTip("随堂小测开启：射线点击选项作答，答对增加安心值！", 6f);
                    }
                    //解锁时护士读第一题
                    PlayQuestionAudio();
                }
                return;
            }
            if (locked) return;
            // 射线获取：VR手柄优先，桌面鼠标兜底
            Ray ray;
            bool vrClick = false;
            var vrVis = FindObjectOfType<ChuJian.Merge.VRPointerVisual>();
            Vector3 vrO, vrD;
            if (vrVis != null && vrVis.GetRightRay(out vrO, out vrD))
            {
                ray = new Ray(vrO, vrD);
                bool trig = ChuJian.Merge.VRPointerVisual.IsAnyTriggerHeld;
                vrClick = trig && !_prevTrig;
                _prevTrig = trig;
            }
            else
            {
                ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            }

            int hitOption = -1;
            if (Physics.Raycast(ray, out RaycastHit hit, 30f))
            {
                for (int i = 0; i < optionCards.Length; i++)
                {
                    if (hit.collider.gameObject == optionCards[i])
                    {
                        hitOption = i;
                        break;
                    }
                }
            }
            //悬停高亮
            if (hitOption != hoveredOption)
            {
                if (hoveredOption >= 0)
                {
                    SetCardColor(hoveredOption, cardColor);
                }
                if (hitOption >= 0)
                {
                    SetCardColor(hitOption, hoverColor);
                }
                hoveredOption = hitOption;
            }
            if (hitOption >= 0 && (Input.GetMouseButtonDown(0) || vrClick))
            {
                Answer(hitOption);
            }
        }

        private bool _prevTrig;
        private void Answer(int optionIdx)
        {
            locked = true;
            QuestionInfo q = Questions[currentQuestion];
            bool correct = displayOrder[optionIdx] == q.correctIndex;
            if (correct)
            {
                reassurance += scorePerCorrect;
                correctCount++;
                SetCardColor(optionIdx, correctColor);
                if (audioSource != null && correctPraiseClip != null)
                {
                    audioSource.PlayOneShot(correctPraiseClip);
                }
                if (uiManager != null)
                {
                    uiManager.ShowTip("答对了！安心值 +" + scorePerCorrect + "\n" + q.explanation, explainDuration + 2f);
                }
            }
            else
            {
                SetCardColor(optionIdx, wrongColor);
                if (audioSource != null && wrongFeedbackClip != null)
                {
                    audioSource.PlayOneShot(wrongFeedbackClip);
                }
                if (uiManager != null)
                {
                    uiManager.ShowTip("答错了，没关系。正确答案是「" + q.options[q.correctIndex] + "」\n" + q.explanation, explainDuration + 2f);
                }
            }
            UpdateProgress();
            StartCoroutine(NextAfter(explainDuration));
        }
        private IEnumerator NextAfter(float wait)
        {
            yield return new WaitForSeconds(wait);
            if (currentQuestion + 1 < Questions.Length)
            {
                currentQuestion++;
                ShowQuestion(currentQuestion);
            }
            else
            {
                Finish();
            }
        }
        private void ShowQuestion(int index)
        {
            QuestionInfo q = Questions[index];
            questionLabel.text = q.question;
            //选项随机排列，避免背位置
            List<int> order = new List<int> { 0, 1, 2 };
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            displayOrder = order.ToArray();
            for (int i = 0; i < optionCards.Length; i++)
            {
                optionLabels[i].text = ((char)('A' + i)) + ". " + q.options[displayOrder[i]];
                SetCardColor(i, cardColor);
            }
            UpdateProgress();
            locked = false;
            //切题时护士读题
            if (quizStarted)
            {
                PlayQuestionAudio();
            }
        }
        //护士语音读当前题
        private void PlayQuestionAudio()
        {
            if (audioSource != null
                && questionClips != null
                && currentQuestion < questionClips.Length
                && questionClips[currentQuestion] != null)
            {
                audioSource.PlayOneShot(questionClips[currentQuestion]);
            }
        }
        private void Finish()
        {
            finished = true;
            for (int i = 0; i < optionCards.Length; i++)
            {
                optionCards[i].SetActive(false);
                optionLabels[i].gameObject.SetActive(false);
            }
            titleLabel.text = "随堂小测 · 已完成";
            questionLabel.text = "小测结束！答对 " + correctCount + "/3 题，安心值 +" + reassurance;
            UpdateProgress();
            if (uiManager != null)
            {
                string comment = correctCount >= 3
                    ? "满分！你已经是待产小达人，分娩路上一定从容安心。"
                    : (correctCount >= 2 ? "不错的成绩！把讲解再回顾一遍就更稳了。" : "别灰心，回顾拼图与讲解，再想想这几道题。");
                uiManager.ShowTip("小测结束：答对 " + correctCount + "/3 题，安心值 +" + reassurance + "。" + comment, 12f);
            }
        }
        private void UpdateProgress()
        {
            if (progressLabel != null)
            {
                progressLabel.text = "第 " + Mathf.Min(currentQuestion + 1, Questions.Length) + "/" + Questions.Length + " 题 · 安心值 " + reassurance;
            }
        }
        //程序化构建小测板
        private void BuildBoard()
        {
            //Z轴层级：背板0 → 选项方块0.04 → 标题/题目0.08 → 选项文字0.10，文字永远在最上层
            CreateQuad("Board", new Vector2(2.4f, 1.5f), new Color(0.10f, 0.13f, 0.17f, 0.88f), Vector3.zero, quizBoardAnchor, false); //序幕 Panel
            titleLabel = CreateLabel("Title", "随堂小测（护士提问）", new Color(0.96f, 0.97f, 1.00f),
                new Vector3(0f, 0.60f, 0.08f), new Vector2(2.2f, 0.24f), quizBoardAnchor); //序幕 TextMain
            progressLabel = CreateLabel("Progress", "", new Color(1f, 0.72f, 0.30f,1f),
                new Vector3(0f, 0.44f, 0.08f), new Vector2(2.2f, 0.16f), quizBoardAnchor); //序幕 Warn
            questionLabel = CreateLabel("Question", "", new Color(0.96f, 0.97f, 1.00f),
                new Vector3(0f, 0.20f, 0.08f), new Vector2(2.2f, 0.36f), quizBoardAnchor); //序幕 TextMain
            float[] cardY = { -0.02f, -0.32f, -0.62f };
            for (int i = 0; i < 3; i++)
            {
                //选项方块 Z=0.04
                optionCards[i] = CreateQuad("Option_" + i, new Vector2(2.2f, 0.22f), cardColor,
                    new Vector3(0f, cardY[i], 0.04f), quizBoardAnchor, true);
                //✅选项文字用浅色（TextMain），配深色卡片 Z=0.10，浮在方块上方
                optionLabels[i] = CreateLabel("OptionLabel_" + i, "", new Color(0.96f, 0.97f, 1.00f, 1f),
                    new Vector3(0f, cardY[i], 0.10f), new Vector2(2.0f, 0.2f), quizBoardAnchor);
            }
            UpdateProgress();
        }
        private void SetCardColor(int index, Color color)
        {
            MeshRenderer mr = optionCards[index].GetComponent<MeshRenderer>();
            if (mr != null)
            {
                bool opaque = color.a >= 0.99f;
                Shader useShader = opaque ? Shader.Find("Unlit/Color") : Shader.Find("Sprites/Default");
                mr.material = new Material(useShader) { color = color };
            }
        }
        //创建原生Quad面板
        private GameObject CreateQuad(string name, Vector2 size, Color color, Vector3 localPos, Transform parent, bool withCollider)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = localPos;
            quad.transform.localScale = new Vector3(size.x, size.y, 1f);
            MeshRenderer mr = quad.GetComponent<MeshRenderer>();
            bool opaque = color.a >= 0.99f;
            Shader useShader = opaque ? Shader.Find("Unlit/Color") : Shader.Find("Sprites/Default");
            var quadMat = new Material(useShader) { color = color };
            if (!opaque) quadMat.mainTexture = ChuJian.Merge.ActUIStyle.QuadRoundedTexture;   // 统一圆角面板语言
            mr.sharedMaterial = quadMat;
            if (!withCollider)
            {
                Destroy(quad.GetComponent<Collider>());
            }
            return quad;
        }
        //创建3D世界空间TMP文字，强制scale=Vector3.one，杜绝文字挤压变形
        private TextMeshPro CreateLabel(string name, string text, Color color, Vector3 localPos, Vector2 size, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(TextMeshPro));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one;
            TextMeshPro tmp = go.GetComponent<TextMeshPro>();
            tmp.text = text;
            tmp.font = ChuJian.Merge.ActUIStyle.TmpFont != null ? ChuJian.Merge.ActUIStyle.TmpFont : chineseFont;
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.richText = false;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 0.1f;
            tmp.fontSizeMax = 200f;
            RectTransform rt = tmp.rectTransform;
            rt.sizeDelta = size;
            return tmp;
        }
    }
}
