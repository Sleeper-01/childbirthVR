using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;
using 产前运动;

public static class 自动交互自检
{
    public static string 结果="未运行";
    [MenuItem("产前运动/运行自动交互自检")]
    public static void Run(){var c=UnityEngine.Object.FindObjectOfType<护士顺序讲解>();if(!Application.isPlaying||c==null)throw new Exception("请运行主场景");结果="运行中";c.StartCoroutine(保护(检查(c)));}
    static IEnumerator 保护(IEnumerator routine){while(true){object step;try{if(!routine.MoveNext())break;step=routine.Current;}catch(Exception e){结果="失败："+e.Message;Debug.LogError(结果);yield break;}yield return step;}}
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static IEnumerator 检查(护士顺序讲解 c)
    {
        Check(c.自动讲解&&c.提问区域.activeSelf&&!c.提问按钮.gameObject.activeSelf,"常驻输入与自动模式");
        var settings=c.GetComponent<设置面板控制器>();var v=c.视频;
        settings.设置打开(true);float before=c.剩余阅读时间;yield return new WaitForSecondsRealtime(.2f);Check(Mathf.Abs(before-c.剩余阅读时间)<.1f,"设置冻结计时");settings.设置打开(false);
        c.输入.field.text="中文测试草稿";before=c.剩余阅读时间;yield return new WaitForSecondsRealtime(.2f);Check(Mathf.Abs(before-c.剩余阅读时间)<.1f,"草稿冻结计时");c.输入.field.text="";c.输入.field.DeactivateInputField();
        var originalCatalog=c.问答.catalog;var clone=UnityEngine.Object.Instantiate(originalCatalog);clone.API密钥="";c.问答.catalog=clone;
        int stage=c.阶段,paragraph=c.段落;c.输入.field.text="测试问题";c.发送问题(c.输入.field.text);Check(c.对白.CurrentText.Contains("尚未配置")&&c.输入.field.text=="测试问题","失败保留输入");
        c.返回讲解();c.输入.field.text="";c.问答.catalog=originalCatalog;UnityEngine.Object.Destroy(clone);Check(c.阶段==stage&&c.段落==paragraph,"问答恢复进度");
        float volume=v.音量;settings.音量滑块.value=0;Check(v.音量==0,"静音");settings.音量滑块.value=.7f;Check(Mathf.Abs(v.音量-.7f)<.01f,"音量设置");settings.音量滑块.value=volume;
        for(int i=0;i<c.配置.exercises.Length;i++)
        {
            c.选择视频(i);v.Toggle();float deadline=Time.realtimeSinceStartup+30;
            while((!v.player.isPlaying||v.player.frame<0)&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(v.player.isPlaying&&v.player.frame>=0,"视频出帧 "+i);Check(v.观看中,"观看暂停讲解");Check(v.player.audioOutputMode==VideoAudioOutputMode.AudioSource,"视频音频输出");
            v.暂停();double time=v.player.time;yield return new WaitForSecondsRealtime(.15f);Check(!v.player.isPlaying&&Math.Abs(v.player.time-time)<.15,"独立暂停按钮");
            v.开始拖动();v.进度条.value=.5f;v.结束拖动();deadline=Time.realtimeSinceStartup+15;
            while((!v.进度条.interactable||Math.Abs(v.player.time-v.player.length*.5)>=1)&&Time.realtimeSinceStartup<deadline)yield return null;
            yield return null;
            Check(!v.player.isPlaying&&Math.Abs(v.player.time-v.player.length*.5)<1,"暂停拖动后仍暂停");
            v.Toggle();v.开始拖动();v.进度条.value=.25f;v.结束拖动();deadline=Time.realtimeSinceStartup+15;
            while((!v.player.isPlaying||Math.Abs(v.player.time-v.player.length*.25)>=2)&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(v.player.isPlaying&&Math.Abs(v.player.time-v.player.length*.25)<2,"播放拖动后继续");
            v.Replay();deadline=Time.realtimeSinceStartup+30;while((!v.player.isPlaying||v.player.frame<0)&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(v.player.isPlaying&&v.player.time<3,"重播归零");
            c.下个视频按钮.onClick.Invoke();Check(!v.player.isPlaying,"换片停止");Check(c.阶段==stage&&c.段落==paragraph,"浏览视频独立于讲解");
        }
        c.选择视频(3);v.Toggle();float ready=Time.realtimeSinceStartup+30;while((!v.player.isPlaying||!v.进度条.interactable)&&Time.realtimeSinceStartup<ready)yield return null;Check(v.player.isPlaying,"结束测试准备");v.开始拖动();v.进度条.value=.9f;v.结束拖动();float end=Time.realtimeSinceStartup+30;while(v.观看中&&Time.realtimeSinceStartup<end)yield return null;Check(!v.观看中,"视频结束解除暂停");
        float oldMin=c.最短停留,oldMax=c.最长停留,oldDelay=c.恢复延迟,oldSpeed=c.对白.打字速度;
        c.返回讲解();c.对白.Show("测试","逐字显示检查");yield return null;int visible=c.对白.body.maxVisibleCharacters;
        yield return new WaitForSecondsRealtime(.15f);Check(c.对白.body.maxVisibleCharacters>visible,"逐字递增");
        c.最短停留=.02f;c.最长停留=.02f;c.恢复延迟=0;c.对白.打字速度=10000;
        for(int i=0;i<5;i++)
        {
            end=Time.realtimeSinceStartup+40;bool preparation=false;
            while(!c.等待完成确认&&Time.realtimeSinceStartup<end)
            {
                if(c.阶段==i&&c.正在准备)preparation=true;
                if(c.反馈.阻止推进&&c.反馈.跳过按钮.gameObject.activeInHierarchy)c.反馈.跳过按钮.onClick.Invoke();
                yield return null;
            }
            Check(c.阶段==i&&c.等待完成确认,"逐项停留 "+i);Check(preparation,"准备开始 "+i);
            yield return new WaitForSecondsRealtime(.25f);Check(c.阶段==i&&c.等待完成确认,"不自动越过确认 "+i);
            c.打开提问();yield return null;c.返回讲解();yield return null;Check(c.等待完成确认,"问答保留确认状态");
            c.完成确认按钮.onClick.Invoke();c.确认完成();Check(c.反馈.学习安心值==i+1,"确认仅计一次");
            if(i<4)
            {
                Check(c.剩余休息秒数>29,"30秒休息");c.设置已打开=true;float rest=c.剩余休息秒数;
                yield return new WaitForSecondsRealtime(.2f);Check(Mathf.Abs(rest-c.剩余休息秒数)<.1f,"设置暂停休息");c.设置已打开=false;
                end=Time.realtimeSinceStartup+35;while(c.阶段==i&&Time.realtimeSinceStartup<end)yield return null;
                Check(c.阶段==i+1&&c.正在准备&&c.视频序号==i+1,"休息后片源和准备同步");
            }
        }
        Check(c.阶段==5,"全部确认后结束");
        c.最短停留=oldMin;c.最长停留=oldMax;c.恢复延迟=oldDelay;c.对白.打字速度=oldSpeed;        结果="通过：逐字对白、五项准备/确认/30秒休息、自动阅读、设置与草稿暂停、失败恢复、五个视频播放/暂停/拖动/重播/切换、结束恢复、音量";Debug.Log(结果);
    }
}


