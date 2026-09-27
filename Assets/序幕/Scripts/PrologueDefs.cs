using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChanFangVR
{
    /// 序章五个步骤（对应设计文档表4）
    public enum PrologueStep
    {
        Boot = 0,        // 启动/白光渐亮
        Calibrate = 1,   // 1 手柄校准
        Intro = 2,       // 2 情境导入
        Handbook = 3,    // 3 领取手册
        Transition = 4,  // 4 转场教学
        Toolbar = 5,     // 5 工具栏认知
        Done = 6         // 序章完成，第一幕解锁
    }

    /// 一句台词。
    /// waitUser = true 表示这句说完停下，等待用户做出本步要求的操作后才继续。
    [Serializable]
    public class DialogueLine
    {
        public string speaker;
        public string text;
        public float extraHold;
        public string voiceKey;
        public bool waitUser;

        public DialogueLine(string speaker, string text, float extraHold = 0.8f,
                            string voiceKey = null, bool waitUser = false)
        {
            this.speaker = speaker;
            this.text = text;
            this.extraHold = extraHold;
            this.voiceKey = voiceKey;
            this.waitUser = waitUser;
        }

        /// 无配音时的建议停留时长（约 16 字/秒）
        public float Duration
        {
            get { return Mathf.Max(2.0f, text.Length * 0.16f) + extraHold; }
        }
    }

    /// 运行期共享引用
    public static class PrologueRun
    {
        public static Camera Cam;
        public static bool VRMode;
    }

    public static class PrologueDefs
    {
        // —— 调色板 ——
        public static readonly Color Accent = new Color(0.31f, 0.76f, 0.97f);
        public static readonly Color Success = new Color(0.55f, 0.80f, 0.55f);
        public static readonly Color Warn = new Color(1.00f, 0.72f, 0.30f);
        public static readonly Color Panel = new Color(0.10f, 0.13f, 0.17f, 0.88f);
        public static readonly Color PanelHover = new Color(0.16f, 0.24f, 0.34f, 0.95f);
        public static readonly Color TextMain = new Color(0.96f, 0.97f, 1.00f);
        public static readonly Color TextDark = new Color(0.13f, 0.16f, 0.20f);
        public static readonly Color IdleGray = new Color(0.55f, 0.58f, 0.62f);

        // —— 任务卡文案（序章 5 个任务）——
        public static readonly string[] TaskTexts =
        {
            "校准手柄",
            "听小安自我介绍",
            "领取并翻阅《待产手册》",
            "体验场景转场",
            "试用工具栏（暂停→继续）",
        };

        // —— 场景卡（步骤4）——
        public static readonly string[] RoomNames = { "待产室", "运动区", "产房" };
        public static readonly string[] RoomDescs =
        {
            "您现在的位置\n温馨待产",
            "分娩球 · 自由体位\n活动专区",
            "迎接宝宝\n即将开放",
        };
        public static readonly Color[] RoomColors =
        {
            new Color(0.23f, 0.64f, 0.63f),
            new Color(0.96f, 0.62f, 0.28f),
            new Color(0.86f, 0.36f, 0.42f),
        };

        // —— 步骤1：手柄校准 ——
        public static readonly DialogueLine[] S1_Lines =
        {
            new DialogueLine("系统", "欢迎使用《VR分娩预演》。请放松地保持半卧位，我们用约3分钟带您熟悉待产室。", 1.0f, "s1_welcome"),
            new DialogueLine("系统", "【校准】将手柄射线对准床尾的白色光点，扣动扳机。", 0.0f, "s1_hint", true),
        };
        public static readonly DialogueLine S1_After =
            new DialogueLine("小安", "手柄校准好啦，很棒！接下来请听我说~", 1.0f, "s1_after");

        // —— 步骤2：情境导入 ——
        public static readonly DialogueLine[] S2_Lines =
        {
            new DialogueLine("小安", "您好，我是您的责任护士小安，今天由我陪您提前熟悉分娩全流程。", 1.2f, "s2_0"),
            new DialogueLine("小安", "您全程保持半卧就好，不用起身。底部的「暂停」「重播」按钮随时可以用。", 1.0f, "s2_1"),
            new DialogueLine("小安", "准备好了的话，我们先来认识一下床旁的《待产手册》吧。", 1.0f, "s2_2"),
        };

        // —— 步骤3：领取手册 ——
        public static readonly DialogueLine S3_Hint =
            new DialogueLine("小安", "《待产手册》就在床旁桌上——用射线对准它，扣动扳机领取。", 0.0f, "s3_hint", true);
        public static readonly DialogueLine S3_Grab =
            new DialogueLine("小安", "拿到啦！朝左右推一下摇杆，就可以翻页。", 0.0f, "s3_grab", true);
        public static readonly DialogueLine S3_Flip =
            new DialogueLine("小安", "这一页是今日流程图：入院评估 → 胎心监护 → 待产活动 → 产程观察 → 转入产房，我们一步一步来。", 1.6f, "s3_flip");

        // —— 步骤4：转场教学 ——
        public static readonly DialogueLine S4_0 =
            new DialogueLine("小安", "不用起身，我们可以随时去任何房间。", 0.6f, "s4_0");
        public static readonly DialogueLine S4_Hint =
            new DialogueLine("系统", "推左摇杆在三张场景卡之间选择，扣动扳机确认前往。", 0.0f, "s4_hint", true);
        public static readonly DialogueLine S4_Done =
            new DialogueLine("小安", "看，画面淡入淡出就完成了转场——全程半卧，无移动感，不易眩晕。", 1.2f, "s4_done");

        // —— 步骤5：工具栏认知 ——
        public static readonly DialogueLine S5_Hint =
            new DialogueLine("小安", "最后认识一下工具栏：请用射线点击底部的「暂停」按钮。", 0.0f, "s5_hint", true);
        public static readonly DialogueLine S5_Resume =
            new DialogueLine("系统", "很好，现在点击「继续」恢复。", 0.0f, "s5_resume", true);
        public static readonly DialogueLine S5_Done =
            new DialogueLine("小安", "序章教学完成！第一幕《入院与评估》已为您解锁。", 1.2f, "s5_done");
        public static readonly DialogueLine S5_End =
            new DialogueLine("小安", "休息片刻，随时可以开始第一幕，我会一直陪着您。", 1.0f, "s5_end");

        // —— 手册内页（0..5，左右两页为一组）——
        public const string BookSpread0Left =
            "【目录】\n\n一 · 今日流程图\n二 · 使用说明\n三 · 待产小贴士\n\n（推摇杆翻页）";

        public const string BookSpread0Right =
            "【今日流程图】\n\n" +
            "① 入院评估\n" +
            "      ↓\n" +
            "② 胎心监护\n" +
            "      ↓\n" +
            "③ 待产活动\n" +
            "      ↓\n" +
            "④ 产程观察\n" +
            "      ↓\n" +
            "⑤ 转入产房\n\n" +
            "我们一步一步来。";

        public const string BookSpread1Left =
            "亲爱的准妈妈：\n\n" +
            "    欢迎来到 VR 分娩预演。\n" +
            "    这本手册将陪您完成今天\n" +
            "的关键流程。\n" +
            "    深呼吸，相信自己——\n" +
            "    我们与您同在。\n\n" +
            "                —— 您的小安";

        public const string BookSpread1Right =
            "【使用说明】\n\n" +
            "领取手册：\n" +
            "  射线对准封面，扣动扳机\n\n" +
            "翻页：\n" +
            "  左右推动摇杆\n\n" +
            "【今日要点】\n" +
            "  半卧位休息，节省体力\n" +
            "  随时呼叫责任护士";

        public const string BookSpread2Left =
            "【待产小贴士】\n\n" +
            "1. 宫缩间歇多休息，节省体力\n" +
            "2. 呼吸放松：吸4秒，呼6秒\n" +
            "3. 可选择自由体位活动\n" +
            "4. 少量多次补充能量\n" +
            "5. 疼痛不适随时呼叫小安";

        public const string BookSpread2Right =
            "【备注】\n\n" +
            "责任护士：小安\n" +
            "呼叫方式：射线指向墙上\n" +
            "呼叫铃，扣动扳机\n" +
            "（后续幕开放）\n\n" +
            "今日流程如有调整，\n以床旁护士告知为准。";
    }
}
