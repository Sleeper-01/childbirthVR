using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
namespace VRTour
{
    //团队相识：医护成员依次来到床旁自我介绍
    //射线指向成员后点击（扣扳机）完成“床旁致意”，该成员加入“我的医护团队”收藏卡
    public class TeamMeetManager : MonoBehaviour
    {
        [Header("床旁位置锚点（成员围绕它站位；留空自动创建）")]
        public Transform teamAnchor;
        [Header("“我的医护团队”收藏卡锚点（留空自动创建）")]
        public Transform teamBoardAnchor;
        [Header("中文字体资产")]
        public TMP_FontAsset chineseFont;
        [Header("成员模型模板（菜单自动绑医生模型；留空则用占位胶囊）")]
        public GameObject memberTemplate;
        [Header("自我介绍显示时长（秒）")]
        public float tipDuration = 9f;
        [Header("最后一位（麻醉师）讲完后，额外等待多少秒再开启小测")]
        public float extraWaitAfterLastMember = 2.5f;

        private class MemberInfo
        {
            public string role;
            public string name;
            public Color color;
            public string intro;
            public Vector3 localOffset; //相对床旁锚点的站位
        }
        //三位医护成员数据：医生、护士、麻醉师（站位为一横排，都面向床旁）
        //成员标识色对齐序幕色板：医生=Accent、护士=Success、麻醉师=Warn
        private static readonly MemberInfo[] Members =
        {
            new MemberInfo
            {
                role = "产科医生", name = "李静", color = new Color(0.31f, 0.76f, 0.97f),
                intro = "您好，我是产科医生李静，负责您的整个分娩过程。产程中我会持续评估母婴状况，出现任何情况都会第一时间处理，请放心。",
                localOffset = new Vector3(-1.15f, 0f, 1.2f)
            },
            new MemberInfo
            {
                role = "护士", name = "王芳", color = new Color(0.55f, 0.80f, 0.55f),
                intro = "我是护士王芳，将全程陪伴在您身边，观察产程变化，指导呼吸和用力。有任何不适随时告诉我，您一定可以的。",
                localOffset = new Vector3(0f, 0f, 1.2f)
            },
            new MemberInfo
            {
                role = "麻醉师", name = "周涛", color = new Color(1f, 0.72f, 0.30f),
                intro = "您好，我是麻醉师周涛，负责分娩镇痛（无痛分娩）。如果宫缩疼痛难以忍受，我会为您评估并实施镇痛，让您在更舒适的状态下迎接宝宝。",
                localOffset = new Vector3(1.15f, 0f, 1.2f)
            }
        };
        private readonly List<TeamMember> members = new List<TeamMember>();
        private TextMeshPro boardTitle;
        private readonly TextMeshPro[] rowTexts = new TextMeshPro[3];
        private readonly GameObject[] rowDots = new GameObject[3];
        private Camera mainCamera;
        private OpeningIntro openingIntro;
        private UIManager uiManager;
        private LaborPuzzleManager puzzle;
        private TeamMember hovered;
        private bool built;
        private bool teamStarted;
        private bool allMemberDoneTriggered;
        private bool introSequenceDone;

        //全部成员是否已致意完毕
        public bool IsAssembled
        {
            get { return members.TrueForAll(m => m.Greeted); }
        }

        //全部介绍流程结束（含麻醉师介绍展示完、集结提示播完）：随堂小测在此之后才开始
        public bool IntroSequenceDone
        {
            get { return introSequenceDone; }
        }

