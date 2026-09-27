using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

namespace ChuJianXinSheng.Epilogue
{
    /// <summary>
    /// 场景内教学视频屏幕：点击「立即观看」后，教学动画直接在本面板内播放（不弹浏览器）。
    /// 视频来源（按优先级）：
    ///   1. 序列化字段 videoClip（在检视面板直接指定 VideoClip）；
    ///   2. Assets/Resources/TeachingVideo.mp4 —— 放入该路径的 MP4 会自动加载（即插即用）；
    ///   3. 序列化字段 videoUrl（可直接拉流直链，如 mp4/hls 地址）。
    /// 未接入视频时屏幕显示占位提示。
    /// </summary>
    public class InGameVideoScreen : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("UI 引用（场景搭建时接线）")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panel;
        [SerializeField] private RawImage videoImage;          // 播放画面（RenderTexture）
        [SerializeField] private TMP_Text placeholderText;     // 无视频时的占位提示
        [SerializeField] private TMP_Text playPauseLabel;      // 播放/暂停按钮文字
        [SerializeField] private Image progressFill;          // 进度条填充
        [SerializeField] private CanvasGroup controlsGroup;  // 控制区（进度条+按钮），B站式悬停显隐
        [SerializeField] private TMP_Text fullscreenLabel;    // 全屏按钮文字
        [SerializeField] private VideoSeekBar seekBar;        // 进度条拖动跳转组件

        [Header("视频来源")]
        [SerializeField] private VideoClip videoClip;
        [SerializeField] private string videoUrl = "";
        [SerializeField, TextArea] private string placeholder =
            "教学视频尚未接入\n\n请将 MP4 文件放置到\nAssets/Resources/TeachingVideo.mp4\n\n放入后点「重新检测」即可在场景内播放";

        [Header("播放设置")]
        [SerializeField] private int renderTextureWidth = 1280;
        [SerializeField] private int renderTextureHeight = 720;

        public bool IsOpen { get; private set; }
        public bool HasVideo { get; private set; }

        /// <summary>屏幕关闭时触发，供上层恢复状态。</summary>
        public event System.Action OnClosed;

        private VideoPlayer videoPlayer;
        private AudioSource audioSource;
        private RenderTexture renderTexture;

        private Canvas rootCanvas;
        private RectTransform canvasRect;
        private bool fullscreen;
        private bool pointerOverPanel;
        private float lastPointerClickTime;
        private Vector3 canvasBasePosition;
        private Vector3 canvasBaseScale;
        private Vector2 videoBaseAnchorMin;
        private Vector2 videoBaseAnchorMax;
        private Vector2 videoBaseAnchoredPosition;
        private Vector2 videoBaseSizeDelta;

        /// <summary>无视频时 RawImage 的深色底板（与场景原配色一致）。</summary>
        private static readonly Color NoVideoBackdrop = new Color(0.050f, 0.050f, 0.060f, 1f);

        private const float ControlsFadeSeconds = 0.25f;
        private const float DoubleClickSeconds = 0.35f;
        private const float EndFinishToleranceSeconds = 0.25f;
        private const string FullscreenEnterText = "全屏";
        private const string FullscreenExitText = "退出全屏";

        private void Awake()
        {
            ResolveVideoSource();
            SetupPlayer();
            rootCanvas = GetComponentInParent<Canvas>();
            canvasRect = rootCanvas != null ? rootCanvas.transform as RectTransform : null;
            SetInstant(false);
        }

        private void ResolveVideoSource()
        {
            if (videoClip == null)
            {
                var auto = Resources.Load<VideoClip>("TeachingVideo");
                if (auto != null) videoClip = auto;
            }
            HasVideo = videoClip != null || !string.IsNullOrEmpty(videoUrl);
        }

        private void SetupPlayer()
        {
            // 显式声明 sRGB 读写：VideoPlayer 输出 sRGB 帧，若 RT 走 Linear 路径，
            // 画面经 UI 采样显示时会被压暗约4~5倍，表现为近乎纯黑
            renderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            if (videoImage != null) videoImage.texture = renderTexture;

            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            videoPlayer = gameObject.GetComponent<VideoPlayer>();
            if (videoPlayer == null) videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTexture;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.controlledAudioTrackCount = 1;
            videoPlayer.SetTargetAudioSource(0, audioSource);

            // VideoPlayer 由本组件在运行时添加：创建后立即绑定给进度条拖动组件
            if (seekBar != null) seekBar.Bind(videoPlayer);
        }

