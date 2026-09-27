using UnityEngine;
using UnityEngine.EventSystems;
namespace 产前运动
{
    public class 视频进度拖动 : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public 视频控制器 视频;
        public void OnPointerDown(PointerEventData e) { 视频.开始拖动(); }
        public void OnPointerUp(PointerEventData e) { 视频.结束拖动(); }
        private void OnDisable() { if(视频!=null)视频.结束拖动(); }
    }
}
