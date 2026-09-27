using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChuJian.Merge
{
    /// <summary>
    /// 一键配置：生成主菜单场景 Main.unity，并按方案顺序把全部幕场景写入 Build Settings。
    /// 使用：打开工程后点菜单「初见新生 → 一键配置」。
    /// </summary>
    public static class MergeSetup
    {
        // 编辑器启动时自动校验：Main.unity 已存在而 Build Settings 缺幕场景时自动补齐
        [InitializeOnLoadMethod]
        static void AutoSyncOnLoad()
        {
            EditorApplication.delayCall += SyncSceneList;
        }

        static void SyncSceneList()
        {
            if (Application.isPlaying) return;
            if (!File.Exists(ActFlow.MenuScene)) return; // Main.unity 尚未生成，等待首次一键配置
            string[] needed = new[] { ActFlow.MenuScene }
                .Concat(ActFlow.Entries.Select(e => e.ScenePath))
                .ToArray();
            if (needed.Any(p => !File.Exists(p))) return;
            var current = EditorBuildSettings.scenes.Select(s => s.path).ToList();
            if (needed.All(current.Contains)) return;
            var merged = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .Concat(needed.Where(p => !current.Contains(p)))
                .Distinct()
                .Select(p => new EditorBuildSettingsScene(p, true))
                .ToArray();
            EditorBuildSettings.scenes = merged;
            Debug.Log("[初见新生] 已自动补齐 Build Settings 场景表（共 " + merged.Length + " 个场景）");
        }

        [MenuItem("初见新生/一键配置（生成主菜单+设置场景表）")]
        public static void Setup()
        {
            if (!EditorUtility.DisplayDialog("一键配置",
                "将执行两步：\n1) 按方案顺序把 10 个场景写入 Build Settings\n2) 生成主菜单场景 Main.unity\n\n注意：当前打开场景中未保存的修改会丢失。是否继续？",
                "继续", "取消"))
                return;

            string[] paths = new[] { ActFlow.MenuScene }
                .Concat(ActFlow.Entries.Select(e => e.ScenePath))
                .ToArray();

            // 注意：只校验九个幕场景；Main.unity 是本次要生成的目标，不参与校验
            var missing = ActFlow.Entries.Select(e => e.ScenePath)
                .Where(p => !File.Exists(p))
                .ToArray();
            if (missing.Length > 0)
            {
                EditorUtility.DisplayDialog("缺少场景文件",
                    "以下场景不存在，请检查资产是否拷贝完整：\n" + string.Join("\n", missing), "确定");
                return;
            }

            EditorBuildSettings.scenes = paths
                .Select(p => new EditorBuildSettingsScene(p, true))
                .ToArray();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("MainMenu");
            go.AddComponent<MainMenu>();
            // 背景相机：主菜单是纯Overlay UI不需要相机，但没有相机会显示
            // "Display 1 No cameras rendering" 提示；此相机只清屏为菜单底色，消除提示
            var camGo = new GameObject("MenuBackgroundCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.031f, 0.090f, 0.149f); // 医疗蓝深底 #081726
            cam.cullingMask = 0;
            cam.depth = -10;
            camGo.tag = "MainCamera"; // 让 Camera.main 在菜单场景可用（VR注视点击与手柄位姿依赖它）
            if (!EditorSceneManager.SaveScene(scene, ActFlow.MenuScene))
            {
                EditorUtility.DisplayDialog("保存失败", "Main.unity 保存失败，详情见 Console。", "确定");
                return;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[初见新生] 一键配置完成，" + paths.Length + " 个场景已按方案顺序写入 Build Settings");
            EditorUtility.DisplayDialog("完成",
                "主菜单 Main.unity 已生成，" + paths.Length + " 个场景已按方案顺序写入。\n\n点 Play 即可从主菜单开始：可「顺序体验」全程，也可直选任意一幕。\n幕内快捷键：F8 下一幕 / F7 上一幕 / F9 返回主菜单。",
                "好");
        }
    }
}
