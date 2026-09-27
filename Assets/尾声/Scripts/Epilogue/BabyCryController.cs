using System.Collections;
using UnityEngine;

namespace ChuJianXinSheng.Epilogue
{
    /// <summary>
    /// 襁褓婴儿哭闹表现。婴儿2.fbx 为无骨骼、无 BlendShape 的静态网格模型，
    /// 故采用程序化方案：
    ///   紧闭双眼 = 眼球/睫毛网格沿"竖直"维压扁成缝；
    ///   张嘴大哭 = 嘴部网格放大；
    ///   哭闹颤动 = 婴儿整体小幅 Perlin 抖动；
    ///   配合啼哭音效循环（前段响亮，后段转为抽泣式低音量）。
    ///
    /// 实现要点：
    /// 1. 各部件网格中心偏离部件轴心约 0.39~0.42（共享轴心），直接改 localScale
    ///    会把网格"甩飞"，因此缩放时按 meshCenter 做位置补偿，保证网格原位变形；
    /// 2. wiggleRoot 的静止 localPos/localRot 在 Awake 捕获（含场景搭建时的重心
    ///    校正偏移），停止哭闹时恢复基准位而非清零，避免婴儿瞬移。
    /// </summary>
    public class BabyCryController : MonoBehaviour
    {
        private struct PartAnim
        {
            public Transform t;
            public Vector3 baseScale;
            public Vector3 basePos;
            public Quaternion baseRot;
            public Vector3 meshCenter;
        }

        [Header("部件引用（婴儿2.fbx 静态网格，场景搭建时接线）")]
        [SerializeField] private Transform eyesPart;      // Object_128 眼球
        [SerializeField] private Transform eyelashPart;   // Object_143 睫毛
        [SerializeField] private Transform mouthPart;     // Object_137 嘴
        [SerializeField] private Transform wiggleRoot;    // 婴儿颤动根节点

        [Header("音效")]
        [SerializeField] private AudioSource cryAudio;
        [SerializeField] private float loudSeconds = 4f;
        [SerializeField, Range(0f, 1f)] private float settleVolume = 0.35f;

        [Header("闭眼 / 张嘴幅度（相对网格竖直维的倍率）")]
        [SerializeField] private float eyesClosedScaleY = 0.12f;
        [SerializeField] private float eyelashSquintY = 0.4f;
        [SerializeField] private float mouthOpenScaleY = 2.6f;
        [SerializeField] private float tweenDuration = 0.12f;

        [Header("颤动幅度")]
        [SerializeField] private float positionJitter = 0.006f;
        [SerializeField] private float rotationDegrees = 2.2f;

        private PartAnim eyesAnim;
        private PartAnim lashAnim;
        private PartAnim mouthAnim;
        private float eyesK = 1f;
        private float lashK = 1f;
        private float mouthK = 1f;

        private Vector3 wiggleBasePos;
        private Quaternion wiggleBaseRot;
        private bool crying;
        private float wiggleBoost;
        private Coroutine wiggleRoutine;
        private Coroutine eyesTween;
        private Coroutine lashTween;
        private Coroutine mouthTween;

        public bool IsCrying { get { return crying; } }
        public bool EyesClosed
        {
            get { return eyesPart != null && eyesPart.localScale.y < eyesAnim.baseScale.y * 0.5f; }
        }
        public bool MouthOpen
        {
            get { return mouthPart != null && mouthPart.localScale.y > mouthAnim.baseScale.y * 1.5f; }
        }
        public bool CryAudioPlaying { get { return cryAudio != null && cryAudio.isPlaying; } }

        private void Awake()
        {
            eyesAnim = CapturePart(eyesPart);
            lashAnim = CapturePart(eyelashPart);
            mouthAnim = CapturePart(mouthPart);
            if (wiggleRoot != null)
            {
                wiggleBasePos = wiggleRoot.localPosition;
                wiggleBaseRot = wiggleRoot.localRotation;
            }
        }

        private static PartAnim CapturePart(Transform t)
        {
            var p = new PartAnim();
            p.t = t;
            if (t == null) return p;
            p.baseScale = t.localScale;
            p.basePos = t.localPosition;
            p.baseRot = t.localRotation;
            var mf = t.GetComponent<MeshFilter>();
            p.meshCenter = mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds.center : Vector3.zero;
            return p;
        }

        /// <summary>按倍率 k 缩放部件网格，并补偿轴心偏移使网格原位变形。</summary>
        private void ApplyPartScale(PartAnim p, float k)
        {
            if (p.t == null) return;
            var s = new Vector3(p.baseScale.x, p.baseScale.y * k, p.baseScale.z);
            p.t.localScale = s;
            var delta = Vector3.Scale(p.baseScale - s, p.meshCenter);
            p.t.localPosition = p.basePos + p.baseRot * delta;
        }

