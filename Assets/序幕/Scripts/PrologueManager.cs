using System;
using System.Collections;
using UnityEngine;

namespace ChanFangVR
{
    /// 序章流程管理器（表4 五步骤）。
    /// 用协程顺序推进：每步先播台词，再显式等待用户做出该步要求的操作。
    public class PrologueManager : MonoBehaviour
    {
        public enum StepEvent { None, LightDot, Grab, Flip, SceneCard, Pause, Resume }

        private PrologueWorld _world;
        private PrologueUI _ui;
        private PrologueAudio _audio;

        private PrologueStep _step = PrologueStep.Boot;
        private StepEvent _waitingFor = StepEvent.None;
        private StepEvent _received = StepEvent.None;
        private int _selectedRoom;
        private bool _helpOn;
        private Coroutine _flow;
        // 自由换房（转场教学结束后解锁，可反复使用）
        private bool _roomSwitchUnlocked;
        private bool _roomSwitching;
        private Coroutine _roomSwitchCo;
        // 调试用：L 键跳过。只走键盘，不给玩家暴露按钮。
        private bool _skipRequested;

        public event Action<PrologueStep> StepChanged;
        public event Action StepCompleted;

        public PrologueStep CurrentStep { get { return _step; } }
        public bool WaitingForUser { get { return _waitingFor != StepEvent.None; } }

        public static PrologueManager Create(PrologueWorld world, PrologueUI ui, PrologueAudio audio)
        {
            var go = new GameObject("PrologueManager");
            var m = go.AddComponent<PrologueManager>();
            m._world = world;
            m._ui = ui;
            m._audio = audio;
            m.Bind();
            m.Restart();
            return m;
        }

        private void Bind()
        {
            _ui.OnHelp = () =>
            {
                _audio.PlayClick();
                _helpOn = !_helpOn;
                _ui.HelpVisible(_helpOn);
            };
            _ui.OnReplay = () => Replay();
            _ui.OnPauseToggle = () => TogglePause();
            _ui.OnRoomSwitch = () => ToggleRoomSwitch();

            _world.LightDot.Activated += () => Notify(StepEvent.LightDot);

            _world.Handbook.Activated += () =>
            {
                if (_world.Handbook.Held)
                {
                    // 手持时再次点击 = 把手册放回原处。仅在流程正等待「翻页」时才算完成该步骤，
                    // 避免台词播放阶段提前放下导致后续 WaitFor(Flip) 卡死。
                    if (_waitingFor == StepEvent.Flip)
                    {
                        _world.Handbook.SetHeld(false);
                        Notify(StepEvent.Flip);
                    }
                }
                else
                {
                    Notify(StepEvent.Grab);
                }
            };

            if (_world.SceneCards != null)
            {
                for (int i = 0; i < _world.SceneCards.Length; i++)
                {
                    int idx = i;
                    _world.SceneCards[i].Activated += () =>
                    {
                        _selectedRoom = idx;
                        Notify(StepEvent.SceneCard);
                    };
                }
            }
        }

        // ———— 流程 ————

        private IEnumerator RunFlow()
        {
            yield return StartCoroutine(Boot());
            yield return StartCoroutine(Calibrate());
            yield return StartCoroutine(Intro());
            yield return StartCoroutine(HandbookStep());
            yield return StartCoroutine(TransitionStep());
            yield return StartCoroutine(ToolbarStep());
            yield return StartCoroutine(Finish());
        }

        private IEnumerator Boot()
        {
            SetStep(PrologueStep.Boot);
            // 画面自白光渐亮
            _ui.Fade(Color.white, 0f, 1.8f);
            yield return WaitSeconds(1.9f, true);
        }

        private IEnumerator Calibrate()
        {
            SetStep(PrologueStep.Calibrate);
            _ui.SetTask(0, 1);
            _ui.ToolbarShow(false);
            _world.LightDot.SetCalibrated(false);
            _world.LightDot.SetTarget(true);

            yield return Say(PrologueDefs.S1_Lines);
            yield return WaitFor(StepEvent.LightDot);

            _audio.PlaySuccess();
            _ui.Toast("手柄已就绪");
            _ui.SetTask(0, 2);
            _world.LightDot.SetCalibrated(true);
            _world.Nurse.Wave();

            yield return Say(PrologueDefs.S1_After);
        }

