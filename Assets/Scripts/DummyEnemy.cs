using UnityEngine;

/// <summary>
/// Dummy-Gegner (Testhilfe für die Hitboxen). Läuft auf dem Boden hin und her, bleibt an Kanten und Wänden kurz stehen und
/// dreht um (Raycasts vor den Füßen und vor dem Körper). Die Figur kann auf ihn springen (Stomp): Er verschwindet und die
/// Figur springt mit den Sprungwerten des aktiven Profils ab. Jede andere Berührung beendet den Versuch wie ein Stachel;
/// dafür gilt dieselbe Hitbox-Toleranz (Taste 0): Die gefährliche Fläche ist dann kleiner als die Grafik.
/// Nach jedem Respawn steht der Gegner wieder an seiner Startposition.
///
/// Einrichtung: Prefab "Enemy_Dummy" in die Szene ziehen, so dass die Unterkante der Grafik auf dem Boden steht.
/// Das Objekt braucht keinen Collider und keinen Rigidbody, alles läuft über die Grenzen der Grafik. Ein optionaler Animator
/// bekommt den Bool-Parameter "moving" (true beim Laufen, false in der Pause). Die Ground Mask muss die
/// Boden-Layer enthalten (wie beim Player), der Gegner selbst darf nicht auf einer dieser Layer liegen.
/// Gizmos im Editor: weiß = Grafik, rot = gefährliche Fläche, gelb = Strahlen für Kante und Wand, cyan = Fußbox der Figur.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DummyEnemy : MonoBehaviour
{
    [Header("Bewegung")]
    [Tooltip("Geschwindigkeit in Einheiten pro Sekunde (1 Einheit = 1 Tile)")]
    public float moveSpeed = 2f;
    [Tooltip("Pause an Kante oder Wand in Frames (60 Frames = 1 Sekunde), bevor er umdreht")]
    public int turnPauseFrames = 45;
    public bool startFacingRight = false;
    [Tooltip("Layer, die als Boden und Wand gelten (wie die Ground Mask des Players)")]
    public LayerMask groundMask;
    [Tooltip("Wie tief der Strahl vor den Füßen nach Boden sucht (Einheiten)")]
    public float ledgeCheckDepth = 0.4f;
    [Tooltip("Wie weit der Strahl vor dem Körper nach einer Wand sucht (Einheiten)")]
    public float wallCheckDistance = 0.1f;

    [Header("Stomp")]
    [Tooltip("Die Füße der Figur müssen im Frame davor mindestens so hoch über dem Gegner gewesen sein (Anteil der Höhe, 0,5 = Mitte). Verhindert, dass ein Stomp als Seitentreffer zählt, wenn die Figur schnell fällt.")]
    [Range(0f, 1f)] public float stompHeightFraction = 0.5f;

    private static readonly int MovingHash = Animator.StringToHash("moving");

    private SpriteRenderer sr;
    private Animator anim;
    private Player player;
    private Vector3 startPosition;
    // Grenzen der Grafik, einmal beim Start gemessen, damit die Hitbox während der Animation nicht schwankt
    private Vector3 boundsOffset;
    private Vector3 boundsSize;
    private bool facingRight;
    private bool defeated;
    private int pauseFrames;
    private float previousFeetY = float.NegativeInfinity;

    private void Reset()
    {
        groundMask = LayerMask.GetMask("Ground");
    }

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        startPosition = transform.position;
        Bounds measured = sr.bounds;
        boundsOffset = measured.center - transform.position;
        boundsSize = measured.size;
        SetFacing(startFacingRight);
    }

    private void OnEnable() => AttemptManager.Respawned += ResetEnemy;
    private void OnDisable() => AttemptManager.Respawned -= ResetEnemy;

    private void ResetEnemy()
    {
        defeated = false;
        sr.enabled = true;
        transform.position = startPosition;
        pauseFrames = 0;
        previousFeetY = float.NegativeInfinity;
        SetFacing(startFacingRight);
        SetMoving(false);
    }

    private void SetMoving(bool moving)
    {
        if (anim != null)
            anim.SetBool(MovingHash, moving);
    }

    private void SetFacing(bool right)
    {
        facingRight = right;
        // Die Grafik des Pilzes schaut im Original nach links
        sr.flipX = right;
    }

    private void FixedUpdate()
    {
        if (defeated)
            return;
        if (player == null)
            player = FindFirstObjectByType<Player>();
        if (player == null)
            return;

        Move();
        CheckPlayer();
    }

    private void Move()
    {
        if (pauseFrames > 0)
        {
            pauseFrames--;
            if (pauseFrames == 0)
                SetFacing(!facingRight);
            return;
        }

        Bounds b = GraphicBounds();
        float dir = facingRight ? 1f : -1f;

        Vector2 footOrigin = new Vector2(b.center.x + dir * b.extents.x, b.min.y + 0.1f);
        bool groundAhead = Physics2D.Raycast(footOrigin, Vector2.down, 0.1f + ledgeCheckDepth, groundMask);
        bool wallAhead = Physics2D.Raycast(b.center, Vector2.right * dir, b.extents.x + wallCheckDistance, groundMask);

        if (!groundAhead || wallAhead)
        {
            pauseFrames = Mathf.Max(1, turnPauseFrames);
            SetMoving(false);
            return;
        }

        SetMoving(true);
        transform.position += new Vector3(dir * moveSpeed * Time.fixedDeltaTime, 0f, 0f);
    }

    /// <summary>Feste Grenzen der Grafik (aus dem ersten Bild gemessen), gespiegelt je nach Blickrichtung.</summary>
    private Bounds GraphicBounds()
    {
        Vector3 offset = boundsOffset;
        if (sr.flipX)
            offset.x = -offset.x;
        return new Bounds(transform.position + offset, boundsSize);
    }

    private static bool Overlap(Bounds a, Bounds b)
    {
        return a.min.x < b.max.x && a.max.x > b.min.x && a.min.y < b.max.y && a.max.y > b.min.y;
    }

    /// <summary>Gefährliche Fläche: die Grafik mal Toleranzfaktor, an der Unterkante verankert (wie beim Stachel).</summary>
    private Bounds HurtBounds()
    {
        Bounds b = GraphicBounds();
        float scale = player != null ? player.HitboxScale : 1f;
        Vector3 size = new Vector3(b.size.x * scale, b.size.y * scale, 1f);
        return new Bounds(new Vector3(b.center.x, b.min.y + size.y * 0.5f, 0f), size);
    }

    private void CheckPlayer()
    {
        if (player.isDead || AttemptManager.Instance == null)
        {
            previousFeetY = float.NegativeInfinity;
            return;
        }

        Bounds enemy = GraphicBounds();
        Bounds body = player.BodyBounds;

        bool wasAbove = previousFeetY >= enemy.min.y + enemy.size.y * stompHeightFraction;
        previousFeetY = body.min.y;

        // Stomp hat Vorrang: Füße auf dem Gegner, die Figur fällt und war im Frame davor oberhalb
        if (wasAbove && player.rb.linearVelocity.y <= 0f && Overlap(player.FeetBounds, enemy))
        {
            defeated = true;
            sr.enabled = false;
            player.Stomp();
            return;
        }

        if (Overlap(body, HurtBounds()))
            AttemptManager.Instance.KillPlayer(player);
    }

    private void OnDrawGizmos()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null || renderer.sprite == null)
            return;

        Bounds b = Application.isPlaying ? GraphicBounds() : renderer.bounds;
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(b.center, new Vector3(b.size.x, b.size.y, 0f));

        float scale = Application.isPlaying && player != null ? player.HitboxScale : 1f;
        Vector3 size = new Vector3(b.size.x * scale, b.size.y * scale, 0f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(new Vector3(b.center.x, b.min.y + size.y * 0.5f, 0f), size);

        float dir = (Application.isPlaying ? facingRight : startFacingRight) ? 1f : -1f;
        Gizmos.color = Color.yellow;
        Vector3 foot = new Vector3(b.center.x + dir * b.extents.x, b.min.y + 0.1f, 0f);
        Gizmos.DrawLine(foot, foot + Vector3.down * (0.1f + ledgeCheckDepth));
        Gizmos.DrawLine(b.center, b.center + Vector3.right * dir * (b.extents.x + wallCheckDistance));
    }
}
