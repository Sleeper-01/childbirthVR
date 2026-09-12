using UnityEngine;

namespace ChanFangVR
{
    /// 护士小安：程序化搭建的简易人形，用 Transform 补间做待机 / 招手 / 指向。
    /// 不依赖 AnimatorController（运行时不可用），也不需要任何外部模型。
    public class NurseController : MonoBehaviour
    {
        public enum Pose { Idle, Wave, Point }

        // —— 护士模型（Assets/Resources/Models/Nurse）——
        private const string ModelPath = "Models/Nurse";
        private const float TargetHeight = 1.62f;   // 目标身高（米）
        private const float ModelYaw = 0f;          // 正面修正：若背对玩家改 180
        // 应急开关：设为 false 则不加载 GLB 护士模型，回退到原来的程序化人形
        private const bool UseModel = true;

        private bool _hasModel;
        private Transform _modelRoot;
        private Quaternion _modelBaseRot;
        private Vector3 _modelBasePos;

        private Transform _body;
        private Transform _head;
        private Transform _armL;
        private Transform _armR;
        private Transform _handL;
        private Transform _handR;

        private Pose _pose = Pose.Idle;
        private float _poseTime;
        private float _waveDuration = 2.2f;
        private Transform _pointTarget;

        private Vector3 _bodyBasePos;
        private float _breath;

        public static NurseController Create(Transform parent, Vector3 localPos)
        {
            var go = new GameObject("Nurse");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var nurse = go.AddComponent<NurseController>();
            nurse.Build();
            return nurse;
        }

        private void Build()
        {
            // 优先用 GLB 护士模型替换程序化人形；模型缺失/未导入时回退到下方程序化搭建
            if (TryBuildFromModel()) return;

            var skin = new Color(0.96f, 0.83f, 0.73f);
            var uniform = new Color(0.58f, 0.74f, 0.87f);
            var hair = new Color(0.24f, 0.18f, 0.15f);
            var dark = new Color(0.18f, 0.18f, 0.20f);

            // 腿
            MakePart(transform, PrimitiveType.Capsule, "LegL",
                new Vector3(-0.09f, 0.40f, 0f), new Vector3(0.09f, 0.40f, 0.09f), uniform);
            MakePart(transform, PrimitiveType.Capsule, "LegR",
                new Vector3(0.09f, 0.40f, 0f), new Vector3(0.09f, 0.40f, 0.09f), uniform);

            // 躯干
            _body = MakePart(transform, PrimitiveType.Capsule, "Body",
                new Vector3(0f, 1.02f, 0f), new Vector3(0.20f, 0.28f, 0.14f), uniform);
            _bodyBasePos = _body.localPosition;

            // 领口
            MakePart(_body, PrimitiveType.Cube, "Collar",
                new Vector3(0f, 0.22f, -0.02f), new Vector3(0.17f, 0.05f, 0.16f),
                new Color(0.95f, 0.97f, 1.00f));

            // 头
            _head = MakePart(transform, PrimitiveType.Sphere, "Head",
                new Vector3(0f, 1.46f, 0f), Vector3.one * 0.21f, skin);

            // 头发（后半部分）
            MakePart(_head, PrimitiveType.Sphere, "Hair",
                new Vector3(0f, 0.03f, 0.02f), new Vector3(1.08f, 1.05f, 1.08f), hair);
            // 刘海
            MakePart(_head, PrimitiveType.Cube, "Bang",
                new Vector3(0f, 0.09f, -0.09f), new Vector3(0.22f, 0.07f, 0.10f), hair);

            // 眼睛（面朝 -Z 为正面，见 FaceTo 说明）
            MakePart(_head, PrimitiveType.Sphere, "EyeL",
                new Vector3(-0.06f, 0.01f, -0.19f), new Vector3(0.032f, 0.045f, 0.02f), dark);
            MakePart(_head, PrimitiveType.Sphere, "EyeR",
                new Vector3(0.06f, 0.01f, -0.19f), new Vector3(0.032f, 0.045f, 0.02f), dark);

            // 微笑
            MakePart(_head, PrimitiveType.Sphere, "Mouth",
                new Vector3(0f, -0.07f, -0.185f), new Vector3(0.075f, 0.022f, 0.02f),
                new Color(0.72f, 0.35f, 0.36f));

            // 手臂：以肩为轴，肢体沿 -Y 延伸
            _armL = BuildArm("ArmL", new Vector3(-0.21f, 1.24f, 0f), skin, uniform, out _handL);
            _armR = BuildArm("ArmR", new Vector3(0.21f, 1.24f, 0f), skin, uniform, out _handR);

            _armL.localRotation = Quaternion.Euler(0f, 0f, -8f);
            _armR.localRotation = Quaternion.Euler(0f, 0f, 8f);
        }

