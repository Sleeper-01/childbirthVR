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
                if (ignorePause || !_ui.Paused) t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private IEnumerator WaitFor(StepEvent e)
        {
            _received = StepEvent.None;
            _waitingFor = e;
            yield return new WaitUntil(() => _received == e);
            _waitingFor = StepEvent.None;
            _received = StepEvent.None;
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
            else if (_step == PrologueStep.Transition)
            {
                int max = _world.SceneCards != null ? _world.SceneCards.Length - 1 : 0;
                _selectedRoom = Mathf.Clamp(_selectedRoom + dir, 0, max);
                _world.SelectSceneCard(_selectedRoom);
                _audio.PlayBlip();
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
            _ui.ComfortShow(false);
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
            // 转场选房：回车 / 空格 = 确认当前选中的场景卡（鼠标点不到时的兜底）
            if (_step == PrologueStep.Transition &&
                (DesktopInput.GetKeyDown(KeyCode.Return) || DesktopInput.GetKeyDown(KeyCode.Space)))
            {
                Notify(StepEvent.SceneCard);
            }
        }
    }
}
