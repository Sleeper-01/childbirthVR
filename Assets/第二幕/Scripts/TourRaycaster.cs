using UnityEngine;

namespace VRTour
{
    public class TourRaycaster : MonoBehaviour
    {
        [Header("可点击物体的层级")]
        [SerializeField]
        private LayerMask interactiveLayers = ~0;

        [Header("射线最大距离")]
        [SerializeField]
        private float maxRayDistance = 100f;

        private Camera mainCamera;
        private bool prevVRTrigger;

        void Update()
        {
            // VR手柄射线（扳机边沿检测）
            bool vrTrigger = ChuJian.Merge.VRPointerVisual.IsAnyTriggerHeld;
            bool vrDown = vrTrigger && !prevVRTrigger;
            prevVRTrigger = vrTrigger;

            if (vrDown)
            {
                TryVRClick();
                return;
            }

            // 桌面鼠标（原有逻辑）
            if (!Input.GetMouseButtonDown(0)) return;
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            HandleRayHit(mainCamera.ScreenPointToRay(Input.mousePosition));
        }

        void TryVRClick()
        {
            var vis = FindObjectOfType<ChuJian.Merge.VRPointerVisual>();
            Vector3 origin, dir;
            if (vis == null || !vis.GetRightRay(out origin, out dir)) return;
            HandleRayHit(new Ray(origin, dir));
        }

        void HandleRayHit(Ray ray)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, interactiveLayers))
            {
                ClickableDevice device = hit.collider.GetComponentInParent<ClickableDevice>();
                if (device != null)
                {
                    device.PlayDeviceIntro();
                }
            }
        }
    }
}
