using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VRHospitalAI.ThirdAct
{
    /// <summary>
    /// 第三幕总控制器：运行时自动创建所有 UI 和模块。
    /// </summary>
    public class ThirdActController : MonoBehaviour
    {
        public enum ActPhase { DrugIntroduction, ContractionDialog, BreathingAdvanced, PainToolbox, End }

        [Header("子模块引用")]
        public DrugPainModule drugModule;
        public BreathingModule breathingModule;
        public ToolboxModule toolboxModule;

        [Header("场景转场")]
        public CameraFade cameraFade;

        [Header("安心值")]
        public ScoreManager scoreManager;

        [Header("HUD")]
        public ThirdActHUD hud;

        private ActPhase currentPhase;
        private bool isModuleRunning;

        void Start()
        {
            Debug.Log("[第三幕] Start 开始");

            // 1. 确保 ScoreManager 存在
            scoreManager = FindObjectOfType<ScoreManager>();
            if (scoreManager == null)
            {
                var go = new GameObject("ScoreManager");
                scoreManager = go.AddComponent<ScoreManager>();
            }

            // 2. 确保 CameraFade 存在
            cameraFade = FindObjectOfType<CameraFade>();
            if (cameraFade == null)
            {
                var go = new GameObject("FadeCanvas");
                cameraFade = go.AddComponent<CameraFade>();
            }

            // 3. 创建或获取 HUD Canvas
            var canvasGO = GameObject.Find("ThirdActHUD_Canvas");
            Canvas hudCanvas;
            if (canvasGO == null)
            {
                canvasGO = new GameObject("ThirdActHUD_Canvas");
                var canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 2000;
                var scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                canvasGO.AddComponent<GraphicRaycaster>();
                Debug.Log("[第三幕] 创建 Canvas: " + canvasGO.name);
            }
            hudCanvas = canvasGO.GetComponent<Canvas>();

            // 4. 创建或获取 HUD
            var hudGO = GameObject.Find("ThirdActHUD");
            if (hudGO == null)
            {
                hudGO = new GameObject("ThirdActHUD");
                hud = hudGO.AddComponent<ThirdActHUD>();
                hudGO.transform.SetParent(canvasGO.transform, false);
            }
            else
            {
                hud = hudGO.GetComponent<ThirdActHUD>();
            }

            // 5. 确保 EventSystem 存在（UI 射线检测必需）
            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            // 6. 检测是否已有 AutoBuilder 创建的模块（避免重复创建）
            bool autoBuilt = drugModule != null && drugModule.explanationPanel != null;
            Debug.Log("[第三幕] autoBuilt=" + autoBuilt + " drugModule=" + (drugModule != null) +
                     " drugModule.explanationPanel=" + (drugModule?.explanationPanel != null));

            if (!autoBuilt)
            {
                // 7. 创建或获取模块（仅当 AutoBuilder 未运行时）
                EnsureModule<DrugPainModule>("DrugPainModule", ref drugModule);
                EnsureModule<BreathingModule>("BreathingModule", ref breathingModule);
                EnsureModule<ToolboxModule>("ToolboxModule", ref toolboxModule);

                // 8. 为模块构建 UI
                Debug.Log("[第三幕] 构建模块 UI，Canvas: " + hudCanvas.gameObject.name);
                BuildModuleUI(drugModule, hudCanvas);
                BuildModuleUI(breathingModule, hudCanvas);
                BuildModuleUI(toolboxModule, hudCanvas);
            }
            else
            {
                // AutoBuilder 已创建，找到已有的模块引用
                drugModule ??= FindObjectOfType<DrugPainModule>();
                breathingModule ??= FindObjectOfType<BreathingModule>();
                toolboxModule ??= FindObjectOfType<ToolboxModule>();
                Debug.Log("[第三幕] 检测到 AutoBuilder 已运行，跳过 UI 创建");
            }

            // 9. 绑定依赖和事件
            BindModule(drugModule, "DrugPainModule");
            BindModule(breathingModule, "BreathingModule");
            BindModule(toolboxModule, "ToolboxModule");

            // 10. 开始流程（仅当 AutoBuilder 未调用 InitAndStart 时）
            if (isModuleRunning == false && currentPhase == ActPhase.DrugIntroduction && drugModule == null)
            {
                Debug.Log("[第三幕] 初始化完成，进入 DrugIntroduction 阶段（fallback 路径）");
                EnterPhase(currentPhase, true);
            }
        }

        /// <summary>
        /// 由 AutoBuilder 在场景构建完成后调用，跳过 Start 时序问题。
        /// </summary>
        public void InitAndStart()
        {
            Debug.Log("[第三幕] InitAndStart 被 AutoBuilder 调用");

            // 确保依赖项
            scoreManager = FindObjectOfType<ScoreManager>();
            cameraFade = FindObjectOfType<CameraFade>();
            hud = FindObjectOfType<ThirdActHUD>();

            // 确保 EventSystem
            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            // 找到或创建模块
            if (drugModule == null) drugModule = FindObjectOfType<DrugPainModule>();
            if (breathingModule == null) breathingModule = FindObjectOfType<BreathingModule>();
            if (toolboxModule == null) toolboxModule = FindObjectOfType<ToolboxModule>();

            Debug.Log("[第三幕] InitAndStart: drugModule=" + (drugModule != null) +
                     " explanationPanel=" + (drugModule?.explanationPanel != null) +
                     " breathingPanel=" + (breathingModule?.breathingPanel != null) +
                     " toolboxPanel=" + (toolboxModule?.toolboxPanel != null));

            BindModule(drugModule, "DrugPainModule");
            BindModule(breathingModule, "BreathingModule");
            BindModule(toolboxModule, "ToolboxModule");

            isModuleRunning = false;
            currentPhase = ActPhase.DrugIntroduction;
            Debug.Log("[第三幕] 流程启动，进入 DrugIntroduction 阶段");
            EnterPhase(currentPhase, true);
        }

        void EnsureModule<T>(string name, ref T module) where T : MonoBehaviour
        {
            if (module == null)
            {
                var go = new GameObject(name);
                module = go.AddComponent<T>();
                Debug.Log("[第三幕] 创建 " + name);
            }
        }

        void BuildModuleUI(MonoBehaviour module, Canvas canvas)
        {
            if (module == null) return;
            if (module is DrugPainModule drug)
            {
                if (drug.explanationPanel == null)
                {
                    Debug.Log("[第三幕] 构建 DrugPainModule UI，Canvas: " + canvas.gameObject.name);
                    ThirdActAutoBuilder.BuildDrugPainUIModular(drug, canvas);
                }
                else
                {
                    Debug.Log("[第三幕] DrugPainModule UI 已存在，跳过构建");
                }
            }
            else if (module is BreathingModule breathe)
            {
                if (breathe.breathingPanel == null)
                {
                    Debug.Log("[第三幕] 构建 BreathingModule UI");
                    ThirdActAutoBuilder.BuildBreathingUIModular(breathe, canvas);
                }
            }
            else if (module is ToolboxModule toolbox)
            {
                if (toolbox.toolboxPanel == null)
                {
                    Debug.Log("[第三幕] 构建 ToolboxModule UI");
                    ThirdActAutoBuilder.BuildToolboxUIModular(toolbox, canvas);
                }
            }
        }

        void BindModule(MonoBehaviour module, string name)
        {
            if (module == null)
            {
                Debug.LogError("[第三幕] 模块 " + name + " 是 null！");
                return;
            }
            if (module is DrugPainModule drug)
            {
                drug.cameraFade = cameraFade;
                drug.scoreManager = scoreManager;
                drug.hud = hud;
                drug.OnModuleComplete += OnDrugModuleDone;
                drug.OnDrugTalkComplete += OnDrugTalkComplete;
                Debug.Log("[第三幕] 绑定 DrugPainModule，explanationPanel=" + (drug.explanationPanel != null) +
                         " positionDemoPanel=" + (drug.positionDemoPanel != null) +
                         " positionQuizPanel=" + (drug.positionQuizPanel != null) +
                         " contractionPanel=" + (drug.contractionPanel != null));
            }
            else if (module is BreathingModule breathe)
            {
                breathe.cameraFade = cameraFade;
                breathe.scoreManager = scoreManager;
                breathe.hud = hud;
                breathe.OnModuleComplete += OnBreathingModuleDone;
                Debug.Log("[第三幕] 绑定 BreathingModule，breathingPanel=" + (breathe.breathingPanel != null));
            }
            else if (module is ToolboxModule toolbox)
            {
                toolbox.cameraFade = cameraFade;
                toolbox.scoreManager = scoreManager;
                toolbox.hud = hud;
                toolbox.OnToolboxComplete += OnToolboxDone;
                Debug.Log("[第三幕] 绑定 ToolboxModule，toolboxPanel=" + (toolbox.toolboxPanel != null));
            }
        }

        void OnDisable()
        {
            if (drugModule != null)
            {
                drugModule.OnModuleComplete -= OnDrugModuleDone;
                drugModule.OnDrugTalkComplete -= OnDrugTalkComplete;
            }
            if (breathingModule != null) breathingModule.OnModuleComplete -= OnBreathingModuleDone;
            if (toolboxModule != null) toolboxModule.OnToolboxComplete -= OnToolboxDone;
        }

        void Update()
        {
            if (VRInput.MenuDown && hud != null) hud.TogglePause();
            if (hud != null) hud.UpdateScoreUI();
        }

        void EnterPhase(ActPhase phase, bool isFirstEnter = false)
        {
            // 首次进入时不检查重复
            if (!isFirstEnter && phase == currentPhase) return;
            currentPhase = phase;
            isModuleRunning = true;
            Debug.Log("[第三幕] 进入阶段: " + phase);

            // 进入新阶段前，确保所有面板都已关闭
            CleanupAllPanels();

            switch (phase)
            {
                case ActPhase.DrugIntroduction:
                    if (drugModule != null)
                    {
                        Debug.Log("[第三幕] 调用 StartDrugTalk，explanationPanel=" + (drugModule.explanationPanel != null));
                        drugModule.StartDrugTalk();
                        hud?.ShowStepHint("麻醉医生正在为您讲解药物镇痛");
                    }
                    else
                    {
                        Debug.LogError("[第三幕] drugModule 是 null！");
                    }
                    break;
                case ActPhase.ContractionDialog:
                    drugModule?.StartContractionDialog();
                    hud?.ShowStepHint("宫缩来了，请练习如何向护士表达需求");
                    break;
                case ActPhase.BreathingAdvanced:
                    breathingModule?.StartBreathing();
                    hud?.ShowStepHint("跟随光球节奏，练习拉玛泽呼吸");
                    break;
                case ActPhase.PainToolbox:
                    toolboxModule?.StartToolbox();
                    hud?.ShowStepHint("将想使用的镇痛法宝放入待产包");
                    break;
                case ActPhase.End:
                    EndAct();
                    break;
            }
        }

        // 清理所有模块的面板（确保阶段切换时旧面板不残留）
        void CleanupAllPanels()
        {
            if (drugModule != null) drugModule.HideAll();
            if (breathingModule != null) breathingModule.HideAll();
            if (toolboxModule != null) toolboxModule.HideAll();
            Debug.Log("[第三幕] 清理所有面板，进入新阶段");
        }

        void OnDrugTalkComplete()
        {
            Debug.Log("[第三幕] 上篇完成，进入宫缩演练");
            EnterPhase(ActPhase.ContractionDialog);
        }

        void OnDrugModuleDone()
        {
            Debug.Log("[第三幕] 药物镇痛全篇完成");
            isModuleRunning = false;
            EnterPhase(ActPhase.BreathingAdvanced);
        }

        void OnBreathingModuleDone()
        {
            Debug.Log("[第三幕] 呼吸训练完成");
            isModuleRunning = false;
            EnterPhase(ActPhase.PainToolbox);
        }

        void OnToolboxDone(List<int> _)
        {
            Debug.Log("[第三幕] 镇痛决策完成");
            isModuleRunning = false;
            EnterPhase(ActPhase.End);
        }

        void EndAct()
        {
            Debug.Log("[第三幕] 流程结束");
            scoreManager?.AddScore(8);
            hud?.ShowSafetyReminder("第三幕体验完成！");
            // 合并工程：取消结束时的黑屏淡出，保持画面可见，
            // 由 ActFlow 的「进入下一幕/返回主菜单」按钮接管后续跳转
        }

        public void QuitToMenu()
        {
            Debug.Log("[第三幕] 返回主菜单");
            Time.timeScale = 1f;
            hud?.ShowSafetyReminder("已退出第三幕");
        }
    }
}
