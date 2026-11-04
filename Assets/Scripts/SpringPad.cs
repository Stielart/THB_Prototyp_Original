using UnityEngine;

/// <summary>
/// Sprungfeder. Berührt die Figur den Bereich der Feder (Trigger) und steigt sie nicht gerade nach oben, wird sie
/// in Richtung der Feder geschleudert (Player.SpringLaunch). Die Richtung ist die Oberseite des Objekts: Drehen des Objekts
/// um die Z-Achse dreht die Feder (90 Grad = nach links, -90 Grad = nach rechts, 180 Grad = nach unten).
/// Die Stärke wird als Höhe in Tiles angegeben, die ein senkrechter Absprung erreicht, und gilt für alle Profile gleich, damit
/// sich die Profile an derselben Feder vergleichen lassen. Taste und Tempo-Bonus spielen keine Rolle. Bei senkrechter Feder
/// bleibt die waagerechte Geschwindigkeit erhalten, bei schräger oder waagerechter Feder bleibt die Federgeschwindigkeit
/// für Horizontal Hold Frames Frames fest, danach gilt wieder die normale Luftsteuerung.
///
/// Einrichtung: Prefab "SpringPad" in die Szene ziehen, die Unterkante der Grafik auf den Boden setzen. Das Prefab liegt
/// auf der Standard-Layer (nicht auf der Ground-Layer des Players). Der Animator bekommt den Trigger "activate".
/// Gizmo im Editor (Objekt wählen): gelbe Linie = Höhe des Absprungs über der Oberkante der Feder.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class SpringPad : MonoBehaviour
{
    [Tooltip("Stärke als Höhe in Tiles (1 Tile = 1 Einheit), die ein senkrechter Absprung erreicht, gleich für alle Profile. Zum Vergleich: Sprung im Profil Mittel aus dem Stand 5,4 Tiles, mit Anlauf 6,3, im Profil Direkt etwa 4,5. Bei gedrehter Feder gilt dieselbe Geschwindigkeit in Richtung der Feder.")]
    [Min(0f)] public float launchHeight = 9f;
    [Tooltip("Nur bei schräger oder waagerechter Feder: Frames (60 = 1 Sekunde), in denen die waagerechte Geschwindigkeit fest bleibt, bevor die Luftsteuerung wieder wirkt")]
    [Min(0)] public int horizontalHoldFrames = 12;
    [Tooltip("Pause nach einem Absprung in Frames (60 Frames = 1 Sekunde), damit die Feder nicht doppelt auslöst")]
    public int cooldownFrames = 12;
    [Tooltip("Die Feder löst nur aus, wenn die Figur sich höchstens so schnell in Richtung der Feder bewegt (Einheiten pro Sekunde). Verhindert das Auslösen von hinten, zum Beispiel beim Hochspringen von unten durch eine Feder, die nach oben zeigt.")]
    public float maxRiseSpeed = 0.5f;

    private static readonly int ActivateHash = Animator.StringToHash("activate");

    private Animator anim;
    private int cooldown;

    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    private void FixedUpdate()
    {
        if (cooldown > 0)
            cooldown--;
    }

    private void OnTriggerEnter2D(Collider2D other) => TryLaunch(other);

    // Stay: Wer bei der Berührung noch aufsteigt, wird beim Fallen in der Feder ausgelöst
    private void OnTriggerStay2D(Collider2D other) => TryLaunch(other);

    private void TryLaunch(Collider2D other)
    {
        if (cooldown > 0)
            return;
        Player player = other.GetComponentInParent<Player>();
        if (player == null || player.isDead)
            return;
        Vector2 direction = transform.up;
        if (Vector2.Dot(player.rb.linearVelocity, direction) > maxRiseSpeed)
            return;

        player.SpringLaunch(direction, launchHeight, horizontalHoldFrames);
        cooldown = Mathf.Max(1, cooldownFrames);
        if (anim != null)
            anim.SetTrigger(ActivateHash);
    }

    private void OnDrawGizmosSelected()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null)
            return;
        // Start: Mitte der Oberseite des Triggers, Ende: launchHeight Tiles in Richtung der Feder
        Vector3 start = transform.TransformPoint(box.offset + Vector2.up * (box.size.y * 0.5f));
        Vector3 end = start + transform.up * launchHeight;
        Vector3 side = transform.right * 0.4f;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawLine(end - side, end + side);
    }
}
