using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChuJian.Merge
{
    /// <summary>
    /// 序幕风格共享库：与序幕(ChanFangVR.PrologueDefs)同值的调色板、圆角/圆点图元、系统中文字体。
    /// 各幕 UI 一律在自己的生成/构建代码里直接使用本样式（不做任何运行时启发式补扫）：
    ///   · 第三幕 ThirdActAutoBuilder → 用自带 ProStyle（同值副本，因独立程序集无法引用本类）
    ///   · 第一幕 温和医疗界面生成器 + 护士顺序讲解 → 直接引用本类
    ///   · 尾声 各弹窗组件 → 在 Awake 里调用 SkinDialogueTree 只刷自己的面板
    /// 本类仅保留：字体统一（每幕一次性）与全局 MSAA，不再有周期补扫。
    /// </summary>
    public static class ActUIStyle
    {
        // —— 调色板（与序幕 ChanFangVR.PrologueDefs 同值）——
        // 色彩角色规范（全工程只允许按角色取色，不允许随手写颜色）：
        //   背景  Backdrop / Panel     深色底、面板底
        //   主色  Accent  蓝           可交互、链接、进度、当前项
        //   辅助  Success 绿           成功、已完成
        //   强调  Warn    橙           面板标题、进行中、注意力引导
        //   交互  PanelHover           悬停/选中底色
        //   警告  Danger  红           错误、疼痛/宫缩等医疗警示（序幕未用红，作为扩展角色保留）
        //   正文  TextMain / TextDark / IdleGray
        // 写实道具的白名单色（监护仪绿等）不属于 UI 配色，不受此规范约束。
        public static readonly Color Accent = new Color(0.31f, 0.76f, 0.97f);
        public static readonly Color Success = new Color(0.55f, 0.80f, 0.55f);
        public static readonly Color Warn = new Color(1.00f, 0.72f, 0.30f);
        // 序幕色板没有红；医疗警示/错误语义保留一个红，其余颜色全部归入序幕色板
        public static readonly Color Danger = new Color(0.95f, 0.42f, 0.42f);
        public static readonly Color Panel = new Color(0.10f, 0.13f, 0.17f, 0.88f);
        public static readonly Color PanelHover = new Color(0.16f, 0.24f, 0.34f, 0.95f);
        public static readonly Color ButtonBg = new Color(0.16f, 0.21f, 0.28f, 0.96f);
        public static readonly Color TextMain = new Color(0.96f, 0.97f, 1.00f);
        public static readonly Color TextDark = new Color(0.13f, 0.16f, 0.20f);
        public static readonly Color IdleGray = new Color(0.55f, 0.58f, 0.62f);
        public static readonly Color Backdrop = new Color(0.07f, 0.09f, 0.12f);
        public static readonly Color Surface = new Color(0.13f, 0.16f, 0.21f);   // 比 Background 稍亮的层级面

        // —— 版式刻度（全工程 UI 统一从这里取字号，保证界面一致）——
        public const int FontHero = 56;      // 主菜单 Hero 标题（最大视觉锚点）
        public const int FontDisplay = 40;   // 面板大标题（帮助/进度地图）
        public const int FontHeading = 28;   // 卡片标题 / 按钮文字
        public const int FontBody = 24;      // 正文 / 列表行 / HUD
        public const int FontSmall = 20;     // 次要说明 / 状态
        public const int FontMicro = 17;     // 辅助小字 / 描述行

        /// <summary>全局统一中文字体（序幕同款）</summary>
        public static Font UiFont
        {
            get { return ChanFangVR.PrologueFont.Get(); }
        }

        private static Sprite s_roundS, s_roundM, s_roundL;
        private static Sprite s_circle;
        private static Sprite s_blob;
        private static TMP_FontAsset s_tmpFont;
        private static bool s_tmpTried;

        /// <summary>用系统中文字体运行时生成的 TMP 字体资产（失败返回 null，保留各幕原字体）</summary>
        public static TMP_FontAsset TmpFont
        {
            get
            {
                if (s_tmpTried) return s_tmpFont;
                s_tmpTried = true;
                try { s_tmpFont = TMP_FontAsset.CreateFontAsset(ChanFangVR.PrologueFont.Get()); }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[ActUIStyle] TMP 运行时字体生成失败，保留原字体：" + e.Message);
                }
                return s_tmpFont;
            }
        }

        /// <summary>
        /// 面板顶缘受光条（材质规范：高光只允许出现在顶部，模拟顶灯；克制到 2px、低透明度）。
        /// 用于大中型常驻面板，小按钮不要使用。
        /// </summary>
        public static void AddTopLight(RectTransform panel, float alpha = 0.05f)
        {
            if (panel == null) return;
            var go = new GameObject("TopLight", typeof(RectTransform));
            go.transform.SetParent(panel, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, alpha);
            img.raycastTarget = false;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.06f, 1f);
            rt.anchorMax = new Vector2(0.94f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -1);
            rt.sizeDelta = new Vector2(0, 2);
        }

        /// <summary>圆角规范（三档，全工程统一）：
        /// 小型按钮 RoundSmallSprite(r10) / 普通面板 RoundSprite(r14) / 大型主面板 RoundLargeSprite(r20)。</summary>
        public static Sprite RoundSprite { get { return GetRounded(14, 16); } }

        public static Sprite RoundSmallSprite { get { return GetRounded(10, 12); } }

        public static Sprite RoundLargeSprite { get { return GetRounded(20, 24); } }

        private static Sprite GetRounded(int r, int border)
        {
            Sprite cached = r == 10 ? s_roundS : r == 20 ? s_roundL : s_roundM;
            if (cached != null) return cached;
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(r - x, 0, x - (size - 1 - r));
                    float dy = Mathf.Max(r - y, 0, y - (size - 1 - r));
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(r + 0.5f - d));
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            var sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                100, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            if (r == 10) s_roundS = sp; else if (r == 20) s_roundL = sp; else s_roundM = sp;
            return sp;
        }

        private static Sprite s_vig;

        /// <summary>全屏暗角图（边缘渐暗、中心全透）——主菜单底与全局转场共用</summary>
        public static Sprite VignetteSprite
        {
            get
            {
                if (s_vig != null) return s_vig;
                const int size = 256;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var px = new Color[size * size];
                float r = size * 0.5f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x - r, dy = y - r;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01(1f - d / r);
                        px[y * size + x] = new Color(0f, 0f, 0f, 1f - a * a * 0.9f);
                    }
                }
                tex.SetPixels(px);
                tex.Apply();
                s_vig = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
                return s_vig;
            }
        }

        /// <summary>
        /// Elevation 分级（v2：只用向下的投影区分层级，不再使用四周白描边——
        /// 高光只允许出现在面板顶缘，由需要的地方自行添加细条）：
        /// L1 普通信息=无 / L2 卡片=轻投影 / L3 重要交互=中投影 / L4 主菜单与核心弹窗=明显投影。
        /// </summary>
        public static void ApplyElevation(Image img, int level)
        {
            if (img == null) return;
            var shadow = img.GetComponent<Shadow>();
            if (shadow == null) shadow = img.gameObject.AddComponent<Shadow>();
            switch (Mathf.Clamp(level, 1, 4))
            {
                case 1:
                    shadow.enabled = false;
                    break;
                case 2:
                    shadow.effectColor = new Color(0f, 0f, 0f, 0.20f);
                    shadow.effectDistance = new Vector2(0f, -2f);
                    shadow.enabled = true;
                    break;
                case 3:
                    shadow.effectColor = new Color(0f, 0f, 0f, 0.26f);
                    shadow.effectDistance = new Vector2(0f, -3f);
                    shadow.enabled = true;
                    break;
                default:
                    shadow.effectColor = new Color(0f, 0f, 0f, 0.32f);
                    shadow.effectDistance = new Vector2(0f, -5f);
                    shadow.enabled = true;
                    break;
            }
        }

        /// <summary>径向柔光图：仅用于光晕/光点这类需要软边渐隐的元素（不做面板）</summary>
        public static Sprite SoftBlobSprite
        {
            get
            {
                if (s_blob == null) s_blob = MakeRadialSprite(256);
                return s_blob;
            }
        }

        private static Texture2D s_quadRound;

        /// <summary>3D 世界面板的圆角纹理（小相对圆角，供网格 Quad 的 UV 0-1 直贴，与 UI 圆角语言一致）</summary>
        public static Texture2D QuadRoundedTexture
        {
            get
            {
                if (s_quadRound != null) return s_quadRound;
                const int size = 128, r = 6;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var px = new Color[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = Mathf.Max(r - x, 0, x - (size - 1 - r));
                        float dy = Mathf.Max(r - y, 0, y - (size - 1 - r));
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01(r + 0.5f - d);
                        px[y * size + x] = new Color(1f, 1f, 1f, a);
                    }
                }
                tex.SetPixels(px);
                tex.Apply();
                s_quadRound = tex;
                return tex;
            }
        }

        /// <summary>硬边圆点图（准星/圆点同款）</summary>
        public static Sprite CircleSprite
        {
            get
            {
                if (s_circle == null) s_circle = MakeCircleSprite(128);
                return s_circle;
            }
        }

        /// <summary>
        /// 定向面板刷：只刷给定根节点子树内的图（面板底→Panel 圆角、按钮→序幕按钮、文字→TextMain）。
        /// 供各幕自己的组件在构建/唤醒时调用（如尾声弹窗），不是全局扫描。
        /// </summary>
        public static void SkinDialogueTree(Transform root)
        {
            if (root == null) return;
            var images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                var img = images[i];
                if (img == null) continue;
                var btn = img.GetComponent<Button>();
                if (btn != null)
                {
                    img.sprite = RoundSprite;
                    img.type = Image.Type.Sliced;
                    img.color = ButtonBg;
                    var col = btn.colors;
                    col.highlightedColor = new Color(1.16f, 1.16f, 1.20f);
                    col.pressedColor = new Color(0.82f, 0.82f, 0.88f);
                    col.selectedColor = Color.white;
                    col.disabledColor = new Color(1f, 1f, 1f, 0.45f);
                    btn.colors = col;
                    continue;
                }
                img.sprite = RoundSprite;
                img.type = Image.Type.Sliced;
                img.color = Panel;
            }
            var texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] == null) continue;
                if (TmpFont != null) texts[i].font = TmpFont;   // 字体统一（皮肤重刷也换字体，避免霞鹜/思源残留）
                texts[i].color = TextMain;
            }
            var legacy = root.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < legacy.Length; i++)
                if (legacy[i] != null) legacy[i].color = TextMain;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoRun()
        {
            if (Object.FindObjectOfType<ActUIStyleRunner>() != null) return;
            var go = new GameObject("~ActUIStyle");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<ActUIStyleRunner>();
        }

        // ———— 程序化图（与序幕 PrologueWorld 同法，保证视觉一致）———

        private static Sprite MakeRadialSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - r, dy = y - r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d / r));
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private static Sprite MakeCircleSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - r, dy = y - r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    px[y * size + x] = new Color(1f, 1f, 1f, d < r * 0.9f ? 1f : 0f);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
        /// <summary>面板动效（F1）：出现=0.12s 淡入+0.97→1 缩放；消失=0.08s 淡出。短促克制，不影响操作。</summary>
        public static void ShowPanel(GameObject panel, bool show)
        {
            if (panel == null) return;
            var runner = Object.FindObjectOfType<ActUIStyleRunner>();
            if (runner == null) { panel.SetActive(show); return; }
            runner.StartCoroutine(ShowPanelCo(panel, show));
        }

        private static IEnumerator ShowPanelCo(GameObject panel, bool show)
        {
            var cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.AddComponent<CanvasGroup>();
            var rt = panel.transform as RectTransform;
            Vector3 target = rt != null ? rt.localScale : Vector3.one;
            if (show)
            {
                panel.SetActive(true);
                if (rt != null) rt.localScale = target * 0.97f;
                float t = 0f;
                while (t < 1f)
                {
                    t += Time.unscaledDeltaTime / 0.12f;
                    cg.alpha = Mathf.Clamp01(t);
                    if (rt != null) rt.localScale = Vector3.Lerp(target * 0.97f, target, Mathf.Clamp01(t));
                    yield return null;
                }
                cg.alpha = 1f;
                if (rt != null) rt.localScale = target;
            }
            else
            {
                float t = 0f;
                while (t < 1f && panel.activeSelf)
                {
                    t += Time.unscaledDeltaTime / 0.08f;
                    cg.alpha = 1f - Mathf.Clamp01(t);
                    yield return null;
                }
                panel.SetActive(false);
                cg.alpha = 1f;
            }
        }
    }

    /// <summary>
    /// 统一交互反馈三件套（C2）——全工程所有 UGUI 按钮共用同一语言：
    ///   指向：底色增亮由 Button.colorTint 承担，本组件补 1.04 缩放；
    ///   按下：0.97 缩放；
    ///   成功：点击后一次 1.03 短脉冲（0.15s）。
    /// 全部基于 unscaledTime，暂停时依然自然。由 ProButton/MakeButton/主菜单卡片自动挂载。
    /// </summary>
    public class UIButtonFeedback : MonoBehaviour,
        UnityEngine.EventSystems.IPointerEnterHandler,
        UnityEngine.EventSystems.IPointerExitHandler,
        UnityEngine.EventSystems.IPointerDownHandler,
        UnityEngine.EventSystems.IPointerUpHandler
    {
        [Tooltip("悬停缩放（大按钮 1.04，导航行等克制元素可调小）")]
        public float hoverScale = 1.04f;
        [Tooltip("按下缩放")]
        public float pressScale = 0.97f;
        [Tooltip("可选：悬停时提亮的底图（透明底导航行用，普通按钮留空）")]
        public Image hoverBg;
        [Tooltip("悬停底图提亮到的颜色")]
        public Color hoverColor = new Color(1f, 1f, 1f, 0.5f);

        private Vector3 _base;
        private bool _baseReady;
        private float _hover = 1f;
        private float _pulse;
        private Color _bgBase;
        private bool _bgInit;
        private bool _bgHover;

        private void Awake()
        {
            _base = transform.localScale;
            _baseReady = true;
            if (hoverBg != null) _bgBase = hoverBg.color;
            var btn = GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(OnClicked);
        }

        private void OnClicked() { _pulse = 1f; }

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData d) { _hover = hoverScale; _bgHover = true; }
        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData d) { _hover = 1f; _bgHover = false; }
        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData d) { _hover = pressScale; }
        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData d) { _hover = 1.02f; }

        private void Update()
        {
            if (!_baseReady) return;
            if (_pulse > 0f) _pulse = Mathf.Max(0f, _pulse - Time.unscaledDeltaTime / 0.15f);
            float k = _hover + _pulse * 0.03f;
            Vector3 target = new Vector3(_base.x * k, _base.y * k, _base.z);
            transform.localScale = Vector3.Lerp(transform.localScale, target, Time.unscaledDeltaTime * 14f);
            if (hoverBg != null)
            {
                if (!_bgInit) { _bgBase = hoverBg.color; _bgInit = true; }
                var goal = _bgHover ? hoverColor : _bgBase;
                hoverBg.color = Color.Lerp(hoverBg.color, goal, Time.unscaledDeltaTime * 12f);
            }
        }
    }

    /// <summary>
    /// ActUIStyle 驱动器：只负责全局 MSAA 与每幕一次性的字体统一
    /// （场景烘焙的 Text 若还是 Arial 等缺中文字体，换成系统中文字体；不改任何颜色与结构）。
    /// </summary>
    public class ActUIStyleRunner : MonoBehaviour
    {
        private bool _lateDone;

        private void Start()
        {
            // VR 质量设置由 VRPerformanceProfile 统一管理，避免此处与序幕/全局流程互相覆盖。
            FixFonts("场景加载");
        }

        private void Update()
        {
            // 各幕运行时代码建的 UI（如第三幕 ThirdActAutoBuilder 在 Start 构建）晚于首扫，补一次即止
            if (!_lateDone && Time.realtimeSinceStartup > 3f)
            {
                _lateDone = true;
                FixFonts("3s一次性补齐");
            }
        }

        private static void FixFonts(string reason)
        {
            var f = ChanFangVR.PrologueFont.Get();
            if (f == null) return;
            int changed = 0;
            foreach (var t in Object.FindObjectsOfType<Text>(true))
                if (t != null && t.font != f) { t.font = f; changed++; }
            foreach (var c in Object.FindObjectsOfType<Canvas>(true))
            {
                if (c == null || c.gameObject.scene.name == "Prologue") continue;
                foreach (var t in c.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (t == null) continue;
                    var f2 = ActUIStyle.TmpFont;
                    if (f2 != null && t.font != f2) { t.font = f2; changed++; }
                }
            }
            if (changed > 0)
                Debug.Log("[ActUIStyle] " + reason + "：统一字体 " + changed + " 个文本");
        }
    }
}
