using UnityEngine;

public enum FruitType
{
    Apple,
    Banana,
    Cherry,
    Kiwi,
    Melon,
    Orange,
    Pineapple,
    Strawberry
}

/// <summary>
/// Einsammelbares Objekt (Trigger). Berührt die Figur die Frucht, wird sie eingesammelt: Sie verschwindet,
/// ein kurzer Effekt wird abgespielt und der Zähler oben steigt (siehe FruitManager).
/// Die Gesamtzahl entsteht automatisch aus allen Früchten, die in der Szene platziert sind.
/// Einrichtung: Prefab "Fruit" in die Szene ziehen. Die Frucht liegt bewusst auf der Layer Default
/// (nicht in der Ground Mask des Players), damit sie nichts blockiert.
/// Die Frucht wird nicht zerstört, sondern nur ausgeblendet. So kann sie nach einem Respawn wiederkehren (FruitManager.rule).
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class Fruit : MonoBehaviour
{
    [SerializeField] private FruitType fruitType;
    [Tooltip("Kurze Animation beim Einsammeln (Prefab Pickup_VFX).")]
    [SerializeField] private GameObject pickupEffect;
    [Tooltip("Symbol neben dem Zähler. Es reicht, wenn eine Frucht der Szene eines hat.")]
    [SerializeField] private Sprite hudIcon;

    private Animator anim;

    /// <summary>Wurde die Frucht eingesammelt (und ist gerade ausgeblendet)?</summary>
    public bool Collected { get; private set; }

    /// <summary>Vom FruitManager gesetzt: Die Frucht gilt als gesichert, weil danach ein Checkpoint erreicht wurde.</summary>
    public bool Secured { get; set; }

    public Sprite HudIcon => hudIcon;

    private void Reset()
    {
        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = 0.5f;
    }

    private void Awake() => anim = GetComponentInChildren<Animator>();

    // OnEnable statt Awake: Nach dem Wiedereinblenden (Respawn) startet der Animator neu und braucht den Wert erneut.
    private void OnEnable()
    {
        if (anim != null)
            anim.SetFloat("fruitIndex", (int)fruitType);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (Collected)
            return;
        if (other.GetComponentInParent<Player>() == null)
            return;
        if (FruitManager.Instance == null)
            return;

        FruitManager.Instance.Collect(this);
    }

    /// <summary>Wird vom FruitManager aufgerufen. Blendet die Frucht aus und zeigt den Effekt.</summary>
    public void Take()
    {
        Collected = true;
        Secured = false;
        if (pickupEffect != null)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);
        gameObject.SetActive(false);
    }

    /// <summary>Wird vom FruitManager aufgerufen. Die Frucht erscheint wieder an ihrem Platz.</summary>
    public void Restore()
    {
        Collected = false;
        Secured = false;
        gameObject.SetActive(true);
    }
}
