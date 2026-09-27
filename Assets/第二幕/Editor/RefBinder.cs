using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace VRTour
{
    //编辑器工具：一键绑定全部语音与引用——开场迎接语音、小测读题语音、各管理器中文字体、监护仪读数组件
    public static class RefBinder
    {
        [MenuItem("产程演示/一键绑定语音引用")]
        public static void BindAll()
        {
            TMP_FontAsset font = FindFont("LXGWWenKai SDF");
            if (font == null) font = FindFont("SIMHEI1 SDF");
            if (font == null) font = FindFont("SIMHEI SDF");

            int bound = 0;

            //1. 开场迎接：每行口播语音
            OpeningIntro intro = Object.FindFirstObjectByType<OpeningIntro>();
            if (intro != null)
            {
                intro.greetingClips = new[]
                {
                    LoadClip("Assets/第二幕/Audio/opening_line1.mp3"),
                    LoadClip("Assets/第二幕/Audio/opening_line2.mp3"),
                    LoadClip("Assets/第二幕/Audio/opening_line3.mp3")
                };
                bound++;
            }
            else
            {
                Debug.LogWarning("场景里没有 OpeningIntro（开场迎接），跳过开场语音绑定。");
            }

            //2. 小测：读题语音 + 字体 + 反馈语音
            QuizManager quiz = Object.FindFirstObjectByType<QuizManager>();
            if (quiz != null)
            {
                quiz.chineseFont = font;
                quiz.questionClips = new[]
                {
                    LoadClip("Assets/第二幕/Audio/quiz_q1.mp3"),
                    LoadClip("Assets/第二幕/Audio/quiz_q2.mp3"),
                    LoadClip("Assets/第二幕/Audio/quiz_q3.mp3")
                };
                quiz.correctPraiseClip = LoadClip("Assets/第二幕/Audio/quiz_correct.mp3");
                quiz.wrongFeedbackClip = LoadClip("Assets/第二幕/Audio/quiz_wrong.mp3");
                bound++;
            }

            //3. 各管理器中文字体刷新
            LaborPuzzleManager puzzle = Object.FindFirstObjectByType<LaborPuzzleManager>();
            if (puzzle != null)
            {
                puzzle.chineseFont = font;
            }
            TeamMeetManager team = Object.FindFirstObjectByType<TeamMeetManager>();
            if (team != null)
            {
                team.chineseFont = font;
                if (team.memberTemplate == null)
                {
                    team.memberTemplate = FindDoctorTemplate();
                }
            }

            //4. 监护仪：按介绍文字含“监护仪”识别并挂读数动画组件
            ClickableDevice[] devices = Object.FindObjectsByType<ClickableDevice>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (ClickableDevice d in devices)
            {
                if (d.introText != null && d.introText.Contains("监护仪"))
                {
                    MonitorReadout readout = d.GetComponent<MonitorReadout>();
                    if (readout == null)
                    {
                        readout = Undo.AddComponent<MonitorReadout>(d.gameObject);
                    }
                    readout.chineseFont = font;
                    bound++;
                    Debug.Log("监护仪读数动画已挂载到：" + d.gameObject.name + "（位置偏移可在 MonitorReadout.screenWorldOffset 调整）");
                }
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("语音与引用绑定完成，共处理 " + bound + " 项。");
        }

        private static AudioClip LoadClip(string path)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                Debug.LogWarning("未找到音频文件：" + path + "（该条语音将静音，可后补文件再执行本菜单）");
            }
            return clip;
        }

        private static GameObject FindDoctorTemplate()
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name.Contains("Ch16"))
                {
                    return root;
                }
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/第二幕/doctor/source/Ch16_nonPBR.fbx");
        }

        private static TMP_FontAsset FindFont(string name)
        {
            string[] guids = AssetDatabase.FindAssets(name + " t:TMP_FontAsset");
            if (guids == null || guids.Length == 0)
            {
                return null;
            }
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
