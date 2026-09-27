using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class SequentialTextManager : MonoBehaviour
{
    // ==========================================
    // 【序幕】
    // ==========================================
    [Header("=== 序幕：标题文本 ===")]
    public TextMeshProUGUI titleText;
    [Tooltip("标题淡入时间")]
    public float fadeInTime = 1.5f;
    [Tooltip("标题停留时间")]
    public float holdTime = 3f;
    [Tooltip("标题淡出时间")]
    public float fadeOutTime = 1.2f;
    [Tooltip("是否允许按空格跳过序幕")]
    public bool allowSkipPrologue = true;

    // ==========================================
    // 【第1步】5个顺序文本
    // ==========================================
    [Header("=== 第1步：5个顺序文本 ===")]
    public TextMeshProUGUI seqText1;
    public TextMeshProUGUI seqText2;
    public TextMeshProUGUI seqText3;
    public TextMeshProUGUI seqText4;
    public TextMeshProUGUI seqText5;

    [Header("=== 第1步：初始显示控制 ===")]
    public bool showText1AtStart = true;
    public bool showText2AtStart = false;
    public bool showText3AtStart = false;
    public bool showText4AtStart = false;
    public bool showText5AtStart = false;

    [Header("=== 第1步：播放设置 ===")]
    public float seqTextDisplayTime = 3f;
    public float seqTextInterval = 0.5f;

    // ==========================================
    // 【第1步后】4个按钮 + 3个后续文本
    // ==========================================
    [Header("=== 第2步：4个按钮 ===")]
    public Button resetAllButton;
    public Button showNextATextButton;
    public Button showNextBTextButton;
    public Button showNextCTextButton;

    [Header("=== 第2步：3个后续文本 ===")]
    public TextMeshProUGUI nextTextA;
    public TextMeshProUGUI nextTextB;
    public TextMeshProUGUI nextTextC;

    [Header("=== 最终文本 ===")]
    public TextMeshProUGUI finalText;
    public float finalTextDelay = 1.5f;
    public float finalTextDisplayTime = 3f;

    // ==========================================
    // 【第2步：答题环节】
    // ==========================================
    [Header("=== 第2步：引入文本 ===")]
    [Tooltip("第1段引入文本")]
    public TextMeshProUGUI step2IntroText1;
    [Tooltip("第2段引入文本")]
    public TextMeshProUGUI step2IntroText2;
    [Tooltip("每段引入文本显示时长")]
    public float step2IntroDisplayTime = 3f;

    [Header("=== 第2步：视频播放 ===")]
    public VideoPlayer step2VideoPlayer;
    public RawImage step2VideoDisplay;
    [Tooltip("是否等待视频播放完毕才进入答题")]
    public bool step2WaitVideoFinish = true;

    [Header("=== 第2步：常驻图片 ===")]
    [Tooltip("图片1：全程显示的基础图片")]
    public Image step2Image1;
    [Tooltip("图片2：答对时显示")]
    public Image step2Image2;
    [Tooltip("图片3：答错时显示")]
    public Image step2Image3;

    [Header("=== 第2步：答题设置 ===")]
    [Tooltip("题目数量（1~7）")]
    [Range(1, 7)]
    public int step2QuestionCount = 7;
    [Tooltip("每道题的按钮数量（1~4）")]
    [Range(1, 4)]
    public int step2ButtonsPerQuestion = 4;
    [Tooltip("答对/答错后，反馈文本与图片2/3一起显示的时长")]
    public float step2AnswerTextDisplayTime = 2f;

    [Header("=== 第2步：7道题 Prompt 文本 ===")]
    public TextMeshProUGUI questionPrompt1;
    public TextMeshProUGUI questionPrompt2;
    public TextMeshProUGUI questionPrompt3;
    public TextMeshProUGUI questionPrompt4;
    public TextMeshProUGUI questionPrompt5;
    public TextMeshProUGUI questionPrompt6;
    public TextMeshProUGUI questionPrompt7;

    [Header("=== 第2步：7道题 按钮组（每题4个按钮）===")]
    public Button q1Button1; public Button q1Button2; public Button q1Button3; public Button q1Button4;
    public Button q2Button1; public Button q2Button2; public Button q2Button3; public Button q2Button4;
    public Button q3Button1; public Button q3Button2; public Button q3Button3; public Button q3Button4;
    public Button q4Button1; public Button q4Button2; public Button q4Button3; public Button q4Button4;
    public Button q5Button1; public Button q5Button2; public Button q5Button3; public Button q5Button4;
    public Button q6Button1; public Button q6Button2; public Button q6Button3; public Button q6Button4;
    public Button q7Button1; public Button q7Button2; public Button q7Button3; public Button q7Button4;

    [Header("=== 第2步：7道题 反馈文本（每题的选项文本）===")]
    [Tooltip("第1题 4个选项对应的反馈文本")]
    public TextMeshProUGUI q1Text1; public TextMeshProUGUI q1Text2; public TextMeshProUGUI q1Text3; public TextMeshProUGUI q1Text4;
    [Tooltip("第2题 4个选项对应的反馈文本")]
    public TextMeshProUGUI q2Text1; public TextMeshProUGUI q2Text2; public TextMeshProUGUI q2Text3; public TextMeshProUGUI q2Text4;
    [Tooltip("第3题 4个选项对应的反馈文本")]
    public TextMeshProUGUI q3Text1; public TextMeshProUGUI q3Text2; public TextMeshProUGUI q3Text3; public TextMeshProUGUI q3Text4;
    [Tooltip("第4题 4个选项对应的反馈文本")]
    public TextMeshProUGUI q4Text1; public TextMeshProUGUI q4Text2; public TextMeshProUGUI q4Text3; public TextMeshProUGUI q4Text4;
    [Tooltip("第5题 4个选项对应的反馈文本")]
    public TextMeshProUGUI q5Text1; public TextMeshProUGUI q5Text2; public TextMeshProUGUI q5Text3; public TextMeshProUGUI q5Text4;
    [Tooltip("第6题 4个选项对应的反馈文本")]
    public TextMeshProUGUI q6Text1; public TextMeshProUGUI q6Text2; public TextMeshProUGUI q6Text3; public TextMeshProUGUI q6Text4;
    [Tooltip("第7题 4个选项对应的反馈文本")]
    public TextMeshProUGUI q7Text1; public TextMeshProUGUI q7Text2; public TextMeshProUGUI q7Text3; public TextMeshProUGUI q7Text4;

    [Header("=== 第2步：正确答案设置 ===")]
    [Tooltip("每道题的正确选项索引（0=按钮1, 1=按钮2, 2=按钮3, 3=按钮4）")]
    public int[] step2CorrectAnswers = new int[7] { 0, 0, 0, 0, 0, 0, 0 };

    // ==========================================
    // 【第3步：暖光过渡 + 场景切换】
    // ==========================================
    [Header("=== 第3步：暖光滤镜 ===")]
    public Image finalWarmLightFilter;
    [Range(0f, 1f)]
    public float finalMaxWarmAlpha = 1f;

    [Header("=== 第3步：暖光时间控制 ===")]
    [Tooltip("暖光从透明到最亮的时间")]
    public float finalWarmFadeInTime = 3f;
    [Tooltip("暖光达到最亮后停留的时间")]
    public float finalWarmHoldTime = 0f;
    [Tooltip("暖光淡出时间")]
    public float finalWarmFadeOutTime = 1f;
    [Tooltip("是否在暖光淡出后切换场景")]
    public bool finalFadeOutBeforeLoad = false;

    [Header("=== 第3步：场景切换 ===")]
    [Tooltip("要切换到的场景名称（合并工程按幕内顺序自动推断，此处可留空）")]
    public string nextSceneName = "";
    [Tooltip("是否使用场景名称。合并工程强制按名字跳转")]
    public bool useSceneName = true;
    [Tooltip("要切换到的场景 buildIndex。填 -1 表示自动加载下一个场景")]
    public int nextSceneIndex = 3;

    [Header("=== 第3步：过渡文本（可选） ===")]
    [Tooltip("暖光渐入时同步淡入的文本（可留空）")]
    public TextMeshProUGUI finalTransitionText;

    [Header("=== 调试 ===")]
    public bool showDebugLog = true;

    // 运行时状态
    private bool isPlayingPrologue = true;
    private bool[] seqTextPlayed = new bool[5];
    private bool finalShown = false;
    private bool hasChosenNextText = false;

    // 答题运行时
    private int currentQuestionIndex = -1;
    private bool waitingForAnswer = false;
    private int chosenAnswerIndex = -1;

    // 缓存题目数据
    private TextMeshProUGUI[] questionPrompts;
    private Button[][] questionButtons;
    private TextMeshProUGUI[][] questionTexts;

    void Start()
    {
        BuildQuestionCache();
        ForceHideAllUI();
        SetupButtons();

        StartCoroutine(PrologueSequence());
    }

    // ==========================================
    // 缓存题目相关对象
    // ==========================================
    void BuildQuestionCache()
    {
        questionPrompts = new TextMeshProUGUI[7]
        {
            questionPrompt1, questionPrompt2, questionPrompt3,
            questionPrompt4, questionPrompt5, questionPrompt6, questionPrompt7
        };

        questionButtons = new Button[7][];
        questionButtons[0] = new Button[] { q1Button1, q1Button2, q1Button3, q1Button4 };
        questionButtons[1] = new Button[] { q2Button1, q2Button2, q2Button3, q2Button4 };
        questionButtons[2] = new Button[] { q3Button1, q3Button2, q3Button3, q3Button4 };
        questionButtons[3] = new Button[] { q4Button1, q4Button2, q4Button3, q4Button4 };
        questionButtons[4] = new Button[] { q5Button1, q5Button2, q5Button3, q5Button4 };
        questionButtons[5] = new Button[] { q6Button1, q6Button2, q6Button3, q6Button4 };
        questionButtons[6] = new Button[] { q7Button1, q7Button2, q7Button3, q7Button4 };

        questionTexts = new TextMeshProUGUI[7][];
        questionTexts[0] = new TextMeshProUGUI[] { q1Text1, q1Text2, q1Text3, q1Text4 };
        questionTexts[1] = new TextMeshProUGUI[] { q2Text1, q2Text2, q2Text3, q2Text4 };
        questionTexts[2] = new TextMeshProUGUI[] { q3Text1, q3Text2, q3Text3, q3Text4 };
        questionTexts[3] = new TextMeshProUGUI[] { q4Text1, q4Text2, q4Text3, q4Text4 };
        questionTexts[4] = new TextMeshProUGUI[] { q5Text1, q5Text2, q5Text3, q5Text4 };
        questionTexts[5] = new TextMeshProUGUI[] { q6Text1, q6Text2, q6Text3, q6Text4 };
        questionTexts[6] = new TextMeshProUGUI[] { q7Text1, q7Text2, q7Text3, q7Text4 };
    }

    // ==========================================
    // 序幕
    // ==========================================
    IEnumerator PrologueSequence()
    {
        isPlayingPrologue = true;

        if (titleText != null)
        {
            titleText.gameObject.SetActive(true);

            yield return StartCoroutine(FadeText(0f, 1f, fadeInTime));

            float timer = 0f;
            while (timer < holdTime)
            {
                timer += Time.deltaTime;
                float scale = 1 + Mathf.Sin(Time.time * 1.2f) * 0.02f;
                titleText.transform.localScale = Vector3.one * scale;
                yield return null;
            }

            yield return StartCoroutine(FadeText(1f, 0f, fadeOutTime));

            titleText.gameObject.SetActive(false);
        }

        isPlayingPrologue = false;

        StartCoroutine(StartSequence());
    }

    IEnumerator FadeText(float startAlpha, float endAlpha, float duration)
    {
        if (titleText == null) yield break;

        float t = 0f;
        Color col = titleText.color;

        while (t < duration)
        {
            t += Time.deltaTime;
            col.a = Mathf.Lerp(startAlpha, endAlpha, t / duration);
            titleText.color = col;
            yield return null;
        }

        col.a = endAlpha;
        titleText.color = col;
    }

    void Update()
    {
        if (isPlayingPrologue && allowSkipPrologue && Input.GetKeyDown(KeyCode.Space))
        {
            StopAllCoroutines();
            SkipPrologue();
        }
    }

    void SkipPrologue()
    {
        isPlayingPrologue = false;

        if (titleText != null)
        {
            Color col = titleText.color;
            col.a = 0f;
            titleText.color = col;
            titleText.gameObject.SetActive(false);
        }

        StartCoroutine(StartSequence());
    }

    // ==========================================
    // 隐藏所有UI
    // ==========================================
    void ForceHideAllUI()
    {
        if (titleText != null)
        {
            titleText.gameObject.SetActive(false);
            Color col = titleText.color;
            col.a = 0f;
            titleText.color = col;
        }

        if (seqText1 != null) seqText1.gameObject.SetActive(false);
        if (seqText2 != null) seqText2.gameObject.SetActive(false);
        if (seqText3 != null) seqText3.gameObject.SetActive(false);
        if (seqText4 != null) seqText4.gameObject.SetActive(false);
        if (seqText5 != null) seqText5.gameObject.SetActive(false);

        if (nextTextA != null) nextTextA.gameObject.SetActive(false);
        if (nextTextB != null) nextTextB.gameObject.SetActive(false);
        if (nextTextC != null) nextTextC.gameObject.SetActive(false);

        if (resetAllButton != null) resetAllButton.gameObject.SetActive(false);
        if (showNextATextButton != null) showNextATextButton.gameObject.SetActive(false);
        if (showNextBTextButton != null) showNextBTextButton.gameObject.SetActive(false);
        if (showNextCTextButton != null) showNextCTextButton.gameObject.SetActive(false);

        if (finalText != null) finalText.gameObject.SetActive(false);

        if (step2IntroText1 != null) step2IntroText1.gameObject.SetActive(false);
        if (step2IntroText2 != null) step2IntroText2.gameObject.SetActive(false);

        if (step2VideoDisplay != null) step2VideoDisplay.gameObject.SetActive(false);

        if (step2Image1 != null) step2Image1.gameObject.SetActive(false);
        if (step2Image2 != null) step2Image2.gameObject.SetActive(false);
        if (step2Image3 != null) step2Image3.gameObject.SetActive(false);

        HideAllQuestions();
        HideAllQuestionTexts();

        if (finalWarmLightFilter != null)
        {
            finalWarmLightFilter.gameObject.SetActive(false);
            Color warmColor = finalWarmLightFilter.color;
            warmColor.a = 0f;
            finalWarmLightFilter.color = warmColor;
        }

        if (finalTransitionText != null)
        {
            finalTransitionText.gameObject.SetActive(false);
            Color textColor = finalTransitionText.color;
            textColor.a = 0f;
            finalTransitionText.color = textColor;
        }

        finalShown = false;
        hasChosenNextText = false;
        for (int i = 0; i < seqTextPlayed.Length; i++) seqTextPlayed[i] = false;
    }

    // ==========================================
    // 按钮绑定
    // ==========================================
    void SetupButtons()
    {
        if (resetAllButton != null)
            resetAllButton.onClick.AddListener(OnResetAllClicked);

        if (showNextATextButton != null)
            showNextATextButton.onClick.AddListener(() => OnNextTextClicked(0));

        if (showNextBTextButton != null)
            showNextBTextButton.onClick.AddListener(() => OnNextTextClicked(1));

        if (showNextCTextButton != null)
            showNextCTextButton.onClick.AddListener(() => OnNextTextClicked(2));

        for (int q = 0; q < 7; q++)
        {
            if (questionButtons[q] == null) continue;

            for (int b = 0; b < questionButtons[q].Length; b++)
            {
                int capturedQ = q;
                int capturedB = b;

                if (questionButtons[q][b] != null)
                {
                    questionButtons[q][b].onClick.AddListener(() => OnAnswerClicked(capturedQ, capturedB));
                }
            }
        }
    }

    // ==========================================
    // 正文开始（第1步）
    // ==========================================
    IEnumerator StartSequence()
    {
        yield return new WaitForSeconds(seqTextInterval);

        yield return StartCoroutine(PlayInitialTexts());

        ShowButtonsAfterSequence();
    }

    IEnumerator PlayInitialTexts()
    {
        if (seqText1 != null && showText1AtStart)
            yield return StartCoroutine(ShowAndHideSeqText(seqText1, 0));

        if (seqText2 != null && showText2AtStart)
            yield return StartCoroutine(ShowAndHideSeqText(seqText2, 1));

        if (seqText3 != null && showText3AtStart)
            yield return StartCoroutine(ShowAndHideSeqText(seqText3, 2));

        if (seqText4 != null && showText4AtStart)
            yield return StartCoroutine(ShowAndHideSeqText(seqText4, 3));

        if (seqText5 != null && showText5AtStart)
            yield return StartCoroutine(ShowAndHideSeqText(seqText5, 4));
    }

    IEnumerator ShowAndHideSeqText(TextMeshProUGUI text, int index)
    {
        if (text == null) yield break;

        HideAllSeqTextsExcept(text);

        text.gameObject.SetActive(true);
        seqTextPlayed[index] = true;

        yield return new WaitForSeconds(seqTextDisplayTime);

        text.gameObject.SetActive(false);
    }

    void ShowButtonsAfterSequence()
    {
        if (finalShown) return;

        if (resetAllButton != null)
            resetAllButton.gameObject.SetActive(true);

        if (showNextATextButton != null && nextTextA != null)
            showNextATextButton.gameObject.SetActive(true);

        if (showNextBTextButton != null && nextTextB != null)
            showNextBTextButton.gameObject.SetActive(true);

        if (showNextCTextButton != null && nextTextC != null)
            showNextCTextButton.gameObject.SetActive(true);
    }

    void OnResetAllClicked()
    {
        StopAllCoroutines();
        StartCoroutine(ResetAllRoutine());
    }

    IEnumerator ResetAllRoutine()
    {
        HideAllSeqTexts();
        HideAllNextTexts();
        HideAllButtons();

        if (finalText != null) finalText.gameObject.SetActive(false);
        finalShown = false;
        hasChosenNextText = false;

        for (int i = 0; i < seqTextPlayed.Length; i++) seqTextPlayed[i] = false;

        yield return StartCoroutine(StartSequence());
    }

    // ==========================================
    // 第1步后的按钮 → 显示后续文本 → 最终文本 → 第2步
    // ==========================================
    void OnNextTextClicked(int index)
    {
        if (hasChosenNextText) return;
        hasChosenNextText = true;

        TextMeshProUGUI text = GetNextTextByIndex(index);
        if (text == null) return;

        StartCoroutine(NextTextRoutine(text, index));
    }

    IEnumerator NextTextRoutine(TextMeshProUGUI text, int index)
    {
        yield return new WaitForSeconds(seqTextInterval);

        HideAllButtons();

        yield return StartCoroutine(ShowAndHideNextText(text, index));

        yield return StartCoroutine(ShowFinalTextBeforeStep2());

        yield return StartCoroutine(StartStep2());
    }

    IEnumerator ShowAndHideNextText(TextMeshProUGUI text, int index)
    {
        if (text == null) yield break;

        HideAllNextTextsExcept(text);

        text.gameObject.SetActive(true);

        yield return new WaitForSeconds(seqTextDisplayTime);

        text.gameObject.SetActive(false);
    }

    // ==========================================
    // 【最终文本】显示在引入文本之前
    // ==========================================
    IEnumerator ShowFinalTextBeforeStep2()
    {
        if (finalShown) yield break;
        if (finalText == null) yield break;

        HideAllButtons();
        HideAllNextTexts();

        yield return new WaitForSeconds(finalTextDelay);

        finalText.gameObject.SetActive(true);
        finalShown = true;

        if (showDebugLog) Debug.Log("显示最终文本（引入文本之前）。");

        yield return new WaitForSeconds(finalTextDisplayTime);

        finalText.gameObject.SetActive(false);

        if (showDebugLog) Debug.Log("最终文本消失，进入第2步。");
    }

    // ==========================================
    // 【第2步】引入文本 → 视频 → 答题
    // ==========================================
    IEnumerator StartStep2()
    {
        if (showDebugLog) Debug.Log("进入第2步：答题环节。");

        if (step2IntroText1 != null)
        {
            step2IntroText1.gameObject.SetActive(true);
            yield return new WaitForSeconds(step2IntroDisplayTime);
            step2IntroText1.gameObject.SetActive(false);
        }

        if (step2IntroText2 != null)
        {
            step2IntroText2.gameObject.SetActive(true);
            yield return new WaitForSeconds(step2IntroDisplayTime);
            step2IntroText2.gameObject.SetActive(false);
        }

        yield return StartCoroutine(PlayStep2Video());

        yield return StartCoroutine(StartStep2Questions());
    }

    IEnumerator PlayStep2Video()
    {
        if (step2VideoDisplay != null)
            step2VideoDisplay.gameObject.SetActive(true);

        if (step2VideoPlayer != null)
        {
            step2VideoPlayer.Stop();
            step2VideoPlayer.time = 0;
            step2VideoPlayer.Prepare();

            while (!step2VideoPlayer.isPrepared)
                yield return null;

            step2VideoPlayer.Play();

            if (step2WaitVideoFinish)
            {
                while (step2VideoPlayer.isPlaying)
                    yield return null;
            }
            else
            {
                yield return new WaitForSeconds(1f);
            }
        }

        if (step2VideoDisplay != null)
            step2VideoDisplay.gameObject.SetActive(false);
    }

    // ==========================================
    // 【第2步】答题流程（答题结束 → 直接进入第3步）
    // ==========================================
    IEnumerator StartStep2Questions()
    {
        for (int q = 0; q < step2QuestionCount && q < 7; q++)
        {
            if (!HasValidQuestion(q))
            {
                if (showDebugLog) Debug.Log($"第{q + 1}题没有资源，跳过。");
                continue;
            }

            yield return StartCoroutine(AskQuestion(q));
        }

        yield return StartCoroutine(StartStep3WarmLightTransition());
    }

    bool HasValidQuestion(int qIndex)
    {
        if (questionPrompts != null && questionPrompts[qIndex] != null) return true;

        if (questionButtons != null && questionButtons[qIndex] != null)
        {
            for (int b = 0; b < questionButtons[qIndex].Length; b++)
            {
                if (questionButtons[qIndex][b] != null) return true;
            }
        }

        return false;
    }

    IEnumerator AskQuestion(int qIndex)
    {
        currentQuestionIndex = qIndex;
        waitingForAnswer = true;
        chosenAnswerIndex = -1;

        ShowImage1();

        if (questionPrompts[qIndex] != null)
            questionPrompts[qIndex].gameObject.SetActive(true);

        ShowQuestionButtons(qIndex, step2ButtonsPerQuestion);

        while (waitingForAnswer)
            yield return null;

        HideQuestionButtons(qIndex);

        if (questionPrompts[qIndex] != null)
            questionPrompts[qIndex].gameObject.SetActive(false);

        bool isCorrect = (chosenAnswerIndex == step2CorrectAnswers[qIndex]);

        yield return StartCoroutine(ShowAnswerWithImage(qIndex, chosenAnswerIndex, isCorrect));

        HideImage2();
        HideImage3();

        bool isLastQuestion = IsLastValidQuestion(qIndex);

        if (isLastQuestion)
        {
            HideImage1();
        }
        else
        {
            ShowImage1();
        }
    }

    bool IsLastValidQuestion(int qIndex)
    {
        for (int q = qIndex + 1; q < step2QuestionCount && q < 7; q++)
        {
            if (HasValidQuestion(q)) return false;
        }
        return true;
    }

    void OnAnswerClicked(int qIndex, int bIndex)
    {
        if (!waitingForAnswer) return;
        if (qIndex != currentQuestionIndex) return;

        chosenAnswerIndex = bIndex;
        waitingForAnswer = false;

        if (showDebugLog) Debug.Log($"第{qIndex + 1}题，选择了选项{bIndex + 1}。");
    }

    void ShowQuestionButtons(int qIndex, int count)
    {
        if (questionButtons[qIndex] == null) return;

        for (int b = 0; b < questionButtons[qIndex].Length; b++)
        {
            if (questionButtons[qIndex][b] == null) continue;

            bool show = (b < count);
            questionButtons[qIndex][b].gameObject.SetActive(show);
        }
    }

    void HideQuestionButtons(int qIndex)
    {
        if (questionButtons[qIndex] == null) return;

        for (int b = 0; b < questionButtons[qIndex].Length; b++)
        {
            if (questionButtons[qIndex][b] != null)
                questionButtons[qIndex][b].gameObject.SetActive(false);
        }
    }

    void HideAllQuestions()
    {
        for (int q = 0; q < 7; q++)
        {
            if (questionPrompts != null && questionPrompts[q] != null)
                questionPrompts[q].gameObject.SetActive(false);

            HideQuestionButtons(q);
        }
    }

    IEnumerator ShowAnswerWithImage(int qIndex, int bIndex, bool isCorrect)
    {
        HideImage1();

        if (isCorrect)
            ShowImage2();
        else
            ShowImage3();

        TextMeshProUGUI feedback = null;
        if (questionTexts[qIndex] != null && bIndex >= 0 && bIndex < questionTexts[qIndex].Length)
        {
            feedback = questionTexts[qIndex][bIndex];
        }

        if (feedback != null)
            feedback.gameObject.SetActive(true);

        yield return new WaitForSeconds(step2AnswerTextDisplayTime);

        if (feedback != null)
            feedback.gameObject.SetActive(false);

        HideImage2();
        HideImage3();
    }

    void HideAllQuestionTexts()
    {
        if (questionTexts == null) return;

        for (int q = 0; q < 7; q++)
        {
            if (questionTexts[q] == null) continue;

            for (int b = 0; b < questionTexts[q].Length; b++)
            {
                if (questionTexts[q][b] != null)
                    questionTexts[q][b].gameObject.SetActive(false);
            }
        }
    }

    // ==========================================
    // 图片控制
    // ==========================================
    void ShowImage1()
    {
        if (step2Image1 != null) step2Image1.gameObject.SetActive(true);
    }

    void HideImage1()
    {
        if (step2Image1 != null) step2Image1.gameObject.SetActive(false);
    }

    void ShowImage2()
    {
        if (step2Image2 != null) step2Image2.gameObject.SetActive(true);
    }

    void HideImage2()
    {
        if (step2Image2 != null) step2Image2.gameObject.SetActive(false);
    }

    void ShowImage3()
    {
        if (step2Image3 != null) step2Image3.gameObject.SetActive(true);
    }

    void HideImage3()
    {
        if (step2Image3 != null) step2Image3.gameObject.SetActive(false);
    }

    // ==========================================
    // 【第3步】暖光过渡 → 暖光到一半时切换场景
    // ==========================================
    IEnumerator StartStep3WarmLightTransition()
    {
        if (showDebugLog) Debug.Log("进入第3步：暖光过渡，准备切换场景。");

        HideAllButtons();
        HideAllQuestions();
        HideAllQuestionTexts();
        HideImage1();
        HideImage2();
        HideImage3();

        if (finalWarmLightFilter == null)
        {
            if (showDebugLog) Debug.LogWarning("暖光滤镜未拖入，直接切换场景。");
            LoadNextScene();
            yield break;
        }

        finalWarmLightFilter.gameObject.SetActive(true);
        Color filterColor = finalWarmLightFilter.color;
        filterColor.a = 0f;
        finalWarmLightFilter.color = filterColor;

        Color textColor = Color.white;
        if (finalTransitionText != null)
        {
            finalTransitionText.gameObject.SetActive(true);
            textColor = finalTransitionText.color;
            textColor.a = 0f;
            finalTransitionText.color = textColor;
        }

        bool sceneLoaded = false;
        float halfPoint = finalWarmFadeInTime * 0.5f;
        float t = 0f;

        while (t < finalWarmFadeInTime)
        {
            t += Time.deltaTime;
            float progress = t / finalWarmFadeInTime;

            filterColor.a = Mathf.Lerp(0f, finalMaxWarmAlpha, progress);
            finalWarmLightFilter.color = filterColor;

            if (finalTransitionText != null)
            {
                textColor.a = Mathf.Lerp(0f, 1f, progress);
                finalTransitionText.color = textColor;
            }

            if (!sceneLoaded && t >= halfPoint)
            {
                sceneLoaded = true;
                if (showDebugLog) Debug.Log("暖光到达一半，开始切换场景。");
                LoadNextScene();
            }

            yield return null;
        }

        filterColor.a = finalMaxWarmAlpha;
        finalWarmLightFilter.color = filterColor;
        if (finalTransitionText != null)
        {
            textColor.a = 1f;
            finalTransitionText.color = textColor;
        }

        if (finalWarmHoldTime > 0f)
            yield return new WaitForSeconds(finalWarmHoldTime);

        if (finalFadeOutBeforeLoad)
        {
            t = 0f;
            while (t < finalWarmFadeOutTime)
            {
                t += Time.deltaTime;
                filterColor.a = Mathf.Lerp(finalMaxWarmAlpha, 0f, t / finalWarmFadeOutTime);
                finalWarmLightFilter.color = filterColor;
                yield return null;
            }
            filterColor.a = 0f;
            finalWarmLightFilter.color = filterColor;
        }
    }

    // ==========================================
    // 加载下一个场景
    // ==========================================
    void LoadNextScene()
    {
        // 合并工程：双路径相互独立，仅剖腹产分支继续进手术室；顺产是独立完整路径（路径选择由 UI界面 按钮决定）
        string cur = SceneManager.GetActiveScene().name;
        string mappedNext = cur == "刨腹产" ? "手术室" : null;
        if (!string.IsNullOrEmpty(mappedNext))
        {
            if (showDebugLog) Debug.Log("加载场景（幕内顺序）：" + mappedNext);
            SceneManager.LoadScene(mappedNext);
            return;
        }

        // 优先使用场景名称
        if (useSceneName && !string.IsNullOrEmpty(nextSceneName))
        {
            string target = nextSceneName;
            if (target.StartsWith("Scenes/")) target = System.IO.Path.GetFileNameWithoutExtension(target);
            if (showDebugLog) Debug.Log($"加载场景（名称）：{target}");
            SceneManager.LoadScene(target);
            return;
        }

        // 使用 buildIndex
        int targetIndex = nextSceneIndex;

        if (targetIndex < 0)
        {
            targetIndex = SceneManager.GetActiveScene().buildIndex + 1;
        }

        if (targetIndex < SceneManager.sceneCountInBuildSettings)
        {
            if (showDebugLog) Debug.Log($"加载场景（buildIndex）：{targetIndex}");
            SceneManager.LoadScene(targetIndex);
        }
        else
        {
            if (showDebugLog)
                Debug.LogWarning($"场景索引 {targetIndex} 超出范围（共 {SceneManager.sceneCountInBuildSettings} 个场景），无法加载。");
        }
    }

    // ==========================================
    // 工具方法
    // ==========================================
    TextMeshProUGUI GetNextTextByIndex(int index)
    {
        switch (index)
        {
            case 0: return nextTextA;
            case 1: return nextTextB;
            case 2: return nextTextC;
            default: return null;
        }
    }

    void HideAllSeqTexts()
    {
        if (seqText1 != null) seqText1.gameObject.SetActive(false);
        if (seqText2 != null) seqText2.gameObject.SetActive(false);
        if (seqText3 != null) seqText3.gameObject.SetActive(false);
        if (seqText4 != null) seqText4.gameObject.SetActive(false);
        if (seqText5 != null) seqText5.gameObject.SetActive(false);
    }

    void HideAllSeqTextsExcept(TextMeshProUGUI keep)
    {
        if (seqText1 != null && seqText1 != keep) seqText1.gameObject.SetActive(false);
        if (seqText2 != null && seqText2 != keep) seqText2.gameObject.SetActive(false);
        if (seqText3 != null && seqText3 != keep) seqText3.gameObject.SetActive(false);
        if (seqText4 != null && seqText4 != keep) seqText4.gameObject.SetActive(false);
        if (seqText5 != null && seqText5 != keep) seqText5.gameObject.SetActive(false);
    }

    void HideAllNextTexts()
    {
        if (nextTextA != null) nextTextA.gameObject.SetActive(false);
        if (nextTextB != null) nextTextB.gameObject.SetActive(false);
        if (nextTextC != null) nextTextC.gameObject.SetActive(false);
    }

    void HideAllNextTextsExcept(TextMeshProUGUI keep)
    {
        if (nextTextA != null && nextTextA != keep) nextTextA.gameObject.SetActive(false);
        if (nextTextB != null && nextTextB != keep) nextTextB.gameObject.SetActive(false);
        if (nextTextC != null && nextTextC != keep) nextTextC.gameObject.SetActive(false);
    }

    void HideAllButtons()
    {
        if (resetAllButton != null) resetAllButton.gameObject.SetActive(false);
        if (showNextATextButton != null) showNextATextButton.gameObject.SetActive(false);
        if (showNextBTextButton != null) showNextBTextButton.gameObject.SetActive(false);
        if (showNextCTextButton != null) showNextCTextButton.gameObject.SetActive(false);
    }

    public void ResetAll()
    {
        StopAllCoroutines();
        ForceHideAllUI();
        StartCoroutine(PrologueSequence());
    }
}