using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Achieve.CheatTerminal.UI
{
    /// <summary>Visible, draggable safe-area dock with separate cheat and terminal buttons.</summary>
    [AddComponentMenu("Achieve.CheatTerminal/Terminal Corner Trigger")]
    public sealed class TerminalCornerTrigger : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Vector2 _size = new Vector2(252f, 104f);
        [SerializeField] private int _requiredTaps = 1;
        [SerializeField] private float _tapWindow = 0.5f;
        [SerializeField] private bool _showHandle = true;
        [SerializeField] private bool _visibleOnStart = true;

        private GameObject _canvasGo;
        private RectTransform _canvasRect;
        private RectTransform _dock;
        private Vector2 _normalizedPosition = Vector2.one;
        private Vector2 _lastPointer;
        private bool _dragging;
        private int _tapCount;
        private float _firstTapTime;
        private Rect _safeArea;
        private Vector2Int _screen;

        public bool Visible
        {
            get => _canvasGo != null && _canvasGo.activeSelf;
            set
            {
                if (_canvasGo == null) return;
                _canvasGo.SetActive(value);
                _tapCount = 0;
                _dragging = false;
                if (value) PlaceDock();
            }
        }

        public bool Enabled { get => Visible; set => Visible = value; }
        public void Toggle() => Visible = !Visible;
        public event Action OnTriggered;
        public event Action OnCheatHudRequested;

        private void Awake() => BuildUi();

        private void Update()
        {
            if (Visible && (_safeArea != Screen.safeArea || _screen.x != Screen.width || _screen.y != Screen.height))
                PlaceDock();
        }

        private void OnTap()
        {
            if (_dragging) return;
            float now = Time.unscaledTime;
            if (_tapCount == 0 || now - _firstTapTime > _tapWindow)
            {
                _tapCount = 1;
                _firstTapTime = now;
            }
            else _tapCount++;
            if (_tapCount < Mathf.Max(1, _requiredTaps)) return;
            _tapCount = 0;
            OnTriggered?.Invoke();
        }

        public void OnBeginDrag(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left) return;
            var hit = data.pointerPressRaycast.gameObject;
            if (hit == null || !hit.transform.IsChildOf(_dock)) return;
            _dragging = true;
            _tapCount = 0;
            data.eligibleForClick = false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, data.position, null, out _lastPointer);
        }

        public void OnDrag(PointerEventData data)
        {
            if (!_dragging) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, data.position, null, out var point)) return;
            _dock.anchoredPosition += point - _lastPointer;
            _lastPointer = point;
            GetBounds(out var min, out var max);
            Vector2 position = _dock.anchoredPosition;
            _normalizedPosition = new Vector2(Mathf.InverseLerp(min.x, max.x, position.x), Mathf.InverseLerp(min.y, max.y, position.y));
            _dock.anchoredPosition = new Vector2(Mathf.Clamp(position.x, min.x, max.x), Mathf.Clamp(position.y, min.y, max.y));
        }

        public void OnEndDrag(PointerEventData data)
        {
            data.eligibleForClick = false;
            _dragging = false;
        }

        private void GetBounds(out Vector2 min, out Vector2 max)
        {
            Rect safe = Screen.safeArea;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, safe.min, null, out var lower);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, safe.max, null, out var upper);
            Vector2 anchor = _canvasRect.rect.max;
            min = lower - anchor + _dock.sizeDelta + Vector2.one * 12f;
            max = upper - anchor - Vector2.one * 12f;
            min = Vector2.Min(min, max);
        }

        private void PlaceDock()
        {
            Canvas.ForceUpdateCanvases();
            GetBounds(out var min, out var max);
            _dock.anchoredPosition = new Vector2(Mathf.Lerp(min.x, max.x, _normalizedPosition.x), Mathf.Lerp(min.y, max.y, _normalizedPosition.y));
            _safeArea = Screen.safeArea;
            _screen = new Vector2Int(Screen.width, Screen.height);
        }

        private void BuildUi()
        {
            _canvasGo = new GameObject("Achieve.CheatTerminalTriggerCanvas", typeof(RectTransform));
            _canvasGo.transform.SetParent(transform, false);
            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Keep the dock behind the cheat panel (1000) and terminal so it never
            // covers their close buttons while either surface is open.
            canvas.sortingOrder = 999;
            _canvasRect = _canvasGo.GetComponent<RectTransform>();
            var scaler = _canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            _canvasGo.AddComponent<GraphicRaycaster>();

            _dock = Rect("DeveloperDock", _canvasGo.transform, Vector2.zero, Vector2.one);
            _dock.anchorMin = _dock.anchorMax = _dock.pivot = Vector2.one;
            _dock.sizeDelta = new Vector2(Mathf.Max(252f, _size.x), Mathf.Max(104f, _size.y));
            var background = _dock.gameObject.AddComponent<TerminalGlyph>();
            background.Kind = TerminalGlyph.Shape.Surface;
            background.color = _showHandle ? new Color(.055f, .075f, .10f, .97f) : Color.clear;

            var grip = Rect("DragGrip", _dock, new Vector2(0f, .28f), new Vector2(.15f, .72f));
            var glyph = grip.gameObject.AddComponent<TerminalGlyph>();
            glyph.Kind = TerminalGlyph.Shape.Grip;
            glyph.color = _showHandle ? new Color(.38f, .46f, .52f) : Color.clear;
            glyph.raycastTarget = false;
            BuildButton("CheatUI", .15f, .575f, TerminalGlyph.Shape.Cheats, "F1", () => { if (!_dragging) OnCheatHudRequested?.Invoke(); });
            BuildButton("Terminal", .575f, 1f, TerminalGlyph.Shape.Terminal, "F2", OnTap);
            Visible = _visibleOnStart;
        }

        private void BuildButton(string name, float left, float right, TerminalGlyph.Shape kind, string shortcut, UnityEngine.Events.UnityAction action)
        {
            var rect = Rect(name, _dock, new Vector2(left, 0f), new Vector2(right, 1f));
            var image = rect.gameObject.AddComponent<Image>();
            image.color = Color.clear;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            var icon = Rect("Icon", rect, new Vector2(.26f, .36f), new Vector2(.74f, .86f));
            var glyph = icon.gameObject.AddComponent<TerminalGlyph>();
            glyph.Kind = kind;
            glyph.color = _showHandle ? new Color(.40f, .89f, .76f) : Color.clear;
            glyph.raycastTarget = false;
            var labelRect = Rect("Shortcut", rect, new Vector2(0f, .08f), new Vector2(1f, .35f));
            var text = labelRect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = shortcut;
            text.fontSize = 20;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = _showHandle ? new Color(.64f, .71f, .77f) : Color.clear;
            text.raycastTarget = false;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
    }
}
