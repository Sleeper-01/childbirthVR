using 产前运动;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class 温和医疗界面生成器
{
    static TMP_FontAsset font;
    static Sprite rounded;
    // 序幕同源配色：墨绿纸感 → 序幕深色圆角面板 + 浅色正文（与 ChanFangVR.PrologueDefs 同值）
    static readonly Color Ink=new Color32(245,247,255,255), Accent=new Color32(79,194,247,255), Paper=new Color32(26,33,43,235), Soft=new Color32(41,61,87,242);
    [MenuItem("产前运动/应用自动讲解与医疗界面")]
    public static void 应用()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("请先停止运行");
        var c=Object.FindObjectOfType<护士顺序讲解>();if(c==null)throw new System.Exception("请打开主场景");
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/产前运动/字体/中文字体.asset");rounded=圆角();
        c.自动讲解=true;
        c.最短停留=5f;c.最长停留=5f;
        c.分阶段学习=true;c.对白.启用打字机=true;
        var dc=c.对白.GetComponentInParent<Canvas>();dc.transform.localPosition=new Vector3(0,-.35f,1.2f);dc.transform.localRotation=Quaternion.identity;dc.transform.localScale=Vector3.one*.0012f;
        foreach(var t in dc.GetComponentsInChildren<TMP_Text>(true)){t.color=Ink;t.fontSize=Mathf.Max(28,t.fontSize);}
        装饰(c.对白.GetComponent<Image>(),Paper);
        c.对白.speaker.text="护士 · 运动指导";c.对白.speaker.fontSize=32;
        布局(c.对白.speaker.transform,.035f,.82f,.58f,.97f);
        c.对白.body.fontSize=38;布局(c.对白.body.transform,.035f,.27f,.965f,.79f);
        布局(c.对白.pageLabel.transform,.86f,.84f,.965f,.96f);
        c.讲解状态=文字(c.对白.transform,"讲解状态","自动讲解",26,.59f,.84f,.83f,.96f);
        c.讲解状态.alignment=TextAlignmentOptions.Right;c.讲解状态.color=Accent;
        c.提问按钮.gameObject.SetActive(false);c.返回按钮.gameObject.SetActive(false);
        布局(c.对白.next.transform,.76f,.04f,.965f,.22f);按钮样式(c.对白.next,"暂停讲解",true,28);
        布局(c.重看按钮.transform,.035f,.04f,.24f,.22f);按钮样式(c.重看按钮,"重看本项",false,28);
        var oldSettings=c.对白.transform.Find("设置按钮");if(oldSettings!=null)oldSettings.gameObject.SetActive(false);
        var corner=Camera.main.transform.Find("左上角工具栏");
        if(corner==null){var o=new GameObject("左上角工具栏",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));corner=o.transform;corner.SetParent(Camera.main.transform,false);}
        var cornerCanvas=corner.GetComponent<Canvas>();cornerCanvas.renderMode=RenderMode.WorldSpace;cornerCanvas.worldCamera=Camera.main;cornerCanvas.sortingOrder=40;
        corner.localPosition=new Vector3(-1.15f,.65f,1.2f);corner.localRotation=Quaternion.identity;corner.localScale=Vector3.one*.0012f;((RectTransform)corner).sizeDelta=new Vector2(160,65);
        var settingButton=按钮(corner,"设置按钮","设置",0,0,1,1,false,30);
        c.完成确认按钮=按钮(c.对白.transform,"确认完成按钮","确认完成",.39f,.04f,.72f,.22f,true,28);c.完成确认按钮.gameObject.SetActive(false);
        c.提问区域.SetActive(true);布局(c.提问区域.transform,0,-.30f,1,-.025f);装饰(c.提问区域.GetComponent<Image>(),Paper);
        布局(c.输入.transform,.02f,.12f,.82f,.88f);装饰(c.输入.field.GetComponent<Image>(),Soft);
        布局(c.输入.send.transform,.84f,.12f,.98f,.88f);按钮样式(c.输入.send,"发送",true,30);
        c.输入.field.textComponent.color=Ink;c.输入.field.textComponent.fontSize=30;
        var hint=c.输入.field.placeholder as TMP_Text;if(hint!=null){hint.text="有什么想了解的？在这里输入…";hint.fontSize=30;hint.color=new Color(.55f,.58f,.62f);}
        var vc=c.视频.screen.GetComponentInParent<Canvas>();var vr=(RectTransform)vc.transform;
        vr.sizeDelta=new Vector2(1280,1050);vc.transform.position=new Vector3(-.75f,2.12f,2.1f);vc.transform.rotation=Camera.main.transform.rotation;vc.transform.localScale=Vector3.one*.00125f;
        var bg=vc.GetComponent<Image>();if(bg==null)bg=vc.gameObject.AddComponent<Image>();装饰(bg,Paper);
        // 1280 x 720 picture area; remaining space is reserved for title and controls.
        布局(c.视频.screen.transform.parent,0,250f/1050,1,970f/1050);装饰(c.视频.screen.transform.parent.GetComponent<Image>(),new Color(.07f,.09f,.12f));
        布局(c.视频标题.transform,.025f,975f/1050,.975f,.995f);c.视频标题.fontSize=42;c.视频标题.color=Ink;c.视频标题.alignment=TextAlignmentOptions.Left;
        c.视频.placeholder.fontSize=46;c.视频.placeholder.color=Color.white;
        布局(c.上个视频按钮.transform,.025f,.025f,.205f,.12f);按钮样式(c.上个视频按钮,"上一个",false,44);
        布局(c.视频.play.transform,.22f,.025f,.39f,.12f);按钮样式(c.视频.play,"播放",true,44);
        c.视频.暂停按钮=按钮(vc.transform,"暂停视频按钮","暂停",.405f,.025f,.575f,.12f,false,44);
        布局(c.视频.replay.transform,.59f,.025f,.76f,.12f);按钮样式(c.视频.replay,"重播",false,44);
        布局(c.下个视频按钮.transform,.775f,.025f,.975f,.12f);按钮样式(c.下个视频按钮,"下一个",false,44);
        c.视频.进度条=滑块(vc.transform,"视频进度条",.025f,.135f,.735f,.22f);
        var drag=c.视频.进度条.GetComponent<视频进度拖动>();if(drag==null)drag=c.视频.进度条.gameObject.AddComponent<视频进度拖动>();drag.视频=c.视频;
        c.视频.时间文字=文字(vc.transform,"视频时间","00:00 / 00:00",38,.755f,.135f,.975f,.22f);c.视频.时间文字.alignment=TextAlignmentOptions.Right;
        var audio=c.视频.GetComponent<AudioSource>();if(audio==null)audio=c.视频.gameObject.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=0;audio.volume=.5f;c.视频.视频声音=audio;
        c.视频.player.audioOutputMode=UnityEngine.Video.VideoAudioOutputMode.AudioSource;c.视频.player.playOnAwake=false;c.视频.player.aspectRatio=UnityEngine.Video.VideoAspectRatio.FitInside;
        if(Camera.main.GetComponent<AudioListener>()==null)Camera.main.gameObject.AddComponent<AudioListener>();
        var stand=GameObject.Find("视频支架");if(stand!=null){stand.transform.position=new Vector3(-.75f,.72f,2.2f);stand.transform.localScale=new Vector3(.12f,1.44f,.12f);}
        var settings=c.GetComponent<设置面板控制器>();if(settings==null)settings=c.gameObject.AddComponent<设置面板控制器>();
        var st=Camera.main.transform.Find("设置画布");Canvas sc;
        if(st==null){var o=new GameObject("设置画布",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));o.transform.SetParent(Camera.main.transform,false);sc=o.GetComponent<Canvas>();}else sc=st.GetComponent<Canvas>();
        sc.renderMode=RenderMode.WorldSpace;sc.worldCamera=Camera.main;sc.sortingOrder=50;sc.transform.localPosition=new Vector3(0,.1f,1.05f);sc.transform.localRotation=Quaternion.identity;sc.transform.localScale=Vector3.one*.0012f;((RectTransform)sc.transform).sizeDelta=new Vector2(660,360);
        var panel=框(sc.transform,"声音设置面板",0,0,1,1,Paper);
        文字(panel,"设置标题","声音设置",38,.06f,.73f,.7f,.94f);
        var close=按钮(panel,"关闭设置按钮","关闭",.76f,.76f,.95f,.94f,false,28);
        文字(panel,"视频音量标题","视频音量",30,.06f,.47f,.7f,.64f);
        settings.音量文字=文字(panel,"音量百分比","50%",30,.75f,.47f,.94f,.64f);settings.音量文字.alignment=TextAlignmentOptions.Right;
        settings.音量滑块=滑块(panel,"视频音量滑块",.06f,.26f,.94f,.43f);
        文字(panel,"音量说明","0% 静音 · 音量自动保存",24,.06f,.07f,.94f,.2f).color=new Color(.55f,.58f,.62f);
        settings.面板=panel.gameObject;settings.打开按钮=settingButton;settings.关闭按钮=close;settings.视频=c.视频;settings.讲解=c;panel.gameObject.SetActive(false);
        var feedback=c.GetComponent<运动引导反馈>();if(feedback==null)feedback=c.gameObject.AddComponent<运动引导反馈>();c.反馈=feedback;feedback.讲解=c;
        var fp=框(c.视频.screen.transform.parent,"运动引导反馈区",0,0,1,1,Paper);fp.SetAsLastSibling();feedback.面板=fp.gameObject;
        feedback.标题=文字(fp,"引导标题","运动引导",44,.04f,.86f,.96f,.98f);
        feedback.节拍=文字(fp,"节拍提示","准备开始",48,.4f,.64f,.96f,.81f);
        feedback.组数=文字(fp,"组数提示","引导组数",38,.4f,.5f,.96f,.63f);
        feedback.星星=文字(fp,"星级进度","",36,.4f,.37f,.96f,.5f);
        feedback.说明=文字(fp,"反馈说明","",32,.04f,.03f,.96f,.21f);
        feedback.楼层=new Image[5];for(int i=0;i<5;i++)feedback.楼层[i]=框(fp,"电梯第"+(i+1)+"层",.08f,.27f+i*.10f,.30f,.35f+i*.10f,Soft).GetComponent<Image>();
        feedback.意象=(RectTransform)框(fp,"动作意象",.08f,.3f,.3f,.72f,Soft);文字(feedback.意象,"意象名称","呼吸",40,.05f,.3f,.95f,.65f).alignment=TextAlignmentOptions.Center;
        var track=框(fp,"引导进度底",.4f,.28f,.96f,.31f,Soft);feedback.进度=框(track,"引导进度",0,0,1,1,Accent).GetComponent<Image>();feedback.进度.type=Image.Type.Filled;feedback.进度.fillMethod=Image.FillMethod.Horizontal;
        feedback.跳过按钮=按钮(fp,"跳过变式按钮","疲劳，跳过本轮",.4f,.17f,.78f,.27f,false,30);fp.gameObject.SetActive(false);
        EditorUtility.SetDirty(feedback);
        EditorUtility.SetDirty(c);EditorUtility.SetDirty(c.视频);EditorUtility.SetDirty(settings);EditorSceneManager.MarkSceneDirty(c.gameObject.scene);EditorSceneManager.SaveScene(c.gameObject.scene);AssetDatabase.SaveAssets();
    }
    static void 布局(Transform t,float x,float y,float r,float top){var rt=(RectTransform)t;rt.anchorMin=new Vector2(x,y);rt.anchorMax=new Vector2(r,top);rt.offsetMin=rt.offsetMax=Vector2.zero;rt.localScale=Vector3.one;}
    static Transform 框(Transform p,string name,float x,float y,float r,float top,Color color){var t=p.Find(name);if(t==null){t=new GameObject(name,typeof(RectTransform),typeof(Image)).transform;t.SetParent(p,false);}布局(t,x,y,r,top);装饰(t.GetComponent<Image>(),color);return t;}
    static void 装饰(Image im,Color color){im.sprite=rounded;im.type=Image.Type.Sliced;im.color=color;}
    static TMP_Text 文字(Transform p,string name,string text,int size,float x,float y,float r,float top){var t=p.Find(name);if(t==null){t=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI)).transform;t.SetParent(p,false);}布局(t,x,y,r,top);var v=t.GetComponent<TextMeshProUGUI>();v.font=font;v.text=text;v.fontSize=size;v.color=Ink;v.raycastTarget=false;v.richText=false;return v;}
    static Button 按钮(Transform p,string name,string text,float x,float y,float r,float top,bool primary,int size){var t=框(p,name,x,y,r,top,Soft);var b=t.GetComponent<Button>();if(b==null)b=t.gameObject.AddComponent<Button>();if(t.childCount==0)文字(t,"按钮文字",text,size,.02f,.04f,.98f,.96f);按钮样式(b,text,primary,size);return b;}
    static void 按钮样式(Button b,string text,bool primary,int size){装饰(b.GetComponent<Image>(),primary?Accent:Soft);b.targetGraphic=b.GetComponent<Image>();var cb=b.colors;cb.normalColor=Color.white;cb.highlightedColor=new Color(1.3f,1.3f,1.36f);cb.pressedColor=new Color(.8f,.82f,.88f);cb.disabledColor=new Color(1f,1f,1f,.45f);b.colors=cb;var t=b.GetComponentInChildren<TMP_Text>(true);t.text=text;t.font=font;t.fontSize=size;t.color=primary?Color.white:Ink;t.alignment=TextAlignmentOptions.Center;var nav=b.navigation;nav.mode=Navigation.Mode.None;b.navigation=nav;}
    static Slider 滑块(Transform p,string name,float x,float y,float r,float top)
    {
        var t=框(p,name,x,y,r,top,new Color(1,1,1,0));var s=t.GetComponent<Slider>();if(s==null)s=t.gameObject.AddComponent<Slider>();
        框(t,"轨道",0,.40f,1,.6f,Soft);
        var area=框(t,"填充区域",.015f,.40f,.985f,.6f,Color.clear);area.GetComponent<Image>().raycastTarget=false;
        var fill=框(area,"已完成",0,0,1,1,Accent);fill.GetComponent<Image>().raycastTarget=false;
        var handles=框(t,"滑柄区域",.015f,0,.985f,1,Color.clear);handles.GetComponent<Image>().raycastTarget=false;
        var h=框(handles,"滑柄",0,.18f,0,.82f,Accent);((RectTransform)h).sizeDelta=new Vector2(28,0);
        s.fillRect=(RectTransform)fill;s.handleRect=(RectTransform)h;s.targetGraphic=h.GetComponent<Image>();s.minValue=0;s.maxValue=1;s.wholeNumbers=false;s.direction=Slider.Direction.LeftToRight;
        return s;
    }
    static Sprite 圆角()
    {
        const string path="Assets/产前运动/配置/医疗圆角.asset";var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(existing!=null)return existing;
        var tex=new Texture2D(64,64,TextureFormat.RGBA32,false);tex.name="医疗圆角纹理";var pixels=new Color[4096];
        for(int y=0;y<64;y++)for(int x=0;x<64;x++){float dx=Mathf.Max(14-x,0,x-49),dy=Mathf.Max(14-y,0,y-49);pixels[y*64+x]=new Color(1,1,1,Mathf.Clamp01(14.5f-Mathf.Sqrt(dx*dx+dy*dy)));}
        tex.SetPixels(pixels);tex.Apply();var sprite=Sprite.Create(tex,new Rect(0,0,64,64),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(16,16,16,16));sprite.name="医疗圆角";AssetDatabase.CreateAsset(sprite,path);AssetDatabase.AddObjectToAsset(tex,sprite);return sprite;
    }
}