        private IEnumerator Intro()
        {
            SetStep(PrologueStep.Intro);
            _ui.SetTask(1, 1);
            _ui.ToolbarShow(true);
            _world.Nurse.SetPose(NurseController.Pose.Idle);

            yield return Say(PrologueDefs.S2_Lines);
            _ui.SetTask(1, 2);
        }

        private IEnumerator HandbookStep()
        {
            SetStep(PrologueStep.Handbook);
            _ui.SetTask(2, 1);
            _world.Handbook.SetTarget(true);
            _world.Nurse.PointAt(_world.Handbook.transform);

            yield return Say(PrologueDefs.S3_Hint);
            yield return WaitFor(StepEvent.Grab);

            _world.Handbook.SetTarget(false);
            _world.Handbook.SetHeld(true);
            _audio.PlayPage();
            _ui.Toast("拿到《待产手册》");

            yield return Say(PrologueDefs.S3_Grab);
            yield return WaitFor(StepEvent.Flip);

            _audio.PlayChime();
            yield return Say(PrologueDefs.S3_Flip);

            _world.Handbook.SetHeld(false);
            _world.Nurse.StopPointing();
            _world.Handbook.Interactive = true;
            _ui.SetTask(2, 2);
        }

        private IEnumerator TransitionStep()
        {
            SetStep(PrologueStep.Transition);
            _ui.SetTask(3, 1);
            _selectedRoom = 0;
            _world.ShowSceneCards(true);
            _world.SelectSceneCard(0);
            _ui.Toast("选择要前往的房间：A/D 或左摇杆切换，点击 / 扣扳机确认");

            yield return Say(PrologueDefs.S4_0);
            yield return Say(PrologueDefs.S4_Hint);
            yield return WaitFor(StepEvent.SceneCard);

            // 转场：淡出 → 换房 → 淡入，全程不移动玩家
            // 注：工具栏不再需要临时禁用 —— 桌面端命中判定已改为屏幕空间矩形测试，
            // 只在鼠标真正压在按钮上才触发，不会误截场景卡的点击。
            _ui.Vignette(0.85f);
            _audio.PlayWhoosh();
            _ui.Fade(Color.black, 1f, 0.45f);
            yield return WaitSeconds(0.5f, true);

            _world.ApplyRoom(_selectedRoom);

            _ui.Fade(Color.black, 0f, 0.5f);
            yield return WaitSeconds(0.55f, true);
            _ui.Vignette(0f);

            yield return Say(PrologueDefs.S4_Done);

            _world.ShowSceneCards(false);
            _ui.SetTask(3, 2);

            // 教学结束后解锁自由换房：之后随时可按 M 或点工具栏「换房间」再去别的房间，
            // 转场不再是一次性的。
            _roomSwitchUnlocked = true;
            _ui.Toast("随时可按 M 或点「换房间」再切换房间");
        }

        // ———— 自由换房（转场教学结束后可反复使用）————

        /// 按 M 键 / 点击工具栏「换房间」调用；再次触发则取消。
        public void ToggleRoomSwitch()
        {
            if (_roomSwitching) { CancelRoomSwitch(); return; }
            if (!_roomSwitchUnlocked)
            {
                _ui.Toast("完成转场教学后即可自由切换房间");
                return;
            }
            _roomSwitchCo = StartCoroutine(RoomSwitch());
        }

        private IEnumerator RoomSwitch()
        {
            _roomSwitching = true;
            _audio.PlayClick();
            _selectedRoom = _world.RoomIndex;              // 从当前所在房间开始选
            _world.ShowSceneCards(true);
            _world.SelectSceneCard(_selectedRoom);
            _ui.Toast("选择要前往的房间：A/D 或左摇杆切换，点击 / 回车确认，M 取消");

            // 复用流程里的 WaitFor 机制：只有当前等待 SceneCard 时点击才会被接受
            yield return WaitFor(StepEvent.SceneCard);
            _world.ShowSceneCards(false);

            _ui.Vignette(0.85f);
            _audio.PlayWhoosh();
            _ui.Fade(Color.black, 1f, 0.45f);
            yield return WaitSeconds(0.5f, true);

            _world.ApplyRoom(_selectedRoom);

            _ui.Fade(Color.black, 0f, 0.5f);
            yield return WaitSeconds(0.55f, true);
            _ui.Vignette(0f);

            _ui.Toast("已前往：" + PrologueDefs.RoomNames[_selectedRoom]);
            _roomSwitching = false;
            _roomSwitchCo = null;
        }

