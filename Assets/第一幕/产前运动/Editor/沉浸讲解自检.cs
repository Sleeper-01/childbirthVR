using System;
using System.Collections.Generic;
using 产前运动;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class 沉浸讲解自检
{
    [MenuItem("产前运动/运行三维讲解自检")]
    public static void Run()
    {
        var c=UnityEngine.Object.FindObjectOfType<护士顺序讲解>();
        if(c!=null && c.自动讲解){自动交互自检.Run();return;}
        Check(Application.isPlaying && c!=null,"请先打开主场景并开始运行");
        Check(c.阶段==-2 && c.对白.CurrentText==c.配置.opening,"开场原文");
        Check(!c.提问区域.activeSelf && !c.问答.IsBusy,"开场没有提问或联网");
        foreach(var canvas in UnityEngine.Object.FindObjectsOfType<Canvas>())Check(canvas.renderMode==RenderMode.WorldSpace,"不存在屏幕空间布局");
        Check(c.对白.transform.IsChildOf(Camera.main.transform),"对白跟随玩家视角");
        var videoCanvas=c.视频.screen.GetComponentInParent<Canvas>();
        Check(!videoCanvas.transform.IsChildOf(Camera.main.transform),"视频独立于玩家视角");
        Check(c.视频.player.gameObject.name=="视频播放器","独立原生播放器");
        Check(UnityEngine.Object.FindObjectOfType<历史第一人称控制器>()==null && UnityEngine.Object.FindObjectOfType<历史接近触发器>()==null,"无移动或接近触发");
        Check(Camera.main.GetComponent<Camera>().orthographic==false,"透视摄像机");
        Check(Quaternion.Angle(videoCanvas.transform.rotation,Camera.main.transform.rotation)<.1f,"视频屏正向平行玩家视角");
        Check(c.视频序号==0&&!c.上个视频按钮.interactable,"首个视频边界");
        for(int i=1;i<c.配置.exercises.Length;i++){c.下个视频按钮.onClick.Invoke();Check(c.视频序号==i&&c.视频.player.clip==c.配置.exercises[i].videoClip,"下一个视频片源");}
        Check(!c.下个视频按钮.interactable,"最后视频边界");
        for(int i=c.配置.exercises.Length-2;i>=0;i--){c.上个视频按钮.onClick.Invoke();Check(c.视频序号==i,"上一个视频");}
        Check(c.阶段==-2&&c.段落==0&&c.对白.CurrentText==c.配置.opening,"视频切换独立于对白");
        c.提问按钮.onClick.Invoke();Check(c.正在提问 && c.提问区域.activeSelf,"展开提问");
        c.输入.field.text="";c.输入.send.onClick.Invoke();Check(c.对白.CurrentText.Contains("请先输入"),"空输入");
        if(!c.问答.IsConfigured){c.输入.field.text="怎样开始练习？";c.输入.send.onClick.Invoke();Check(c.对白.CurrentText.Contains("尚未配置") && c.输入.field.text.Length>0,"缺失密钥提示与保留输入");}
        c.对白.Show("护士",new string('测',1600));Check(c.对白.PageCount>1,"长文分页");while(c.对白.Advance()){}Check(c.对白.CurrentPage==c.对白.PageCount,"末页可达");
        c.返回按钮.onClick.Invoke();Check(!c.正在提问 && c.阶段==-2 && c.对白.CurrentText==c.配置.opening,"恢复开场讲解");
        var seen=new HashSet<string>();int ticks=0;
        while(c.阶段<c.配置.exercises.Length && ticks++<300)
        {
            var expected=c.阶段==-2?c.配置.opening:c.阶段==-1?c.配置.safety[c.段落]:c.配置.exercises[c.阶段].passages[c.段落];
            Check(c.对白.CurrentText==expected,"固定讲解顺序");seen.Add(c.阶段+":"+c.段落);
            if(c.阶段==0 && c.段落==2 && c.对白.CurrentPage==1)
            { c.打开提问();c.返回讲解();Check(c.阶段==0&&c.段落==2&&c.对白.CurrentText==expected,"问答不改变讲解进度"); }
            c.下一句();
        }
        Check(ticks<300,"流程可以结束");
        int expectedCount=1+c.配置.safety.Length;foreach(var item in c.配置.exercises)expectedCount+=item.passages.Length;
        Check(seen.Count==expectedCount,"所有讲解段落均可到达");
        c.重看本项();Check(c.阶段==4 && c.段落==0,"结束后重看最后一项");
        Check(c.视频.player.audioOutputMode==UnityEngine.Video.VideoAudioOutputMode.None && !c.视频.player.playOnAwake,"视频静音手动播放");
        Debug.Log("三维讲解自检通过：结构、开场、提问、分页、全部"+expectedCount+"段讲解、进度恢复与视频默认值。");
    }
    public static void CheckLayout()
    {
        var c=UnityEngine.Object.FindObjectOfType<护士顺序讲解>();
        Check(c.正在提问,"先打开提问区域，等待一帧让TMP输入裁剪和光标完成布局，再检查");
        Canvas.ForceUpdateCanvases();
        foreach(var t in new[]{c.对白.transform,c.提问区域.transform,c.视频.play.transform,c.视频.replay.transform,c.上个视频按钮.transform,c.下个视频按钮.transform})
        {
            var corners=new Vector3[4];((RectTransform)t).GetWorldCorners(corners);
            foreach(var point in corners){var v=Camera.main.WorldToViewportPoint(point);Check(v.z>0&&v.x>=0&&v.x<=1&&v.y>=0&&v.y<=1,"可见区域 "+t.name);}
        }
        CheckUI(c.输入.transform);CheckUI(c.输入.send.transform);CheckUI(c.返回按钮.transform);
        CheckUI(c.上个视频按钮.transform);CheckUI(c.下个视频按钮.transform);
        Debug.Log("布局与点击检测通过："+Screen.width+"×"+Screen.height);
    }
    public static void CheckClosedLayout()
    {
        var c=UnityEngine.Object.FindObjectOfType<护士顺序讲解>();
        Check(!c.正在提问,"先返回讲解并等待一帧完成按钮布局");
        CheckUI(c.对白.next.transform);CheckUI(c.提问按钮.transform);
    }
    static void CheckUI(Transform target)
    {
        Canvas.ForceUpdateCanvases();var hits=new List<RaycastResult>();
        var data=new PointerEventData(EventSystem.current){position=Camera.main.WorldToScreenPoint(target.position)};
        EventSystem.current.RaycastAll(data,hits);
        Check(hits.Count>0&&(hits[0].gameObject.transform==target||hits[0].gameObject.transform.IsChildOf(target)),"鼠标点击命中 "+target.name);
    }
    static void Check(bool value,string message){if(!value)throw new InvalidOperationException("检查失败："+message);}
}
