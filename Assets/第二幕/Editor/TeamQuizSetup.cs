using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace VRTour
{
    //编辑器工具：一键生成“医护团队与小测”并自动接好引用；重复执行时只刷新引用，不重复创建
    public static class TeamQuizSetup
    {
        [MenuItem("产程演示/生成医护团队与小测")]
        public static void CreateTeamQuiz()
        {
            TMP_FontAsset font = FindFont("LXGWWenKai SDF");
            if (font == null) font = FindFont("SIMHEI1 SDF");
            if (font == null) font = FindFont("SIMHEI SDF");

            //幂等：场景里已有就刷新引用，避免重复生成
            TeamMeetManager existing = Object.FindFirstObjectByType<TeamMeetManager>();
            if (existing != null)
            {
                existing.chineseFont = font;
                if (existing.memberTemplate == null)
                {
                    existing.memberTemplate = FindDoctorTemplate();
                }
                QuizManager quiz = existing.GetComponent<QuizManager>();
                if (quiz == null) quiz = Object.FindFirstObjectByType<QuizManager>();
                if (quiz != null) BindQuiz(quiz, font);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();
                Debug.Log("检测到已有的医护团队与小测，已刷新引用（含读题语音），未重复创建。");
                return;
            }

            GameObject rig = new GameObject("TeamQuizRig");
            Undo.RegisterCreatedObjectUndo(rig, "生成医护团队与小测");

            //三个可摆放锚点
            Transform teamAnchor = new GameObject("TeamAnchor（床旁）").transform;
            teamAnchor.SetParent(rig.transform, false);
            teamAnchor.localPosition = Vector3.zero;

            Transform teamBoard = new GameObject("TeamBoardAnchor（收藏卡）").transform;
            teamBoard.SetParent(rig.transform, false);
            teamBoard.localPosition = new Vector3(-2f, 1.5f, 0.5f);

            Transform quizBoard = new GameObject("QuizBoardAnchor（小测墙）").transform;
            quizBoard.SetParent(rig.transform, false);
            quizBoard.localPosition = new Vector3(2f, 1.6f, 0.5f);

            TeamMeetManager team = rig.AddComponent<TeamMeetManager>();
            team.teamAnchor = teamAnchor;
            team.teamBoardAnchor = teamBoard;
            team.chineseFont = font;
            team.memberTemplate = FindDoctorTemplate();

            QuizManager quizMgr = rig.AddComponent<QuizManager>();
            quizMgr.quizBoardAnchor = quizBoard;
            BindQuiz(quizMgr, font);

            Selection.activeGameObject = rig;
            Debug.Log("医护团队与小测已生成。摆放说明：\n" +
                "1. 把 TeamAnchor（床旁）挪到产床旁——成员会围绕它依次出现；\n" +
                "2. 把 TeamBoardAnchor（收藏卡）挪到想展示“我的医护团队”的位置；\n" +
                "3. 把 QuizBoardAnchor（小测墙）挪到墙面（Z 轴朝向房间）；\n" +
                "4. 按 Play 生效。若成员模型大小不合适，把 TeamMeetManager.memberTemplate 换成场景里的医生物体即可。");
        }

        //绑定小测引用：字体、反馈语音、读题语音
        private static void BindQuiz(QuizManager quiz, TMP_FontAsset font)
        {
            quiz.chineseFont = font;
            quiz.correctPraiseClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/第二幕/Audio/quiz_correct.mp3");
            quiz.wrongFeedbackClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/第二幕/Audio/quiz_wrong.mp3");
            quiz.questionClips = new[]
            {
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/第二幕/Audio/quiz_q1.mp3"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/第二幕/Audio/quiz_q2.mp3"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/第二幕/Audio/quiz_q3.mp3")
            };
        }

        private static GameObject FindDoctorTemplate()
        {
            //优先场景里的医生实例（保留用户的摆放与缩放）
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name.Contains("Ch16"))
                {
                    return root;
                }
            }
            //其次直接使用 FBX 资产
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
