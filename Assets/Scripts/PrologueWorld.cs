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
        // —— 活动室（运动区）模型（Assets/Resources/Models/ActivityRoom）——
        private const string ActivityRoomModelPath = "Models/ActivityRoom";
        private const float ActivityRoomHeight = 3.0f;  // 目标层高（米）
        private static readonly Vector2 ActivityRoomCenterXZ = new Vector2(0f, 0.35f);

        // —— 应急开关：新加的模型/自动测量若导致画面异常，把对应项改 false 即可回到上一版的程序化效果 ——
        private const bool UseActivityRoom = true;      // false = 不使用活动室模型（转场只换色调）
        // 相机座位默认自动测量床模型得出。若自动结果位置不对，
        // 改成 true 并直接填写下面的 ManualCamSeat（x 左右, y 高度, z 前后）即可固定视点。
        private const bool UseManualCamSeat = false;
        private static readonly Vector3 ManualCamSeat = new Vector3(0f, 1.35f, -0.55f);
        // 自动测量结果的高度钳制范围（米），防止模型包围盒异常把相机算到地板下 / 天花板上
        private const float CamSeatMinY = 0.45f;
        private const float CamSeatMaxY = 2.20f;

        private GameObject _roomMain;       // 待产室（手术室 GLB）
        private GameObject _roomActivity;   // 运动区（活动室 GLB）

        private Transform _vrHead;
        private Transform _vrCameraOffset;
        private Transform _trackingSpace;
        private bool _vrMode;
        private float _desktopYaw;
        private float _desktopPitch;

        private int _roomIndex;
        private readonly List<Renderer> _tints = new List<Renderer>();
        // 相机座位（头枕上方，半卧位视点）与基础朝向（头枕在 -Z 端时为 180° 朝 +Z 看向护士；在 +Z 端时为 0°）
        private Vector3 _camSeat = new Vector3(0f, 0.99f, -1.0f);
        private float _camYawBase = 0f;

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
            // 性能模式（关 MSAA / 收紧远裁剪 / 降质量档）+ F3 诊断面板
            // PerformanceTuner.Attach(gameObject, MainCamera, uiCam);  // 已回退：改画质会动到全局设置，暂不启用
            // 世界相机不再渲染 HUD，避免重复绘制与深度遮挡
            MainCamera.cullingMask = ~(1 << 5);

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

        /// 运动区（活动室）模型：默认隐藏，选中「运动区」场景卡时显示
        private void BuildActivityRoom()
        {
            if (!UseActivityRoom) return;     // 应急开关关掉时完全不加载活动室模型
            var go = TryLoadModel(ActivityRoomModelPath, "ActivityRoom");
            if (go == null) return;
            // 生成模型通常被归一化到约 1 单位，按目标层高整体缩放并对齐地面/水平中心
            FitToHeight(go, ActivityRoomHeight, ActivityRoomCenterXZ);
            go.SetActive(false);
            _roomActivity = go;
        }

        /// 按目标高度等比缩放模型，把底面放到 y=0，并把水平中心对齐到 centerXZ
        private static void FitToHeight(GameObject go, float targetHeight, Vector2 centerXZ)
        {
            var b = CalcWorldBounds(go);
            float s = (b.size.y > 0.001f) ? (targetHeight / b.size.y) : 1f;
            go.transform.localScale = Vector3.Scale(go.transform.localScale, Vector3.one * s);

            b = CalcWorldBounds(go);
            go.transform.position += new Vector3(centerXZ.x - b.center.x, -b.min.y, centerXZ.y - b.center.z);
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
                // 只按手动常量摆放床，不做任何缩放/翻转/移位；仅据床模型算出相机座位
                bed.transform.localPosition = BedModelPos;
                bed.transform.localRotation = Quaternion.Euler(BedModelRot);
                bed.transform.localScale = BedModelScale;
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
            Nurse = NurseController.Create(transform, new Vector3(-0.92f, 0f, 1.05f));
            Nurse.FaceTo(new Vector3(0f, 0.6f, 0f));

            RoomTints = _tints.ToArray();
        }

        // ———— 运行时 ————

        private void Update()
        {
            if (_vrMode) UpdateVRCamera();
            else UpdateDesktopCamera();

            Head = MainCamera.transform;
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

            if (SceneCards != null)
            {
                for (int i = 0; i < SceneCards.Length; i++)
                {
                    if (SceneCards[i] != null) SceneCards[i].SetSelected(i == _roomIndex);
                }
            }
        }
    }
}
