using UnityEngine;
namespace VRTour
{
    public class ClickableDevice : MonoBehaviour
    {
        [Header("点击后相机飞到这个机位")]
        public Transform targetCameraPoint;
        [Header("【新增】延时之后退回到这个指定机位")]
        public Transform returnCameraPoint;
        [Header("跳转后等待多少秒退回，<=0不退回")]
        public float autoBackDelay = 7f;

        [Header("设备讲解文字")]
        [Multiline]
        public string introText;
        [Header("介绍文字显示时长（秒）")]
        public float tipDuration = 4f;
        [Header("拖拽主相机上的 OpeningIntro 组件")]
        public OpeningIntro openingIntro;
        public Animator bedPlateAnimator;
        public string animStateName = "bad";
        private CameraTour cameraTour;

        void Start()
        {
            if (openingIntro == null)
            {
                openingIntro = FindFirstObjectByType<OpeningIntro>();
            }
        }

        private CameraTour GetCameraTour()
        {
            if (cameraTour == null)
            {
                cameraTour = FindFirstObjectByType<CameraTour>();
            }
            return cameraTour;
        }

        public void PlayDeviceIntro()
        {
            if (openingIntro != null && !openingIntro.tourCanStart) return;

            if (targetCameraPoint != null)
            {
                CameraTour camTour = GetCameraTour();
                if (camTour != null)
                {
                    if(autoBackDelay > 0 && returnCameraPoint != null)
                    {
                        // 带指定退回机位
                        camTour.SetCameraTargetWithAutoBack(targetCameraPoint, returnCameraPoint, autoBackDelay);
                    }
                    else
                    {
                        // 不自动退回，原有逻辑
                        camTour.SetCameraTarget(targetCameraPoint);
                    }
                }
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowTip(introText, tipDuration);
            }

            if (bedPlateAnimator != null)
            {
                bedPlateAnimator.Play(animStateName, 0, 0f);
            }
        }
    }
}
