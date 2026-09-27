using UnityEngine;

namespace VRHospitalAI
{
    /// <summary>
    /// VR 射线交互：从控制器/手柄发出射线，命中带 NPCBehaviour 的物体，
    /// 按下扳机/确认键时触发对话。
    ///
    /// 兼容两种用法：
    /// 1) 简易方案：用普通的射线检测（本脚本），手动接 XR 控制器的按键。
    /// 2) 官方方案：用 XR Interaction Toolkit 的 XRGrabInteractable / 简单 Ray，
    ///    把本脚本的 InteractWith 暴露给 UI 事件调用。
    /// </summary>
    public class VRInteractor : MonoBehaviour
    {
        [Header("射线发射点（通常是 XR 控制器的 transform）")]
        public Transform rayOrigin;

        [Header("射线参数")]
        public float maxDistance = 10f;
        public LayerMask npcLayer = ~0;   // 默认所有层，建议在 Inspector 设成只含 NPC 层

        [Header("按键（简易方案用；接 XR Input 时可忽略）")]
        public KeyCode triggerKey = KeyCode.Mouse0; // 开发期用鼠标左键测试

        [Header("射线可视化")]
        public LineRenderer lineRenderer;
        public Gradient normalColor;
        public Gradient hoverColor;

        private NPCBehaviour currentHover;
        private NPCBehaviour currentTalking;

        void Update()
        {
            if (rayOrigin == null) return;

            // 发射射线，检测命中 NPC
            bool hit = Physics.Raycast(rayOrigin.position, rayOrigin.forward,
                                       out RaycastHit info, maxDistance, npcLayer);
            var hovered = hit ? info.collider.GetComponent<NPCBehaviour>() : null;

            UpdateHover(hovered);
            UpdateLineRenderer(hit, info);

            // 触发交互
            if (Input.GetKeyDown(triggerKey) && hovered != null)
            {
                // 如果当前正在跟别人说话，先结束
                if (currentTalking != null && currentTalking != hovered)
                    currentTalking.EndConversation();

                currentTalking = hovered;
                hovered.Interact();
            }
        }

        void UpdateHover(NPCBehaviour newHover)
        {
            if (newHover == currentHover) return;
            currentHover = newHover;
            // 这里可加 hover 高亮/注视等反馈
        }

        void UpdateLineRenderer(bool hit, RaycastHit info)
        {
            if (lineRenderer == null) return;
            lineRenderer.enabled = true;
            Vector3 end = hit ? info.point : rayOrigin.position + rayOrigin.forward * maxDistance;
            lineRenderer.SetPosition(0, rayOrigin.position);
            lineRenderer.SetPosition(1, end);
            lineRenderer.colorGradient = currentHover != null ? hoverColor : normalColor;
        }

        // 供 XR Interaction Toolkit 的 UI 事件调用（如果你用官方 XR）
        public void InteractWith(GameObject target)
        {
            var npc = target.GetComponent<NPCBehaviour>();
            if (npc != null)
            {
                if (currentTalking != null && currentTalking != npc)
                    currentTalking.EndConversation();
                currentTalking = npc;
                npc.Interact();
            }
        }
    }
}
