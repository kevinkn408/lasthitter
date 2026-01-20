using UnityEngine;

/// <summary>
/// Archero-style drifting joystick driven by mouse.
/// Outputs a Vector2 in [-1..1] where magnitude is analog strength (0..1).
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
    public Vector2 RawValue => _rawValue;         // unsmoothed
    public Vector2 Value => _value;               // smoothed (if smoothing > 0)

    // Useful if you want to draw UI later
    public Vector2 AnchorScreenPos => _anchorPos;
    public Vector2 KnobScreenPos => _anchorPos + (_rawClampedDelta);

    bool _isDragging;
    Vector2 _anchorPos;
    Vector2 _currentPos;

    Vector2 _rawClampedDelta;
    Vector2 _rawValue;
    Vector2 _value;


    [Header("Tap vs Drag")]
    public float tapMaxDuration = 0.18f;   // seconds
    public float tapMaxMovePixels = 12f;   // "slop" radius

    public bool TapThisFrame { get; private set; } // true for 1 frame after release
    public bool DragThisFrame { get; private set; } // true for 1 frame when drag is recognized
    public bool IsDraggingGesture => _dragStarted;  // latched once movement exceeds slop

    private float _pressStartTime;
    private Vector2 _pressStartPos;
    private bool _dragStarted;

    void Update()
    {
        TapThisFrame = false;
        DragThisFrame = false;

        _rawValue = Read();
        _value = ApplySmoothing(_rawValue);
    }

    /// <summary>Call this if you want to force-release the joystick.</summary>
    public void Cancel()
    {
        _isDragging = false;
        _rawClampedDelta = Vector2.zero;
        _rawValue = Vector2.zero;
        _value = Vector2.zero;
    }

    Vector2 Read()
    {
        // Start
        if (Input.GetMouseButtonDown(mouseButton))
        {
            
            _isDragging = true;
            _anchorPos = _currentPos = Input.mousePosition;
            _rawClampedDelta = Vector2.zero;

            _pressStartTime = Time.time;
            _pressStartPos = _currentPos;
            _dragStarted = false;
            return Vector2.zero;
        }

        // Stop
        if (Input.GetMouseButtonUp(mouseButton))
        {
            

            _isDragging = false;
            _rawClampedDelta = Vector2.zero;

            float duration = Time.time - _pressStartTime;
            float totalMove = (Input.mousePosition.magnitude - _pressStartPos.magnitude);
            bool isTap = !_dragStarted && duration <= tapMaxDuration && totalMove <= tapMaxMovePixels;
            TapThisFrame = isTap;
            return Vector2.zero;
        }

        if (!_isDragging)
        {
            _rawClampedDelta = Vector2.zero;
            return Vector2.zero;
        }

        _currentPos = Input.mousePosition;

        Vector2 delta = _currentPos - _anchorPos;
        float mag = delta.magnitude;

        // Deadzone
        if (mag < deadZonePixels)
        {
            _rawClampedDelta = Vector2.zero;
            return Vector2.zero;
        }

        // Drifting anchor
        if (mag > maxRadiusPixels)
        {
            Vector2 dir = delta / mag;
            _anchorPos = _currentPos - dir * maxRadiusPixels;
            delta = _currentPos - _anchorPos;
        }

        // (Optional) If you want strict clamp even inside radius:
        // delta = Vector2.ClampMagnitude(delta, maxRadiusPixels);

        _rawClampedDelta = delta;

        // Normalize to [-1..1] joystick range
        return delta / maxRadiusPixels;
    }

    Vector2 ApplySmoothing(Vector2 raw)
    {
        if (smoothing <= 0f) return raw;

        float t = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        // Smooth toward raw input
        return Vector2.Lerp(_value, raw, t);
    }
}