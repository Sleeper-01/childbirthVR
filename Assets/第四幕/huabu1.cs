using UnityEngine;
using UnityEngine.UI; 
using TMPro;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

public class BreathingGameManager : MonoBehaviour
{
    [Header("=== 界面拖拽区 ===")]
    public TextMeshProUGUI titleText;        
    public GameObject breathingPanel;        
    public Transform lightBall;              
    
    [Header("=== 呼吸提示文本 ===")]
    public TextMeshProUGUI inhaleText;       
    public TextMeshProUGUI exhaleText;       
    
    [Header("=== 按钮区 ===")]
    public Button chestButton;               
    public Button fastButton;                
    public Button puffButton;                

    [Header("=== 滑动条与确认阶段 ===")]
    public GameObject progressSliderObject;  
    public Slider progressSlider;            
    public TextMeshProUGUI confirmText;      
    public float requiredHoldTime = 2f;      

    [Header("=== 松开提示文本 ===")]
    public TextMeshProUGUI releaseAText;     
    public TextMeshProUGUI releaseSText;     
    public TextMeshProUGUI releaseBothText;  

    [Header("=== 滑动条相关文本 ===")]
    public TextMeshProUGUI startLoadingText; 
    public TextMeshProUGUI timeoutText;      

    [Header("=== 第4步：选择阶段 ===")]
    public TextMeshProUGUI step4PromptText;  
    public Button step4OptionA;              
    public Button step4OptionB;              
    public Button step4OptionC;              
    
    [Header("=== 第4步：结果文本 ===")]
    public TextMeshProUGUI resultTextA;      
    public TextMeshProUGUI resultTextB;      
    public TextMeshProUGUI resultTextC;      

    [Header("=== 第5步：快速哈气 ===")]
    public TextMeshProUGUI step5IntroText;   
    public float step5IntroDuration = 2f;    
    public float step5PuffsPerSecond = 4f;   
    public int step5TotalPuffs = 10;         

    [Header("=== 第5步：结尾暖光（滤镜模式） ===")]
    public Image warmLightFilter;            
    [Range(0f, 1f)]
    public float maxWarmAlpha = 0.6f;        
    
    [Header("=== 暖光时间控制 ===")]
    public float warmLightFadeInTime = 2f;   
    public float warmLightHoldTime = 2f;     
    public bool fadeOutAtEnd = true;         

    [Header("=== 结尾文本 ===")]
    public TextMeshProUGUI endingText;       
    public float endingTextDuration = 3f;    

    [Header("=== 暖光与文本同步设置 ===")]
    public bool syncWarmLightAndText = true;

    [Header("=== 第6步：视频播放 ===")]
    public VideoPlayer videoPlayer;           
    public RawImage videoDisplay;             
    public Button replayButton;               
    public Button continueButton;             

    [Header("=== 第6步：5个顺序文本 ===")]
    public TextMeshProUGUI seqText1;          
    public TextMeshProUGUI seqText2;          
    public TextMeshProUGUI seqText3;          
    public TextMeshProUGUI seqText4;          
    public TextMeshProUGUI seqText5;          
    public float seqTextDisplayTime = 3f;     
    public float seqTextInterval = 0.5f;      

    // ==========================================
    // 【按钮区】结束按钮 + 重播文本按钮
    // ==========================================
    [Header("=== 结束按钮 ===")]
    public Button endButton;                  
    
    [Header("=== 重播文本按钮 ===")]
    public Button replayTextButton;           

    [Header("=== 参数设置 ===")]
    public float fadeInTime = 1.5f;
    public float holdTime = 3f;
    public float fadeOutTime = 1.2f;
    public float maxScale = 2.0f;
    public float minScale = 1.0f;
    
    [Header("=== 呼吸次数限制 ===")]
    public int totalBreathingCycles = 5;     

    private bool isPlayingPrologue = true;
    private bool isBreathing = false;
    private bool hasClickedContinue = false;
    // 【新增】用于控制“重播文本”的标记
    private bool hasClickedReplayText = false;

