using System.Collections;
using UnityEngine;

namespace ChuJianXinSheng.Epilogue
{
    /// <summary>
    /// 鼠标驱动双手输入（手柄阶段前的占位实现，接入真实 XR 后由 XR Rig 替换同一 IHandsRig 接口）。
    /// 操作约定：
    ///   长按鼠标左键：双手自动移至胸前并拢并扣握，按住期间累积环抱进度（约3秒完成「环抱」）
    ///   环抱完成后：双手保持胸前环抱位；单击鼠标左键 = 扳机轻点（轻拍宝宝）
    /// 另提供自动化 API，供运行时验证脚本驱动完整流程。
    /// </summary>
    public class MouseHandsRig : MonoBehaviour, IHandsRig
    {
        [SerializeField] private SimulatedHand leftHand;
        [SerializeField] private SimulatedHand rightHand;
        [Tooltip("双手移向胸前/体侧的动画时长")]
        [SerializeField] private float handMoveSeconds = 0.35f;
        [Tooltip("环抱目标位（rig 局部坐标）")]
        [SerializeField] private Vector3 leftChestLocal = new Vector3(-0.08f, 0.6f, 0.5f);
        [SerializeField] private Vector3 rightChestLocal = new Vector3(0.08f, 0.6f, 0.5f);
        [Tooltip("按下时长低于该值视为单击轻点（未环抱时）")]
        [SerializeField] private float tapMaxSeconds = 0.3f;

        public IHandInput Left { get { return leftHand; } }
        public IHandInput Right { get { return rightHand; } }
        public Vector3 LeftChestLocal { get { return leftChestLocal; } }
        public Vector3 RightChestLocal { get { return rightChestLocal; } }

        private Vector3 leftRestLocal;
        private Vector3 rightRestLocal;
        private Coroutine moveRoutine;
        private bool lockedAtChest;
        private bool pressing;
        private bool suppressTapOnRelease;
        private float pressTimer;

        private void Awake()
        {
            leftRestLocal = leftHand.transform.localPosition;
            rightRestLocal = rightHand.transform.localPosition;
        }

        private bool prevGrabbing = false;

        /// <summary>统一"抓住"状态：鼠标左键 或 VR扳机 按住均算</summary>
        private bool IsGrabbing()
        {
            return Input.GetMouseButton(0) || ChuJian.Merge.VRPointerVisual.IsAnyTriggerHeld;
        }

        private void Update()
        {
            bool grabbing = IsGrabbing();
            bool grabDown = grabbing && !prevGrabbing;
            bool grabUp = !grabbing && prevGrabbing;
            prevGrabbing = grabbing;

            if (grabDown)
            {
                pressing = true;
                pressTimer = 0f;
                suppressTapOnRelease = false;
                SetGripBoth(true);
                if (lockedAtChest) rightHand.SetTriggerTap();  // 环抱后：按下即轻点
                else MoveHands(LeftChestLocal, RightChestLocal, handMoveSeconds);
            }

            if (!pressing) return;
            pressTimer += Time.unscaledDeltaTime;

            if (grabUp)
            {
                pressing = false;
                SetGripBoth(false);
                if (!lockedAtChest && !suppressTapOnRelease)
                {
                    if (pressTimer <= tapMaxSeconds) rightHand.SetTriggerTap(); // 短按 = 单击轻点
                    MoveHands(leftRestLocal, rightRestLocal, handMoveSeconds);   // 未环抱：松手双手回体侧
                }
                suppressTapOnRelease = false;
            }
        }

        public void LockHandsAtChest(bool locked)
        {
            lockedAtChest = locked;
            if (locked && pressing) suppressTapOnRelease = true; // 本次长按用于完成环抱，松开不误触发轻拍
        }

        public void ResetHands()
        {
            lockedAtChest = false;
            pressing = false;
            suppressTapOnRelease = false;
            SetGripBoth(false);
            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
                moveRoutine = null;
            }
            leftHand.transform.localPosition = leftRestLocal;
            rightHand.transform.localPosition = rightRestLocal;
        }

        // ---------- 自动化 API（运行时验证 / 演示驱动用） ----------

        public void SetGripBoth(bool held)
        {
            leftHand.SetGrip(held);
            rightHand.SetGrip(held);
        }

        /// <summary>扳机轻点（默认右手，与鼠标单击行为一致）。</summary>
        public void TapTrigger()
        {
            rightHand.SetTriggerTap();
        }

        /// <summary>一次性将双手平滑移动到 rig 局部坐标指定位置。</summary>
        public void MoveHands(Vector3 leftLocal, Vector3 rightLocal, float duration)
        {
            if (moveRoutine != null) StopCoroutine(moveRoutine);
            moveRoutine = StartCoroutine(MoveTween(leftLocal, rightLocal, duration));
        }

        /// <summary>自动化流程：双手移至胸前并拢 → 扣住双侧握键指定秒数（环抱成功后由 Director 锁定双手位）。</summary>
        public IEnumerator EmbraceRoutine(float holdSeconds)
        {
            yield return MoveTween(LeftChestLocal, RightChestLocal, handMoveSeconds);
            SetGripBoth(true);
            float t = 0f;
            while (t < holdSeconds)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator MoveTween(Vector3 leftLocal, Vector3 rightLocal, float duration)
        {
            Vector3 l0 = leftHand.transform.localPosition;
            Vector3 r0 = rightHand.transform.localPosition;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
                leftHand.transform.localPosition = Vector3.Lerp(l0, leftLocal, k);
                rightHand.transform.localPosition = Vector3.Lerp(r0, rightLocal, k);
                yield return null;
            }
            leftHand.transform.localPosition = leftLocal;
            rightHand.transform.localPosition = rightLocal;
        }
    }
}
