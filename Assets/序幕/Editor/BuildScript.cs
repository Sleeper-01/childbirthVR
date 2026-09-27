using System.IO;
using UnityEditor;
using UnityEngine;

namespace ChanFangVR
{
    /// 一键打包：把序章打成 Windows 64 位可执行文件，拷到别的电脑双击即可运行（无需安装 Unity）。
    /// 用法二选一：
    ///   1) Unity 菜单 Build → 打包 Windows 64 位
    ///   2) 命令行："<Unity安装目录>/Editor/Unity.exe" -batchmode -quit -projectPath "<项目路径>"
    ///             -executeMethod ChanFangVR.BuildScript.BuildWin64 -logFile "<日志路径>"
    ///
    /// 注意：本脚本不硬编码任何个人路径。输出目录默认是「项目文件夹旁边的 <项目名>-Build」，
    /// 也可以在菜单里换成任意目录（会记在 EditorPrefs 里，下次沿用）。
    public static class BuildScript
    {
        private const string PrefKey = "ChanFangVR.BuildScript.OutputDir";
        private static readonly string[] Scenes = { "Assets/Scenes/Prologue.unity" };

        /// 默认输出目录：项目文件夹同级，例如 Project/ 旁边生成 Project-Build/
        private static string DefaultOutputDir()
        {
            // Application.dataPath = <项目目录>/Assets，取两级父目录即项目目录本身
            var projectDir = Directory.GetParent(Application.dataPath).Parent;
            return projectDir == null ? "Build" : projectDir.FullName + "-Build";
        }

        private static string OutputDir
        {
            get { return EditorPrefs.GetString(PrefKey, DefaultOutputDir()); }
            set { EditorPrefs.SetString(PrefKey, value); }
        }

        [MenuItem("Build/打包 Windows 64 位")]
        public static void BuildWin64()
        {
            string dir = OutputDir.Replace("\\", "/");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string exe = Path.Combine(dir, Application.productName + ".exe");
            Debug.Log("[BUILD] 开始打包 -> " + exe);

            var report = BuildPipeline.BuildPlayer(Scenes, exe,
                BuildTarget.StandaloneWindows64, BuildOptions.None);

            Debug.Log(string.Format("[BUILD] 结果={0}  错误={1}  警告={2}  耗时={3:F1}s",
                report.summary.result, report.summary.totalErrors,
                report.summary.totalWarnings, report.summary.totalTime.TotalSeconds));

            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                foreach (var step in report.steps)
                {
                    foreach (var msg in step.messages)
                    {
                        if (msg.type == LogType.Error || msg.type == LogType.Exception)
                            Debug.LogError("[BUILD] " + step.name + ": " + msg.content);
                    }
                }
                Debug.LogError("[BUILD] 打包失败，请看上面的错误");
            }
            else
            {
                Debug.Log("[BUILD] 打包完成：" + dir);
            }
        }

        [MenuItem("Build/选择输出目录…")]
        public static void PickOutputDir()
        {
            string picked = EditorUtility.OpenFolderPanel("选择打包输出目录", OutputDir, "");
            if (!string.IsNullOrEmpty(picked)) OutputDir = picked;
            Debug.Log("[BUILD] 输出目录已设为：" + OutputDir);
        }
    }
}
