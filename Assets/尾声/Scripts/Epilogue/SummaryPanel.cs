using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChuJianXinSheng.Epilogue
{
    /// <summary>
    /// 教学视频结束（用户关闭或自然播完）后弹出的总结画面：
    /// 面板占屏幕约三分之二，上半部分预留"分析表"展示区（内容后续接入），
    /// 下半部分展示总结语（顺产/剖腹产配合要点），底部提供关闭按钮；
    /// 关闭后触发 OnClosed，由 EpilogueDirector 播放鲜花飘落特效并恢复成就面板。
    /// </summary>
    public class SummaryPanel : MonoBehaviour
    {
        [Header("UI 引用（场景搭建时接线）")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panel;
        [SerializeField] private TMP_Text analysisText;    // 上半部分：分析表占位区
        [SerializeField] private TMP_Text summaryText;     // 下半部分：总结语
        [SerializeField] private Button closeButton;

        [Header("内容与行为")]
        [SerializeField, TextArea] private string analysisPlaceholder = "产后恢复分析表\n（此处待接入）";
        [SerializeField, TextArea] private string summaryContent =
            "总结\n顺产侧重「用力配合与呼吸」\n剖腹产侧重「术前沟通与放松配合」";

        public bool IsVisible { get; private set; }

        /// <summary>面板关闭时触发，供 EpilogueDirector 播放花瓣特效并恢复成就面板。</summary>
        public event System.Action OnClosed;

        private void Awake()
        {
            // 序幕风格：本组件自己把自己的面板刷成序幕深色圆角样式（不依赖任何全局扫描）
            if (panel != null) ChuJian.Merge.ActUIStyle.SkinDialogueTree(panel);
            if (analysisText != null) analysisText.color = ChuJian.Merge.ActUIStyle.Warn;
            SetInstant(false);
        }

        public void Show()
        {
            if (analysisText != null) analysisText.text = analysisPlaceholder;
            if (summaryText != null) summaryText.text = summaryContent;
            if (closeButton != null) closeButton.interactable = true;
            IsVisible = true;
            StartCoroutine(AnimateIn());
        }

        /// <summary>关闭按钮回调 / 外部关闭：触发 OnClosed。</summary>
        public void Hide()
        {
            if (!IsVisible) return;
            IsVisible = false;
            StopAllCoroutines();
            SetInstant(false);
            if (OnClosed != null) OnClosed();
        }

        /// <summary>静默隐藏（不触发 OnClosed），用于演示重置。</summary>
        public void HideSilent()
        {
            if (!IsVisible) return;
            IsVisible = false;
            StopAllCoroutines();
            SetInstant(false);
        }

        private void SetInstant(bool visible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }
            if (panel != null)
                panel.localScale = visible ? Vector3.one : Vector3.one * 0.6f;
        }

        private IEnumerator AnimateIn()
        {
            float t = 0f;
            const float showSeconds = 0.45f;
            Vector3 small = Vector3.one * 0.6f;
            while (t < showSeconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / showSeconds);
                if (canvasGroup != null) canvasGroup.alpha = k;
                if (panel != null) panel.localScale = Vector3.Lerp(small, Vector3.one, EaseOutBack(k));
                yield return null;
            }
            SetInstant(true);
        }

        private static float EaseOutBack(float k)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float x = k - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }
    }
}
