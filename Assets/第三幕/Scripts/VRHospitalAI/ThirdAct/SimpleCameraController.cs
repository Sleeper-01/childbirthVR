using UnityEngine;

/// <summary>
/// 简单的 PC 相机控制器
/// 固定视角，不随鼠标移动
/// </summary>
public class SimpleCameraController : MonoBehaviour
{
    void Start()
    {
        // 固定相机，不移动。合并工程中本幕依赖鼠标点击UI，保持指针可见可移动
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        // 不响应鼠标移动，保持固定视角
    }
}
