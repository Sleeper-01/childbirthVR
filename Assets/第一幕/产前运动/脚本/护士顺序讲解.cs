using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace 产前运动
{
    public class 护士顺序讲解 : MonoBehaviour
    {
        public 运动讲解配置 配置;
        public 对话面板 对白;
        public 智能问答服务 问答;
        public 文字输入 输入;
        public 视频控制器 视频;
        public GameObject 提问区域;
        public Button 提问按钮, 返回按钮, 重看按钮;
        public TMP_Text 下一句文字;
        [Header("自动阅读")]
        public bool 自动讲解;
        [Min(1)] public float 每秒字数 = 4;
        public float 最短停留 = 6, 最长停留 = 30, 阅读缓冲 = 2, 恢复延迟 = 3;
        public TMP_Text 讲解状态;
        public bool 分阶段学习;
        public 运动引导反馈 反馈;
        public bool 引导已暂停 => 设置已打开||手动暂停||问答.IsBusy||正在提问||视频.观看中||输入占用;
        public Button 完成确认按钮;
        public bool 等待完成确认 {get; private set;}
        public bool 正在准备 {get; private set;}
        public float 剩余休息秒数 {get; private set;}
        private bool 休息中 => 剩余休息秒数 > 0;
        public bool 设置已打开 { get; set; }
        public bool 手动暂停 { get; private set; }
        public float 剩余阅读时间 { get; private set; }
        private bool 刚才受阻;
        private float 恢复等待;
        private bool 输入占用 => 输入.field.isFocused || !string.IsNullOrWhiteSpace(输入.field.text);
        public Button 上个视频按钮, 下个视频按钮;
        public TMP_Text 视频标题;
        public int 视频序号 { get; private set; }
        public void 上个视频() { 选择视频(视频序号 - 1); }
        public void 下个视频() { 选择视频(视频序号 + 1); }
        public void 选择视频(int 序号)
        {
            if (配置.exercises.Length == 0) return;
            视频序号 = Mathf.Clamp(序号, 0, 配置.exercises.Length - 1);
            视频.Select(配置.exercises[视频序号].videoClip);
            if (视频标题 != null) 视频标题.text = (视频序号 + 1) + " / " + 配置.exercises.Length + "　" + 配置.exercises[视频序号].title;
            if (上个视频按钮 != null) 上个视频按钮.interactable = 视频序号 > 0;
            if (下个视频按钮 != null) 下个视频按钮.interactable = 视频序号 < 配置.exercises.Length - 1;
        }
        // -2 开场，-1 安全提示，0..N-1 运动，N 结束。
        public int 阶段 { get; private set; } = -2;
        public int 段落 { get; private set; }
        public bool 正在提问 { get; private set; }
        private int 讲解页码 = 1, 请求代次;
        private bool 运行中;
        private bool 已结束 => 阶段 >= 配置.exercises.Length;
        private bool 本项末尾 => 阶段 >= 0 && !已结束 && 段落 == 配置.exercises[阶段].passages.Length - 1;
        private string 当前讲解 => 休息中 ? "这一项已确认完成。我们先休息30秒，以不觉疲劳为度，再准备下一项运动。" : 正在准备 ? "我们准备开始"+配置.exercises[阶段].title+"。请保持舒适的半卧位，准备好后跟随引导学习。" : 阶段 == -2 ? 配置.opening : 阶段 == -1 ? 配置.safety[段落] : 已结束 ? "今天的五项卧位运动讲解到这里结束。以不觉疲劳为度，练习前后请补充适量温水。需要时请随时呼叫护士。" : 配置.exercises[阶段].passages[段落];

        private void Start()
        {
            运行中 = true;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            序幕化界面.应用(this);
            对白.next.onClick.AddListener(自动讲解 ? (UnityEngine.Events.UnityAction)切换讲解暂停 : 下一句);
            if (提问按钮 != null) 提问按钮.onClick.AddListener(打开提问);
            返回按钮.onClick.AddListener(返回讲解);
            重看按钮.onClick.AddListener(重看本项);
            输入.Submitted = 发送问题;
            提问区域.SetActive(自动讲解);
            if (上个视频按钮 != null) 上个视频按钮.onClick.AddListener(上个视频);
            if (下个视频按钮 != null) 下个视频按钮.onClick.AddListener(下个视频);
            选择视频(0);
            if(完成确认按钮!=null)完成确认按钮.onClick.AddListener(确认完成);
            显示讲解();
        }
        private void 显示讲解()
        {
            对白.Show("护士", 当前讲解, 讲解页码);
            重置阅读时间();
            更新按钮();
        }
        private void LateUpdate() { if (运行中) { 更新按钮(); if (自动讲解) 自动阅读(); } }
        private void 重置阅读时间()
        {
            对白.body.ForceMeshUpdate();
            int count = 0;
            for (int i=0;i<对白.body.textInfo.characterCount;i++)
                if (对白.body.textInfo.characterInfo[i].pageNumber == 对白.CurrentPage-1) count++;
            剩余阅读时间 = Mathf.Clamp(count / Mathf.Max(1,每秒字数) + 阅读缓冲, 最短停留, Mathf.Max(最短停留,最长停留));
        }
        private void 自动阅读()
        {
            bool blocked = 设置已打开 || 手动暂停 || 问答.IsBusy || 视频.观看中 || 输入占用;
            对白.暂停打字=blocked;
            if (讲解状态 != null) 讲解状态.text = 设置已打开 ? "设置中" : 问答.IsBusy ? "正在回复" : 输入占用 ? "等待输入" : 视频.观看中 ? "视频观看中" : 手动暂停 ? "讲解已暂停" : 已结束 && !正在提问 ? "讲解结束" : 正在提问 ? "护士回复" : "自动讲解";
            if (blocked) { 刚才受阻=true; return; }
            if(分阶段学习&&!正在提问)
            {
                if(等待完成确认){if(讲解状态!=null)讲解状态.text="等待确认完成";return;}
                if(休息中)
                {
                    剩余休息秒数=Mathf.Max(0,剩余休息秒数-Time.unscaledDeltaTime);
                    if(讲解状态!=null)讲解状态.text="休息 "+Mathf.CeilToInt(剩余休息秒数)+" 秒";
                    if(!休息中)进入下一运动();
                    return;
                }
            }
            if (刚才受阻) { 刚才受阻=false; 恢复等待=恢复延迟; }
            if (恢复等待>0) { 恢复等待-=Time.unscaledDeltaTime; return; }
            if (已结束 && !正在提问) return;
            if(对白.正在打字)return;
            if(分阶段学习&&!正在提问&&反馈!=null&&反馈.阻止推进)return;
            剩余阅读时间-=Time.unscaledDeltaTime;
            if (剩余阅读时间>0) return;
            if (正在提问)
            {
                if (对白.Advance()) 重置阅读时间();
                else 返回讲解();
            }
            else 下一句();
        }
        public void 切换讲解暂停()
        {
            if (手动暂停 || 视频.观看中 || 正在提问) { 手动暂停=false; 视频.结束观看(); 返回讲解(); 恢复等待=恢复延迟; }
            else 手动暂停=true;
        }
        private void 更新按钮()
        {
            bool 可翻页 = 对白.CurrentPage < 对白.PageCount;
            if(完成确认按钮!=null)完成确认按钮.gameObject.SetActive(分阶段学习&&等待完成确认&&!正在提问);
            if (自动讲解)
            {
                下一句文字.text = 手动暂停 || 视频.观看中 || 正在提问 ? "继续讲解" : "暂停讲解";
                对白.next.interactable = !已结束 || 正在提问 || 视频.观看中;
                if(提问按钮!=null) 提问按钮.gameObject.SetActive(false);
                返回按钮.gameObject.SetActive(false);
                重看按钮.gameObject.SetActive(阶段>=0 && !正在提问 && !休息中);
                return;
            }
            下一句文字.text = 可翻页 ? "下一页" : 本项末尾 && !正在提问 ? (阶段 == 配置.exercises.Length - 1 ? "结束讲解" : "下一项") : "下一句";
            对白.next.interactable = !问答.IsBusy && (正在提问 ? 可翻页 : !已结束 || 可翻页);
            重看按钮.gameObject.SetActive(!正在提问 && (本项末尾 || 已结束) && !可翻页);
            提问按钮.gameObject.SetActive(!正在提问);
        }
        public void 下一句()
        {
            if(分阶段学习&&!正在提问&&(等待完成确认||休息中))return;
            if (问答.IsBusy) return;
            if (对白.Advance()) { 重置阅读时间(); return; }
            if (正在提问 || 已结束) return;
            if(分阶段学习&&正在准备){正在准备=false;显示讲解();return;}
            if(分阶段学习&&本项末尾){等待完成确认=true;更新按钮();return;}
            讲解页码 = 1;
            if (阶段 == -2) { 阶段 = -1; 段落 = 0; }
            else if (阶段 == -1)
            {
                if (++段落 >= 配置.safety.Length) { 阶段 = 0; 段落 = 0; 正在准备=分阶段学习; 选择视频(0); }
            }
            else if (++段落 >= 配置.exercises[阶段].passages.Length)
            {
                阶段++; 段落 = 0;
                if (!已结束) 选择视频(阶段);
            }
            显示讲解();
        }
        public void 重看本项()
        {
            if (正在提问 || 问答.IsBusy || 阶段 < 0) return;
            if (已结束) { 阶段 = 配置.exercises.Length - 1; 视频.SetInteraction(true); 选择视频(阶段); }
            等待完成确认=false;剩余休息秒数=0;正在准备=分阶段学习;
            if(反馈!=null)反馈.重置本项();
            段落 = 0; 讲解页码 = 1; 显示讲解();
        }
        public void 确认完成()
        {
            if(!等待完成确认||正在提问||问答.IsBusy)return;
            等待完成确认=false;视频.结束观看();
            if(反馈!=null)反馈.计入完成(阶段);
            if(阶段<配置.exercises.Length-1){剩余休息秒数=30;显示讲解();}
            else 进入下一运动();
        }
        private void 进入下一运动()
        {
            阶段++;段落=0;讲解页码=1;正在准备=!已结束;
            if(!已结束)选择视频(阶段);
            显示讲解();
        }
        public void 打开提问()
        {
            if (正在提问) return;
            讲解页码 = 对白.CurrentPage; 正在提问 = true;
            提问区域.SetActive(true); 输入.SetBusy(false);
            对白.Show("护士", "有什么想了解的吗？请在下方输入问题。");
            更新按钮(); 输入.field.ActivateInputField();
            重置阅读时间();
        }
        public void 返回讲解()
        {
            if (!正在提问) return;
            请求代次++; 问答.Cancel(); 输入.SetBusy(false); 输入.field.DeactivateInputField();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            正在提问 = false; 提问区域.SetActive(自动讲解); 显示讲解();
        }
        public void 发送问题(string 问题)
        {
            if (问答.IsBusy || (!自动讲解 && !正在提问)) return;
            if(自动讲解 && !正在提问) { 讲解页码=对白.CurrentPage; 正在提问=true; }
            if (string.IsNullOrWhiteSpace(问题)) { 对白.Show("提示", "请先输入问题。"); 重置阅读时间(); return; }
            int 代次 = ++请求代次;
            输入.SetBusy(true); 对白.Show("护士", "正在回复…");
            问答.Ask(问题, 阶段, (文字, 成功) =>
            {
                if (!运行中 || !正在提问 || 请求代次 != 代次) return;
                输入.SetBusy(false); 对白.Show(成功 ? "护士" : "提示", 文字);
                if (成功) 输入.field.text = "";
                输入.field.DeactivateInputField();
                if(EventSystem.current!=null) EventSystem.current.SetSelectedGameObject(null);
                重置阅读时间();
                更新按钮();
            });
        }
        private void OnDisable()
        {
            运行中 = false; 请求代次++;
            if (问答 != null) 问答.Cancel();
        }
    }
}
