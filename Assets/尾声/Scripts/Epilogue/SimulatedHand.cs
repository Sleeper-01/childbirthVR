using System.Collections;
using UnityEngine;

namespace ChuJianXinSheng.Epilogue
{
    /// <summary>
    /// 键盘模拟的虚拟手（XR 手柄占位视觉），实现 IHandInput。
    /// 输入状态由 KeyboardHandsRig 或自动化脚本写入。
    /// </summary>
    public class SimulatedHand : MonoBehaviour, IHandInput
    {
        [SerializeField] private HandSide side;
        [Tooltip("手部视觉子物体，震动脉冲与轻拍下沉动画只作用于它，不影响逻辑位置")]
        [SerializeField] private Transform visual;

        private bool gripHeld;
        private bool tapPending;
        private Vector3 visualBaseScale = Vector3.one;
        private Vector3 visualRestPos;
        private Coroutine dipRoutine;

        public HandSide Side { get { return side; } }
        public Vector3 Position { get { return transform.position; } }
        public Quaternion Rotation { get { return transform.rotation; } }
        public bool GripHeld { get { return gripHeld; } }

        public bool TriggerTapped
        {
            get
            {
                bool t = tapPending;
                tapPending = false;
                return t;
            }
        }

        private void Awake()
        {
            if (visual != null)
            {
                visualBaseScale = visual.localScale;
                visualRestPos = visual.localPosition;
            }
        }

        public void SetGrip(bool held)
        {
            gripHeld = held;
        }

        /// <summary>记录一次扳机轻点（本帧或下一帧被交互状态机消费）。</summary>
        public void SetTriggerTap()
        {
            tapPending = true;
        }

        public void HapticPulse(float amplitude, float duration)
        {
            StartCoroutine(PulseVisual(amplitude, duration));
        }

        /// <summary>轻拍时手部视觉沿指定世界方向轻点再回弹。</summary>
        public void PlayPatDip(Vector3 worldDirection)
        {
            if (visual == null) return;
            if (dipRoutine != null) StopCoroutine(dipRoutine);
            dipRoutine = StartCoroutine(PatDip(worldDirection));
        }

        private IEnumerator PulseVisual(float amplitude, float duration)
        {
            if (visual == null) yield break;
            float t = 0f;
            float safeDuration = Mathf.Max(0.02f, duration);
            while (t < safeDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - Mathf.Clamp01(t / safeDuration); // 1→0 衰减脉冲
                visual.localScale = visualBaseScale * (1f + 0.16f * amplitude * k);
                yield return null;
            }
            visual.localScale = visualBaseScale;
        }

        private IEnumerator PatDip(Vector3 worldDir)
        {
            Transform parent = visual.parent != null ? visual.parent : transform;
            Vector3 localDir = parent.InverseTransformDirection(worldDir.normalized);
            const float dipDepth = 0.055f;
            const float totalSeconds = 0.26f;
            float t = 0f;
            while (t < totalSeconds)
            {
                t += Time.deltaTime;
                float phase = Mathf.Clamp01(t / totalSeconds);
                float offset = Mathf.Sin(Mathf.PI * phase) * dipDepth; // 0 → 峰值 → 0
                visual.localPosition = visualRestPos + localDir * offset;
                yield return null;
            }
            visual.localPosition = visualRestPos;
        }
    }
}