        void Start()
        {
            mainCamera = Camera.main;
            openingIntro = FindFirstObjectByType<OpeningIntro>();
            uiManager = FindFirstObjectByType<UIManager>();
            puzzle = FindFirstObjectByType<LaborPuzzleManager>();
            if (teamAnchor == null)
            {
                teamAnchor = new GameObject("TeamAnchor").transform;
                teamAnchor.SetParent(transform, false);
            }
            if (teamBoardAnchor == null)
            {
                teamBoardAnchor = new GameObject("TeamBoardAnchor").transform;
                teamBoardAnchor.SetParent(transform, false);
                teamBoardAnchor.localPosition = new Vector3(-2f, 1.5f, 0f);
            }
            if (chineseFont == null)
            {
                Debug.LogWarning("TeamMeetManager.chineseFont 未指定，中文将无法显示。请重新执行【产程演示/生成医护团队与小测】。", this);
            }
            BuildTeamBoard();
            for (int i = 0; i < Members.Length; i++)
            {
                CreateMember(i);
            }
            //依次来到床旁：全部先隐藏，等产程拼图完成后第一位才出现
            for (int i = 0; i < members.Count; i++)
            {
                members[i].gameObject.SetActive(false);
            }
            built = true;
        }

        void Update()
        {
            if (!built) return;
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;
            //团队相识在开场介绍结束且拼图恭喜语展示完毕后才开始
            //（场景里没有产程拼图时，只等开场介绍）
            if (!teamStarted)
            {
                bool introDone = openingIntro == null || openingIntro.tourCanStart;
                //拼图恭喜语展示完毕后，团队相识才开始
                bool puzzleDone = puzzle == null || puzzle.CongratsFinished;
                if (introDone && puzzleDone)
                {
                    teamStarted = true;
                    for (int i = 0; i < members.Count; i++)
                    {
                        if (!members[i].Greeted)
                        {
                            StartCoroutine(PopIn(members[i]));
                            break;
                        }
                    }
                    if (uiManager != null)
                    {
                        uiManager.ShowTip("你的医护团队来到床旁：射线指向 TA 并点击，完成床旁致意。", 7f);
                    }
                }
                return;
            }
            //名字牌始终朝向相机
            foreach (TeamMember m in members)
            {
                if (m.gameObject.activeSelf)
                {
                    m.Billboard(mainCamera);
                }
            }
            //悬停高亮 + 点击致意（桌面鼠标 或 VR手柄射线+扳机）
            TeamMember hitMember = null;
            Ray ray;
            bool vrClick = false;
            var vrVis = FindObjectOfType<ChuJian.Merge.VRPointerVisual>();
            Vector3 vrO, vrD;
            if (vrVis != null && vrVis.GetRightRay(out vrO, out vrD))
            {
                ray = new Ray(vrO, vrD);
                // VR扳机边沿检测
                bool trig = ChuJian.Merge.VRPointerVisual.IsAnyTriggerHeld;
                vrClick = trig && !_prevTrig;
                _prevTrig = trig;
            }
            else
            {
                ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            }

            if (Physics.Raycast(ray, out RaycastHit hit, 30f))
            {
                hitMember = hit.collider.GetComponentInParent<TeamMember>();
            }
            if (hovered != hitMember)
            {
                if (hovered != null) hovered.SetHover(false);
                hovered = hitMember;
                if (hovered != null) hovered.SetHover(true);
            }
            if (hovered != null && (Input.GetMouseButtonDown(0) || vrClick))
            {
                Greet(hovered);
            }
        }

        private bool _prevTrig;

        private void Greet(TeamMember member)
        {
            member.MarkGreeted();
            MemberInfo info = Members[member.Index];
            if (uiManager != null)
            {
                uiManager.ShowTip(info.role + " " + info.name + "：" + info.intro, tipDuration);
            }
            UpdateTeamBoard(member.Index);
            member.PlayBow();

            //下一位成员来到床旁
            for (int i = 0; i < members.Count; i++)
            {
                if (!members[i].Greeted && !members[i].gameObject.activeSelf)
                {
                    StartCoroutine(PopIn(members[i]));
                    break;
                }
            }

            //✅全部成员完成致意：**不再立刻执行，开启延时协程**
            if (members.TrueForAll(m => m.Greeted) && !allMemberDoneTriggered)
            {
                allMemberDoneTriggered = true;
                StartCoroutine(DelayAfterLastMember());
            }
        }

