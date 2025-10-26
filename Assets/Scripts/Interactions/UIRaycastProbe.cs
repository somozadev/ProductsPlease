using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIRaycastProbe : MonoBehaviour
{
    public bool logEveryFrame = false;

    void Update()
    {
        if (EventSystem.current == null)
        {
            if (Input.GetMouseButtonDown(0)) Debug.LogError("No EventSystem in scene.");
            return;
        }

        var ped = new PointerEventData(EventSystem.current)
        {
            position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
        };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);

        if (logEveryFrame)
            Debug.Log($"[UIProbe] hits: {results.Count}");

        if (Input.GetMouseButtonDown(0))
        {
            if (results.Count == 0)
            {
                Debug.LogWarning("UI Raycast 0 hits. Revisa: Canvas GraphicRaycaster + (si no es Overlay) EventCamera asignada + que el Graphic (Image/Text/TMP) tenga Raycast Target ON + CanvasGroup.blocksRaycasts=true + Sorting Order.");
            }
            else
            {
                Debug.Log($"UI Raycast hits ({results.Count}):");
                foreach (var r in results)
                {
                    string path = r.gameObject.name;
                    Transform t = r.gameObject.transform;
                    while (t.parent) { t = t.parent; path = t.name + "/" + path; }
                    var g = r.gameObject.GetComponent<Graphic>();
                    var cg = r.gameObject.GetComponentInParent<CanvasGroup>();
                    Debug.Log($" - {path} | Graphic={(g? g.raycastTarget.ToString() : "no-graphic")} | CanvasGroup.blocksRaycasts={(cg? cg.blocksRaycasts.ToString() : "n/a")} | Module={r.module?.GetType().Name}");
                }
            }
        }
    }
}