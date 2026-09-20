using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace ChanFangVR
{
    /// 待产室世界：房间 + 床 + 交互物布局 + 摄像机（VR/桌面）。
    /// 坐标约定：玩家头部原点在 (0, 0.6, 0)，视线朝 +Z；所有交互物都放在 +Z 侧。
    public class PrologueWorld : MonoBehaviour
    {
        public Camera MainCamera;
        public Transform Head;
        public Transform LeftHand;
        public Transform RightHand;

        public LightDot LightDot;
        public Handbook Handbook;
        public SceneCard[] SceneCards;
        public NurseController Nurse;
        public Renderer[] RoomTints;

        public PrologueUI UI;
        public PrologueAudio Audio;

        public static Sprite RoundSprite;
        public static Sprite CircleSprite;
        public static Texture2D GlowTex;
        public static Texture2D RingTex;
        public static Texture2D WhiteTex;
        public static Texture2D VignetteTex;

        // —— 替换用 GLB 模型（放在 Assets/Resources/Models 下；需安装 UnityGLTF 包 org.khronos.unitygltf，从 git URL 安装后才能导入 .glb）——
        private const string RoomModelPath = "Models/OperatingRoom";
        private const string BedModelPath = "Models/OperatingTable";
        // 若模型比例/朝向/位置不对，改下面这几组常量即可（单位：米，绕 X/Y/Z 度数）
        private static readonly Vector3 RoomModelPos = new Vector3(0f, 0f, 0f);
        private static readonly Vector3 RoomModelRot = new Vector3(0f, 0f, 0f);
        private static readonly Vector3 RoomModelScale = new Vector3(1f, 1f, 1f);
        private static readonly Vector3 BedModelPos = new Vector3(0f, 0.20f, -0.40f);
        private static readonly Vector3 BedModelRot = new Vector3(0f, 0f, 0f);
        private static readonly Vector3 BedModelScale = new Vector3(1f, 1f, 1f);
        // 仅移动相机用：相机（头部位姿）相对头枕床面的抬高量（米）—— 解决相机嵌进床体穿模
        private const float BedHeadClearance = 0.45f;

        // —— 护士模型（Assets/Resources/Models/Nurse）；模型缺失时回退到程序化人形 ——
        private const string NurseModelPath = "Models/Nurse";
        private const float NurseModelHeight = 1.62f;   // 目标身高（米）
        private const float NurseModelYaw = 0f;         // 正面修正：若背对玩家改 180
        // 护士站立高度的额外微调（米）。地面基准自动取手术床腿底，若护士仍略陷/略浮就改这里。
        private const float NurseGroundOffset = 0f;
        // —— 活动室（运动区）模型（Assets/Resources/Models/ActivityRoom）——
        private const string ActivityRoomModelPath = "Models/ActivityRoom";
        private const float ActivityRoomHeight = 3.0f;  // 目标层高（米）
        private static readonly Vector2 ActivityRoomCenterXZ = new Vector2(0f, 0.35f);

        // —— 应急开关：新加的模型/自动测量若导致画面异常，把对应项改 false 即可回到上一版的程序化效果 ——
        private const bool UseActivityRoom = true;      // false = 不使用活动室模型（转场只换色调）
        // —— 活动区（运动区）专用视点：转场后坐到房间右侧那把椅子上 ——
        // 椅子是用几何实测出来的：活动室是整体合并的单个 mesh（没有 chair 节点），
        // 扫描全部近水平面片后，在房间右侧找到一处 0.4x0.4m 的孤立座面
        // （世界坐标 x≈1.30, 座面高 y≈0.55, z≈0.32；左侧 x≈-1.2 那组是桌子）。
        // 坐姿眼睛离座面约 0.72m，所以视点高度取 0.55+0.72≈1.27m。
        private const bool MoveCamToActivityChair = true;   // false = 转场只换色调、不移动视点（回到旧行为）
        private static readonly Vector3 ActivityCamSeat = new Vector3(1.30f, 1.27f, 0.32f);
        private const float ActivityCamYaw = -90f;          // 坐在椅上面朝房间中心（-X 方向）
        // AI 生成的手术室 GLB 常常缺墙（只生成了两面）或墙面法线朝外被背面剔除，
        // 从房间内部看过去前方/右方就是一片虚空。true = 沿房间包围盒内壁补四面墙兜底。
        // 代价：补的是纯色墙，而该房间包围盒有 13x7.7x13 米，墙会显得又远又暗。
        // 若觉得那面墙碍眼，改 false 即可（代价是前方/右方可能又变成虚空）。
        private const bool FillMissingWalls = true;
        // true = 兜底补墙用 Unlit/Color（纯色、不受光）代替 Standard。
        // 补墙占了视野里很大一块面积，Standard 的逐像素光照在 VR 双目下等于画两遍，
        // 纯色墙看不出画质差别，改 false 可回到受光的 Standard 版本。
        private const bool UseCheapWallMat = true;

        // 兜底补墙的颜色（Unlit 不受光，所以这里填的是「房间墙色 × 场景光照后」的等效亮度，
        // 直接填贴图的原始基色会偏暗）。两个房间的墙色不同，各自一个常量：
        // 待产室 = 米白；活动室 = 淡绿（由 ActivityRoom 贴图主色实测：墙 0.56/0.66/0.56，整体平均 0.67/0.66/0.60）。
        private static readonly Color MainWallTint = new Color(0.78f, 0.81f, 0.84f);
        private static readonly Color ActivityWallTint = new Color(0.63f, 0.71f, 0.62f);
        // —— 性能开关（都不减面、不改模型，只减少每帧的重复着色）——
        // true = 补光走顶点光照。Forward 渲染下每个像素光都会让场景多渲染一遍，
        //        场景现有 2 个方向光 → 设成顶点光后渲染量直接减半，画面几乎无差别。
        private const bool FastLighting = true;
        // 抗锯齿档位：0=关、2=2x MSAA、4=4x MSAA。
        // 2x 是「锯齿 / 帧数」的最佳折中：VR 里边缘毛刺主要靠它压，开销又远小于 4x；
        // 觉得卡改 0（回到之前完全关闭的状态），觉得还毛改 4（代价是填充率）。
        private const int AntiAliasingLevel = 2;
        // —— 掉帧兜底：不动模型面数，只调渲染设置 ——
        // 活动区一帧要画 72 万面（活动室 41.7 万 + 护士 30 万），能省的边角（顶点光/Unlit 墙）都省了，
        // 剩下的大头只能靠渲染设置换：纹理档位、关软粒子/实时反射、关 HDR、VR 眼图分辨率缩放。
        private const bool LowQualityMode = true;   // 关软粒子/实时反射等纯浪费项
        private const int TextureLimit = 0;         // 0=贴图原分辨率（清晰）；1=1/2；2=1/4。显存紧就改 1
        private const bool EnableAniso = true;      // 各向异性过滤：斜着看的地面/墙面贴图不再糊，开销极小
        private const float VRRenderScale = 0.85f;  // VR 眼图分辨率：1.0=原生。锯齿明显就往 1.0 调，卡就往 0.7 调
        private const bool DisableHDR = true;       // 关 HDR，省显存带宽（Quest 等一体机收益最明显）
        // true = 自适应渲染缩放：摇头/转场掉帧时自动把 VR 眼图分辨率降下来，帧率回稳后再自动升回去。
        // 这是「不动模型面数」前提下最有效的一招 —— 72 万面的瓶颈本质是每像素要画的东西太多，
        // 降分辨率直接按比例砍掉像素量（0.7 → 像素量只剩 49%），换来的是画面糊一点点。
        // 注：通过 XRSettings 眼图缩放实现，VR 下生效；桌面/编辑器下该值恒为 1，不会有副作用。
        private const bool AdaptiveResolution = true;
        private const float TargetFPS = 72f;        // VR 常见刷新率；桌面可保持 72 或改 60
        private const float MinResScale = 0.6f;     // 分辨率下限，不建议再低（会明显糊）
        // 相机座位默认自动测量床模型得出。若自动结果位置不对，
        // 改成 true 并直接填写下面的 ManualCamSeat（x 左右, y 高度, z 前后）即可固定视点。
        private const bool UseManualCamSeat = false;
        private static readonly Vector3 ManualCamSeat = new Vector3(0f, 1.35f, -0.55f);
        // 自动测量结果的高度钳制范围（米），防止模型包围盒异常把相机算到地板下 / 天花板上
        private const float CamSeatMinY = 0.45f;
        private const float CamSeatMaxY = 2.20f;

        private GameObject _roomMain;       // 待产室（手术室 GLB）
        private GameObject _roomActivity;   // 运动区（活动室 GLB）
        // 地板高度：用手术床的腿底（包围盒 min.y）推算——床是唯一确定站在地板上的道具。
        // 护士/其它道具都以它为地面基准，否则会陷进地板或浮空。
        private float _floorY;

        private Transform _vrHead;
        private Transform _vrCameraOffset;
        private Transform _trackingSpace;
        private bool _vrMode;
        private float _desktopYaw;
        private float _desktopPitch;

        private int _roomIndex;
        // 自适应动态分辨率的采样状态
        private float _resAccum;
        private int _resFrames;
        private float _resScale = 1f;
        private readonly List<Renderer> _tints = new List<Renderer>();
        // 相机座位（头枕上方，半卧位视点）与基础朝向（头枕在 -Z 端时为 180° 朝 +Z 看向护士；在 +Z 端时为 0°）
        private Vector3 _camSeat = new Vector3(0f, 0.99f, -1.0f);
        private float _camYawBase = 0f;
        // 待产室（床上半卧位）的原始视点：切到活动区会临时换成椅子视点，切回来要靠它还原
        private Vector3 _prologueCamSeat = new Vector3(0f, 0.99f, -1.0f);
        private float _prologueCamYaw = 0f;

        public bool VRMode { get { return _vrMode; } }
        /// XR rig 的旋转/位移承载节点（相机挂载点）
        public Transform CameraRig { get { return _vrCameraOffset; } }
        /// 手柄追踪空间：只跟随右摇杆转身增量，不含头部座位偏移
        public Transform TrackingSpace { get { return _trackingSpace; } }

        /// 右摇杆转视角：桌面改变 yaw/pitch；VR 旋转 XR rig（以头部为轴原地转，不会绕世界原点公转）
        public void AddLookDelta(float yawDeg, float pitchDeg)
        {
            _desktopYaw += yawDeg;
            _desktopPitch = Mathf.Clamp(_desktopPitch + pitchDeg, -70f, 70f);
            if (_vrMode)
            {
                if (_vrCameraOffset != null)
                    _vrCameraOffset.localRotation = Quaternion.Euler(0f, _camYawBase + _desktopYaw, 0f);
                if (_vrHead != null)
                    _vrHead.localRotation = Quaternion.Euler(_desktopPitch, 0f, 0f);
            }
            if (_trackingSpace != null)
                _trackingSpace.localRotation = Quaternion.Euler(0f, _desktopYaw, 0f);
        }

        // ———— 程序化资源 ————

        public static Material Mat(Color c, float smoothness, float metallic)
        {
            var shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Diffuse");
            var m = new Material(shader);
            m.color = c;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            return m;
        }

        /// 兜底补墙专用材质：纯色、不受光。shader 缺失时自动退回 Standard 版本。
        private static Material MakeWallMat(Color tint)
        {
            if (UseCheapWallMat)
            {
                var unlit = Shader.Find("Unlit/Color");
                if (unlit != null)
                {
                    var m = new Material(unlit);
                    m.color = tint;
                    return m;
                }
            }
            return Mat(tint, 0.05f, 0.15f);
        }

        public static void Init()
        {
            if (RoundSprite != null) return;
            RoundSprite = MakeRoundSprite(256);
            CircleSprite = MakeCircleSprite(256);
            GlowTex = MakeGlowTex(256);
            RingTex = MakeRingTex(256);
            WhiteTex = MakeWhiteTex(4);
            VignetteTex = MakeVignetteTex(256);
        }

        private static Sprite MakeRoundSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - r;
                    float dy = y - r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d / r));
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private static Sprite MakeCircleSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - r;
                    float dy = y - r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, d < r * 0.9f ? 1f : 0f);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private static Texture2D MakeGlowTex(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - r;
                    float dy = y - r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d / r);
                    float v = a * a * 0.8f;
                    pixels[y * size + x] = new Color(v, v, v, a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static Texture2D MakeRingTex(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - r;
                    float dy = y - r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Abs(d - r * 0.78f) < r * 0.09f ? 1f : 0f;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static Texture2D MakeWhiteTex(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static Texture2D MakeVignetteTex(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - r;
                    float dy = y - r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d / r);
                    float v = 1f - a * a * 0.9f;
                    pixels[y * size + x] = new Color(0f, 0f, 0f, v);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// 掉帧兜底：完全不动模型面数，只调渲染设置。
        /// 画面会略糊，但这是不改模型的前提下提升帧数最直接的一组开关；
        /// 想还原把顶部对应常量改回（LowQualityMode=false / VRRenderScale=1.0 / DisableHDR=false）即可。
        private void ApplyPerfSettings(Camera uiCam)
        {
            if (DisableHDR)
            {
                MainCamera.allowHDR = false;
                if (uiCam != null) uiCam.allowHDR = false;
            }
            if (!LowQualityMode) return;

            QualitySettings.globalTextureMipmapLimit = TextureLimit;     // 0=原生 1=1/2 2=1/4
            // 各向异性只影响斜视角的贴图采样，开销极小但能让地面/墙面恢复清晰，性价比很高
            QualitySettings.anisotropicFiltering = EnableAniso
                ? AnisotropicFiltering.Enable
                : AnisotropicFiltering.Disable;
            QualitySettings.softParticles = false;
            QualitySettings.realtimeReflectionProbes = false;
            QualitySettings.shadowDistance = 0f;                             // 本项目本来没开阴影，兜底清零
            QualitySettings.shadowCascades = 1;
            QualitySettings.pixelLightCount = 1;
            QualitySettings.vSyncCount = 0;                                  // 不锁垂直同步，避免被压到 30fps

            // 只在 VR 下有意义：眼图分辨率缩放。桌面/编辑器下该值为 1，不影响预览。
            if (VRRenderScale > 0f && VRRenderScale < 1f)
                XRSettings.eyeTextureResolutionScale = VRRenderScale;

            Debug.Log(string.Format(
                "[PERF] 画质设置：MSAA={0}x 纹理={1} 各向异性={2} 软粒子=关 实时反射=关 HDR={3} VR眼图={4:0.00} vSync={5}",
                QualitySettings.antiAliasing,
                QualitySettings.globalTextureMipmapLimit == 0 ? "原分辨率" : "1/2 分辨率",
                EnableAniso ? "开" : "关",
                MainCamera.allowHDR ? "开" : "关",
                XRSettings.eyeTextureResolutionScale,
                QualitySettings.vSyncCount));
        }

        // ———— 构建 ————

        public static PrologueWorld Create()
        {
            Init();
            var go = new GameObject("PrologueWorld");
            var world = go.AddComponent<PrologueWorld>();
            world.Build();
            return world;
        }

        private void Build()
        {
            // 摄像机：优先复用场景中已存在的 MainCamera（编辑期预览 / 去掉 "no cameras rendering" 警告），
            // 否则运行时动态创建。这样无论编辑期还是运行期都只有一台主相机，避免重复渲染。
            var camGo = GameObject.FindWithTag("MainCamera");
            if (camGo == null)
            {
                camGo = new GameObject("MainCamera");
                camGo.tag = "MainCamera";
            }
            MainCamera = camGo.GetComponent<Camera>();
            if (MainCamera == null)
            {
                MainCamera = camGo.AddComponent<Camera>();
                MainCamera.clearFlags = CameraClearFlags.SolidColor;
                MainCamera.backgroundColor = new Color(0.09f, 0.11f, 0.14f);
                MainCamera.nearClipPlane = 0.05f;
                MainCamera.farClipPlane = 60f;
                MainCamera.fieldOfView = 60f;
                MainCamera.orthographic = false;
                MainCamera.allowHDR = false;
                MainCamera.allowMSAA = true;
            }
            if (camGo.GetComponent<AudioListener>() == null)
                camGo.AddComponent<AudioListener>();

            // HUD 专用相机：只渲染 UI 层，叠在世界相机之上（depth 更高、clearFlags=Depth），
            // 保证头部 HUD（字幕/任务卡/工具栏）永不因 WorldSpace Canvas 处于 z=1.5 而被房间/床/护士等
            // 几何体遮挡（即「字母被阻挡」）；同时工具栏按钮的点击射线只走 UI 层，世界几何不会拦截点击。
            var uiCamGo = new GameObject("UICamera");
            uiCamGo.transform.SetParent(MainCamera.transform, false);
            var uiCam = uiCamGo.AddComponent<Camera>();
            uiCam.clearFlags = CameraClearFlags.Depth;
            uiCam.depth = 10f;
            uiCam.cullingMask = 1 << 5;            // 仅渲染 UI 层（默认 layer 5）
            uiCam.nearClipPlane = 0.01f;
            uiCam.farClipPlane = 5f;
            uiCam.fieldOfView = MainCamera.fieldOfView;
            uiCam.orthographic = MainCamera.orthographic;
            uiCam.allowHDR = MainCamera.allowHDR;
            uiCam.allowMSAA = MainCamera.allowMSAA;
            uiCam.enabled = true;
            // 世界相机不再渲染 HUD，避免重复绘制与深度遮挡
            MainCamera.cullingMask = ~(1 << 5);

            // 抗锯齿：主相机和 UI 相机一起开，否则 UI 文字边缘仍然有毛刺。
            // Play 模式下的改动不会写回工程，退出即还原。
            bool msaa = AntiAliasingLevel > 0;
            MainCamera.allowMSAA = msaa;
            uiCam.allowMSAA = msaa;
            QualitySettings.antiAliasing = msaa ? AntiAliasingLevel : 0;
            ApplyPerfSettings(uiCam);

            // XR rig：CameraOffset 承载追踪空间，Head 承载头显位姿
            var rig = new GameObject("XRRig");
            rig.transform.SetParent(transform, false);
            _vrCameraOffset = new GameObject("CameraOffset").transform;
            _vrCameraOffset.SetParent(rig.transform, false);
            _vrHead = new GameObject("Head").transform;
            _vrHead.SetParent(_vrCameraOffset, false);

            // 手柄追踪空间：只跟随右摇杆的转身增量（不含头部座位偏移），
            // 这样手柄位姿不会被抬到头部高度，转身后射线方向也保持一致。
            _trackingSpace = new GameObject("TrackingSpace").transform;
            _trackingSpace.SetParent(transform, false);

            SetVRMode(false);

            PrologueRun.Cam = MainCamera;
            PrologueRun.VRMode = false;

            BuildRoom();
            BuildLights();
            BuildProps();
            ClampCamSeat();

            // 记下待产室（床上半卧位）视点：转场去活动区坐椅子后，切回来靠它还原
            _prologueCamSeat = _camSeat;
            _prologueCamYaw = _camYawBase;

            // 诊断日志：按 Play 后在 Console 搜 "[CAM]" 就能看到视点是怎么算出来的
            Debug.Log(string.Format(
                "[CAM] 最终视点 camSeat={0} yawBase={1:F0} | 房间={2} | 活动室={3}",
                _camSeat, _camYawBase,
                _roomMain != null ? (_roomMain.name + " bounds=" + CalcWorldBounds(_roomMain)) : "程序化房间",
                _roomActivity != null ? "已加载(默认隐藏)" : "未加载"));
        }

        private void BuildRoom()
        {
            // 优先用 GLB 手术室模型替换程序化房间（未安装 GLTF 包 / 模型缺失时自动回退到下方程序化房间）
            var room = TryLoadModel(RoomModelPath, "OperatingRoom");
            if (room != null)
            {
                // 直接按手动常量摆放。注意：该 GLB 的 accessor.min/max 元数据有误（地板被标成 -2.4），
                // 若用包围盒 min.y 自动“落地”会把整个房间抬高 ~2.4m，导致所有道具被压到地板下。
                // 实测模型真实地板就在 y≈0，故不再做包围盒抬升；如房间漂浮/下沉，改 RoomModelPos.y 即可。
                room.transform.localPosition = RoomModelPos;
                room.transform.localRotation = Quaternion.Euler(RoomModelRot);
                room.transform.localScale = RoomModelScale;
                _roomMain = room;
                if (FillMissingWalls) AddInnerWalls(room, MainWallTint);
                BuildActivityRoom();
                return;
            }

            var floorMat = Mat(new Color(0.72f, 0.70f, 0.66f), 0.15f, 0f);
            var wallMat = Mat(new Color(0.88f, 0.89f, 0.86f), 0.1f, 0f);
            var accentMat = Mat(new Color(0.82f, 0.88f, 0.90f), 0.2f, 0f);

            AddSolid(floorMat, "Floor", new Vector3(0f, -0.01f, 0f), new Vector3(6f, 0.02f, 6f));
            AddSolid(wallMat, "WallBack", new Vector3(0f, 1.5f, -2.6f), new Vector3(6f, 3f, 0.1f));
            AddSolid(wallMat, "WallFront", new Vector3(0f, 1.5f, 3.0f), new Vector3(6f, 3f, 0.1f));
            AddSolid(accentMat, "WallLeft", new Vector3(-2.6f, 1.5f, 0f), new Vector3(0.1f, 3f, 6f));
            AddSolid(accentMat, "WallRight", new Vector3(2.6f, 1.5f, 0f), new Vector3(0.1f, 3f, 6f));
            AddSolid(Mat(new Color(0.95f, 0.96f, 0.97f), 0.3f, 0f), "Ceiling",
                new Vector3(0f, 3.0f, 0f), new Vector3(6f, 0.1f, 6f));

            // 呼叫铃（墙上，后续幕使用，先做视觉）
            var bell = AddSolid(Mat(new Color(0.95f, 0.75f, 0.30f), 0.4f, 0.1f), "CallBell",
                new Vector3(-2.5f, 1.25f, 1.2f), new Vector3(0.06f, 0.16f, 0.16f));
            _tints.Add(bell);

            BuildActivityRoom();
        }

        /// 沿房间包围盒内壁补四面墙。
        /// AI 生成的房间模型经常缺墙、或墙的法线朝外被背面剔除，从房间里看就是一片虚空。
        /// 墙挂成 room 的子物体，转场时跟着房间一起隐藏。
        private void AddInnerWalls(GameObject room, Color tint)
        {
            var b = CalcWorldBounds(room);
            // 兜底墙是纯色、不需要受光，却占了视野里很大一块面积（3.7x3 米、VR 双目各画一遍）。
            // 用 Unlit/Color 代替 Standard：省掉每像素的光照/PBS 计算，转动视角时填充率压力明显下降。
            var mat = MakeWallMat(tint);
            // 关键：向外偏移而不是向内内缩。
            // 房间模型的墙多为单薄片（没有厚度），包围盒边界≈墙面本身；
            // 若向内内缩 0.1m，补出来的纯色 Cube 会糊在真墙前面 10cm 处，
            // 把带贴图的墙面完全遮住 —— 看起来就像"房间变成空白贴图"。
            // 改为贴着包围盒外侧放：有真墙的方向补墙被真墙挡住（看不见），
            // 缺墙的方向（前方/右方）才真正补出一面墙。
            float inset = -0.03f;                // 负值 = 向外推，避免遮挡原有墙面
            float t = 0.06f;                     // 墙厚
            float h = b.size.y, cy = b.center.y;

            AddWallPlane(room, mat, "WallInner+X", new Vector3(b.max.x - inset, cy, b.center.z), new Vector3(t, h, b.size.z));
            AddWallPlane(room, mat, "WallInner-X", new Vector3(b.min.x + inset, cy, b.center.z), new Vector3(t, h, b.size.z));
            AddWallPlane(room, mat, "WallInner+Z", new Vector3(b.center.x, cy, b.max.z - inset), new Vector3(b.size.x, h, t));
            AddWallPlane(room, mat, "WallInner-Z", new Vector3(b.center.x, cy, b.min.z + inset), new Vector3(b.size.x, h, t));
        }

        private static void AddWallPlane(GameObject room, Material mat, string name,
                                         Vector3 worldCenter, Vector3 worldSize)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(room.transform, false);
            go.transform.localPosition = room.transform.InverseTransformPoint(worldCenter);
            // 关键：GLB 根节点带绕 X +90° 的旋转（Blender 的 Z-up→Y-up 转换，nodes[0].rotation 实测
            // = [0.7071,0,0,0.7071]）。挂在这种父节点下：
            //   1) 朝向：子物体 localRotation 若是 identity，世界朝向 = 父的 90° → 竖直墙被转成横板；
            //      所以 localRotation 取父节点世界旋转的逆，让墙在世界空间保持轴对齐。
            //   2) 尺寸：世界尺寸 = R * S * R⁻¹ * localScale，缩放均匀时 R·S·R⁻¹ = S，
            //      即 worldSize = scale * localScale → 直接 worldSize/lossyScale 即可，
            //      **不能**再对尺寸做一次旋转置换（那会把 y/z 互换，墙又变回横的）。
            go.transform.localRotation = Quaternion.Inverse(room.transform.rotation);
            var ls = room.transform.lossyScale;
            go.transform.localScale = new Vector3(
                Mathf.Abs(worldSize.x) / (Mathf.Abs(ls.x) < 1e-5f ? 1f : Mathf.Abs(ls.x)),
                Mathf.Abs(worldSize.y) / (Mathf.Abs(ls.y) < 1e-5f ? 1f : Mathf.Abs(ls.y)),
                Mathf.Abs(worldSize.z) / (Mathf.Abs(ls.z) < 1e-5f ? 1f : Mathf.Abs(ls.z)));
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var wb = r.bounds;
            Debug.Log(string.Format("[WALL] {0}: 世界尺寸={1:F2}x{2:F2}x{3:F2}（y 应为层高、不是厚度）中心={4}",
                name, wb.size.x, wb.size.y, wb.size.z, wb.center));
        }

        /// 运动区（活动室）模型：默认隐藏，选中「运动区」场景卡时显示
        private void BuildActivityRoom()
        {
            if (!UseActivityRoom) return;     // 应急开关关掉时完全不加载活动室模型
            var go = TryLoadModel(ActivityRoomModelPath, "ActivityRoom");
            if (go == null) return;
            // 生成模型通常被归一化到约 1 单位，按目标层高整体缩放并对齐地面/水平中心。
            // autoFixOrientation 必须传 false：活动室是「扁平体」，宽 0.746 / 深 0.746 / 高 0.604，
            // 深度和宽度几乎相等，而 Unity 的 Renderer.bounds 与顶点包围盒有微小差异，
            // 那个「z > y 且 z >= x 就补一次绕 X 90°」的启发式会在 z 和 x 打平时误触发
            // —— 整个房间连同球池、床一起被转躺下，原本竖直的床板就横在房间中间。
            // （人形模型那种「Z 远大于 X」的细长体才需要这个兜底。）
            FitToHeight(go, ActivityRoomHeight, ActivityRoomCenterXZ, false);
            // 活动室和待产室一个毛病：前方/右方的墙没生成，从房间里看是一片虚空。
            // 同样沿包围盒外侧补四面兜底墙（补墙挂在房间下，转场隐藏时一起隐藏）。
            // 必须在 FitToHeight 之后调用 —— 否则拿到的是缩放前的包围盒，墙会补错位置。
            if (FillMissingWalls) AddInnerWalls(go, ActivityWallTint);
            go.SetActive(false);
            _roomActivity = go;
        }

        /// 按目标高度等比缩放模型，把底面放到 y=0，并把水平中心对齐到 centerXZ
        /// <param name="autoFixOrientation">
        /// 是否允许「最长轴是 Z 就补一次绕 X 90° 旋转」的兜底。
        /// 该启发式对「细长人形」有效，但房间是扁平体（深度常常大于层高），
        /// 开着会把已经立好的房间再转躺下 —— 传 false 关闭。
        /// </param>
        private static void FitToHeight(GameObject go, float targetHeight, Vector2 centerXZ,
                                       bool autoFixOrientation = true)
        {
            var b = CalcWorldBounds(go);

            // 同护士：glTF 根节点自带绕 X 轴 +90° 的「立起来」旋转，若导入器没应用，
            // 最长轴就会是 Z 而不是 Y —— 那样按 b.size.y 缩放等于按厚度缩放，房间会被放大好几倍。
            if (autoFixOrientation && b.size.z > b.size.y && b.size.z >= b.size.x)
            {
                go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f) * go.transform.localRotation;
                b = CalcWorldBounds(go);
            }

            float h0 = b.size.y;
            float s = (h0 > 0.001f) ? (targetHeight / h0) : 1f;
            go.transform.localScale = Vector3.Scale(go.transform.localScale, Vector3.one * s);

            b = CalcWorldBounds(go);
            go.transform.position += new Vector3(centerXZ.x - b.center.x, -b.min.y, centerXZ.y - b.center.z);

            Debug.Log(string.Format(
                "[FIT] {0}: 原高={1:F3}m → 放大 {2:F2} 倍 → 层高={3:F2}m | 最终 min={4} max={5} 底面y={6:F3}",
                go.name, h0, s, b.size.y, b.min, b.max, b.min.y));
        }

        private Renderer AddSolid(Material mat, string name, Vector3 pos, Vector3 scale,
                                  bool hide = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            Object.Destroy(go.GetComponent<Collider>());
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (hide) go.SetActive(false);
            return r;
        }

        // 从 Resources 加载 GLB 模型预制体并实例化到本世界下；导入包缺失或文件不存在时返回 null（由调用方回退）。
        private GameObject TryLoadModel(string resPath, string name)
        {
            var prefab = Resources.Load<GameObject>(resPath);
            if (prefab == null) return null;
            var go = Instantiate(prefab, transform, false);
            go.name = name;
            return go;
        }

        // 计算模型世界包围盒（含所有子 Renderer），用于自动归一化与落地
        private static Bounds CalcWorldBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs == null || rs.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.1f);
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        // 仅读取床模型包围盒，计算头枕端（倾斜抬高端）上方的相机座位；不修改床本身。
        // 头枕端判定：z 哪半边的几何更高（手术床床头倾斜抬高），相机就坐在那一端上方、面朝床尾。
        private void ComputeHeadrestSeat(GameObject bed)
        {
            var b = CalcWorldBounds(bed);
            _floorY = b.min.y;              // 床腿底 = 地板高度，供护士/其它道具贴地用
            float midZ = b.min.z + b.size.z * 0.5f;
            float yNeg = -1e9f, yPos = -1e9f;
            foreach (var r in bed.GetComponentsInChildren<Renderer>())
            {
                if (r.bounds.center.z <= midZ) yNeg = Mathf.Max(yNeg, r.bounds.max.y);
                else yPos = Mathf.Max(yPos, r.bounds.max.y);
            }
            bool headAtNegZ = yNeg >= yPos;                // 头枕（抬高端）在 -Z 还是 +Z
            float headZ = headAtNegZ ? (b.min.z + b.size.z * 0.18f) : (b.max.z - b.size.z * 0.18f);
            float headSurfaceY = headAtNegZ ? yNeg : yPos;
            _camSeat = new Vector3(b.center.x, headSurfaceY + BedHeadClearance, headZ);
            // Unity 相机看向自身局部 +Z，所以 yaw = atan2(dir.x, dir.z)。
            // 视线方向 = 头枕端 -> 床尾端；头在 -Z 时 dir=(0,0,+1) => yaw=0（面向 +Z，正对护士/手册/场景卡）。
            float footZ = headAtNegZ ? b.max.z : b.min.z;
            Vector3 dir = new Vector3(0f, 0f, footZ - headZ);
            if (dir.sqrMagnitude < 1e-6f) dir = headAtNegZ ? Vector3.forward : Vector3.back;
            _camYawBase = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

            Debug.Log(string.Format(
                "[CAM] 床包围盒 min={0} max={1} | 头枕在{2}端 surfaceY={3:F3} headZ={4:F3} => camSeat={5} yawBase={6:F0}",
                b.min, b.max, headAtNegZ ? "-Z" : "+Z", headSurfaceY, headZ, _camSeat, _camYawBase));
        }

        /// 对自动测出的相机座位做安全钳制：高度限制在 CamSeatMinY~CamSeatMaxY，
        /// 避免模型包围盒异常（本项目 OperatingRoom.glb 的元数据就是错的）把相机算到地板下或天花板上。
        private void ClampCamSeat()
        {
            if (UseManualCamSeat)
            {
                _camSeat = ManualCamSeat;
                return;
            }
            float y = Mathf.Clamp(_camSeat.y, CamSeatMinY, CamSeatMaxY);
            if (Mathf.Abs(y - _camSeat.y) > 0.001f)
                Debug.LogWarning(string.Format("[CAM] 自动测出的相机高度 {0:F3} 超出合理范围，已钳制为 {1:F3}（可改 UseManualCamSeat 手动指定）", _camSeat.y, y));
            _camSeat.y = y;

            // 若视点落到房间包围盒之外（模型坐标异常或头枕端判定反了），回退到手动视点，
            // 避免出现「相机跑到地图底下 / 房间外面」这类看不见内容的状况。
            if (_roomMain != null)
            {
                var rb = CalcWorldBounds(_roomMain);
                var p = transform.TransformPoint(_camSeat);
                if (p.x < rb.min.x || p.x > rb.max.x || p.z < rb.min.z || p.z > rb.max.z)
                {
                    Debug.LogWarning(string.Format(
                        "[CAM] 视点 {0} 落在房间包围盒 {1}~{2} 之外，已回退到手动视点 {3}",
                        p, rb.min, rb.max, ManualCamSeat));
                    _camSeat = ManualCamSeat;
                }
            }
        }

        private void BuildLights()
        {
            var lightGo = new GameObject("KeyLight");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localRotation = Quaternion.Euler(52f, -28f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.98f, 0.94f);
            light.intensity = 1.15f;
            light.shadows = LightShadows.None;

            var fillGo = new GameObject("FillLight");
            fillGo.transform.SetParent(transform, false);
            fillGo.transform.localRotation = Quaternion.Euler(20f, 160f, 0f);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.82f, 0.90f, 1.00f);
            fill.intensity = 0.45f;
            fill.shadows = LightShadows.None;
            // 补光改顶点光照：前向渲染中每多一个「像素光」整个场景就要多画一遍，
            // 把它降级为顶点光后渲染量减半，而补光本身很弱（0.45），画面几乎看不出差别。
            fill.renderMode = FastLighting ? LightRenderMode.ForceVertex : LightRenderMode.Auto;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.45f, 0.50f);
        }

        private void BuildProps()
        {
            // —— 病床 / 手术床：优先用 GLB 模型替换；缺失时回退程序化床 ——
            var bed = TryLoadModel(BedModelPath, "OperatingTable");
            if (bed == null)
            {
                var frameMat = Mat(new Color(0.55f, 0.57f, 0.60f), 0.4f, 0.35f);
                var sheetMat = Mat(new Color(0.94f, 0.96f, 0.99f), 0.2f, 0f);

                AddSolid(frameMat, "BedFrame", new Vector3(0f, 0.36f, -0.40f), new Vector3(1.00f, 0.16f, 2.00f));
                AddSolid(sheetMat, "Mattress", new Vector3(0f, 0.48f, -0.42f), new Vector3(0.96f, 0.12f, 1.88f));
                AddSolid(frameMat, "HeadBoard", new Vector3(0f, 0.80f, -1.38f), new Vector3(1.00f, 0.72f, 0.08f));
                AddSolid(frameMat, "FootBoard", new Vector3(0f, 0.66f, 0.56f), new Vector3(1.00f, 0.44f, 0.06f));
                // 枕头（半卧位靠背）
                AddSolid(sheetMat, "Pillow", new Vector3(0f, 0.62f, -1.05f), new Vector3(0.62f, 0.16f, 0.34f));
                _camSeat = new Vector3(0f, 0.99f, -1.0f); // 程序化床：头枕在 -Z 床头上方
                _camYawBase = 0f;
            }
            else
            {
                // 按常量摆放（y 只是额外抬高量，床会自动落到房间地板上），
                // 不做缩放/翻转；仅据床模型算出相机座位
                bed.transform.localPosition = BedModelPos;
                bed.transform.localRotation = Quaternion.Euler(BedModelRot);
                bed.transform.localScale = BedModelScale;

                // 只按手动常量摆放床，不做任何缩放/翻转/移位；仅据床模型算出相机座位
                ComputeHeadrestSeat(bed);
            }

            // —— 床尾校准光点（步骤1）——
            LightDot = LightDot.Create(transform, new Vector3(0f, 0.70f, 0.72f));

            // —— 床旁桌 + 手册（步骤3）——
            AddSolid(Mat(new Color(0.80f, 0.76f, 0.70f), 0.3f, 0f), "BedsideTable",
                new Vector3(0.88f, 0.35f, 0.86f), new Vector3(0.46f, 0.70f, 0.46f));
            Handbook = Handbook.Create(transform, new Vector3(0.88f, 0.74f, 0.86f));
            // 桌上姿态：书根 -Z 面（内容面）朝向相机，故不要额外绕 Y 翻转
            Handbook.transform.localRotation = Quaternion.Euler(-18f, 0f, 0f);
            Handbook.RememberHome();

            // —— 场景卡（步骤4，默认隐藏）——
            // 放在相机正前方、略高于视线处：用相机朝向推算，确保始终在视线内且不被床/工具栏挡住射线。
            SceneCards = new SceneCard[3];
            Vector3 fwd = Quaternion.Euler(0f, _camYawBase, 0f) * Vector3.forward; // 相机前向（Unity 相机看向 +Z）
            Vector3 cardCenter = _camSeat + fwd * 2.4f;
            cardCenter.y = _camSeat.y + 0.12f;
            for (int i = 0; i < 3; i++)
            {
                float x = -0.52f + i * 0.52f;
                SceneCards[i] = SceneCard.Create(transform, new Vector3(cardCenter.x + x, cardCenter.y, cardCenter.z), i);
            }

            // —— 护士小安 ——
            // y 传地板高度（_floorY 来自房间模型底面），否则护士会陷进地板或浮空
            Vector3 nurseSeat = new Vector3(-0.92f, _floorY + NurseGroundOffset, 1.05f);
            Nurse = NurseController.Create(transform, nurseSeat, 0f);
            Nurse.FaceTo(new Vector3(0f, _floorY + 0.6f, 0f));

            RoomTints = _tints.ToArray();

            // 启动诊断：Console 搜索 [PERF] 可看到每个顶层物体的三角形数，F3 开关 FPS 面板。
            // 只读，不修改任何画质设置。
            PerfDump.Log(transform, MainCamera);
            PerfDump.Attach(gameObject);
        }

        // ———— 运行时 ————

        private void Update()
        {
            UpdateAdaptiveResolution(Time.unscaledDeltaTime);

            if (_vrMode) UpdateVRCamera();
            else UpdateDesktopCamera();

            Head = MainCamera.transform;
        }

        /// 自适应动态分辨率：每 0.5 秒统计一次实际帧率，低于目标就把渲染缓冲按比例缩小，
        /// 高于目标再慢慢升回 1.0。摇头时 GPU 压力上来的那几秒会自动降档，稳住帧率。
        private void UpdateAdaptiveResolution(float dt)
        {
            if (!AdaptiveResolution) return;
            _resAccum += dt;
            _resFrames++;
            if (_resAccum < 0.5f) return;

            float fps = _resFrames / _resAccum;
            _resAccum = 0f;
            _resFrames = 0;

            float before = _resScale;
            if (fps < TargetFPS * 0.9f)
                _resScale = Mathf.Max(MinResScale, _resScale - 0.05f);   // 掉帧：降一档
            else if (fps > TargetFPS * 1.08f)
                _resScale = Mathf.Min(1f, _resScale + 0.05f);            // 富余：升一档

            if (!Mathf.Approximately(before, _resScale))
            {
                // 注意：Unity 2022.3 的 Built-in 管线里 ScalableBufferManager 没有 ResizeScale，
                // 所以这里用 XR 眼图缩放来做等效的动态分辨率（VR 下生效，桌面下该值恒为 1、无副作用），
                // 并与常量里的 VRRenderScale 基础值相乘。
                XRSettings.eyeTextureResolutionScale = Mathf.Clamp(VRRenderScale * _resScale, 0.5f, 1f);
                Debug.Log(string.Format("[PERF] 帧率 {0:F0} → 渲染缩放调整为 {1:0.00}",
                    fps, XRSettings.eyeTextureResolutionScale));
            }
        }

        private void UpdateDesktopCamera()
        {
            // 先摆放相机位置，再读输入：万一输入后端异常，相机也不会被留在原点（表现为"跑到地图底下"）。
            MainCamera.transform.localPosition = _camSeat;

            if (DesktopInput.GetMouseButton(1))
            {
                _desktopYaw += DesktopInput.GetAxis("Mouse X") * 3.2f;
                _desktopPitch -= DesktopInput.GetAxis("Mouse Y") * 3.2f;
                _desktopPitch = Mathf.Clamp(_desktopPitch, -70f, 70f);
            }
            if (DesktopInput.GetKeyDown(KeyCode.C) && !DesktopInput.GetMouseButton(1))
            {
                _desktopYaw = 0f;
                _desktopPitch = 0f;
            }
            // Unity 相机看向自身 +Z：yaw=0 面向 +Z，yaw=180 面向 -Z。
            // 这里直接让视线从「头枕端」指向「床尾端」，护士 / 手册 / 场景卡都在床尾那一侧。
            MainCamera.transform.localRotation = Quaternion.Euler(_desktopPitch, _camYawBase + _desktopYaw, 0f);
        }

        private void UpdateVRCamera()
        {
            var hmd = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
            Vector3 pos;
            Quaternion rot;
            if (hmd.isValid)
            {
                hmd.TryGetFeatureValue(CommonUsages.devicePosition, out pos);
                hmd.TryGetFeatureValue(CommonUsages.deviceRotation, out rot);
                MainCamera.transform.localPosition = pos;
                MainCamera.transform.localRotation = rot;
            }
            else
            {
                // rig 已承载头部座位与朝向，相机自身保持在 rig 原点即可
                MainCamera.transform.localPosition = Vector3.zero;
                MainCamera.transform.localRotation = Quaternion.identity;
            }
        }

        public void SetVRMode(bool vr)
        {
            _vrMode = vr;
            PrologueRun.VRMode = vr;
            if (vr)
            {
                MainCamera.transform.SetParent(_vrHead, false);
                // rig 原点放在头部座位上：右摇杆转身才是「原地转」，而不是绕世界原点公转
                _vrCameraOffset.localPosition = _camSeat;
                _vrCameraOffset.localRotation = Quaternion.Euler(0f, _camYawBase + _desktopYaw, 0f);
                _vrHead.localRotation = Quaternion.Euler(_desktopPitch, 0f, 0f);
                MainCamera.transform.localPosition = Vector3.zero;
                MainCamera.transform.localRotation = Quaternion.identity;
            }
            else
            {
                MainCamera.transform.SetParent(_vrCameraOffset, false);
                _vrCameraOffset.localPosition = Vector3.zero;
                _vrCameraOffset.localRotation = Quaternion.identity;
                _vrHead.localRotation = Quaternion.identity;
                MainCamera.transform.localPosition = Vector3.zero;
                MainCamera.transform.localRotation = Quaternion.identity;
            }
            Cursor.visible = !vr;
        }

        /// 更新手柄 Transform（VR 模式由设备位姿驱动，桌面模式跟随视角）
        public void UpdateHandTransforms(Transform left, Transform right)
        {
            if (_vrMode)
            {
                ApplyDevicePose(left, XRNode.LeftHand);
                ApplyDevicePose(right, XRNode.RightHand);
            }
            else
            {
                if (left != null)
                {
                    left.position = MainCamera.transform.position;
                    left.rotation = MainCamera.transform.rotation;
                }
                if (right != null)
                {
                    right.position = MainCamera.transform.position;
                    right.rotation = MainCamera.transform.rotation;
                }
            }
        }

        private void ApplyDevicePose(Transform t, XRNode node)
        {
            if (t == null) return;
            var device = InputDevices.GetDeviceAtXRNode(node);
            Vector3 pos;
            Quaternion rot;
            if (device.isValid &&
                device.TryGetFeatureValue(CommonUsages.devicePosition, out pos) &&
                device.TryGetFeatureValue(CommonUsages.deviceRotation, out rot))
            {
                t.localPosition = pos;
                t.localRotation = rot;
            }
        }

        // ———— 场景卡 ————

        public void ShowSceneCards(bool show)
        {
            if (SceneCards == null) return;
            for (int i = 0; i < SceneCards.Length; i++)
            {
                if (SceneCards[i] != null) SceneCards[i].SetVisible(show);
            }
        }

        public void SelectSceneCard(int index)
        {
            if (SceneCards == null) return;
            for (int i = 0; i < SceneCards.Length; i++)
            {
                if (SceneCards[i] != null) SceneCards[i].SetSelected(i == index);
            }
        }

        public int RoomIndex { get { return _roomIndex; } }

        /// 转场：切换房间色调（不移动玩家，避免眩晕）
        /// 序章（待产室）专属道具的名字白名单。它们建在 root 下、不属于房间模型，
        /// 转场时必须整组显隐，否则会和新房间重叠。
        private static readonly string[] ProloguePropNames = {
            // 注意：护士不在此列 —— 她是全程陪伴角色，两个房间都要出现
            "OperatingTable", "BedFrame", "Mattress", "HeadBoard", "FootBoard", "Pillow",
            "BedsideTable", "Handbook", "LightDot"
        };
        private readonly List<GameObject> _prologueProps = new List<GameObject>();

        private void CollectPrologueProps()
        {
            _prologueProps.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                var c = transform.GetChild(i).gameObject;
                if (System.Array.IndexOf(ProloguePropNames, c.name) >= 0)
                    _prologueProps.Add(c);
            }
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < _prologueProps.Count; i++)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(_prologueProps[i].name);
            }
            Debug.Log("[ROOM] 序章道具登记 " + _prologueProps.Count + " 个：" + sb);
        }

        public void ApplyRoom(int index)
        {
            _roomIndex = Mathf.Clamp(index, 0, 2);
            Color tint = new Color(0.42f, 0.45f, 0.50f);
            if (_roomIndex == 0) tint = new Color(0.42f, 0.45f, 0.50f);
            else if (_roomIndex == 1) tint = new Color(0.52f, 0.46f, 0.38f);
            else tint = new Color(0.50f, 0.40f, 0.44f);
            RenderSettings.ambientLight = tint;

            // 运动区切到活动室模型，其余房间回到待产室模型
            if (_roomMain != null) _roomMain.SetActive(_roomIndex != 1);
            if (_roomActivity != null) _roomActivity.SetActive(_roomIndex == 1);

            // 序章道具（手术床/护士/床头柜…）不属于房间模型，必须单独隐藏，
            // 否则切到运动区后它们会留在原地，和活动室里的球池等物件穿模。
            if (_prologueProps.Count == 0) CollectPrologueProps();
            for (int i = 0; i < _prologueProps.Count; i++)
            {
                if (_prologueProps[i] != null) _prologueProps[i].SetActive(_roomIndex != 1);
            }

            // 护士全程陪伴：切房间时换站位（避免和活动室里的球池穿模），并始终面朝玩家。
            // 地板基准统一用 _floorY（手术床腿底推算），两个房间的地面高度一致。
            if (Nurse != null)
            {
                Vector3 seat = (_roomIndex == 1)
                    ? new Vector3(-0.55f, _floorY + NurseGroundOffset, 1.25f)     // 运动区：站到侧前方
                    : new Vector3(-0.92f, _floorY + NurseGroundOffset, 1.05f);    // 待产室：床侧
                Nurse.PlaceAt(seat, new Vector3(0f, _floorY + 0.6f, 0f));
            }

            // 转场视点：去活动区就坐到右侧那把椅子上，回待产室还原床上的半卧位视点
            if (MoveCamToActivityChair && _roomActivity != null)
            {
                if (_roomIndex == 1)
                {
                    _camSeat = ActivityCamSeat;
                    _camYawBase = ActivityCamYaw;
                }
                else
                {
                    _camSeat = _prologueCamSeat;
                    _camYawBase = _prologueCamYaw;
                }
                ApplyCamSeat();
                Debug.Log(string.Format("[CAM] 转场到房间{0}：视点={1} yaw={2:F0}",
                    _roomIndex, _camSeat, _camYawBase));
            }

            if (SceneCards != null)
            {
                for (int i = 0; i < SceneCards.Length; i++)
                {
                    if (SceneCards[i] != null) SceneCards[i].SetSelected(i == _roomIndex);
                }
            }
        }

        /// 把 _camSeat/_camYawBase 应用到相机。
        /// 桌面模式下 Update 每帧都会写 MainCamera.localPosition，改完自动生效；
        /// VR 模式相机挂在 rig 上，必须这里同步挪动 rig，否则视点不会变。
        private void ApplyCamSeat()
        {
            if (_vrMode)
            {
                if (_vrCameraOffset != null)
                {
                    _vrCameraOffset.localPosition = _camSeat;
                    _vrCameraOffset.localRotation = Quaternion.Euler(0f, _camYawBase + _desktopYaw, 0f);
                }
                if (_vrHead != null)
                    _vrHead.localRotation = Quaternion.Euler(_desktopPitch, 0f, 0f);
            }
            else if (MainCamera != null)
            {
                MainCamera.transform.localPosition = _camSeat;
                MainCamera.transform.localRotation =
                    Quaternion.Euler(_desktopPitch, _camYawBase + _desktopYaw, 0f);
            }
        }
    }
}
