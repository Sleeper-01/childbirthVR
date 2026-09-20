using System.IO;
using UnityEditor;
using UnityEngine;

namespace ChanFangVR
{
    /// 一键打包：把序章打成 Windows 64 位可执行文件，拷到别的电脑双击即可运行（无需安装 Unity）。
    /// 用法二选一：
    ///   1) Unity 菜单 Build → 打包 Windows 64 位到桌面
    ///   2) 命令行："D:/unity/Editor/Unity.exe" -batchmode -quit -projectPath "<项目路径>"
    ///             -executeMethod ChanFangVR.BuildScript.BuildWin64 -logFile "<日志路径>"
    public static class BuildScript
    {
        // 输出目录（桌面）。注意与桌面上的项目目录区分，加了 -Build 后缀。
        private const string OutputDir = "C:/Users/23089/Desktop/ChildbirthVR-Prologue-Build";
        private const string ExeName = "ChildbirthVR_Prologue.exe";
        private static readonly string[] Scenes = { "Assets/Scenes/Prologue.unity" };

        [MenuItem("Build/打包 Windows 64 位到桌面")]
        public static void BuildWin64()
        {
            if (!Directory.Exists(OutputDir)) Directory.CreateDirectory(OutputDir);

            string exe = Path.Combine(OutputDir, ExeName);
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
                Debug.Log("[BUILD] 打包完成：" + OutputDir);
            }
        }
    }
}