    void Start()
    {
        ForceHideAllUI();
        
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        if (chestButton != null) chestButton.onClick.AddListener(() => StartBreathing(1)); 
        if (fastButton != null) fastButton.onClick.AddListener(() => StartBreathing(2));  
        if (puffButton != null) puffButton.onClick.AddListener(() => StartBreathing(3));  

        if (step4OptionA != null) step4OptionA.onClick.AddListener(() => OnStep4ChoiceSelected(1));
        if (step4OptionB != null) step4OptionB.onClick.AddListener(() => OnStep4ChoiceSelected(2));
        if (step4OptionC != null) step4OptionC.onClick.AddListener(() => OnStep4ChoiceSelected(3));

        if (replayButton != null) replayButton.onClick.AddListener(ReplayVideo);
        if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
        if (endButton != null) endButton.onClick.AddListener(OnEndClicked);
        // 【新增】绑定重播文本按钮
        if (replayTextButton != null) replayTextButton.onClick.AddListener(OnReplayTextClicked);

        StartCoroutine(PrologueSequence());
    }

    void ForceHideAllUI()
    {
        if (inhaleText != null) inhaleText.gameObject.SetActive(false);
        if (exhaleText != null) exhaleText.gameObject.SetActive(false);
        if (breathingPanel != null) breathingPanel.SetActive(false);
        if (progressSliderObject != null) progressSliderObject.SetActive(false);
        if (confirmText != null) confirmText.gameObject.SetActive(false);
        if (releaseAText != null) releaseAText.gameObject.SetActive(false);
        if (releaseSText != null) releaseSText.gameObject.SetActive(false);
        if (releaseBothText != null) releaseBothText.gameObject.SetActive(false);
        if (startLoadingText != null) startLoadingText.gameObject.SetActive(false);
        if (timeoutText != null) timeoutText.gameObject.SetActive(false);

        if (step4PromptText != null) step4PromptText.gameObject.SetActive(false);
        if (step4OptionA != null) step4OptionA.gameObject.SetActive(false);
        if (step4OptionB != null) step4OptionB.gameObject.SetActive(false);
        if (step4OptionC != null) step4OptionC.gameObject.SetActive(false);
        if (resultTextA != null) resultTextA.gameObject.SetActive(false);
        if (resultTextB != null) resultTextB.gameObject.SetActive(false);
        if (resultTextC != null) resultTextC.gameObject.SetActive(false);

        if (step5IntroText != null) step5IntroText.gameObject.SetActive(false);

        if (warmLightFilter != null)
        {
            warmLightFilter.gameObject.SetActive(false);
            Color warmColor = warmLightFilter.color;
            warmColor.a = 0f; 
            warmLightFilter.color = warmColor;
        }
        if (endingText != null) endingText.gameObject.SetActive(false);

        if (videoDisplay != null) videoDisplay.gameObject.SetActive(false);
        if (replayButton != null) replayButton.gameObject.SetActive(false);
        if (continueButton != null) continueButton.gameObject.SetActive(false);
        if (endButton != null) endButton.gameObject.SetActive(false);
        // 【新增】隐藏重播文本按钮
        if (replayTextButton != null) replayTextButton.gameObject.SetActive(false);

        if (seqText1 != null) seqText1.gameObject.SetActive(false);
        if (seqText2 != null) seqText2.gameObject.SetActive(false);
        if (seqText3 != null) seqText3.gameObject.SetActive(false);
        if (seqText4 != null) seqText4.gameObject.SetActive(false);
        if (seqText5 != null) seqText5.gameObject.SetActive(false);
    }

    void Update()
    {
        if (isPlayingPrologue && Input.GetKeyDown(KeyCode.Space))
        {
            StopAllCoroutines();
            EndPrologue();
        }
    }

    IEnumerator PrologueSequence()
    {
        yield return FadeText(0, 1, fadeInTime);
        
        float timer = 0;
        while (timer < holdTime)
        {
            timer += Time.deltaTime;
            float scale = 1 + Mathf.Sin(Time.time * 1.2f) * 0.02f;
            if(titleText != null) titleText.transform.localScale = Vector3.one * scale;
            yield return null;
        }
        
        yield return FadeText(1, 0, fadeOutTime);
        EndPrologue();
    }

