using UnityEngine;
using TMPro;

namespace VRTour
{
    //监护仪读数动画：点击监护仪时在设备上方弹出跳动的生命体征读数屏（黑底绿字，模拟读数）
    //与设备点击介绍并存：点监护仪同时切机位、显示文字并弹出读数屏
    public class MonitorReadout : MonoBehaviour
    {
        [Header("读数屏相对设备原点的世界偏移（米）")]
        public Vector3 screenWorldOffset = new Vector3(0f, 0.6f, 0f);
        [Header("读数屏显示时长（秒）")]
        public float displayDuration = 8f;
        [Header("数字跳动间隔（秒）")]
        public float refreshInterval = 0.6f;
        [Header("中文字体资产")]
        public TMP_FontAsset chineseFont;

        private GameObject screenRoot;
        private TextMeshPro readoutLabel;
        private Camera mainCamera;
        private OpeningIntro openingIntro;
        private float hideAt;
        private float nextRefresh;
        private int hr = 138;
        private int sys = 120;
        private int dia = 80;
        private int spo2 = 98;

        void Start()
        {
            mainCamera = Camera.main;
            openingIntro = FindFirstObjectByType<OpeningIntro>();
            BuildScreen();
        }

        void Update()
        {
            if (screenRoot == null) return;
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            //点击监护仪弹出读数屏（开场迎接期间不触发）
            if (Input.GetMouseButtonDown(0) && (openingIntro == null || openingIntro.tourCanStart))
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 30f) && hit.transform.IsChildOf(transform))
                {
                    Show();
                }
            }

            if (!screenRoot.activeSelf) return;

            //读数屏始终面向相机
            Vector3 dir = screenRoot.transform.position - mainCamera.transform.position;
            if (dir.sqrMagnitude > 0.0001f)
            {
                screenRoot.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }

            //数字按间隔跳动，到时隐藏
            if (Time.time >= nextRefresh)
            {
                JitterNumbers();
                nextRefresh = Time.time + refreshInterval;
            }
            if (Time.time >= hideAt)
            {
                screenRoot.SetActive(false);
            }
        }

        private void Show()
        {
            screenRoot.SetActive(true);
            hideAt = Time.time + displayDuration;
            JitterNumbers();
            nextRefresh = Time.time + refreshInterval;
        }

        //生命体征在合理区间内随机游走，模拟实时读数
        private void JitterNumbers()
        {
            hr = Mathf.Clamp(hr + Random.Range(-4, 5), 126, 148);
            sys = Mathf.Clamp(sys + Random.Range(-2, 3), 112, 128);
            dia = Mathf.Clamp(dia + Random.Range(-2, 3), 76, 86);
            spo2 = Mathf.Clamp(spo2 + Random.Range(-1, 2), 96, 100);
            if (readoutLabel != null)
            {
                readoutLabel.text = "心率  " + hr + "  次/分\n血压  " + sys + "/" + dia + "  mmHg\n血氧  " + spo2 + "  %";
            }
        }

        //程序化构建读数屏：深色底（自定义 mesh，scale=1）+ 监护仪绿字
        private void BuildScreen()
        {
            //挂在场景根而非设备本身，避免继承模型缩放
            screenRoot = new GameObject("MonitorReadoutScreen");
            screenRoot.transform.position = transform.position + screenWorldOffset;

            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(screenRoot.transform, false);
            MeshFilter mf = panel.AddComponent<MeshFilter>();
            Mesh mesh = new Mesh();
            float hw = 0.55f * 0.5f, hh = 0.34f * 0.5f;
            mesh.vertices = new[] { new Vector3(-hw, -hh, 0f), new Vector3(hw, -hh, 0f), new Vector3(-hw, hh, 0f), new Vector3(hw, hh, 0f) };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            mesh.triangles = new[] { 0, 1, 2, 2, 1, 3 };
            mesh.RecalculateNormals();
            mf.sharedMesh = mesh;
            MeshRenderer mr = panel.AddComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(Shader.Find("Unlit/Color")) { color = new Color(0f, 0.06f, 0.04f) }; //不透明深色底

            GameObject labelGo = new GameObject("Readout", typeof(TextMeshPro));
            //挂在 panel 下（scale 为 1，文本框即世界尺寸）；z 抬高 2cm 避免与面板争排序
            labelGo.transform.SetParent(panel.transform, false);
            TextMeshPro tmp = labelGo.GetComponent<TextMeshPro>();
            tmp.text = "";
            tmp.font = chineseFont;
            tmp.color = new Color(0.45f, 1f, 0.55f); //监护仪绿字
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.richText = false;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 0.1f;
            tmp.fontSizeMax = 200f;
            RectTransform rt = tmp.rectTransform;
            rt.sizeDelta = new Vector2(0.48f, 0.28f);
            rt.localPosition = new Vector3(0f, 0f, 0.02f);

            readoutLabel = tmp;
            screenRoot.SetActive(false);
        }
    }
}
