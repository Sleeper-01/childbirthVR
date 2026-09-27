using UnityEngine;
using UnityEngine.UI;

namespace VRHospitalAI.ThirdAct
{
    /// <summary>
    /// 第三幕界面样式：与序幕(ChanFangVR.PrologueDefs)同源的配色、圆角面板图与中文字体。
    /// 本程序集独立编译，无法引用合并工程的全局样式类，故在此按同值自带一份。
    /// 所有 UI 由 ThirdActAutoBuilder 代码生成，直接使用本样式，运行时不再做任何补扫。
    /// </summary>
    public static class ProStyle
    {
        // —— 调色板（与序幕 PrologueDefs 同值）——
        public static readonly Color Accent = new Color(0.31f, 0.76f, 0.97f);
        public static readonly Color Success = new Color(0.55f, 0.80f, 0.55f);
        public static readonly Color Warn = new Color(1.00f, 0.72f, 0.30f);
        public static readonly Color Danger = new Color(0.95f, 0.42f, 0.42f);
        public static readonly Color Panel = new Color(0.10f, 0.13f, 0.17f, 0.88f);
        public static readonly Color PanelHover = new Color(0.16f, 0.24f, 0.34f, 0.95f);
        public static readonly Color ButtonBg = new Color(0.16f, 0.21f, 0.28f, 0.96f);
        public static readonly Color TextMain = new Color(0.96f, 0.97f, 1.00f);
        public static readonly Color TextDark = new Color(0.13f, 0.16f, 0.20f);
        public static readonly Color IdleGray = new Color(0.55f, 0.58f, 0.62f);
        public static readonly Color Backdrop = new Color(0.07f, 0.09f, 0.12f);

        // —— 版式刻度（与全局 ActUIStyle 一致）——
        public const int FontHeading = 28;   // 面板/卡片标题
        public const int FontBody = 24;      // 正文/字幕
        public const int FontCaption = 20;   // 说明/状态
        public const int FontMicro = 17;     // 辅助小字

        private static Font _font;

        /// <summary>系统中文字体（微软雅黑优先，黑体兜底），与序幕同一优先级</summary>
        public static Font Font
        {
            get
            {
                if (_font != null) return _font;
                try
                {
                    string[] prefs = { "Microsoft YaHei", "Microsoft YaHei UI", "微软雅黑", "DengXian", "等线", "SimHei", "黑体" };
                    var names = Font.GetOSInstalledFontNames();
                    if (names != null)
                        foreach (var p in prefs)
                            foreach (var n in names)
                                if (string.Equals(n, p, System.StringComparison.OrdinalIgnoreCase))
                                {
                                    _font = Font.CreateDynamicFontFromOSFont(n, 32);
                                    if (_font != null) return _font;
                                }
                }
                catch { }
                _font = Resources.Load<Font>("Fonts/simhei");
                if (_font == null) { try { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { } }
                if (_font == null) { try { _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
                return _font;
            }
        }

        private static Sprite _rounded;
        private static Sprite _roundedSmall;

        /// <summary>9 宫格圆角面板图（16px 边框，任意尺寸面板都有干净圆角）</summary>
        public static Sprite Rounded
        {
            get
            {
                if (_rounded != null) return _rounded;
                _rounded = MakeRounded(14, 16);
                return _rounded;
            }
        }

        /// <summary>小圆角（10px 边框 12）——按钮用</summary>
        public static Sprite RoundedSmall
        {
            get
            {
                if (_roundedSmall != null) return _roundedSmall;
                _roundedSmall = MakeRounded(10, 12);
                return _roundedSmall;
            }
        }

        private static Sprite MakeRounded(int r, int border)
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(r - x, 0, x - (size - 1 - r));
                    float dy = Mathf.Max(r - y, 0, y - (size - 1 - r));
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(r + 0.5f - d));
                }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                100, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        private static Texture2D _floorTex;

        /// <summary>程序化木纹地板纹理（浅色底供 tint 相乘，横向板缝 + 轻噪点），消除大面积纯色感</summary>
        public static Texture2D FloorTexture
        {
            get
            {
                if (_floorTex != null) return _floorTex;
                const int size = 256;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var px = new Color[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float v = 0.94f;
                        v *= ((y / 64) % 2 == 0) ? 1.0f : 0.96f;          // 相邻板条明度差
                        if (y % 64 == 0 || y % 64 == 1) v *= 0.80f;       // 横向板缝
                        if (x % 128 == 0) v *= 0.88f;                     // 竖向拼缝
                        float n = Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f;
                        n -= Mathf.Floor(n);
                        v *= 0.97f + n * 0.06f;                            // 细噪点
                        px[y * size + x] = new Color(v, v * 0.99f, v * 0.97f, 1f);
                    }
                }
                tex.SetPixels(px);
                tex.Apply();
                tex.wrapMode = TextureWrapMode.Repeat;
                _floorTex = tex;
                return tex;
            }
        }

        /// <summary>把一张 Image 变成序幕面板：深色半透明 + 圆角 9 宫格</summary>
        public static void MakePanel(Image img, float alpha)
        {
            if (img == null) return;
            img.sprite = Rounded;
            img.type = Image.Type.Sliced;
            img.color = new Color(Panel.r, Panel.g, Panel.b, alpha);
        }

        /// <summary>把一张 Button 的 Image 变成序幕按钮：小圆角深底 + 顶部受光细条 + 悬停增亮</summary>
        public static void MakeButton(Image img, Button btn)
        {
            if (img == null) return;
            img.sprite = RoundedSmall;
            img.type = Image.Type.Sliced;
            img.color = ButtonBg;
            if (btn == null) return;
            var col = btn.colors;
            col.highlightedColor = new Color(1.16f, 1.16f, 1.20f);
            col.pressedColor = new Color(0.85f, 0.85f, 0.90f);
            col.selectedColor = Color.white;
            col.disabledColor = new Color(1f, 1f, 1f, 0.45f);
            btn.colors = col;
            // 顶缘受光细条（材质规范：高光只在顶部）
            var stripGO = new GameObject("TopLight", typeof(RectTransform));
            stripGO.transform.SetParent(img.transform, false);
            var strip = stripGO.AddComponent<Image>();
            strip.color = new Color(1f, 1f, 1f, 0.06f);
            strip.raycastTarget = false;
            var srt = (RectTransform)stripGO.transform;
            srt.anchorMin = new Vector2(0.08f, 1f);
            srt.anchorMax = new Vector2(0.92f, 1f);
            srt.pivot = new Vector2(0.5f, 1f);
            srt.anchoredPosition = new Vector2(0, -1);
            srt.sizeDelta = new Vector2(0, 2);
        }
    }
}
