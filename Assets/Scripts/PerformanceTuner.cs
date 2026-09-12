using System.Collections.Generic;
using UnityEngine;

namespace ChanFangVR
{
    /// <summary>
    /// 性能开关与诊断面板。
    ///
    /// 1) Attach() 时统一做一次「性能模式」设置：关 MSAA / 关各向异性 / 关软粒子与实时反射探针 /
    ///    限制像素光与阴影 / 关掉 HDR，并把两台相机的远裁剪面收紧。
    ///    这些设置会在 Editor 里持久化，属于预期行为。
    /// 2) 按 F3 打开/关闭左上角诊断面板：FPS、当前可见三角形数、当前相机数量。
    ///    用它能判断掉帧到底是 GPU 画不动（三角形数很高）还是别的原因。
    /// </summary>
    public class PerformanceTuner : MonoBehaviour
    {
        /// <summary>关掉它就完全不改动任何画质设置。</summary>
        public const bool Enabled = true;

        /// <summary>1 = 纹理分辨率减半（弱显卡可再提帧，但会糊）；0 = 原分辨率。</summary>
        private const int MasterTextureLimit = 0;

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
            if (Enabled)
            {
                t.ApplyQuality();
                t.ApplyCamera(worldCam);
                t.ApplyCamera(uiCam);
            }
            return t;
        }

        private void ApplyQuality()
        {
            // MSAA 是最贵的抗锯齿，弱显卡/集显上常占 20%~40% 帧时间
            QualitySettings.antiAliasing = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            QualitySettings.pixelLightCount = 1;
            QualitySettings.softParticles = false;
            QualitySettings.realtimeReflectionProbes = false;
            QualitySettings.maximumLODLevel = 0;
            // 场景里的两盏灯本身就设了 LightShadows.None，这里再兜底关掉阴影贴图
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowDistance = 15f;
            QualitySettings.globalTextureMipmapLimit = MasterTextureLimit;
            // 锁 60 帧，避免编辑器里无谓地满载渲染
            QualitySettings.vSyncCount = 1;
        }

        private void ApplyCamera(Camera cam)
        {
            if (cam == null) return;
            cam.allowHDR = false;
            cam.allowMSAA = false;      // 独立于 QualitySettings.antiAliasing，必须单独关
            if (cam.farClipPlane > 80f) cam.farClipPlane = 80f;
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
