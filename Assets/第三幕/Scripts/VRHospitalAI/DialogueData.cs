using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRHospitalAI
{
    // ============ 服务端返回的数据结构 ============
    [Serializable]
    public class ChatResponse
    {
        public string npc_id;      // "doctor" / "nurse" / "anesthesiologist"
        public string name;        // 角色名
        public string reply;       // 台词
        public string emotion;     // 情绪标签
        public List<string> options = new List<string>(); // 3个选项
    }

    // ============ 发送给服务端的数据 ============
    [Serializable]
    public class ChatRequest
    {
        public string npc_id;
        public string player_input; // null 表示开始对话
    }

    // ============ 流式事件（服务器逐行推送）============
    [Serializable]
    public class StreamEvent
    {
        public string type;       // "reply" / "done" / "error"
        public string text;       // reply 的增量文本
        public string name;       // done 事件的角色名
        public string emotion;    // done 事件的情绪
        public List<string> options; // done 事件的选项
        public string message;    // error 事件的错误信息
    }
}