        /// <summary>麻醉师自我介绍播放完整后，再执行全部完成逻辑、解锁小测</summary>
        private IEnumerator DelayAfterLastMember()
        {
            //等待自我介绍文字完整显示时间 + 额外缓冲时间
            yield return new WaitForSeconds(tipDuration + extraWaitAfterLastMember);

            if (boardTitle != null)
            {
                boardTitle.text = "我的医护团队（已集结）";
            }
            if (uiManager != null)
            {
                uiManager.ShowTip("你的医护团队已集结完毕！产科医生、护士、麻醉师都会陪你迎接宝宝的到来。", 10f);
            }
            //集结提示展示完后才置位：随堂小测在此之后才出现（避免与小测开启提示互相覆盖）
            yield return new WaitForSeconds(10f);
            introSequenceDone = true;
        }

        private IEnumerator PopIn(TeamMember member)
        {
            member.gameObject.SetActive(true);
            Transform t = member.transform;
            t.localScale = Vector3.zero;
            float elapsed = 0f;
            while (elapsed < 0.35f)
            {
                elapsed += Time.deltaTime;
                t.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, elapsed / 0.35f);
                yield return null;
            }
            t.localScale = Vector3.one;
        }

        //生成一名成员：模型 + 点击判定盒 + 名字牌
        private void CreateMember(int index)
        {
            MemberInfo info = Members[index];
            GameObject root = new GameObject("Member_" + info.role);
            root.transform.SetParent(teamAnchor, false);
            root.transform.localPosition = info.localOffset;
            //面向床旁中心
            root.transform.rotation = Quaternion.LookRotation(teamAnchor.position - root.transform.position, Vector3.up);
            GameObject model;
            if (memberTemplate != null)
            {
                model = Instantiate(memberTemplate, root.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                //整体着色区分成员（材质各自实例化，不影响原模型）
                foreach (Renderer r in model.GetComponentsInChildren<Renderer>())
                {
                    r.material.color = info.color;
                }
            }
            else
            {
                model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                model.name = "Placeholder";
                model.transform.SetParent(root.transform, false);
                model.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                model.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
                Destroy(model.GetComponent<Collider>());
                MeshRenderer mr = model.GetComponent<MeshRenderer>();
                mr.sharedMaterial = new Material(Shader.Find("Sprites/Default")) { color = info.color };
            }
            //点击判定盒（独立挂载，不随模型缩放变化）
            GameObject hitbox = new GameObject("Hitbox");
            hitbox.transform.SetParent(root.transform, false);
            CapsuleCollider capsule = hitbox.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 0.9f, 0f);
            capsule.height = 1.9f;
            capsule.radius = 0.45f;
            TextMeshPro label = CreateLabel("NameLabel", info.role + " · " + info.name + "\n（点击致意）", new Color(0.96f, 0.97f, 1.00f),
                new Vector3(0f, 2.1f, 0f), new Vector2(1.4f, 0.45f), root.transform); //序幕 TextMain
            TeamMember member = root.AddComponent<TeamMember>();
            member.Init(index, model.transform, label);
            members.Add(member);
        }

        //“我的医护团队”收藏卡面板
        private void BuildTeamBoard()
        {
            //z 层级：背板0 / 圆点0.04 / 文字0.06，层间留足距离避免透明排序问题
            CreateQuad("Board", new Vector2(1.5f, 1.1f), new Color(0.10f, 0.13f, 0.17f, 0.88f), Vector3.zero, teamBoardAnchor); //序幕 Panel
            boardTitle = CreateLabel("Title", "我的医护团队", new Color(0.96f, 0.97f, 1.00f),
                new Vector3(0f, 0.42f, 0.06f), new Vector2(1.3f, 0.2f), teamBoardAnchor); //序幕 TextMain
            float[] rowY = { 0.16f, -0.08f, -0.32f };
            for (int i = 0; i < Members.Length; i++)
            {
                rowDots[i] = CreateQuad("Dot_" + i, new Vector2(0.14f, 0.14f), new Color(0.55f, 0.58f, 0.62f, 0.8f),
                    new Vector3(-0.58f, rowY[i], 0.04f), teamBoardAnchor); //序幕 IdleGray
                rowTexts[i] = CreateLabel("Row_" + i, "—— 虚位以待 ——", new Color(0.55f, 0.58f, 0.62f),
                    new Vector3(0.02f, rowY[i], 0.06f), new Vector2(1.0f, 0.2f), teamBoardAnchor); //序幕 IdleGray
            }
        }

