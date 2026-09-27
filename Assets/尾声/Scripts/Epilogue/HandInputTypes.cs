using UnityEngine;

namespace ChuJianXinSheng.Epilogue
{
    public enum HandSide { Left, Right }

    /// <summary>
    /// 手部输入抽象层。真实 XR 手柄与键盘模拟手均实现此接口；
    /// 交互状态机只依赖本接口，后续接入 XR SDK 时仅需替换输入实现，无需改动交互逻辑。
    /// </summary>
    public interface IHandInput
    {
        HandSide Side { get; }

        /// <summary>手部当前位置（世界坐标）。</summary>
        Vector3 Position { get; }

        /// <summary>手部朝向。</summary>
        Quaternion Rotation { get; }

        /// <summary>侧握键是否持续按住（对应"扣住侧握键"）。</summary>
        bool GripHeld { get; }

        /// <summary>扳机键是否发生一次轻点（读取后自动清除，防止一次点击跨阶段重复消费）。</summary>
        bool TriggerTapped { get; }

        /// <summary>手柄震动反馈（键盘模拟下以视觉脉冲呈现，XR 下映射为真实震动）。</summary>
        void HapticPulse(float amplitude, float duration);
    }

    /// <summary>
    /// 双手输入设备抽象。鼠标模拟 Rig 与未来的 XR Rig 均实现此接口，
    /// EpilogueDirector 只认 IHandsRig，实现可整体替换。
    /// </summary>
    public interface IHandsRig
    {
        IHandInput Left { get; }
        IHandInput Right { get; }

        /// <summary>将双手复位到初始体侧位置。</summary>
        void ResetHands();

        /// <summary>环抱完成后锁定双手于胸前环抱位（后续轻拍不必重复长按）；传 false 解锁。</summary>
        void LockHandsAtChest(bool locked);
    }
}
