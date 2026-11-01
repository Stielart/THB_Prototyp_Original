using UnityEngine;

/// <summary>
/// Stachel (F5). Die Hitbox (Trigger) ist unabhängig von der Grafik und lässt sich verkleinern: Mit eingeschalteter
/// Hitbox-Toleranz (Taste 0) hat sie nur einen Teil der Größe der Grafik (Player.hitboxToleranceScale), sonst die volle Größe.
/// Die Hitbox bleibt an der Basisseite verankert, damit man die Spitze der Grafik berühren kann, ohne zu sterben.
///
/// Einrichtung: GameObject mit Sprite (SpriteRenderer) anlegen, dieses Skript hinzufügen (der Trigger-Collider
/// kommt automatisch dazu). Das Objekt darf NICHT auf einer Layer liegen, die in der Ground Mask des Players steht.
/// Gizmos im Editor: weiß = Grafik, rot = Hitbox.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class Spike : MonoBehaviour
{
    public enum Base { Bottom, Top, Left, Right, Center }

    [Tooltip("Seite der Grafik, an der die Hitbox verankert bleibt (die Seite, auf der der Stachel sitzt)")]
    public Base baseSide = Base.Bottom;

    private float appliedScale = -1f;

    private static Player cachedPlayer;

    private static Player GetPlayer()
    {
        if (cachedPlayer == null)
            cachedPlayer = FindFirstObjectByType<Player>();
        return cachedPlayer;
    }

    private void Reset()
    {
        ApplyHitbox(1f);
    }

    private void OnValidate()
    {
        ApplyHitbox(CurrentScale());
    }

    private void Start()
    {
        Player player = GetPlayer();
        if (player != null && (player.groundMask.value & (1 << gameObject.layer)) != 0)
            Debug.LogWarning($"Stachel '{name}' liegt auf einer Layer, die in der Ground Mask des Players steht. Der Boden-Strahl würde ihn als Boden erkennen.", this);
        ApplyHitbox(CurrentScale());
    }

    private void Update()
    {
        float scale = CurrentScale();
        if (!Mathf.Approximately(scale, appliedScale))
            ApplyHitbox(scale);
    }

    private static float CurrentScale()
    {
        Player player = GetPlayer();
        return player != null ? player.HitboxScale : 1f;
    }

    /// <summary>Größe und Mittelpunkt der Hitbox im lokalen Raum des Stachels für einen Verkleinerungsfaktor.</summary>
    private void GetHitbox(float scale, out Vector2 size, out Vector2 offset)
    {
        Bounds graphic = GetComponent<SpriteRenderer>().localBounds;
        size = (Vector2)graphic.size * scale;
        offset = graphic.center;
        switch (baseSide)
        {
            case Base.Bottom: offset.y = graphic.min.y + size.y * 0.5f; break;
            case Base.Top: offset.y = graphic.max.y - size.y * 0.5f; break;
            case Base.Left: offset.x = graphic.min.x + size.x * 0.5f; break;
            case Base.Right: offset.x = graphic.max.x - size.x * 0.5f; break;
        }
    }

    private void ApplyHitbox(float scale)
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (box == null || sr == null || sr.sprite == null)
            return;

        GetHitbox(scale, out Vector2 size, out Vector2 offset);
        box.isTrigger = true;
        box.size = size;
        box.offset = offset;
        appliedScale = scale;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (player != null && AttemptManager.Instance != null)
            AttemptManager.Instance.KillPlayer(player);
    }

    private void OnDrawGizmos()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
            return;

        Gizmos.matrix = transform.localToWorldMatrix;

        Bounds graphic = sr.localBounds;
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(graphic.center, graphic.size);

        GetHitbox(CurrentScale(), out Vector2 size, out Vector2 offset);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(offset, size);
    }
}
