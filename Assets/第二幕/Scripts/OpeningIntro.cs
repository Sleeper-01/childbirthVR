using UnityEngine;
using TMPro;

namespace VRTour
{
    //开场迎接：产科医生分步口播，每行显示后等用户点击再继续进一步介绍
    //全部讲完隐藏文字、置 tourCanStart = true，并点亮设备描边（高亮兴趣点时序）
    public class OpeningIntro : MonoBehaviour
    {
        public TMP_Text openingText;

        [Header("医生迎接口播文字（逐行显示，点击继续；留空行会自动跳过）")]
        [TextArea]
        public string[] greetingLines =
        {
            "您好，我是产科医生李静，欢迎来到产房。整个分娩过程中，我和团队会全程陪伴您。",
            "先带您认识一下房间：中央是可调节产床，靠背可以抬起切换成坐位，方便分娩操作。",
            "左边是母婴监护仪，实时读取心率和血压；右边是新生儿保暖台，宝宝出生后会在这里保暖。准备好了就点击高亮设备，开始云参观吧！"
        };

        [Header("每行配套语音（Assets/Audio/opening_line1~3.mp3，菜单【产程演示/一键绑定语音引用】自动填）")]
        public AudioClip[] greetingClips;

        [Header("每行显示后多少秒内忽略点击（防误触跳过）")]
        public float clickGuard = 1f;

        public bool tourCanStart;

        private MeshOutline[] deviceOutlines;
        private AudioSource audioSource;
        private int lineIndex = -1;
        private float lineShownAt = -999f;

        void Start()
        {
            tourCanStart = false;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;

            //开场期间先熄灭设备描边，迎接结束后点亮（云参观的高亮兴趣点时序）
            deviceOutlines = FindObjectsByType<MeshOutline>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (MeshOutline outline in deviceOutlines)
            {
                outline.enabled = false;
            }

            if (greetingLines == null || greetingLines.Length == 0)
            {
                FinishIntro();
                return;
            }
            ShowNext();
        }

        void Update()
        {
            if (tourCanStart) return;
            // VR扳机或鼠标左键均可推进对话
            bool click = Input.GetMouseButtonDown(0) || ChuJian.Merge.VRPointerVisual.IsAnyTriggerHeld;
            if (click && Time.time - lineShownAt >= clickGuard)
            {
                ShowNext();
            }
        }

        private void ShowNext()
        {
            lineIndex++;
            //跳过留空的行
            while (lineIndex < greetingLines.Length && string.IsNullOrEmpty(greetingLines[lineIndex]))
            {
                lineIndex++;
            }

            if (lineIndex < greetingLines.Length)
            {
                ShowLine(lineIndex);
            }
            else
            {
                FinishIntro();
            }
        }

        private void ShowLine(int index)
        {
            lineShownAt = Time.time;
            if (openingText != null)
            {
                openingText.gameObject.SetActive(true);
                //最后一行提示开始参观，其余提示点击继续
                string suffix = index == greetingLines.Length - 1 ? "\n【点击开始参观】" : "\n【点击继续】";
                openingText.text = greetingLines[index] + suffix;
            }
            if (greetingClips != null && index < greetingClips.Length && greetingClips[index] != null)
            {
                audioSource.PlayOneShot(greetingClips[index]);
            }
        }

        private void FinishIntro()
        {
            if (openingText != null)
            {
                openingText.gameObject.SetActive(false);
            }
            tourCanStart = true;
            //点亮设备的高亮描边（三个兴趣点）
            foreach (MeshOutline outline in deviceOutlines)
            {
                if (outline != null)
                {
                    outline.enabled = true;
                }
            }
        }
    }
}
