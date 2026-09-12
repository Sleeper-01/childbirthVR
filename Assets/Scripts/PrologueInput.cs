using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
// UnityEngine.XR 与 UnityEngine.InputSystem 都定义了 CommonUsages，
// 两个 using 同时存在会导致 CS0104 歧义（曾使整个程序集编译失败、编辑器进安全模式）。
// 用 using 别名明确绑定到 XR 版本，彻底消除歧义。
using CommonUsages = UnityEngine.XR.CommonUsages;

namespace ChanFangVR
{
    /// 输入：VR 手柄 + 桌面鼠标/键盘/手柄，统一为「射线瞄准 + 扳机 + 摇杆」三个信号。
    /// 不依赖 XRI 的 Interactor 体系，直接用物理射线命中 PrologueInteractable。
    ///
    /// 手柄映射（VR 控制器 / 通用手柄通用）：
    ///   左摇杆左右  = 选项切换（翻页、选房、推进步骤）
    ///   右摇杆      = 转动摄像头（左右转身，上下抬头/低头）
    ///   右扳机      = 确认 / 交互（等同 VR 设备扳机）
    public class PrologueInput : MonoBehaviour
    {
        // 右摇杆转视角速度（度/秒）
        private const float TurnSpeedDeg = 110f;
        private const float PitchSpeedDeg = 70f;
        private const float StickDeadZone = 0.25f;

        private PrologueWorld _world;
        private PrologueManager _manager;

        private Transform _handL;
        private Transform _handR;
        private PrologueInteractable _hover;
        private bool _joyLatch;
        private bool _triggerPrev;

        public static PrologueInput Create(PrologueWorld world, PrologueManager manager)
        {
            var go = new GameObject("PrologueInput");
            var input = go.AddComponent<PrologueInput>();
            input._world = world;
            input._manager = manager;
            input.Build();
            return input;
        }

        private void Build()
        {
            // 手柄挂在 XR rig 上：右摇杆转身后射线仍与手柄一致
            var anchor = _world.TrackingSpace != null ? _world.TrackingSpace
                      : (_world.CameraRig != null ? _world.CameraRig : _world.transform);
            _handL = new GameObject("HandL").transform;
            _handL.SetParent(anchor, false);
            _handR = new GameObject("HandR").transform;
            _handR.SetParent(anchor, false);
        }

        private void Update()
        {
            if (_world == null) return;

            _world.UpdateHandTransforms(_handL, _handR);
            UpdateAim();
            UpdateLeftStick();   // 左摇杆：选项
            UpdateRightStick();  // 右摇杆：摄像头
        }

        private void UpdateAim()
        {
            Ray ray;
            if (_world.VRMode && _handR != null)
            {
                ray = new Ray(_handR.position, _handR.forward);
            }
            else
            {
                var cam = _world.MainCamera;
                if (cam == null) return;
                ray = cam.ScreenPointToRay(DesktopInput.mousePosition);
            }

            RaycastHit hit;
            PrologueInteractable found = null;

            if (_world.VRMode)
            {
                // VR：沿手柄射线命中 UI 层（仅 UI 层掩码，世界几何不拦截）
                int uiMask = 1 << 5;
                if (Physics.Raycast(ray, out hit, 12f, uiMask))
                {
                    var it = hit.collider.GetComponent<PrologueInteractable>();
                    if (it != null && it.Interactive && it.isActiveAndEnabled) found = it;
                }
            }
            else
            {
                // 桌面：直接做屏幕空间命中测试判定头部工具栏按钮。
                // 不依赖物理射线 / 图层掩码，床、房间、手册等世界几何永远不会拦截工具栏点击。
                found = HitTestToolbarScreenSpace();
            }

            // 世界交互物（手册/场景卡/护士/光点）：排除 UI 层，避免工具栏碰撞体误截
            if (found == null && Physics.Raycast(ray, out hit, 12f, ~(1 << 5)))
            {
                var it = hit.collider.GetComponent<PrologueInteractable>();
                if (it != null && it.Interactive && it.isActiveAndEnabled) found = it;
            }

            if (found != _hover)
            {
                if (_hover != null) _hover.SetHover(false);
                _hover = found;
                if (_hover != null) _hover.SetHover(true);
            }

            bool trigger = ReadTriggerDown();
            if (trigger && _hover != null)
            {
                _hover.NotifyActivated();
            }
        }

        private ToolbarButton[] _uiButtons;

        /// 桌面模式：把工具栏按钮的世界位置投影到屏幕，直接做矩形命中测试。
        /// 不依赖物理射线/图层掩码，因此「暂停/重播/帮助/舒适度」永远可点，
        /// 不会被床、房间、手册等世界几何体拦截。
        private PrologueInteractable HitTestToolbarScreenSpace()
        {
            var cam = _world.MainCamera;
            var ui = _world.UI;
            if (cam == null || ui == null) return null;

            if (_uiButtons == null)
                _uiButtons = ui.GetComponentsInChildren<ToolbarButton>(true);

            var mouse = DesktopInput.mousePosition;
            for (int i = 0; i < _uiButtons.Length; i++)
            {
                var btn = _uiButtons[i];
                if (btn == null || !btn.Interactive || !btn.isActiveAndEnabled) continue;
                var t = btn.transform;
                if (!t.gameObject.activeInHierarchy) continue;

                var col = btn.GetComponent<BoxCollider>();
                var center = col != null ? t.TransformPoint(col.center) : t.position;
                float halfW, halfH;
                if (col != null)
                {
                    var s = t.lossyScale;
                    halfW = col.size.x * Mathf.Abs(s.x) * 0.5f;
                    halfH = col.size.y * Mathf.Abs(s.y) * 0.5f;
                }
                else
                {
                    halfW = 0.078f;
                    halfH = 0.037f;
                }

                var sc = cam.WorldToScreenPoint(center);
                if (sc.z <= 0f) continue;   // 在相机后方

                var sx = cam.WorldToScreenPoint(center + cam.transform.right * halfW);
                var sy = cam.WorldToScreenPoint(center + cam.transform.up * halfH);
                float rx = Mathf.Abs(sx.x - sc.x);
                float ry = Mathf.Abs(sy.y - sc.y);
                if (Mathf.Abs(mouse.x - sc.x) <= rx && Mathf.Abs(mouse.y - sc.y) <= ry)
                    return btn;
            }
            return null;
        }

