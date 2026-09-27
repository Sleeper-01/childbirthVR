using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

namespace ChuJianXinSheng.Epilogue
{
    /// <summary>
    /// 视频进度条拖动跳转（B站式）：单击或按住拖动进度条即跳转到对应位置；
    /// 拖动开始时暂停视频（避免画面与进度来回拉扯），松手后恢复原有播放状态。
    /// 挂载在进度条背景（ProgressBg）上，事件由其子级（填充/命中区）冒泡而来。
    /// </summary>
    public class VideoSeekBar : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [Header("接线")]
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private Image progressFill;

        /// <summary>正在拖动中（InGameVideoScreen 据此跳过自动填充进度）。</summary>
        public bool IsDragging { get; private set; }

        private bool wasPlayingBeforeDrag;

        /// <summary>绑定目标播放器：InGameVideoScreen 在运行时创建播放器后调用（编辑态场景无该组件，序列化引用可能为空）。</summary>
        public void Bind(VideoPlayer player)
        {
            videoPlayer = player;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!CanSeek()) return;
            IsDragging = true;
            wasPlayingBeforeDrag = videoPlayer.isPlaying;
            if (wasPlayingBeforeDrag) videoPlayer.Pause();
            ApplyPointer(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsDragging) return;
            ApplyPointer(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!IsDragging) return;
            ApplyPointer(eventData);
            IsDragging = false;
            if (wasPlayingBeforeDrag) videoPlayer.Play();
        }

        /// <summary>单击进度条任意位置直接跳转（未发生拖动时）。</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsDragging || !CanSeek()) return;
            ApplyPointer(eventData);
        }

        private bool CanSeek()
        {
            return videoPlayer != null && videoPlayer.length > 0.0 && videoPlayer.canSetTime;
        }

        /// <summary>把指针屏幕位置换算为进度条上的比例并跳转；拖动中同步驱动填充反馈。</summary>
        private void ApplyPointer(PointerEventData eventData)
        {
            var barRect = transform as RectTransform;
            if (barRect == null) return;

            // 常规流程 pressEventCamera 由 EventSystem 射线结果提供；为空时兜底取画布相机/主相机
            Camera cam = eventData.pressEventCamera;
            if (cam == null)
            {
                var canvas = GetComponentInParent<Canvas>();
                cam = canvas != null && canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            }
            if (cam == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    barRect, eventData.position, cam, out Vector2 localPoint))
                return;

            float fraction = Mathf.Clamp01((localPoint.x - barRect.rect.x) / barRect.rect.width);
            videoPlayer.time = fraction * videoPlayer.length;
            if (progressFill != null) progressFill.fillAmount = fraction;
        }
    }
}
