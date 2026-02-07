using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Archero-style drifting joystick driven by mouse.
/// Outputs a Vector2 in [-1..1] where magnitude is analog strength (0..1).
/// Supports a drag speed trigger that can fire repeatedly while dragging.
/// Supports an edge trigger that fires when the knob hits the outer perimeter.
/// </summary>
public class DriftingJoystickMouse : MonoBehaviour
{
    [Header("Mouse")]
    public int mouseButton = 0; // 0=left, 1=right, 2=middle

    [Header("Joystick Feel")]
    public float maxRadiusPixels = 120f;
    public float deadZonePixels = 8f;
    public float smoothing = 0f; // 0=off, try 12-20

    public bool IsActive => _isDragging;
    public Vector2 RawValue => _rawValue;
    public Vector2 Value => _value;

    public Vector2 AnchorScreenPos => _anchorPos;
    public Vector2 KnobScreenPos => _anchorPos + _rawClampedDelta;

    bool _isDragging;
    Vector2 _anchorPos;
    Vector2 _currentPos;

    Vector2 _rawClampedDelta;
    Vector2 _rawValue;
    Vector2 _value;

    [Header("Tap vs Drag")]
    public float tapMaxDuration = 0.18f;
    public float tapMaxMovePixels = 12f;

    public bool TapThisFrame { get; private set; }
    public bool DragThisFrame { get; private set; }
    public bool IsDraggingGesture => _dragStarted;

    private float _pressStartTime;
    private Vector2 _pressStartPos;
    private bool _dragStarted;

    // =========================
    // Drag speed trigger
    // =========================
    [Header("Drag Speed Trigger")]
    public bool enableDragSpeedTrigger = true;

    [Tooltip("Minimum distance between samples required to evaluate speed (pixels).")]
    public float speedTriggerMinSegmentDistancePixels = 10f;

    [Tooltip("Speed threshold in pixels per second.")]
    public float speedTriggerThresholdPixelsPerSecond = 900f;

    [Tooltip("Ignore speed checks if time between samples is too large (seconds). 0 = ignore.")]
    public float speedTriggerMaxSampleDeltaTime = 0.12f;

    public bool SpeedTriggeredThisFrame { get; private set; }

    private Vector2 _lastSpeedSamplePos;
    private float _lastSpeedSampleTime;

    public event Action DragSpeedTriggered;

    public void OnDragSpeedTriggered()
    {
        DragSpeedTriggered?.Invoke();
    }

    // =========================
    // Edge trigger (outer perimeter)
    // =========================
    [Header("Edge Trigger")]
    public bool enableEdgeTrigger = true;

    [Tooltip("How close (in pixels) to the max radius counts as 'on the edge'.")]
    public float edgeTriggerTolerancePixels = 1.5f;

    public bool EdgeTriggeredThisFrame { get; private set; }

    public event Action EdgeTriggered;

    public bool IsOnEdge { get; private set; }
    public event Action<bool> EdgeChanged; // true = entered edge, false = left edge

    public void OnEdgeTriggered()
    {
        EdgeTriggered?.Invoke();
    }

    private bool _wasOnEdge;

    void Update()
    {
        TapThisFrame = false;
        DragThisFrame = false;
        SpeedTriggeredThisFrame = false;
        EdgeTriggeredThisFrame = false;

        _rawValue = Read();
        _value = ApplySmoothing(_rawValue);
    }

    public void Cancel()
    {
        _isDragging = false;
        _rawClampedDelta = Vector2.zero;
        _rawValue = Vector2.zero;
        _value = Vector2.zero;

        _dragStarted = false;
        _wasOnEdge = false;
    }

