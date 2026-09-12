using UnityEngine;

namespace ChanFangVR
{
    /// 序章唯一入口：挂在场景中一个空物体上即可，其余组件全部由代码创建。
    public class PrologueBootstrap : MonoBehaviour
    {
        [Tooltip("启动时是否进入 VR 模式（无头显时会自动保持桌面模式）")]
        public bool StartInVR = false;

        private void Awake()
        {
            PrologueWorld.Init();

            var world = PrologueWorld.Create();
            var audio = PrologueAudio.Create(world.transform);
            var ui = PrologueUI.Create(world.MainCamera, audio);

            world.UI = ui;
            world.Audio = audio;

            var manager = PrologueManager.Create(world, ui, audio);
            PrologueInput.Create(world, manager);

            if (StartInVR) world.SetVRMode(true);
        }
    }
}
