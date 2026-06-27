using System;
using UnityEngine;
using UnityEngine.UI;

namespace Achieve.CheatTerminal.UI
{
    /// <summary>
    /// Opens the terminal when the top-right corner is tapped. Builds a tiny,
    /// transparent button on its own minimal canvas (a single graphic), so the
    /// runtime cost is negligible. Defaults to a single tap for fast access.
    /// </summary>
    [AddComponentMenu("Achieve.CheatTerminal/Terminal Corner Trigger")]
    public sealed class TerminalCornerTrigger : MonoBehaviour
    {
        [SerializeField] private Vector2 _size = new Vector2(76f, 50f);
        [SerializeField] private int _requiredTaps = 1;
        [SerializeField] private float _tapWindow = 0.5f;
        [SerializeField] private bool _showHandle = true;

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
            _canvasGo = new GameObject("Achieve.CheatTerminalTriggerCanvas");
            _canvasGo.transform.SetParent(transform, false);

            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue - 1;

            var scaler = _canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            _canvasGo.AddComponent<GraphicRaycaster>();

            var btnGo = new GameObject("CornerButton", typeof(RectTransform));
            var rect = btnGo.GetComponent<RectTransform>();
            rect.SetParent(_canvasGo.transform, false);
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = _size;
            rect.anchoredPosition = new Vector2(-8f, -8f);

            var img = btnGo.AddComponent<Image>();
            img.color = _showHandle
                ? new Color(0.10f, 0.12f, 0.16f, 0.65f) // visible translucent handle
                : new Color(1f, 1f, 1f, 0f);            // invisible but raycastable
            img.raycastTarget = true;

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(OnTap);

            if (_showHandle)
                BuildLabel(rect);
        }

        private static void BuildLabel(RectTransform parent)
        {
            var labelGo = new GameObject("Label", typeof(RectTransform));
            var rect = labelGo.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var label = labelGo.AddComponent<Text>();
            label.text = ">_";
            label.alignment = TextAnchor.MiddleCenter;
            label.fontStyle = FontStyle.Bold;
            label.fontSize = 26;
            label.color = new Color(0.7f, 0.9f, 1f, 0.9f);
            label.raycastTarget = false;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
