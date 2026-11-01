using UnityEngine;

/// <summary>
/// Fallende Plattform (Testhilfe für Fehlertoleranz). Sobald die Figur auf ihr steht, läuft ein Zähler in Frames
/// (Fall Delay Frames, 60 Frames = 1 Sekunde). Danach fällt die Plattform mit gleichmäßiger Beschleunigung und nimmt die Figur
/// mit, solange sie darauf steht (Springen löst sie von der Plattform). Nach Fall Duration Frames verschwindet die Plattform,
/// die Figur fällt dann weiter. Nach jedem Respawn steht sie wieder an ihrer Startposition.
///
/// Einrichtung: Prefab "Falling_Platform" in die Szene ziehen. Das Prefab liegt auf der Layer "Ground", damit die Figur darauf
/// stehen kann (der Boden-Strahl des Players prüft die Ground Mask). Das Skript braucht den Collider als festen (nicht Trigger) Collider.
/// Die Figur wird nur über ihre Position mitgenommen, am Player-Skript wurde dafür nichts geändert.
/// Ein optionaler Animator bekommt den Trigger "deactivate", wenn der Fall beginnt.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class FallingPlatform : MonoBehaviour
{
    private enum PlatformState { Waiting, Countdown, Falling, Gone }

    [Tooltip("Frames zwischen dem Aufsetzen der Figur und dem Beginn des Falls (60 Frames = 1 Sekunde)")]
    public int fallDelayFrames = 5;
    [Tooltip("Fallbeschleunigung in Einheiten pro Sekunde² (1 Einheit = 1 Tile)")]
    public float fallAcceleration = 40f;
    [Tooltip("Höchste Fallgeschwindigkeit in Einheiten pro Sekunde")]
    public float maxFallSpeed = 20f;
    [Tooltip("Nach so vielen Frames Fall verschwindet die Plattform (bis zum nächsten Respawn)")]
    public int fallDurationFrames = 90;
    [Tooltip("Die Figur fällt mit, solange sie auf der Plattform steht")]
    public bool carryPlayer = true;
    [Tooltip("Abstand zwischen den Füßen der Figur und der Oberkante, bis zu dem sie als 'steht darauf' gilt (Einheiten)")]
    public float standTolerance = 0.15f;

    private BoxCollider2D col;
    private SpriteRenderer sr;
    private Animator anim;
    private Player player;
    private Vector3 startPosition;
    private PlatformState state = PlatformState.Waiting;
    private int framesLeft;
    private float fallSpeed;

    private void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        startPosition = transform.position;
    }

    private void OnEnable() => AttemptManager.Respawned += ResetPlatform;
    private void OnDisable() => AttemptManager.Respawned -= ResetPlatform;

    private void ResetPlatform()
    {
        state = PlatformState.Waiting;
        transform.position = startPosition;
        fallSpeed = 0f;
        col.enabled = true;
        if (sr != null)
            sr.enabled = true;
        if (anim != null)
        {
            anim.ResetTrigger("deactivate");
            anim.Rebind();
            anim.Update(0f);
        }
    }

    private bool PlayerStands()
    {
        if (player == null || player.isDead || !player.isGrounded)
            return false;

        Bounds body = player.BodyBounds;
        Bounds me = col.bounds;
        bool overlapsX = body.max.x > me.min.x && body.min.x < me.max.x;
        bool onTop = Mathf.Abs(body.min.y - me.max.y) <= standTolerance;
        return overlapsX && onTop;
    }

    private void FixedUpdate()
    {
        if (player == null)
            player = FindFirstObjectByType<Player>();

        switch (state)
        {
            case PlatformState.Waiting:
                if (PlayerStands())
                {
                    framesLeft = Mathf.Max(0, fallDelayFrames);
                    state = PlatformState.Countdown;
                    if (framesLeft == 0)
                        StartFall();
                }
                break;

            case PlatformState.Countdown:
                framesLeft--;
                if (framesLeft <= 0)
                    StartFall();
                break;

            case PlatformState.Falling:
                Fall();
                break;
        }
    }

    private void StartFall()
    {
        state = PlatformState.Falling;
        framesLeft = Mathf.Max(1, fallDurationFrames);
        fallSpeed = 0f;
        if (anim != null)
            anim.SetTrigger("deactivate");
    }

    private void Fall()
    {
        // "Steht darauf" wird vor der Bewegung geprüft, damit Plattform und Figur im selben Schritt um dieselbe Strecke sinken
        bool carry = carryPlayer && PlayerStands();

        fallSpeed = Mathf.Min(maxFallSpeed, fallSpeed + fallAcceleration * Time.fixedDeltaTime);
        Vector3 delta = Vector3.down * fallSpeed * Time.fixedDeltaTime;
        transform.position += delta;
        if (carry)
            player.rb.position += (Vector2)delta;

        framesLeft--;
        if (framesLeft <= 0)
        {
            state = PlatformState.Gone;
            col.enabled = false;
            if (sr != null)
                sr.enabled = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null)
            return;
        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.offset, box.size);
    }
}
