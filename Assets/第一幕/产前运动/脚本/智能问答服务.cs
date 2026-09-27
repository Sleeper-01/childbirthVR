using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace 产前运动
{
    public class 智能问答服务 : MonoBehaviour
    {
        public 运动讲解配置 catalog;
        private readonly List<JObject> history = new List<JObject>();
        private UnityWebRequest active;
        public bool IsBusy { get; private set; }
        public int HistoryTurns => history.Count / 2;
        public bool IsConfigured => catalog != null && !string.IsNullOrWhiteSpace(catalog.API密钥);

        public bool Ask(string question, int exercise, Action<string, bool> completed)
        {
            if (IsBusy) return false;
            if (string.IsNullOrWhiteSpace(question)) { completed("请先输入问题。", false); return false; }
            if (!IsConfigured) { completed("尚未配置对话服务。请在护士对白配置的 Inspector 中填写 API 密钥；你可以继续查看固定讲解。", false); return false; }
            if (!Uri.TryCreate(catalog.endpoint, UriKind.Absolute, out var uri) || uri.Scheme != "https")
            { completed("对话服务地址无效，请配置 HTTPS 接口地址。", false); return false; }
            IsBusy = true;
            StartCoroutine(Request(question, exercise, completed));
            return true;
        }

        private IEnumerator Request(string question, int exercise, Action<string, bool> completed)
        {
            var messages = new JArray { Message("system", catalog.BuildContext(exercise)) };
            foreach (var message in history) messages.Add(message.DeepClone());
            messages.Add(Message("user", question));
            var payload = new JObject { ["model"] = catalog.model, ["messages"] = messages,
                ["stream"] = false, ["max_tokens"] = 700,
                ["thinking"] = new JObject { ["type"] = "disabled" } };
            active = new UnityWebRequest(catalog.endpoint, "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload.ToString(Formatting.None))),
                downloadHandler = new DownloadHandlerBuffer(), timeout = 30, redirectLimit = 0
            };
            active.SetRequestHeader("Content-Type", "application/json");
            active.SetRequestHeader("Authorization", "Bearer " + catalog.API密钥.Trim());
            yield return active.SendWebRequest();
            bool success = false;
            string reply;
            if (active.result != UnityWebRequest.Result.Success)
                reply = FailureMessage(active.responseCode);
            else
            {
                try
                {
                    var json = JObject.Parse(active.downloadHandler.text);
                    reply = (string)json["choices"]?[0]?["message"]?["content"];
                    success = !string.IsNullOrWhiteSpace(reply);
                    if (!success) reply = "暂时没有收到有效回复，请重试。";
                    else if ((string)json["choices"]?[0]?["finish_reason"] == "length") reply += "\n（回复未完整生成，可以请护士继续说明。）";
                }
                catch (Exception) { reply = "服务返回格式异常，请稍后重试。"; }
            }
            active.Dispose(); active = null; IsBusy = false;
            if (success)
            {
                history.Add(Message("user", question)); history.Add(Message("assistant", reply));
                while (history.Count > 20) history.RemoveRange(0, 2);
            }
            completed(reply, success);
        }

        public static string FailureMessage(long code)
        {
            if (code == 401 || code == 403) return "对话服务鉴权失败，请检查 Inspector 中的 API 密钥与接口权限。输入已保留。";
            if (code == 402) return "对话服务额度不足，请检查账户。输入已保留。";
            if (code == 429) return "请求过于频繁，请稍后重试。输入已保留。";
            if (code == 0) return "网络连接失败或请求超时，请检查网络后重试。输入已保留。";
            return "对话服务暂不可用，请检查接口与模型配置后重试。输入已保留。";
        }
        private static JObject Message(string role, string content) => new JObject { ["role"] = role, ["content"] = content };
        public void Cancel()
        {
            StopAllCoroutines();
            if (active != null) { active.Abort(); active.Dispose(); active = null; }
            IsBusy = false;
        }
        private void OnDisable() { Cancel(); }
    }
}
