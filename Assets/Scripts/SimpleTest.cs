using UnityEngine;

namespace ChanFangVR
{
    /// 简化测试脚本：确保项目能成功运行
    public class SimpleTest : MonoBehaviour
    {
        void Start()
        {
            Debug.Log("=== VR分娩预演项目测试 ===");
            Debug.Log("Unity版本: " + Application.unityVersion);
            Debug.Log("运行平台: " + Application.platform);
            Debug.Log("当前场景: " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            
            // 测试基本组件
            if (Camera.main != null)
            {
                Debug.Log("✓ 主摄像机正常: " + Camera.main.name);
            }
            else
            {
                Debug.LogWarning("✗ 未找到主摄像机");
            }
            
            // 测试UI组件
            GameObject uiRoot = GameObject.Find("UIRoot");
            if (uiRoot != null)
            {
                Debug.Log("✓ UI Root存在: " + uiRoot.name);
            }
            else
            {
                Debug.LogWarning("✗ 未找到UI Root");
            }
            
            Debug.Log("=== 测试完成 ===");
        }
        
        void Update()
        {
            // 按ESC退出测试
            if (DesktopInput.GetKeyDown(KeyCode.Escape))
            {
                Debug.Log("用户退出测试");
                Application.Quit();
            }
        }
    }
}