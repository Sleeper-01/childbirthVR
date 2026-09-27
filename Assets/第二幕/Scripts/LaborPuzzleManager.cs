using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
namespace VRTour
{
    //产程拼图：墙面上有三产程时间轴与打乱的图标卡
    //鼠标抓取图标拖到时间轴槽位：放对吸附并弹出医生讲解，放错弹回原位，不限重试次数
    public class LaborPuzzleManager : MonoBehaviour
    {
        [Header("拼图板锚点（留空自动创建子物体；挪动/缩放它即可整体贴墙）")]
        public Transform boardAnchor;
        [Header("中文字体资产（菜单【产程演示/生成产程拼图】会自动填好）")]
        public TMP_FontAsset chineseFont;
        [Header("吸附判定半径（米，指图标中心到槽位中心的距离）")]
        public float snapRadius = 0.3f;
        [Header("医生讲解文字显示时长（秒）")]
        public float tipDuration = 12f;
        private class StageInfo
        {
            public string slotLabel; //时间轴槽位下的阶段名
            public string keyword;   //图标卡上的关键词
            public string keyPoints; //放对后医生讲解的要点
        }
        //三个产程数据，数组顺序即正确顺序
        private static readonly StageInfo[] Stages =
        {
            new StageInfo
            {
                slotLabel = "第一产程\n宫颈扩张期",
                keyword = "宫缩",
                keyPoints = "【第一产程·宫颈扩张期】：规律宫缩使宫颈管逐渐消失、宫口缓慢扩张至10厘米（开全）。初产妇一般需要11~12小时，此阶段可适当走动休息、及时进食补充体力。"
            },
            new StageInfo
            {
                slotLabel = "第二产程\n胎儿娩出期",
                keyword = "娩出",
                keyPoints = "【第二产程·胎儿娩出期】：宫口开全后，产妇随宫缩屏气用力，胎儿经阴道逐渐娩出。初产妇一般不超过2小时，正确配合用力能加快产程。"
            },
            new StageInfo
            {
                slotLabel = "第三产程\n胎盘娩出期",
                keyword = "胎盘",
                keyPoints = "【第三产程·胎盘娩出期】：胎儿娩出后，子宫继续收缩使胎盘剥离并娩出，一般需要5~15分钟。之后需在产房观察2小时，警惕产后出血。"
            }
        };
        //图标卡平时/吸附后的局部 Z（拖拽悬浮高度见 DraggableIcon.DragZ）
        private const float RestZ = 0.025f;
        private readonly List<Transform> slots = new List<Transform>();       //按正确顺序排列
        private readonly List<DraggableIcon> icons = new List<DraggableIcon>();
        private TextMeshPro title;
        private Camera mainCamera;
        private OpeningIntro openingIntro;
        private UIManager uiManager;
        private DraggableIcon dragging;
        private Plane dragPlane;
        private bool built;
        private int solvedCount;
        private bool congratsFinished;
        //产程拼图是否已全部完成
        public bool IsCompleted
        {
            get { return solvedCount >= Stages.Length; }
        }

        //恭喜语展示完毕：团队相识在此之后才开始
        public bool CongratsFinished
        {
            get { return congratsFinished; }
        }
        void Start()
        {
            mainCamera = Camera.main;
            openingIntro = FindFirstObjectByType<OpeningIntro>();
            uiManager = FindFirstObjectByType<UIManager>();
            if (boardAnchor == null)
            {
                var board = new GameObject("PuzzleBoard");
                board.transform.SetParent(transform, false);
                boardAnchor = board.transform;
            }
            if (chineseFont == null)
            {
                Debug.LogWarning("LaborPuzzleManager.chineseFont 未指定，中文将无法显示。请用菜单【产程演示/生成产程拼图】重新生成，或手动拖入字体资产。", this);
            }
            BuildBoard();
            built = true;
        }
        private bool prevVRTrig;

