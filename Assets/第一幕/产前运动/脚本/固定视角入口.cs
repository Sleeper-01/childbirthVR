using UnityEngine;

namespace 产前运动
{
    public class 固定视角入口 : MonoBehaviour
    {
        public 讲解控制器 lesson;
        private void Awake() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        private void OnDisable() { if (lesson != null) lesson.CloseInteraction(); }
    }
}