        private void UpdateTeamBoard(int index)
        {
            MemberInfo info = Members[index];
            rowDots[index].GetComponent<MeshRenderer>().material.color = info.color;
            rowTexts[index].text = info.role + " · " + info.name;
            rowTexts[index].color = new Color(0.96f, 0.97f, 1.00f); //序幕 TextMain
        }

        private void ShowTip(string msg, float time)
        {
            if (uiManager != null)
            {
                uiManager.ShowTip(msg, time);
            }
        }

        //创建指定世界尺寸的面板：自定义 mesh 生成几何（scale 恒为 1），半透明用 Sprites/Default
        private GameObject CreateQuad(string name, Vector2 size, Color color, Vector3 localPos, Transform parent)
        {
            GameObject quad = new GameObject(name);
            quad.transform.SetParent(parent, false);
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
            var quadMat = new Material(Shader.Find("Sprites/Default")) { color = color };
            quadMat.mainTexture = ChuJian.Merge.ActUIStyle.QuadRoundedTexture;   // 统一圆角面板语言
            mr.sharedMaterial = quadMat;
            return quad;
        }

        //创建 3D 文本（字号按文本框自动缩放）
        private TextMeshPro CreateLabel(string name, string text, Color color, Vector3 localPos, Vector2 size, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(TextMeshPro));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            TextMeshPro tmp = go.GetComponent<TextMeshPro>();
            tmp.text = text;
            tmp.font = ChuJian.Merge.ActUIStyle.TmpFont != null ? ChuJian.Merge.ActUIStyle.TmpFont : chineseFont;
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.richText = false;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 0.1f;
            tmp.fontSizeMax = 200f;
            RectTransform rt = tmp.rectTransform;
            rt.sizeDelta = size;
            tmp.rectTransform.localPosition = localPos;
            return tmp;
        }
    }

    //医护成员：悬停高亮、鞠躬、名字牌朝向
    public class TeamMember : MonoBehaviour
    {
        public int Index { get; private set; }
        public bool Greeted { get; private set; }
        private Transform modelPivot;
        private TextMeshPro label;
        private string baseName;
        public void Init(int index, Transform pivot, TextMeshPro nameLabel)
        {
            Index = index;
            modelPivot = pivot;
            label = nameLabel;
            baseName = label.text.Split('\n')[0];
        }
        public void SetHover(bool hovering)
        {
            if (label == null) return;
            if (hovering)
            {
                label.color = new Color(1f, 0.72f, 0.30f); //序幕 Warn
            }
            else
            {
                //已致意的成员恢复绿色（序幕 Success），未致意的恢复白色（序幕 TextMain）
                label.color = Greeted ? new Color(0.55f, 0.80f, 0.55f) : new Color(0.96f, 0.97f, 1.00f);
            }
        }
        public void MarkGreeted()
        {
            if (Greeted) return;
            Greeted = true;
            if (label != null)
            {
                label.text = baseName; //去掉“点击致意”提示
                label.color = new Color(0.55f, 0.80f, 0.55f); //序幕 Success
            }
        }
        public void PlayBow()
        {
            StartCoroutine(BowRoutine());
        }
        private IEnumerator BowRoutine()
        {
            if (modelPivot == null) yield break;
            Quaternion start = modelPivot.localRotation;
            Quaternion bowed = start * Quaternion.Euler(22f, 0f, 0f);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 2.5f;
                modelPivot.localRotation = Quaternion.Slerp(start, bowed, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 2.5f;
                modelPivot.localRotation = Quaternion.Slerp(bowed, start, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            modelPivot.localRotation = start;
        }
        public void Billboard(Camera cam)
        {
            if (label == null) return;
            Vector3 dir = label.transform.position - cam.transform.position;
            if (dir.sqrMagnitude > 0.0001f)
            {
                label.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
        }
    }
}
