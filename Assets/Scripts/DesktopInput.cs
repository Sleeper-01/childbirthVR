using UnityEngine;

namespace ChanFangVR
{
    /// <summary>
    /// 桌面（键盘 / 鼠标）输入的兼容读取层。
    ///
    /// Project Settings -> Player -> Active Input Handling 有三种取值：
    ///   0 = 仅旧 Input Manager，1 = 仅新 Input System，2 = 两者都启用。
    /// 只有取值 1 时 UnityEngine.Input 每个方法都会抛 InvalidOperationException，
    /// 会让调用处后面的代码整段不执行（本项目曾因此导致相机一直停在原点 = "跑到地图底下"），
    /// 并且触发 Console 的 Error Pause 造成"一启动就自动暂停"。
    ///
    /// 本类在新旧两种后端下都能正常读取：旧后端走 UnityEngine.Input，
    /// 仅启用新后端时走 UnityEngine.InputSystem，行为保持一致。
    /// 业务代码请统一用 DesktopInput.xxx 代替 Input.xxx。
    /// </summary>
    public static class DesktopInput
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        private const bool UseInputSystem = true;
#else
        private const bool UseInputSystem = false;
#endif

        /// <summary>
        /// 新输入系统下鼠标位移的缩放。
        /// 新 InputSystem 的 Mouse.delta 是「原始像素增量」，而旧 Input 的 "Mouse X/Y"
        /// 轴在 Input Manager 里默认 Sensitivity = 0.1，两者相差 10 倍。
        /// 这里乘 0.1 把新后端对齐到旧后端的手感；想再快/再慢直接调这个值（0.05 更慢，0.2 更快）。
        /// </summary>
        private const float MouseDeltaScale = 0.1f;

        public static bool UsingInputSystem { get { return UseInputSystem; } }

        public static Vector3 mousePosition
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                var m = UnityEngine.InputSystem.Mouse.current;
                if (m == null) return Vector3.zero;
                var p = m.position.ReadValue();
                return new Vector3(p.x, p.y, 0f);
#else
                return Input.mousePosition;
#endif
            }
        }

        public static bool GetMouseButton(int button)
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var c = MouseButtonControl(button);
            return c != null && c.isPressed;
#else
            return Input.GetMouseButton(button);
#endif
        }

        public static bool GetMouseButtonDown(int button)
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var c = MouseButtonControl(button);
            return c != null && c.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(button);
#endif
        }

        public static bool GetMouseButtonUp(int button)
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var c = MouseButtonControl(button);
            return c != null && c.wasReleasedThisFrame;
#else
            return Input.GetMouseButtonUp(button);
#endif
        }

        public static bool GetKey(KeyCode code)
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return false;
            var key = ToKey(code);
            if (key == UnityEngine.InputSystem.Key.None) return false;
            return kb[key].isPressed;
#else
            return Input.GetKey(code);
#endif
        }

        public static bool GetKeyDown(KeyCode code)
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return false;
            var key = ToKey(code);
            if (key == UnityEngine.InputSystem.Key.None) return false;
            return kb[key].wasPressedThisFrame;
#else
            return Input.GetKeyDown(code);
#endif
        }

        public static bool GetKeyUp(KeyCode code)
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return false;
            var key = ToKey(code);
            if (key == UnityEngine.InputSystem.Key.None) return false;
            return kb[key].wasReleasedThisFrame;
#else
            return Input.GetKeyUp(code);
#endif
        }

        /// <summary>仅支持本项目用到的轴：Mouse X / Mouse Y / Horizontal / Vertical。</summary>
        public static float GetAxis(string name)
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var m = UnityEngine.InputSystem.Mouse.current;
            if (name == "Mouse X")
                return m == null ? 0f : m.delta.x.ReadValue() * MouseDeltaScale;
            if (name == "Mouse Y")
                return m == null ? 0f : m.delta.y.ReadValue() * MouseDeltaScale;
            if (name == "Horizontal")
            {
                float v = 0f;
                if (GetKey(KeyCode.A) || GetKey(KeyCode.LeftArrow)) v -= 1f;
                if (GetKey(KeyCode.D) || GetKey(KeyCode.RightArrow)) v += 1f;
                return v;
            }
            if (name == "Vertical")
            {
                float v = 0f;
                if (GetKey(KeyCode.S) || GetKey(KeyCode.DownArrow)) v -= 1f;
                if (GetKey(KeyCode.W) || GetKey(KeyCode.UpArrow)) v += 1f;
                return v;
            }
            return 0f;
#else
            return Input.GetAxis(name);
#endif
        }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        private static UnityEngine.InputSystem.Controls.ButtonControl MouseButtonControl(int button)
        {
            var m = UnityEngine.InputSystem.Mouse.current;
            if (m == null) return null;
            switch (button)
            {
                case 0: return m.leftButton;
                case 1: return m.rightButton;
                case 2: return m.middleButton;
                default: return null;
            }
        }

        /// <summary>把 UnityEngine.KeyCode 映射到 InputSystem 的 Key；不支持的键返回 None。</summary>
        private static UnityEngine.InputSystem.Key ToKey(KeyCode code)
        {
            // A~Z 在两个枚举里都是连续区间，可直接做偏移换算
            if (code >= KeyCode.A && code <= KeyCode.Z)
                return UnityEngine.InputSystem.Key.A + (code - KeyCode.A);

            switch (code)
            {
                case KeyCode.Space: return UnityEngine.InputSystem.Key.Space;
                case KeyCode.Return: return UnityEngine.InputSystem.Key.Enter;
                case KeyCode.KeypadEnter: return UnityEngine.InputSystem.Key.Enter;
                case KeyCode.Escape: return UnityEngine.InputSystem.Key.Escape;
                case KeyCode.Tab: return UnityEngine.InputSystem.Key.Tab;
                case KeyCode.Backspace: return UnityEngine.InputSystem.Key.Backspace;
                case KeyCode.LeftArrow: return UnityEngine.InputSystem.Key.LeftArrow;
                case KeyCode.RightArrow: return UnityEngine.InputSystem.Key.RightArrow;
                case KeyCode.UpArrow: return UnityEngine.InputSystem.Key.UpArrow;
                case KeyCode.DownArrow: return UnityEngine.InputSystem.Key.DownArrow;
                case KeyCode.LeftShift: return UnityEngine.InputSystem.Key.LeftShift;
                case KeyCode.RightShift: return UnityEngine.InputSystem.Key.RightShift;
                default: return UnityEngine.InputSystem.Key.None;
            }
        }
#endif
    }
}
