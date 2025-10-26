using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class AimUIInteractor : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("Camera used as aim source (defaults to Camera.main)")]
    public Camera cam;
    [Tooltip("Just for debug ray length")]
    public float debugRayDistance = 50f;

    [Header("Filter (optional)")]
    [Tooltip("If empty, any UI under the crosshair counts. If set, only UI under these canvases is considered.")]
    public Canvas[] targetCanvases;

    [Header("Input")]
    [Tooltip("Pointer id for events (-1 = mouse left button convention).")]
    public int pointerId = -1;

    [Header("Debug")]
    public bool debugLogs = false;
    public bool debugDraw = false;

    public bool IsAimingUI { get; private set; }

    GameObject _currentTarget;     
    RaycastResult _currentRaycast; 
    GameObject _pressTarget;       
    PointerEventData _ped;         
    readonly List<RaycastResult> _results = new List<RaycastResult>(16);

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (EventSystem.current == null)
            Debug.LogError("[AimUIInteractor] No EventSystem present. UI won't work.");
    }

    void Update()
    {
        if (EventSystem.current == null) { IsAimingUI = false; return; }
        if (!cam) cam = Camera.main;
        if (!cam) { IsAimingUI = false; return; }

        EnsurePointer();

        // 1) Raycast UI desde el centro de pantalla
        _results.Clear();
        _ped.position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        EventSystem.current.RaycastAll(_ped, _results);

        var (hit, rr) = FirstValidWithRaycast(_results);
        UpdateHover(hit, rr);

        // 2) Down / Up / Click
        if (hit != null && Input.GetMouseButtonDown(0))
            PointerDown(hit);

        if (Input.GetMouseButtonUp(0))
            PointerUp(hit);

        if (_currentTarget != null)
            ExecuteEvents.Execute(_currentTarget, _ped, ExecuteEvents.pointerMoveHandler);

        if (debugDraw)
            Debug.DrawRay(cam.transform.position, cam.transform.forward * debugRayDistance, hit ? Color.green : Color.red);
    }

    // ----------------------------------------------------------------

    void EnsurePointer()
    {
        if (_ped == null)
            _ped = new PointerEventData(EventSystem.current);

        _ped.Reset();
        _ped.pointerId = pointerId;
        _ped.button = PointerEventData.InputButton.Left;
        _ped.delta = Vector2.zero;
        _ped.scrollDelta = Vector2.zero;
    }

    (GameObject, RaycastResult) FirstValidWithRaycast(List<RaycastResult> list)
    {
        if (list == null || list.Count == 0) return (null, default);

        foreach (var r in list)
        {
            var go = r.gameObject;

            var clickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(go);
            if (clickHandler != null && IsAllowedCanvas(clickHandler))
                return (clickHandler, r);

            var downHandler = ExecuteEvents.GetEventHandler<IPointerDownHandler>(go);
            if (downHandler != null && IsAllowedCanvas(downHandler))
                return (downHandler, r);

            var selectable = go.GetComponentInParent<Selectable>();
            if (selectable != null && selectable.interactable && selectable.gameObject.activeInHierarchy && IsAllowedCanvas(selectable.gameObject))
                return (selectable.gameObject, r);
        }
        return (null, default);
    }

    bool IsAllowedCanvas(GameObject go)
    {
        if (targetCanvases == null || targetCanvases.Length == 0) return true;
        foreach (var c in targetCanvases)
            if (c && go.transform.IsChildOf(c.transform))
                return true;
        return false;
    }

    void UpdateHover(GameObject hit, RaycastResult rr)
    {
        if (hit == _currentTarget)
        {
            IsAimingUI = hit != null;
            _currentRaycast = rr;
            _ped.pointerCurrentRaycast = _currentRaycast;
            return;
        }

        if (_currentTarget)
            ExecuteEvents.Execute(_currentTarget, _ped, ExecuteEvents.pointerExitHandler);

        _currentTarget = hit;
        _currentRaycast = rr;

        if (_currentTarget)
        {
            _ped.pointerEnter = _currentTarget;
            _ped.pointerCurrentRaycast = _currentRaycast;

            ExecuteEvents.Execute(_currentTarget, _ped, ExecuteEvents.pointerEnterHandler);
        }

        IsAimingUI = _currentTarget != null;
    }

    void PointerDown(GameObject hit)
    {
        _ped.pointerCurrentRaycast = _currentRaycast;

        var newPressed = ExecuteEvents.ExecuteHierarchy(hit, _ped, ExecuteEvents.pointerDownHandler);

        if (newPressed == null)
        {
            newPressed = ExecuteEvents.ExecuteHierarchy(hit, _ped, ExecuteEvents.pointerClickHandler);
        }

        _ped.pressPosition = _ped.position;
        _ped.pointerPressRaycast = _currentRaycast;
        _ped.pointerPress = newPressed;
        _ped.rawPointerPress = hit;
        _ped.eligibleForClick = true;
        _ped.clickCount = 1;
        _ped.clickTime = Time.unscaledTime;

        _pressTarget = hit;

        if (debugLogs)
            Debug.Log($"[AimUIInteractor] DOWN on: {(newPressed ? newPressed.name : "(none)")} (hit: {hit.name})");
    }

    void PointerUp(GameObject hitUnderRelease)
    {
        if (_ped.pointerPress != null)
            ExecuteEvents.Execute(_ped.pointerPress, _ped, ExecuteEvents.pointerUpHandler);

        bool clicked = _ped.eligibleForClick && _ped.pointerPress != null && hitUnderRelease == _pressTarget;
        if (clicked)
            ExecuteEvents.Execute(_ped.pointerPress, _ped, ExecuteEvents.pointerClickHandler);

        if (debugLogs)
        {
            string namePress = _ped.pointerPress ? _ped.pointerPress.name : "(none)";
            string nameRelease = hitUnderRelease ? hitUnderRelease.name : "(null)";
            Debug.Log($"[AimUIInteractor] UP on: {nameRelease} | pressed: {namePress} | clicked: {clicked}");
        }

        _ped.eligibleForClick = false;
        _ped.pointerPress = null;
        _ped.rawPointerPress = null;
        _pressTarget = null;
    }
}
