using UnityEngine;

namespace VRHospitalAI.ThirdAct
{
    /// <summary>
    /// VR 手柄输入抽象层。
    /// PC 调试用键盘，VR 设备用 Unity Input 系统。
    ///
    /// PC 键位：空格=Trigger，左Ctrl=Grip，ESC=Menu，WASD=摇杆
    /// VR 键位：Trigger/Grip/Menu 按钮 + VRJoystick/Thumbstick 摇杆
    /// </summary>
    public static class VRInput
    {
        // ── 按键 ──────────────────────────────────────────────

        public static bool TriggerDown
        {
            get
            {
                if (IsXRConnected()) return Input.GetButtonDown("Trigger");
                // PC 调试：直接检测空格键，每次按下去都返回 true
                return Input.GetKeyDown(KeyCode.Space);
            }
        }

        public static bool TriggerHeld
        {
            get
            {
                if (IsXRConnected()) return Input.GetButton("Trigger");
                return Input.GetKey(KeyCode.Space);
            }
        }

        public static bool TriggerUp
        {
            get
            {
                if (IsXRConnected()) return Input.GetButtonUp("Trigger");
                return Input.GetKeyUp(KeyCode.Space);
            }
        }

        public static bool GripDown
        {
            get
            {
                if (IsXRConnected()) return Input.GetButtonDown("Grip");
                return Input.GetKeyDown(KeyCode.LeftControl);
            }
        }

        public static bool GripHeld
        {
            get
            {
                if (IsXRConnected()) return Input.GetButton("Grip");
                return Input.GetKey(KeyCode.LeftControl);
            }
        }

        public static bool MenuDown
        {
            get
            {
                if (IsXRConnected()) return Input.GetButtonDown("Menu");
                return Input.GetKeyDown(KeyCode.Escape);
            }
        }

        // ── 摇杆 ──────────────────────────────────────────────

        public static float LeftStickX
        {
            get
            {
                if (IsXRConnected()) return Input.GetAxis("VRJoystickX");
                return Input.GetAxisRaw("Horizontal");
            }
        }

        public static float LeftStickY
        {
            get
            {
                if (IsXRConnected()) return Input.GetAxis("VRJoystickY");
                return Input.GetAxisRaw("Vertical");
            }
        }

        public static float RightStickX => Input.GetAxis("ThumbstickX");
        public static float RightStickY => Input.GetAxis("ThumbstickY");
        public static float TriggerAxis => Input.GetAxis("Trigger");

        // ── 震动（VR 设备时有用，PC 时静默忽略） ───────────────

        public static void VibrateRight(float strength = 0.5f, float duration = 0.1f) { }
        public static void VibrateLeft(float strength = 0.5f, float duration = 0.1f) { }
        public static void VibrateBoth(float strength = 0.5f, float duration = 0.1f) { }

        // ── 私有 ──────────────────────────────────────────────

        static bool IsXRConnected()
        {
            try
            {
                return UnityEngine.XR.XRSettings.loadedDeviceName != string.Empty;
            }
            catch { return false; }
        }
    }
}
