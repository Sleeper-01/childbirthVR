using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro; // 新增：为了使用 TextMeshProUGUI
using System.Collections;

[RequireComponent(typeof(Scrollbar))]
public class HoldToFillScrollbar : MonoBehaviour
{
    [Header("出现")]
    [Tooltip("从脚本开始到滚动条瞬间出现之间的等待时间（秒）")]
    public float appearDelay = 1f;

    [Tooltip("出现时滚动条的初始值（0 = 空，1 = 满）")]
    [Range(0f, 1f)]
    public float appearStartValue = 0f;

    [Header("开始时间")]
    [Tooltip("出现之后，等多久才允许开始蓄力")]
    public float startDelay = 1f;

    [Header("速度（每秒变化量 0~1）")]
    public float forwardSpeed = 0.6f;
    public float backwardSpeed = 0.4f;

    [Header("满值后")]
    [Tooltip("满值后到下一次重新出现之间的间隔")]
    public float restartDelay = 1f;

    public bool loop = true;

    [Tooltip("满值后是否隐藏滚动条")]
    public bool hideWhenFull = true;

    [Header("次数")]
    [Tooltip("蓄满多少次后结束；<=0 表示无限（配合 loop）")]
    public int requiredSuccessCount = 3;

    [Tooltip("全部完成后是否禁用整个 GameObject")]
    public bool deactivateOnFinish = false;

    [Header("完成后回调")]
    public UnityEvent onAllCompleted;

    [Header("输入")]
    public bool requirePointerOver = false;

    // ==================== 新增：超时监控 ====================
    [Header("=== 新增：超时设置 ===")]
    [Tooltip("单次蓄力允许的最长时间（秒），超时就会弹出提示")]
    public float maxActiveTime = 5f;
    [Tooltip("超时提示文本（会自动显示2秒后隐藏）")]
    public TextMeshProUGUI timeoutText;

    private Scrollbar bar;
    private CanvasGroup canvasGroup;
    private float timer;
    private int successCount;
    private State state;

    // 新增：用于记录当前 Active 状态下的实际蓄力时间
    private float activeTimer = 0f;
    // 新增：标记本轮是否已经触发过超时（防止一直重复弹）
    private bool hasTimedOutThisCycle = false;

    private enum State { AppearDelay, Waiting, Active, Hidden, Finished }

    private void Awake()
    {
        bar = GetComponent<Scrollbar>();
        bar.direction = Scrollbar.Direction.LeftToRight;
        bar.numberOfSteps = 0;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnEnable() 
    {
        // 当主管理器把这个物体激活时，重新开始流程
        Begin();
    }

    public void Begin()
    {
        timer = 0f;
        activeTimer = 0f;              
        hasTimedOutThisCycle = false;  
        successCount = 0;

        SetVisible(false);
        bar.value = 0f;

        // 如果有超时文本，开始时隐藏它
        if (timeoutText != null) timeoutText.gameObject.SetActive(false);

        state = State.AppearDelay;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        switch (state)
        {
            case State.AppearDelay:
                timer += dt;
                if (timer >= appearDelay)
                {
                    timer = 0f;
                    SetVisible(true);
                    bar.value = appearStartValue;
                    state = State.Waiting;
                }
                break;

            case State.Waiting:
                timer += dt;
                if (timer >= startDelay) { timer = 0f; state = State.Active; }
                break;

            case State.Active:
                // 处理蓄力或倒退
                if (IsPressing())
                    bar.value += forwardSpeed * dt;
                else
                    bar.value -= backwardSpeed * dt;

                // 限制滑动条的值在 0-1 之间
                bar.value = Mathf.Clamp01(bar.value);

                // 判断是否成功（到达顶部）
                if (bar.value >= 1f)
                {
                    bar.value = 1f;
                    activeTimer = 0f; // 成功到达顶部！重置超时计时器
                    hasTimedOutThisCycle = false;
                    OnSuccess();
                    break; 
                }

                // ==================== 新增：超时检测逻辑 ====================
                activeTimer += dt;

                // 如果累积时间超过了设定值，且本轮还没弹过提示
                if (activeTimer >= maxActiveTime && !hasTimedOutThisCycle)
                {
                    hasTimedOutThisCycle = true; // 标记已经弹过，防止重复触发
                    StartCoroutine(ShowTimeoutText());
                }
                break;

            case State.Hidden:
                timer += dt;
                if (timer >= restartDelay)
                {
                    if (requiredSuccessCount > 0 && successCount >= requiredSuccessCount)
                    {
                        Finish();
                        break;
                    }
                    if (loop)
                        RestartCycle();
                }
                break;

            case State.Finished:
                break;
        }
    }

    private void OnSuccess()
    {
        successCount++;
        timer = 0f;

        if (hideWhenFull) SetVisible(false);

        if (requiredSuccessCount > 0 && successCount >= requiredSuccessCount)
        {
            Finish();
            return;
        }

        state = State.Hidden;
    }

    private void RestartCycle()
    {
        timer = 0f;
        activeTimer = 0f;              // 新一轮，重置超时时间
        hasTimedOutThisCycle = false;  // 新一轮，重置超时标记

        bar.value = 0f;
        SetVisible(false);
        state = State.AppearDelay;
    }

    private void Finish()
    {
        state = State.Finished;
        bar.value = 0f;
        SetVisible(false);
        if (timeoutText != null) timeoutText.gameObject.SetActive(false);

        onAllCompleted?.Invoke();

        if (deactivateOnFinish)
            gameObject.SetActive(false);
        else
            enabled = false;
    }

    private bool IsPressing()
    {
        if (!Input.GetMouseButton(0) && !ChuJian.Merge.VRPointerVisual.IsAnyTriggerHeld) return false;
        if (!requirePointerOver) return true;

        RectTransform rt = GetComponent<RectTransform>();
        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera : null;

        return RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, cam);
    }

    private void SetVisible(bool visible)
    {
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
    }

    // ==================== 新增：超时提示协程 ====================
    private IEnumerator ShowTimeoutText()
    {
        if (timeoutText == null) yield break;

        Debug.Log("滚动条超时！未在规定时间内到达顶部。");

        timeoutText.gameObject.SetActive(true);  // 显示文本
        yield return new WaitForSeconds(2f);     // 持续2秒
        timeoutText.gameObject.SetActive(false); // 自动隐藏
    }
}