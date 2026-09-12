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

        private GameObject _root;
        private GameObject _cover;
        private GameObject _leftPage;
        private GameObject _rightPage;
        private Text _leftText;
        private Text _rightText;

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
            _leftText = BuildText("LeftText", -PageWidth * 0.5f);
            _rightText = BuildText("RightText", PageWidth * 0.5f);

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

        // 文字面板：挂在书根（不被页面缩放压扁），置于页面 -Z 侧、比页面更靠前
        // （z=-0.02 < 页面前表面 -0.004），避免被书页/书壳立方体遮挡导致「空白 / 看不见字」。
        // 旋转保持 identity：内容面本就在局部 -Z 侧，正对相机；加 180° 反而会看到背面导致字镜像。
        private Text BuildText(string name, float x)
        {
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
            rt.sizeDelta = new Vector2(PageWidth * 1000f, PageHeight * 1000f);
            rt.localScale = Vector3.one * 0.001f;

            return PrologueUI.MakeText(rt, "", 26, PrologueDefs.TextDark, TextAnchor.UpperLeft,
                Vector2.zero, new Vector2(PageWidth * 1000f - 28f, PageHeight * 1000f - 24f));
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
            if (_leftText != null) _leftText.text = GetPageText(left);
            if (_rightText != null) _rightText.text = GetPageText(right);
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
