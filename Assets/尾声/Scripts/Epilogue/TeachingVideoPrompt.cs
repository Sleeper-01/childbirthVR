using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChuJianXinSheng.Epilogue
{
    /// <summary>
    /// 成就解锁后的教学动画选择面板（对应策划3.6尾声"随后播放早开奶与肌肤接触（袋鼠式护理）」教学动画"）：
    /// 询问用户是否播放教学动画；选择「立即观看」后在场景内视频屏幕播放（InGameVideoScreen），
    /// 未接入视频文件时屏幕显示占位提示；屏幕组件缺失时回退为默认浏览器打开链接。
    /// </summary>
    public class TeachingVideoPrompt : MonoBehaviour
    {
        [Header("UI 引用（场景搭建时接线）")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panel;
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private Button playButton;
        [SerializeField] private Button declineButton;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private InGameVideoScreen videoScreen;
        [SerializeField] private SummaryPanel summaryPanel;

        [Header("内容与行为")]
        [SerializeField, TextArea] private string question = "是否播放「早开奶与肌肤接触（袋鼠式护理）」教学动画？";
        [SerializeField] private string videoUrl = "https://www.bilibili.com/video/BV12XzZB6Ec8/";

        public bool IsVisible { get; private set; }

        /// <summary>面板关闭时触发（暂不观看或播放后自动收起），供 EpilogueDirector 恢复字幕。</summary>
        public event System.Action OnClosed;

        private bool parkedForScreen;

        private void Awake()
        {
            // 序幕风格：本组件自己把自己的面板刷成序幕深色圆角样式（不依赖任何全局扫描）
            if (panel != null) ChuJian.Merge.ActUIStyle.SkinDialogueTree(panel);
            if (questionText != null) questionText.color = ChuJian.Merge.ActUIStyle.TextMain;
            SetInstant(false);
            if (videoScreen != null)
                videoScreen.OnClosed += HandleScreenClosed;
        }

        public void Show()
        {
            IsVisible = true;
            if (questionText != null) questionText.text = question;
            if (statusText != null) statusText.text = string.Empty;
            if (playButton != null) playButton.interactable = true;
            if (declineButton != null) declineButton.interactable = true;
            StartCoroutine(AnimateIn());
        }

        public void Hide()
        {
            // 任何隐藏路径都先确保视频屏幕关闭（先清除回开标记，避免屏幕关闭时又把本面板弹回来）
            if (videoScreen != null && videoScreen.IsOpen)
            {
                parkedForScreen = false;
                videoScreen.Close();
            }
            if (!IsVisible) return;
            IsVisible = false;
            StopAllCoroutines();
            SetInstant(false);
            if (OnClosed != null) OnClosed();
        }

        /// <summary>视频屏幕关闭后（用户关闭或自然播完）：改为弹出总结画面，不再回到本选择面板。</summary>
        private void HandleScreenClosed()
        {
            if (!parkedForScreen) return;
            parkedForScreen = false;
            if (summaryPanel != null) summaryPanel.Show();
            else Show();
        }

        /// <summary>用户选择「立即观看」时触发，供总控静音场景音（如婴儿啼哭）等。</summary>
        public event System.Action OnPlayVideo;

        /// <summary>「立即观看」按钮回调：在场景内视频屏幕播放（面板静默让位，屏幕关闭后自动回来）。</summary>
        public void PlayVideo()
        {
            if (!IsVisible) return;
            if (OnPlayVideo != null) OnPlayVideo();
            if (videoScreen != null)
            {
                parkedForScreen = true;
                videoScreen.Open();
                // 静默让位（不触发 OnClosed，成就面板保持隐藏由屏幕接管）
                IsVisible = false;
                StopAllCoroutines();
                SetInstant(false);
                Debug.Log("[Epilogue] 教学动画切换到场景内屏幕");
            }
            else
            {
                if (statusText != null) statusText.text = "已为您在浏览器中打开教学视频";
                Application.OpenURL(videoUrl);
                Debug.Log("[Epilogue] 已打开教学动画链接（外部浏览器，回退方案）");
            }
        }

        /// <summary>「暂不观看」按钮回调。</summary>
        public void Decline()
        {
            Hide();
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