    Vector2 Read()
    {
        // Start
        if (Input.GetMouseButtonDown(mouseButton))
        {
            _isDragging = true;
            _anchorPos = _currentPos = (Vector2)Input.mousePosition;
            _rawClampedDelta = Vector2.zero;

            _pressStartTime = Time.time;
            _pressStartPos = _currentPos;
            _dragStarted = false;

            _lastSpeedSamplePos = _currentPos;
            _lastSpeedSampleTime = Time.time;

            _wasOnEdge = false;

            return Vector2.zero;
        }

        // Stop
        if (Input.GetMouseButtonUp(mouseButton))
        {
            _isDragging = false;
            _rawClampedDelta = Vector2.zero;

            float duration = Time.time - _pressStartTime;
            float totalMove = ((Vector2)Input.mousePosition - _pressStartPos).magnitude;

            TapThisFrame = !_dragStarted &&
                           duration <= tapMaxDuration &&
                           totalMove <= tapMaxMovePixels;

            _wasOnEdge = false;
            return Vector2.zero;
        }

        if (!_isDragging)
        {
            _rawClampedDelta = Vector2.zero;
            _wasOnEdge = false;
            return Vector2.zero;
        }

        _currentPos = (Vector2)Input.mousePosition;

        TryDragSpeedTrigger(_currentPos);

        Vector2 delta = _currentPos - _anchorPos;
        float mag = delta.magnitude;

        if (mag < deadZonePixels)
        {
            _rawClampedDelta = Vector2.zero;
            TryEdgeTrigger(0f);
            return Vector2.zero;
        }

        if (!_dragStarted && mag > tapMaxMovePixels)
        {
            _dragStarted = true;
            DragThisFrame = true;
        }

        if (mag > maxRadiusPixels)
        {
            Vector2 dir = delta / mag;
            _anchorPos = _currentPos - dir * maxRadiusPixels;
            delta = _currentPos - _anchorPos;
        }

        _rawClampedDelta = delta;

        TryEdgeTrigger(_rawClampedDelta.magnitude);

        return delta / maxRadiusPixels;
    }

    void TryDragSpeedTrigger(Vector2 currentPos)
    {
        if (!enableDragSpeedTrigger) return;

        float now = Time.time;
        float dt = now - _lastSpeedSampleTime;

        if (dt <= 0f)
        {
            _lastSpeedSampleTime = now;
            _lastSpeedSamplePos = currentPos;
            return;
        }

        if (speedTriggerMaxSampleDeltaTime > 0f && dt > speedTriggerMaxSampleDeltaTime)
        {
            _lastSpeedSampleTime = now;
            _lastSpeedSamplePos = currentPos;
            return;
        }

        float dist = (currentPos - _lastSpeedSamplePos).magnitude;
        if (dist < speedTriggerMinSegmentDistancePixels) return;

        float speed = dist / dt;

        if (speed >= speedTriggerThresholdPixelsPerSecond)
        {
            SpeedTriggeredThisFrame = true;
            OnDragSpeedTriggered();
        }

        _lastSpeedSampleTime = now;
        _lastSpeedSamplePos = currentPos;
    }

    void TryEdgeTrigger(float clampedMagnitudePixels)
    {
        if (!enableEdgeTrigger) { _wasOnEdge = false; return; }

        bool onEdge = clampedMagnitudePixels >= (maxRadiusPixels - edgeTriggerTolerancePixels);

        if (onEdge != IsOnEdge)
        {
            IsOnEdge = onEdge;
            EdgeChanged?.Invoke(IsOnEdge);
        }

        if (onEdge && !_wasOnEdge)
        {
            EdgeTriggeredThisFrame = true;
            OnEdgeTriggered();
        }

        _wasOnEdge = onEdge;
    }

    Vector2 ApplySmoothing(Vector2 raw)
    {
        if (smoothing <= 0f) return raw;
        float t = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        return Vector2.Lerp(_value, raw, t);
    }

    // ===== Debug OnGUI =====
    [Header("Debug Draw (OnGUI)")]
    public bool debugDraw = true;

    [Range(32, 256)] public int circleTexSize = 128;
    [Range(1f, 6f)] public float outlineThicknessPx = 2f;
    [Range(1f, 12f)] public float anchorDotRadiusPx = 6f;
    [Range(1f, 12f)] public float knobDotRadiusPx = 5f;
    [Range(1f, 10f)] public float lineThicknessPx = 2.5f;

    public Color fillColor = new Color(0f, 1f, 0.8f, 0.12f);
    public Color outlineColor = new Color(0f, 1f, 0.8f, 0.85f);
    public Color lineFillColor = new Color(1f, 1f, 1f, 0.25f);
    public Color lineOutlineColor = new Color(1f, 1f, 1f, 0.70f);

    private Texture2D _circleFillTex;
    private Texture2D _circleRingTex;

    void EnsureDebugTextures()
    {
        if (_circleFillTex != null && _circleRingTex != null) return;
        _circleFillTex = MakeCircleTexture(circleTexSize, true, 0.08f);
        _circleRingTex = MakeCircleTexture(circleTexSize, false, 0.10f);
    }

