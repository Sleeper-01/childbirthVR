using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChuJian.Merge
{
    /// <summary>
    /// 主菜单 —— 全工程 Design Reference（非对称编辑部构图）：
    /// 左 = Hero 主视觉（眉题 / 56pt 标题 / 一句定位，下方刻意留白）；
    /// 右 = 导航列（Primary「开始体验」+ 细分隔线 + 六幕目录行 + Ghost 退出）。
    /// 视觉语言 = ActUIStyle 版式刻度 / 色彩角色 / 三档圆角 / Surface 材质 / 向下投影 / 错峰淡入。
    /// 加载逻辑不变：开始体验 → ActFlow.LoadEntry(0)；目录行 → ActFlow.LoadActStart(幕名)；退出 = 结束运行。
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        private readonly List<(CanvasGroup cg, float delay)> _intro = new List<(CanvasGroup cg, float delay)>();

        void Awake()
        {
            BuildUI();
        }

        void Start()
        {
            StartCoroutine(IntroCo());
        }

        void BuildUI()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            gameObject.AddComponent<GraphicRaycaster>();
            VRUI.MakeVisibleInHeadset(canvas, 3.15f);
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            // —— 背景空间：暗底 + 左上柔光（光有来源）+ 边缘暗角 + 地平暗示 + 左右分区细线 ——
            var bg = NewImage(transform, "bg", ActUIStyle.Backdrop);
            Stretch(bg.rectTransform);

            var glow = NewImage(transform, "glow", new Color(1f, 1f, 1f, 0.05f));
            glow.sprite = ActUIStyle.SoftBlobSprite;
            glow.raycastTarget = false;
            var glrt = glow.rectTransform;
            glrt.anchorMin = glrt.anchorMax = new Vector2(0.5f, 0.5f);
            glrt.anchoredPosition = new Vector2(-490, 240);
            glrt.sizeDelta = new Vector2(1200, 840);

            var vig = NewImage(transform, "vignette", new Color(0f, 0f, 0f, 0.40f));
            vig.sprite = ActUIStyle.VignetteSprite;
            vig.raycastTarget = false;
            Stretch(vig.rectTransform);

            var horizon = NewImage(transform, "horizon", new Color(1f, 1f, 1f, 0.03f));
            var hrt = horizon.rectTransform;
            hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 0f);
            hrt.anchoredPosition = new Vector2(0, 252);
            hrt.sizeDelta = new Vector2(1920, 1);

            var zoneLine = NewImage(transform, "zoneLine", new Color(1f, 1f, 1f, 0.05f));
            var zrt = zoneLine.rectTransform;
            zrt.anchorMin = zrt.anchorMax = new Vector2(0.5f, 0.5f);
            zrt.anchoredPosition = new Vector2(160, 70);
            zrt.sizeDelta = new Vector2(1, 560);

            // —— Hero 主视觉（左）：眉题 → 标题 → 一句定位，下方留白 ——
            var eyebrow = NewText(transform, "VR 产前健康教育体验 · 医院版", ActUIStyle.FontMicro, ActUIStyle.IdleGray);
            eyebrow.alignment = TextAnchor.MiddleLeft;
            var ert = eyebrow.rectTransform;
            ert.anchorMin = ert.anchorMax = ert.pivot = new Vector2(0f, 1f);
            ert.anchoredPosition = new Vector2(140, -192);
            ert.sizeDelta = new Vector2(700, 26);
            RegisterIntro(eyebrow.gameObject, 0f);

            var title = NewText(transform, "初见 · 新生", ActUIStyle.FontHero, ActUIStyle.TextMain);
            title.alignment = TextAnchor.MiddleLeft;
            title.fontStyle = FontStyle.Bold;
            var trt = title.rectTransform;
            trt.anchorMin = trt.anchorMax = trt.pivot = new Vector2(0f, 1f);
            trt.anchoredPosition = new Vector2(136, -238);
            trt.sizeDelta = new Vector2(900, 140);
            RegisterIntro(title.gameObject, 0.06f);

            var positioning = NewText(transform, "把一次分娩的过程，\n提前讲给每一位准妈妈。", ActUIStyle.FontBody, ActUIStyle.IdleGray);
            positioning.alignment = TextAnchor.UpperLeft;
            var prt = positioning.rectTransform;
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0f, 1f);
            prt.anchoredPosition = new Vector2(140, -398);
            prt.sizeDelta = new Vector2(560, 120);
            RegisterIntro(positioning.gameObject, 0.12f);

            // —— 导航列（右）：Primary「开始体验」→ 细分隔线 → 六幕目录行 ——
            var cta = new GameObject("cta_start", typeof(RectTransform));
            cta.transform.SetParent(transform, false);
            var crt = (RectTransform)cta.transform;
            crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(1f, 1f);
            crt.anchoredPosition = new Vector2(-140, -176);
            crt.sizeDelta = new Vector2(620, 92);
            var ctaBtn = cta.AddComponent<Button>();
            ctaBtn.onClick.AddListener(() => ActFlow.LoadEntry(0));

            var ctaBg = NewImage(cta.transform, "bg", new Color(ActUIStyle.Surface.r, ActUIStyle.Surface.g, ActUIStyle.Surface.b, 0.95f));
            ctaBg.sprite = ActUIStyle.RoundSmallSprite;
            ctaBg.type = Image.Type.Sliced;
            Stretch(ctaBg.rectTransform);
            ctaBtn.targetGraphic = ctaBg;
            ActUIStyle.ApplyElevation(ctaBg, 3);

            // 顶缘受光细条（Elevation 规范：高光只在顶部，2px，避开圆角）
            var topLight = NewImage(cta.transform, "topLight", new Color(1f, 1f, 1f, 0.06f));
            var tlt = topLight.rectTransform;
            tlt.anchorMin = new Vector2(0.06f, 1f);
            tlt.anchorMax = new Vector2(0.94f, 1f);
            tlt.pivot = new Vector2(0.5f, 1f);
            tlt.anchoredPosition = new Vector2(0, -1);
            tlt.sizeDelta = new Vector2(0, 2);

            // 左侧 4px Primary 竖条：唯一的强调色，标明「这是主行动」
            var accentBar = NewImage(cta.transform, "accentBar", ActUIStyle.Accent);
            var abrt = accentBar.rectTransform;
            abrt.anchorMin = abrt.anchorMax = abrt.pivot = new Vector2(0f, 0.5f);
            abrt.anchoredPosition = new Vector2(20, 0);
            abrt.sizeDelta = new Vector2(4, 44);

            var ctaLabel = NewText(cta.transform, "开始体验", ActUIStyle.FontHeading, ActUIStyle.TextMain);
            ctaLabel.alignment = TextAnchor.MiddleLeft;
            ctaLabel.fontStyle = FontStyle.Bold;
            var clrt = ctaLabel.rectTransform;
            clrt.anchorMin = Vector2.zero; clrt.anchorMax = Vector2.one;
            clrt.offsetMin = new Vector2(56, 0);
            clrt.offsetMax = new Vector2(-96, 0);

            var ctaArrow = NewText(cta.transform, "→", ActUIStyle.FontHeading, ActUIStyle.Accent);
            var cart = ctaArrow.rectTransform;
            cart.anchorMin = cart.anchorMax = new Vector2(1f, 0.5f);
            cart.pivot = new Vector2(1f, 0.5f);
            cart.anchoredPosition = new Vector2(-30, 0);
            cart.sizeDelta = new Vector2(44, 40);

            cta.AddComponent<UIButtonFeedback>();
            RegisterIntro(cta, 0.2f);

            var hint = NewText(transform, "从序幕按顺序体验至尾声 · 幕内 F8 下一幕 / F9 返回菜单", ActUIStyle.FontMicro, ActUIStyle.IdleGray);
            hint.alignment = TextAnchor.MiddleLeft;
            var hintRt = hint.rectTransform;
            hintRt.anchorMin = hintRt.anchorMax = hintRt.pivot = new Vector2(1f, 1f);
            hintRt.anchoredPosition = new Vector2(-140, -288);
            hintRt.sizeDelta = new Vector2(620, 26);
            RegisterIntro(hint.gameObject, 0.26f);

            var navDivider = NewImage(transform, "navDivider", new Color(1f, 1f, 1f, 0.10f));
            var ndrt = navDivider.rectTransform;
            ndrt.anchorMin = ndrt.anchorMax = ndrt.pivot = new Vector2(1f, 1f);
            ndrt.anchoredPosition = new Vector2(-140, -332);
            ndrt.sizeDelta = new Vector2(620, 1);

            // —— 六幕目录行（导航，不是六个按钮）：编号 + 幕名 + 描述，默认近乎透明，悬停轻提亮 ——
            string[] acts = { "序幕", "第一幕", "第二幕", "第三幕", "第四幕", "尾声" };
            string[] actDesc = { "入院与认识环境", "产前运动", "产房参观", "镇痛选择", "分娩(双路径)", "报告与总结" };
            for (int i = 0; i < acts.Length; i++)
            {
                var row = new GameObject("nav_" + acts[i], typeof(RectTransform));
                row.transform.SetParent(transform, false);
                var rrt = (RectTransform)row.transform;
                rrt.anchorMin = rrt.anchorMax = rrt.pivot = new Vector2(1f, 1f);
                rrt.anchoredPosition = new Vector2(-140, -344 - i * 76);
                rrt.sizeDelta = new Vector2(620, 74);
                string actLabel = acts[i];   // 闭包必须捕获局部副本，否则六行都指向最后一幕
                var rowBtn = row.AddComponent<Button>();
                rowBtn.onClick.AddListener(() => ActFlow.LoadActStart(actLabel));

                var rowBg = row.AddComponent<Image>();
                rowBg.sprite = ActUIStyle.RoundSmallSprite;
                rowBg.type = Image.Type.Sliced;
                rowBg.color = new Color(ActUIStyle.Panel.r, ActUIStyle.Panel.g, ActUIStyle.Panel.b, 0f);   // 默认隐形
                rowBtn.targetGraphic = rowBg;

                var num = NewText(row.transform, "0" + (i + 1), ActUIStyle.FontSmall, ActUIStyle.IdleGray);
                num.alignment = TextAnchor.MiddleLeft;
                var numRt = num.rectTransform;
                numRt.anchorMin = numRt.anchorMax = numRt.pivot = new Vector2(0f, 0.5f);
                numRt.anchoredPosition = new Vector2(22, 0);
                numRt.sizeDelta = new Vector2(44, 26);

                var nameT = NewText(row.transform, acts[i], ActUIStyle.FontBody, ActUIStyle.TextMain);
                nameT.alignment = TextAnchor.MiddleLeft;
                var nrt = nameT.rectTransform;
                nrt.anchorMin = nrt.anchorMax = nrt.pivot = new Vector2(0f, 0.5f);
                nrt.anchoredPosition = new Vector2(82, 0);
                nrt.sizeDelta = new Vector2(200, 30);

                var descT = NewText(row.transform, actDesc[i], ActUIStyle.FontMicro, ActUIStyle.IdleGray);
                descT.alignment = TextAnchor.MiddleRight;
                var drt = descT.rectTransform;
                drt.anchorMin = drt.anchorMax = drt.pivot = new Vector2(1f, 0.5f);
                drt.anchoredPosition = new Vector2(-22, 0);
                drt.sizeDelta = new Vector2(300, 24);

                var fb = row.AddComponent<UIButtonFeedback>();
                fb.hoverScale = 1.01f;      // 目录行：极克制
                fb.pressScale = 0.99f;
                fb.hoverBg = rowBg;         // 悬停整行轻提亮
                fb.hoverColor = new Color(ActUIStyle.Panel.r, ActUIStyle.Panel.g, ActUIStyle.Panel.b, 0.55f);
                RegisterIntro(row, 0.36f + i * 0.045f);
            }

            // —— 辅助信息（最低层级）——
            var ver = NewText(transform, "v1.0 · 初见新生团队", ActUIStyle.FontMicro, ActUIStyle.IdleGray);
            ver.alignment = TextAnchor.MiddleLeft;
            var vrt = ver.rectTransform;
            vrt.anchorMin = vrt.anchorMax = vrt.pivot = new Vector2(0f, 0f);
            vrt.anchoredPosition = new Vector2(140, 52);
            vrt.sizeDelta = new Vector2(400, 26);
            RegisterIntro(ver.gameObject, 0.62f);

            var exit = new GameObject("ghost_exit", typeof(RectTransform));
            exit.transform.SetParent(transform, false);
            var ert2 = (RectTransform)exit.transform;
            ert2.anchorMin = ert2.anchorMax = ert2.pivot = new Vector2(1f, 0f);
            ert2.anchoredPosition = new Vector2(-140, 44);
            ert2.sizeDelta = new Vector2(160, 52);
            var exitBtn = exit.AddComponent<Button>();
            exitBtn.onClick.AddListener(() =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
            var exitBg = exit.AddComponent<Image>();
            exitBg.sprite = ActUIStyle.RoundSmallSprite;
            exitBg.type = Image.Type.Sliced;
            exitBg.color = new Color(ActUIStyle.Panel.r, ActUIStyle.Panel.g, ActUIStyle.Panel.b, 0f);
            exitBtn.targetGraphic = exitBg;
            var exitT = NewText(exit.transform, "退 出", ActUIStyle.FontBody, ActUIStyle.IdleGray);
            var etrt = exitT.rectTransform;
            etrt.anchorMin = Vector2.zero; etrt.anchorMax = Vector2.one;
            etrt.sizeDelta = Vector2.zero;
            var exFb = exit.AddComponent<UIButtonFeedback>();
            exFb.hoverScale = 1.02f;
            exFb.pressScale = 0.98f;
            exFb.hoverBg = exitBg;
            exFb.hoverColor = new Color(ActUIStyle.Panel.r, ActUIStyle.Panel.g, ActUIStyle.Panel.b, 0.40f);
            RegisterIntro(exit, 0.66f);
        }

        // ---------- 入场动效（Motion：仅淡入，160ms，错峰） ----------

        void RegisterIntro(GameObject go, float delay)
        {
            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            _intro.Add((cg, delay));
        }

        IEnumerator IntroCo()
        {
            yield return new WaitForSecondsRealtime(0.2f);   // 与全局黑场淡入衔接
            float t = 0f;
            float maxEnd = 0f;
            foreach (var it in _intro) maxEnd = Mathf.Max(maxEnd, it.delay + 0.16f);
            while (t < maxEnd)
            {
                t += Time.unscaledDeltaTime;
                foreach (var (cg, delay) in _intro)
                    cg.alpha = Mathf.Clamp01((t - delay) / 0.16f);
                yield return null;
            }
            foreach (var (cg, _) in _intro) cg.alpha = 1f;
        }

        // ---------- 基础工具 ----------

        Image NewImage(Transform parent, string name, Color c)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        Text NewText(Transform parent, string content, int size, Color c)
        {
            var go = new GameObject("txt");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = UiFont.Get();
            t.text = content;
            t.fontSize = size;
            t.color = c;
            t.alignment = TextAnchor.MiddleCenter;
            return t;
        }

        void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
        }
    }
}