        private void CancelRoomSwitch()
        {
            if (_roomSwitchCo != null) StopCoroutine(_roomSwitchCo);
            _roomSwitchCo = null;
            // 清掉等待状态，避免之后误触发其它步骤的 Notify
            _waitingFor = StepEvent.None;
            _received = StepEvent.None;
            if (_world != null) _world.ShowSceneCards(false);
            _roomSwitching = false;
            _ui.Toast("已取消切换房间");
        }

        /// 调试用：L 键。跳过当前这一句台词，或把当前等待的交互（校准/抓取/翻页/选卡/暂停）
        /// 直接当成已完成 —— 连按几次就能快速走完整个序章。
        /// 只走键盘，不提供 UI 按钮，避免暴露给玩家。
        public void SkipStep()
        {
            _skipRequested = true;
            _ui.Toast(_waitingFor != StepEvent.None
                ? "[调试] 跳过等待：" + _waitingFor
                : "[调试] 跳过当前台词 / 等待");
        }

        private IEnumerator ToolbarStep()
        {
            SetStep(PrologueStep.Toolbar);
            _ui.SetTask(4, 1);
            _ui.ComfortShow(true);
            _ui.BtnPause.SetTarget(true);

            yield return Say(PrologueDefs.S5_Hint);
            yield return WaitFor(StepEvent.Pause);

            _ui.BtnPause.SetTarget(false);

            // 此刻处于暂停状态，字幕仍需推进
            yield return Say(PrologueDefs.S5_Resume, true);
            _ui.BtnPause.SetTarget(true);

            yield return WaitFor(StepEvent.Resume);

            _ui.BtnPause.SetTarget(false);
            _ui.SetTask(4, 2);
            yield return Say(PrologueDefs.S5_Done);
        }

        private IEnumerator Finish()
        {
            SetStep(PrologueStep.Done);
            _ui.Banner(true);
            yield return Say(PrologueDefs.S5_End);
            var done = StepCompleted;
            if (done != null) done();
        }

        // ———— 基础工具 ————

        private void SetStep(PrologueStep step)
        {
            _step = step;
            var changed = StepChanged;
            if (changed != null) changed(step);
        }

        private IEnumerator Say(DialogueLine line, bool ignorePause = false)
        {
            return Say(new[] { line }, ignorePause);
        }

        private IEnumerator Say(DialogueLine[] lines, bool ignorePause = false)
        {
            if (lines == null) yield break;
            for (int i = 0; i < lines.Length; i++)
            {
                var ln = lines[i];
                if (ln == null) continue;
                _skipRequested = false;      // 按一次 L 只跳过当前这一句，后面的台词照常播放
                _ui.ShowLine(ln.speaker, ln.text);
                float voiced = _audio.Speak(ln.voiceKey);
                float dur = voiced > 0.2f ? voiced + 0.3f : ln.Duration;
                yield return WaitSeconds(dur, ignorePause);
            }
        }

