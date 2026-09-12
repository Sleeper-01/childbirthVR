using UnityEngine;
using UnityEngine.UI;

namespace ChanFangVR
{
    /// 步骤4：场景卡（待产室 / 运动区 / 产房）。摇杆选择，扳机确认。
    public class SceneCard : PrologueInteractable
    {
        public int Index;

        private Image _bg;
        private Outline _outline;
        private Text _title;
        private Text _hint;
        private Color _roomColor;
        private bool _selected;

        public static SceneCard Create(Transform parent, Vector3 localPos, int index)
        {
            var go = new GameObject("SceneCard_" + index);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            // 与相机同向：WorldSpace Canvas 正面在其局部 -Z 侧，无需额外旋转
            go.transform.localRotation = Quaternion.identity;

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.pixelPerfect = false;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 400);
            rt.localScale = Vector3.one * 0.0014f;

            var card = go.AddComponent<SceneCard>();
            card.Index = index;
            card._roomColor = PrologueDefs.RoomColors[index];
            card.Label = PrologueDefs.RoomNames[index];

            card._bg = PrologueUI.MakeImage(rt, new Color(0.12f, 0.15f, 0.19f, 0.92f),
                Vector2.zero, new Vector2(300, 400), PrologueWorld.RoundSprite, true);
            card._outline = card._bg.gameObject.AddComponent<Outline>();
            card._outline.effectColor = new Color(1f, 1f, 1f, 0f);
            card._outline.effectDistance = new Vector2(3f, -3f);

            var strip = PrologueUI.MakeImage(rt, card._roomColor, new Vector2(0, 158),
                new Vector2(268, 130), PrologueWorld.RoundSprite, true);
            PrologueUI.MakeText(strip.transform, (index + 1).ToString(), 44,
                new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleCenter, Vector2.zero, new Vector2(240, 120));

            card._title = PrologueUI.MakeText(rt, PrologueDefs.RoomNames[index], 42,
                PrologueDefs.TextMain, TextAnchor.MiddleCenter, new Vector2(0, 62), new Vector2(280, 60));
            PrologueUI.MakeText(rt, PrologueDefs.RoomDescs[index], 24,
                PrologueDefs.IdleGray, TextAnchor.MiddleCenter, new Vector2(0, -32), new Vector2(280, 110));
            card._hint = PrologueUI.MakeText(rt, "扣扳机前往", 24,
                card._roomColor, TextAnchor.MiddleCenter, new Vector2(0, -150), new Vector2(280, 40));

            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = false;
            PrologueUI.FitCollider(rt, col);

            go.SetActive(false);
            return card;
        }

        public void SetSelected(bool selected)
        {
            if (_selected == selected) return;
            _selected = selected;
            ApplyVisual();
        }

        public override void SetVisible(bool visible)
        {
            if (gameObject.activeSelf == visible) return;
            gameObject.SetActive(visible);
            if (!visible) SetSelected(false);
        }

        protected override void ApplyVisual()
        {
            if (_bg == null) return;
            if (_outline != null)
            {
                Color oc = _selected ? _roomColor : (_hover ? Color.white : Color.clear);
                oc.a = _selected ? 1f : (_hover ? 0.8f : 0f);
                _outline.effectColor = oc;
            }
            Color c = _bg.color;
            c.a = (_selected || _hover) ? 0.98f : 0.82f;
            _bg.color = c;
            if (_title != null) _title.color = _selected ? _roomColor : PrologueDefs.TextMain;
            if (_hint != null) _hint.text = _selected ? "▶ 扣扳机确认前往" : "扣扳机前往";
            transform.localScale = _selected ? Vector3.one * 1.12f : Vector3.one;
        }
    }
}
