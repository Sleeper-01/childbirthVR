using UnityEngine;
using UnityEngine.UI;

namespace ChanFangVR
{
    /// 步骤3：床旁手册。射线对准 → 扣扳机领取；摇杆左右翻页。
    public class Handbook : PrologueInteractable
    {
        private const float PageWidth = 0.26f;
        private const float PageHeight = 0.36f;
        private const float PageDepth = 0.008f;
        private const float BookWidth = PageWidth * 2f;
        private const float BookHeight = PageHeight;

        // —— 书页文字排版（画布像素单位）——
        // 原来画布只有 260x360 像素贴在 0.26 米宽的书页上，字是糊的；这里把像素密度提到 2 倍，
        // 物理尺寸不变（localScale 相应减半），文字锐利很多。
        private const float PageResW = 520f;
        private const float PageResH = 720f;
        private const float PadX = 42f;          // 左右内边距
        private const float PadTop = 36f;        // 顶部内边距
        private const float PadBottom = 54f;     // 底部内边距（留给页码）
        private const float HeadHeight = 66f;    // 页眉高度
        private const int BodyFontSize = 42;     // 正文字号（超出会自动缩到 BodyFitMin，不再溢出书页）
        private const int BodyFitMin = 28;
        private const int HeadFontSize = 34;
        private const int FooterFontSize = 26;
        private const float BodyLineSpacing = 1.10f;
        // 页眉/页码用比正文更柔的颜色，做出层次
        private static readonly Color HeadColor = new Color(0.16f, 0.24f, 0.34f);
        private static readonly Color FooterColor = new Color(0.46f, 0.51f, 0.57f);

        private GameObject _root;
        private GameObject _cover;
        private GameObject _leftPage;
        private GameObject _rightPage;
        private PageView _leftText;
        private PageView _rightText;

        private Transform _homeParent;
        private Vector3 _homeLocalPos;
        private Quaternion _homeLocalRot;

        private int _page;
        private bool _held;

        public bool Held { get { return _held; } }
        public int Page { get { return _page; } }

        public static Handbook Create(Transform parent, Vector3 localPos)
        {
            var go = new GameObject("Handbook");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var book = go.AddComponent<Handbook>();
            book._root = go;
            book._homeParent = parent;
            book._homeLocalPos = localPos;
            book._homeLocalRot = Quaternion.identity;
            book.Build();
            book.SetPage(0);
            return book;
        }

        private void Build()
        {
            // 整本书的碰撞体（非 trigger）
            var col = _root.AddComponent<BoxCollider>();
            col.size = new Vector3(BookWidth, BookHeight, 0.06f);
            col.center = Vector3.zero;
            col.isTrigger = false;

            // 书壳
            _cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _cover.name = "Cover";
            Object.Destroy(_cover.GetComponent<Collider>());
            _cover.transform.SetParent(_root.transform, false);
            _cover.transform.localPosition = new Vector3(0f, 0f, 0.012f);
            _cover.transform.localScale = new Vector3(BookWidth + 0.02f, BookHeight + 0.02f, 0.012f);
            _cover.GetComponent<Renderer>().sharedMaterial =
                PrologueWorld.Mat(new Color(0.16f, 0.22f, 0.30f), 0.2f, 0f);
            _cover.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            _leftPage = BuildPage("LeftPage", -PageWidth * 0.5f);
            _rightPage = BuildPage("RightPage", PageWidth * 0.5f);
            // 文字面板挂在书根物体下（不是被缩放的页面立方体下，否则会被页面缩放压成极小导致「空白」）
            _leftText = BuildPageView("LeftText", -PageWidth * 0.5f);
            _rightText = BuildPageView("RightText", PageWidth * 0.5f);

            Label = "《待产手册》";
        }

        private GameObject BuildPage(string name, float x)
        {
            var page = GameObject.CreatePrimitive(PrimitiveType.Cube);
            page.name = name;
            Object.Destroy(page.GetComponent<Collider>());
            page.transform.SetParent(_root.transform, false);
            page.transform.localPosition = new Vector3(x, 0f, 0f);
            page.transform.localScale = new Vector3(PageWidth, PageHeight, PageDepth);
            page.GetComponent<Renderer>().sharedMaterial =
                PrologueWorld.Mat(new Color(0.96f, 0.97f, 1.00f), 0.1f, 0f);
            page.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return page;
        }