        /// 扳机（确认/交互）：VR 模式读右扳机（左扳机兜底）；桌面模式读鼠标左键 + 手柄右扳机。
        /// 同时兼容 triggerButton（布尔）与 trigger（0~1 模拟量）两种上报方式，
        /// 避免部分 OpenXR 运行时只上报模拟量导致「扣扳机没反应」。
        private bool ReadTriggerDown()
        {
            bool pressed;

            if (_world != null && _world.VRMode)
            {
                pressed = ReadXRTrigger(XRNode.RightHand) || ReadXRTrigger(XRNode.LeftHand);
                if (!pressed) pressed = ReadPadTrigger();
            }
            else
            {
                pressed = DesktopInput.GetMouseButton(0) || ReadPadTrigger();
                // 未接手柄时，桌面也可直接按空格/E 代替扳机
                if (!pressed) pressed = DesktopInput.GetKey(KeyCode.E);
            }

            bool down = pressed && !_triggerPrev;
            _triggerPrev = pressed;
            return down;
        }

        private static bool ReadXRTrigger(XRNode node)
        {
            var dev = InputDevices.GetDeviceAtXRNode(node);
            if (!dev.isValid) return false;
            bool btn = false;
            if (dev.TryGetFeatureValue(CommonUsages.triggerButton, out btn) && btn) return true;
            float v = 0f;
            if (dev.TryGetFeatureValue(CommonUsages.trigger, out v) && v >= 0.5f) return true;
            // 部分设备把手柄扳机映射到 indexFinger / grip
            float grip = 0f;
            if (dev.TryGetFeatureValue(CommonUsages.grip, out grip) && grip >= 0.9f) return true;
            bool gripBtn = false;
            if (dev.TryGetFeatureValue(CommonUsages.gripButton, out gripBtn) && gripBtn) return true;
            return false;
        }

        private static bool ReadPadTrigger()
        {
            var gp = Gamepad.current;
            if (gp == null) return false;
            return gp.rightTrigger.ReadValue() >= 0.5f;
        }

        /// 左摇杆：选项切换（手册翻页 / 场景卡选择 / 步骤推进），带回中闩锁避免连发
        private void UpdateLeftStick()
        {
            float x = 0f;

            if (_world.VRMode)
            {
                var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
                Vector2 axis;
                if (left.isValid && left.TryGetFeatureValue(CommonUsages.primary2DAxis, out axis))
                    x = axis.x;
            }
            else
            {
                x = DesktopInput.GetAxis("Horizontal");
                var gp = Gamepad.current;
                if (Mathf.Abs(x) < 0.01f && gp != null)
                {
                    float gx = gp.leftStick.ReadValue().x;
                    if (Mathf.Abs(gx) > StickDeadZone) x = gx;
                }
                if (Mathf.Abs(x) < 0.01f)
                {
                    if (DesktopInput.GetKey(KeyCode.A) || DesktopInput.GetKey(KeyCode.LeftArrow)) x = -1f;
                    else if (DesktopInput.GetKey(KeyCode.D) || DesktopInput.GetKey(KeyCode.RightArrow)) x = 1f;
                }
            }

            if (Mathf.Abs(x) > 0.55f)
            {
                if (!_joyLatch)
                {
                    _joyLatch = true;
                    if (_manager != null) _manager.OnJoystick(x > 0f ? 1 : -1);
                }
            }
            else if (Mathf.Abs(x) < 0.25f)
            {
                _joyLatch = false;
            }
        }

        /// 右摇杆：转动摄像头（左右转身 yaw，上下抬头/低头 pitch）
        private void UpdateRightStick()
        {
            float dx = 0f, dy = 0f;

            if (_world.VRMode)
            {
                var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
                Vector2 axis;
                if (right.isValid && right.TryGetFeatureValue(CommonUsages.primary2DAxis, out axis))
                {
                    dx = axis.x;
                    dy = axis.y;
                }
            }
            else
            {
                var gp = Gamepad.current;
                if (gp != null)
                {
                    Vector2 rs = gp.rightStick.ReadValue();
                    dx = rs.x;
                    dy = rs.y;
                }
            }

            dx = ApplyDeadZone(dx);
            dy = ApplyDeadZone(dy);
            if (Mathf.Abs(dx) < 0.0001f && Mathf.Abs(dy) < 0.0001f) return;

            float dt = Time.unscaledDeltaTime;
            _world.AddLookDelta(dx * TurnSpeedDeg * dt, dy * PitchSpeedDeg * dt);
        }

        private static float ApplyDeadZone(float v)
        {
            float a = Mathf.Abs(v);
            if (a < StickDeadZone) return 0f;
            return Mathf.Sign(v) * (a - StickDeadZone) / (1f - StickDeadZone);
        }
    }
}
