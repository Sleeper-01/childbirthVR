using System.Collections;
using UnityEngine;
namespace VRTour
{
    //产程图标卡：只承载数据与动画表现，拖放判定由 LaborPuzzleManager 驱动
    public class DraggableIcon : MonoBehaviour
    {
        [Header("卡片整体缩放（小于1缩小，大于1放大）")]
        public float iconScale = 0.6f;

        //拖拽时悬浮在墙面外侧的高度（局部 Z，米）
        internal const float DragZ = 0.03f;
        public int StageIndex { get; private set; }
        public bool IsSolved { get; private set; }
        private Vector3 restLocalPos;
        private MeshRenderer meshRenderer;
        private Color baseColor;

        public void Init(int stageIndex, Vector3 restLocalPos)
        {
            StageIndex = stageIndex;
            this.restLocalPos = restLocalPos;
            meshRenderer = GetComponent<MeshRenderer>();
            baseColor = meshRenderer.material.color;

            //应用面板设置的缩放
            transform.localScale = Vector3.one * iconScale;
        }

        public void BeginDrag()
        {
            Vector3 p = restLocalPos;
            p.z = DragZ;
            transform.localPosition = p;
            SetTint(new Color(1f, 0.72f, 0.30f)); //拖拽高亮：序幕 Warn
        }

        public void DragTo(Vector3 localPos)
        {
            localPos.z = DragZ;
            transform.localPosition = localPos;
        }

        public void EndDrag()
        {
            // 删掉 scale=one，不再覆盖自定义缩放
            SetTint(baseColor);
        }

        //放对：吸附动画并锁定
        public void SnapTo(Vector3 slotLocalPos)
        {
            IsSolved = true;
            SetTint(new Color(0.55f, 0.80f, 0.55f, 0.95f)); //放对保持半透明，改序幕 Success 绿
            StartCoroutine(LerpToLocal(slotLocalPos, 0.15f, null));
        }

        //放错/没放槽位：闪红弹回原位，不限重试次数
        public void BounceBack()
        {
            StartCoroutine(LerpToLocal(restLocalPos, 0.3f, new Color(0.95f, 0.42f, 0.42f))); //序幕 Danger 红
        }

        private IEnumerator LerpToLocal(Vector3 target, float duration, Color? warnColor)
        {
            Vector3 start = transform.localPosition;
            Color from = meshRenderer.material.color;
            Color to = IsSolved ? new Color(0.55f, 0.80f, 0.55f, 0.95f) : baseColor; //序幕 Success
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                transform.localPosition = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
                if (warnColor.HasValue)
                {
                    //弹回时由警示红渐回原色
                    meshRenderer.material.color = Color.Lerp(warnColor.Value, to, t);
                }
                yield return null;
            }
            transform.localPosition = target;
            meshRenderer.material.color = to;
            //删掉强制scale=one，保护自定义缩放
        }

        private void SetTint(Color color)
        {
            meshRenderer.material.color = color;
        }
    }
}
