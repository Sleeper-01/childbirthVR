using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace 产前运动
{
    public class 运动引导反馈 : MonoBehaviour
    {
        public 护士顺序讲解 讲解;
        public GameObject 面板;
        public TMP_Text 标题,节拍,组数,星星,说明;
        public RectTransform 意象;
        public Image[] 楼层;
        public Image 进度;
        public Button 跳过按钮;
        public float 演示半拍秒数=3;
        public bool 阻止推进 => 讲解.阶段==0&&(讲解.段落==2||讲解.段落==3)&&!完成&&!讲解.正在准备;
        public int 学习安心值 {get;private set;}
        public bool 完成 {get;private set;}
        private readonly bool[] 已计入=new bool[5];
        private int 上项=-9,上段=-9;
        private float elapsed;
        private void Start(){跳过按钮.onClick.AddListener(()=>{完成=true;说明.text="已跳过本轮，可按自己的状态休息。";});面板.SetActive(false);}
        public void 计入完成(int index){if(index>=0&&index<已计入.Length&&!已计入[index]){已计入[index]=true;学习安心值++;}}
        public void 重置本项(){上项=-9;上段=-9;}
        private void Update()
        {
            int item=讲解.阶段,step=讲解.段落;
            bool show=item>=0&&item<5&&!讲解.正在准备&&!讲解.正在提问&&!讲解.视频.观看中&&讲解.视频序号==item&&讲解.剩余休息秒数<=0&&step>=1;
            面板.SetActive(show);if(!show)return;
            if(item!=上项||step!=上段){上项=item;上段=step;elapsed=0;完成=false;}
            if(!讲解.引导已暂停&&!讲解.对白.正在打字&&!完成&&!讲解.等待完成确认)elapsed+=Time.unscaledDeltaTime;
            bool training=item==0&&(step==2||step==3);
            float half=training?(step==2?3:5):Mathf.Max(1,演示半拍秒数);
            int target=training?(step==2?8:4):0;
            float phase=elapsed%(half*2),ratio=phase<half?phase/half:1-(phase-half)/half;
            int count=Mathf.FloorToInt(elapsed/(half*2));
            if(training&&count>=target){完成=true;count=target;ratio=0;}
            标题.text=讲解.配置.exercises[item].title+" · 引导反馈";
            string[] up={"电梯上行 · 收缩","勾脚尖","缓慢屈伸 · 握拳","轻轻收腹压床","吸气"};
            string[] down={"电梯下行 · 放松","绷脚尖","放开 · 放松","放松","呼气"};
            节拍.text=完成?"本轮节拍结束":phase<half?up[item]:down[item];
            组数.text=training?"引导组数 "+count+" / "+target:"动作意象演示";
            星星.text=training?new string('★',count)+new string('☆',target-count):"学习安心值 "+学习安心值;
            说明.text="全程自然呼吸，不要憋气\n跟随节奏，感觉疲劳可暂停休息";
            if(item==2)说明.text="小幅而舒缓，可仅做健侧\n输液侧手臂不参与伸展";
            if(讲解.等待完成确认){节拍.text="本项学习结束";组数.text="请在下方点击“确认完成”";}
            跳过按钮.gameObject.SetActive(training&&!完成);
            进度.fillAmount=training?Mathf.Clamp01(elapsed/(target*half*2)):ratio;
            for(int i=0;i<楼层.Length;i++){楼层[i].gameObject.SetActive(item==0);楼层[i].color=ratio>=(i+1f)/楼层.Length?new Color(.16f,.55f,.48f):new Color(.82f,.91f,.88f);}
            意象.gameObject.SetActive(item!=0);
            意象.localRotation=Quaternion.identity;意象.localScale=Vector3.one;
            if(item==1)意象.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(-25,25,ratio));
            else if(item==2)意象.localScale=Vector3.one*Mathf.Lerp(.7f,1,ratio);
            else if(item==3)意象.localScale=new Vector3(1,Mathf.Lerp(.5f,1,ratio),1);
            else if(item==4)意象.localScale=Vector3.one*Mathf.Lerp(.55f,1,ratio);
            var label=意象.GetComponentInChildren<TMP_Text>();if(label!=null)label.text=item==1?"脚尖":item==2?"握 / 放":item==3?"腹部":"呼吸";
        }
    }
}

