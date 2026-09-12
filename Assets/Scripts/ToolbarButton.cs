using UnityEngine;
using UnityEngine.UI;

namespace ChanFangVR
{
    public enum ToolbarRole { Help, Replay, Pause, Comfort }

    /// 工具栏 / 面板按钮：射线悬停高亮，扣扳机（或鼠标点击）触发
    public class ToolbarButton : PrologueInteractable
    {
        public ToolbarRole Role;

        private Image _bg;
        private Text _label;
        private Outline _outline;
        private Color _baseColor;
        private bool _highlight;

        public static ToolbarButton Create(Transform parent, string label, Vector2 anchoredPos,
                                           Vector2 size, ToolbarRole role)
        {
            var go = new GameObject("Btn_" + role);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.sprite = PrologueWorld.RoundSprite;
            img.type = Image.Type.Sliced;
            img.color = new Color(0.16f, 0.21f, 0.28f, 0.96f);
            img.raycastTarget = false;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(PrologueDefs.Warn.r, PrologueDefs.Warn.g, PrologueDefs.Warn.b, 0f);
            outline.effectDistance = new Vector2(2.5f, -2.5f);

            if (!string.IsNullOrEmpty(label))
            {
                PrologueUI.MakeText(rt, label, 30, PrologueDefs.TextMain,
                    TextAnchor.MiddleCenter, Vector2.zero, size);
            }

            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = false;
            PrologueUI.FitCollider(rt, col);

            var btn = go.AddComponent<ToolbarButton>();
            btn._bg = img;
            btn._outline = outline;
            btn._baseColor = img.color;
            btn.Label = label;
            btn.Role = role;
            return btn;
        }

        public void SetLabel(string s)
        {
            Label = s;
            if (_label == null) _label = GetComponentInChildren<Text>(true);
            if (_label != null) _label.text = s;
        }

        public void SetHighlight(bool on)
        {
            _highlight = on;
            if (_bg != null) _bg.color = on ? PrologueDefs.PanelHover : _baseColor;
        }

        /// 隐藏按钮底板，只保留命中区域（用于覆盖在文字上的透明热区）
        public void MakeTransparent()
        {
            if (_bg != null)
            {
                _baseColor = new Color(0f, 0f, 0f, 0f);
                _bg.color = _baseColor;
            }
        }

        protected override void ApplyVisual()
        {
            if (_bg != null) _bg.color = (_hover || _highlight) ? PrologueDefs.PanelHover : _baseColor;
            transform.localScale = _hover ? Vector3.one * 1.08f : Vector3.one;
            if (_outline != null && !IsTarget)
            {
                var oc = _outline.effectColor;
                oc.a = 0f;
                _outline.effectColor = oc;
            }
        }

        protected override void ApplyPulse()
        {
            if (_outline == null) return;
            float k = 0.3f + Pulse01(4.5f) * 0.7f;
            _outline.effectColor =
                new Color(PrologueDefs.Warn.r, PrologueDefs.Warn.g, PrologueDefs.Warn.b, k);
        }
    }
}
