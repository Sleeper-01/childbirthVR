using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace 产前运动
{
    public class 对话面板 : MonoBehaviour
    {
        public TMP_Text speaker, body, pageLabel;
        public Button next;
        public bool 启用打字机;
        [Min(1)] public float 打字速度 = 18;
        public bool 暂停打字 { get; set; }
        public bool 正在打字 => 启用打字机 && body.maxVisibleCharacters < 页末;
        private int 页末;
        private float 打字累计;
        private int page = 1;
        public int PageCount => Mathf.Max(1, body.textInfo.pageCount);
        public int CurrentPage => page;
        public string CurrentText => body.text;
        public void Show(string name, string content, int restorePage = 1)
        {
            speaker.text = name;
            body.richText = false;
            body.overflowMode = TextOverflowModes.Page;
            body.text = content ?? "";
            body.ForceMeshUpdate();
            page = Mathf.Clamp(restorePage, 1, PageCount);
            RenderPage();
            开始本页();
        }
        public bool Advance()
        {
            if(正在打字){完成打字();return true;}
            body.ForceMeshUpdate();
            if (page >= PageCount) return false;
            page++; RenderPage(); 开始本页(); return true;
        }
        public void 完成打字(){body.maxVisibleCharacters=页末;}
        private void 开始本页()
        {
            body.ForceMeshUpdate();
            var info=body.textInfo.pageInfo[page-1];
            页末=Mathf.Min(body.textInfo.characterCount,info.lastCharacterIndex+1);
            body.maxVisibleCharacters=启用打字机?Mathf.Max(0,info.firstCharacterIndex):int.MaxValue;
            打字累计=0;
        }
        private void RenderPage()
        {
            body.pageToDisplay = page;
            pageLabel.text = page + " / " + PageCount;
        }
        private void LateUpdate()
        {
            page = Mathf.Clamp(page, 1, PageCount);
            RenderPage();
            if(正在打字&&!暂停打字)
            {
                打字累计+=Time.unscaledDeltaTime*Mathf.Max(1,打字速度);
                int count=Mathf.FloorToInt(打字累计);
                if(count>0){body.maxVisibleCharacters=Mathf.Min(页末,body.maxVisibleCharacters+count);打字累计-=count;}
            }
        }
    }
}
