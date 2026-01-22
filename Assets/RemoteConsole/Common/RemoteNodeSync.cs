using UnityEngine;

namespace RConsole.Common
{
    [ExecuteAlways]
    public class RemoteNodeSync : MonoBehaviour
    {
        public int RuntimeInstanceID;
        
        private Vector3 _lastPosition;
        private Quaternion _lastRotation;
        private Vector3 _lastScale;
        private Vector2 _lastSizeDelta;
        private bool _lastActive;
        private bool _isRectTransform;
        private RectTransform _rectTransform;

        private float _lastSyncTime;
        private const float SYNC_INTERVAL = 0.1f; // 限制同步频率

        private void OnEnable()
        {
            _rectTransform = GetComponent<RectTransform>();
            _isRectTransform = _rectTransform != null;
        }

        public void Initialize()
        {
            _lastPosition = transform.localPosition;
            _lastRotation = transform.localRotation;
            _lastScale = transform.localScale;
            _lastActive = gameObject.activeSelf;
            
            _rectTransform = GetComponent<RectTransform>();
            _isRectTransform = _rectTransform != null;
            if (_isRectTransform) _lastSizeDelta = _rectTransform.sizeDelta;
            
            _lastSyncTime = Time.realtimeSinceStartup;
        }

        private void OnDisable()
        {
            if (!gameObject.activeSelf && _lastActive)
            {
                SendSync("Active", "0");
                _lastActive = false;
            }
        }

        private void Update()
        {
            if (Application.isPlaying) return; // 仅在 Editor 模式下工作
            if (Time.realtimeSinceStartup - _lastSyncTime < SYNC_INTERVAL) return;

            bool changed = false;

            // Active
            if (gameObject.activeSelf != _lastActive)
            {
                SendSync("Active", gameObject.activeSelf ? "1" : "0");
                _lastActive = gameObject.activeSelf;
                changed = true;
            }

            // Position
            if (Vector3.SqrMagnitude(transform.localPosition - _lastPosition) > 0.001f)
            {
                SendSync("Pos", $"{transform.localPosition.x},{transform.localPosition.y},{transform.localPosition.z}");
                _lastPosition = transform.localPosition;
                changed = true;
            }

            // Rotation
            if (Quaternion.Angle(transform.localRotation, _lastRotation) > 0.1f)
            {
                SendSync("Rot", $"{transform.localEulerAngles.x},{transform.localEulerAngles.y},{transform.localEulerAngles.z}");
                _lastRotation = transform.localRotation;
                changed = true;
            }

            // Scale
            if (Vector3.SqrMagnitude(transform.localScale - _lastScale) > 0.001f)
            {
                SendSync("Scale", $"{transform.localScale.x},{transform.localScale.y},{transform.localScale.z}");
                _lastScale = transform.localScale;
                changed = true;
            }

            // SizeDelta (RectTransform)
            if (_isRectTransform && Vector2.SqrMagnitude(_rectTransform.sizeDelta - _lastSizeDelta) > 0.001f)
            {
                SendSync("Size", $"{_rectTransform.sizeDelta.x},{_rectTransform.sizeDelta.y}");
                _lastSizeDelta = _rectTransform.sizeDelta;
                changed = true;
            }

            if (changed)
            {
                _lastSyncTime = Time.realtimeSinceStartup;
            }
        }

        private void SendSync(string type, string value)
        {
            // 通过反射调用 Editor 层的发送方法，避免直接引用 Editor 程序集
            // 或者我们可以约定一个静态委托
            // 简单起见，我们假设 RConsole.Editor.RConsoleCtrl 提供了静态方法，但这里是 Runtime 代码（虽然包在 #if UNITY_EDITOR）
            // 最好的方式是提供一个 Action 供外部注册
            OnSyncRequest?.Invoke(RuntimeInstanceID, type, value);
        }

        public static System.Action<int, string, string> OnSyncRequest;
    }
}
