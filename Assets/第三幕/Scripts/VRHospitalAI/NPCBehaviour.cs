using UnityEngine;
using VRHospitalAI;

namespace VRHospitalAI
{
    /// <summary>
    /// 挂在每个 NPC 角色上。负责：被交互 → 发起对话 → 播放台词 → 驱动表现。
    /// </summary>
    public class NPCBehaviour : MonoBehaviour
    {
        [Header("角色配置")]
        public string npcId = "doctor";
        public string displayName = "李医生";

        [Header("表现层引用")]
        public Animator animator;
        public AudioSource audioSource;

        [Header("待机/说话 动画参数名")]
        public string talkParam = "IsTalking";
        public string thinkParam = "IsThinking";

        private System.Collections.Generic.List<string> currentOptions;
        private static System.Collections.Generic.List<NPCBehaviour> allNpcs;
        private bool hiddenByConversation = false;

        public static NPCBehaviour ActiveConversation { get; private set; }

        public static NPCBehaviour FindById(string id)
        {
            EnsureNpcList();
            foreach (var npc in allNpcs)
            {
                if (npc == null) continue;
                if (npc.npcId == id && npc.gameObject.activeSelf)
                    return npc;
            }
            return null;
        }

        static void EnsureNpcList()
        {
            if (allNpcs == null || allNpcs.Count == 0)
                allNpcs = new System.Collections.Generic.List<NPCBehaviour>(FindObjectsOfType<NPCBehaviour>());
        }

        void Start()
        {
            DialogueUI.onEndConversation += OnEndConversationRequested;
        }

        void OnDestroy()
        {
            DialogueUI.onEndConversation -= OnEndConversationRequested;
        }

        void OnEndConversationRequested()
        {
            EndConversation();
        }

        public void Interact()
        {
            Debug.Log($"[NPC] Interact() 被调用！displayName={displayName}, npcId={npcId}");
            HideOtherNpcs();
            ActiveConversation = this;
            StartConversation();
        }

        void HideOtherNpcs()
        {
            if (allNpcs == null)
                allNpcs = new System.Collections.Generic.List<NPCBehaviour>(
                    FindObjectsOfType<NPCBehaviour>());

            foreach (var npc in allNpcs)
            {
                if (npc == this) continue;
                if (!npc.gameObject.activeSelf) continue;
                npc.hiddenByConversation = true;
                npc.gameObject.SetActive(false);
            }
        }

        void RestoreOtherNpcs()
        {
            if (allNpcs == null) return;
            foreach (var npc in allNpcs)
            {
                if (npc == this) continue;
                if (npc.hiddenByConversation && !npc.gameObject.activeSelf)
                {
                    npc.gameObject.SetActive(true);
                    npc.hiddenByConversation = false;
                }
            }
        }

        public void StartConversation(string playerInput = null)
        {
            Debug.Log($"[NPC] StartConversation 开始，playerInput={playerInput}");

            // 检查是否有对话客户端
            if (DialogueClient.Instance == null)
            {
                Debug.LogWarning($"[NPC] {displayName} 没有对话客户端，跳过对话。");
                // 不调用 EndConversation，避免触发自动跳转
                SetThink(false);
                SetTalk(false);
                return;
            }

            if (DialogueUI.Instance != null)
                DialogueUI.Instance.ShowThinking(displayName);

            SetThink(true);
            SetTalk(false);
            DialogueClient.Instance.Chat(npcId, playerInput, OnReply, OnError);
        }

        void OnReply(ChatResponse resp)
        {
            SetThink(false);
            currentOptions = resp.options;
            ApplyEmotion(resp.emotion);
            SetTalk(true);

            if (DialogueUI.Instance != null)
            {
                DialogueUI.Instance.ShowReply(resp, (optionIndex) =>
                {
                    if (currentOptions != null && optionIndex < currentOptions.Count)
                        StartConversation(currentOptions[optionIndex]);
                });
                DialogueUI.Instance.SetCustomInputCallback((customText) =>
                {
                    StartConversation(customText);
                });
            }
        }

        void OnError(string err)
        {
            SetThink(false);
            SetTalk(false);
            Debug.LogWarning($"[{displayName}] 对话失败: {err}，直接结束对话。");
            if (DialogueUI.Instance != null)
                DialogueUI.Instance.ShowError(err);
            EndConversation();
        }

        void ApplyEmotion(string emotion)
        {
            if (animator == null || string.IsNullOrEmpty(emotion)) return;
            animator.SetTrigger("React");
            animator.SetFloat("Mood", MoodToValue(emotion));
        }

        float MoodToValue(string emotion)
        {
            switch (emotion)
            {
                case "笑": case "微笑": case "轻松": return 1.0f;
                case "调侃": case "安心": case "耐心": return 0.6f;
                case "关切": case "认真": return 0.4f;
                case "严肃": case "警惕": case "愤怒": return 0.0f;
                default: return 0.5f;
            }
        }

        void SetTalk(bool on) { if (animator != null) animator.SetBool(talkParam, on); }
        void SetThink(bool on) { if (animator != null) animator.SetBool(thinkParam, on); }

        public void EndConversation()
        {
            SetTalk(false);
            SetThink(false);
            if (DialogueUI.Instance != null) DialogueUI.Instance.Hide();
            if (ActiveConversation == this) ActiveConversation = null;
            RestoreOtherNpcs();
        }
    }
}
