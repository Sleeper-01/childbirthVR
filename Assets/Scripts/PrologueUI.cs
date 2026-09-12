using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChanFangVR
{
    /// 头部跟随 UI：字幕 / 任务卡 / 常驻工具栏 / 帮助 / 暂停遮罩 / 解锁横幅 / 舒适度图标 / 渐变与晕动保护
    public class PrologueUI : MonoBehaviour
    {
        public Action OnHelp;
        public Action OnReplay;
        public Action OnPauseToggle;

        public ToolbarButton BtnHelp;
        public ToolbarButton BtnReplay;
        public ToolbarButton BtnPause;
        public ToolbarButton BtnComfort;

        public bool Paused { get; set; }

        private PrologueAudio _audio;
        private RectTransform _root;
        private RectTransform _toolbar;
        private float _toolbarY = -440f;
        private float _toolbarTarget = -440f;

        private Text _subSpeaker;
        private Text _subText;
        private string _subFull = "";
        private int _subPos;
        private float _subTimer;
        private bool _subActive;

        private Text[] _taskTexts;
        private Image[] _taskDots;
        private Text _toast;
        private Image _toastBg;
        private float _toastRemain;
        private float _toastAlpha;

        private RectTransform _comfortRoot;
        private Text _comfortText;
        private bool _comfortOn = true;

        private GameObject _helpPanel;
        private GameObject _pauseOverlay;
        private GameObject _banner;

        private Image _crosshair;

        private Renderer _fadeRenderer;
        private Renderer _vigRenderer;
        private Color _fadeColor = Color.white;
        private float _fadeFrom;
        private float _fadeTo;
        private float _fadeT = 1f;
        private float _fadeDur = 1f;
        private float _vigTarget;

        // ———— 构建 ————

        public static PrologueUI Create(Camera cam, PrologueAudio audio)
        {
            var go = new GameObject("PrologueUI");
            go.transform.SetParent(cam.transform, false);
            var ui = go.AddComponent<PrologueUI>();
            ui._audio = audio;
            ui.Build();
            return ui;
        }

        private void Build()
        {
            var canvasGo = new GameObject("HeadCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.pixelPerfect = false;
            _root = canvasGo.GetComponent<RectTransform>();
            _root.sizeDelta = new Vector2(1200, 760);
            _root.localScale = Vector3.one * 0.0012f;
            _root.localPosition = new Vector3(0f, -0.02f, 1.5f);
            // 与相机同向即可正对相机；WorldSpace Canvas 的正面在其局部 -Z 侧
            _root.localRotation = Quaternion.identity;

            // —— 字幕（常开） ——
            var subPanel = MakeImage(_root, PrologueDefs.Panel, new Vector2(0, -250),
                new Vector2(940, 130), PrologueWorld.RoundSprite, true).gameObject;
            _subSpeaker = MakeText(subPanel.transform, "", 28, PrologueDefs.Accent,
                TextAnchor.MiddleLeft, new Vector2(-450, 32), new Vector2(500, 40));
            _subText = MakeText(subPanel.transform, "", 40, PrologueDefs.TextMain,
                TextAnchor.UpperLeft, new Vector2(0, -18), new Vector2(880, 80));

            // —— 工具栏 ——
            var tb = MakeImage(_root, PrologueDefs.Panel, new Vector2(0, _toolbarY),
                new Vector2(470, 88), PrologueWorld.RoundSprite, true);
            _toolbar = tb.rectTransform;
            BtnHelp = ToolbarButton.Create(_toolbar, "帮助", new Vector2(-155, 0),
                new Vector2(130, 62), ToolbarRole.Help);
            BtnReplay = ToolbarButton.Create(_toolbar, "重播", new Vector2(0, 0),
                new Vector2(130, 62), ToolbarRole.Replay);
            BtnPause = ToolbarButton.Create(_toolbar, "暂停", new Vector2(155, 0),
                new Vector2(130, 62), ToolbarRole.Pause);
            BtnHelp.Activated += () => { if (OnHelp != null) OnHelp(); };
            BtnReplay.Activated += () => { if (OnReplay != null) OnReplay(); };
            BtnPause.Activated += () => { if (OnPauseToggle != null) OnPauseToggle(); };

            // —— 任务卡 ——
            var card = MakeImage(_root, PrologueDefs.Panel, new Vector2(445, 235),
                new Vector2(300, 250), PrologueWorld.RoundSprite, true);
            MakeText(card.transform, "任务卡 · 序章", 30, PrologueDefs.Warn,
                TextAnchor.MiddleCenter, new Vector2(0, 100), new Vector2(280, 40));
            _taskTexts = new Text[5];
            _taskDots = new Image[5];
            for (int i = 0; i < 5; i++)
            {
                float y = 52f - i * 34f;
                _taskDots[i] = MakeImage(card.transform, PrologueDefs.IdleGray,
                    new Vector2(-128, y), new Vector2(20, 20), PrologueWorld.CircleSprite, false);
                _taskTexts[i] = MakeText(card.transform, PrologueDefs.TaskTexts[i], 24,
                    PrologueDefs.IdleGray, TextAnchor.MiddleLeft, new Vector2(12, y), new Vector2(250, 30));
            }

            // —— 舒适度图标 ——
            var comfort = MakeImage(_root, PrologueDefs.Panel, new Vector2(445, 85),
                new Vector2(300, 58), PrologueWorld.RoundSprite, true);
            _comfortRoot = comfort.rectTransform;
            _comfortText = MakeText(comfort.transform, "舒适度：晕动保护 开", 26,
                PrologueDefs.Success, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(290, 50));
            BtnComfort = ToolbarButton.Create(_comfortRoot, "", Vector2.zero,
                new Vector2(300, 58), ToolbarRole.Comfort);
            BtnComfort.MakeTransparent();
            if (_comfortText != null)
            {
                _comfortText.transform.SetParent(BtnComfort.transform, false);
                _comfortText.transform.SetAsLastSibling();
            }
            BtnComfort.Activated += ToggleComfort;
            _comfortRoot.gameObject.SetActive(false);

            // —— 提示 Toast ——
            var toastBg = MakeImage(_root, new Color(0.1f, 0.3f, 0.45f, 0.9f),
                new Vector2(0, 215), new Vector2(720, 70), PrologueWorld.RoundSprite, true);
            toastBg.gameObject.SetActive(false);
            _toastBg = toastBg;
            _toast = MakeText(toastBg.transform, "", 34, PrologueDefs.TextMain,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(700, 60));

            // —— 帮助面板 ——
            _helpPanel = BuildHelpPanel();
            _helpPanel.SetActive(false);

            // —— 暂停遮罩 ——
            _pauseOverlay = BuildPauseOverlay();
            _pauseOverlay.SetActive(false);

            // —— 解锁横幅 ——
            _banner = BuildBanner();
            _banner.SetActive(false);

            // —— 准星（桌面调试用，默认关闭） ——
            _crosshair = MakeImage(_root, new Color(1f, 1f, 1f, 0.85f), Vector2.zero,
                new Vector2(14, 14), PrologueWorld.CircleSprite, false);
            _crosshair.gameObject.SetActive(false);

            // —— 渐变贴片（白光入场 / 转场黑幕） ——
            _fadeRenderer = BuildCameraQuad(0.32f, PrologueWorld.WhiteTex, new Color(1f, 1f, 1f, 1f));
            // —— 晕动保护暗角 ——
            _vigRenderer = BuildCameraQuad(0.30f, PrologueWorld.VignetteTex, new Color(0f, 0f, 0f, 0f));

            // 整棵 HUD（字幕/任务卡/工具栏/帮助/暂停/横幅）放到 UI 层，
            // 由专用 UICamera 渲染在一切之上、且点击射线只命中 UI 层，解决「字母被阻挡 / 工具栏点不到」。
            SetLayerRecursively(gameObject, 5);
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        private GameObject BuildHelpPanel()
        {
            var panel = MakeImage(_root, PrologueDefs.Panel, new Vector2(0, 20),
                new Vector2(880, 580), PrologueWorld.RoundSprite, true).gameObject;
            MakeText(panel.transform, "操 作 帮 助", 40, PrologueDefs.Warn,
                TextAnchor.MiddleCenter, new Vector2(0, 245), new Vector2(800, 50));
            string body =
                "【VR 手柄】\n" +
                "射线瞄准 → 扣动扳机：确认 / 抓取 / 点击按钮\n" +
                "左摇杆左右推：翻页 / 在场景卡间选择\n\n" +
                "【桌面预览（无头显）】\n" +
                "按住鼠标右键拖动 = 转视角；左键点击 = 扳机\n" +
                "方向键 ←/→ 或 A/D = 摇杆；C = 视角回正\n\n" +
                "【快捷键】\n" +
                "P = 暂停 / 继续    R = 重播    H = 帮助    V = 切换 VR 模式";
            MakeText(panel.transform, body, 28, PrologueDefs.TextMain,
                TextAnchor.UpperLeft, new Vector2(0, -10), new Vector2(800, 380));
            var close = ToolbarButton.Create(panel.transform, "关闭", new Vector2(0, -235),
                new Vector2(150, 56), ToolbarRole.Help);
            close.Activated += () => { if (OnHelp != null) OnHelp(); };
            return panel;
        }

        private GameObject BuildPauseOverlay()
        {
            var overlay = new GameObject("PauseOverlay");
            var rt = overlay.AddComponent<RectTransform>();
            rt.SetParent(_root, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var dim = overlay.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.6f);
            dim.raycastTarget = false;
            MakeText(rt, "已 暂 停", 68, PrologueDefs.TextMain,
                TextAnchor.MiddleCenter, new Vector2(0, 40), new Vector2(600, 90));
            MakeText(rt, PausedHint(), 30, PrologueDefs.IdleGray,
                TextAnchor.MiddleCenter, new Vector2(0, -50), new Vector2(700, 40));
            return overlay;
        }

        private static string PausedHint()
        {
            return PrologueRun.VRMode ? "点击工具栏「继续」按钮恢复" : "点击工具栏「继续」按钮，或按 P 键恢复";
        }

        private GameObject BuildBanner()
        {
            var panel = MakeImage(_root, new Color(0.08f, 0.32f, 0.24f, 0.94f),
                new Vector2(0, 90), new Vector2(780, 230), PrologueWorld.RoundSprite, true).gameObject;
            var outline = panel.AddComponent<Outline>();
            outline.effectColor = PrologueDefs.Success;
            outline.effectDistance = new Vector2(3f, -3f);
            MakeText(panel.transform, "★ 序章完成 ★", 52, PrologueDefs.TextMain,
                TextAnchor.MiddleCenter, new Vector2(0, 45), new Vector2(740, 70));
            MakeText(panel.transform, "第一幕《入院与评估》已解锁\n按 R 或点击「重播」重新体验序章", 30,
                new Color(0.85f, 0.96f, 0.88f), TextAnchor.MiddleCenter, new Vector2(0, -40), new Vector2(740, 90));
            return panel;
        }

        private Renderer BuildCameraQuad(float dist, Texture2D tex, Color color)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            UnityEngine.Object.Destroy(quad.GetComponent<Collider>());
            quad.name = "Quad_" + (tex != null ? tex.name : "plain");
            var cam = PrologueRun.Cam;
            quad.transform.SetParent(cam != null ? cam.transform : transform, false);
            quad.transform.localPosition = new Vector3(0f, 0f, dist);
            quad.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            quad.transform.localScale = new Vector3(3.4f, 2.4f, 1f);
            var r = quad.GetComponent<Renderer>();
            var m = new Material(Shader.Find("Sprites/Default"));
            m.mainTexture = tex;
            m.color = color;
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return r;
        }

        // ———— 每帧动画 ————

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (!Mathf.Approximately(_toolbarY, _toolbarTarget))
            {
                _toolbarY = Mathf.MoveTowards(_toolbarY, _toolbarTarget, dt * 260f);
                _toolbar.anchoredPosition = new Vector2(0f, _toolbarY);
            }

            // 字幕打字机（常开，暂停时也继续显示）
            if (_subActive && _subPos < _subFull.Length)
            {
                _subTimer += dt * 26f;
                while (_subTimer >= 1f && _subPos < _subFull.Length)
                {
                    _subTimer -= 1f;
                    _subPos++;
                }
                _subText.text = _subFull.Substring(0, _subPos);
            }

            if (_toastRemain > 0f)
            {
                _toastRemain -= dt;
                _toastAlpha = Mathf.MoveTowards(_toastAlpha, _toastRemain > 0.4f ? 1f : _toastRemain / 0.4f, dt * 5f);
                var c = _toastBg.color; c.a = 0.9f * _toastAlpha; _toastBg.color = c;
                var tc = _toast.color; tc.a = _toastAlpha; _toast.color = tc;
                if (_toastRemain <= 0f) _toastBg.gameObject.SetActive(false);
            }

            if (_fadeT < 1f)
            {
                _fadeT = Mathf.MoveTowards(_fadeT, 1f, dt / Mathf.Max(0.05f, _fadeDur));
                float a = Mathf.Lerp(_fadeFrom, _fadeTo, _fadeT);
                var fc = _fadeRenderer.material.color;
                fc.r = _fadeColor.r; fc.g = _fadeColor.g; fc.b = _fadeColor.b;
                fc.a = a;
                _fadeRenderer.material.color = fc;
            }

            if (_vigRenderer != null)
            {
                float va = _vigRenderer.material.color.a;
                if (!Mathf.Approximately(va, _vigTarget))
                {
                    va = Mathf.MoveTowards(va, _vigTarget, dt * 1.6f);
                    var c2 = _vigRenderer.material.color; c2.a = va; _vigRenderer.material.color = c2;
                }
            }
        }

        // ———— 对外接口 ————

        public void ShowLine(string speaker, string text)
        {
            _subFull = text ?? "";
            _subPos = 0;
            _subTimer = 0f;
            _subActive = true;
            _subSpeaker.text = "【" + speaker + "】";
            _subText.text = "";
        }

        public void ClearLine()
        {
            _subActive = false;
            _subFull = "";
            _subText.text = "";
            _subSpeaker.text = "";
        }

        public void Toast(string msg, float dur = 2.2f)
        {
            _toast.text = msg;
            _toastRemain = dur;
            _toastAlpha = 0f;
            _toastBg.gameObject.SetActive(true);
        }

        /// state: 0 待完成 / 1 进行中 / 2 已完成
        public void SetTask(int index, int state)
        {
            if (index < 0 || index >= 5) return;
            Color c = state == 2 ? PrologueDefs.Success : state == 1 ? PrologueDefs.Warn : PrologueDefs.IdleGray;
            _taskDots[index].color = c;
            _taskTexts[index].color = state == 0 ? PrologueDefs.IdleGray : PrologueDefs.TextMain;
            _taskTexts[index].fontStyle = state == 2 ? FontStyle.Italic : FontStyle.Normal;
        }

        public void ResetTasks()
        {
            for (int i = 0; i < 5; i++) SetTask(i, 0);
        }

        public void ToolbarShow(bool show)
        {
            _toolbarTarget = show ? -332f : -440f;
        }

        public void SetPauseLabel(bool paused)
        {
            if (BtnPause != null) BtnPause.SetLabel(paused ? "继续" : "暂停");
            if (_pauseOverlay != null)
            {
                var texts = _pauseOverlay.GetComponentsInChildren<Text>(true);
                if (texts.Length > 1) texts[1].text = PausedHint();
            }
        }

        public void HelpVisible(bool show) { if (_helpPanel != null) _helpPanel.SetActive(show); }

        public void PauseOverlay(bool show)
        {
            if (_pauseOverlay == null) return;
            _pauseOverlay.SetActive(show);
            // 遮罩会盖住工具栏，把工具栏提到最上层
            if (show && _toolbar != null) _toolbar.SetAsLastSibling();
        }

        public void Banner(bool show)
        {
            if (_banner == null) return;
            _banner.SetActive(show);
            if (show) _banner.transform.SetAsLastSibling();
        }

        public void Crosshair(bool show) { if (_crosshair != null) _crosshair.gameObject.SetActive(show); }

        public void ComfortShow(bool show) { if (_comfortRoot != null) _comfortRoot.gameObject.SetActive(show); }

        public bool ComfortOn { get { return _comfortOn; } }

        private void ToggleComfort()
        {
            _comfortOn = !_comfortOn;
            _comfortText.text = _comfortOn ? "舒适度：晕动保护 开" : "舒适度：晕动保护 关";
            _comfortText.color = _comfortOn ? PrologueDefs.Success : PrologueDefs.IdleGray;
            if (_audio != null) _audio.PlayClick();
            if (!_comfortOn) Vignette(0f);
        }

        /// 转场时自动拉起暗角（舒适度开启时才生效）
        public void Vignette(float target)
        {
            _vigTarget = _comfortOn ? target : 0f;
        }

        /// 渐变：把幕布渐变为 color 的 targetAlpha 不透明度
        public void Fade(Color color, float targetAlpha, float dur)
        {
            if (_fadeRenderer == null) return;
            float current = _fadeRenderer.material.color.a;
            _fadeFrom = current;
            _fadeTo = targetAlpha;
            _fadeColor = new Color(color.r, color.g, color.b, current);
            _fadeDur = Mathf.Max(0.05f, dur);
            _fadeT = 0f;
        }

        public bool FadeDone { get { return _fadeT >= 1f; } }

        // ———— 构建工具 ————

        public static Text MakeText(Transform parent, string content, int size, Color color,
                                    TextAnchor anchor, Vector2 pos, Vector2 sizeDelta)
        {
            var go = new GameObject("Text");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = sizeDelta;
            var t = go.AddComponent<Text>();
            t.font = PrologueFont.Get();
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.text = content;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = false;
            return t;
        }

        public static Image MakeImage(Transform parent, Color color, Vector2 pos, Vector2 size,
                                      Sprite sprite, bool sliced)
        {
            var go = new GameObject("Image");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            if (sliced && sprite != null) img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// 用世界四角拟合 UI 元素的射线命中碰撞体
        public static void FitCollider(RectTransform rt, BoxCollider col)
        {
            Vector3[] c = new Vector3[4];
            rt.GetWorldCorners(c);
            Vector3 center = (c[0] + c[2]) * 0.5f;
            float w = Vector3.Distance(c[0], c[3]);
            float h = Vector3.Distance(c[0], c[1]);
            col.center = rt.transform.InverseTransformPoint(center);
            col.size = new Vector3(Mathf.Max(0.01f, w), Mathf.Max(0.01f, h), 0.02f);
        }
    }
}
