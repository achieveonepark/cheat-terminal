using System;
using UnityEngine;
using UnityEngine.UI;

namespace UniTerminal.UI
{
    /// <summary>
    /// Opens the terminal when the top-right corner is tapped. Builds a tiny,
    /// transparent button on its own minimal canvas (a single graphic), so the
    /// runtime cost is negligible. Defaults to a double-tap to avoid accidental opens.
    /// </summary>
    [AddComponentMenu("UniTerminal/Terminal Corner Trigger")]
    public sealed class TerminalCornerTrigger : MonoBehaviour, ITerminalTrigger
    {
        [SerializeField] private Vector2 _size = new Vector2(96f, 96f);
        [SerializeField] private int _requiredTaps = 2;
        [SerializeField] private float _tapWindow = 0.5f;

        private GameObject _canvasGo;
        private int _tapCount;
        private float _firstTapTime;

        public bool Enabled
        {
            get => _canvasGo != null && _canvasGo.activeSelf;
            set { if (_canvasGo != null) _canvasGo.SetActive(value); }
        }

        public event Action OnTriggered;

        private void Awake() => BuildUi();

        private void OnTap()
        {
            float now = Time.unscaledTime;
            if (_tapCount == 0 || now - _firstTapTime > _tapWindow)
            {
                _tapCount = 1;
                _firstTapTime = now;
            }
            else
            {
                _tapCount++;
            }

            if (_tapCount >= Mathf.Max(1, _requiredTaps))
            {
                _tapCount = 0;
                OnTriggered?.Invoke();
            }
        }

        private void BuildUi()
        {
            _canvasGo = new GameObject("UniTerminalTriggerCanvas");
            _canvasGo.transform.SetParent(transform, false);

            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue - 1;
            _canvasGo.AddComponent<GraphicRaycaster>();

            var btnGo = new GameObject("CornerButton", typeof(RectTransform));
            var rect = btnGo.GetComponent<RectTransform>();
            rect.SetParent(_canvasGo.transform, false);
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = _size;
            rect.anchoredPosition = Vector2.zero;

            var img = btnGo.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f); // invisible but raycastable
            img.raycastTarget = true;

            var btn = btnGo.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(OnTap);
        }
    }
}
