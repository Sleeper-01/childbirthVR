using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace 产前运动
{
    public class 视频控制器 : MonoBehaviour
    {
        public VideoPlayer player;
        public RawImage screen;
        public TMP_Text placeholder, playLabel;
        public Button play, replay, 暂停按钮;
        public Slider 进度条;
        public TMP_Text 时间文字;
        public AudioSource 视频声音;
        public bool 观看中 { get; private set; }
        public bool 正在拖动 { get; private set; }
        private bool playWhenReady, interactionEnabled=true, seeking, resumeAfterSeek;
        private float operationDeadline;
        private float pendingSeek=-1;
        public float 音量 => 视频声音 != null ? 视频声音.volume : 0;
        public void 设置音量(float value) { if(视频声音!=null) 视频声音.volume=Mathf.Clamp01(value); }
        private void Awake()
        {
            player.playOnAwake=false;
            if(视频声音!=null)
            {
                视频声音.playOnAwake=false; 视频声音.spatialBlend=0;
                player.audioOutputMode=VideoAudioOutputMode.AudioSource;
                设置音量(PlayerPrefs.GetFloat("产前运动.视频音量",.5f));
            }
            else player.audioOutputMode=VideoAudioOutputMode.None;
            player.prepareCompleted+=Prepared;player.errorReceived+=Failed;player.loopPointReached+=Finished;player.seekCompleted+=Seeked;
            play.onClick.AddListener(Toggle);replay.onClick.AddListener(Replay);
            if(暂停按钮!=null) 暂停按钮.onClick.AddListener(暂停);
            if(进度条!=null) 进度条.onValueChanged.AddListener(修改进度);
        }
        public void SetInteraction(bool enabled) { interactionEnabled=enabled;if(!enabled)结束观看();RefreshControls(); }
        public void Select(VideoClip clip)
        {
            player.Stop();playWhenReady=false;seeking=false;pendingSeek=-1;正在拖动=false;观看中=false;player.clip=clip;
            if(视频声音!=null && clip!=null)
            {
                player.controlledAudioTrackCount=(ushort)Mathf.Min((int)clip.audioTrackCount,1);
                if(clip.audioTrackCount>0){player.EnableAudioTrack(0,true);player.SetTargetAudioSource(0,视频声音);}
            }
            screen.enabled=false;placeholder.gameObject.SetActive(true);
            placeholder.text=clip==null?"暂无示范视频\n可先阅读护士讲解":"示范视频已就绪\n点击播放";
            if(进度条!=null)进度条.SetValueWithoutNotify(0);
            RefreshControls();
        }
        public void Toggle()
        {
            if(!interactionEnabled||player.clip==null||seeking)return;
            if(player.isPlaying){if(暂停按钮==null)暂停();return;}
            观看中=true;
            if(player.isPrepared){if(player.time>=player.length-.1)player.time=0;player.Play();}
            else{playWhenReady=true;operationDeadline=Time.realtimeSinceStartup+30;placeholder.text="正在准备视频…";player.Prepare();}
            RefreshControls();
        }
        public void 暂停() { playWhenReady=false;player.Pause();RefreshControls(); }
        public void 结束观看() { 暂停();观看中=false;resumeAfterSeek=false; }
        public void Replay(){if(!interactionEnabled||player.clip==null)return;var clip=player.clip;Select(clip);Toggle();}
        private void Prepared(VideoPlayer source)
        {
            screen.enabled=true;placeholder.gameObject.SetActive(false);
            if(interactionEnabled&&playWhenReady){source.Play();观看中=true;}
            playWhenReady=false;RefreshControls();
        }
        public void 开始拖动()
        {
            if(!可跳转)return;
            pendingSeek=-1;正在拖动=true;观看中=true;resumeAfterSeek=player.isPlaying;player.Pause();
        }
        public void 结束拖动()
        {
            if(!正在拖动)return;
            正在拖动=false;跳转(进度条.value,resumeAfterSeek);
        }
        private bool 可跳转 => interactionEnabled&&player.isPrepared&&player.canSetTime&&player.length>0&&!seeking;
        private void 修改进度(float value){if(!正在拖动 && 可跳转)pendingSeek=value;}
        private void LateUpdate(){if(pendingSeek>=0&&!正在拖动){float value=pendingSeek;pendingSeek=-1;跳转(value,player.isPlaying);}}
        private void 跳转(float value,bool resume)
        {
            if(!可跳转)return;
            观看中=true;resumeAfterSeek=resume;seeking=true;operationDeadline=Time.realtimeSinceStartup+15;
            player.time=System.Math.Min(value*player.length,System.Math.Max(0,player.length-.1));RefreshControls();
        }
        private void Seeked(VideoPlayer source){if(!seeking)return;seeking=false;if(resumeAfterSeek&&interactionEnabled)source.Play();else source.Pause();RefreshControls();}
        private void Failed(VideoPlayer source,string error)
        {
            source.Stop();playWhenReady=false;seeking=false;正在拖动=false;观看中=false;screen.enabled=false;
            placeholder.gameObject.SetActive(true);placeholder.text="视频无法播放，请检查文件格式后重试。";RefreshControls();
        }
        private void Finished(VideoPlayer source){观看中=false;RefreshControls();}
        private void Update()
        {
            if((playWhenReady||seeking)&&Time.realtimeSinceStartup>operationDeadline){Failed(player,"超时");return;}
            if(进度条!=null && !正在拖动&&!seeking)进度条.SetValueWithoutNotify(player.isPrepared&&player.length>0?(float)(player.time/player.length):0);
            if(时间文字!=null){double length=player.clip!=null?player.clip.length:0;double time=正在拖动?进度条.value*length:player.isPrepared?player.time:0;时间文字.text=Format(time)+" / "+Format(length);}
            RefreshControls();
        }
        private static string Format(double seconds){int s=Mathf.Max(0,(int)seconds);return (s/60).ToString("00")+":"+(s%60).ToString("00");}
        private void RefreshControls()
        {
            bool available=interactionEnabled&&player.clip!=null&&!playWhenReady&&!seeking;
            play.interactable=available&&(暂停按钮==null||!player.isPlaying);
            replay.interactable=available;
            playLabel.text=暂停按钮==null&&player.isPlaying?"暂停":"播放";
            if(暂停按钮!=null)暂停按钮.interactable=available&&player.isPlaying;
            if(进度条!=null)进度条.interactable=可跳转||正在拖动;
        }
        private void OnDestroy(){if(player==null)return;player.prepareCompleted-=Prepared;player.errorReceived-=Failed;player.loopPointReached-=Finished;player.seekCompleted-=Seeked;}
    }
}
