using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace VRHospitalAI.ThirdAct
{
    /// <summary>
    /// 镇痛法宝选择模块（3.4.3）。
    /// 用户从工具架选取 1-2 件物品放入待产包。
    /// 选满2件自动进入问答；选1件需点击"确认选择"按钮手动进入。
    /// 护士询问选择原因，给予个性化回应。
    /// </summary>
    public class ToolboxModule : MonoBehaviour
    {
        [Header("工具架")]
        public GameObject toolboxPanel;
        public Text toolTitleText;
        public Button[] toolButtons = new Button[5];
        public Button confirmButton;  // 手动确认按钮（选1件时使用）

        [Header("待产包 UI")]
        public GameObject baggagePanel;
        public Text baggageCountText;
        public GameObject[] baggageSlots = new GameObject[2];

        [Header("问答")]
        public GameObject questionPanel;
        public Text questionText;
        public Text responseText;
        public Button[] answerButtons = new Button[5];

        [Header("转场")]
        public CameraFade cameraFade;
        public ScoreManager scoreManager;
        public ThirdActHUD hud;

        private List<int> selectedTools = new List<int>();
        private bool questionShown;
        private bool autoAdvanceTriggered;  // 防止自动跳转与手动点击同时触发

        // 五种镇痛法宝（要点式短文案）
        private readonly (string name, string desc, string subtitle)[] tools = new[]
        {
            ("呼吸卡", "拉玛泽呼吸指南卡，自主控制节奏", "宫缩时跟着三阶段图示做"),
            ("分娩球", "卧式分娩球，调整体位缓解腰酸", "宫缩时帮您找到更舒服的姿势"),
            ("镇痛泵", "镇痛药物的主要载体", "持续给药，宫缩时更舒适"),
            ("按摩手", "腰骶部按摩工具", "宫缩时按压腰骶，缓解酸痛"),
            ("音乐盒", "播放舒缓音乐，放松心情", "放松心情，转移注意力"),
        };

        // 预设回答选项
        private readonly (string answer, string response, string subtitle)[] presetAnswers = new[]
        {
            ("呼吸法我最有把握，能自己控制",
             "好选择！呼吸法您已经练过了，继续保持！",
             "护士小安：呼吸法您自己就能掌握，很实用"),
            ("镇痛泵听起来最安心",
             "镇痛泵安全有效，医生会评估是否适用。",
             "护士小安：镇痛泵由医生评估，全程监测"),
            ("音乐和按摩能让我放松",
             "音乐加按摩，家人一起配合效果更好。",
             "护士小安：让家人一起参与，效果更好"),
            ("我想都试试，多一种选择多一分安心",
             "多一分准备，多一分安心，您做得很好。",
             "护士小安：您已经准备得很好了"),
            ("还没想好，想再了解一下",
             "不着急，全程都有我们陪着，随时可调整。",
             "护士小安：有任何问题随时告诉我们"),
        };

        public event Action<List<int>> OnToolboxComplete;

        private bool buttonsSetup;  // 防止重复连线

        void Start()
        {
            if (cameraFade == null) cameraFade = FindObjectOfType<CameraFade>();
            if (scoreManager == null) scoreManager = FindObjectOfType<ScoreManager>();
            if (hud == null) hud = FindObjectOfType<ThirdActHUD>();
            HideAll();
        }

        // 延迟连接按钮：Start 时 UI 面板可能尚未创建
        void Update()
        {
            if (!buttonsSetup && toolboxPanel != null)
            {
                SetupButtons();
                UpdateToolLabels();
                buttonsSetup = true;
            }
        }

        public void StartToolbox()
        {
            HideAll();
            toolboxPanel.SetActive(true);
            selectedTools.Clear();
            questionShown = false;
            autoAdvanceTriggered = false;
            UpdateBaggageUI();
            UpdateToolLabels();
            if (hud != null)
                hud.ShowSubtitle("小安：挑 1-2 件法宝放入待产包");
        }

        void SetupButtons()
        {
            for (int i = 0; i < toolButtons.Length; i++)
            {
                int idx = i;
                toolButtons[i]?.onClick.AddListener(() => OnToolClick(idx));
            }
            confirmButton?.onClick.AddListener(() => {
                if (!questionShown && selectedTools.Count >= 1)
                    ShowQuestion();
            });
        }

        void UpdateToolLabels()
        {
            if (toolTitleText != null)
                toolTitleText.text = "选择法宝放入待产包（" + selectedTools.Count + "/2）";

            for (int i = 0; i < toolButtons.Length; i++)
            {
                if (toolButtons[i] == null) continue;
                var label = toolButtons[i].GetComponentInChildren<Text>();
                if (label != null) label.text = tools[i].name;
                toolButtons[i].interactable = !selectedTools.Contains(i) && selectedTools.Count < 2;
            }

            // 确认按钮：选1件时高亮提示，选2件后隐藏
            if (confirmButton != null)
            {
                confirmButton.gameObject.SetActive(selectedTools.Count == 1);
                var confirmText = confirmButton.GetComponentInChildren<Text>();
                if (confirmText != null)
                    confirmText.text = selectedTools.Count == 1 ? "确认选择，继续" : "";
            }
        }

        void OnToolClick(int idx)
        {
            if (selectedTools.Contains(idx) || selectedTools.Count >= 2) return;
            selectedTools.Add(idx);
            if (VRInput.TriggerDown) VRInput.VibrateRight(0.3f, 0.05f);
            if (hud != null)
                hud.ShowSubtitle($"您选择了「{tools[idx].name}」：{tools[idx].subtitle}");
            Debug.Log($"[镇痛法宝] 放入: {tools[idx].name}");
            UpdateBaggageUI();
            UpdateToolLabels();

            // 选满2件后自动进入问答（只选1件时需手动点击确认按钮）
            if (selectedTools.Count >= 2 && !autoAdvanceTriggered)
            {
                autoAdvanceTriggered = true;
                Invoke(nameof(ShowQuestion), 0.8f);
            }
        }

        void UpdateBaggageUI()
        {
            if (baggageCountText != null) baggageCountText.text = $"{selectedTools.Count}/2";
            for (int i = 0; i < baggageSlots.Length; i++)
                if (baggageSlots[i] != null)
                    baggageSlots[i].SetActive(i < selectedTools.Count);
        }

        void ShowQuestion()
        {
            if (questionShown) return;  // 防止重复触发
            autoAdvanceTriggered = true;
            toolboxPanel.SetActive(false);
            questionPanel.SetActive(true);
            questionShown = true;

            if (questionText != null)
                questionText.text = "如果宫缩越来越强，您会优先尝试什么？";
            if (responseText != null) responseText.text = "";

            if (hud != null)
                hud.ShowSubtitle("小安：凭刚才的体验来选就好");
        }

        public void SetupAnswerButtons(Button[] answerBtns)
        {
            for (int i = 0; i < answerBtns.Length && i < presetAnswers.Length; i++)
            {
                int idx = i;
                answerBtns[i]?.onClick.AddListener(() => OnAnswerClick(idx));
            }
        }

        void OnAnswerClick(int idx)
        {
            if (!questionShown) return;
            if (VRInput.TriggerDown) VRInput.VibrateRight(0.3f, 0.05f);

            var ans = presetAnswers[idx];
            if (responseText != null) responseText.text = ans.response;
            if (hud != null)
            {
                hud.ShowSubtitle(ans.subtitle);
                hud.ClearSubtitle();
            }

            Debug.Log($"[镇痛决策] 选择: {ans.answer}");
            Debug.Log($"[镇痛决策] 护士回应: {ans.response}");

            SavePainManagementPreference();
            questionPanel.SetActive(false);
            scoreManager?.AddScore(3);
            OnToolboxComplete?.Invoke(selectedTools);
        }

        void SavePainManagementPreference()
        {
            var prefs = new List<string>(selectedTools.Count);
            foreach (int idx in selectedTools)
                prefs.Add(tools[idx].name);
            PlayerPrefs.SetString("ThirdAct_PainPreference", string.Join(",", prefs));
            PlayerPrefs.Save();
            Debug.Log($"[镇痛决策] 已保存偏好: {string.Join(",", prefs)}");
        }

        public void HideAll()
        {
            if (toolboxPanel) toolboxPanel.SetActive(false);
            if (baggagePanel) baggagePanel.SetActive(false);
            if (questionPanel) questionPanel.SetActive(false);
        }
    }
}
