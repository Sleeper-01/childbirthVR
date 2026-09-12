using System.Collections.Generic;
using UnityEngine;

namespace ChanFangVR
{
    /// 提示音全部程序化合成（无需外部音频文件）；
    /// 台词语音若在 Resources/Voice/<key> 存在则播放，否则以字幕代替。
    public class PrologueAudio : MonoBehaviour
    {
        private AudioSource _sfx;
        private AudioSource _voice;

        private AudioClip _chime;
        private AudioClip _success;
        private AudioClip _click;
        private AudioClip _blip;
        private AudioClip _whoosh;
        private AudioClip _page;

        private readonly Dictionary<string, AudioClip> _voiceCache = new Dictionary<string, AudioClip>();
        private bool _built;

        public static PrologueAudio Create(Transform parent)
        {
            var go = new GameObject("PrologueAudio");
            if (parent != null) go.transform.SetParent(parent, false);
            var audio = go.AddComponent<PrologueAudio>();
            audio.Build();
            return audio;
        }

        private void Build()
        {
            if (_built) return;
            _built = true;

            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
            _sfx.spatialBlend = 0f;

            _voice = gameObject.AddComponent<AudioSource>();
            _voice.playOnAwake = false;
            _voice.spatialBlend = 0f;
            _voice.volume = 1f;

            _chime = ClipTone("chime", new[] { 523.25f, 659.25f, 783.99f }, 0.55f);
            _success = ClipTone("success", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 1.0f);
            _click = ClipTone("click", new[] { 880f }, 0.08f);
            _blip = ClipTone("blip", new[] { 660f }, 0.05f);
            _whoosh = ClipNoise("whoosh", 0.7f, true);
            _page = ClipNoise("page", 0.16f, false);
        }

        public void PlayChime() { Play(_chime, 0.9f); }
        public void PlaySuccess() { Play(_success, 0.95f); }
        public void PlayClick() { Play(_click, 0.5f); }
        public void PlayBlip() { Play(_blip, 0.18f); }
        public void PlayWhoosh() { Play(_whoosh, 0.8f); }
        public void PlayPage() { Play(_page, 0.6f); }

        private void Play(AudioClip clip, float volume)
        {
            if (clip == null || _sfx == null) return;
            _sfx.PlayOneShot(clip, volume);
        }

        /// 播放台词配音，返回配音时长（秒）。
        /// 返回 0 表示没有对应语音包，调用方应改用字幕估算时长。
        public float Speak(string key)
        {
            if (string.IsNullOrEmpty(key) || _voice == null) return 0f;
            AudioClip clip;
            if (!_voiceCache.TryGetValue(key, out clip))
            {
                clip = Resources.Load<AudioClip>("Voice/" + key);
                _voiceCache[key] = clip;
            }
            if (clip == null) return 0f;
            _voice.Stop();
            _voice.clip = clip;
            _voice.Play();
            return clip.length;
        }

        public void StopVoice()
        {
            if (_voice != null && _voice.isPlaying) _voice.Stop();
        }

        // —— 合成 ——

        private static AudioClip ClipTone(string name, float[] freqs, float dur)
        {
            int rate = 44100;
            int n = Mathf.CeilToInt(dur * rate);
            float[] data = new float[n];
            int seg = Mathf.Max(1, n / freqs.Length);
            for (int i = 0; i < n; i++)
            {
                int segIdx = Mathf.Min(freqs.Length - 1, i / seg);
                float t = (float)i / rate;
                float env = Mathf.Clamp01(i / (rate * 0.012f));
                float decay = Mathf.Exp(-2.6f * t);
                float v = Mathf.Sin(2f * Mathf.PI * freqs[segIdx] * t) * 0.6f;
                v += Mathf.Sin(2f * Mathf.PI * freqs[segIdx] * 2f * t) * 0.12f;
                data[i] = v * env * decay;
            }
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip ClipNoise(string name, float dur, bool sweep)
        {
            int rate = 22050;
            int n = Mathf.CeilToInt(dur * rate);
            float[] data = new float[n];
            var rng = new System.Random(12345);
            float last = 0f;
            for (int i = 0; i < n; i++)
            {
                float k = (float)i / n;
                float env = Mathf.Sin(k * Mathf.PI);
                float lp = sweep ? (0.12f + 0.8f * k) : 0.5f;
                last = Mathf.Lerp(last, (float)(rng.NextDouble() * 2.0 - 1.0), lp);
                data[i] = last * env * 0.8f;
            }
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
