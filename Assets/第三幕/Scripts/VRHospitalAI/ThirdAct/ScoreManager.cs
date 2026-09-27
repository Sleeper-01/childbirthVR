using UnityEngine;

namespace VRHospitalAI.ThirdAct
{
    /// <summary>
    /// 安心值管理器（单例）。
    /// 第三幕各模块完成后调用 AddScore() 累加分数。
    /// 初始值 60，与文档 5.3 一致。
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [Header("安心值范围")]
        public int minScore = 0;
        public int maxScore = 100;
        public int currentScore = 60;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void AddScore(int delta)
        {
            currentScore = Mathf.Clamp(currentScore + delta, minScore, maxScore);
            string sign = delta > 0 ? "+" : "";
            Debug.Log("[安心值] " + sign + delta + " → " + currentScore);
        }

        public void SubtractScore(int delta) => AddScore(-delta);
        public int GetScore() => currentScore;
        public void ResetScore() => currentScore = 60;
    }
}
