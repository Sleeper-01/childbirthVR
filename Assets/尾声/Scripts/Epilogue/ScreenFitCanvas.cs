using UnityEngine;

namespace ChuJianXinSheng.Epilogue
{
    /// <summary>
    /// 让挂载本组件的 WorldSpace 画布在运行时精确铺满父相机视野：
    /// 以固定画布高度 50 单位反推等比缩放与画布宽度，随相机宽高比变化自动重算。
    /// 供"占屏幕三分之二"的总结面板与全屏花瓣特效使用，
    /// 子元素统一用锚点相对画布布局，即可始终与屏幕比例对齐。
    /// </summary>
    public class ScreenFitCanvas : MonoBehaviour
    {
        private const float CanvasHeightUnits = 50f;

        private RectTransform selfRect;
        private Camera viewCamera;
        private float lastAspect = -1f;

        private void Awake()
        {
            selfRect = transform as RectTransform;
            viewCamera = GetComponentInParent<Camera>();
            Fit();
        }

        private void Update()
        {
            // GameView 尺寸/宽高比变化时保持铺满（全屏视频只改自己的画布缩放，互不影响）
            if (viewCamera != null && !Mathf.Approximately(viewCamera.aspect, lastAspect))
                Fit();
        }

        private void Fit()
        {
            if (selfRect == null || viewCamera == null) return;

            float dist = Mathf.Abs(selfRect.localPosition.z);
            float viewH = viewCamera.orthographic
                ? viewCamera.orthographicSize * 2f
                : 2f * dist * Mathf.Tan(viewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            if (viewH <= 0f) return;

            lastAspect = viewCamera.aspect;
            float scale = viewH / CanvasHeightUnits;
            selfRect.localScale = new Vector3(scale, scale, scale);
            selfRect.sizeDelta = new Vector2(CanvasHeightUnits * viewCamera.aspect, CanvasHeightUnits);
        }
    }
}
