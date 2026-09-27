using UnityEditor;
using UnityEngine;

namespace ChanFangVR
{
    /// 编辑器工具菜单：序章调试与重置
    public static class PrologueEditorTools
    {
        [MenuItem("ChanFangVR/序章/重置序章（回到步骤1）")]
        private static void ResetPrologue()
        {
            var manager = Object.FindObjectOfType<PrologueManager>();
            if (manager != null)
            {
                manager.Restart();
                Debug.Log("序章已重置到步骤1");
            }
            else
            {
                Debug.LogWarning("找不到 PrologueManager，请先进入 Play 模式。");
            }
        }

        [MenuItem("ChanFangVR/序章/切换 VR 模式")]
        private static void ToggleVRMode()
        {
            var world = Object.FindObjectOfType<PrologueWorld>();
            if (world != null)
            {
                world.SetVRMode(!world.VRMode);
                Debug.Log("切换到 " + (world.VRMode ? "VR" : "桌面") + " 模式");
            }
            else
            {
                Debug.LogWarning("找不到 PrologueWorld，请先进入 Play 模式。");
            }
        }

        [MenuItem("ChanFangVR/序章/创建入口物体")]
        private static void CreateBootstrap()
        {
            var go = new GameObject("PrologueBootstrap");
            go.AddComponent<PrologueBootstrap>();
            Selection.activeGameObject = go;
            Debug.Log("已创建 PrologueBootstrap，运行场景即可启动序章。");
        }
    }
}
