using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChuJianXinSheng.Epilogue
{
    /// <summary>
    /// 鲜花飘落特效：总结画面关闭后，在铺满相机视野的画布上生成花瓣 UI 图像。
    /// 花瓣自屏幕顶部飘落，Perlin 噪声驱动横向摇摆，绕 Y 轴翻面 + 绕 Z 轴自旋
    /// 制造"翻飞"层次感，顶部渐入、临近底部渐隐；持续 playSeconds 后停止生成，
    /// 已落花瓣自然落完消失。花瓣贴图未指定时用程序化软边椭圆贴图兜底。
    /// </summary>
    public class PetalFallEffect : MonoBehaviour
    {
        [Header("花瓣资源")]
        [SerializeField] private Sprite petalSprite;

        [Header("表现参数")]
        [SerializeField] private float playSeconds = 8f;        // 持续飘落时长
        [SerializeField] private float petalsPerSecond = 8f;
        [SerializeField] private float sizeMin = 1.8f;
        [SerializeField] private float sizeMax = 3.4f;
        [SerializeField] private float fallSpeedMin = 32f;      // 画布单位/秒
        [SerializeField] private float fallSpeedMax = 50f;
        [SerializeField] private float swayWidth = 3.5f;        // 横向摇摆幅度
        [SerializeField] private float spinDegreesMax = 120f;   // 每秒自旋上限
        [SerializeField, Range(0.5f, 0.98f)] private float fadeStartRatio = 0.86f;

        [Header("花瓣配色（随机取色）")]
        [SerializeField] private Color[] petalColors =
        {
            new Color(1.00f, 0.72f, 0.82f),
            new Color(1.00f, 0.55f, 0.68f),
            new Color(1.00f, 0.85f, 0.90f),
            new Color(1.00f, 0.63f, 0.75f),
            new Color(0.98f, 0.80f, 0.90f),
        };

        private RectTransform canvasRect;
        private readonly List<GameObject> petals = new List<GameObject>();
        private Sprite fallbackSprite;

        public bool IsPlaying { get; private set; }
        /// <summary>当前存活花瓣数（供调试/验证读取）。</summary>
        public int ActivePetalCount { get { return petals.Count; } }

        /// <summary>开始播放（重复调用时先清理上一轮）。</summary>
        public void Play()
        {
            StopAllCoroutines();
            ClearPetals();
            StartCoroutine(PlayRoutine());
        }

        /// <summary>停止并清理全部花瓣（演示重置用）。</summary>
        public void StopAndClear()
        {
            StopAllCoroutines();
            ClearPetals();
            IsPlaying = false;
        }

        private IEnumerator PlayRoutine()
        {
            canvasRect = transform as RectTransform;
            IsPlaying = true;

            float elapsed = 0f;
            float spawnAccumulator = 0f;
            while (elapsed < playSeconds)
            {
                float dt = Time.deltaTime;
                elapsed += dt;
                spawnAccumulator += dt * petalsPerSecond;
                while (spawnAccumulator >= 1f)
                {
                    spawnAccumulator -= 1f;
                    SpawnPetal();
                }
                yield return null;
            }
            IsPlaying = false;
        }

        private void SpawnPetal()
        {
            var go = new GameObject("Petal", typeof(Image));
            go.transform.SetParent(transform, false);
            var img = go.GetComponent<Image>();
            img.sprite = ResolveSprite();
            img.raycastTarget = false;
            var c = petalColors != null && petalColors.Length > 0
                ? petalColors[Random.Range(0, petalColors.Length)]
                : new Color(1f, 0.78f, 0.86f);
            img.color = new Color(c.r, c.g, c.b, 0f);   // 初始透明，由落花协程负责渐入

            var rt = (RectTransform)go.transform;
            float size = Random.Range(sizeMin, sizeMax);
            rt.sizeDelta = new Vector2(size, size * Random.Range(1.15f, 1.45f));

            petals.Add(go);
            StartCoroutine(FallRoutine(img, rt));
        }

        private IEnumerator FallRoutine(Image img, RectTransform rt)
        {
            float halfH = canvasRect.rect.height * 0.5f;
            float halfW = canvasRect.rect.width * 0.5f;
            float startX = Random.Range(-halfW, halfW) * 0.96f;
            float startY = halfH + rt.rect.height;
            float endY = -halfH - rt.rect.height;
            float speed = Random.Range(fallSpeedMin, fallSpeedMax);
            float phase = Random.value * 64f;
            float swayFreq = Random.Range(0.7f, 1.3f);
            float spin = Random.Range(-spinDegreesMax, spinDegreesMax);
            float life = 0f;
            Color baseColor = img.color;

            rt.anchoredPosition = new Vector2(startX, startY);

            while (rt.anchoredPosition.y > endY)
            {
                life += Time.deltaTime;
                float y = rt.anchoredPosition.y - speed * Time.deltaTime;

                float sway = (Mathf.PerlinNoise(phase, life * swayFreq) - 0.5f) * 2f;
                float flutter = (Mathf.PerlinNoise(phase + 17f, life * 1.7f) - 0.5f) * 2f;
                rt.anchoredPosition = new Vector2(startX + sway * swayWidth, y);
                rt.localRotation = Quaternion.Euler(0f, flutter * 70f, life * spin);

                // 顶部渐入、临近底部渐隐
                float progress = Mathf.InverseLerp(startY, endY, y);
                float alpha = 1f;
                if (progress < 0.1f) alpha = progress / 0.1f;
                else if (progress > fadeStartRatio) alpha = 1f - Mathf.InverseLerp(fadeStartRatio, 1f, progress);
                img.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Clamp01(alpha));

                yield return null;
            }

            petals.Remove(img.gameObject);
            Destroy(img.gameObject);
        }

        private void ClearPetals()
        {
            for (int i = petals.Count - 1; i >= 0; i--)
                if (petals[i] != null) Destroy(petals[i]);
            petals.Clear();
        }

        private Sprite ResolveSprite()
        {
            if (petalSprite != null) return petalSprite;
            if (fallbackSprite == null) fallbackSprite = BuildFallbackSprite();
            return fallbackSprite;
        }

        /// <summary>程序化兜底花瓣：软边椭圆、底端深粉顶端浅粉（贴图资产未接入时使用）。</summary>
        private static Sprite BuildFallbackSprite()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var cLight = new Color(1f, 0.92f, 0.95f, 1f);
            var cDeep = new Color(1f, 0.58f, 0.74f, 1f);
            float r = size * 0.5f;
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - r + 0.5f) / r;
                    float ny = (y - r + 0.5f) / r;
                    float d = Mathf.Sqrt(nx * nx + ny * ny);
                    float edge = Mathf.Clamp01((1f - d) * 7f);
                    var c = Color.Lerp(cDeep, cLight, Mathf.Clamp01((ny + 1f) * 0.5f));
                    px[y * size + x] = new Color(c.r, c.g, c.b, edge);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
