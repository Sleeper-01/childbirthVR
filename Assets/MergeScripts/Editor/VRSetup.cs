using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace ChuJian.Merge
{
    /// <summary>
    /// VR 一键配置：生成 XR Plug-in Management 的全套设置资产并为 PC(Standalone)
    /// 启用 OpenXR 加载器 + 启动时自动初始化。加载器实例复用序幕工程的
    /// Assets/序幕/XR/Loaders/OpenXRLoader.asset（保留其交互配置）。
    /// 用法：菜单「初见新生 → VR配置（启用OpenXR）」，执行一次即可。
    /// </summary>
    public static class VRSetup
    {
        [MenuItem("初见新生/VR配置（启用OpenXR）")]
        public static void EnableOpenXR()
        {
            try
            {
                // 1. 加载器：优先复用序幕的 OpenXRLoader 资产（带交互配置），否则新建
                OpenXRLoader loader = AssetDatabase.LoadAssetAtPath<OpenXRLoader>(
                    "Assets/序幕/XR/Loaders/OpenXRLoader.asset");
                if (loader == null)
                {
                    loader = ScriptableObject.CreateInstance<OpenXRLoader>();
                    loader.name = "OpenXR Loader";
                    if (!AssetDatabase.IsValidFolder("Assets/XR/Loaders"))
                        AssetDatabase.CreateFolder("Assets/XR", "Loaders");
                    AssetDatabase.CreateAsset(loader, "Assets/XR/Loaders/OpenXRLoader.asset");
                }

                // 2. 取/建 XRGeneralSettingsPerBuildTarget（挂在 EditorBuildSettings 配置对象上）
                XRGeneralSettingsPerBuildTarget perTarget;
                EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out perTarget);
                if (perTarget == null)
                {
                    // 优先复用工程里已有的设置资产（第三幕带来的那份已修好引用），避免重复创建
                    var found = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
                    foreach (var g in found)
                    {
                        var existing = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(
                            AssetDatabase.GUIDToAssetPath(g));
                        if (existing != null) { perTarget = existing; break; }
                    }
                }
                if (perTarget == null)
                {
                    perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                    perTarget.name = "XRGeneralSettings";
                    if (!AssetDatabase.IsValidFolder("Assets/XR/Settings"))
                        AssetDatabase.CreateFolder("Assets/XR", "Settings");
                    AssetDatabase.CreateAsset(perTarget, "Assets/XR/Settings/XRGeneralSettings.asset");
                }

                // 3. Standalone(PC) 的 GeneralSettings
                var general = perTarget.SettingsForBuildTarget(BuildTargetGroup.Standalone);
                if (general == null)
                {
                    general = ScriptableObject.CreateInstance<XRGeneralSettings>();
                    general.name = "PC Settings";
                    AssetDatabase.AddObjectToAsset(general, perTarget);
                    perTarget.SetSettingsForBuildTarget(BuildTargetGroup.Standalone, general);
                }

                // 4. ManagerSettings + 挂载 OpenXR 加载器
                if (general.Manager == null)
                {
                    var manager = ScriptableObject.CreateInstance<XRManagerSettings>();
                    manager.name = "XRManagerSettings";
                    AssetDatabase.AddObjectToAsset(manager, perTarget);
                    general.Manager = manager;
                }
                if (!general.Manager.loaders.Contains(loader))
                    general.Manager.loaders.Add(loader);
                general.InitManagerOnStart = true; // 启动自动初始化（无头显时自动失败回落桌面模式）

                // 5. 注册配置对象 + 强制标脏保存（确保落盘，防止域重载后配置丢失）
                EditorUtility.SetDirty(perTarget);
                EditorUtility.SetDirty(general);
                EditorUtility.SetDirty(general.Manager);
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // 6. 自验证：确认资产真的写进磁盘（内存配置重启会丢，必须落盘）
                string savedPath = AssetDatabase.GetAssetPath(perTarget);
                bool persisted = !string.IsNullOrEmpty(savedPath) && System.IO.File.Exists(savedPath);

                if (persisted)
                {
                    Debug.Log("[初见新生/VR配置] OpenXR 已启用并持久化：" + savedPath);
                    EditorUtility.DisplayDialog("VR配置完成",
                        "OpenXR 已为 PC 启用、启动时初始化，且已写入磁盘：\n" + savedPath +
                        "\n\n使用步骤：\n1. 先启动 SteamVR 并戴上头显\n2. 进入 Play —— 画面应出现在头显中\n3. 无头显时自动回落桌面键鼠模式", "好");
                }
                else
                {
                    Debug.LogWarning("[初见新生/VR配置] 资产路径异常：" + (savedPath ?? "空") + "，尝试备用路径重试");
                    // 备用：直接放到 Assets 根下换个名字重建
                    var backupPath = "Assets/XRGeneralSettings_初见新生.asset";
                    AssetDatabase.CreateAsset(perTarget, backupPath);
                    EditorUtility.SetDirty(perTarget);
                    AssetDatabase.SaveAssets();
                    EditorUtility.DisplayDialog("VR配置完成（备用路径）",
                        "配置已写入：" + backupPath + "\n\n重启编辑器后配置仍会保留。若 Play 仍不进头显，把此弹窗内容告知开发者。", "好");
                }
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("VR配置失败", ex.Message + "\n\n请把 Console 报错发给开发者。", "确定");
                Debug.LogException(ex);
            }
        }
    }
}
