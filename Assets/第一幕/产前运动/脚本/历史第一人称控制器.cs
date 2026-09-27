using UnityEngine;

namespace 产前运动
{
    /// <summary>
    /// 第一幕第一人称：只负责 WASD 移动与对话状态；视角统一走全局的右键拖动
    /// （MergeScripts.DesktopCameraLook），身体朝向跟随相机水平朝向，保证移动方向与视线一致。
    /// 指针全程不锁定：全局 UI（返回主菜单/工具栏等）随时可点。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class 历史第一人称控制器 : MonoBehaviour
    {
        public Camera viewCamera;
        public float speed = 3f;
        public float sensitivity = 2f;
        public bool InConversation { get; private set; }
        public bool IsExploring => !InConversation;
        private CharacterController motor;
        private float verticalVelocity;
        private void Awake() { motor = GetComponent<CharacterController>(); }
        private void Start() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        private void Update()
        {
            // 身体朝向跟随相机水平朝向（相机由全局右键拖动控制）
            if (viewCamera != null)
            {
                var e = viewCamera.transform.eulerAngles;
                transform.rotation = Quaternion.Euler(0f, e.y, 0f);
            }
            Move(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), Time.deltaTime);
        }
        public void Move(Vector2 input, float deltaTime)
        {
            if (InConversation) return;
            if (motor.isGrounded && verticalVelocity < 0) verticalVelocity = -2;
            verticalVelocity = Mathf.Max(-30, verticalVelocity - 20 * deltaTime);
            var direction = Vector2.ClampMagnitude(input, 1);
            var velocity = (transform.right * direction.x + transform.forward * direction.y) * speed;
            velocity.y = verticalVelocity;
            motor.Move(velocity * deltaTime);
        }
        public void SetConversation(bool value)
        {
            InConversation = value;
            if (value) verticalVelocity = 0;
        }
        public void ReleasePointer()
        { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        public void LockPointer()
        { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        private void OnApplicationFocus(bool focused) { if (!focused) ReleasePointer(); }
        private void OnDisable() { ReleasePointer(); }
    }
}
