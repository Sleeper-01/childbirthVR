using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace VRTour
{
    public class CameraTour : MonoBehaviour
    {
        [Header("预置机位列表（方向键用）")]
        public List<Transform> cameraPoints;
        [Header("机位切换平滑速度，推荐 2~4")]
        public float smoothSpeed = 3f;

        private Transform currentTargetPoint;
        private Coroutine autoBackCoroutine;
        private Vector3 velocityPos;

        void Start()
        {
            if (cameraPoints == null || cameraPoints.Count == 0) return;
            currentTargetPoint = cameraPoints[0];
            transform.position = currentTargetPoint.position;
            transform.rotation = currentTargetPoint.rotation;
            velocityPos = Vector3.zero;
        }

        void Update()
        {
            if (currentTargetPoint == null) return;

            if (Input.GetKeyDown(KeyCode.RightArrow)) SwitchPoint(1);
            if (Input.GetKeyDown(KeyCode.LeftArrow)) SwitchPoint(-1);

            transform.position = Vector3.SmoothDamp(
                transform.position,
                currentTargetPoint.position,
                ref velocityPos,
                1f / smoothSpeed
            );

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                currentTargetPoint.rotation,
                smoothSpeed * 90f * Time.deltaTime
            );
        }

        private void SwitchPoint(int delta)
        {
            StopAutoBack();
            int idx = cameraPoints.IndexOf(currentTargetPoint) + delta;
            if (idx < 0) idx = cameraPoints.Count - 1;
            else if (idx >= cameraPoints.Count) idx = 0;

            currentTargetPoint = cameraPoints[idx];
            velocityPos = Vector3.zero;
        }

        /// <summary>普通跳转，无自动回退</summary>
        public void SetCameraTarget(Transform pointTrans)
        {
            StopAutoBack();
            if (pointTrans == null) return;
            currentTargetPoint = pointTrans;
            velocityPos = Vector3.zero;
        }

        /// <summary>跳转到目标，delay秒后退回到指定returnPoint</summary>
        public void SetCameraTargetWithAutoBack(Transform goPoint, Transform returnPoint, float delay)
        {
            StopAutoBack();
            if (goPoint == null) return;

            currentTargetPoint = goPoint;
            velocityPos = Vector3.zero;

            if (delay > 0 && returnPoint != null)
            {
                autoBackCoroutine = StartCoroutine(AutoBack(returnPoint, delay));
            }
        }

        private IEnumerator AutoBack(Transform returnPoint, float delay)
        {
            yield return new WaitForSeconds(delay);
            Debug.Log($"[CameraTour]执行退回，目标机位：{returnPoint.name}");
            currentTargetPoint = returnPoint;
            velocityPos = Vector3.zero;
            autoBackCoroutine = null;
        }

        public void StopAutoBack()
        {
            if (autoBackCoroutine != null)
            {
                StopCoroutine(autoBackCoroutine);
                autoBackCoroutine = null;
            }
        }
    }
}
