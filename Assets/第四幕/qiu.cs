using UnityEngine;
using UnityEngine.UI;

public enum FadeFinishAction
{
    DisableComponent,      // 只禁用本脚本
    DeactivateGameObject,  // 禁用整个 GameObject
    DoNothing              // 停在透明状态，什么都不做
}

[RequireComponent(typeof(Image))]
public class ImageOrbFade : MonoBehaviour
{
    [Header("Timing (seconds)")]
    [Tooltip("首次淡入前的等待时间；这段时间内物体保持透明")]
    [SerializeField] private float startDelay = 0f;

    [Tooltip("淡入（透明 → 不透明）时长。0 表示直接出现")]
    [SerializeField] private float fadeInDuration = 0.5f;

    [Tooltip("完全显示后的保持时长")]
    [SerializeField] private float holdDuration = 0f;

    [Tooltip("淡出（不透明 → 透明）时长。0 表示瞬间消失")]
    [SerializeField] private float fadeOutDuration = 1f;

    [Tooltip("一轮结束到下一轮淡入开始之间的间隔；0 表示无缝衔接")]
    [SerializeField] private float repeatInterval = 0f;

    [Tooltip("重复次数。<=0 表示无限循环")]
    [SerializeField] private int repeatCount = 1;

    [Header("Curves")]
    [Tooltip("淡入曲线：横轴 0→1 进度，纵轴透明度倍率 0→1")]
    [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("淡出曲线：横轴 0→1 进度，纵轴透明度倍率 1→0")]
    [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Misc")]
    [Tooltip("勾选后不受 Time.timeScale 影响（UI 推荐勾选）")]
    [SerializeField] private bool useUnscaledTime = true;

    [Tooltip("全部播放结束后的处理方式")]
    [SerializeField] private FadeFinishAction onFinish = FadeFinishAction.DisableComponent;

    private enum Phase { Idle, StartDelay, FadeIn, Hold, FadeOut, Interval, Done }

    private Image img;
    private Color baseColor;
    private Phase phase = Phase.Idle;
    private float timer;
    private int completedLoops;

    private void Awake()
    {
        img = GetComponent<Image>();
        baseColor = img.color;
    }

    private void OnEnable()
    {
        Play();
    }

    /// <summary>从头开始播放（重置次数与透明度）。</summary>
    public void Play()
    {
        completedLoops = 0;
        timer = 0f;
        img.enabled = true;
        SetAlpha(0f);                                      // 从全透明开始
        phase = startDelay > 0f ? Phase.StartDelay : Phase.FadeIn;
    }

    private void Update()
    {
        if (phase == Phase.Idle || phase == Phase.Done) return;

        timer += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        switch (phase)
        {
            case Phase.StartDelay:
                if (timer >= startDelay) { timer = 0f; phase = Phase.FadeIn; }
                break;

            case Phase.FadeIn:
            {
                float p = Progress(timer, fadeInDuration);
                SetAlpha(fadeInCurve.Evaluate(p));
                if (p >= 1f) { timer = 0f; SetAlpha(1f); phase = Phase.Hold; }
                break;
            }

            case Phase.Hold:
                if (timer >= holdDuration) { timer = 0f; phase = Phase.FadeOut; }
                break;

            case Phase.FadeOut:
            {
                float p = Progress(timer, fadeOutDuration);
                SetAlpha(fadeOutCurve.Evaluate(p));
                if (p >= 1f) OnLoopComplete();
                break;
            }

            case Phase.Interval:
                if (timer >= repeatInterval) { timer = 0f; phase = Phase.FadeIn; }
                break;
        }
    }

    private void OnLoopComplete()
    {
        completedLoops++;
        timer = 0f;
        SetAlpha(0f);                                      // 结束后一定是全透明

        bool hasNext = repeatCount <= 0 || completedLoops < repeatCount;
        if (!hasNext) { Finish(); return; }

        phase = repeatInterval > 0f ? Phase.Interval : Phase.FadeIn;
    }

    private void Finish()
    {
        phase = Phase.Done;
        SetAlpha(0f);

        switch (onFinish)
        {
            case FadeFinishAction.DisableComponent:     enabled = false; break;
            case FadeFinishAction.DeactivateGameObject: gameObject.SetActive(false); break;
            case FadeFinishAction.DoNothing:            break;
        }
    }

    private void SetAlpha(float multiplier)
    {
        Color c = baseColor;
        c.a = baseColor.a * multiplier;
        img.color = c;
    }

    private static float Progress(float elapsed, float duration)
    {
        if (duration <= 0f) return 1f;                     // 防 0 / 0 = NaN
        return Mathf.Clamp01(elapsed / duration);
    }
}