        private IEnumerator WaitSeconds(float dur, bool ignorePause)
        {
            float t = 0f;
            while (t < dur)
            {
                if (_skipRequested) break;      // 调试：L 键立即结束当前等待（台词/淡入淡出都适用）
                if (ignorePause || !_ui.Paused) t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private IEnumerator WaitFor(StepEvent e)
        {
            _received = StepEvent.None;
            _waitingFor = e;
            _skipRequested = false;
            // 调试：L 键把当前这一步的交互要求当成已完成，流程继续往下走
            yield return new WaitUntil(() => _received == e || _skipRequested);
            if (_skipRequested && _received != e)
                _ui.Toast("[调试] 跳过等待：" + e);
            _waitingFor = StepEvent.None;
            _received = StepEvent.None;
            _skipRequested = false;
        }

        /// 交互物/按钮回调统一入口：只有当前正在等待的事件才会被接受
        public void Notify(StepEvent e)
        {
            if (_waitingFor != e) return;
            _received = e;
        }

        private void SetToolbarInteractive(bool on)
        {
            if (_ui == null) return;
            if (_ui.BtnHelp != null) _ui.BtnHelp.Interactive = on;
            if (_ui.BtnReplay != null) _ui.BtnReplay.Interactive = on;
            if (_ui.BtnPause != null) _ui.BtnPause.Interactive = on;
            if (_ui.BtnRoom != null) _ui.BtnRoom.Interactive = on;
        }

        // ———— 外部输入 ————

        /// dir: -1 左 / +1 右
        public void OnJoystick(int dir)
        {
            if (_world == null) return;

            if (_step == PrologueStep.Handbook && _world.Handbook.Held)
            {
                if (_world.Handbook.Flip(dir))
                {
                    _audio.PlayPage();
                    Notify(StepEvent.Flip);
                }
            }
            else if (_step == PrologueStep.Transition || _roomSwitching)
            {
                int max = _world.SceneCards != null ? _world.SceneCards.Length - 1 : 0;
                _selectedRoom = Mathf.Clamp(_selectedRoom + dir, 0, max);
                _world.SelectSceneCard(_selectedRoom);
                _audio.PlayBlip();
                _ui.Toast(string.Format("已选择：{0}（{1}/{2}）· 点击或扣扳机确认前往",
                    PrologueDefs.RoomNames[_selectedRoom], _selectedRoom + 1, max + 1));
            }
        }

        public void TogglePause()
        {
            _ui.Paused = !_ui.Paused;
            _ui.PauseOverlay(_ui.Paused);
            _ui.SetPauseLabel(_ui.Paused);
            _audio.PlayClick();
            _ui.Toast(_ui.Paused ? "已暂停，点击「继续」恢复" : "继续中");

            if (_step == PrologueStep.Toolbar)
            {
                Notify(_ui.Paused ? StepEvent.Pause : StepEvent.Resume);
            }
        }

        public void Replay()
        {
            _audio.PlayClick();
            _ui.Toast("重新开始序章");
            Restart();
        }

        /// 从头重放序章（工具栏「重播」与编辑器菜单共用）
        public void Restart()
        {
            if (_flow != null) StopCoroutine(_flow);
            // 自由换房状态一并复位：下一次要等转场教学走完才重新解锁
            if (_roomSwitchCo != null) StopCoroutine(_roomSwitchCo);
            _roomSwitchCo = null;
            _roomSwitchUnlocked = false;
            _roomSwitching = false;

            _received = StepEvent.None;
            _waitingFor = StepEvent.None;
            _selectedRoom = 0;
            _helpOn = false;

            _ui.Paused = false;
            _ui.PauseOverlay(false);
            _ui.SetPauseLabel(false);
            _ui.HelpVisible(false);
            _ui.Banner(false);
            _ui.ToolbarShow(false);
            _ui.ComfortShow(true);      // 晕动保护默认开、面板常显，重开后同样保持
            _ui.ResetTasks();
            _ui.ClearLine();
            _ui.Vignette(0f);

            _world.Handbook.SetHeld(false);
            _world.Handbook.SetPage(0);
            _world.Handbook.Interactive = true;
            _world.Handbook.SetTarget(false);
            _world.LightDot.SetCalibrated(false);
            _world.LightDot.SetTarget(false);
            _world.ShowSceneCards(false);
            _world.ApplyRoom(0);
            _world.Nurse.StopPointing();

            _flow = StartCoroutine(RunFlow());
        }

        private void Update()
        {
            if (DesktopInput.GetKeyDown(KeyCode.P)) TogglePause();
            if (DesktopInput.GetKeyDown(KeyCode.R)) Replay();
            if (DesktopInput.GetKeyDown(KeyCode.H))
            {
                _helpOn = !_helpOn;
                _ui.HelpVisible(_helpOn);
                _audio.PlayClick();
            }
            if (DesktopInput.GetKeyDown(KeyCode.V))
            {
                _world.SetVRMode(!_world.VRMode);
                _audio.PlayClick();
                _ui.Toast(_world.VRMode ? "已切换到 VR 模式" : "已切换到桌面模式");
            }
            if (DesktopInput.GetKeyDown(KeyCode.M)) ToggleRoomSwitch();
            if (DesktopInput.GetKeyDown(KeyCode.L)) SkipStep();   // 调试用：只有键盘能触发

            // 转场选房：回车 / 空格 = 确认当前选中的场景卡（鼠标点不到时的兜底）。
            // 自由换房模式下（_roomSwitching）同样生效。
            if ((_step == PrologueStep.Transition || _roomSwitching) &&
                (DesktopInput.GetKeyDown(KeyCode.Return) || DesktopInput.GetKeyDown(KeyCode.Space)))
            {
                Notify(StepEvent.SceneCard);
            }
        }
    }
}