    IEnumerator FadeText(float startAlpha, float endAlpha, float duration)
    {
        float t = 0;
        if (titleText == null) yield break;
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

    void EndPrologue()
    {
        isPlayingPrologue = false;
        if (titleText != null) titleText.gameObject.SetActive(false);
        if (breathingPanel != null) breathingPanel.SetActive(true);
        
        if (chestButton != null) chestButton.gameObject.SetActive(true);
        if (fastButton != null) fastButton.gameObject.SetActive(true);
        if (puffButton != null) puffButton.gameObject.SetActive(true);
        
        if (lightBall != null) lightBall.gameObject.SetActive(false);
        if (inhaleText != null) inhaleText.gameObject.SetActive(false);
        if (exhaleText != null) exhaleText.gameObject.SetActive(false);
        
        if (progressSliderObject != null) progressSliderObject.SetActive(false);
        if (confirmText != null) confirmText.gameObject.SetActive(false);
        if (releaseAText != null) releaseAText.gameObject.SetActive(false);
        if (releaseSText != null) releaseSText.gameObject.SetActive(false);
        if (releaseBothText != null) releaseBothText.gameObject.SetActive(false);
        if (startLoadingText != null) startLoadingText.gameObject.SetActive(false);
        if (timeoutText != null) timeoutText.gameObject.SetActive(false);
    }

    public void StartBreathing(int type)
    {
        if (isBreathing) return;
        
        if (chestButton != null) chestButton.gameObject.SetActive(false);
        if (fastButton != null) fastButton.gameObject.SetActive(false);
        if (puffButton != null) puffButton.gameObject.SetActive(false);
        
        if (lightBall != null) 
        {
            lightBall.gameObject.SetActive(true);
            lightBall.localScale = Vector3.one * minScale;
        }

        if (inhaleText != null) inhaleText.gameObject.SetActive(true);
        if (exhaleText != null) exhaleText.gameObject.SetActive(false);

        isBreathing = true;
        StartCoroutine(BreathingRoutine(type));
    }

    private IEnumerator BreathingRoutine(int type)
    {
        float inhaleTime = 4f; 
        float exhaleTime = 4f; 

        if (type == 2) { inhaleTime = 1.5f; exhaleTime = 1.5f; } 
        else if (type == 3) { inhaleTime = 2f; exhaleTime = 1f; }     

        int currentCycle = 0;

        while (isBreathing && currentCycle < totalBreathingCycles)
        {
            if (inhaleText != null) inhaleText.gameObject.SetActive(true);
            if (exhaleText != null) exhaleText.gameObject.SetActive(false);
            
            yield return StartCoroutine(ScaleLightBall(minScale, maxScale, inhaleTime));
            yield return new WaitForSeconds(0.2f);

            if (inhaleText != null) inhaleText.gameObject.SetActive(false);
            if (exhaleText != null) exhaleText.gameObject.SetActive(true);
            
            yield return StartCoroutine(ScaleLightBall(maxScale, minScale, exhaleTime));
            yield return new WaitForSeconds(0.2f);

            currentCycle++;
        }

        isBreathing = false;
        
        if (lightBall != null) lightBall.gameObject.SetActive(false);
        if (inhaleText != null) inhaleText.gameObject.SetActive(false);
        if (exhaleText != null) exhaleText.gameObject.SetActive(false);
        
        yield return StartCoroutine(WaitForPlayerConfirmation());
    }

    private IEnumerator WaitForPlayerConfirmation()
    {
        if (confirmText != null) confirmText.gameObject.SetActive(true);
        if (releaseAText != null) releaseAText.gameObject.SetActive(false);
        if (releaseSText != null) releaseSText.gameObject.SetActive(false);
        if (releaseBothText != null) releaseBothText.gameObject.SetActive(false);

        float holdTimer = 0f;
        bool isConfirmed = false;

        while (!isConfirmed)
        {
            // VR模式：右侧握键替代A键，左侧握键替代S键；桌面模式键盘照常
            bool isAHeld = Input.GetKey(KeyCode.A) || ChuJian.Merge.VRPointerVisual.IsRGripHeld;
            bool isSHeld = Input.GetKey(KeyCode.S) || ChuJian.Merge.VRPointerVisual.IsLGripHeld;

            if (isAHeld && isSHeld)
            {
                holdTimer += Time.deltaTime;
                
                if (releaseAText != null) releaseAText.gameObject.SetActive(false);
                if (releaseSText != null) releaseSText.gameObject.SetActive(false);
                if (releaseBothText != null) releaseBothText.gameObject.SetActive(false);
                if (confirmText != null) confirmText.gameObject.SetActive(true);

                if (holdTimer >= requiredHoldTime)
                {
                    isConfirmed = true;
                    if (confirmText != null) confirmText.gameObject.SetActive(false);
                }
            }
            else if (!isAHeld && isSHeld)
            {
                holdTimer = 0f;
                if (confirmText != null) confirmText.gameObject.SetActive(false);
                if (releaseAText != null) releaseAText.gameObject.SetActive(true);
                if (releaseSText != null) releaseSText.gameObject.SetActive(false);
                if (releaseBothText != null) releaseBothText.gameObject.SetActive(false);
            }
            else if (isAHeld && !isSHeld)
            {
                holdTimer = 0f;
                if (confirmText != null) confirmText.gameObject.SetActive(false);
                if (releaseAText != null) releaseAText.gameObject.SetActive(false);
                if (releaseSText != null) releaseSText.gameObject.SetActive(true);
                if (releaseBothText != null) releaseBothText.gameObject.SetActive(false);
            }
            else
            {
                holdTimer = 0f;
                if (confirmText != null) confirmText.gameObject.SetActive(false);
                if (releaseAText != null) releaseAText.gameObject.SetActive(false);
                if (releaseSText != null) releaseSText.gameObject.SetActive(false);
                if (releaseBothText != null) releaseBothText.gameObject.SetActive(true);
            }

            yield return null;
        }

        if (startLoadingText != null) startLoadingText.gameObject.SetActive(true); 
        yield return new WaitForSeconds(2f); 
        if (startLoadingText != null) startLoadingText.gameObject.SetActive(false); 

        if (progressSliderObject != null)
        {
            progressSliderObject.SetActive(true);
            yield return new WaitForSeconds(2f); 
            
            StartCoroutine(MonitorSliderLifecycle()); 
        }
    }

    private IEnumerator MonitorSliderLifecycle()
    {
        HoldToFillScrollbar sliderScript = progressSliderObject.GetComponent<HoldToFillScrollbar>();
        
        if (sliderScript == null)
        {
            Debug.LogError("警告：你的滚动条物体上没有挂载 HoldToFillScrollbar 脚本！");
        }

        float safetyTimer = 0f;
        float safetyTimeout = 60f; 

        while (sliderScript != null && sliderScript.enabled == true)
        {
            safetyTimer += Time.deltaTime;
            
            if (safetyTimer >= safetyTimeout)
            {
                Debug.Log("安全超时：强制进入下一步。");
                break;
            }
            
            yield return null;
        }

        Debug.Log("滚动条结束！准备进入第4步。");

        if (progressSliderObject != null) progressSliderObject.SetActive(false);
        yield return StartCoroutine(StartStep4Choice());
    }

    private IEnumerator StartStep4Choice()
    {
        if (step4PromptText != null) step4PromptText.gameObject.SetActive(true);
        if (step4OptionA != null) step4OptionA.gameObject.SetActive(true);
        if (step4OptionB != null) step4OptionB.gameObject.SetActive(true);
        if (step4OptionC != null) step4OptionC.gameObject.SetActive(true);

        if (resultTextA != null) resultTextA.gameObject.SetActive(false);
        if (resultTextB != null) resultTextB.gameObject.SetActive(false);
        if (resultTextC != null) resultTextC.gameObject.SetActive(false);

        while (true)
        {
            if (resultTextA != null && resultTextA.gameObject.activeSelf) break;
            if (resultTextB != null && resultTextB.gameObject.activeSelf) break;
            if (resultTextC != null && resultTextC.gameObject.activeSelf) break;
            yield return null;
        }

        yield return new WaitForSeconds(3f);
        
        if (resultTextA != null) resultTextA.gameObject.SetActive(false);
        if (resultTextB != null) resultTextB.gameObject.SetActive(false);
        if (resultTextC != null) resultTextC.gameObject.SetActive(false);
        
        Debug.Log("第4步结束，进入第5步：快速哈气！");

        yield return StartCoroutine(StartStep5FastPuffing());
    }

    private IEnumerator StartStep5FastPuffing()
    {
        if (step5IntroText != null) step5IntroText.gameObject.SetActive(true);
        yield return new WaitForSeconds(step5IntroDuration);
        if (step5IntroText != null) step5IntroText.gameObject.SetActive(false);

        if (lightBall != null)
        {
            lightBall.gameObject.SetActive(true);
            lightBall.localScale = Vector3.one * minScale;
        }

        float timePerPuff = 1f / step5PuffsPerSecond;

        for (int i = 0; i < step5TotalPuffs; i++)
        {
            yield return StartCoroutine(ScaleLightBall(minScale, maxScale, timePerPuff * 0.5f));
            yield return StartCoroutine(ScaleLightBall(maxScale, minScale, timePerPuff * 0.5f));
        }

        if (lightBall != null) lightBall.gameObject.SetActive(false);

        Debug.Log("快速哈气结束！开始暖光结尾...");

        yield return StartCoroutine(StartWarmLightEnding());
    }

    private IEnumerator StartWarmLightEnding()
    {
        if (warmLightFilter == null)
        {
            Debug.LogError("暖光滤镜未拖入！");
            yield break;
        }

        warmLightFilter.gameObject.SetActive(true);
        Color filterColor = warmLightFilter.color;
        filterColor.a = 0f; 
        warmLightFilter.color = filterColor;

        Color textColor = Color.white;
        if (endingText != null)
        {
            endingText.gameObject.SetActive(true);
            textColor = endingText.color;
            textColor.a = 0f; 
            endingText.color = textColor;
        }

        float t = 0f;
        while (t < warmLightFadeInTime)
        {
            t += Time.deltaTime;
            float progress = t / warmLightFadeInTime;

            filterColor.a = Mathf.Lerp(0f, maxWarmAlpha, progress);
            warmLightFilter.color = filterColor;

            if (syncWarmLightAndText && endingText != null)
            {
                textColor.a = Mathf.Lerp(0f, 1f, progress);
                endingText.color = textColor;
            }

            yield return null;
        }
        
        filterColor.a = maxWarmAlpha;
        warmLightFilter.color = filterColor;
        if (syncWarmLightAndText && endingText != null)
        {
            textColor.a = 1f;
            endingText.color = textColor;
        }

        yield return new WaitForSeconds(warmLightHoldTime);

        if (endingText != null)
        {
            endingText.gameObject.SetActive(false);
        }

        if (fadeOutAtEnd)
        {
            t = 0f;
            while (t < warmLightFadeInTime)
            {
                t += Time.deltaTime;
                filterColor.a = Mathf.Lerp(maxWarmAlpha, 0f, t / warmLightFadeInTime);
                warmLightFilter.color = filterColor;
                yield return null;
            }
            filterColor.a = 0f;
            warmLightFilter.color = filterColor;
        }

        Debug.Log("第5步完全结束！进入第6步：视频播放。");

        yield return StartCoroutine(StartStep6Video());
    }

    // ==========================================
    // 【核心】第6步：视频 + 继续按钮 + 5个顺序文本 + 重播文本
    // ==========================================
    private IEnumerator StartStep6Video()
    {
        hasClickedContinue = false;
        hasClickedReplayText = false;

        if (videoDisplay != null) videoDisplay.gameObject.SetActive(true);
        if (replayButton != null) replayButton.gameObject.SetActive(false);
        if (continueButton != null) continueButton.gameObject.SetActive(false);
        if (endButton != null) endButton.gameObject.SetActive(false);
        // 【新增】隐藏重播文本按钮
        if (replayTextButton != null) replayTextButton.gameObject.SetActive(false);

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.time = 0;

            videoPlayer.Prepare();
            while (!videoPlayer.isPrepared)
            {
                yield return null;
            }
            
            videoPlayer.Play();
            Debug.Log("视频开始播放...");
            
            while (videoPlayer.isPlaying)
            {
                yield return null;
            }
        }
        else
        {
            Debug.LogWarning("VideoPlayer 未拖入！跳过视频播放。");
        }

        if (videoDisplay != null) videoDisplay.gameObject.SetActive(false);
        if (replayButton != null) replayButton.gameObject.SetActive(true);
        if (continueButton != null) continueButton.gameObject.SetActive(true);

        Debug.Log("视频播放完毕。等待玩家点击继续...");

        while (!hasClickedContinue)
        {
            yield return null;
        }

        if (continueButton != null) continueButton.gameObject.SetActive(false);
        if (replayButton != null) replayButton.gameObject.SetActive(false);

        Debug.Log("玩家点击继续，开始播放5个文本。");

        // 【核心】5个文本播放 + 按钮逻辑
        yield return StartCoroutine(PlaySequentialTexts());
    }

