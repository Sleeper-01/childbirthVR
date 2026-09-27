using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChuJianXinSheng.Epilogue
{
    /// <summary>
    /// "初见·新生"成就徽章弹窗：徽章图 + 标题 + 描述，
    /// 伴随成就提示音与弹性缩放入场动画。
    /// </summary>
    public class AchievementPopup : MonoBehaviour
    {
        [Header("UI 引用（场景搭建时接线）")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panel;
        [SerializeField] private Image badgeImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descText;
        [SerializeField] private AudioSource chimeAudio;

        [Header("文案与动画")]
        [SerializeField] private float showSeconds = 0.55f;
        [SerializeField] private string achievementName = "初见·新生";
        [SerializeField] private string defaultDesc = "成就解锁 · 完成母婴早接触：环抱新生儿，轻拍相伴";

        public bool IsVisible { get; private set; }

        private void Awake()
        {
            if (titleText != null) titleText.text = achievementName;
            if (descText != null) descText.text = defaultDesc;
            // 序幕风格：刷自己的面板；徽章图保持原色不被面板刷影响
            if (panel != null)
            {
                var badgeColor = badgeImage != null ? badgeImage.color : Color.white;
                ChuJian.Merge.ActUIStyle.SkinDialogueTree(panel);
                if (badgeImage != null) badgeImage.color = badgeColor;
            }
            if (titleText != null) titleText.color = ChuJian.Merge.ActUIStyle.Warn;
            SetInstant(false);
        }

        /// <summary>展示成就弹窗（badge 为空时保留场景中已配置的徽章图）。</summary>
        public void Show(Sprite badge)
        {
            if (badgeImage != null && badge != null) badgeImage.sprite = badge;
            if (titleText != null) titleText.text = achievementName;
            IsVisible = true;
            if (chimeAudio != null && chimeAudio.clip != null) chimeAudio.Play();
            StartCoroutine(AnimateIn());
        }

        public void Hide()
        {
            IsVisible = false;
            SetInstant(false);
        }

        /// <summary>静默恢复（不重播提示音与入场动画），用于教学选择面板关闭后找回成就面板。</summary>
        public void Restore()
        {
            IsVisible = true;
            SetInstant(true);
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
