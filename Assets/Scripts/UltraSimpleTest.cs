using UnityEngine;

namespace ChanFangVR
{
    /// 极简测试：完全避免任何潜在的问题API
    public class UltraSimpleTest : MonoBehaviour
    {
        void Start()
        {
            Debug.Log("=== 极简测试开始 ===");
            Debug.Log("Unity版本: " + Application.unityVersion);
            Debug.Log("目标平台: " + Application.platform);
            
            // 只测试最基本的Unity功能
            if (Camera.main != null)
            {
                Debug.Log("✓ 主摄像机存在");
                Debug.Log("摄像机位置: " + Camera.main.transform.position);
            }
            else
            {
                Debug.LogWarning("⚠️ 找不到主摄像机");
            }
            
            // 创建一个简单的立方体
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = new Vector3(0, 0, 5);
            Debug.Log("✓ 创建立方体成功");
            
            Debug.Log("=== 极简测试完成 ===");
            
            // 5秒后自动退出
            Invoke("QuitTest", 5f);
        }
        
        void Update()
        {
            // 只响应ESC键
            if (DesktopInput.GetKeyDown(KeyCode.Escape))
            {
                Debug.Log("用户按ESC退出");
                QuitTest();
            }
        }
        
        void QuitTest()
        {
            Debug.Log("测试完成，退出应用程序");
            Application.Quit();
        }
    }
}