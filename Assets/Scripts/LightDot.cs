using UnityEngine;

namespace ChanFangVR
{
    /// 步骤1：床尾校准光点。射线对准 + 扣扳机完成校准。
    public class LightDot : PrologueInteractable
    {
        private GameObject _core;
        private GameObject _halo;
        private GameObject _ring;
        private Material _coreMat;
        private Material _haloMat;
        private Material _ringMat;
        private bool _calibrated;

        public static LightDot Create(Transform parent, Vector3 localPos)
        {
            var root = new GameObject("LightDot");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;

            // 光核
            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "Core";
            Object.Destroy(core.GetComponent<Collider>());
            core.transform.SetParent(root.transform, false);
            core.transform.localScale = Vector3.one * 0.07f;
            var coreMat = PrologueWorld.Mat(new Color(1f, 1f, 0.95f), 0f, 0f);
            core.GetComponent<Renderer>().sharedMaterial = coreMat;

            // 光晕贴片
            var halo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            halo.name = "Halo";
            Object.Destroy(halo.GetComponent<Collider>());
            halo.transform.SetParent(root.transform, false);
            halo.transform.localScale = Vector3.one * 0.42f;
            var haloMat = new Material(Shader.Find("Sprites/Default"));
            haloMat.mainTexture = PrologueWorld.GlowTex;
            haloMat.color = new Color(1f, 1f, 0.9f, 0.55f);
            halo.GetComponent<Renderer>().sharedMaterial = haloMat;
            halo.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // 提示环
            var ring = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ring.name = "Ring";
            Object.Destroy(ring.GetComponent<Collider>());
            ring.transform.SetParent(root.transform, false);
            ring.transform.localScale = Vector3.one * 0.2f;
            var ringMat = new Material(Shader.Find("Sprites/Default"));
            ringMat.mainTexture = PrologueWorld.RingTex;
            ringMat.color = PrologueDefs.Accent;
            ring.GetComponent<Renderer>().sharedMaterial = ringMat;
            ring.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // 命中碰撞体（非 trigger，保证 Physics.Raycast 一定命中）
            var col = root.AddComponent<SphereCollider>();
            col.radius = 0.18f;
            col.isTrigger = false;

            var dot = root.AddComponent<LightDot>();
            dot._core = core;
            dot._halo = halo;
            dot._ring = ring;
            dot._coreMat = coreMat;
            dot._haloMat = haloMat;
            dot._ringMat = ringMat;
            dot.Label = "校准光点";
            return dot;
        }

        /// 校准完成后光点转为绿色常亮，并退出可交互
        public void SetCalibrated(bool on)
        {
            _calibrated = on;
            IsTarget = false;
            Interactive = !on;
            ApplyVisual();
        }

        protected override void ApplyVisual()
        {
            if (_ringMat == null) return;
            Color c;
            if (_calibrated) c = PrologueDefs.Success;
            else if (_hover) c = Color.white;
            else c = PrologueDefs.Accent;
            c.a = (_hover || _calibrated) ? 1f : 0.8f;
            _ringMat.color = c;
            if (_ring != null)
            {
                float s = _hover ? 0.26f : 0.2f;
                _ring.transform.localScale = Vector3.one * s;
            }
        }

        protected override void ApplyPulse()
        {
            float k = Pulse01(4f);
            if (_haloMat != null) _haloMat.color = new Color(1f, 1f, 0.9f, 0.35f + k * 0.4f);
            if (_halo != null) _halo.transform.localScale = Vector3.one * (0.4f + k * 0.12f);
        }

        protected override void Update()
        {
            base.Update();
            if (_halo != null) FaceCamera(_halo.transform);
            if (_ring != null) FaceCamera(_ring.transform);
        }
    }
}
