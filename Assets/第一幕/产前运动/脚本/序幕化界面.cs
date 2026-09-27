using ChuJian.Merge;
using UnityEngine;
using UnityEngine.UI;

namespace 产前运动
{
    /// <summary>
    /// 序幕化界面：把第一幕场景里已烘焙的界面（对白画布/视频画布/设置画布）按序幕配色重刷。
    /// 这是本幕自己的确定性样式代码——只在已知的三个画布上做固定规则的颜色/按钮处理，
    /// 保留原布局与"医疗圆角"9 宫格图，不做任何全局扫描。
    /// 由 护士顺序讲解.Start 调用；再次运行 温和医疗界面生成器 后依然会自动生效。
    /// </summary>
    public static class 序幕化界面
    {
        public static void 应用(护士顺序讲解 c)
        {
            if (c == null || c.对白 == null) return;

            // 对白画布（含提问输入区）
            var dc = c.对白.GetComponentInParent<Canvas>();
            if (dc != null) ActUIStyle.SkinDialogueTree(dc.transform);

            // 视频画布（含运动引导反馈区）
            if (c.视频 != null && c.视频.screen != null)
            {
                var vc = c.视频.screen.GetComponentInParent<Canvas>();
                if (vc != null) ActUIStyle.SkinDialogueTree(vc.transform);
            }

            // 设置画布
            var settings = c.GetComponent<设置面板控制器>();
            if (settings != null && settings.面板 != null)
                ActUIStyle.SkinDialogueTree(settings.面板.transform);

            // 定向微调：发言者/状态用强调蓝，页码用灰，输入框底用悬停蓝
            if (c.对白.speaker != null) c.对白.speaker.color = ActUIStyle.Accent;
            if (c.对白.pageLabel != null) c.对白.pageLabel.color = ActUIStyle.IdleGray;
            if (c.输入 != null && c.输入.field != null)
            {
                var fimg = c.输入.field.GetComponent<Image>();
                if (fimg != null) fimg.color = ActUIStyle.PanelHover;
                var p = c.输入.field.placeholder as TMPro.TMP_Text;
                if (p != null) p.color = ActUIStyle.IdleGray;
            }
        }
    }
}
