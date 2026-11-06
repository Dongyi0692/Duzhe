using UnityEngine;
using UnityEngine.EventSystems;

namespace Duzhe.Core
{
    /// TEMPORARY diagnostic probe (remove in C11 cleanup): logs every mouse
    /// click with the pointer position, whether the EventSystem saw UI under
    /// the pointer and which object it selected. This pinpoints whether a
    /// user click reaches the game and what the raycast actually hits.
    [DefaultExecutionOrder(1000)]
    public class ClickProbe : MonoBehaviour
    {
        private void Update()
        {
            if (!Input.GetMouseButtonDown(0)) return;

            Vector3 pos = Input.mousePosition;
            EventSystem es = EventSystem.current;
            bool overUi = es != null && es.IsPointerOverGameObject();
            GameObject selected = es != null ? es.currentSelectedGameObject : null;
            string module = es != null && es.currentInputModule != null
                ? es.currentInputModule.GetType().Name
                : "NONE";

            string hit = "none";
            if (es != null)
            {
                PointerEventData ped = new PointerEventData(es);
                ped.position = pos;
                var results = new System.Collections.Generic.List<RaycastResult>();
                es.RaycastAll(ped, results);
                if (results.Count > 0) hit = results[0].gameObject.name;
            }

            Debug.Log("[Duzhe] CLICK pos=" + pos.ToString("F0") +
                " window=" + Screen.width + "x" + Screen.height +
                " module=" + module +
                " overUI=" + overUi +
                " selected=" + (selected != null ? selected.name : "null") +
                " topHit=" + hit);
        }
    }
}