    // ==========================================
    // 【修改】5个文本播放 + 按钮控制
    // ==========================================
    private IEnumerator PlaySequentialTexts()
    {
        // 每次开始播放文本前，确保重播按钮和结束按钮隐藏
        if (replayTextButton != null) replayTextButton.gameObject.SetActive(false);
        if (endButton != null) endButton.gameObject.SetActive(false);

        TextMeshProUGUI[] texts = { seqText1, seqText2, seqText3, seqText4, seqText5 };

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null) continue;

            texts[i].gameObject.SetActive(true);
            yield return new WaitForSeconds(seqTextDisplayTime);
            texts[i].gameObject.SetActive(false);
            yield return new WaitForSeconds(seqTextInterval);
        }

        // 5个文本播完后，显示“重播文本”按钮和“结束”按钮
        if (replayTextButton != null) replayTextButton.gameObject.SetActive(true);
        if (endButton != null) endButton.gameObject.SetActive(true);

        // 等待玩家点击“重播文本”按钮
        while (!hasClickedReplayText)
        {
            yield return null;
        }

        // 玩家点击了重播文本，重置标记，重新开始播放文本
        hasClickedReplayText = false;
        Debug.Log("玩家点击重播文本，重新开始播放5个文本。");

        // 递归调用（或进入一个新的循环）来重新播放文本
        yield return StartCoroutine(PlaySequentialTexts());
    }

    // ==========================================
    // 按钮事件
    // ==========================================
    private void ReplayVideo()
    {
        if (videoPlayer != null)
        {
            if (seqText1 != null) seqText1.gameObject.SetActive(false);
            if (seqText2 != null) seqText2.gameObject.SetActive(false);
            if (seqText3 != null) seqText3.gameObject.SetActive(false);
            if (seqText4 != null) seqText4.gameObject.SetActive(false);
            if (seqText5 != null) seqText5.gameObject.SetActive(false);

            if (replayButton != null) replayButton.gameObject.SetActive(false);
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            if (endButton != null) endButton.gameObject.SetActive(false);
            if (replayTextButton != null) replayTextButton.gameObject.SetActive(false);

            StartCoroutine(StartStep6Video()); 
        }
    }

    private void OnContinueClicked()
    {
        hasClickedContinue = true;
    }

    // 【新增】重播文本按钮点击事件
    private void OnReplayTextClicked()
    {
        hasClickedReplayText = true;
    }

    private void OnEndClicked()
    {
        Debug.Log("玩家点击结束按钮！准备返回上一个场景。");

        if (endButton != null) endButton.gameObject.SetActive(false);

        // 合并工程：双路径相互独立（顺产 / 剖腹产→手术室），回退只回选择界面
        string current = SceneManager.GetActiveScene().name;
        if (current == "UI界面")
        {
            Debug.LogWarning("已在选择界面，无法继续退回。");
            return;
        }
        string previous = current == "手术室" ? "刨腹产" : "UI界面";
        SceneManager.LoadScene(previous);
    }

    private void OnStep4ChoiceSelected(int choice)
    {
        if (step4PromptText != null) step4PromptText.gameObject.SetActive(false);
        if (step4OptionA != null) step4OptionA.gameObject.SetActive(false);
        if (step4OptionB != null) step4OptionB.gameObject.SetActive(false);
        if (step4OptionC != null) step4OptionC.gameObject.SetActive(false);

        if (choice == 1 && resultTextA != null) resultTextA.gameObject.SetActive(true);
        if (choice == 2 && resultTextB != null) resultTextB.gameObject.SetActive(true);
        if (choice == 3 && resultTextC != null) resultTextC.gameObject.SetActive(true);
    }

    private IEnumerator ScaleLightBall(float startScale, float endScale, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float currentScale = Mathf.Lerp(startScale, endScale, elapsedTime / duration);
            if(lightBall != null) lightBall.localScale = new Vector3(currentScale, currentScale, currentScale);
            yield return null;
        }
        if(lightBall != null) lightBall.localScale = new Vector3(endScale, endScale, endScale);
    }
}