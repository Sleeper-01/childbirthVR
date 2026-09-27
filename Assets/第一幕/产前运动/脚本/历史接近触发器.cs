using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace 产前运动
{
    // Evaluate proximity before the player processes input, so the triggering frame cannot move the camera.
    [DefaultExecutionOrder(-30)]
    public class 历史接近触发器 : MonoBehaviour
    {
        public 历史第一人称控制器 player;
        public 讲解控制器 lesson;
        public Transform panel;
        public Button exit;
        public GameObject crosshair;
        public TMP_Text hint;
        public float enterDistance = 2.5f, rearmDistance = 3.5f;
        public bool IsOpen { get; private set; }
        public bool IsArmed { get; private set; } = true;
        private void Start() { exit.onClick.AddListener(EndConversation); }
        private void Update()
        {
            if (IsOpen)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) EndConversation();
            }
            else EvaluateProximity();
            // VR 交互规范：不使用 FPS 式屏幕中心准星；可交互性由世界内物体的悬停反馈承担
            if (crosshair != null && crosshair.activeSelf) crosshair.SetActive(false);
            hint.text = IsOpen ? "鼠标点击 · 输入问题   |   Esc 结束对话" :
                !player.IsExploring ? "点击画面继续探索" :
                IsArmed ? "WASD 行走 · 右键拖动转向   |   靠近护士开始交流" : "已结束对话 · 离开护士后再次靠近可继续";
        }
        public void EvaluateProximity()
        {
            if (IsOpen) return;
            float distance = Vector3.Distance(player.transform.position, new Vector3(transform.position.x, player.transform.position.y, transform.position.z));
            if (distance > rearmDistance) IsArmed = true;
            if (IsArmed && distance <= enterDistance) BeginConversation();
        }
        private void BeginConversation()
        {
            IsArmed = false; IsOpen = true;
            var toward = player.transform.position - transform.position; toward.y = 0;
            if (toward.sqrMagnitude < .01f) toward = -transform.forward;
            toward.Normalize();
            var right = Vector3.Cross(toward, Vector3.up);
            panel.localScale = Vector3.one * .0025f;
            panel.position = transform.position + Vector3.up * .65f + right * 1.9f - toward * .9f;
            FaceCamera();
            player.SetConversation(true); lesson.OpenInteraction(); crosshair.SetActive(false);
        }
        private void LateUpdate()
        {
            if (IsOpen) FaceCamera();
        }
        private void FaceCamera()
        {
            // Canvas text faces local -Z; use the camera-to-panel vector for +Z.
            var forward = panel.position - player.viewCamera.transform.position;
            if (forward.sqrMagnitude > .0001f)
                panel.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }
        public void EndConversation()
        {
            if (!IsOpen) return;
            lesson.CloseInteraction(); IsOpen = false;
            player.SetConversation(false);
        }
        private void OnDisable()
        {
            if (IsOpen && lesson != null) lesson.CloseInteraction();
            IsOpen = false;
            if (player != null) { player.SetConversation(false); player.ReleasePointer(); }
        }
    }
}
