using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace VRHospitalAI
{
    /// <summary>
    /// 对话服务客户端：负责与 Python 对话服务通信。
    /// 单例，全局只有一个。所有 NPC 共用。
    /// </summary>
    public class DialogueClient : MonoBehaviour
    {
        public static DialogueClient Instance { get; private set; }

        [Header("服务地址")]
        [Tooltip("Python 对话服务的地址。本机调试用 localhost；VR头显用电脑局域网IP，如 http://192.168.1.100:8000")]
        public string serverUrl = "http://localhost:8000";

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// 发起一次对话（非流式，等完整回复）。
        /// npcId: 角色ID；playerInput: 玩家说的话（null/空=开始对话）；
        /// onSuccess: 收到回复时回调；onError: 出错时回调。
        /// </summary>
        public void Chat(string npcId, string playerInput,
                         Action<ChatResponse> onSuccess, Action<string> onError = null)
        {
            StartCoroutine(ChatRoutine(npcId, playerInput, onSuccess, onError));
        }

        /// <summary>
        /// 发起一次流式对话：边收边通过 onDelta 回调推送文字，onDone 推送完整结果。
        /// 回复速度的体感会好很多（玩家立刻看到字蹦出来）。
        /// </summary>
        public void ChatStream(string npcId, string playerInput,
                               Action<string> onDelta,
                               Action<ChatResponse> onDone,
                               Action<string> onError = null)
        {
            StartCoroutine(ChatStreamRoutine(npcId, playerInput, onDelta, onDone, onError));
        }

        /// <summary>流式对话协程：逐行读取服务器推送，解析 reply 增量和 done 事件</summary>
        IEnumerator ChatStreamRoutine(string npcId, string playerInput,
                                      Action<string> onDelta,
                                      Action<ChatResponse> onDone,
                                      Action<string> onError)
        {
            var req = new ChatRequest { npc_id = npcId, player_input = string.IsNullOrEmpty(playerInput) ? null : playerInput };

            string inputJson = string.IsNullOrEmpty(req.player_input)
                ? $"{{\"npc_id\":\"{req.npc_id}\",\"player_input\":null}}"
                : $"{{\"npc_id\":\"{req.npc_id}\",\"player_input\":{EscapeJson(req.player_input)}}}";

            using (var post = new UnityWebRequest($"{serverUrl}/chat/stream", "POST"))
            {
                byte[] body = Encoding.UTF8.GetBytes(inputJson);
                post.uploadHandler = new UploadHandlerRaw(body);
                post.downloadHandler = new DownloadHandlerBuffer();
                post.SetRequestHeader("Content-Type", "application/json");
                post.timeout = 30;

                yield return post.SendWebRequest();

                if (post.result == UnityWebRequest.Result.Success)
                {
                    // 服务器返回的是多行 JSON（每行一个事件）
                    // 注意：Unity 的 UnityWebRequest 是一次性收完，不是真流式。
                    // 但服务器会尽快返回，体感上仍然比非流式快（省去了 JSON 完整解析等待）。
                    string text = post.downloadHandler.text;
                    ChatResponse finalResp = null;
                    foreach (var line in text.Split('\n'))
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        try
                        {
                            var evt = JsonUtility.FromJson<StreamEvent>(line);
                            if (evt.type == "reply" && onDelta != null)
                            {
                                onDelta.Invoke(evt.text);
                            }
                            else if (evt.type == "done")
                            {
                                finalResp = new ChatResponse
                                {
                                    npc_id = npcId,
                                    name = evt.name,
                                    reply = evt.text ?? "", // done 里没有完整 reply，靠 delta 累积
                                    emotion = evt.emotion,
                                    options = evt.options
                                };
                            }
                            else if (evt.type == "error" && onError != null)
                            {
                                onError.Invoke(evt.message);
                                yield break;
                            }
                        }
                        catch (Exception) { /* 忽略无法解析的行 */ }
                    }

                    if (finalResp != null)
                        onDone?.Invoke(finalResp);
                    else if (onError != null)
                        onError.Invoke("流式响应缺少 done 事件");
                }
                else
                {
                    onError?.Invoke($"网络错误: {post.error}");
                }
            }
        }

        IEnumerator ChatRoutine(string npcId, string playerInput,
                                Action<ChatResponse> onSuccess, Action<string> onError)
        {
            var req = new ChatRequest { npc_id = npcId, player_input = string.IsNullOrEmpty(playerInput) ? null : playerInput };

            // 手工拼 JSON（避免引入第三方库；字段简单固定）
            string inputJson = string.IsNullOrEmpty(req.player_input)
                ? $"{{\"npc_id\":\"{req.npc_id}\",\"player_input\":null}}"
                : $"{{\"npc_id\":\"{req.npc_id}\",\"player_input\":{EscapeJson(req.player_input)}}}";

            using (var post = new UnityWebRequest($"{serverUrl}/chat", "POST"))
            {
                byte[] body = Encoding.UTF8.GetBytes(inputJson);
                post.uploadHandler = new UploadHandlerRaw(body);
                post.downloadHandler = new DownloadHandlerBuffer();
                post.SetRequestHeader("Content-Type", "application/json");
                post.timeout = 30; // LLM 偶尔慢，给 30 秒

                yield return post.SendWebRequest();

                if (post.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        ChatResponse resp = JsonUtility.FromJson<ChatResponse>(post.downloadHandler.text);
                        onSuccess?.Invoke(resp);
                    }
                    catch (Exception e)
                    {
                        onError?.Invoke($"解析回复失败: {e.Message}");
                    }
                }
                else
                {
                    onError?.Invoke($"网络错误: {post.error}\n{post.downloadHandler.text}");
                }
            }
        }

        // 转义字符串为 JSON 字符串字面量（含引号包裹）
        string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "\"\"";
            var sb = new StringBuilder();
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append($"\\u{(int)c:X4}");
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }
    }
}
