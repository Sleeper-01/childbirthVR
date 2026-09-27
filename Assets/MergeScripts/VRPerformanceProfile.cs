using UnityEngine;
using UnityEngine.XR;

namespace ChuJian.Merge
{
    /// <summary>
    /// 全局 VR 渲染性能配置。
    /// 只在 XR 设备已经加载时生效，避免影响 Unity 编辑器中的桌面调试。
    /// </summary>
    public static class VRPerformanceProfile
    {
        public const float RenderScale = 0.85f;
        public const float ShadowDistance = 30f;
        public const int ShadowCascades = 1;
        public const int PixelLightCount = 1;
        public const float LodBias = 1f;
        public const int Msaa = 2;

        private static bool _wasVrActive;

        public static bool IsVrActive
        {
            get
            {
                try
                {
                    return XRSettings.enabled && !string.IsNullOrEmpty(XRSettings.loadedDeviceName);
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>在场景加载或 XR 刚启动时调用一次。</summary>
        public static void ApplyForCurrentScene()
        {
            ApplyForCurrentScene(ShadowDistance);
        }

        /// <summary>
        /// 应用当前场景的 VR 配置。场景可传入更低的阴影距离以保持已有视觉基线。
        /// </summary>
        public static void ApplyForCurrentScene(float sceneShadowDistance)
        {
            if (!IsVrActive) return;

            QualitySettings.shadowDistance = Mathf.Clamp(sceneShadowDistance, 0f, ShadowDistance);
            QualitySettings.shadowCascades = ShadowCascades;
            QualitySettings.softParticles = false;
            QualitySettings.realtimeReflectionProbes = false;
            QualitySettings.pixelLightCount = PixelLightCount;
            QualitySettings.lodBias = LodBias;
            QualitySettings.antiAliasing = Msaa;
            QualitySettings.vSyncCount = 0;
            QualitySettings.globalTextureMipmapLimit = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;

            XRSettings.eyeTextureResolutionScale = RenderScale;
            ApplyToEnabledCameras();
            _wasVrActive = true;

            Debug.Log(string.Format(
                "[VR PERF] 应用统一配置：Scale={0:0.00}, MSAA={1}x, 阴影距离={2:0}, 级联={3}, HDR=关, 反射探针=关, 软粒子=关, 像素光={4}, LOD Bias={5:0.0}",
                RenderScale, Msaa, Mathf.Clamp(sceneShadowDistance, 0f, ShadowDistance), ShadowCascades, PixelLightCount, LodBias));
        }

        /// <summary>由全局管理器每帧轻量调用，仅在 XR 刚启用时执行一次。</summary>
        public static void ApplyIfVrJustActivated()
        {
            bool active = IsVrActive;
            if (active && !_wasVrActive) ApplyForCurrentScene();
            if (!active) _wasVrActive = false;
        }

        /// <summary>供序幕创建世界相机和 UI 相机后调用。</summary>
        public static void ApplyToCameras(Camera worldCamera, Camera uiCamera)
        {
            if (!IsVrActive) return;
            ApplyCamera(worldCamera);
            ApplyCamera(uiCamera);
        }

        /// <summary>刷新当前已启用相机的 VR 相机开关，不重复修改全局质量设置。</summary>
        public static void RefreshEnabledCameras()
        {
            if (!IsVrActive) return;
            ApplyToEnabledCameras();
        }

        private static void ApplyToEnabledCameras()
        {
            var cameras = Camera.allCameras;
            for (int i = 0; i < cameras.Length; i++) ApplyCamera(cameras[i]);
        }

        private static void ApplyCamera(Camera camera)
        {
            if (camera == null) return;
            camera.allowHDR = false;
            camera.allowMSAA = Msaa > 0;
        }
    }
}
