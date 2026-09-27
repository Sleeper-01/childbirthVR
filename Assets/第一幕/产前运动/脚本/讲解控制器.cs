using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace 产前运动
{
    public class 讲解控制器 : MonoBehaviour
    {
        public 运动讲解配置 catalog;
        public 对话面板 dialogue;
        public 文字输入 input;
        public 智能问答服务 service;
        public 视频控制器 video;
        public TMP_Text heading, status;
        public Button[] exerciseButtons;
        public Button resume, restart, safety;
        public bool openOnStart = true;
        public CanvasGroup interactionPanel;
        public bool IsInteractionOpen { get; private set; }
        private bool hasOpened;
        private int requestVersion;
        private int exercise = -1, passage, savedPage = 1, safetyIndex;
        private enum View { Lesson, Reply, Safety }
        private View view;
        public int CurrentExercise => exercise;
        public int CurrentPassage => passage;
        private string FixedText => exercise < 0 ? catalog.opening : catalog.exercises[exercise].passages[passage];

        private void Start()
        {
            for (int i = 0; i < exerciseButtons.Length; i++) { int index = i; exerciseButtons[i].onClick.AddListener(() => SelectExercise(index)); }
            dialogue.next.onClick.AddListener(Next);
            resume.onClick.AddListener(Resume);
            restart.onClick.AddListener(Restart);
            safety.onClick.AddListener(ShowSafety);
            input.Submitted = Ask;
            video.Select(null);
            if (openOnStart) OpenInteraction();
            else { SetVisible(false); video.SetInteraction(false); }
        }
        public void OpenInteraction()
        {
            IsInteractionOpen = true;
            SetVisible(true);
            video.SetInteraction(true);
            if (!hasOpened) { ShowLesson(); hasOpened = true; }
            else dialogue.Show(dialogue.speaker.text, dialogue.CurrentText, dialogue.CurrentPage);
        }
        public void CloseInteraction()
        {
            requestVersion++;
            bool pending = service.IsBusy;
            service.Cancel();
            SetBusy(false);
            if (pending) { dialogue.Show("提示", "已取消本次请求，问题已保留。下次可重新发送，或继续讲解。"); status.text = "请求已取消"; }
            input.field.DeactivateInputField();
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            video.SetInteraction(false);
            IsInteractionOpen = false;
            SetVisible(false);
        }
        private void SetVisible(bool visible)
        {
            if (interactionPanel == null) return;
            interactionPanel.alpha = visible ? 1 : 0;
            interactionPanel.interactable = interactionPanel.blocksRaycasts = visible;
        }
        public void SelectExercise(int index)
        {
            if (!IsInteractionOpen || service.IsBusy || index < 0 || index >= catalog.exercises.Length) return;
            exercise = index; passage = 0; savedPage = 1;
            video.Select(catalog.exercises[index].videoClip); ShowLesson();
        }
        private void RememberPage() { if (view == View.Lesson) savedPage = dialogue.CurrentPage; }
        public void Resume() { if (IsInteractionOpen && !service.IsBusy) ShowLesson(savedPage); }
        public void Restart() { if (!IsInteractionOpen || service.IsBusy) return; passage = 0; savedPage = 1; ShowLesson(); }
        private void ShowLesson(int page = 1)
        {
            view = View.Lesson;
            heading.text = exercise < 0 ? "从一次温和的练习开始" : catalog.exercises[exercise].title;
            dialogue.Show("护士 · 运动讲解", FixedText, page);
            status.text = exercise < 0 ? "请选择运动，或先了解安全提示" : "讲解 " + (passage + 1) + " / " + catalog.exercises[exercise].passages.Length;
        }
        public void Next()
        {
            if (!IsInteractionOpen || service.IsBusy || dialogue.Advance()) return;
            if (view == View.Safety)
            {
                if (++safetyIndex < catalog.safety.Length) dialogue.Show("护士 · 安全提示", catalog.safety[safetyIndex]);
                else Resume();
                return;
            }
            if (view != View.Lesson) { status.text = "回复已读完，可继续提问或返回讲解"; return; }
            if (exercise < 0) { SelectExercise(0); return; }
            if (passage + 1 < catalog.exercises[exercise].passages.Length) { passage++; savedPage = 1; ShowLesson(); }
            else status.text = "本项讲解结束，请选择其他运动或重看本项";
        }
        public void ShowSafety()
        {
            if (!IsInteractionOpen || service.IsBusy) return;
            RememberPage(); view = View.Safety; safetyIndex = 0;
            dialogue.Show("护士 · 安全提示", catalog.safety[0]); status.text = "点击下一句继续查看，或点击继续讲解返回";
        }
        public void Ask(string question)
        {
            if (!IsInteractionOpen || service.IsBusy) return;
            if (string.IsNullOrWhiteSpace(question)) { status.text = "请先输入问题"; return; }
            RememberPage(); view = View.Reply;
            SetBusy(true); dialogue.Show("护士", "正在回复…"); status.text = "正在回复";
            int version = ++requestVersion;
            service.Ask(question, exercise, (reply, success) =>
            {
                if (this == null || !IsInteractionOpen || version != requestVersion) return;
                SetBusy(false); dialogue.Show(success ? "护士 · 问答" : "提示", reply);
                status.text = success ? "可继续提问，或点击继续讲解" : "输入已保留 · 固定讲解仍可使用";
                if (success) input.field.text = "";
            });
        }
        private void SetBusy(bool busy)
        {
            input.SetBusy(busy);
            foreach (var button in exerciseButtons) button.interactable = !busy;
            resume.interactable = restart.interactable = safety.interactable = dialogue.next.interactable = !busy;
        }
    }
}
