using UnityEngine;

namespace ChanFangVR
{
    /// 测试脚本：验证项目是否能正常编译和运行
    public class TestProject : MonoBehaviour
    {
        void Start()
        {
            Debug.Log("项目测试启动成功！");
            Debug.Log("Unity版本: " + Application.unityVersion);
            Debug.Log("目标平台: " + Application.platform);
            
            if (Application.isEditor)
            {
                Debug.Log("当前运行环境: Unity编辑器");
            }
            
            // 测试基本组件
            var camera = Camera.main;
            if (camera != null)
            {
                Debug.Log("主摄像机正常: " + camera.name);
            }
            else
            {
                Debug.LogWarning("未找到主摄像机");
            }
        }
    }
}