using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChanFangVR
{
    /// 射线可交互物基类：射线命中 → 高亮；扣扳机 → NotifyActivated()
    public abstract class PrologueInteractable : MonoBehaviour
    {
        public string Label = "";
        public bool Interactive = true;   // 是否处于可交互阶段（由流程控制）
        public bool IsTarget;             // 是否为当前引导目标（呼吸高亮）

        public event Action Activated;

        protected bool _hover;
        protected float _pulse;

        public virtual void SetHover(bool hover)
        {
            if (_hover == hover) return;
            _hover = hover;
            ApplyVisual();
        }

        /// 设为/取消本步的引导目标（呼吸描边）
        public virtual void SetTarget(bool on)
        {
            IsTarget = on;
            if (!on) ApplyVisual();
        }

        public void NotifyActivated()
        {
            if (!Interactive) return;
            var handler = Activated;
            if (handler != null) handler();
        }

        public void ClearActivationHandlers()
        {
            Activated = null;
        }

        /// 显示/隐藏整个物体（含碰撞体）
        public virtual void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        protected abstract void ApplyVisual();

        protected virtual void Update()
        {
            _pulse += Time.unscaledDeltaTime;
            if (IsTarget && Interactive) ApplyPulse();
        }

        protected virtual void ApplyPulse() { }

        protected float Pulse01(float speed)
        {
            return 0.5f + 0.5f * Mathf.Sin(_pulse * speed);
        }

        // —— 通用小工具 ——

        protected static Material TintMat(Material m, Color c)
        {
            if (m == null) return null;
            if (m.HasProperty("_Color")) m.color = c;
            return m;
        }

        /// 让 quad 之类的贴片始终面向相机
        protected static void FaceCamera(Transform t)
        {
            var cam = PrologueRun.Cam;
            if (cam == null || t == null) return;
            Vector3 toCam = cam.transform.position - t.position;
            if (toCam.sqrMagnitude < 0.0001f) return;
            t.rotation = Quaternion.LookRotation(-toCam, Vector3.up);
        }
    }
}
