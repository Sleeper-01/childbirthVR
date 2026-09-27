using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChuJian.Merge
{
    /// <summary>
    /// 幕次流转管理：按策划方案顺序串联六幕场景。
    /// 顺序体验：F8 进入下一幕；F7 上一幕；F9 返回主菜单。
    /// 直选体验：由主菜单按钮直接加载对应幕的首场景。
    /// 第五幕如需补充，在 Entries 中按顺序插入一行即可。
    /// </summary>
    public class ActFlow : MonoBehaviour
    {
        public class ActEntry
        {
            public string ActLabel;
            public string ScenePath;
            public bool IsActStart;
            public ActEntry(string label, string path, bool actStart)
            {
                ActLabel = label;
                ScenePath = path;
                IsActStart = actStart;
            }
        }

        public const string MenuScene = "Assets/Scenes/Main.unity";

        /// <summary>按方案顺序的幕次场景表（序幕→第一幕→…→第六幕）</summary>
        public static readonly List<ActEntry> Entries = new List<ActEntry>
        {
            new ActEntry("序幕·入院与认识环境", "Assets/序幕/Scenes/Prologue.unity", true),
            new ActEntry("第一幕·产前运动", "Assets/第一幕/产前运动/场景/产前运动互动.unity", true),
            new ActEntry("第二幕·产房参观", "Assets/第二幕/Scenes/SampleScene.unity", true),
            new ActEntry("第三幕·镇痛选择", "Assets/第三幕/Scenes/ThirdAct_PainManagement.unity", true),
            new ActEntry("第四幕·分娩选择", "Assets/第四幕/Scenes/UI界面.scene", true),
            new ActEntry("第四幕·顺产路径", "Assets/第四幕/Scenes/顺产.scene", false),
            new ActEntry("第四幕·剖腹产路径", "Assets/第四幕/Scenes/刨腹产.scene", false),
            new ActEntry("第四幕·手术室", "Assets/第四幕/Scenes/手术室.scene", false),
            new ActEntry("尾声·报告与总结", "Assets/尾声/Scenes/SampleScene.scene", true),
        };

        static ActFlow _inst;
        int _index = -1;
        Text _hud;
        Button _next;
        Canvas _canvas;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoInstall()
        {
            if (_inst != null) return;
            var go = new GameObject("~ActFlow");
            DontDestroyOnLoad(go);
            _inst = go.AddComponent<ActFlow>();
            go.AddComponent<DesktopCameraLook>();
            _inst.BuildUI();
            _inst.OnSceneChanged(SceneManager.GetActiveScene());
            SceneManager.sceneLoaded += (scene, mode) => _inst.OnSceneChanged(scene);
        }

        void BuildUI()
        {
            _canvas = new GameObject("ActFlowCanvas").AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 900;
            var scaler = _canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            _canvas.gameObject.AddComponent<GraphicRaycaster>();
            _canvas.transform.SetParent(transform, false);
            EnsureEventSystem();
            if (VRUI.Active) { MakeOverlayWorld(); _vrApplied = true; }

            // 左上角幕次 HUD
            _hud = MakeText(_canvas.transform, "", ActUIStyle.FontBody, MC1.TextDim);
            var rt = _hud.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(24, -24);
            rt.sizeDelta = new Vector2(900, 40);
            _hud.alignment = TextAnchor.MiddleLeft;

            // 右下角“进入下一幕”悬浮按钮
            _next = MakeButton(_canvas.transform, "进入下一幕 ▶", MC1.Accent, ActUIStyle.FontBody);
            var brt = _next.transform as RectTransform;
            brt.anchorMin = brt.anchorMax = new Vector2(1, 0);
            brt.pivot = new Vector2(1, 0);
            brt.anchoredPosition = new Vector2(-24, 24);
            brt.sizeDelta = new Vector2(260, 60);
            _next.onClick.AddListener(ClickNext);

            // “返回主菜单”悬浮按钮（所有幕通用，位于下一幕按钮上方）
            _btnMenu = MakeButton(_canvas.transform, "⌂ 返回主菜单", MC1.Primary, ActUIStyle.FontBody);
            var mrt = _btnMenu.transform as RectTransform;
            mrt.anchorMin = mrt.anchorMax = new Vector2(1, 0);
            mrt.pivot = new Vector2(1, 0);
            mrt.anchoredPosition = new Vector2(-24, 96);
            mrt.sizeDelta = new Vector2(260, 60);
            _btnMenu.onClick.AddListener(ClickMenu);
            // 辅助操作:Ghost 层级——弱化存在感,不与中央内容抢注意力
            var mimg = _btnMenu.GetComponent<Image>();
            mimg.color = new Color(ActUIStyle.Panel.r, ActUIStyle.Panel.g, ActUIStyle.Panel.b, 0.15f);
            var mlabel = _btnMenu.GetComponentInChildren<Text>();
            if (mlabel != null) mlabel.color = ActUIStyle.IdleGray;

            // 底部序幕风格工具栏：帮助 / 重播 / 暂停 / 进度（与序幕 PrologueUI 工具栏同款样式，对话框下方常驻）
            BuildActNodes();
            BuildPrologueToolbar();

            // 右侧序幕任务卡式进度卡（圆点 + 幕名 + 总进度），随时可见；地图面板按 M / 工具栏「进度」打开
            BuildProgressCard();

            // 序幕式转场层：黑场渐变 + 舒适暗角（纯覆盖层，不改任何场景内容）
            BuildFadeLayer();
        }

        // ———— 序幕式转场（底层体验对齐）：黑场渐变 + 转场暗角 ————

        private Canvas _fadeCanvas;
        private Image _fadeBlack;
        private Image _fadeVig;
        private Coroutine _fadeCo;

        private void BuildFadeLayer()
        {
            var go = new GameObject("FadeLayer", typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false);
            _fadeCanvas = go.GetComponent<Canvas>();
            _fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _fadeCanvas.sortingOrder = 5000;   // 永远在最上层（含第三幕 2000 的 HUD）
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            var root = go.GetComponent<RectTransform>();
            root.sizeDelta = new Vector2(1920, 1080);

            var vigGO = new GameObject("Vignette", typeof(RectTransform));
            vigGO.transform.SetParent(root, false);
            _fadeVig = vigGO.AddComponent<Image>();
            _fadeVig.sprite = MakeVignetteSprite();
            // 常驻极轻暗角（B3 轻后处理替代）：收敛画面四周、统一六幕的边缘观感；转场时升至 0.5
            _fadeVig.color = new Color(0f, 0f, 0f, 0.16f);
            _fadeVig.raycastTarget = false;
            StretchRT((RectTransform)vigGO.transform);

            var blackGO = new GameObject("Black", typeof(RectTransform));
            blackGO.transform.SetParent(root, false);
            _fadeBlack = blackGO.AddComponent<Image>();
            _fadeBlack.color = new Color(0f, 0f, 0f, 1f);   // 启动即黑场，首幕像序幕一样淡入
            _fadeBlack.raycastTarget = true;
            StretchRT((RectTransform)blackGO.transform);
        }

        private static void StretchRT(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        /// <summary>序幕同款径向暗角：边缘渐暗、中心全透，仅在转场时升起（资产由 ActUIStyle 统一提供）</summary>
        private Sprite MakeVignetteSprite()
        {
            return ActUIStyle.VignetteSprite;
        }

        /// <summary>黑场渐变协程：toAlpha=1 盖住（随后可加载场景），0 揭开；暗角跟随黑场同步升降</summary>
        private IEnumerator FadeCo(float toAlpha, float dur, bool loadAfter, string scenePath)
        {
            float from = _fadeBlack.color.a;
            float t = 0f;
            _fadeBlack.raycastTarget = true;    // 渐变期间挡住点击，避免连点误触
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / Mathf.Max(0.05f, dur);
                float a = Mathf.Lerp(from, toAlpha, Mathf.Clamp01(t));
                _fadeBlack.color = new Color(0f, 0f, 0f, a);
                _fadeVig.color = new Color(0f, 0f, 0f, Mathf.Max(0.16f, a * 0.5f));
                yield return null;
            }
            _fadeBlack.color = new Color(0f, 0f, 0f, toAlpha);
            _fadeVig.color = new Color(0f, 0f, 0f, Mathf.Max(0.16f, toAlpha * 0.5f));
            if (loadAfter)
            {
                SceneManager.LoadScene(scenePath);
            }
            else if (toAlpha <= 0.01f)
            {
                _fadeBlack.raycastTarget = false;   // 完全揭开才放行点击
            }
        }

        /// <summary>先渐黑再加载场景（序幕转场同款，全程不破坏画面，只是短暂覆盖）</summary>
        public void FadeThenLoad(string scenePath)
        {
            if (_fadeCo != null) StopCoroutine(_fadeCo);
            _fadeCo = StartCoroutine(FadeCo(1f, 0.32f, true, scenePath));
        }

        private void BeginFadeIn()
        {
            if (_fadeCo != null) StopCoroutine(_fadeCo);
            _fadeCo = StartCoroutine(FadeCo(0f, 0.6f, false, null));
        }

        // ———— 序幕风格工具栏与遮罩 ————

        private Button _btnHelp, _btnReplay, _btnPause, _btnProgress;
        private GameObject _helpPanel;
        private GameObject _pausePanel;
        private GameObject _toolbarPanel;
        private GameObject _progressCard;
        private bool _hudPaused;

        private void BuildPrologueToolbar()
        {
            // 工具栏底板：与序幕工具栏一致的深色圆角面板
            var panelGO = new GameObject("ToolbarPanel", typeof(RectTransform));
            panelGO.transform.SetParent(_canvas.transform, false);
            _toolbarPanel = panelGO;
            var prt = (RectTransform)panelGO.transform;
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0);
            prt.anchoredPosition = new Vector2(0, 14);
            prt.sizeDelta = new Vector2(560, 86);
            var pimg = panelGO.AddComponent<Image>();
            pimg.sprite = ActUIStyle.RoundSprite;
            pimg.type = Image.Type.Sliced;
            pimg.color = new Color(ActUIStyle.Panel.r, ActUIStyle.Panel.g, ActUIStyle.Panel.b, 0.80f);
            pimg.raycastTarget = false;
            ActUIStyle.AddTopLight(prt, 0.06f);   // 顶缘受光,与主菜单一致的材质语言

            _btnHelp = ProButton(panelGO.transform, "帮助", new Vector2(-189, 12), new Vector2(118, 62), ActUIStyle.FontHeading);
            _btnReplay = ProButton(panelGO.transform, "重播", new Vector2(-63, 12), new Vector2(118, 62), ActUIStyle.FontHeading);
            _btnPause = ProButton(panelGO.transform, "暂停", new Vector2(63, 12), new Vector2(118, 62), ActUIStyle.FontHeading);
            _btnProgress = ProButton(panelGO.transform, "进度", new Vector2(189, 12), new Vector2(118, 62), ActUIStyle.FontHeading);
            _btnHelp.onClick.AddListener(ClickHelp);
            _btnReplay.onClick.AddListener(ClickReplay);
            _btnPause.onClick.AddListener(ClickPause);
            _btnProgress.onClick.AddListener(ClickProgress);
        }

        // ———— 全局按钮点击（带闸门 + 手动兜底）————
        // 部分幕会改自带的 EventSystem/输入模块状态，UGUI 点击可能整条失效（点击射线能命中但 onClick 不触发）。
        // 因此全局按钮统一走闸门包装：UGUI 正常时点一次；模块失效时由 Update 里的手动射线兜底再触发；
        // 0.4 秒闸门保证两条路径同帧/连点只执行一次。

        private Button _btnMenu;
        private float _lastGlobalClick = -10f;

        private bool GateClick()
        {
            if (Time.unscaledTime - _lastGlobalClick < 0.4f) return false;
            _lastGlobalClick = Time.unscaledTime;
            return true;
        }

        private void ClickHelp() { if (GateClick()) ToggleHelp(); }
        private void ClickReplay() { if (GateClick()) ReplayCurrent(); }
        private void ClickPause() { if (GateClick()) ToggleHudPause(); }
        private void ClickProgress() { if (GateClick()) ToggleProgressMap(); }
        private void ClickMenu() { if (GateClick()) BackToMenu(); }
        private void ClickNext() { if (GateClick()) LoadNext(); }

        /// <summary>鼠标抬起时手动判定：纯数学命中（按钮屏幕矩形），完全不依赖 EventSystem/输入模块</summary>
        private void ManualGlobalClick()
        {
            if (_canvas == null || !_canvas.gameObject.activeInHierarchy) return;
            Vector2 mp = Input.mousePosition;
            if (InScreenRect(_btnMenu, mp)) { Debug.Log("[ActFlow] 兜底点击：返回主菜单"); ClickMenu(); }
            else if (InScreenRect(_next, mp)) { Debug.Log("[ActFlow] 兜底点击：进入下一幕"); ClickNext(); }
            else if (InScreenRect(_btnHelp, mp)) ClickHelp();
            else if (InScreenRect(_btnReplay, mp)) ClickReplay();
            else if (InScreenRect(_btnPause, mp)) ClickPause();
            else if (InScreenRect(_btnProgress, mp)) ClickProgress();
        }

        /// <summary>屏幕坐标是否落在按钮矩形内（ScreenSpaceOverlay 画布的世界坐标即屏幕像素）</summary>
        private static bool InScreenRect(Button b, Vector2 mp)
        {
            if (b == null || !b.gameObject.activeInHierarchy) return false;
            var c = new Vector3[4];
            ((RectTransform)b.transform).GetWorldCorners(c);
            return mp.x >= c[0].x && mp.x <= c[3].x && mp.y >= c[0].y && mp.y <= c[1].y;
        }

        // ———— 进度卡与进度地图 ————
        // 行程节点不是写死的：先识别全局幕次表(Entries)，把「序幕·入院与认识环境」
        // 解析成 节点名 + 描述 后自动生成。以后加幕/改名，进度条自动跟上。

        private class ActNode
        {
            public string Name;
            public string Desc;
            public int EntryIndex;
        }

        private System.Collections.Generic.List<ActNode> _actNodes;
        private int ActCount { get { return _actNodes != null ? _actNodes.Count : 0; } }

        /// <summary>识别全局内容：扫描幕次表生成行程节点</summary>
        private void BuildActNodes()
        {
            _actNodes = new System.Collections.Generic.List<ActNode>();
            for (int i = 0; i < Entries.Count; i++)
            {
                if (!Entries[i].IsActStart) continue;
                string label = Entries[i].ActLabel;
                int sep = label.IndexOf('·');
                if (sep < 0) sep = label.IndexOf('：');
                _actNodes.Add(new ActNode
                {
                    Name = sep > 0 ? label.Substring(0, sep) : label,
                    Desc = sep > 0 ? label.Substring(sep + 1) : "",
                    EntryIndex = i
                });
            }
        }

        private Image[] _cardDots;
        private Text[] _cardTexts;
        private Image[] _cardSegs;
        private Image[] _cardGlow;
        private Image _cardFill;
        private Text _cardPct;
        private float _fillTarget;
        private float _fillShown;
        private float _cardPulse;
        private int _lastPulseActNo = -1;
        private GameObject _mapPanel;
        private Image[] _mapDots;
        private Text[] _mapStateTexts;
        private Image[] _mapRowBgs;
        private Text _mapSummary;
        private int _actNo = -1;   // 当前第几幕（1 基；-1=未知）

        /// <summary>右侧进度卡：序幕任务卡同款——深色圆角面板 + 纵向路径（圆点节点+连接线）+ 底部总进度条。
        /// 节点由全局幕次表自动生成；走过的路段变绿；当前幕节点放大并有呼吸光晕；换幕时整卡轻微脉冲。</summary>
        private void BuildProgressCard()
        {
            // —— 单一版式网格:所有子对象坐标由以下常量推导,保证对齐 ——
            int n = ActCount;
            const float cardW = 330f;
            const float padX = 24f;          // 左右内边距
            const float padTop = 20f;        // 顶部内边距
            const float padBottom = 17f;     // 底部内边距
            const float rowSpacing = 46f;    // 节点固定间距
            const float dotR = 9f;           // 常规圆点半径(当前幕放大到12)

            float contentL = -cardW * 0.5f + padX;    // 内容左边界 = -141
            float contentR = cardW * 0.5f - padX;     // 内容右边界 = +141
            float contentW = contentR - contentL;     // 内容宽度 = 282
            float dotX = contentL + 9f;               // 圆点中心列
            float textX = contentL + 26f;             // 文字左边界列
            float titleY = -(padTop + 18f);           // 标题中心
            float rowsTop = titleY - 18f - 30f;       // 第一行圆点中心
            float lastRowY = rowsTop - (n - 1) * rowSpacing;
            float pctY = lastRowY - 26f;              // 百分比文字中心(进度条右上方)
            float barY = lastRowY - 42f;              // 进度条中心
            float cardH = -barY + 3f + padBottom;

            var rootGO = new GameObject("ProgressCard", typeof(RectTransform));
            rootGO.transform.SetParent(_canvas.transform, false);
            _progressCard = rootGO;
            var root = (RectTransform)rootGO.transform;
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(1f, 0.5f);
            root.anchoredPosition = new Vector2(-140, 40);
            root.sizeDelta = new Vector2(cardW, cardH);

            var bg = rootGO.AddComponent<Image>();
            bg.sprite = ActUIStyle.RoundSprite;
            bg.type = Image.Type.Sliced;
            bg.color = ActUIStyle.Panel;
            bg.raycastTarget = false;
            ActUIStyle.AddTopLight(root, 0.05f);   // 顶缘受光

            // —— Header:标题水平居中于 Card,独立于节点文字基线 ——
            var title = MakeText(root, "旅 程 进 度", ActUIStyle.FontHeading, ActUIStyle.Warn);
            var trt = title.rectTransform;
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0, titleY);
            trt.sizeDelta = new Vector2(contentW, 36);

            _cardDots = new Image[ActCount];
            _cardTexts = new Text[ActCount];
            _cardSegs = new Image[ActCount - 1];
            _cardGlow = new Image[ActCount];
            for (int i = 0; i < ActCount; i++)
            {
                float y = rowsTop - i * rowSpacing;

                // —— NodeRail:连接线,与圆点同列、上下连续(穿过两侧圆点之间的空隙) ——
                if (i < ActCount - 1)
                {
                    var segGO = new GameObject("Seg" + i, typeof(RectTransform));
                    segGO.transform.SetParent(root, false);
                    var seg = segGO.AddComponent<Image>();
                    seg.sprite = ActUIStyle.RoundSprite;
                    seg.type = Image.Type.Sliced;
                    seg.raycastTarget = false;
                    var srt = (RectTransform)segGO.transform;
                    srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 1);
                    srt.anchoredPosition = new Vector2(dotX, y - rowSpacing * 0.5f);
                    srt.sizeDelta = new Vector2(4, rowSpacing - dotR - 12f);
                    _cardSegs[i] = seg;
                }

                // 当前幕呼吸光晕(大圆垫底)
                var glowGO = new GameObject("Glow" + i, typeof(RectTransform));
                glowGO.transform.SetParent(root, false);
                var glow = glowGO.AddComponent<Image>();
                glow.sprite = ActUIStyle.SoftBlobSprite;
                glow.raycastTarget = false;
                glow.color = new Color(ActUIStyle.Warn.r, ActUIStyle.Warn.g, ActUIStyle.Warn.b, 0f);
                var grt = (RectTransform)glowGO.transform;
                grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 1);
                grt.anchoredPosition = new Vector2(dotX, y);
                grt.sizeDelta = new Vector2(38, 38);
                _cardGlow[i] = glow;

                var dotGO = new GameObject("Dot" + i, typeof(RectTransform));
                dotGO.transform.SetParent(root, false);
                var dot = dotGO.AddComponent<Image>();
                dot.sprite = ActUIStyle.CircleSprite;
                dot.raycastTarget = false;
                var drt = (RectTransform)dotGO.transform;
                drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 1);
                drt.anchoredPosition = new Vector2(dotX, y);
                drt.sizeDelta = new Vector2(18, 18);
                _cardDots[i] = dot;

                // 双行排版:幕名 + 描述,左边缘统一在 textX
                var name = MakeText(root, _actNodes[i].Name, ActUIStyle.FontBody, ActUIStyle.TextMain);
                name.alignment = TextAnchor.LowerLeft;
                var nrt = name.rectTransform;
                nrt.anchorMin = nrt.anchorMax = new Vector2(0.5f, 1);
                nrt.pivot = new Vector2(0, 0.5f);
                nrt.anchoredPosition = new Vector2(textX, y + 10);
                nrt.sizeDelta = new Vector2(contentR - textX, 22);
                _cardTexts[i] = name;

                if (!string.IsNullOrEmpty(_actNodes[i].Desc))
                {
                    var desc = MakeText(root, _actNodes[i].Desc, ActUIStyle.FontMicro, ActUIStyle.IdleGray);
                    desc.alignment = TextAnchor.UpperLeft;
                    var drt2 = desc.rectTransform;
                    drt2.anchorMin = drt2.anchorMax = new Vector2(0.5f, 1);
                    drt2.pivot = new Vector2(0, 0.5f);
                    drt2.anchoredPosition = new Vector2(textX, y - 11);
                    drt2.sizeDelta = new Vector2(contentR - textX, 18);
                }
            }

            // —— ProgressFooter:百分比(右上)+ 进度条(左右端与内容边界对齐)——
            _cardPct = MakeText(root, "0%", ActUIStyle.FontSmall, ActUIStyle.TextMain);
            _cardPct.alignment = TextAnchor.MiddleRight;
            var prt2 = _cardPct.rectTransform;
            prt2.anchorMin = prt2.anchorMax = new Vector2(0.5f, 1);
            prt2.pivot = new Vector2(1f, 0.5f);
            prt2.anchoredPosition = new Vector2(contentR, pctY);
            prt2.sizeDelta = new Vector2(120, 24);

            var barGO = new GameObject("BarBg", typeof(RectTransform));
            barGO.transform.SetParent(root, false);
            var barBg = barGO.AddComponent<Image>();
            barBg.sprite = ActUIStyle.RoundSprite;
            barBg.type = Image.Type.Sliced;
            barBg.color = new Color(ActUIStyle.IdleGray.r, ActUIStyle.IdleGray.g, ActUIStyle.IdleGray.b, 0.30f);
            barBg.raycastTarget = false;
            var brt = (RectTransform)barGO.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 1);
            brt.pivot = new Vector2(0, 0.5f);
            brt.anchoredPosition = new Vector2(contentL, barY);
            brt.sizeDelta = new Vector2(contentW, 6);
            // Tuanjie/UGUI 环境下,同一 GameObject 上加第二个 Image 会使 AddComponent 返回空,
            // 因此进度条填充改为挂在 barGO 的子物体上(尺寸/位置完全覆盖父进度条区域)。
            var fillGO = new GameObject("BarFill", typeof(RectTransform));
            fillGO.transform.SetParent(barGO.transform, false);
            var frt = (RectTransform)fillGO.transform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;
            _cardFill = fillGO.AddComponent<Image>();
            _cardFill.sprite = ActUIStyle.RoundSprite;
            _cardFill.type = Image.Type.Filled;
            _cardFill.fillMethod = Image.FillMethod.Horizontal;
            _cardFill.color = ActUIStyle.Accent;
            _cardFill.raycastTarget = false;
            _cardFill.fillAmount = 0f;
            _fillShown = 0f;
        }

        /// <summary>按当前幕号刷新进度卡（与序幕任务卡同规则：完成=绿+斜体 / 进行中=橙 / 未到=灰）</summary>
        private void RefreshProgressVisual()
        {
            if (_cardDots == null) return;
            for (int i = 0; i < ActCount; i++)
            {
                bool done = _actNo > i + 1;
                bool current = _actNo == i + 1;
                _cardDots[i].color = done ? ActUIStyle.Success
                    : current ? ActUIStyle.Warn
                    : new Color(0.42f, 0.45f, 0.50f, 0.9f);
                var drt = (RectTransform)_cardDots[i].transform;
                drt.sizeDelta = current ? new Vector2(24, 24) : new Vector2(18, 18);
                _cardTexts[i].color = current ? ActUIStyle.Warn : (done ? ActUIStyle.TextMain : ActUIStyle.IdleGray);
                _cardTexts[i].fontStyle = done ? FontStyle.Italic : FontStyle.Normal;
                if (i < ActCount - 1)
                    _cardSegs[i].color = _actNo > i + 1 ? ActUIStyle.Success
                        : new Color(ActUIStyle.IdleGray.r, ActUIStyle.IdleGray.g, ActUIStyle.IdleGray.b, 0.30f);
            }
            _fillTarget = Mathf.Clamp01(_actNo / (float)ActCount);
            if (_cardPct != null) _cardPct.text = Mathf.RoundToInt(_fillTarget * 100f) + "%";
            if (_actNo != _lastPulseActNo) { _lastPulseActNo = _actNo; _cardPulse = 1f; }
        }

        /// <summary>进度卡动效：进度条平滑充能 + 当前幕光晕呼吸 + 换幕脉冲</summary>
        private void AnimateProgressCard()
        {
            if (_cardFill == null || _progressCard == null || !_progressCard.activeInHierarchy) return;
            float dt = Time.unscaledDeltaTime;
            if (!Mathf.Approximately(_fillShown, _fillTarget))
            {
                _fillShown = Mathf.MoveTowards(_fillShown, _fillTarget, dt * 0.45f);
                _cardFill.fillAmount = _fillShown;
                if (_cardPct != null) _cardPct.text = Mathf.RoundToInt(_fillShown * 100f) + "%";
            }
            for (int i = 0; i < ActCount; i++)
            {
                if (_cardGlow[i] == null) continue;
                bool current = _actNo == i + 1;
                float a = current ? 0.15f + Mathf.Sin(Time.unscaledTime * 2.6f) * 0.07f : 0f;
                var c = _cardGlow[i].color;
                c.a = Mathf.Lerp(c.a, Mathf.Max(0f, a), dt * 8f);
                _cardGlow[i].color = c;
            }
            if (_cardPulse > 0f)
            {
                _cardPulse = Mathf.Max(0f, _cardPulse - dt * 2.2f);
                float k = _cardPulse * (1f - _cardPulse) * 4f;
                _progressCard.transform.localScale = Vector3.one * (1f + 0.05f * k);
            }
            else if (_progressCard.transform.localScale != Vector3.one)
            {
                _progressCard.transform.localScale = Vector3.one;
            }
        }

        private void ToggleProgressMap()
        {
            if (_mapPanel == null) _mapPanel = BuildProgressMap();
            bool show = !_mapPanel.activeSelf;
            if (show) RefreshProgressMap();
            ActUIStyle.ShowPanel(_mapPanel, show);
            if (show) _mapPanel.transform.SetAsLastSibling();
        }

        private GameObject BuildProgressMap()
        {
            int n = ActCount;
            float rowH = 64f;
            float panelH = 92f + n * rowH + 124f;

            var go = new GameObject("ProgressMap", typeof(RectTransform));
            go.transform.SetParent(_canvas.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 40);
            rt.sizeDelta = new Vector2(880, panelH);
            var img = go.AddComponent<Image>();
            img.sprite = ActUIStyle.RoundLargeSprite;   // 大型主面板：较明显但克制的圆角
            img.type = Image.Type.Sliced;
            img.color = ActUIStyle.Panel;
            ActUIStyle.AddTopLight(rt, 0.05f);

            var title = MakeText(rt, "旅 程 进 度", ActUIStyle.FontDisplay, ActUIStyle.Warn);
            var trt = title.rectTransform;
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 1);
            trt.anchoredPosition = new Vector2(0, -46);
            trt.sizeDelta = new Vector2(700, 52);

            _mapDots = new Image[ActCount];
            _mapStateTexts = new Text[ActCount];
            _mapRowBgs = new Image[ActCount];
            for (int i = 0; i < ActCount; i++)
            {
                float rowCenter = -92f - i * rowH - rowH * 0.5f;
                var rowGO = new GameObject("Row" + i, typeof(RectTransform));
                rowGO.transform.SetParent(rt, false);
                var rowBg = rowGO.AddComponent<Image>();
                rowBg.sprite = ActUIStyle.RoundSprite;
                rowBg.type = Image.Type.Sliced;
                rowBg.color = Color.clear;
                rowBg.raycastTarget = false;
                var rrt = (RectTransform)rowGO.transform;
                rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 1);
                rrt.anchoredPosition = new Vector2(0, rowCenter);
                rrt.sizeDelta = new Vector2(800, rowH - 8);
                _mapRowBgs[i] = rowBg;

                var dotGO = new GameObject("Dot", typeof(RectTransform));
                dotGO.transform.SetParent(rowGO.transform, false);
                var dot = dotGO.AddComponent<Image>();
                dot.sprite = ActUIStyle.CircleSprite;
                dot.raycastTarget = false;
                var drt = (RectTransform)dotGO.transform;
                drt.anchorMin = drt.anchorMax = new Vector2(0, 0.5f);
                drt.anchoredPosition = new Vector2(28, 0);
                drt.sizeDelta = new Vector2(22, 22);
                _mapDots[i] = dot;

                var name = MakeText(rowGO.transform, _actNodes[i].Name + " · " + _actNodes[i].Desc, ActUIStyle.FontBody, ActUIStyle.TextMain);
                var nrt = name.rectTransform;
                nrt.anchorMin = nrt.anchorMax = new Vector2(0, 0.5f);
                nrt.anchoredPosition = new Vector2(56, 0);
                nrt.sizeDelta = new Vector2(500, 34);

                var state = MakeText(rowGO.transform, "", ActUIStyle.FontSmall, ActUIStyle.IdleGray);
                var srt = state.rectTransform;
                srt.anchorMin = srt.anchorMax = new Vector2(1, 0.5f);
                srt.pivot = new Vector2(1, 0.5f);
                srt.anchoredPosition = new Vector2(-28, 0);
                srt.sizeDelta = new Vector2(160, 30);
                state.alignment = TextAnchor.MiddleRight;
                _mapStateTexts[i] = state;
            }

            _mapSummary = MakeText(rt, "", ActUIStyle.FontBody, ActUIStyle.TextMain);
            var mrt = _mapSummary.rectTransform;
            mrt.anchorMin = mrt.anchorMax = new Vector2(0.5f, 0);
            mrt.anchoredPosition = new Vector2(0, 106);
            mrt.sizeDelta = new Vector2(800, 32);

            var hint = MakeText(rt, "按 M 键或工具栏「进度」随时查看 · F8 下一幕 · F9 返回主菜单", ActUIStyle.FontMicro, ActUIStyle.IdleGray);
            var hrt = hint.rectTransform;
            hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 0);
            hrt.anchoredPosition = new Vector2(0, 78);
            hrt.sizeDelta = new Vector2(800, 26);

            var close = ProButton(rt, "关闭", new Vector2(0, 20), new Vector2(150, 50), ActUIStyle.FontSmall);
            close.onClick.AddListener(ClickProgress);
            go.SetActive(false);
            return go;
        }

        private void RefreshProgressMap()
        {
            if (_mapDots == null) return;
            for (int i = 0; i < ActCount; i++)
            {
                bool done = _actNo > i + 1;
                bool current = _actNo == i + 1;
                Color dot = done ? ActUIStyle.Success : current ? ActUIStyle.Accent
                    : new Color(0.35f, 0.38f, 0.42f, 0.9f);
                _mapDots[i].color = dot;
                _mapStateTexts[i].text = done ? "✓ 已完成" : current ? "▶ 进行中" : "未开始";
                _mapStateTexts[i].color = done ? ActUIStyle.Success : current ? ActUIStyle.Warn : ActUIStyle.IdleGray;
                _mapRowBgs[i].color = current ? ActUIStyle.PanelHover : Color.clear;
            }
            int pct = Mathf.RoundToInt(Mathf.Clamp01(_actNo / (float)ActCount) * 100f);
            string cur = (_actNo >= 1 && _index >= 0 && _index < Entries.Count) ? Entries[_index].ActLabel : _actNodes[0].Name;
            _mapSummary.text = "整体进度 " + pct + "% · 当前：" + cur;
        }

        /// <summary>序幕风格按钮：小圆角深底 + 白字 + 悬停增亮（复用 ActUIStyle 的调色板与圆角图）</summary>
        private Button ProButton(Transform parent, string label, Vector2 pos, Vector2 size, int fontSize)
        {
            var b = new GameObject("btn_" + label).AddComponent<Button>();
            b.transform.SetParent(parent, false);
            var img = b.gameObject.AddComponent<Image>();
            img.sprite = ActUIStyle.RoundSmallSprite;
            img.type = Image.Type.Sliced;
            img.color = ActUIStyle.ButtonBg;
            var rt = (RectTransform)b.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var txt = MakeText(b.transform, label, fontSize, ActUIStyle.TextMain);
            var trt = txt.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
            txt.alignment = TextAnchor.MiddleCenter;
            var col = b.colors;
            col.highlightedColor = new Color(1.16f, 1.16f, 1.20f);
            col.pressedColor = new Color(0.82f, 0.82f, 0.88f);
            col.selectedColor = Color.white;
            col.disabledColor = new Color(1f, 1f, 1f, 0.45f);
            b.colors = col;
            var nav = b.navigation; nav.mode = Navigation.Mode.None; b.navigation = nav;
            b.gameObject.AddComponent<UIButtonFeedback>();   // 统一交互反馈：悬停/按下/成功脉冲
            return b;
        }

        private void ToggleHelp()
        {
            if (_helpPanel == null) _helpPanel = BuildHelpPanel();
            bool show = !_helpPanel.activeSelf;
            ActUIStyle.ShowPanel(_helpPanel, show);
            if (show) _helpPanel.transform.SetAsLastSibling();
        }

        private GameObject BuildHelpPanel()
        {
            var go = new GameObject("HelpPanel", typeof(RectTransform));
            go.transform.SetParent(_canvas.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(820, 500);
            var img = go.AddComponent<Image>();
            img.sprite = ActUIStyle.RoundLargeSprite;   // 大型主面板：较明显但克制的圆角
            img.type = Image.Type.Sliced;
            img.color = ActUIStyle.Panel;
            ActUIStyle.AddTopLight(rt, 0.05f);

            var title = MakeText(rt, "操 作 帮 助", ActUIStyle.FontDisplay, ActUIStyle.Warn);
            var trt = title.rectTransform;
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 1);
            trt.anchoredPosition = new Vector2(0, -50);
            trt.sizeDelta = new Vector2(700, 54);

            string body =
                "【键鼠】\n" +
                "按住鼠标右键拖动 = 转动视角\n" +
                "鼠标左键 = 点击 / 确认\n\n" +
                "【VR 手柄】\n" +
                "射线瞄准 → 扣动扳机：确认\n\n" +
                "【快捷键】\n" +
                "P = 暂停 / 继续    R = 重播本幕    H = 帮助\n" +
                "M = 进度地图    F8 = 下一幕    F9 = 返回主菜单\n\n" +
                "【不适时】按 Menu / P 暂停并呼叫护士";
            var txt = MakeText(rt, body, ActUIStyle.FontBody, ActUIStyle.TextMain);
            txt.alignment = TextAnchor.UpperLeft;
            var brt = txt.rectTransform;
            brt.anchorMin = new Vector2(0.5f, 0.5f);
            brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = new Vector2(0, -10);
            brt.sizeDelta = new Vector2(720, 360);

            var close = ProButton(rt, "关闭", new Vector2(0, -220), new Vector2(150, 54), ActUIStyle.FontHeading);
            close.onClick.AddListener(ClickHelp);
            go.SetActive(false);
            return go;
        }

        private void ToggleHudPause()
        {
            _hudPaused = !_hudPaused;
            Time.timeScale = _hudPaused ? 0f : 1f;
            if (_pausePanel == null) _pausePanel = BuildPausePanel();
            ActUIStyle.ShowPanel(_pausePanel, _hudPaused);
            if (_hudPaused) _pausePanel.transform.SetAsLastSibling();
            if (_btnPause != null)
            {
                var t = _btnPause.GetComponentInChildren<Text>();
                if (t != null) t.text = _hudPaused ? "继续" : "暂停";
            }
        }

        private GameObject BuildPausePanel()
        {
            var go = new GameObject("HudPauseOverlay", typeof(RectTransform));
            go.transform.SetParent(_canvas.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var dim = go.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.6f);
            dim.raycastTarget = false;

            var title = MakeText(rt, "已 暂 停", ActUIStyle.FontDisplay + 8, ActUIStyle.TextMain);
            var trt = title.rectTransform;
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.anchoredPosition = new Vector2(0, 60);
            trt.sizeDelta = new Vector2(600, 90);

            var hint = MakeText(rt, "点击工具栏「继续」按钮，或按 P 键恢复", ActUIStyle.FontBody, ActUIStyle.IdleGray);
            var hrt = hint.rectTransform;
            hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 0.5f);
            hrt.anchoredPosition = new Vector2(0, -30);
            hrt.sizeDelta = new Vector2(700, 40);

            go.SetActive(false);
            return go;
        }

        private void ReplayCurrent()
        {
            var s = SceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(s.path)) FadeThenLoad(s.path);
        }

        /// <summary>序幕场景自带 P/R/H/L 处理，避免双重触发</summary>
        private static bool SceneIsPrologue
        {
            get
            {
                var s = SceneManager.GetActiveScene();
                return s.name == "Prologue" || s.path.Contains("序幕");
            }
        }

        /// <summary>正在输入框打字时，字母快捷键不触发（否则打 P/R/H/L 会误操作）</summary>
        private static bool IsTyping()
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es == null) return false;
            var sel = es.currentSelectedGameObject;
            if (sel == null) return false;
            return sel.GetComponent<UnityEngine.UI.InputField>() != null ||
                   sel.GetComponent<TMPro.TMP_InputField>() != null;
        }

        GameObject _esGO;
        bool _vrApplied;
        bool _prevTrigger;
        Image _reticle;

        /// <summary>全局画布根节点（供 VRPointerVisual 每帧手动驱动到相机前方）</summary>
        internal static Transform OverlayRoot;

        /// <summary>全局画布改为世界空间模式（此环境下相机空间画布的自动跟随失灵，世界空间+手动驱动已验证可行）</summary>
        void MakeOverlayWorld()
        {
            if (_canvas == null) return;
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.worldCamera = VRUI.UiCamera;
            _canvas.sortingOrder = 900;
            var rt = _canvas.transform as RectTransform;
            rt.sizeDelta = new Vector2(1920, 1080);
            rt.localScale = Vector3.one * 0.0007f; // 1920*0.0007≈1.34米宽，0.9米处视野内舒适
            OverlayRoot = rt;
        }

        /// <summary>VR 视线准星（屏幕中心小点，仅在 VR 模式显示）</summary>
        void BuildReticle()
        {
            if (_reticle != null || _canvas == null) return;
            var go = new GameObject("GazeReticle");
            go.transform.SetParent(_canvas.transform, false);
            _reticle = go.AddComponent<Image>();
            _reticle.color = new Color(1f, 1f, 1f, 0.65f);
            _reticle.raycastTarget = false;
            var rt = _reticle.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(10, 10);
            go.SetActive(false);
        }

        /// <summary>VR 注视点击：准星（视线中心）指向的 UGUI 按钮，扣任意手柄扳机触发</summary>
        void GazeClick()
        {
            bool trig = false;
            // 首选 Input System 扳机（此环境下旧XR输入API无效）
            var pv = FindObjectOfType<VRPointerVisual>();
            if (pv != null && pv.TriggerHeld) trig = true;
            if (!trig)
            {
                var right = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
                bool b;
                if (right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out b) && b) trig = true;
                if (!trig)
                {
                    var left = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
                    if (left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out b) && b) trig = true;
                }
            }
            bool down = trig && !_prevTrigger;
            _prevTrigger = trig;
            if (!down) return;

            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es == null) return;

            // 优先：手柄射线点击（射线与UI屏交点换算为屏幕坐标）
            if (TryRayClick(es)) return;

            // 兜底：视线中心准星点击
            var cam = Camera.main;
            if (cam == null) return;
            var ped = new UnityEngine.EventSystems.PointerEventData(es);
            ped.position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            es.RaycastAll(ped, results);
            if (results.Count > 0)
            {
                Debug.Log("[初见新生/VR注视点击] " + results[0].gameObject.name);
                UnityEngine.EventSystems.ExecuteEvents.ExecuteHierarchy(
                    results[0].gameObject, ped,
                    UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
            }
            else
            {
                // 视线中心也走磁吸：对准按钮附近即可选中
                TryMagnetClick(ped.position, es);
            }
        }

        /// <summary>手柄射线点击：射线与 UI 屏求交，把交点换算为屏幕坐标后走标准 UGUI 点击</summary>
        bool TryRayClick(UnityEngine.EventSystems.EventSystem es)
        {
            var vis = FindObjectOfType<VRPointerVisual>();
            Vector3 origin, dir;
            if (vis == null || !vis.GetRightRay(out origin, out dir)) return false;

            var uiCam = VRUI.UiCamera;
            if (uiCam == null || _canvas == null) return false;

            // UI 屏平面（挂在 UI 相机前 planeDistance 处）
            var plane = new Plane(-uiCam.transform.forward,
                uiCam.transform.position + uiCam.transform.forward * 0.9f);
            float enter;
            if (!plane.Raycast(new Ray(origin, dir), out enter) || enter < 0f) return false;

            Vector3 world = origin + dir * enter;
            Vector2 sp = uiCam.WorldToScreenPoint(world);

            // 命中标记：在落点放一个小球 1.5 秒，直观校准射线指向与落点偏差
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "~HitMarker";
            var mcol = marker.GetComponent<Collider>(); if (mcol != null) Destroy(mcol);
            marker.transform.position = world;
            marker.transform.localScale = Vector3.one * 0.02f;
            Destroy(marker, 1.5f);

            var ped = new UnityEngine.EventSystems.PointerEventData(es);
            ped.position = sp;
            var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            es.RaycastAll(ped, results);
            // 沿命中清单找第一个真正可点击的元素（跳过纯装饰的文字/背景图）
            for (int i = 0; i < results.Count; i++)
            {
                var handler = UnityEngine.EventSystems.ExecuteEvents.ExecuteHierarchy(
                    results[i].gameObject, ped,
                    UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
                if (handler != null)
                {
                    Debug.Log("[初见新生/VR射线点击] " + handler.name);
                    return true;
                }
            }
            // 磁吸：精确命中失败时，吸附到落点附近最近的可用按钮
            if (TryMagnetClick(sp, es)) return true;

            // 物理桥：序幕等幕使用自制碰撞体交互（PrologueInteractable），手柄射线直接激活
            TryPhysicsBridgeFromHand(origin, dir);
            return true;
        }

        /// <summary>手柄射线物理桥：命中 PrologueInteractable（序幕工具栏/手册/场景卡等）则调用其激活</summary>
        void TryPhysicsBridgeFromHand(Vector3 origin, Vector3 dir)
        {
            var itType = System.Type.GetType("ChanFangVR.PrologueInteractable, Assembly-CSharp");
            if (itType == null) return;
            var hits = Physics.RaycastAll(new Ray(origin, dir), 8f);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                var it = hits[i].collider.GetComponent(itType) as MonoBehaviour;
                if (it == null || !it.isActiveAndEnabled) continue;
                var fld = itType.GetField("Interactive");
                if (fld != null && !(bool)fld.GetValue(it)) continue;
                itType.GetMethod("NotifyActivated").Invoke(it, null);
                Debug.Log("[初见新生/VR物理桥] 已激活: " + it.gameObject.name);
                return;
            }
        }

        /// <summary>磁吸点击：找落点附近最近的可用按钮并触发（半径=屏高12%）</summary>
        bool TryMagnetClick(Vector2 sp, UnityEngine.EventSystems.EventSystem es)
        {
            var uiCam = VRUI.UiCamera;
            if (uiCam == null) return false;

            float snap = Mathf.Max(Screen.height * 0.25f, 200f); // 至少200px，确保VR低分辨率下也能吸附
            float best = float.MaxValue;
            Button bestBtn = null;
            Vector2 bestSp = sp;
            foreach (var btn in FindObjectsOfType<Button>())
            {
                if (btn == null || !btn.gameObject.activeInHierarchy || !btn.interactable) continue;
                var rt = btn.transform as RectTransform;
                if (rt == null) continue;
                Vector2 bsp = uiCam.WorldToScreenPoint(rt.TransformPoint(rt.rect.center));
                float d = Vector2.Distance(sp, bsp);
                if (d < best) { best = d; bestBtn = btn; bestSp = bsp; }
            }
            if (bestBtn == null || best > snap)
            {
                Debug.Log("[初见新生/磁吸未命中] " + (bestBtn != null
                    ? "最近按钮=" + bestBtn.name + " 距离" + (int)best + "px 超出" + (int)snap
                    : "全场没有可用Button"));
                return false;
            }

            // 在被吸附的按钮上闪一个标记球
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "~SnapMarker";
            var mcol = marker.GetComponent<Collider>(); if (mcol != null) Destroy(mcol);
            marker.transform.position = uiCam.ScreenToWorldPoint(new Vector3(bestSp.x, bestSp.y, 3.15f));
            marker.transform.localScale = Vector3.one * 0.03f;
            Destroy(marker, 0.8f);

            bestBtn.onClick.Invoke();
            Debug.Log("[初见新生/磁吸点击] " + bestBtn.name + " 吸附距离" + (int)best + "px");
            return true;
        }

        void EnsureEventSystem()
        {
            foreach (var es in FindObjectsOfType<UnityEngine.EventSystems.EventSystem>())
            {
                // 场景自带 EventSystem 时优先用它的，停用常驻的那份，避免双份冲突导致点击失效
                if (_esGO == null || es.gameObject != _esGO)
                {
                    if (_esGO != null) _esGO.SetActive(false);
                    // 自带的若没挂输入模块则补一个，否则照样点不动
                    if (es.isActiveAndEnabled && es.GetComponent<UnityEngine.EventSystems.BaseInputModule>() == null)
                        es.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                    return;
                }
            }
            if (_esGO != null) { _esGO.SetActive(true); return; }
            _esGO = new GameObject("EventSystem_ActFlow");
            _esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            _esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            DontDestroyOnLoad(_esGO);
        }

        void OnSceneChanged(Scene scene)
        {
            EnsureEventSystem();
            // VR 质量设置集中由 VRPerformanceProfile 管理；只在场景切换时应用一次。
            VRPerformanceProfile.ApplyForCurrentScene();
            // 换幕时清掉全局暂停状态，避免上一幕的 timeScale=0 把新幕冻住
            if (_hudPaused) { _hudPaused = false; Time.timeScale = 1f; }
            if (_pausePanel != null) _pausePanel.SetActive(false);
            if (_helpPanel != null) _helpPanel.SetActive(false);
            // 序幕自带同款工具栏与任务卡：隐藏全局的这两块，避免重叠（其余幕照常显示）
            bool pro = SceneIsPrologue;
            if (_toolbarPanel != null) _toolbarPanel.SetActive(!pro);
            if (_progressCard != null) _progressCard.SetActive(!pro);
            bool isMenu = scene.path == MenuScene || scene.name == "Main";
            _canvas.gameObject.SetActive(!isMenu);
            if (!isMenu)
            {
                // 每幕重申全局画布的VR挂载与层级（防各幕脚本改动画布设置导致按钮消失）
                if (VRUI.Active) MakeOverlayWorld();
                _canvas.sortingOrder = 900;
                if (_next != null) _next.gameObject.SetActive(true);
                var menuBtns = _canvas.GetComponentsInChildren<Button>(true);
                foreach (var b in menuBtns) b.gameObject.SetActive(true);
            }
            if (isMenu) { _index = -1; Diagnose("主菜单"); BeginFadeIn(); return; }

            _index = Entries.FindIndex(e => e.ScenePath == scene.path);
            if (_index < 0) _index = Entries.FindIndex(e => System.IO.Path.GetFileName(e.ScenePath) == scene.name && e.ScenePath.EndsWith(".scene") == scene.path.EndsWith(".scene"));
            RefreshHud();
            Diagnose("幕场景 " + scene.name);
            // 新幕淡入（序幕转场同款：黑场揭开 + 暗角回落）
            BeginFadeIn();
        }

        /// <summary>
        /// 点击桥（合并工程兼容层）：序幕等幕使用自定义射线交互（BoxCollider + PrologueInteractable），
        /// 其内部瞄准在合并环境中可能失效。本桥在 UGUI 未命中、其 _hover 为空时，
        /// 用物理射线直接命中交互物并调用 NotifyActivated()，保证鼠标点击始终有效。
        /// </summary>
        void TryBridgeActivate()
        {
            try
            {
                // 1. UGUI 已命中则不介入（UGUI 按钮优先）
                var es = UnityEngine.EventSystems.EventSystem.current;
                if (es != null)
                {
                    var ped = new UnityEngine.EventSystems.PointerEventData(es);
                    ped.position = Input.mousePosition;
                    var res = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                    es.RaycastAll(ped, res);
                    if (res.Count > 0) return;
                }

                // 2. 幕内自带瞄准系统若正悬停在目标上，交给它处理（避免双触发）
                var pinType = System.Type.GetType("ChanFangVR.PrologueInput, Assembly-CSharp");
                if (pinType != null)
                {
                    var pins = FindObjectsOfType(pinType);
                    if (pins.Length > 0)
                    {
                        var fld = pinType.GetField("_hover", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var hv = fld != null ? fld.GetValue(pins[0]) as MonoBehaviour : null;
                        if (hv != null) return;
                    }
                }

                // 3. 物理射线桥：遍历全部相机，命中 PrologueInteractable 即激活；未命中时输出几何诊断
                var itType = System.Type.GetType("ChanFangVR.PrologueInteractable, Assembly-CSharp");
                if (itType == null) return;
                var sb = new System.Text.StringBuilder();
                bool fired = false;
                foreach (var cam in Camera.allCameras)
                {
                    if (cam == null || !cam.enabled) continue;
                    var ray = cam.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, 25f))
                    {
                        var it = hit.collider.GetComponent(itType) as MonoBehaviour;
                        bool inter = true;
                        var f = itType.GetField("Interactive");
                        if (it != null && f != null) inter = (bool)f.GetValue(it);
                        sb.Append(cam.name + "→" + hit.collider.name + (it != null ? "(交互物,可交互=" + inter + ")" : "(非交互)") + "; ");
                        if (!fired && it != null && it.isActiveAndEnabled && inter)
                        {
                            itType.GetMethod("NotifyActivated").Invoke(it, null);
                            Debug.Log("[初见新生/点击桥] 已代为激活: " + it.gameObject.name + "（相机:" + cam.name + "）");
                            fired = true;
                        }
                    }
                    else
                    {
                        sb.Append(cam.name + "→未命中; ");
                    }
                }
                if (!fired)
                    Debug.Log("[初见新生/点击桥诊断] 鼠标" + Input.mousePosition + " 射线结果: " + sb);
            }
            catch { }
        }

        bool TryRayActivate(Camera cam, System.Type itType)
        {
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, 25f)) return false;
            var it = hit.collider.GetComponent(itType) as MonoBehaviour;
            if (it == null || !it.isActiveAndEnabled) return false;
            var fldInt = itType.GetField("Interactive");
            if (fldInt != null && !(bool)fldInt.GetValue(it)) return false;
            itType.GetMethod("NotifyActivated").Invoke(it, null);
            Debug.Log("[初见新生/点击桥] 已代为激活: " + it.gameObject.name + "（相机:" + cam.name + "）");
            return true;
        }

        /// <summary>F10 打印点击链路自检：EventSystem、输入模块、鼠标锁定、射线画布、序幕运行时状态</summary>
        void Diagnose(string context)
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            string esInfo = "无";
            if (es != null)
            {
                var mod = es.GetComponent<UnityEngine.EventSystems.BaseInputModule>();
                esInfo = es.gameObject.name + (mod != null ? " 模块:" + mod.GetType().Name : " 无输入模块!");
            }
            int raycasters = FindObjectsOfType<GraphicRaycaster>().Length;
            int esCount = FindObjectsOfType<UnityEngine.EventSystems.EventSystem>().Length;

            // 序幕深度诊断：PrologueInput 组件是否在跑、PrologueRun.VRMode 实际值、运行时相机列表
            string prologueInfo = "";
            var pinType = System.Type.GetType("ChanFangVR.PrologueInput, Assembly-CSharp");
            if (pinType != null)
            {
                var pin = FindObjectsOfType(pinType);
                prologueInfo = "；PrologueInput实例=" + pin.Length;
                if (pin.Length > 0)
                {
                    var behaviour = pin[0] as MonoBehaviour;
                    if (behaviour != null && !behaviour.enabled) prologueInfo += "(已禁用!)";
                }
                var runType = System.Type.GetType("ChanFangVR.PrologueRun, Assembly-CSharp");
                if (runType != null)
                {
                    var prop = runType.GetProperty("VRMode");
                    if (prop != null) prologueInfo += "；PrologueRun.VRMode=" + prop.GetValue(null);
                }
            }
            var cams = Camera.allCameras;
            var camList = new System.Text.StringBuilder();
            for (int i = 0; i < cams.Length; i++)
            {
                var c = cams[i];
                camList.Append(string.Format("{0}@({1:F1})fov{2:F0}d{3}mask{4}{5}",
                    c.name, c.transform.position, c.fieldOfView, c.depth, c.cullingMask,
                    i < cams.Length - 1 ? " | " : ""));
            }

            // 序幕悬停监视：读 PrologueInput._hover 私有字段，看瞄准系统当前悬停在什么上
            if (pinType != null)
            {
                var pins = FindObjectsOfType(pinType);
                if (pins.Length > 0)
                {
                    try
                    {
                        var fld = pinType.GetField("_hover", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (fld != null)
                        {
                            var hv = fld.GetValue(pins[0]) as MonoBehaviour;
                            prologueInfo += "；当前悬停=" + (hv != null ? hv.gameObject.name : "无(瞄准未命中任何按钮)");
                        }
                    }
                    catch { }
                }
            }

            Debug.Log(string.Format("[初见新生/自检:{0}] EventSystem={1}（活跃={5}）；鼠标锁定={2}；可见={3}；Raycaster={4}{6}；相机[{7}]",
                context, esInfo, Cursor.lockState, Cursor.visible, raycasters, esCount, prologueInfo, camList));
        }

        /// <summary>F11 VR诊断：手柄输入链路与代理模型状态</summary>
        void VrDiagnose()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("XR启用=" + UnityEngine.XR.XRSettings.enabled + " 设备=" + UnityEngine.XR.XRSettings.loadedDeviceName);
            sb.AppendLine("VRUI.Active=" + VRUI.Active
                + " 画布模式=" + (_canvas != null ? _canvas.renderMode.ToString() : "无")
                + " 画布相机=" + (_canvas != null && _canvas.worldCamera != null ? _canvas.worldCamera.name : "无"));
            sb.AppendLine("准星=" + (_reticle == null ? "未创建" : (_reticle.gameObject.activeSelf ? "显示中" : "已隐藏")));

            if (_canvas != null)
            {
                sb.AppendLine("[全局画布] active=" + _canvas.gameObject.activeInHierarchy
                    + " mode=" + _canvas.renderMode
                    + " dist=" + _canvas.planeDistance.ToString("F1")
                    + " cam=" + (_canvas.worldCamera != null ? _canvas.worldCamera.name : "null")
                    + " order=" + _canvas.sortingOrder);
                sb.AppendLine("[下一幕按钮] " + (_next != null ? "active=" + _next.gameObject.activeInHierarchy : "null"));
            }

            var vis = FindObjectOfType<VRPointerVisual>();
            sb.AppendLine("手柄可视化对象=" + (vis != null ? "存在" : "未创建"));
            if (vis != null)
            {
                foreach (var t in vis.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.Contains("Controller"))
                        sb.AppendLine("  " + t.name + " 激活=" + t.gameObject.activeSelf + " 位置=" + t.position.ToString("F2"));
                }
            }

            var d = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
            sb.AppendLine("[旧API] 右手设备=" + (d.isValid ? d.name : "无效"));
            Vector3 p; Quaternion q; bool tb;
            bool hp = d.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out p);
            bool hr = d.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out q);
            bool ht = d.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out tb);
            sb.AppendLine("[旧API] 位置=" + hp + " 旋转=" + hr + " 扳机=" + (ht ? (tb ? "按下" : "松开") : "失败"));

            // Input System 设备与动作状态（此环境的有效数据源）
            int xrDevs = 0;
            foreach (var dev in UnityEngine.InputSystem.InputSystem.devices)
            {
                if (dev is UnityEngine.InputSystem.XR.XRController || dev is UnityEngine.InputSystem.XR.XRHMD)
                {
                    xrDevs++;
                    sb.AppendLine("[InputSystem] " + dev.GetType().Name + " \"" + dev.displayName + "\"");
                }
            }
            if (xrDevs == 0) sb.AppendLine("[InputSystem] 无XR专用设备!");
            var allNames = new System.Text.StringBuilder();
            foreach (var d2 in UnityEngine.InputSystem.InputSystem.devices)
                allNames.Append(d2.name).Append(", ");
            sb.AppendLine("[InputSystem] 全部设备(" + UnityEngine.InputSystem.InputSystem.devices.Count + "): " + allNames);
            var pv2 = FindObjectOfType<VRPointerVisual>();
            if (pv2 != null) sb.AppendLine("扳机(InputSystem)=" + pv2.TriggerHeld);

            // 3D定位：下一幕按钮与相机的几何关系（夹角大=视角外；夹角小距离对=被遮挡或未渲染）
            var vcam = VRUI.UiCamera;
            if (_next != null && vcam != null)
            {
                var wp = _next.transform.position;
                var to = wp - vcam.transform.position;
                sb.AppendLine("[按钮3D] pos=" + wp.ToString("F2")
                    + " 距相机=" + to.magnitude.ToString("F2") + "m"
                    + " 与视线夹角=" + Vector3.Angle(to, vcam.transform.forward).ToString("F0") + "°"
                    + " 相机位=" + vcam.transform.position.ToString("F2"));
                var es2 = UnityEngine.EventSystems.EventSystem.current;
                if (es2 != null)
                {
                    var sp2 = vcam.WorldToScreenPoint(wp);
                    var ped2 = new UnityEngine.EventSystems.PointerEventData(es2);
                    ped2.position = sp2;
                    var res2 = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                    es2.RaycastAll(ped2, res2);
                    var names = new System.Text.StringBuilder();
                    foreach (var r in res2) names.Append(r.gameObject.name).Append(" > ");
                    sb.AppendLine("[按钮处UI栈] " + (res2.Count > 0 ? names.ToString() : "空(该方向无任何UI被命中!)"));
                }
            }

            Debug.Log("[初见新生/VR诊断] " + Time.time.ToString("F1") + "s\n" + sb);
        }


        void RefreshHud()
        {
            if (_index < 0 || _index >= Entries.Count) { _hud.text = ""; _actNo = -1; RefreshProgressVisual(); return; }
            int actNo = 0, actTotal = 0;
            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].IsActStart) actTotal++;
                if (i <= _index && Entries[i].IsActStart) actNo++;
            }
            var e = Entries[_index];
            _hud.text = string.Format("第 {0}/{1} 幕 · {2}    [F8 下一幕 | F7 上一幕 | F9 返回主菜单]", actNo, actTotal, e.ActLabel);
            _next.gameObject.SetActive(_index < Entries.Count - 1);
            if (_index >= Entries.Count - 1)
                _next.GetComponentInChildren<Text>().text = "已是最后一幕";

            // 同步进度条与进度地图
            _actNo = actNo;
            RefreshProgressVisual();
            if (_mapPanel != null && _mapPanel.activeSelf) RefreshProgressMap();
        }

        /// <summary>VR下把各幕画布转为挂头部相机的相机空间画布，使其在头显内可见且可被射线点击；
        /// 覆盖两种形态：屏幕叠加画布、以及挂空相机的相机空间画布（渲染到null相机=不可见）</summary>
        void ConvertOverlayCanvases()
        {
            var uiCam = VRUI.UiCamera;
            if (uiCam == null) return;
            int converted = 0;
            foreach (var c in FindObjectsOfType<Canvas>())
            {
                if (c == null || c == _canvas) continue;
                if (c.transform.root == transform) continue; // 自己的画布
                bool needConvert = c.renderMode == RenderMode.ScreenSpaceOverlay
                    || (c.renderMode == RenderMode.ScreenSpaceCamera && c.worldCamera == null);
                if (!needConvert) continue;

                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = uiCam;
                // 按原sortingOrder分层放置：order越大越近用户，保留遮挡关系
                c.planeDistance = 3.15f + Mathf.Clamp(100 - c.sortingOrder, 0, 100) * 0.01f;
                converted++;
            }

            // 静音其他所有相机（本相机渲染一切；各幕相机的变换仍保留供其脚本使用）
            foreach (var cam in Camera.allCameras)
            {
                if (cam == null || cam == uiCam) continue;
                if (cam.cullingMask != 0) cam.cullingMask = 0;
            }

            if (converted > 0)
                Debug.Log("[初见新生/VR] 已转换 " + converted + " 个画布为头显可见");
        }

        void Update()
        {
            VRPerformanceProfile.ApplyIfVrJustActivated();
            // XR 启用后：补挂头显可见模式 + 准星 + 手柄可视化（各自独立标记，避免XR提前激活时被跳过）
            if (Time.frameCount % 60 == 0 && VRUI.Active)
            {
                VRPerformanceProfile.RefreshEnabledCameras();
                ConvertOverlayCanvases();
                if (!_vrApplied && VRUI.Active) { MakeOverlayWorld(); _vrApplied = true; }
                if (_reticle == null)
                {
                    BuildReticle();
                    if (_reticle != null) _reticle.gameObject.SetActive(true);
                }
                if (FindObjectOfType<VRPointerVisual>() == null) VRPointerVisual.Ensure();
            }

            // VR：注视中心 + 扳机 点击 UGUI（主菜单与全局按钮在头显内可操作）
            if (VRUI.Active) GazeClick();

            if (Input.GetKeyDown(KeyCode.F8)) LoadNext();
            if (Input.GetKeyDown(KeyCode.F7)) LoadPrev();
            if (Input.GetKeyDown(KeyCode.F9)) BackToMenu();

            // 序幕同款快捷键（序幕场景由 PrologueManager 自己处理，这里跳过防双重触发；
            // 输入框打字时不响应，避免把字母当成快捷键）
            if (!SceneIsPrologue && !IsTyping())
            {
                if (Input.GetKeyDown(KeyCode.P)) ToggleHudPause();
                if (Input.GetKeyDown(KeyCode.R)) ReplayCurrent();
                if (Input.GetKeyDown(KeyCode.H)) ToggleHelp();
                // 进度地图：随时查看旅程进度（序幕场景里 M 被房间切换占用，用工具栏按钮打开）
                if (Input.GetKeyDown(KeyCode.M)) ToggleProgressMap();
                // 隐藏调试跳过：不出现在任何帮助文字里，与序幕 L=SkipStep 同思路
                if (Input.GetKeyDown(KeyCode.L)) LoadNext();
            }
            // F6 强制释放鼠标（部分幕的第一人称控制会锁定并隐藏指针，导致UI无法点击）
            if (Input.GetKeyDown(KeyCode.F6))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Debug.Log("[初见新生] F6 已强制释放鼠标，指针恢复可见，请再尝试点击UI");
            }
            // F10 打印点击链路自检
            if (Input.GetKeyDown(KeyCode.F10)) Diagnose("手动F10");

            // F11 VR诊断：手柄位姿/扳机/代理模型状态
            if (Input.GetKeyDown(KeyCode.F11)) VrDiagnose();

            // 点击桥：UGUI 无命中、序幕自带瞄准也未悬停时，物理射线命中 PrologueInteractable 则代为激活
            if (Input.GetMouseButtonDown(0)) TryBridgeActivate();

            // 全局按钮手动兜底：鼠标抬起时直接判定（输入模块失效时全局按钮也能点到）
            if (Input.GetMouseButtonUp(0) && !VRUI.Active) ManualGlobalClick();

            // 进度卡动效：进度条充能 / 当前幕光晕呼吸 / 换幕脉冲
            AnimateProgressCard();

            // 点击探测：每次左键点击，输出实际命中的UI物体（定位隐形遮挡物）
            if (Input.GetMouseButtonDown(0) && UnityEngine.EventSystems.EventSystem.current != null)
            {
                var es = UnityEngine.EventSystems.EventSystem.current;
                var ped = new UnityEngine.EventSystems.PointerEventData(es);
                ped.position = Input.mousePosition;
                var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                es.RaycastAll(ped, results);
                if (results.Count == 0)
                    Debug.Log("[初见新生/点击探测] 点击处没有命中任何UI，坐标=" + Input.mousePosition);
                else
                {
                    var s = results[0].gameObject;
                    var btn = s.GetComponentInParent<Button>();
                    string extra = "";
                    if (btn != null)
                    {
                        int pc = btn.onClick.GetPersistentEventCount();
                        string m0 = pc > 0 ? (btn.onClick.GetPersistentTarget(0) != null ? btn.onClick.GetPersistentTarget(0).name + "." + btn.onClick.GetPersistentMethodName(0) : "空目标") : "无绑定";
                        var cg = btn.GetComponentInParent<CanvasGroup>();
                        extra = " | 按钮可交互=" + btn.interactable
                              + " 监听=" + pc + "[" + m0 + "]"
                              + (cg != null ? " CanvasGroup(可交互=" + cg.interactable + ",拦截=" + cg.blocksRaycasts + ")" : " 无CanvasGroup");
                    }
                    Debug.Log("[初见新生/点击探测] 命中顶层: " + results[0].gameObject.name
                        + "  层级排序=" + results[0].sortingOrder
                        + "  共" + results.Count + "层"
                        + extra);
                }
            }
        }

        public void LoadNext()
        {
            if (_index < 0) { LoadEntry(0); return; }
            // 幕粒度前进：跳到下一个幕的首场景（第四幕的顺产/剖腹产是并列路径，任一路径中"下一幕"都直达尾声）
            for (int j = _index + 1; j < Entries.Count; j++)
            {
                if (Entries[j].IsActStart) { LoadEntry(j); return; }
            }
            BackToMenu();
        }

        public void LoadPrev()
        {
            if (_index <= 0) { BackToMenu(); return; }
            // 幕粒度后退：跳到前一个幕的首场景
            for (int j = _index - 1; j >= 0; j--)
            {
                if (Entries[j].IsActStart) { LoadEntry(j); return; }
            }
            BackToMenu();
        }

        public void BackToMenu()
        {
            if (_fadeCo != null) { FadeThenLoad(MenuScene); return; }
            SceneManager.LoadScene(MenuScene);
        }

        /// <summary>加载指定下标的幕场景（直选模式也走这里）</summary>
        public static void LoadEntry(int index)
        {
            if (index < 0 || index >= Entries.Count) return;
            if (_inst != null && _inst._fadeBlack != null) { _inst.FadeThenLoad(Entries[index].ScenePath); return; }
            SceneManager.LoadScene(Entries[index].ScenePath);
        }

        /// <summary>加载某一幕的首场景（主菜单直选用）</summary>
        public static void LoadActStart(string actLabel)
        {
            var e = Entries.Find(x => x.IsActStart && x.ActLabel.StartsWith(actLabel));
            if (e == null) { Debug.LogWarning("未找到幕：" + actLabel); return; }
            if (_inst != null && _inst._fadeBlack != null) { _inst.FadeThenLoad(e.ScenePath); return; }
            SceneManager.LoadScene(e.ScenePath);
        }

        // ---------- UI 工具 ----------
        Text MakeText(Transform parent, string content, int size, Color color)
        {
            var t = new GameObject("txt").AddComponent<Text>();
            t.transform.SetParent(parent, false);
            t.font = UiFont.Get();
            t.text = content;
            t.fontSize = size;
            t.color = color;
            t.raycastTarget = false;
            return t;
        }

        Button MakeButton(Transform parent, string label, Color bg, int size)
        {
            var b = new GameObject("btn").AddComponent<Button>();
            b.transform.SetParent(parent, false);
            var img = b.gameObject.AddComponent<Image>();
            // 序幕按钮样式：小圆角深底 + 白字 + 悬停增亮
            img.sprite = ActUIStyle.RoundSmallSprite;
            img.type = Image.Type.Sliced;
            img.color = ActUIStyle.ButtonBg;
            var rt = b.transform as RectTransform;
            var txt = MakeText(b.transform, label, size, ActUIStyle.TextMain);
            var trt = txt.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
            txt.alignment = TextAnchor.MiddleCenter;
            var colors = b.colors;
            colors.highlightedColor = new Color(1.16f, 1.16f, 1.20f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.88f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
            b.colors = colors;
            b.gameObject.AddComponent<UIButtonFeedback>();   // 统一交互反馈
            return b;
        }
    }

    /// <summary>
    /// 全幕通用的键鼠视角：按住右键拖动 = 转动当前主相机（灵敏度与序幕桌面模式一致）。
    /// 以增量方式在 LateUpdate 施加，不与各幕自己的相机控制器抢状态；
    /// VR 模式、以及指针被幕内第一人称控制器锁定（它自己在做鼠标视角）时不介入。
    /// </summary>
    public class DesktopCameraLook : MonoBehaviour
    {
        private const float Sens = 3.2f;

        private void LateUpdate()
        {
            if (VRUI.Active) return;
            if (Cursor.lockState == CursorLockMode.Locked) return;
            if (!Input.GetMouseButton(1)) return;
            var cam = PickCam();
            if (cam == null) return;

            float dx = Input.GetAxis("Mouse X");
            float dy = Input.GetAxis("Mouse Y");
            if (Mathf.Approximately(dx, 0f) && Mathf.Approximately(dy, 0f)) return;

            var e = cam.transform.rotation.eulerAngles;
            float pitch = e.x;
            if (pitch > 180f) pitch -= 360f;
            pitch = Mathf.Clamp(pitch - dy * Sens, -70f, 70f);
            cam.transform.rotation = Quaternion.Euler(pitch, e.y + dx * Sens, e.z);
        }

        /// <summary>取景相机：优先 MainCamera；某些幕的相机没打 MainCamera 标签，兜底取任一在渲染世界的相机</summary>
        private static Camera PickCam()
        {
            var cam = Camera.main;
            if (cam != null) return cam;
            foreach (var c in Camera.allCameras)
            {
                if (c == null || !c.enabled || c.targetTexture != null) continue;
                if (c.cullingMask == 0) continue;            // 被 VR 逻辑静音的相机跳过
                if (c.name.Contains("VRUI")) continue;       // 全局 UI 相机跳过
                return c;
            }
            return null;
        }
    }

    /// <summary>
    /// VR 手柄可视化：两个简易手柄代理模型 + 右手柄射线（视觉参照）。
    /// 实际选择仍以"视线中心准星 + 扳机"为准，射线用于指示手柄朝向。
    /// </summary>
    public class VRPointerVisual : MonoBehaviour
    {
        Transform _right, _left;
        LineRenderer _ray;
        Material _mat;
        // Input System 控制器动作：此环境（Tuanjie+OpenXR）下旧的 UnityEngine.XR.InputDevices
        // 不注册 Action Set、设备永远无效；必须用 InputSystem 显式动作才会建立输入映射
        UnityEngine.InputSystem.InputAction _rp, _rr, _lp, _lr, _rt, _lt;

        // ==================== 第四幕：头部位移碰撞约束（方案A） ====================
        // 只读检查结论：第四幕三个内容场景没有"床"模型；顺产环境 = CameraFarPlane 全景视频
        // （画面锁定视线，头部平移不改变图像，无可碰几何）+ 3 块人像 Quad（唯一真实几何，
        // 自带 CapsuleCollider）。OpenXR 立体渲染会在相机 transform 之上叠加 HMD 真实位姿，
        // 头部平移因此能穿入真实几何。本约束只移动"追踪原点"（uiCam 位置的组成项），分两条路径：
        //   1) 真实几何 → 头部自然位移喂给 CharacterController.Move，被挡住的残差 = 原点回退量；
        //   2) 全景视频 → 无几何体可碰，限制头部相对入幕基准的水平偏移上限（低头/俯仰不受影响）。
        // 不覆盖 HMD 局部位移、不改头部旋转、不锁头部 Y；非第四幕场景完全旁路。
        const float BodyRadius = 0.18f;
        const float BodyHeight = 1.70f;
        const float BodyCenterY = 0.85f;
        /// <summary>头部水平偏移上限（米，相对进入第四幕时的头部基准）。设 0 关闭范围限制。</summary>
        public static float Act4HeadOffsetLimit = 0.35f;

        CharacterController _body;
        HeadConstraintProbe _probe;
        bool _bodyPlaced;
        bool _act4Active;
        bool _act4DiagLogged;
        bool _act4BoundLogged;
        Vector3 _act4HeadBase;
        readonly HashSet<string> _hitLogged = new HashSet<string>();

        static bool IsAct4Scene(string n)
        {
            return n == "顺产" || n == "刨腹产" || n == "手术室";
        }

        void EnsureBody()
        {
            if (_body != null) return;
            // Play 模式下脚本重编译会让引用失效但 DontDestroyOnLoad 物体仍在——按名复用，避免重复碰撞体
            var old = GameObject.Find("VRPlayerCollision");
            if (old != null)
            {
                var oldCc = old.GetComponent<CharacterController>();
                if (oldCc != null)
                {
                    _body = oldCc;
                    _probe = old.GetComponent<HeadConstraintProbe>();
                    if (_probe == null) _probe = old.AddComponent<HeadConstraintProbe>();
                    _bodyPlaced = false; // 复用时重新落位
                    return;
                }
            }
            var go = new GameObject("VRPlayerCollision");
            DontDestroyOnLoad(go); // 与 uiCam 同生命周期，避免切场景销毁打断解算
            var cc = go.AddComponent<CharacterController>();
            cc.radius = BodyRadius;
            cc.height = BodyHeight;
            cc.center = new Vector3(0f, BodyCenterY, 0f);
            cc.slopeLimit = 60f;
            cc.stepOffset = 0.25f;
            cc.skinWidth = 0.02f;
            _probe = go.AddComponent<HeadConstraintProbe>();
            _body = cc;
            _bodyPlaced = false;
        }

        void TeardownBody()
        {
            if (_body == null) return;
            Destroy(_body.gameObject); // 离开第四幕即销毁，避免残骸被其他幕的射线/物理误命中
            _body = null;
            _probe = null;
            _bodyPlaced = false;
        }

        /// <summary>第四幕头部位移约束：返回叠加到追踪原点上的校正向量（0 = 未约束）。</summary>
        Vector3 HeadConstraint(Vector3 anchorPos, Quaternion worldRot, Vector3 headLocal)
        {
            // 头部"自然"世界位置：原点保持锚点不动时，OpenXR 叠加后头部会渲染到的位置
            Vector3 naturalHead = anchorPos + worldRot * headLocal;
            if (!_act4Active)
            {
                _act4Active = true;
                _act4BoundLogged = false;
                _hitLogged.Clear();
                _act4HeadBase = naturalHead; // 每次进入第四幕重新取基准，坐姿偏移不会被误判为越界
            }
            Vector3 correction = Vector3.zero;

            // 1) 真实几何：胶囊中心对齐头部，帧间位移真正调用 Move，由物理滑动/阻挡解算
            EnsureBody();
            if (_body != null)
            {
                Vector3 bodyTarget = naturalHead - Vector3.up * BodyCenterY;
                if (!_bodyPlaced)
                {
                    _body.transform.position = bodyTarget; // 首次直接落位，避免长距离扫掠被中途几何卡住
                    _bodyPlaced = true;
                }
                else
                {
                    Vector3 delta = bodyTarget - _body.transform.position;
                    if (delta.sqrMagnitude > 1e-8f) _body.Move(delta); // 碰撞解算发生在这里
                }
                correction = _body.transform.position - bodyTarget; // 被挡住的部分 = 原点需要回退的量
                LogHitOnce();
            }

            // 2) 全景视频内容：画面无几何体，限制头部水平偏移；Y 分量不动（低头真实保留）
            if (!_act4DiagLogged)
            {
                _act4DiagLogged = true;
                Debug.Log("[VR Collision]\n  scene=" + SceneManager.GetActiveScene().name
                    + "\n  trackingRoot=" + (_body != null ? _body.gameObject.name + "(" + _body.transform.position.ToString("F3") + ")" : "null")
                    + "\n  uiCamera=~VRUI_Camera"
                    + "\n  hmdPosition=" + naturalHead.ToString("F3")
                    + "\n  hmdLocalPosition=" + headLocal.ToString("F3")
                    + "\n  collisionController=" + (_body != null ? "CharacterController(r=" + BodyRadius + ",h=" + BodyHeight + ",center=(0," + BodyCenterY + ",0))" : "null"));
            }
            Vector3 off = naturalHead + correction - _act4HeadBase;
            off.y = 0f;
            float lim = Act4HeadOffsetLimit;
            if (lim > 0f && off.magnitude > lim)
            {
                correction -= off.normalized * (off.magnitude - lim);
                if (!_act4BoundLogged)
                {
                    _act4BoundLogged = true;
                    Debug.Log("[VR Collision] 头部水平偏移触及上限 " + lim.ToString("F2")
                        + "m（第四幕环境为全景视频内容，无真实几何碰撞体，采用范围限制）");
                }
            }
            return correction;
        }

        void LogHitOnce()
        {
            if (_probe == null || _probe.last == null) return;
            var h = _probe.last;
            _probe.last = null;
            if (h.collider == null) return;
            string key = h.collider.name + "#" + h.collider.GetType().Name;
            if (_hitLogged.Add(key))
                Debug.Log("[VR Collision Hit]\n  collider=" + key
                    + "\n  point=" + h.point.ToString("F3")
                    + "\n  normal=" + h.normal.ToString("F3"));
        }

        /// <summary>记录 CharacterController 最近一次物理接触（供一次性碰撞日志读取）。</summary>
        class HeadConstraintProbe : MonoBehaviour
        {
            [System.NonSerialized] public ControllerColliderHit last;
            void OnControllerColliderHit(ControllerColliderHit h) { last = h; }
        }

        public static VRPointerVisual Ensure()
        {
            var ex = FindObjectOfType<VRPointerVisual>();
            if (ex != null) return ex;
            var go = new GameObject("~VRPointerVisual");
            DontDestroyOnLoad(go);
            return go.AddComponent<VRPointerVisual>();
        }

        void Start() { Build(); }

        void Build()
        {
            if (_right != null) return; // 防重复构建

            _mat = new Material(Shader.Find("Sprites/Default"));
            _right = MakeProxy("~Controller_R", new Color(0.95f, 0.95f, 0.95f, 1f));
            _left = MakeProxy("~Controller_L", new Color(0.85f, 0.85f, 0.9f, 1f));

            var rayGo = new GameObject("~Controller_Ray");
            rayGo.transform.SetParent(transform, false); // 同样挂本组件名下防场景切换销毁
            _ray = rayGo.AddComponent<LineRenderer>();
            _ray.material = _mat;
            // C3 射线视觉收敛：服务于交互而非视觉主体——更细、半透明、远端渐隐至无
            _ray.startColor = new Color(MC1.Accent.r, MC1.Accent.g, MC1.Accent.b, 0.55f);
            _ray.endColor = new Color(MC1.Accent.r, MC1.Accent.g, MC1.Accent.b, 0f);
            _ray.startWidth = 0.0045f;
            _ray.endWidth = 0.0008f;
            _ray.positionCount = 2;

            _rp = Act("vrRPos", UnityEngine.InputSystem.InputActionType.Value, "<XRController>{RightHand}/devicePosition");
            _rr = Act("vrRRot", UnityEngine.InputSystem.InputActionType.Value, "<XRController>{RightHand}/deviceRotation");
            _lp = Act("vrLPos", UnityEngine.InputSystem.InputActionType.Value, "<XRController>{LeftHand}/devicePosition");
            _lr = Act("vrLRot", UnityEngine.InputSystem.InputActionType.Value, "<XRController>{LeftHand}/deviceRotation");
            _rt = Act("vrRTrig", UnityEngine.InputSystem.InputActionType.Button, "<XRController>{RightHand}/trigger");
            _lt = Act("vrLTrig", UnityEngine.InputSystem.InputActionType.Button, "<XRController>{LeftHand}/trigger");
            _rg = Act("vrRGrip", UnityEngine.InputSystem.InputActionType.Button, "<XRController>{RightHand}/gripPressed");
            if (_rg != null && _rg.controls.Count == 0)
                _rg = Act("vrRGrip2", UnityEngine.InputSystem.InputActionType.Button, "<XRController>{RightHand}/grip");
            _lg = Act("vrLGrip", UnityEngine.InputSystem.InputActionType.Button, "<XRController>{LeftHand}/gripPressed");
            if (_lg != null && _lg.controls.Count == 0)
                _lg = Act("vrLGrip2", UnityEngine.InputSystem.InputActionType.Button, "<XRController>{LeftHand}/grip");
        }

        UnityEngine.InputSystem.InputAction _rg, _lg;

        /// <summary>右手侧握键(Grip)是否按住（InputSystem优先，旧XR API兜底，主按钮备选）</summary>
        public static bool IsRGripHeld
        {
            get
            {
                var inst = FindObjectOfType<VRPointerVisual>();
                if (inst == null) return false;
                // InputSystem路径
                if (inst._rg != null && inst._rg.IsPressed()) return true;
                // 旧XR API兜底
                var d = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
                bool held;
                if (d.isValid && d.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out held) && held) return true;
                return false;
            }
        }

        /// <summary>左手侧握键(Grip)是否按住（同上三级检测）</summary>
        public static bool IsLGripHeld
        {
            get
            {
                var inst = FindObjectOfType<VRPointerVisual>();
                if (inst == null) return false;
                if (inst._lg != null && inst._lg.IsPressed()) return true;
                var d = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
                bool held;
                if (d.isValid && d.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out held) && held) return true;
                return false;
            }
        }

        /// <summary>任一手柄扳机是否按住（供各幕脚本使用）</summary>
        public static bool IsAnyTriggerHeld
        {
            get
            {
                // 旧XR API（已确认此环境Vive设备有效）
                var d = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
                bool held;
                if (d.isValid && d.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out held) && held) return true;
                var dl = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
                if (dl.isValid && dl.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out held) && held) return true;
                // InputSystem兜底
                var inst = FindObjectOfType<VRPointerVisual>();
                return inst != null && inst.TriggerHeld;
            }
        }

        static UnityEngine.InputSystem.InputAction Act(string name, UnityEngine.InputSystem.InputActionType type, string path)
        {
            var a = new UnityEngine.InputSystem.InputAction(name, type, path);
            a.Enable();
            return a;
        }

        /// <summary>任一手柄扳机是否按下（供注视点击使用）</summary>
        public bool TriggerHeld
        {
            get
            {
                if (_rt != null && _rt.IsPressed()) return true;
                if (_lt != null && _lt.IsPressed()) return true;
                return false;
            }
        }

        /// <summary>右手柄射线的起点与方向（供射线点击使用）</summary>
        public bool GetRightRay(out Vector3 origin, out Vector3 dir)
        {
            origin = Vector3.zero; dir = Vector3.zero;
            if (_right == null || !_right.gameObject.activeSelf) return false;
            origin = _right.position;
            dir = -_right.up;
            return true;
        }

        void OnDisable()
        {
            // 动作随组件销毁释放
            if (_rp != null) _rp.Dispose();
            if (_rr != null) _rr.Dispose();
            if (_lp != null) _lp.Dispose();
            if (_lr != null) _lr.Dispose();
            if (_rt != null) _rt.Dispose();
            if (_lt != null) _lt.Dispose();
        }

        Transform MakeProxy(string name, Color c)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            // 关键1：挂到本组件（DontDestroyOnLoad）名下，否则切换场景时会被销毁——各幕就看不见手柄
            go.transform.SetParent(transform, false);
            // 关键2：删掉图元自带的碰撞体，避免挡住各幕的射线点击
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = _mat;
            r.material.color = c;
            go.transform.localScale = new Vector3(0.04f, 0.09f, 0.04f);
            return go.transform;
        }

        /// <summary>视角后移距离：各幕场景内容离用户的距离（正值后退）。觉得太近就调大。</summary>
        public static float ViewPullback = 3f;

        void LateUpdate()
        {
            // 代理若被销毁（历史版本bug或极端情况）自动重建
            if (_right == null || _left == null) Build();

            // 锚定视角模式：以各幕主相机的位置为取景位（authored），叠加头部旋转可环视；
            // 位置不随头部平移乱跑，避免穿入各幕场景几何。手柄按"相对头部"的偏移映射到锚定系。
            var hmd = UnityEngine.InputSystem.InputSystem.GetDevice<UnityEngine.InputSystem.XR.XRHMD>();
            var uiCam = VRUI.UiCamera;
            var anchor = ResolveAnchor();
            if (hmd == null || uiCam == null || anchor == null) return;

            var headLp = hmd.devicePosition.ReadValue();
            var headLr = hmd.deviceRotation.ReadValue();

            // 双立体相机会互相干扰（视差错乱）：VR模式下让锚定相机停止渲染，
            // 由本相机统一渲染（锚定相机的变换仍保留，即使其组件被各幕禁用也继续可用）
            if (anchor != uiCam && anchor.enabled && anchor.cullingMask != 0)
                anchor.cullingMask = 0;

            // 世界旋转 = 锚点旋转 * 头部旋转；世界位置 = 锚点位置（transform 与组件启用状态无关）
            var worldRot = anchor.transform.rotation * headLr;
            // 视角后移：仅第三幕退3米（暖色墙壁太近），其他幕保持锚点原位
            float pullback = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("ThirdAct") ? ViewPullback : 0f;
            Vector3 originPos = anchor.transform.position - anchor.transform.forward * pullback;
            // 第四幕：头部平移碰撞约束（真实几何走 CharacterController.Move；全景视频内容走水平偏移上限）
            if (IsAct4Scene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name))
                originPos += HeadConstraint(anchor.transform.position, worldRot, headLp);
            else if (_act4Active)
            {
                _act4Active = false;
                TeardownBody();
            }
            uiCam.transform.SetPositionAndRotation(originPos, worldRot);

            // 手动驱动全局画布到相机正前方0.9米（世界空间画布，不依赖失灵的自动跟随）
            var overlay = ActFlow.OverlayRoot;
            if (overlay != null)
            {
                overlay.position = uiCam.transform.position + uiCam.transform.forward * 0.9f;
                overlay.rotation = uiCam.transform.rotation;
            }

            // 手柄：世界位置 = 相机实际位置（含后移） + 世界旋转 * (手柄追踪位 - 头部追踪位)
            PoseHand(_right, _rp, _rr, true, headLp, uiCam.transform.position, worldRot);
            PoseHand(_left, _lp, _lr, false, headLp, uiCam.transform.position, worldRot);
        }

        /// <summary>锚点解析：主相机 → 任一渲染世界的相机 → 任一相机（含被禁用的）</summary>
        static Camera ResolveAnchor()
        {
            var cam = Camera.main;
            if (cam != null) return cam;
            var all = Camera.allCameras;
            for (int i = 0; i < all.Length; i++)
            {
                var c = all[i];
                if (c == null || c == VRUI.UiCamera) continue;
                if ((c.cullingMask & ~(1 << 5)) != 0) return c; // 渲染世界内容的相机
            }
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && all[i] != VRUI.UiCamera) return all[i];
            return FindObjectsOfType<Camera>().Length > 0
                ? System.Linq.Enumerable.FirstOrDefault(FindObjectsOfType<Camera>(), c => c != VRUI.UiCamera)
                : null;
        }

        void PoseHand(Transform t, UnityEngine.InputSystem.InputAction pos, UnityEngine.InputSystem.InputAction rot,
            bool withRay, Vector3 headLp, Vector3 anchorPos, Quaternion worldRot)
        {
            if (t == null) return;
            bool hasPose = pos != null && pos.controls.Count > 0;
            bool hasRot = rot != null && rot.controls.Count > 0;

            // 没有控制器数据时隐藏代理，避免幽灵模型
            if (!hasPose && !hasRot)
            {
                if (t.gameObject.activeSelf) t.gameObject.SetActive(false);
                if (withRay && _ray != null) _ray.enabled = false;
                return;
            }
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            if (withRay && _ray != null) _ray.enabled = true;

            // 锚定系合成：以头部追踪位为原点，把手柄相对头部的偏移旋转到当前视角方向
            if (hasPose)
                t.position = anchorPos + worldRot * (pos.ReadValue<Vector3>() - headLp);
            if (hasRot)
                t.rotation = worldRot * rot.ReadValue<Quaternion>();

            if (withRay && _ray != null)
            {
                // 射线沿手柄 -Y 轴发射，跟随手柄朝向
                Vector3 dir = -t.up;
                _ray.SetPosition(0, t.position + dir * 0.05f);
                _ray.SetPosition(1, t.position + dir * 3f);
            }
        }
    }

    /// <summary>VR 支持：XR 启用时把全局 UI 画布切成头显可见（相机空间锁定）模式</summary>
    public static class VRUI
    {
        public static bool Active
        {
            get
            {
                try { return UnityEngine.XR.XRSettings.enabled && !string.IsNullOrEmpty(UnityEngine.XR.XRSettings.loadedDeviceName); }
                catch { return false; }
            }
        }

        static Camera _uiCam;

        /// <summary>全局 UI 相机（由 VRPointerVisual 每帧用 HMD 位姿手动驱动）</summary>
        public static Camera UiCamera { get { return _uiCam; } }

        static Camera EnsureUiCamera()
        {
            if (_uiCam != null) return _uiCam;
            var go = new GameObject("~VRUI_Camera");
            var cam = go.AddComponent<Camera>();
            // 头部追踪相机渲染一切（含各幕UI层），叠加在最上层；
            // 其他相机由 VR 逻辑统一静音，避免重复渲染与视差冲突
            cam.cullingMask = -1;
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.depth = 100;                       // 最上层
            // 头部位姿由 VRPointerVisual.LateUpdate 从 XRHMD 设备直读驱动（比TrackedPoseDriver可靠）
            UnityEngine.Object.DontDestroyOnLoad(go);
            _uiCam = cam;
            return cam;
        }

        /// <summary>XR 未启用返回 false；启用则把画布挂到专用相机上，头显内跟随视线可见</summary>
        public static bool MakeVisibleInHeadset(Canvas canvas, float planeDistance = 3.1f)
        {
            if (canvas == null || !Active) return false;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = EnsureUiCamera();
            canvas.planeDistance = planeDistance;
            return true;
        }
    }

    /// <summary>UI 字体：与序幕共用同一套系统中文字体（ChanFangVR.PrologueFont），保证全工程字形一致</summary>
    public static class UiFont
    {
        static Font _cached;
        public static Font Get()
        {
            if (_cached != null) return _cached;
            try { var f = ChanFangVR.PrologueFont.Get(); if (f != null) { _cached = f; return _cached; } } catch { }
            if (_cached == null) { try { _cached = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 28); } catch { } }
            if (_cached == null) { try { _cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { } }
            if (_cached == null) { try { _cached = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
            if (_cached == null) { try { _cached = Font.CreateDynamicFontFromOSFont("Arial", 28); } catch { } }
            return _cached;
        }
    }

    /// <summary>配色（原"医疗蓝 MC-1"，现已整体对齐序幕 PrologueDefs 色板，使六幕 UI 风格一致）</summary>
    public static class MC1
    {
        public static Color Hex(string h)
        {
            h = h.Replace("#", "");
            return new Color(
                System.Convert.ToInt32(h.Substring(0, 2), 16) / 255f,
                System.Convert.ToInt32(h.Substring(2, 2), 16) / 255f,
                System.Convert.ToInt32(h.Substring(4, 2), 16) / 255f);
        }
        public static readonly Color Primary = Hex("293647");      // 序幕按钮底（深灰蓝）
        public static readonly Color Accent = Hex("4FC2F7");       // 序幕 Accent
        public static readonly Color AccentLight = Hex("7DD3FF");  // Accent 提亮（悬停）
        public static readonly Color Teal = Hex("8CCC8C");         // 序幕 Success
        public static readonly Color Gold = Hex("FFB84D");         // 序幕 Warn
        public static readonly Color SurfaceDark = Hex("1A212B");  // 序幕 Panel（不透明版）
        public static readonly Color SurfaceDarker = Hex("0D1014");// 序幕 Backdrop
        public static readonly Color TextDim = Hex("8C949E");      // 序幕 IdleGray
    }
}