        /// <summary>打开屏幕并开始播放（无视频则显示占位提示）。</summary>
        public void Open()
        {
            ResolveVideoSource();
            HasVideo = videoClip != null || !string.IsNullOrEmpty(videoUrl);
            IsOpen = true;
            UpdateBackdropColor();
            pointerOverPanel = false;

            if (placeholderText != null)
            {
                placeholderText.gameObject.SetActive(!HasVideo);
                if (!HasVideo) placeholderText.text = placeholder;
            }
            if (playPauseLabel != null) playPauseLabel.text = "暂停";

            if (HasVideo)
            {
                if (videoClip != null) { videoPlayer.source = VideoSource.VideoClip; videoPlayer.clip = videoClip; }
                else { videoPlayer.source = VideoSource.Url; videoPlayer.url = videoUrl; }
                videoPlayer.Stop();          // 强制归零：避免沿用上次的播放位置
                videoPlayer.Prepare();
                videoPlayer.Play();
                Debug.Log("[Epilogue] 教学动画开始在场景内播放（从头播放）");
            }

            StartCoroutine(AnimateIn());
        }

        /// <summary>关闭屏幕并停止播放。</summary>
        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (fullscreen) ExitFullscreen();
            if (videoPlayer != null) videoPlayer.Stop();   // 无条件停止：防备 Prepare 完成后播放器自行启动导致关屏后仍在播
            StopAllCoroutines();
            SetInstant(false);
            if (OnClosed != null) OnClosed();
        }

        /// <summary>「播放/暂停」按钮回调。</summary>
        public void TogglePlayPause()
        {
            if (!IsOpen || !HasVideo || videoPlayer == null) return;
            if (videoPlayer.isPlaying)
            {
                videoPlayer.Pause();
                if (playPauseLabel != null) playPauseLabel.text = "播放";
            }
            else
            {
                videoPlayer.Play();
                if (playPauseLabel != null) playPauseLabel.text = "暂停";
            }
        }

        /// <summary>「重新检测」按钮回调：接入视频文件后无需重进即可开始播放。</summary>
        public void RetryDetect()
        {
            ResolveVideoSource();
            UpdateBackdropColor();
            if (placeholderText != null)
            {
                placeholderText.gameObject.SetActive(!HasVideo);
                if (!HasVideo) placeholderText.text = placeholder;
            }
            if (HasVideo)
            {
                if (videoClip != null) { videoPlayer.source = VideoSource.VideoClip; videoPlayer.clip = videoClip; }
                else { videoPlayer.source = VideoSource.Url; videoPlayer.url = videoUrl; }
                videoPlayer.Stop();
                videoPlayer.Prepare();
                videoPlayer.Play();
                if (playPauseLabel != null) playPauseLabel.text = "暂停";
            }
        }

        /// <summary>RawImage 的 color 会乘以整幅视频画面：有视频时必须为纯白，
        /// 否则深色 tint 会把画面压成近黑（亮度约为原来的 1/20）；
        /// 无视频时保留深色底板衬托占位提示。</summary>
        private void UpdateBackdropColor()
        {
            if (videoImage != null)
                videoImage.color = HasVideo ? Color.white : NoVideoBackdrop;
        }

        /// <summary>「全屏」按钮 / 双击视频区回调：悬浮面板 ↔ 铺满相机视野。</summary>
        public void ToggleFullscreen()
        {
            if (!IsOpen) return;
            if (fullscreen) ExitFullscreen();
            else EnterFullscreen();
        }

        private void EnterFullscreen()
        {
            if (canvasRect == null) return;
            fullscreen = true;
            canvasBasePosition = canvasRect.localPosition;
            canvasBaseScale = canvasRect.localScale;

            // WorldSpace 画布挂在主相机下：按视锥尺寸非等比缩放画布（=面板）正好铺满视野，
            // 面板内子元素随比例保持相对位置
            var cam = rootCanvas.worldCamera != null ? rootCanvas.worldCamera
                : canvasRect.parent.GetComponent<Camera>();
            if (cam == null) { fullscreen = false; return; }

            float dist = Mathf.Abs(canvasRect.localPosition.z);
            float viewH = cam.orthographic
                ? cam.orthographicSize * 2f
                : 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float viewW = viewH * cam.aspect;
            canvasRect.localScale = new Vector3(
                viewW / canvasRect.rect.width,
                viewH / canvasRect.rect.height,
                canvasRect.localScale.z);
            canvasRect.localPosition = new Vector3(0f, 0f, canvasRect.localPosition.z);

            // 视频画面按 16:9 letterbox，避免被竖屏视口拉伸变形
            if (videoImage != null)
            {
                var rt = videoImage.rectTransform;
                videoBaseAnchorMin = rt.anchorMin;
                videoBaseAnchorMax = rt.anchorMax;
                videoBaseAnchoredPosition = rt.anchoredPosition;
                videoBaseSizeDelta = rt.sizeDelta;

                float videoAspect = videoPlayer != null && videoPlayer.width > 0
                    ? (float)videoPlayer.width / videoPlayer.height
                    : 16f / 9f;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                if (viewW / viewH > videoAspect)
                {
                    float slack = (canvasRect.rect.width - canvasRect.rect.height * videoAspect) * 0.5f;
                    rt.offsetMin = new Vector2(slack, 0f);
                    rt.offsetMax = new Vector2(-slack, 0f);
                }
                else
                {
                    float slack = (canvasRect.rect.height - canvasRect.rect.width / videoAspect) * 0.5f;
                    rt.offsetMin = new Vector2(0f, slack);
                    rt.offsetMax = new Vector2(0f, -slack);
                }
            }

            if (fullscreenLabel != null) fullscreenLabel.text = FullscreenExitText;
        }

        private void ExitFullscreen()
        {
            if (canvasRect == null) return;
            fullscreen = false;
            canvasRect.localPosition = canvasBasePosition;
            canvasRect.localScale = canvasBaseScale;
            if (videoImage != null)
            {
                var rt = videoImage.rectTransform;
                rt.anchorMin = videoBaseAnchorMin;
                rt.anchorMax = videoBaseAnchorMax;
                rt.anchoredPosition = videoBaseAnchoredPosition;
                rt.sizeDelta = videoBaseSizeDelta;
            }
            if (fullscreenLabel != null) fullscreenLabel.text = FullscreenEnterText;
        }

        /// <summary>指针进入面板：控制区（进度条+按钮）淡入。</summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerOverPanel = true;
        }

        /// <summary>指针离开面板：控制区淡出隐藏。</summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            pointerOverPanel = false;
        }

        /// <summary>双击面板空白/视频区切换全屏（按钮点击不会冒泡到此处）。</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (Time.unscaledTime - lastPointerClickTime < DoubleClickSeconds) ToggleFullscreen();
            lastPointerClickTime = Time.unscaledTime;
        }

        private void Update()
        {
            // 控制区 B 站式悬停显隐：移出时禁射线，避免挡住面板其他区域
            if (controlsGroup != null)
            {
                float target = IsOpen && pointerOverPanel ? 1f : 0f;
                controlsGroup.alpha = Mathf.MoveTowards(controlsGroup.alpha, target,
                    Time.unscaledDeltaTime / ControlsFadeSeconds);
                controlsGroup.blocksRaycasts = controlsGroup.alpha > 0.5f;
            }

            if (!IsOpen || progressFill == null) return;
            // 拖动进度条期间由 VideoSeekBar 直接驱动填充（seek 生效前 player.time 可能滞后）
            if (seekBar != null && seekBar.IsDragging) return;
            if (HasVideo && videoPlayer != null && videoPlayer.length > 0.0)
                progressFill.fillAmount = (float)(videoPlayer.time / videoPlayer.length);
            else
                progressFill.fillAmount = 0f;
        }

        /// <summary>视频播完后自动回到选择面板（保持成就状态）。</summary>
        private void LateUpdate()
        {
            // 自然播完自动关闭。Tuanjie VideoPlayer 到达片尾后进入 isPaused=true 的"到尾暂停"
            // 状态（而非 isPlaying=false && isPaused=false），因此用 time≈length 判定播完；
            // 用户中途手动暂停时 time 远小于片长，不会误判
            if (IsOpen && HasVideo && videoPlayer != null && !videoPlayer.isPlaying
                && videoPlayer.frame > 0 && videoPlayer.length > 0.0
                && videoPlayer.time >= videoPlayer.length - EndFinishToleranceSeconds)
            {
                Close();
            }
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
            const float showSeconds = 0.4f;
            Vector3 small = Vector3.one * 0.6f;
            while (t < showSeconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / showSeconds);
                if (canvasGroup != null) canvasGroup.alpha = k;
                if (panel != null) panel.localScale = Vector3.Lerp(small, Vector3.one, k);
                yield return null;
            }
            SetInstant(true);
        }
    }
}
