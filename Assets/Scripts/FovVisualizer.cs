using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Draws a filled mesh cone in the Scene view to visualise the Cat's field of view.
/// Reads viewRadius and viewAngle directly from the sibling CatAI component.
/// Editor-only: has zero runtime overhead.
/// </summary>
[RequireComponent(typeof(CatAI))]
public class FovVisualizer : MonoBehaviour
{
    [Header("Cone Appearance")]
    [SerializeField]
    [Tooltip("The color used to fill the field of view cone. This color is semi-transparent to allow visibility of objects behind it.")]
    private Color _fillColor = new(1f, 1f, 0f, 0.15f);

    [SerializeField]
    [Tooltip("The color used for the outline of the field of view cone.")]
    private Color _outlineColor = new(1f, 0.85f, 0f, 1f);

    private void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        if (!TryGetComponent<CatAI>(out var cat)) 
        { 
            return; 
        }

        // Get the cat's vision parameters from the CatAI component
        float radius = cat.ViewRadius;
        float angle = cat.ViewAngle;
        float height = cat.ViewHeight;

        // Calculate the origin and forward direction for the cone
        Vector3 origin = transform.position + Vector3.up * height;
        Vector3 forward = transform.forward;

        // Calculate the starting point of the arc (left boundary of the cone)
        Vector3 arcFrom = Quaternion.Euler(0f, -angle * 0.5f, 0f) * forward;

        // Solid filled arc
        Handles.color = _fillColor;
        Handles.DrawSolidArc(origin, Vector3.up, arcFrom, angle, radius);

        // Wireframe arc outline
        Handles.color = _outlineColor;
        Handles.DrawWireArc(origin, Vector3.up, arcFrom, angle, radius);

        // Lines from origin to arc endpoints
        Gizmos.color = _outlineColor;
        Gizmos.DrawLine(origin, origin + arcFrom.normalized * radius);
        Gizmos.DrawLine(origin, origin + (Quaternion.Euler(0f, angle * 0.5f, 0f) * forward).normalized * radius);

        // Optional: draw a wire sphere to indicate the maximum view radius
        Gizmos.color = new Color(_outlineColor.r, _outlineColor.g, _outlineColor.b, 0.15f);
        Gizmos.DrawWireSphere(origin, radius);
#endif
    }
}
