using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChuJianXinSheng.Epilogue
{
    public enum EpiloguePhase
    {
        PromptEmbrace = 0,      // 等待环抱：双手移至胸前并拢
        EmbracingHold = 1,      // 扣握保持中（0-3秒进度）
        EmbracedPromptPat = 2,  // 已环抱，等待轻拍
        BabyCrying = 3,         // 宝宝紧闭双眼、大哭中
        AchievementShown = 4    // 成就"初见·新生"已解锁
    }

    /// <summary>
    /// 3.6 尾声交互总控（策划 3.6 节：双手柄移至胸前并拢、扣住侧握键保持约3秒完成"环抱"；
    /// 轻点扳机轻拍宝宝，触发宝宝紧闭双眼、开始大哭；解锁本作同名成就徽章"初见·新生"）。
    /// 当前输入为鼠标占位实现（长按3秒环抱/单击轻拍），只依赖 IHandsRig 抽象，
    /// 接入真实 XR 后整体替换输入实现即可。
    /// </summary>
    public class EpilogueDirector : MonoBehaviour
    {
        [Header("输入设备（实现 IHandsRig 的组件，接入 XR 后替换为 XR Rig）")]
        [SerializeField] private MonoBehaviour handsRigComponent;

        [Header("交互锚点与判定")]
        [SerializeField] private Transform chestAnchor;      // 胸前环抱中心
        [SerializeField] private Transform babyAnchor;        // 婴儿位置（轻拍可达判定）
        [SerializeField] private float embraceZoneRadius = 0.24f;
        [SerializeField] private float handsTogetherDistance = 0.34f;
        [SerializeField] private float embraceHoldSeconds = 3f;
        [SerializeField] private float patReachDistance = 0.42f;
        [SerializeField] private float achievementDelaySeconds = 3f;

        [Header("表现组件")]
        [SerializeField] private BabyCryController baby;
        [SerializeField] private AchievementPopup achievementPopup;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private Image progressRing;
        [SerializeField] private Color ringIdleColor = new Color(1f, 0.78f, 0.5f, 0.5f);
        [SerializeField] private Color ringHoldingColor = new Color(1f, 0.6f, 0.25f, 0.95f);
        [SerializeField] private Sprite badgeSprite;
        [SerializeField] private TeachingVideoPrompt teachingVideoPrompt;
        [SerializeField] private float teachingPromptDelaySeconds = 1.5f;
        [SerializeField] private SummaryPanel summaryPanel;
        [SerializeField] private PetalFallEffect petalEffect;

        public EpiloguePhase Phase { get; private set; }
        public float EmbraceProgress { get; private set; }
        public bool IsBabyCrying { get { return baby != null && baby.IsCrying; } }
        public bool IsAchievementShown { get { return Phase == EpiloguePhase.AchievementShown; } }

        public event System.Action OnEmbraceCompleted;
        public event System.Action OnBabyCried;
        public event System.Action OnAchievementUnlocked;

        private IHandsRig rig;
        private IHandInput left;
        private IHandInput right;
        private float holdPulseTimer;
        private bool subtitleIsHold;
        private bool achievementParkedForTeaching;

        private const string PromptEmbraceText =
            "护士将裹好的新生儿轻放在您的胸前\n长按鼠标左键约3秒，完成「环抱」";
        private const string PromptHoldText = "很好，保持住……";
        private const string PromptPatText = "已环抱宝宝\n单击鼠标左键，轻拍宝宝";
        private const string PromptCryingText = "宝宝紧闭双眼，放声大哭——欢迎来到这个世界";
        private const string PromptDoneText = "成就「初见·新生」已解锁\n（按 R 键重玩演示）";

        private void Awake()
        {
            rig = handsRigComponent as IHandsRig;
            if (rig == null)
            {
                foreach (var mb in GetComponentsInChildren<MonoBehaviour>())
                {
                    var r = mb as IHandsRig;
                    if (r != null) { rig = r; break; }
                }
            }
            if (rig != null)
            {
                left = rig.Left;
                right = rig.Right;
            }
            if (teachingVideoPrompt != null)
            {
                teachingVideoPrompt.OnClosed += HandleTeachingClosed;
                teachingVideoPrompt.OnPlayVideo += HandlePlayVideo;
            }
            if (summaryPanel != null)
                summaryPanel.OnClosed += HandleSummaryClosed;
        }

        private void Start()
        {
            SetSubtitle(PromptEmbraceText);
            UpdateRingVisual();
            if (achievementPopup != null) achievementPopup.Hide();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetDemo();
                return;
            }

            // 每帧消费扳机轻点事件，避免一次点击跨阶段残留
            bool leftTap = left != null && left.TriggerTapped;
            bool rightTap = right != null && right.TriggerTapped;

            switch (Phase)
            {
                case EpiloguePhase.PromptEmbrace:
                case EpiloguePhase.EmbracingHold:
                    UpdateEmbrace();
                    break;

                case EpiloguePhase.EmbracedPromptPat:
                    if (leftTap && HandNearBaby(left)) DoPat(left, firstPat: true);
                    else if (rightTap && HandNearBaby(right)) DoPat(right, firstPat: true);
                    break;

                case EpiloguePhase.BabyCrying:
                    // 大哭中继续允许轻拍安抚：宝宝颤动即时响应
                    if (leftTap && HandNearBaby(left)) DoPat(left, firstPat: false);
                    else if (rightTap && HandNearBaby(right)) DoPat(right, firstPat: false);
                    break;

                case EpiloguePhase.AchievementShown:
                    break;
            }
        }

        private void UpdateEmbrace()
        {
            if (left == null || right == null) return;

            bool inZone = HandInZone(left) && HandInZone(right);
            bool together = Vector3.Distance(left.Position, right.Position) <= handsTogetherDistance;
            bool gripping = left.GripHeld && right.GripHeld;

            if (inZone && together && gripping)
            {
                Phase = EpiloguePhase.EmbracingHold;
                EmbraceProgress = Mathf.Min(1f, EmbraceProgress + Time.deltaTime / embraceHoldSeconds);

                // 保持期间手柄节律性震动反馈（对应文档"用力保持反馈"的手柄震动约定）
                holdPulseTimer += Time.deltaTime;
                if (holdPulseTimer >= 0.6f)
                {
                    holdPulseTimer = 0f;
                    left.HapticPulse(0.35f, 0.08f);
                    right.HapticPulse(0.35f, 0.08f);
                }

                if (!subtitleIsHold)
                {
                    SetSubtitle(PromptHoldText);
                    subtitleIsHold = true;
                }

                if (EmbraceProgress >= 1f) CompleteEmbrace();
            }
            else
            {
                // 脱离区域或松开握键：进度快速回落，回零后退回提示阶段
                EmbraceProgress = Mathf.Max(0f, EmbraceProgress - Time.deltaTime * 2.5f);
                holdPulseTimer = 0f;
                if (Phase == EpiloguePhase.EmbracingHold && EmbraceProgress <= 0f)
                {
                    Phase = EpiloguePhase.PromptEmbrace;
                    subtitleIsHold = false;
                    SetSubtitle(PromptEmbraceText);
                }
            }

            UpdateRingVisual();
        }

        private void CompleteEmbrace()
        {
            Phase = EpiloguePhase.EmbracedPromptPat;
            SetSubtitle(PromptPatText);
            if (rig != null) rig.LockHandsAtChest(true);
            if (left != null) left.HapticPulse(0.8f, 0.3f);
            if (right != null) right.HapticPulse(0.8f, 0.3f);
            StartCoroutine(FadeRingOut());
            OnEmbraceCompleted?.Invoke();
            Debug.Log("[Epilogue] 环抱完成（双手并拢扣握3秒）");
        }

        private void DoPat(IHandInput hand, bool firstPat)
        {
            var simHand = hand as SimulatedHand;
            if (simHand != null && babyAnchor != null)
                simHand.PlayPatDip(babyAnchor.position - hand.Position);
            hand.HapticPulse(0.5f, 0.12f);

            if (!firstPat)
            {
                if (baby != null) baby.Pat();
                return;
            }

            // 首次轻拍：触发宝宝紧闭双眼、开始大哭，随后解锁成就
            Phase = EpiloguePhase.BabyCrying;
            SetSubtitle(PromptCryingText);
            if (baby != null) baby.StartCry();
            OnBabyCried?.Invoke();
            Debug.Log("[Epilogue] 轻拍宝宝：紧闭双眼，开始大哭");
            StartCoroutine(ShowAchievementAfter(achievementDelaySeconds));
        }

        private IEnumerator ShowAchievementAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (achievementPopup != null) achievementPopup.Show(badgeSprite);
            Phase = EpiloguePhase.AchievementShown;
            SetSubtitle(PromptDoneText);
            OnAchievementUnlocked?.Invoke();
            Debug.Log("[Epilogue] 成就「初见·新生」已解锁");

            // 成就展示后延迟弹出"教学动画"选择面板（字幕让位，关闭后恢复）
            if (teachingVideoPrompt != null)
                StartCoroutine(ShowTeachingPromptAfter(teachingPromptDelaySeconds));
        }

        private IEnumerator ShowTeachingPromptAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (Phase == EpiloguePhase.AchievementShown && teachingVideoPrompt != null)
            {
                if (subtitleText != null)
                    subtitleText.transform.parent.gameObject.SetActive(false);
                if (achievementPopup != null) achievementPopup.Hide();
                achievementParkedForTeaching = true;
                teachingVideoPrompt.Show();
                Debug.Log("[Epilogue] 教学动画选择面板已弹出");
            }
        }

        /// <summary>教学选择面板关闭后：若成就此前被教学面板接管，静默找回成就面板并恢复字幕。</summary>
        private void HandleTeachingClosed()
        {
            if (Phase != EpiloguePhase.AchievementShown || !achievementParkedForTeaching) return;
            achievementParkedForTeaching = false;
            if (achievementPopup != null) achievementPopup.Restore();
            if (subtitleText != null)
            {
                subtitleText.transform.parent.gameObject.SetActive(true);
                SetSubtitle(PromptDoneText);
            }
        }

        /// <summary>用户选择观看教学动画：静音婴儿啼哭（视觉哭闹保持），避免哭声与视频音频混杂。</summary>
        private void HandlePlayVideo()
        {
            if (baby != null) baby.StopCrySound();
        }

        /// <summary>总结画面关闭：播放鲜花飘落特效，并找回被教学面板接管的成就面板与结束字幕。</summary>
        private void HandleSummaryClosed()
        {
            if (petalEffect != null) petalEffect.Play();
            if (Phase != EpiloguePhase.AchievementShown || !achievementParkedForTeaching) return;
            achievementParkedForTeaching = false;
            if (achievementPopup != null) achievementPopup.Restore();
            if (subtitleText != null)
            {
                subtitleText.transform.parent.gameObject.SetActive(true);
                SetSubtitle(PromptDoneText);
            }
        }

        private bool HandInZone(IHandInput hand)
        {
            return chestAnchor != null && Vector3.Distance(hand.Position, chestAnchor.position) <= embraceZoneRadius;
        }

        private bool HandNearBaby(IHandInput hand)
        {
            return babyAnchor != null && Vector3.Distance(hand.Position, babyAnchor.position) <= patReachDistance;
        }

        private void SetSubtitle(string text)
        {
            if (subtitleText != null) subtitleText.text = text;
        }

        private void UpdateRingVisual()
        {
            if (progressRing == null) return;
            progressRing.fillAmount = EmbraceProgress;
            progressRing.color = Phase == EpiloguePhase.EmbracingHold ? ringHoldingColor : ringIdleColor;
        }

        private IEnumerator FadeRingOut()
        {
            float t = 0f;
            const float fadeSeconds = 0.8f;
            Color from = ringHoldingColor;
            while (t < fadeSeconds)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / fadeSeconds);
                if (progressRing != null)
                    progressRing.color = new Color(from.r, from.g, from.b, Mathf.Lerp(from.a, 0f, k));
                yield return null;
            }
        }

        /// <summary>重置演示（R 键或外部调用）：宝宝睁眼安静、隐藏弹窗、回初始提示。</summary>
        public void ResetDemo()
        {
            StopAllCoroutines();
            achievementParkedForTeaching = false;
            if (baby != null) baby.StopCry();
            if (achievementPopup != null) achievementPopup.Hide();
            if (teachingVideoPrompt != null) teachingVideoPrompt.Hide();
            if (summaryPanel != null) summaryPanel.HideSilent();
            if (petalEffect != null) petalEffect.StopAndClear();
            if (rig != null)
            {
                rig.LockHandsAtChest(false);
                rig.ResetHands();
            }
            EmbraceProgress = 0f;
            holdPulseTimer = 0f;
            subtitleIsHold = false;
            Phase = EpiloguePhase.PromptEmbrace;
            if (subtitleText != null) subtitleText.transform.parent.gameObject.SetActive(true);
            SetSubtitle(PromptEmbraceText);
            UpdateRingVisual();
            Debug.Log("[Epilogue] 演示已重置");
        }
    }
}