    Texture2D MakeCircleTexture(int size, bool filled, float ringThickness01)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float r = (size - 1) * 0.5f;
        float cx = r, cy = r;
        float t = Mathf.Clamp01(ringThickness01);
        float inner = Mathf.Max(0f, 1f - t);

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - cx) / r;
                float dy = (y - cy) / r;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                float a = filled
                    ? Mathf.SmoothStep(1f, 0.98f, d)
                    : Mathf.Clamp01(
                        Mathf.SmoothStep(1f, 0.98f, d) -
                        Mathf.SmoothStep(inner, inner + 0.02f, d)
                      );

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }

        tex.Apply();
        return tex;
    }

    void OnGUI()
    {
        if (!debugDraw) return;
        if (!Application.isPlaying) return;
        if (!_isDragging) return;

        EnsureDebugTextures();

        Vector2 ToGUIPoint(Vector2 screen) => new Vector2(screen.x, Screen.height - screen.y);

        Vector2 anchor = ToGUIPoint(_anchorPos);
        Vector2 knob = ToGUIPoint(KnobScreenPos);

        float joyRadius = maxRadiusPixels;

        DrawCircle(anchor, joyRadius, _circleRingTex, Color.white);

        Vector2 sample = ToGUIPoint(_lastSpeedSamplePos);
        float thresholdRadius = speedTriggerMinSegmentDistancePixels;

        DrawCircle(sample, thresholdRadius + outlineThicknessPx, _circleRingTex, outlineColor);
        DrawCircle(sample, thresholdRadius, _circleFillTex, fillColor);

        DrawLine(anchor, knob, lineThicknessPx + outlineThicknessPx, lineOutlineColor);
        DrawLine(anchor, knob, lineThicknessPx, lineFillColor);

        DrawCircle(anchor, anchorDotRadiusPx + outlineThicknessPx, _circleRingTex, outlineColor);
        DrawCircle(anchor, anchorDotRadiusPx, _circleFillTex, fillColor);

        DrawCircle(knob, knobDotRadiusPx + outlineThicknessPx, _circleRingTex, outlineColor);
        DrawCircle(knob, knobDotRadiusPx, _circleFillTex, fillColor);
    }

    void DrawCircle(Vector2 center, float radiusPx, Texture2D tex, Color color)
    {
        var prev = GUI.color;
        GUI.color = color;

        float d = radiusPx * 2f;
        var rect = new Rect(center.x - radiusPx, center.y - radiusPx, d, d);
        GUI.DrawTexture(rect, tex);

        GUI.color = prev;
    }

    void DrawLine(Vector2 a, Vector2 b, float thicknessPx, Color color)
    {
        var prev = GUI.color;
        GUI.color = color;

        Vector2 delta = b - a;
        float length = delta.magnitude;
        if (length < 0.001f) { GUI.color = prev; return; }

        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        Matrix4x4 old = GUI.matrix;

        GUIUtility.RotateAroundPivot(angle, a);
        GUI.DrawTexture(new Rect(a.x, a.y - thicknessPx * 0.5f, length, thicknessPx), Texture2D.whiteTexture);

        GUI.matrix = old;
        GUI.color = prev;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!debugDraw) return;
        if (!Application.isPlaying) return;
        if (!_isDragging) return;

        Handles.BeginGUI();

        Vector2 ToGUI(Vector2 p) => new Vector2(p.x, Screen.height - p.y);

        Vector2 anchor = ToGUI(_anchorPos);
        Vector2 knob = ToGUI(KnobScreenPos);
        Vector2 sample = ToGUI(_lastSpeedSamplePos);

        DrawGUICircle(anchor, maxRadiusPixels, outlineColor);
        DrawGUICircle(anchor, deadZonePixels, new Color(outlineColor.r, outlineColor.g, outlineColor.b, 0.4f));
        DrawGUICircle(sample, speedTriggerMinSegmentDistancePixels, outlineColor);

        Handles.color = lineOutlineColor;
        Handles.DrawLine(anchor, knob);

        DrawGUICircle(anchor, anchorDotRadiusPx, outlineColor, filled: true);
        DrawGUICircle(knob, knobDotRadiusPx, outlineColor, filled: true);

        Handles.EndGUI();
    }

    void DrawGUICircle(Vector2 center, float radius, Color color, bool filled = false)
    {
        Handles.color = color;
        int segments = 64;

        Vector3[] pts = new Vector3[segments + 1];
        for (int i = 0; i <= segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2f;
            pts[i] = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
        }

        if (filled)
            Handles.DrawAAConvexPolygon(pts);
        else
            Handles.DrawAAPolyLine(2f, pts);
    }
#endif
}