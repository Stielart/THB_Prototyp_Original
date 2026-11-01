using UnityEngine;

/// <summary>
/// Checkpoint (F6). Berührt die Figur die Trigger-Zone, wird dieser Punkt zum Respawn-Punkt.
/// Einrichtung: am einfachsten das Prefab "Checkpoint" in die Szene ziehen und mit dem Fuß der Stange auf den Boden stellen
/// (Position des Objekts = Boden). Alternativ ein leeres GameObject mit diesem Skript anlegen (Trigger-Collider kommt
/// automatisch) und die Zone im Editor auf die gewünschte Größe ziehen. Die Figur erscheint nach dem Tod an Position plus
/// Respawn Offset (cyanfarbene Box im Editor zeigt die Figur).
/// Optik: Hat das Objekt oder ein Kind einen Animator mit dem Trigger "activate", läuft beim ersten Aktivieren die
/// Flaggenanimation und die Flagge bleibt danach draußen. Ohne Animator wird ein SpriteRenderer stattdessen grün eingefärbt,
/// solange der Checkpoint aktiv ist.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class Checkpoint : MonoBehaviour
{
    private static readonly int ActivateHash = Animator.StringToHash("activate");

    [Tooltip("Versatz der Respawn-Position von der Position des Objekts (Einheiten)")]
    public Vector2 respawnOffset = Vector2.zero;

    private Animator animator;
    private SpriteRenderer sprite;
    private bool flagRaised;

    public Vector2 RespawnPosition => (Vector2)transform.position + respawnOffset;

    private void Reset()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(1f, 3f);
    }

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        sprite = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<Player>() != null && AttemptManager.Instance != null)
            AttemptManager.Instance.SetCheckpoint(this);
    }

    public void SetActiveVisual(bool active)
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            // Die Flagge bleibt auch dann draußen, wenn später ein anderer Checkpoint aktiv wird.
            if (active && !flagRaised)
            {
                flagRaised = true;
                animator.SetTrigger(ActivateHash);
            }
            return;
        }

        if (sprite != null)
            sprite.color = active ? new Color(0.5f, 1f, 0.5f) : Color.white;
    }

    private void OnDrawGizmos()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.offset, box.size);
        }

        // Figur am Respawn-Punkt (Größe des Player-Colliders: 0,84 x 1,94 Einheiten)
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(RespawnPosition, new Vector3(0.84f, 1.94f, 0f));
    }
}
