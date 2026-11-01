using UnityEngine;

/// <summary>
/// Gives positions at random in a circle to relocate ball or target when needed.
/// </summary>
public class Spawner : MonoBehaviour {
    [SerializeField] Color gizmosColor = Color.yellow;
    [SerializeField] float randomRadius = 10f;
    public Vector2 RandomPosition => (Vector2)transform.position + Random.insideUnitCircle * randomRadius;

    private const int gizmoCircleSegments = 32;
    private static Vector3[] gizmosCirclePoints = null;
    private void OnDrawGizmos() {

        if (gizmosCirclePoints == null) {
            gizmosCirclePoints = new Vector3[gizmoCircleSegments];
            for (int i = 0; i < gizmoCircleSegments; i++) {
                float angle = ((float)i / gizmoCircleSegments) * 2f * Mathf.PI;
                gizmosCirclePoints[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            }
        }

        Gizmos.color = gizmosColor;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, Vector3.one * randomRadius);
        Gizmos.DrawLineStrip(gizmosCirclePoints, true);
    }
}
