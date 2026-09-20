using UnityEngine;
using UnityEngine.UI;

namespace ChanFangVR
{
    /// <summary>
    /// 桌面鼠标指针：把 VR 里的「激光笔」在桌面上可视化出来。
    ///
    /// 背景：桌面模式本来就已经是「鼠标移动 = 瞄准、左键 = 扳机」，
    /// 但屏幕上没有任何指示，玩家不知道自己正指着什么，感觉完全不像 VR 的射线交互。
    /// 本类补的就是这层可视化：
    ///   · 跟随鼠标的圆环 + 圆点
    ///   · 指到可交互物（工具栏按钮 / 手册 / 场景卡 / 护士 / 光点）时圆环放大变色，
    ///     并在下方显示目标名字 —— 等价于 VR 里激光打中目标后的高亮
    ///   · 按下左键时圆环收缩一下，给出点击反馈
    ///   · VR 模式下整体隐藏（头显里不需要，也不该挡视线）
    ///
    /// 用 ScreenSpaceOverlay 画布画，所有元素 raycastTarget=false，
    /// 不带 GraphicRaycaster，因此绝不会拦截任何点击。
    /// </summary>
    public class DesktopPointer : MonoBehaviour
    {
        private const float RingIdle = 24f;
        private const float RingHover = 42f;
        private const float DotIdle = 4f;
        private const float DotHover = 8f;
        private const float ClickDecay = 6f;      // 点击脉冲衰减速度
        private const float LerpHover = 12f;      // 悬停过渡速度

        private PrologueInput _input;
        private Canvas _canvas;
        private RectTransform _root;
        private RectTransform _ringRt;
        private RectTransform _dotRt;
        private Image _ring;
        private Image _dot;
        private Image _labelBg;
        private Text _label;

        private float _ringSize = RingIdle;
        private float _dotSize = DotIdle;
        private float _hoverK;      // 0=未指向 1=指向可交互物
        private float _click;       // 1→0 的点击脉冲
        private bool _ownRingSprite;   // 环形 Sprite 是否由本对象创建（共享的不能销毁）

        public static DesktopPointer Create(PrologueInput input)
        {
            var go = new GameObject("DesktopPointer");
            var p = go.AddComponent<DesktopPointer>();
            p._input = input;
            p.Build();
            return p;
        }

        private void Build()
        {
            // Canvas 直接挂在本对象上：ScreenSpaceOverlay 画布嵌套在普通物体下会有额外开销与警告
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 32767;          // 画在最上层
            _canvas.pixelPerfect = false;
            // 刻意不挂 GraphicRaycaster：本画布只负责显示，不参与任何命中测试

            // 指针根：锚在屏幕左下角、pivot 在自身左下，anchoredPosition 直接就是鼠标像素坐标
            _root = gameObject.GetComponent<RectTransform>();
            if (_root == null) _root = gameObject.AddComponent<RectTransform>();
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.zero;
            _root.pivot = Vector2.zero;
            _root.sizeDelta = Vector2.zero;
            _root.anchoredPosition = Vector2.zero;

            // 圆环（PrologueWorld.RingTex 是现成的环形贴图，这里包成 Sprite 复用）
            var ringTex = PrologueWorld.RingTex;
            Sprite ringSprite = null;
            if (ringTex != null)
            {
                ringSprite = Sprite.Create(ringTex, new Rect(0, 0, ringTex.width, ringTex.height),
                                           new Vector2(0.5f, 0.5f));
                _ownRingSprite = true;
            }
            else
            {
                // 兜底：没有环形贴图就用实心圆，只是视觉上差一点，功能不受影响
                ringSprite = PrologueWorld.CircleSprite;
            }

            var ringGo = MakeImg("Ring", ringSprite, new Vector2(RingIdle, RingIdle),
                                 new Color(1f, 1f, 1f, 0.55f));
            _ringRt = ringGo.GetComponent<RectTransform>();
            _ring = ringGo.GetComponent<Image>();

            var dotGo = MakeImg("Dot", PrologueWorld.CircleSprite, new Vector2(DotIdle, DotIdle),
                                new Color(1f, 1f, 1f, 0.95f));
            _dotRt = dotGo.GetComponent<RectTransform>();
            _dot = dotGo.GetComponent<Image>();

            // 悬停时显示的目标名（例如「帮助」「换房间」「待产手册」「产房」）
            _labelBg = PrologueUI.MakeImage(_root, new Color(0.06f, 0.10f, 0.14f, 0.88f),
                new Vector2(0f, 40f), new Vector2(240, 46), PrologueWorld.RoundSprite, true);
            _labelBg.gameObject.SetActive(false);
            _label = PrologueUI.MakeText(_labelBg.transform, "", 26, PrologueDefs.TextMain,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(230, 42));
        }

        private GameObject MakeImg(string name, Sprite sprite, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(_root, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;      // 以鼠标点为原点
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return go;
        }

        private void Update()
        {
            bool vr = _input != null && _input.VRMode;
            if (_canvas != null && _canvas.enabled == vr) _canvas.enabled = !vr;
            if (vr) return;

            // 位置：直接跟随鼠标像素坐标
            var mouse = DesktopInput.mousePosition;
            if (_root != null) _root.anchoredPosition = new Vector2(mouse.x, mouse.y);

            // 是否指向可交互物（等价于 VR 射线命中）
            var hovered = _input != null ? _input.Hovered : null;
            bool on = hovered != null;
            _hoverK = Mathf.MoveTowards(_hoverK, on ? 1f : 0f, Time.unscaledDeltaTime * LerpHover);

            // 点击脉冲
            if (DesktopInput.GetMouseButtonDown(0)) _click = 1f;
            _click = Mathf.MoveTowards(_click, 0f, Time.unscaledDeltaTime * ClickDecay);

            float pulse = 1f - _click * 0.35f;                 // 点下时缩一下
            _ringSize = Mathf.Lerp(RingIdle, RingHover, _hoverK) * pulse;
            _dotSize = Mathf.Lerp(DotIdle, DotHover, _hoverK) * pulse;
            if (_ringRt != null) _ringRt.sizeDelta = new Vector2(_ringSize, _ringSize);
            if (_dotRt != null) _dotRt.sizeDelta = new Vector2(_dotSize, _dotSize);

            if (_ring != null)
            {
                var c = Color.Lerp(new Color(1f, 1f, 1f, 0.55f), PrologueDefs.Warn, _hoverK);
                c.a = Mathf.Lerp(0.55f, 0.95f, Mathf.Max(_hoverK, _click));
                _ring.color = c;
            }
            if (_dot != null)
            {
                var c = Color.Lerp(new Color(1f, 1f, 1f, 0.95f), PrologueDefs.Accent, _hoverK);
                _dot.color = c;
            }

            // 目标名标签
            if (_labelBg != null)
            {
                bool show = on && !string.IsNullOrEmpty(hovered.Label);
                if (_labelBg.gameObject.activeSelf != show) _labelBg.gameObject.SetActive(show);
                if (show && _label != null) _label.text = hovered.Label;
            }
        }

        private void OnDestroy()
        {
            // 只销毁自己 new 出来的那个 Sprite；共享的 CircleSprite 绝不能销毁
            if (_ownRingSprite && _ring != null && _ring.sprite != null) Destroy(_ring.sprite);
        }
    }
}
