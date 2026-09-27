using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace VRTour
{
    //一键把场景内全部 TMP 文本切换为霞鹜文楷（动态图集，运行时按需烘焙字形）
    public static class FontSwitcher
    {
        private const string FontTtfPath = "Assets/第二幕/Fonts/LXGWWenKai-Regular.ttf";
        private const string FontAssetPath = "Assets/第二幕/Fonts/LXGWWenKai-Regular SDF.asset";

        [MenuItem("产程演示/一键切换中文字体（霞鹜文楷）")]
        public static void SwitchToWenKai()
        {
            //已生成过就直接复用
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);

            if (fontAsset == null)
            {
                Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(FontTtfPath);
                if (sourceFont == null)
                {
                    Debug.LogError("未找到字体文件 " + FontTtfPath + "，请先下载霞鹜文楷放入 Assets/Fonts 后重试。");
                    return;
                }

                fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
                if (fontAsset == null)
                {
                    Debug.LogError("TMP_FontAsset.CreateFontAsset 创建失败。可改用 Window > TextMeshPro > Font Asset Creator 手动生成（Atlas Population Mode 选 Dynamic，Source Font 选 LXGWWenKai），然后重新执行本菜单。");
                    return;
                }

                fontAsset.name = "LXGWWenKai SDF";
                fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic; //动态图集：无需预先烘焙全量字形
                AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
                if (fontAsset.material != null)
                {
                    fontAsset.material.name = "LXGWWenKai SDF Material";
                    AssetDatabase.AddObjectToAsset(fontAsset.material, FontAssetPath);
                }
                if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0 && fontAsset.atlasTextures[0] != null)
                {
                    AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], FontAssetPath);
                }
                EditorUtility.SetDirty(fontAsset);
                AssetDatabase.SaveAssets();
                Debug.Log("已生成动态字体资产：" + FontAssetPath);
            }

            int changed = 0;

            //1. 场景内全部 TMP 文本（含未激活的 UI 与预制体实例）
            TMP_Text[] texts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (TMP_Text t in texts)
            {
                if (t.font != fontAsset)
                {
                    t.font = fontAsset;
                    if (fontAsset.material != null)
                    {
                        t.fontSharedMaterial = fontAsset.material;
                    }
                    EditorUtility.SetDirty(t);
                    changed++;
                }
            }

            //2. 场景里的产程拼图管理器（保证之后生成的拼图也用新字体）
            LaborPuzzleManager[] managers = Object.FindObjectsByType<LaborPuzzleManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (LaborPuzzleManager m in managers)
            {
                if (m.chineseFont != fontAsset)
                {
                    m.chineseFont = fontAsset;
                    EditorUtility.SetDirty(m);
                    changed++;
                }
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("字体切换完成：共更新 " + changed + " 处文本/组件为霞鹜文楷。SIMHEI 的两个 SDF 资产已不再被引用，验证无误后可删除。");
        }
    }
}