        /// <summary>开始大哭：紧闭双眼、张嘴、颤动、啼哭音效循环。</summary>
        public void StartCry()
        {
            if (crying) return;
            crying = true;
            wiggleBoost = 1f;

            StopPartTweens();
            eyesTween = StartCoroutine(TweenPart(eyesAnim, 0, eyesK, eyesClosedScaleY, tweenDuration));
            lashTween = StartCoroutine(TweenPart(lashAnim, 1, lashK, eyelashSquintY, tweenDuration));
            mouthTween = StartCoroutine(TweenPart(mouthAnim, 2, mouthK, mouthOpenScaleY, tweenDuration));

            if (cryAudio != null && cryAudio.clip != null)
            {
                cryAudio.loop = true;
                cryAudio.volume = 1f;
                cryAudio.Play();
            }

            if (wiggleRoutine != null) StopCoroutine(wiggleRoutine);
            wiggleRoutine = StartCoroutine(Wiggle());
        }

        /// <summary>停止哭闹并复位（演示重置用）：直接回到捕获的基准姿态。</summary>
        public void StopCry()
        {
            crying = false;
            if (wiggleRoutine != null)
            {
                StopCoroutine(wiggleRoutine);
                wiggleRoutine = null;
            }
            StopPartTweens();
            eyesK = 1f; lashK = 1f; mouthK = 1f;
            ApplyPartScale(eyesAnim, 1f);
            ApplyPartScale(lashAnim, 1f);
            ApplyPartScale(mouthAnim, 1f);

            if (cryAudio != null && cryAudio.isPlaying) cryAudio.Stop();

            if (wiggleRoot != null)
            {
                wiggleRoot.localPosition = wiggleBasePos;
                wiggleRoot.localRotation = wiggleBaseRot;
            }
        }

        /// <summary>仅静音啼哭音效（视觉哭闹保持）：播放教学动画时避免哭声与视频音频混杂。</summary>
        public void StopCrySound()
        {
            if (cryAudio != null && cryAudio.isPlaying) cryAudio.Stop();
        }

        /// <summary>哭闹过程中的每次轻拍：短暂加剧颤动（表现为安抚互动的即时反馈）。</summary>
        public void Pat()
        {
            if (crying) wiggleBoost = Mathf.Max(wiggleBoost, 0.8f);
        }

        private void StopPartTweens()
        {
            if (eyesTween != null) { StopCoroutine(eyesTween); eyesTween = null; }
            if (lashTween != null) { StopCoroutine(lashTween); lashTween = null; }
            if (mouthTween != null) { StopCoroutine(mouthTween); mouthTween = null; }
        }

        private IEnumerator Wiggle()
        {
            float t = 0f;
            while (crying)
            {
                t += Time.deltaTime;
                // 大哭阶段剧烈小幅颤动，随后减弱为抽泣式微颤
                float intensity = t < loudSeconds ? 1f : 0.35f;
                intensity = Mathf.Max(intensity, wiggleBoost);
                wiggleBoost = Mathf.MoveTowards(wiggleBoost, 0f, Time.deltaTime * 1.6f);

                float nz = (Mathf.PerlinNoise(t * 14f, 0.13f) - 0.5f) * 2f;
                float nx = (Mathf.PerlinNoise(0.27f, t * 11f) - 0.5f) * 2f;

                if (wiggleRoot != null)
                {
                    wiggleRoot.localPosition = wiggleBasePos + new Vector3(nx * positionJitter, 0f, nz * positionJitter * 1.3f) * intensity;
                    wiggleRoot.localRotation = wiggleBaseRot * Quaternion.Euler(nx * rotationDegrees * intensity, 0f, nz * rotationDegrees * 1.3f * intensity);
                }

                if (cryAudio != null && cryAudio.isPlaying && t > loudSeconds)
                    cryAudio.volume = Mathf.MoveTowards(cryAudio.volume, settleVolume, Time.deltaTime * 0.6f);

                yield return null;
            }

            if (wiggleRoot != null)
            {
                wiggleRoot.localPosition = wiggleBasePos;
                wiggleRoot.localRotation = wiggleBaseRot;
            }
        }

        /// <summary>部件倍率缓动（index: 0=眼 1=睫毛 2=嘴，读写对应成员倍率字段）。</summary>
        private IEnumerator TweenPart(PartAnim p, int index, float from, float to, float duration)
        {
            float t = 0f;
            float safeDuration = Mathf.Max(0.01f, duration);
            while (t < safeDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(from, to, Mathf.Clamp01(t / safeDuration));
                ApplyK(index, k, p);
                yield return null;
            }
            ApplyK(index, to, p);
        }

        private void ApplyK(int index, float k, PartAnim p)
        {
            switch (index)
            {
                case 0: eyesK = k; ApplyPartScale(eyesAnim, k); break;
                case 1: lashK = k; ApplyPartScale(lashAnim, k); break;
                default: mouthK = k; ApplyPartScale(mouthAnim, k); break;
            }
        }
    }
}
