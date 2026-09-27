using UnityEngine;

namespace ChanFangVR
{
    /// 基础测试：只测试Unity基本功能，不依赖任何XR包
    public class BasicTest : MonoBehaviour
    {
        void Start()
        {
            Debug.Log("=== 基础Unity测试开始 ===");
            Debug.Log("Unity版本: " + Application.unityVersion);
            Debug.Log("目标平台: " + Application.platform);
            Debug.Log("当前场景: " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            
            // 测试Unity基本组件
            if (Camera.main != null)
            {
                Debug.Log("✓ 主摄像机正常");
                Debug.Log("摄像机位置: " + Camera.main.transform.position);
                Debug.Log("摄像机旋转: " + Camera.main.transform.rotation);
            }
            else
            {
                Debug.LogError("✗ 找不到主摄像机");
            }
            
            // 创建测试对象
            GameObject testObject = new GameObject("TestObject");
            testObject.transform.position = Vector3.zero;
            
            if (testObject != null)
            {
                Debug.Log("✓ 创建对象成功");
            }
            
            Debug.Log("=== 基础测试完成 ===");
            
            // 3秒后自动退出
            Invoke("QuitTest", 3f);
        }
        
        void Update()
        {
            if (DesktopInput.GetKeyDown(KeyCode.Escape))
            {
                Debug.Log("用户按ESC退出");
                QuitTest();
            }
        }
        
        void QuitTest()
        {
            Debug.Log("测试完成，退出");
            Application.Quit();
        }
    }
}