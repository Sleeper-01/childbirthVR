using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VRHospitalAI
{
    /// <summary>
    /// 对话UI控制器：显示 NPC 台词 + 3个选项按钮。
    /// 做成 World Space Canvas，挂在场景里，始终面向玩家头部。
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        public static DialogueUI Instance { get; private set; }

        [Header("UI 引用")]
        public Canvas dialogueCanvas;          // 主 Canvas (World Space)
        public UnityEngine.UI.Text npcNameText;           // NPC 名字
        public UnityEngine.UI.Text npcReplyText;          // NPC 台词
        public UnityEngine.UI.Text statusText;            // "正在思考..." 提示
        public Button[] optionButtons = new Button[3]; // 3个选项按钮
        public Button endConversationButton;   // "结束对话"按钮（可选）

        [Header("自定义输入")]
        public InputField playerInputField;  // 玩家打字输入框（可选）
        public Button sendButton;                // 发送按钮
        public UnityEngine.UI.Text placeholderText;         // 输入框占位提示文字"输入你想说的话..."

        [Header("VR 简化模式（手势交互）")]
        [Tooltip("开启后：对话显示时只显示 NPC 姓名 + 台词 + 录音按钮，隐藏选项/输入框/结束按钮")]
        public bool vrMode = false;
        [Tooltip("录音按钮（VR 手势模式下用，握拳点击它开始/停止录音）")]
        public Button recordButton;

        [Header("打字机效果")]
        public float typeSpeed = 0.04f;        // 每字秒数，0=立即显示

        private Coroutine typeCoroutine;
        private System.Action<int> onOptionChosen; // 选中选项后的回调
        private System.Action<string> onCustomInput; // 玩家自定义文字输入的回调
        /// <summary>玩家点击"结束对话"时触发（NPCBehaviour 订阅，调用 EndConversation）</summary>
        public static event System.Action onEndConversation;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Debug.Log($"[DialogueUI] Awake 设置 Instance = {Instance != null}, 物体={gameObject.name}, 启用={gameObject.activeInHierarchy}");
        }

        void OnEnable()
        {
            // OnEnable 在物体激活时调用，比 Awake 更可靠
            if (Instance == null)
            {
                Instance = this;
                Debug.Log($"[DialogueUI] OnEnable 设置 Instance, 物体={gameObject.name}");
            }
        }

        void Start()
        {
            Hide();
            // 绑定选项按钮事件
            for (int i = 0; i < optionButtons.Length; i++)
            {
                int idx = i; // 闭包捕获
                if (optionButtons[i] != null)
                {
                    optionButtons[i].onClick.AddListener(() => OnOptionClicked(idx));
                    optionButtons[i].gameObject.SetActive(false);
                }
            }

            // 绑定"结束对话"按钮
            if (endConversationButton != null)
            {
                endConversationButton.onClick.AddListener(() =>
                {
                    Debug.Log("[DialogueUI] 玩家点击了「结束对话」");
                    onEndConversation?.Invoke();
                    Hide();
                });
            }

            // 绑定发送按钮 + 输入框回车
            if (sendButton != null)
            {
                sendButton.onClick.AddListener(OnSendClicked);
            }
            if (playerInputField != null)
            {
                playerInputField.onSubmit.AddListener((_) => OnSendClicked());
            }
        }

        /// <summary>发送按钮/回车：把输入框的文字发送出去</summary>
        void OnSendClicked()
        {
            if (playerInputField == null) return;
            string text = playerInputField.text;
            if (string.IsNullOrWhiteSpace(text)) return;

            Debug.Log($"[DialogueUI] 玩家输入: {text}");
            playerInputField.text = "";       // 清空输入框
            onCustomInput?.Invoke(text);       // 触发回调（NPCBehaviour 会发起对话）
        }

        /// <summary>外部调用：把输入框当前文字发送出去（语音识别用）</summary>
        public void SendInput()
        {
            OnSendClicked();
        }

        /// <summary>显示NPC正在思考（等待回复时）</summary>
        public void ShowThinking(string npcName)
        {
            // 兜底：如果 Instance 是 null，尝试现在查找
            if (Instance == null)
            {
                Instance = FindObjectOfType<DialogueUI>();
                Debug.Log($"[DialogueUI] ShowThinking 兜底查找 Instance = {Instance != null}");
            }

            Show();
            if (npcNameText) npcNameText.text = npcName;
            if (npcReplyText) npcReplyText.text = "";
            if (statusText) statusText.text = "<i>正在思考...</i>";
            HideAllOptions();
            HideInputField();

            // VR 模式：思考期间隐藏结束按钮，保留录音按钮
            if (vrMode)
            {
                if (endConversationButton != null) endConversationButton.gameObject.SetActive(false);
                if (recordButton != null) recordButton.gameObject.SetActive(true);
            }

            // 停掉之前的打字机效果
            if (typeCoroutine != null) { StopCoroutine(typeCoroutine); typeCoroutine = null; }
        }

        /// <summary>流式显示：服务器每推送一段文字就调用，实时显示已累积的文本</summary>
        public void ShowStreamingReply(string npcName, string accumulatedReply)
        {
            Show();
            if (npcNameText) npcNameText.text = npcName;
            if (statusText) statusText.text = ""; // 收到第一个字了，不再显示"思考中"
            if (npcReplyText)
            {
                npcReplyText.text = accumulatedReply;
                // 停掉打字机协程（流式时不用打字机，服务器已经分段推了）
                if (typeCoroutine != null) { StopCoroutine(typeCoroutine); typeCoroutine = null; }
            }
        }

        /// <summary>显示NPC的台词和选项</summary>
        public void ShowReply(ChatResponse resp, System.Action<int> onChosen)
        {
            onOptionChosen = onChosen;
            if (npcNameText) npcNameText.text = resp.name;
            if (statusText) statusText.text = "";

            // 打字机效果
            if (typeCoroutine != null) StopCoroutine(typeCoroutine);
            if (typeSpeed > 0)
                typeCoroutine = StartCoroutine(TypeText(resp.reply));
            else if (npcReplyText) npcReplyText.text = resp.reply;

            // 显示选项 + 输入框（非 VR 模式）
            ShowOptions(resp.options);
            ShowInputField();

            // VR 模式：显示完之后隐藏多余元素，只留 姓名+台词+录音按钮
            ApplyVRMode();
        }

        /// <summary>VR 简化模式：隐藏选项/输入框，保留 姓名+台词+录音按钮+结束对话按钮。
        /// 注意：绝不递归激活祖先链（会把共同面板激活，导致该隐藏的元素跟着显示）。</summary>
        void ApplyVRMode()
        {
            if (!vrMode) return;

            // 隐藏选项按钮（按钮自己）
            if (optionButtons != null)
                foreach (var b in optionButtons)
                    if (b != null) b.gameObject.SetActive(false);

            // 隐藏输入框区：隐藏 playerInputField 的直接父级（InputArea 容器），
            // 这样输入框+发送按钮+背景一起隐藏。InputArea 不含 Name/Reply，安全。
            if (playerInputField != null && playerInputField.transform.parent != null)
                playerInputField.transform.parent.gameObject.SetActive(false);
            else if (playerInputField != null)
                playerInputField.gameObject.SetActive(false);
            if (sendButton != null) sendButton.gameObject.SetActive(false);

            // 确保 Canvas 激活
            if (dialogueCanvas != null)
            {
                dialogueCanvas.enabled = true;
                dialogueCanvas.gameObject.SetActive(true);
            }

            // 确保这些元素自身激活（不碰它们的父级，避免副作用）
            if (npcNameText != null) npcNameText.gameObject.SetActive(true);
            if (npcReplyText != null) npcReplyText.gameObject.SetActive(true);
            if (recordButton != null) recordButton.gameObject.SetActive(true);
            if (endConversationButton != null) endConversationButton.gameObject.SetActive(true);
        }

        /// <summary>注册自定义输入回调（NPCBehaviour 调用）</summary>
        public void SetCustomInputCallback(System.Action<string> callback)
        {
            onCustomInput = callback;
        }

        /// <summary>显示输入框和发送按钮</summary>
        void ShowInputField()
        {
            // 激活整个输入区（父级），否则子物体启用也看不见
            if (playerInputField != null && playerInputField.transform.parent != null)
                playerInputField.transform.parent.gameObject.SetActive(true);
            if (playerInputField != null) playerInputField.gameObject.SetActive(true);
            if (sendButton != null) sendButton.gameObject.SetActive(true);
            // 注意：不自动激活输入框焦点，否则按 V 键会被输入框吃掉当成打字。
            // 玩家需要主动点击输入框才能打字；语音输入用按住 V 键。
        }

        /// <summary>隐藏输入框（思考中/结束时）</summary>
        void HideInputField()
        {
            if (playerInputField != null && playerInputField.transform.parent != null)
                playerInputField.transform.parent.gameObject.SetActive(false);
            if (playerInputField != null) playerInputField.text = "";
        }

        /// <summary>显示错误信息</summary>
        public void ShowError(string err)
        {
            if (npcReplyText) npcReplyText.text = $"<color=red>出错了：{err}</color>";
            if (statusText) statusText.text = "";
            HideAllOptions();
        }

        IEnumerator TypeText(string text)
        {
            if (npcReplyText == null) yield break;
            npcReplyText.text = "";
            foreach (char c in text)
            {
                npcReplyText.text += c;
                yield return new WaitForSeconds(typeSpeed);
            }
        }

        void ShowOptions(List<string> options)
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] == null) continue;
                if (i < options.Count && !string.IsNullOrEmpty(options[i]))
                {
                    optionButtons[i].gameObject.SetActive(true);
                    var label = optionButtons[i].GetComponentInChildren<UnityEngine.UI.Text>();
                    if (label) label.text = options[i];
                }
                else
                {
                    optionButtons[i].gameObject.SetActive(false);
                }
            }
        }

        void HideAllOptions()
        {
            foreach (var b in optionButtons)
                if (b != null) b.gameObject.SetActive(false);
        }

        void OnOptionClicked(int index)
        {
            // 点了选项，交给回调处理（NPCBehaviour 会发起下一轮对话）
            onOptionChosen?.Invoke(index);
            HideAllOptions();
            HideInputField(); // 选了选项后，等待下一轮回复期间隐藏输入框
        }

        public void Show() { if (dialogueCanvas) dialogueCanvas.enabled = true; }
        public void Hide()
        {
            if (dialogueCanvas) dialogueCanvas.enabled = false;
            if (typeCoroutine != null) StopCoroutine(typeCoroutine);
            HideInputField();
        }
    }
}