        void Update()
        {
            if (!built) return;
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;
            if (openingIntro != null && !openingIntro.tourCanStart) return;

            // 统一输入：鼠标 或 VR扳机
            bool vrTrig = ChuJian.Merge.VRPointerVisual.IsAnyTriggerHeld;
            bool vrDown = vrTrig && !prevVRTrig;
            bool vrUp = !vrTrig && prevVRTrig;
            prevVRTrig = vrTrig;

            bool grabPressed = Input.GetMouseButtonDown(0) || vrDown;
            bool released = Input.GetMouseButtonUp(0) || vrUp;
            // 松手帧也算VR上下文，防止松手瞬间射线跳回鼠标位置导致图标位置被重置
            bool usingVR = vrTrig || vrDown || vrUp;

            // 获取射线：VR用手柄射线，桌面用鼠标
            Ray ray;
            if (usingVR)
            {
                var vis = FindObjectOfType<ChuJian.Merge.VRPointerVisual>();
                Vector3 o, d;
                if (vis != null && vis.GetRightRay(out o, out d))
                    ray = new Ray(o, d);
                else
                    ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            }
            else
            {
                ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            }

            if (dragging == null)
            {
                if (grabPressed)
                {
                    if (Physics.Raycast(ray, out RaycastHit hit, 50f))
                    {
                        DraggableIcon icon = hit.collider.GetComponentInParent<DraggableIcon>();
                        if (icon != null && !icon.IsSolved)
                        {
                            dragging = icon;
                            dragging.BeginDrag();
                            dragPlane = new Plane(boardAnchor.forward, boardAnchor.TransformPoint(0f, 0f, DraggableIcon.DragZ));
                            Debug.Log($"[拼图] 抓取: {icon.name} VR={usingVR} ray.origin={ray.origin:F1} board.forward={boardAnchor.forward} planeOK={dragPlane.normal != Vector3.zero}");
                        }
                        else
                        {
                            Debug.Log($"[拼图] 射线命中 {hit.collider.name} 但非可拖图标");
                        }
                    }
                }
            }
            else
            {
                // 松手判定放最前面：松手帧不做投影（保留图标当前位置）
                if (released)
                {
                    ResolveDrop();
                }
                else
                {
                    // 拖动：把射线投影到拼图平面
                    if (dragPlane.Raycast(ray, out float dist))
                    {
                        Vector3 world = ray.GetPoint(dist);
                        dragging.DragTo(boardAnchor.InverseTransformPoint(world));
                    }
                    else
                    {
                        // 兜底：正交投影手位置到板面
                        Vector3 handPos = ray.origin;
                        float planeDist = dragPlane.GetDistanceToPoint(handPos);
                        Vector3 projected = handPos - dragPlane.normal * planeDist;
                        dragging.DragTo(boardAnchor.InverseTransformPoint(projected));
                    }
                }
            }
        }
        //松手后判定：就近槽位是否与图标产程一致
        private void ResolveDrop()
        {
            DraggableIcon icon = dragging;
            dragging = null;
            icon.EndDrag();
            int nearest = -1;
            // VR模式下放置容差放大2.5倍（手柄射线精度不如鼠标，0.3太紧会放不进去）
            float best = ChuJian.Merge.VRUI.Active ? snapRadius * 2.5f : snapRadius;
            for (int i = 0; i < slots.Count; i++)
            {
                //只比较平面距离，忽略拖拽悬浮高度
                Vector3 a = icon.transform.localPosition; a.z = 0f;
                Vector3 b = slots[i].localPosition;       b.z = 0f;
                float d = Vector3.Distance(a, b);
                Debug.Log($"[拼图] 图标({a.x:F2},{a.y:F2}) vs 槽{i}({b.x:F2},{b.y:F2}) 距离={d:F2} 容差={best:F2}");
                if (d < best)
                {
                    best = d;
                    nearest = i;
                }
            }
            if (nearest < 0)
            {
                //没放到任何槽位，弹回原位
                icon.BounceBack();
            }
            else if (nearest == icon.StageIndex)
            {
                //放对：吸附到槽位并触发医生讲解
                Vector3 target = slots[nearest].localPosition;
                target.z = RestZ;
                icon.SnapTo(target);
                solvedCount++;
                if (uiManager != null)
                {
                    uiManager.ShowTip(Stages[icon.StageIndex].keyPoints, tipDuration);
                }
                //最后一块：延时等待讲解播放完，再弹出恭喜
                if (solvedCount >= Stages.Length)
                {
                    StartCoroutine(DelayAllSolved(tipDuration));
                }
            }
            else
            {
                //放错：弹回原位，不限重试
                icon.BounceBack();
            }
        }
        /// <summary>
        /// 等待提示时长结束后，执行拼图完成恭喜
        /// </summary>
        private IEnumerator DelayAllSolved(float waitSeconds)
        {
            yield return new WaitForSeconds(waitSeconds-6f);
            OnAllSolved();
            //恭喜语展示完（tipDuration）后置位：团队相识在此之后才开始
            yield return new WaitForSeconds(tipDuration);
            congratsFinished = true;
        }
        private void OnAllSolved()
        {
            if (title != null)
            {
                title.text = "拼图完成！分娩三产程顺序全部正确";
            }
            if (uiManager != null)
            {
                uiManager.ShowTip("恭喜完成产程拼图！三产程顺序：第一产程宫颈扩张 → 第二产程胎儿娩出 → 第三产程胎盘娩出。", tipDuration);
            }
        }
        //程序化构建墙面拼图：背景板、标题、时间轴、槽位、打乱的图标卡
        private void BuildBoard()
        {
            //z 层级：背板0 / 时间轴0.02 / 槽位0.04 / 槽位标签0.06 / 拼块0.08（拖动0.12），层间留足距离避免透明排序问题
            //配色对齐序幕色板：背板/拼块=Panel、时间轴/槽位=Accent、文字=TextMain
            CreateQuad("Board", new Vector2(2.6f, 1.5f), new Color(0.10f, 0.13f, 0.17f, 0.88f), Vector3.zero, false);
            title = CreateLabel("Title", "把下方图标拖到时间轴的正确位置", new Color(0.96f, 0.97f, 1.00f),
                new Vector3(0f, 0.62f, 0.06f), new Vector2(2.4f, 0.3f));
            //时间轴横条与三个槽位（按正确顺序排列）
            CreateQuad("Timeline", new Vector2(2.3f, 0.05f), new Color(0.31f, 0.76f, 0.97f, 0.8f), new Vector3(0f, 0.28f, 0.02f), false);
            float[] columnX = { -0.78f, 0f, 0.78f };
            for (int i = 0; i < Stages.Length; i++)
            {
                GameObject slot = CreateQuad("Slot_" + i, new Vector2(0.5f, 0.5f), new Color(0.31f, 0.76f, 0.97f, 0.16f),
                    new Vector3(columnX[i], 0.28f, 0.04f), false);
                slots.Add(slot.transform);
                CreateLabel("SlotLabel_" + i, Stages[i].slotLabel, new Color(0.96f, 0.97f, 1.00f, 0.9f),
                    new Vector3(columnX[i], -0.1f, 0.06f), new Vector2(0.62f, 0.3f));
            }
            //图标卡：随机打乱顺序摆在下方
            List<int> order = new List<int> { 0, 1, 2 };
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            for (int row = 0; row < order.Count; row++)
            {
                int stage = order[row];
                Vector3 rest = new Vector3(columnX[row], -0.48f, RestZ);
                //拼块：半透明深色块 + 白色关键词（序幕 Panel 面板） withCollider=true开启拾取碰撞盒
                GameObject card = CreateQuad("Icon_" + Stages[stage].keyword, new Vector2(0.5f, 0.5f),
                    new Color(0.10f, 0.13f, 0.17f, 0.88f), rest, true);
                card.AddComponent<DraggableIcon>().Init(stage, rest);
                icons.Add(card.GetComponent<DraggableIcon>());
                CreateLabel("Keyword", Stages[stage].keyword, new Color(0.96f, 0.97f, 1.00f),
                    new Vector3(0f, 0f, 0.02f), new Vector2(0.5f, 0.3f), card.transform);
            }
        }
        //创建指定世界尺寸的面板：用自定义 mesh 生成几何而非缩放 Quad，scale 恒为 1
        //自动判断Alpha，半透明用Sprites/Default；完全不透明用Unlit/Color写深度
        //👉修复：碰撞体改用BoxCollider，不再用MeshCollider，解决射线拾取失效
        private GameObject CreateQuad(string name, Vector2 size, Color color, Vector3 localPos, bool withCollider)
        {
            GameObject quad = new GameObject(name);
            quad.transform.SetParent(boardAnchor, false);
            quad.transform.localPosition = localPos;
            MeshFilter mf = quad.AddComponent<MeshFilter>();
            Mesh mesh = new Mesh();
            float hw = size.x * 0.5f, hh = size.y * 0.5f;
            mesh.vertices = new[] { new Vector3(-hw, -hh, 0f), new Vector3(hw, -hh, 0f), new Vector3(-hw, hh, 0f), new Vector3(hw, hh, 0f) };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            mesh.triangles = new[] { 0, 1, 2, 2, 1, 3 };
            mesh.RecalculateNormals();
            mf.sharedMesh = mesh;
            MeshRenderer mr = quad.AddComponent<MeshRenderer>();

            bool opaque = color.a >= 0.99f;
            Shader useShader = opaque ? Shader.Find("Unlit/Color") : Shader.Find("Sprites/Default");
            var quadMat = new Material(useShader) { color = color };
            if (!opaque) quadMat.mainTexture = ChuJian.Merge.ActUIStyle.QuadRoundedTexture;   // 统一圆角面板语言
            mr.sharedMaterial = quadMat;

            if (withCollider)
            {
                // ✅ 使用BoxCollider替代MeshCollider，保证射线可以拾取！
                BoxCollider bc = quad.AddComponent<BoxCollider>();
                bc.size = new Vector3(size.x, size.y, 0.02f);
                bc.center = Vector3.zero;
            }
            return quad;
        }
        //创建 3D 文本（字号按文本框自动缩放，居中加粗；字体为空时中文显示不出来）
        private TextMeshPro CreateLabel(string name, string text, Color color, Vector3 localPos, Vector2 size, Transform parent = null)
        {
            GameObject go = new GameObject(name, typeof(TextMeshPro));
            go.transform.SetParent(parent != null ? parent : boardAnchor, false);
            TextMeshPro tmp = go.GetComponent<TextMeshPro>();
            tmp.text = text;
            tmp.font = ChuJian.Merge.ActUIStyle.TmpFont != null ? ChuJian.Merge.ActUIStyle.TmpFont : chineseFont;
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.richText = false;
            //自动缩放字号：文字会撑满文本框高度，无需手工调字号
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 0.1f;
            tmp.fontSizeMax = 100f;
            RectTransform rt = tmp.rectTransform;
            rt.sizeDelta = size;
            rt.localPosition = localPos;
            return tmp;
        }
    }
}
