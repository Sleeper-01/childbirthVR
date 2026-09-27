using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace VRHospitalAI.ThirdAct
{
    /// <summary>
    /// HUD 状态栏控制器。
    /// 显示安心值、步骤提示、暂停菜单、安全计时器、字幕层。
    /// </summary>
    public class ThirdActHUD : MonoBehaviour
    {
        [Header("安心值")]
        public GameObject scorePanel;
        public Image scoreFill;
        public Text scoreText;

        [Header("步骤提示")]
        public GameObject stepHintPanel;
        public Text stepHintText;

        [Header("安全提醒")]
        public Text reminderText;

        [Header("字幕层")]
        public GameObject subtitlePanel;
        public Text subtitleText;

        [Header("工具栏")]
        public GameObject toolbarPanel;
        public Button helpButton;
        public Button replayButton;
        public Button pauseButton;
        public GameObject pauseMenu;

        [Header("帮助面板")]
        public GameObject helpPanel;
        public Text helpContentText;

        [Header("暂停菜单按钮")]
        public Button resumeButton;
        public Button quitButton;

        private bool isPaused;
        // 字幕打字机状态（序幕同款 26 字/秒）
        private string _subFull = "";
        private int _subPos;
        private float _subTimer;
        private bool _subActive;

        void Start()
        {
            UpdateScoreUI();
            SetupButtons();
            HideAll();
        }

        void Update()
        {
            // 字幕打字机：逐字出现，暂停时也继续推进（unscaled 时间，与序幕一致）
            if (_subActive && subtitleText != null && _subPos < _subFull.Length)
            {
                _subTimer += Time.unscaledDeltaTime * 26f;
                while (_subTimer >= 1f && _subPos < _subFull.Length)
                {
                    _subTimer -= 1f;
                    _subPos++;
                }
                subtitleText.text = _subFull.Substring(0, _subPos);
            }
            if (isPaused) return;
            if (Input.GetKeyDown(KeyCode.Escape))
                TogglePause();
        }

        void SetupButtons()
        {
            helpButton?.onClick.AddListener(OnHelpClick);
            replayButton?.onClick.AddListener(OnReplayClick);
            pauseButton?.onClick.AddListener(OnPauseClick);
            resumeButton?.onClick.AddListener(ResumeGame);
            quitButton?.onClick.AddListener(QuitToMenu);
        }

        public void UpdateScoreUI()
        {
            if (ScoreManager.Instance == null) return;
            int score = ScoreManager.Instance.GetScore();
            if (scoreText != null) scoreText.text = "安心值: " + score;
            if (scoreFill != null) scoreFill.fillAmount = score / 100f;

            // 根据分数变色：>=80 绿色，>=60 黄色，<60 红色
            if (scoreFill != null)
            {
                if (score >= 80) scoreFill.color = new Color(0.2f, 0.8f, 0.3f);
                else if (score >= 60) scoreFill.color = new Color(0.8f, 0.7f, 0.2f);
                else scoreFill.color = new Color(0.9f, 0.3f, 0.3f);
            }
        }

        public void ShowStepHint(string text)
        {
            if (stepHintPanel == null) return;
            stepHintPanel.SetActive(true);
            if (stepHintText != null) stepHintText.text = text;
        }

        public void ShowSafetyReminder(string text)
        {
            if (reminderText == null) return;
            reminderText.text = text;
            reminderText.gameObject.SetActive(true);
            StartCoroutine(HideReminderAfter(3f));
        }

        IEnumerator HideReminderAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (reminderText != null)
                reminderText.gameObject.SetActive(false);
        }

        public void ShowSubtitle(string text)
        {
            if (subtitlePanel == null) return;
            subtitlePanel.SetActive(!string.IsNullOrEmpty(text));
            if (subtitleText != null)
            {
                _subFull = text ?? "";
                _subPos = 0;
                _subTimer = 0f;
                _subActive = true;
                subtitleText.text = "";
            }
        }

        public void ClearSubtitle()
        {
            if (subtitlePanel != null) subtitlePanel.SetActive(false);
            _subActive = false;
            _subFull = "";
            if (subtitleText != null) subtitleText.text = "";
        }

        void OnHelpClick()
        {
            if (helpPanel == null) return;
            helpPanel.SetActive(!helpPanel.activeSelf);
            if (helpPanel.activeSelf && helpContentText != null)
                helpContentText.text =
                    "【手柄】扳机=确认 侧握=握持 Menu=暂停\n\n" +
                    "【键鼠】右键拖动=视角 左键=点击\n\n" +
                    "【不适时】按 Menu 暂停并呼叫护士";
        }

        void OnReplayClick()
        {
            Debug.Log("[HUD] 重播当前讲解（待实现）");
        }

        void OnPauseClick() => TogglePause();

        public void TogglePause()
        {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;
            if (pauseMenu != null) pauseMenu.SetActive(isPaused);
            if (isPaused)
                ShowSafetyReminder("已暂停，您可以稍作休息");
        }

        public void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1f;
            if (pauseMenu != null) pauseMenu.SetActive(false);
        }

        public void QuitToMenu()
        {
            Time.timeScale = 1f;
            isPaused = false;
            if (pauseMenu != null) pauseMenu.SetActive(false);
            FindObjectOfType<ThirdActController>()?.QuitToMenu();
        }

        void HideAll()
        {
            if (stepHintPanel != null) stepHintPanel.SetActive(false);
            if (helpPanel != null) helpPanel.SetActive(false);
            if (pauseMenu != null) pauseMenu.SetActive(false);
            if (subtitlePanel != null) subtitlePanel.SetActive(false);
            if (reminderText != null) reminderText.gameObject.SetActive(false);
        }
    }
}
