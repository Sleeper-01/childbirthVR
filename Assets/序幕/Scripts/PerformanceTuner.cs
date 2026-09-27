using System.Collections.Generic;
using UnityEngine;

namespace ChanFangVR
{
    /// <summary>
    /// 性能开关与诊断面板。
    ///
    /// 1) Attach() 时调用全局 VRPerformanceProfile。配置只在 XR 设备已加载时生效，
    ///    不修改编辑器桌面调试画质，也避免与其他脚本重复写 QualitySettings。
    /// 2) 按 F3 打开/关闭左上角诊断面板：FPS、当前可见三角形数、当前相机数量。
    ///    用它能判断掉帧到底是 GPU 画不动（三角形数很高）还是别的原因。
    /// </summary>
    public class PerformanceTuner : MonoBehaviour
    {
        /// <summary>关掉它就完全不改动任何画质设置。</summary>
        public const bool Enabled = true;

        private const float SampleInterval = 0.5f;

        private float _frames;
        private float _elapsed;
        private float _fps;
        private bool _show;
        private float _sampleTimer;
        private int _visibleTris;
        private int _camCount;
        private readonly Dictionary<Renderer, int> _triCache = new Dictionary<Renderer, int>();
        private Renderer[] _allRenderers = null;
        private GUIStyle _style;

        public static PerformanceTuner Attach(GameObject host, Camera worldCam, Camera uiCam)
        {
            var t = host.GetComponent<PerformanceTuner>();
            if (t == null) t = host.AddComponent<PerformanceTuner>();
            if (Enabled) ChuJian.Merge.VRPerformanceProfile.ApplyToCameras(worldCam, uiCam);
            return t;
        }

        private void Update()
        {
            _frames += 1f;
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed >= SampleInterval)
            {
                _fps = _frames / _elapsed;
                _frames = 0f;
                _elapsed = 0f;
            }

            if (DesktopInput.GetKeyDown(KeyCode.F3)) _show = !_show;

            if (_show)
            {
                _sampleTimer -= Time.unscaledDeltaTime;
                if (_sampleTimer <= 0f)
                {
                    _sampleTimer = SampleInterval;
                    Sample();
                }
            }
        }

        private void Sample()
        {
            if (_allRenderers == null) _allRenderers = FindObjectsOfType<Renderer>();
            int tris = 0;
            for (int i = 0; i < _allRenderers.Length; i++)
            {
                var r = _allRenderers[i];
                if (r == null || !r.isVisible) continue;
                int n;
                if (!_triCache.TryGetValue(r, out n))
                {
                    n = 0;
                    var mf = r.GetComponent<MeshFilter>();
                    var mesh = (mf != null) ? mf.sharedMesh : null;
                    if (mesh != null)
                    {
                        for (int s = 0; s < mesh.subMeshCount; s++)
                            n += (int)mesh.GetIndexCount(s) / 3;
                    }
                    _triCache[r] = n;
                }
                tris += n;
            }
            _visibleTris = tris;
            _camCount = Camera.allCamerasCount;
        }

        private void OnGUI()
        {
            if (!_show) return;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label);
                _style.fontSize = 16;
                _style.normal.textColor = Color.yellow;
            }
            GUI.Label(new Rect(10, 10, 460, 90),
                string.Format("FPS {0:F0}  |  可见三角形 {1:N0}  |  相机 {2}\nF3 关闭", _fps, _visibleTris, _camCount),
                _style);
        }
    }
}