        // 文字面板挂在书根物体下（不是被缩放的页面立方体下，否则会被页面缩放压成极小导致「空白」）
        // 旋转保持 identity：内容面本就在局部 -Z 侧，正对相机；加 180° 反而会看到背面导致字镜像。
        // 每页 = 页眉（取正文首行）+ 正文 + 页码，正文开启 bestFit 且垂直截断，文字再也不会溢出书页。
        private PageView BuildPageView(string name, float x)
        {
            var view = new PageView();
            var canvasGo = new GameObject(name + "_Canvas");
            canvasGo.transform.SetParent(_root.transform, false);
            canvasGo.transform.localPosition = new Vector3(x, 0f, -0.02f);
            // WorldSpace Canvas 的内容面在**局部 -Z 侧**（HeadCanvas 字幕正是如此：canvas 在相机 +Z 前方、
            // 相机位于其 -Z 侧、identity 旋转即可正常显示）。相机同样位于手册的 -Z 侧，
            // 所以这里**不能再绕 Y 转 180°** —— 那会让相机看到画布背面，字左右镜像（字体反转）。
            canvasGo.transform.localRotation = Quaternion.identity;
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.pixelPerfect = false;
            var rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(PageResW, PageResH);
            rt.localScale = Vector3.one * (PageWidth / PageResW);   // 物理宽度仍为 PageWidth

            // 页眉：顶部居中
            view.Head = MakeLabel(rt, "Head", HeadFontSize, HeadColor, TextAnchor.MiddleCenter,
                new Vector2(PadX, PageResH - PadTop - HeadHeight), new Vector2(-PadX, -PadTop),
                1f, false, 0);
            // 正文：页眉下方到页码上方，bestFit + 垂直截断，永不越界
            view.Body = MakeLabel(rt, "Body", BodyFontSize, PrologueDefs.TextDark, TextAnchor.UpperLeft,
                new Vector2(PadX, PadBottom), new Vector2(-PadX, -(PadTop + HeadHeight)),
                BodyLineSpacing, true, BodyFitMin);
            // 页码：底部居中
            view.Footer = MakeLabel(rt, "Footer", FooterFontSize, FooterColor, TextAnchor.LowerCenter,
                new Vector2(PadX, 14f), new Vector2(-PadX, -(PageResH - PadBottom + 6f)),
                1f, false, 0);
            return view;
        }

        private static Text MakeLabel(Transform parent, string name, int size, Color color,
                                      TextAnchor anchor, Vector2 offsetMin, Vector2 offsetMax,
                                      float lineSpacing, bool bestFit, int fitMin)
        {
            var go = new GameObject(name);
            var r = go.AddComponent<RectTransform>();
            r.SetParent(parent, false);
            r.anchorMin = Vector2.zero;      // 相对页面四边拉伸，用 offset 精确留白
            r.anchorMax = Vector2.one;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.offsetMin = offsetMin;
            r.offsetMax = offsetMax;

            var t = go.AddComponent<Text>();
            t.font = PrologueFont.Get();
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.lineSpacing = lineSpacing;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            // 关键：正文用 Truncate + bestFit，超出就自动缩字号，不会再画到书页外面去
            t.verticalOverflow = bestFit ? VerticalWrapMode.Truncate : VerticalWrapMode.Overflow;
            t.resizeTextForBestFit = bestFit;
            if (bestFit)
            {
                t.resizeTextMinSize = fitMin;
                t.resizeTextMaxSize = size;
            }
            t.supportRichText = false;
            t.raycastTarget = false;
            return t;
        }

        /// 一页的三块文字：页眉 / 正文 / 页码
        private sealed class PageView
        {
            public Text Head;
            public Text Body;
            public Text Footer;

            public void Set(string head, string body, string footer)
            {
                if (Head != null)
                {
                    Head.text = head ?? "";
                    Head.gameObject.SetActive(!string.IsNullOrEmpty(head));
                }
                if (Body != null) Body.text = body ?? "";
                if (Footer != null) Footer.text = footer ?? "";
            }
        }

