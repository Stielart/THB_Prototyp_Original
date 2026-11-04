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
///
/// Zwei Bewegungsarten (Feld "Mode"): Ground = läuft auf dem Boden (siehe oben), Flying = schwebt in festem Takt von links nach
/// rechts um die Startposition (Flugweite in Tiles auf jeder Seite, Pause an den Umkehrpunkten). Der fliegende Gegner braucht
/// keinen Boden und keine Wand, hat aber dieselben Regeln: Stomp von oben, sonst Treffer wie ein Stachel. Prefab "Enemy_Flying".
/// Die gefährliche Fläche lässt sich pro Gegner in Breite und Höhe einstellen (Anteil der Grafik, Standard 1) und wird
/// zusätzlich mit der Hitbox-Toleranz (Taste 0) multipliziert.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DummyEnemy : MonoBehaviour
{
    public enum EnemyMode { Ground, Flying }

    [Header("Bewegung")]
    [Tooltip("Ground = läuft auf dem Boden, Flying = schwebt in festem Takt hin und her")]
    public EnemyMode mode = EnemyMode.Ground;
    [Tooltip("Nur Flying: Wie weit er von der Startposition aus nach links und nach rechts fliegt (in Tiles). Die Startposition liegt in der Mitte der Strecke.")]
    public float flyRange = 3f;
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

    [Header("Hitbox")]
    [Tooltip("Breite der gefährlichen Fläche als Anteil der Grafik (1 = ganze Grafik). Wird mit der Hitbox-Toleranz (Taste 0) multipliziert.")]
    [Range(0.2f, 1.5f)] public float hurtWidth = 1f;
    [Tooltip("Höhe der gefährlichen Fläche als Anteil der Grafik (1 = ganze Grafik). Wird mit der Hitbox-Toleranz (Taste 0) multipliziert.")]
    [Range(0.2f, 1.5f)] public float hurtHeight = 1f;

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

        if (mode == EnemyMode.Flying)
            MoveFlying();
        else
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

    /// <summary>Fliegen: Mit fester Geschwindigkeit bis zum Umkehrpunkt, dort kurz warten, dann zurück. Die Höhe bleibt gleich.</summary>
    private void MoveFlying()
    {
        if (pauseFrames > 0)
        {
            pauseFrames--;
            if (pauseFrames == 0)
                SetFacing(!facingRight);
            return;
        }

        float dir = facingRight ? 1f : -1f;
        float limit = startPosition.x + dir * Mathf.Max(0f, flyRange);
        float x = transform.position.x + dir * moveSpeed * Time.fixedDeltaTime;

        bool reachedEnd = dir > 0f ? x >= limit : x <= limit;
        if (reachedEnd)
        {
            x = limit;
            pauseFrames = Mathf.Max(1, turnPauseFrames);
        }

        SetMoving(!reachedEnd);
        transform.position = new Vector3(x, transform.position.y, transform.position.z);
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
        return HurtBoundsFor(GraphicBounds(), player != null ? player.HitboxScale : 1f);
    }

    /// <summary>Gefährliche Fläche zu gegebener Grafik und Toleranzfaktor. Läufer: an der Unterkante verankert, Flieger: in der Mitte.</summary>
    private Bounds HurtBoundsFor(Bounds graphic, float tolerance)
    {
        Vector3 size = new Vector3(graphic.size.x * hurtWidth * tolerance, graphic.size.y * hurtHeight * tolerance, 1f);
        float centerY = mode == EnemyMode.Flying ? graphic.center.y : graphic.min.y + size.y * 0.5f;
        return new Bounds(new Vector3(graphic.center.x, centerY, 0f), size);
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
        Bounds hurt = HurtBoundsFor(b, scale);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(hurt.center, new Vector3(hurt.size.x, hurt.size.y, 0f));

        if (mode == EnemyMode.Flying)
        {
            // Flugstrecke (cyan): von links nach rechts um die Startposition
            Vector3 origin = Application.isPlaying ? startPosition : transform.position;
            float range = Mathf.Max(0f, flyRange);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin + Vector3.left * range, origin + Vector3.right * range);
            Gizmos.DrawLine(origin + new Vector3(-range, -0.3f, 0f), origin + new Vector3(-range, 0.3f, 0f));
            Gizmos.DrawLine(origin + new Vector3(range, -0.3f, 0f), origin + new Vector3(range, 0.3f, 0f));
            return;
        }

        float dir = (Application.isPlaying ? facingRight : startFacingRight) ? 1f : -1f;
        Gizmos.color = Color.yellow;
        Vector3 foot = new Vector3(b.center.x + dir * b.extents.x, b.min.y + 0.1f, 0f);
        Gizmos.DrawLine(foot, foot + Vector3.down * (0.1f + ledgeCheckDepth));
        Gizmos.DrawLine(b.center, b.center + Vector3.right * dir * (b.extents.x + wallCheckDistance));
    }
}
