using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace VRHospitalAI.ThirdAct
{
    /// <summary>
    /// 相机淡入淡出工具。挂在与主相机同级的空物体上即可使用。
    /// </summary>
    public class CameraFade : MonoBehaviour
    {
        [Header("淡出颜色")]
        public Color fadeColor = Color.black;

        [Header("过渡时间（秒）")]
        public float fadeInDuration = 0.5f;
        public float fadeOutDuration = 0.5f;

        private CanvasGroup canvasGroup;

        void Awake()
        {
            var canvasGO = new GameObject("FadeCanvasLayer");
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            // Canvas 自带 RectTransform，不要重复添加

            canvasGroup = canvasGO.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0f;

            var bgGO = new GameObject("FadeBg");
            bgGO.transform.SetParent(canvasGO.transform, false);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.color = fadeColor;
            bgImg.raycastTarget = false;
        }

        public void FadeOut(float duration, System.Action callback)
        {
            StopAllCoroutines();
            // FadeOut：淡入黑色（alpha 0→1），用于隐藏画面
            StartCoroutine(FadeRoutine(canvasGroup.alpha, 1f, duration, callback));
        }

        public void FadeIn(float duration)
        {
            StopAllCoroutines();
            // FadeIn：淡出黑色（alpha 1→0），用于显示画面
            StartCoroutine(FadeRoutine(canvasGroup.alpha, 0f, duration, null));
        }

        public void SetAlpha(float alpha)
        {
            if (canvasGroup != null) canvasGroup.alpha = alpha;
        }

        private IEnumerator FadeRoutine(float from, float to, float duration, System.Action callback)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = Mathf.SmoothStep(0, 1, t);
                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = to;
                Debug.Log("[CameraFade] Fade完成: " + from + "→" + to);
            }
            else
            {
                Debug.LogError("[CameraFade] canvasGroup 是 null！");
            }
            callback?.Invoke();
        }
    }
}
