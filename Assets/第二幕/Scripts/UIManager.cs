using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRTour
{
    /// <summary>
    /// 第二幕提示对话框（序幕字幕同规格版）：
    /// 显示层 = 挂在相机上的世界空间画布，几何参数与序幕字幕完全一致
    /// （1200x760 画布 / 0.0012 缩放 / 眼前 1.5m / 字幕板 940x130 / 正文 40pt），
    /// 带 26 字/秒打字机（序幕同款），文字大小与出现方式因此和序章一模一样。
    /// ShowTip/ClearAllTip 对外接口与原版完全一致；场景旧 tipText 自动隐藏。
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("【旧版提示文本】已由下方对话框取代，自动隐藏")]
        public TMP_Text tipText;
        public float defaultShowTime = 4f;
        [Header("旧字段，保留兼容")]
        public float tipFontSize = 28f;

        private Canvas _dialogCanvas;
        private GameObject _dialogPanel;
        private Text _dialogBody;
        private float timer;
        private string _full = "";
        private int _pos;
        private float _typeTimer;
        private bool _typing;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            if (tipText != null) tipText.gameObject.SetActive(false);   // 旧裸文本下线
        }

        void Update()
        {
            if (_dialogCanvas == null) TryBuildDialog();   // 相机就绪后再建（一次成功即止）
            if (_dialogCanvas == null) return;

            if (timer > 0f)
            {
                timer -= Time.unscaledDeltaTime;
                if (timer <= 0f) ClearAllTip();
            }

            // 打字机：26 字/秒（序幕 PrologueUI 同参数）
            if (_typing && _dialogBody != null && _pos < _full.Length)
            {
                _typeTimer += Time.unscaledDeltaTime * 26f;
                while (_typeTimer >= 1f && _pos < _full.Length)
                {
                    _typeTimer -= 1f;
                    _pos++;
                }
                _dialogBody.text = _full.Substring(0, _pos);
            }
        }

        // ———— 序幕字幕同规格对话框 ————

        private void TryBuildDialog()
        {
            var cam = Camera.main;
            if (cam == null) return;

            var go = new GameObject("TipDialogCanvas", typeof(Canvas));
            go.transform.SetParent(cam.transform, false);
            _dialogCanvas = go.GetComponent<Canvas>();
            _dialogCanvas.renderMode = RenderMode.WorldSpace;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1200, 760);
            rt.localScale = Vector3.one * 0.0012f;
            rt.localPosition = new Vector3(0f, -0.02f, 1.5f);
            rt.localRotation = Quaternion.identity;

            var panelGO = new GameObject("DialogPanel", typeof(RectTransform));
            panelGO.transform.SetParent(rt, false);
            var prt = (RectTransform)panelGO.transform;
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = new Vector2(0, -250);
            prt.sizeDelta = new Vector2(940, 130);
            var img = panelGO.AddComponent<Image>();
            img.sprite = ChuJian.Merge.ActUIStyle.RoundSprite;
            img.type = Image.Type.Sliced;
            img.color = ChuJian.Merge.ActUIStyle.Panel;
            img.raycastTarget = false;

            _dialogBody = CreateText(panelGO.transform, "", 40, ChuJian.Merge.ActUIStyle.TextMain);
            var brt = _dialogBody.rectTransform;
            brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = new Vector2(0, -18);
            brt.sizeDelta = new Vector2(880, 96);
            _dialogBody.alignment = TextAnchor.UpperLeft;

            _dialogPanel.SetActive(false);   // 无内容时不显示
        }

        private Text CreateText(Transform parent, string content, int size, Color c)
        {
            var go = new GameObject("body");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = ChuJian.Merge.UiFont.Get();
            t.text = content;
            t.fontSize = size;
            t.color = c;
            t.alignment = TextAnchor.UpperLeft;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>业务提示：直接显示，新消息直接覆盖旧消息（接口与原版一致）。
        /// 停留时长自适应文字长度，保证打字机播完还有阅读时间。</summary>
        public void ShowTip(string msg, float duration = 0f)
        {
            if (_dialogCanvas == null) TryBuildDialog();
            if (_dialogCanvas == null) return;

            _full = msg ?? "";
            _pos = 0;
            _typeTimer = 0f;
            _typing = true;
            if (_dialogBody != null) _dialogBody.text = "";
            _dialogPanel.SetActive(true);

            float realDur = duration > 0f ? duration : defaultShowTime;
            timer = Mathf.Max(realDur, _full.Length * 0.16f + 1.5f);   // 序幕同款时长公式
        }

        /// <summary>立刻清空提示文字（接口与原版一致）</summary>
        public void ClearAllTip()
        {
            timer = 0;
            _typing = false;
            _full = "";
            if (_dialogBody != null) _dialogBody.text = "";
            if (_dialogPanel != null) _dialogPanel.SetActive(false);
        }
    }
}
