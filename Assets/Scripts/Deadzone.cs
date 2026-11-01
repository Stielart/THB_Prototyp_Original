using UnityEngine;

/// <summary>
/// Todeszone (Trigger). Berührt die Figur die Zone, endet der Versuch genau wie bei einem Stachel: Der Versuchszähler steigt,
/// nach der Pause erscheint die Figur am letzten Checkpoint (siehe AttemptManager). Gedacht für Abgründe unter dem Level.
/// Einrichtung: Prefab "Deadzone" in die Szene ziehen und mit Rect-Werkzeug oder Scale auf die gewünschte Größe ziehen
/// (oder leeres GameObject mit diesem Skript). Die Zone ist im Spiel unsichtbar, im Editor zeigt ein rotes Gizmo ihre Größe.
/// Das Objekt darf NICHT auf einer Layer liegen, die in der Ground Mask des Players steht.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class Deadzone : MonoBehaviour
{
    private void Reset()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(10f, 2f);
    }

    private void Start()
    {
        Player player = FindFirstObjectByType<Player>();
        if (player != null && (player.groundMask.value & (1 << gameObject.layer)) != 0)
            Debug.LogWarning($"Deadzone '{name}' liegt auf einer Layer, die in der Ground Mask des Players steht.", this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (player != null && AttemptManager.Instance != null)
            AttemptManager.Instance.KillPlayer(player);
    }

    private void OnDrawGizmos()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null)
            return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
        Gizmos.DrawCube(box.offset, box.size);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(box.offset, box.size);
    }
}