        /// 用 GLB 模型替代程序化人形：按目标身高归一化、贴地、水平居中。
        /// 返回 false 表示没有可用模型，调用方会回退到程序化搭建。
        private bool TryBuildFromModel()
        {
            if (!UseModel) return false;            // 应急开关：关掉即回退程序化人形
            var prefab = Resources.Load<GameObject>(ModelPath);
            if (prefab == null) return false;

            var go = Instantiate(prefab, transform, false);
            go.name = "NurseModel";

            var b = Encapsulate(go);
            if (b.size.y <= 0.001f) { Object.Destroy(go); return false; }

            // 按身高缩放，再贴地并水平居中到护士站位
            float s = TargetHeight / b.size.y;
            go.transform.localScale = Vector3.Scale(go.transform.localScale, Vector3.one * s);

            b = Encapsulate(go);
            go.transform.position += new Vector3(-b.center.x, -b.min.y, -b.center.z);
            go.transform.localRotation = Quaternion.Euler(0f, ModelYaw, 0f);

            _modelRoot = go.transform;
            _modelBaseRot = go.transform.localRotation;
            _modelBasePos = go.transform.localPosition;
            _hasModel = true;
            return true;
        }

        private static Bounds Encapsulate(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs == null || rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        private Transform BuildArm(string name, Vector3 shoulderPos, Color skin, Color sleeve, out Transform hand)
        {
            var shoulder = new GameObject(name);
            shoulder.transform.SetParent(transform, false);
            shoulder.transform.localPosition = shoulderPos;

            // 袖子（上段）
            MakePart(shoulder.transform, PrimitiveType.Capsule, "Sleeve",
                new Vector3(0f, -0.10f, 0f), new Vector3(0.075f, 0.12f, 0.075f), sleeve);
            // 小臂（肤色）
            MakePart(shoulder.transform, PrimitiveType.Capsule, "Forearm",
                new Vector3(0f, -0.32f, 0f), new Vector3(0.062f, 0.12f, 0.062f), skin);

            hand = MakePart(shoulder.transform, PrimitiveType.Sphere, "Hand",
                new Vector3(0f, -0.50f, 0f), Vector3.one * 0.085f, skin);
            return shoulder.transform;
        }

        private static Transform MakePart(Transform parent, PrimitiveType type, string name,
                                          Vector3 localPos, Vector3 localScale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = PrologueWorld.Mat(color, 0.25f, 0f);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        /// 模型模式：无骨骼动画，用整体呼吸起伏 + 招手摆动代替程序化手臂动作
        private void UpdateModelPose()
        {
            if (_modelRoot == null) return;

            float bob = Mathf.Sin(_breath * 1.6f) * 0.006f;
            Quaternion rot = _modelBaseRot;

            if (_pose == Pose.Wave)
            {
                float t = _poseTime;
                float k = Mathf.Clamp01(t / 0.35f) *
                          (1f - Mathf.Clamp01((t - (_waveDuration - 0.4f)) / 0.4f));
                rot = _modelBaseRot * Quaternion.Euler(0f, Mathf.Sin(t * 9f) * 10f * k, 0f);
                bob += Mathf.Sin(t * 9f) * 0.004f * k;
                if (t > _waveDuration) SetPose(Pose.Idle);
            }
            else if (_pose == Pose.Idle)
            {
                // 说话时轻微点头
                rot = _modelBaseRot * Quaternion.Euler(Mathf.Sin(_breath * 1.1f) * 2.5f, 0f, 0f);
            }

            _modelRoot.localPosition = _modelBasePos + new Vector3(0f, bob, 0f);
            _modelRoot.localRotation = Quaternion.Slerp(_modelRoot.localRotation, rot,
                1f - Mathf.Exp(-Time.unscaledDeltaTime * 9f));
        }

        /// 让护士面向某个世界坐标（只转 Y 轴）
        public void FaceTo(Vector3 worldTarget)
        {
            Vector3 dir = worldTarget - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.LookRotation(dir);
        }

        public void SetPose(Pose pose)
        {
            if (_pose == pose && pose != Pose.Wave) return;
            _pose = pose;
            _poseTime = 0f;
        }

        /// 招手（持续约 2.2 秒后自动回到待机）
        public void Wave()
        {
            _pose = Pose.Wave;
            _poseTime = 0f;
        }

        public void PointAt(Transform target)
        {
            _pointTarget = target;
            SetPose(Pose.Point);
        }

        public void StopPointing()
        {
            _pointTarget = null;
            SetPose(Pose.Idle);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _poseTime += dt;
            _breath += dt;

            if (_hasModel)
            {
                UpdateModelPose();
                return;
            }

            // 呼吸起伏
            if (_body != null)
            {
                float b = Mathf.Sin(_breath * 1.6f) * 0.006f;
                _body.localPosition = _bodyBasePos + new Vector3(0f, b, 0f);
            }

            Quaternion restL = Quaternion.Euler(0f, 0f, -8f);
            Quaternion restR = Quaternion.Euler(0f, 0f, 8f);
            Quaternion targetL = restL;
            Quaternion targetR = restR;

            switch (_pose)
            {
                case Pose.Wave:
                {
                    float t = _poseTime;
                    float raise = Mathf.Clamp01(t / 0.35f);
                    float lower = 1f - Mathf.Clamp01((t - (_waveDuration - 0.4f)) / 0.4f);
                    float k = raise * lower;
                    float swing = Mathf.Sin(t * 9f) * 22f;
                    targetR = Quaternion.Euler(0f, 0f, 8f + (127f + swing) * k);
                    if (_poseTime > _waveDuration) SetPose(Pose.Idle);
                    break;
                }
                case Pose.Point:
                {
                    if (_pointTarget != null && _armR != null)
                    {
                        Vector3 dir = _pointTarget.position - _armR.position;
                        if (dir.sqrMagnitude > 0.0001f)
                        {
                            targetR = Quaternion.FromToRotation(Vector3.down, dir.normalized);
                        }
                    }
                    break;
                }
            }

            float lerp = 1f - Mathf.Exp(-dt * 9f);
            if (_armL != null)
            {
                _armL.localRotation = _pose == Pose.Point
                    ? Quaternion.Slerp(_armL.localRotation, restL, lerp)
                    : Quaternion.Slerp(_armL.localRotation, targetL, lerp);
            }
            if (_armR != null)
            {
                _armR.localRotation = Quaternion.Slerp(_armR.localRotation, targetR, lerp);
            }

            // 说话时轻微点头（待机/指向时）
            if (_head != null && (_pose == Pose.Idle || _pose == Pose.Point))
            {
                float nod = Mathf.Sin(_breath * 1.1f) * 2.5f;
                _head.localRotation = Quaternion.Euler(nod, 0f, 0f);
            }
        }
    }
}