        public void SetPage(int page)
        {
            _page = Mathf.Clamp(page, 0, 2);
            UpdatePages();
        }

        /// dir: -1 上一页，+1 下一页。返回是否真的翻动了。
        public bool Flip(int dir)
        {
            int next = Mathf.Clamp(_page + dir, 0, 2);
            if (next == _page) return false;
            _page = next;
            UpdatePages();
            return true;
        }

        private void UpdatePages()
        {
            int left = _page * 2;
            int right = left + 1;
            if (_leftText != null) ApplyPageText(_leftText, left);
            if (_rightText != null) ApplyPageText(_rightText, right);
        }

        private static void ApplyPageText(PageView view, int idx)
        {
            string head, body;
            SplitHead(GetPageText(idx), out head, out body);
            view.Set(head, body, "— " + (idx + 1) + " / 6 —");
        }

        /// 把整页文本拆成「页眉 + 正文」：页眉取首行（并去掉【】），其余作为正文。
        private static void SplitHead(string raw, out string head, out string body)
        {
            head = "";
            body = raw ?? "";
            if (string.IsNullOrEmpty(raw)) return;

            int i = raw.IndexOf('\n');
            if (i < 0) { body = raw; return; }

            string first = raw.Substring(0, i).Trim();
            string rest = raw.Substring(i).TrimStart('\n').TrimEnd();
            if (first.Length == 0 || first.Length > 14) { body = raw.Trim(); return; }   // 首行不像标题就整段当正文

            if (first.Length >= 2 && first.StartsWith("【") && first.EndsWith("】"))
                first = first.Substring(1, first.Length - 2);
            head = first;
            body = rest;
        }

        private static string GetPageText(int idx)
        {
            switch (idx)
            {
                case 0: return PrologueDefs.BookSpread0Left;
                case 1: return PrologueDefs.BookSpread0Right;
                case 2: return PrologueDefs.BookSpread1Left;
                case 3: return PrologueDefs.BookSpread1Right;
                case 4: return PrologueDefs.BookSpread2Left;
                case 5: return PrologueDefs.BookSpread2Right;
                default: return "";
            }
        }

        public void SetHeld(bool held)
        {
            if (_held == held) return;
            _held = held;

            if (held)
            {
                var cam = PrologueRun.Cam;
                if (cam == null) return;
                _root.transform.SetParent(cam.transform, false);
                _root.transform.localPosition = new Vector3(0f, -0.06f, 0.62f);
                // 与桌上姿态一致（无额外翻转）：书根 -Z 面即内容面，朝向相机
                _root.transform.localRotation = Quaternion.identity;
            }
            else
            {
                _root.transform.SetParent(_homeParent, false);
                _root.transform.localPosition = _homeLocalPos;
                _root.transform.localRotation = _homeLocalRot;
            }
        }

        /// 记住当前摆放姿态，作为放回原位的目标
        public void RememberHome()
        {
            _homeParent = _root.transform.parent;
            _homeLocalPos = _root.transform.localPosition;
            _homeLocalRot = _root.transform.localRotation;
        }

        protected override void ApplyVisual()
        {
            float s = _hover ? 1.06f : 1f;
            if (_cover != null)
            {
                _cover.transform.localScale =
                    new Vector3((BookWidth + 0.02f) * s, (BookHeight + 0.02f) * s, 0.012f);
            }
            if (_leftPage != null)
            {
                _leftPage.transform.localScale = new Vector3(PageWidth * s, PageHeight * s, PageDepth);
            }
            if (_rightPage != null)
            {
                _rightPage.transform.localScale = new Vector3(PageWidth * s, PageHeight * s, PageDepth);
            }
        }

        protected override void ApplyPulse()
        {
            if (_cover == null) return;
            float k = 1f + Pulse01(3.5f) * 0.05f;
            _cover.transform.localScale =
                new Vector3((BookWidth + 0.02f) * k, (BookHeight + 0.02f) * k, 0.012f);
        }
    }
}
