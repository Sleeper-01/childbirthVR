using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace 产前运动
{
    public class 设置面板控制器 : MonoBehaviour
    {
        public GameObject 面板;
        public Button 打开按钮,关闭按钮;
        public Slider 音量滑块;
        public TMP_Text 音量文字;
        public 视频控制器 视频;
        public 护士顺序讲解 讲解;
        private void Start()
        {
            打开按钮.onClick.AddListener(()=>设置打开(true));关闭按钮.onClick.AddListener(()=>设置打开(false));
            音量滑块.SetValueWithoutNotify(PlayerPrefs.GetFloat("产前运动.视频音量",.5f));
            音量滑块.onValueChanged.AddListener(修改音量);修改音量(音量滑块.value);设置打开(false);
        }
        private void 修改音量(float value){视频.设置音量(value);音量文字.text=Mathf.RoundToInt(value*100)+"%";PlayerPrefs.SetFloat("产前运动.视频音量",value);}
        public void 设置打开(bool open){面板.SetActive(open);讲解.设置已打开=open;if(!open)PlayerPrefs.Save();}
        private void OnDisable(){if(讲解!=null)讲解.设置已打开=false;PlayerPrefs.Save();}
    }
}
