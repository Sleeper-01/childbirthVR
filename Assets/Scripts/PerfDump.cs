using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace ChanFangVR
{
    /// <summary>
    /// 性能诊断工具（只读，不修改任何画质设置）。
    ///
    /// 1) 启动时在 Console 打印一份场景统计（前缀 [PERF]）：
    ///    每个顶层物体的三角形数、场景总三角形数、相机数、质量档、MSAA、阴影、分辨率。
    ///    用来定位「到底是谁在吃帧」——按顶层分组，最大的那个就是元凶。
    ///
    /// 2) 运行时按 F3 开关左上角的 FPS 面板。
    ///
    /// 画质调整不在这里（见 PerformanceTuner.cs，默认未启用）。
    /// </summary>
    public class PerfDump : MonoBehaviour
    {
        public static void Log(Transform root, Camera mainCam)
        {
            if (root == null) return;

            var groups = new Dictionary<string, long>();
            long total = 0;
            int rendererCount = 0;

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                rendererCount++;
                // 归到 root 的直接子物体名下，便于一眼看出是哪部分最重
                var t = r.transform;
                while (t.parent != null && t.parent != root) t = t.parent;
                string key = t.name;
                long tris = TrisOf(r);
                total += tris;
                if (!groups.ContainsKey(key)) groups[key] = 0;
                groups[key] += tris;
            }

            var list = new List<KeyValuePair<string, long>>(groups);
            list.Sort((a, b) => b.Value.CompareTo(a.Value));

            var sb = new StringBuilder();
            sb.AppendLine("[PERF] ====== 场景统计（三角形数，按顶层物体分组）======");
            int n = 0;
            foreach (var kv in list)
            {
                if (n++ >= 10) break;
                float pct = total > 0 ? (kv.Value * 100f / total) : 0f;
                sb.AppendLine(string.Format("[PERF]   {0,-28} {1,10}  ({2:F1}%)", kv.Key, kv.Value, pct));
            }
            sb.AppendLine(string.Format("[PERF] 合计 {0} 个 Renderer，{1} 个三角形", rendererCount, total));
            sb.AppendLine(string.Format("[PERF] 相机数={0}  主相机={1}",
                Camera.allCamerasCount, mainCam != null ? mainCam.name : "无"));
            sb.AppendLine(string.Format("[PERF] 质量档={0}  MSAA={1}x  阴影={2}  像素光={3}  VSync={4}  分辨率={5}x{6}",
                QualitySettings.names[QualitySettings.GetQualityLevel()],
                QualitySettings.antiAliasing == 0 ? 1 : QualitySettings.antiAliasing,
                QualitySettings.shadows,
                QualitySettings.pixelLightCount,
                QualitySettings.vSyncCount,
                Screen.width, Screen.height));
            sb.AppendLine("[PERF] 说明：有阴影时场景要多渲染一遍 shadow caster，实际开销约为上表的两倍。");
            sb.AppendLine("[PERF] 按 F3 可开关 FPS 面板。");
            Debug.Log(sb.ToString());
        }

        private static long TrisOf(Renderer r)
        {
            Mesh m = null;
            var mf = r.GetComponent<MeshFilter>();
            if (mf != null) m = mf.sharedMesh;
            if (m == null)
            {
                var smr = r as SkinnedMeshRenderer;
                if (smr != null) m = smr.sharedMesh;
            }
            if (m == null) return 0;
            // 用 GetIndexCount 避免 triangles 属性拷贝整个索引数组（大模型会分配几十 MB）
            long c = 0;
            for (int s = 0; s < m.subMeshCount; s++) c += m.GetIndexCount(s);
            return c / 3;
        }

        // ———— F3 面板 ————

        private bool _show;
        private float _acc, _fps;
        private int _frames;
        private long _tris;
        private GUIStyle _style;
        private Renderer[] _all;            // 缓存一次，避免反复 FindObjectsOfType 造成 GC
        private float _sampleT;
        private int _sampleIdx;

        /// <summary>启动后自动打印 FPS 采样的次数（每 3 秒一次）。设 0 关闭。</summary>
        public static int AutoSamples = 6;

        public static void Attach(GameObject host)
        {
            if (host.GetComponent<PerfDump>() == null) host.AddComponent<PerfDump>();
        }

        private void Update()
        {
            if (DesktopInput.GetKeyDown(KeyCode.F3)) _show = !_show;

            _acc += Time.unscaledDeltaTime;
            _frames++;
            if (_acc >= 0.5f)
            {
                _fps = _frames / _acc;
                _acc = 0f;
                _frames = 0;

                // 只在面板打开时才统计可见三角形（平时不用每 0.5 秒全场景扫一遍）
                if (_show)
                {
                    if (_all == null) _all = FindObjectsOfType<Renderer>();
                    _tris = 0;
                    foreach (var r in _all)
                    {
                        if (r != null && r.isVisible) _tris += TrisOf(r);
                    }
                }

                // 自动采样：Console 搜 [PERF] 采样 就能看到前几秒的真实帧率
                if (_sampleIdx < AutoSamples)
                {
                    _sampleT += 0.5f;
                    if (_sampleT >= 3f)
                    {
                        _sampleT = 0f;
                        _sampleIdx++;
                        Debug.Log(string.Format(
                            "[PERF] 采样{0}/{1}: FPS={2:F1}  帧时间={3:F1}ms  可见三角形={4}",
                            _sampleIdx, AutoSamples, _fps, 1000f / Mathf.Max(_fps, 0.01f), _tris));
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (!_show) return;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label);
                _style.fontSize = 16;
                _style.normal.textColor = Color.white;
            }
            GUI.Box(new Rect(8, 8, 260, 86), "");
            GUI.Label(new Rect(18, 16, 240, 24), "FPS: " + _fps.ToString("F1"), _style);
            GUI.Label(new Rect(18, 40, 240, 24), "可见三角形: " + _tris, _style);
            GUI.Label(new Rect(18, 64, 240, 24), "相机: " + Camera.allCamerasCount + "  (F3 关闭)", _style);
        }
    }
}
