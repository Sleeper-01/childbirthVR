using UnityEditor;
using UnityEngine;
using TMPro;

namespace VRTour
{
    //编辑器工具：一键在场景里生成产程拼图物体并自动接好中文字体
    public static class LaborPuzzleSetup
    {
        [MenuItem("产程演示/生成产程拼图")]
        public static void CreatePuzzle()
        {
            GameObject go = new GameObject("LaborPuzzle");
            Undo.RegisterCreatedObjectUndo(go, "生成产程拼图");
            //默认悬在原点、离地约1.6米，请挪到墙面上（Z 轴朝向房间）
            go.transform.position = new Vector3(0f, 1.6f, 0f);

            LaborPuzzleManager mgr = go.AddComponent<LaborPuzzleManager>();
            //优先使用霞鹜文楷，没有时回退到原有 SIMHEI 字体
            mgr.chineseFont = FindFont("LXGWWenKai SDF");
            if (mgr.chineseFont == null)
            {
                mgr.chineseFont = FindFont("SIMHEI1 SDF");
            }
            if (mgr.chineseFont == null)
            {
                mgr.chineseFont = FindFont("SIMHEI SDF");
            }
            if (mgr.chineseFont == null)
            {
                Debug.LogWarning("未找到中文字体资产。请先执行菜单【产程演示/一键切换中文字体（霞鹜文楷）】，或手动把 Fonts 下的 SDF 资产拖到 LaborPuzzleManager.chineseFont。");
            }

            Selection.activeGameObject = go;
            Debug.Log("产程拼图已生成。请把 LaborPuzzle 挪到墙面位置（Z 轴朝向房间、可整体缩放），运行后自动出现时间轴与打乱的图标卡。");
        }

        private static TMP_FontAsset FindFont(string name)
        {
            string[] guids = AssetDatabase.FindAssets(name + " t:TMP_FontAsset");
            if (guids == null || guids.Length == 0)
            {
                return null;
            }
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }
    }
}
