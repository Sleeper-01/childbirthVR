using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace 产前运动
{
    public class 文字输入 : MonoBehaviour
    {
        public TMP_InputField field;
        public Button send;
        public Action<string> Submitted;
        private int compositionFrame = -100;
        private void Awake()
        {
            send.onClick.AddListener(Submit);
            field.onSubmit.AddListener(_ =>
            {
                if (string.IsNullOrEmpty(Input.compositionString) && Time.frameCount > compositionFrame + 1) Submit();
                else field.ActivateInputField();
            });
        }
        private void Update()
        {
            if (!string.IsNullOrEmpty(Input.compositionString)) compositionFrame = Time.frameCount;
        }
        public void Submit()
        {
            if (send.interactable && string.IsNullOrEmpty(Input.compositionString)) Submitted?.Invoke(field.text.Trim());
        }
        public void SetBusy(bool busy) { send.interactable = !busy; field.interactable = !busy; }
    }
}
