using UnityEngine.InputSystem;

/// <summary>
/// Tastatur-Belegung zur Laufzeit (F8: Versuchspersonen ohne Controller).
/// Die Datei PlayerInput.inputactions enthält nur Controller-Belegungen. Statt sie zu ändern (und die generierte Klasse neu zu erzeugen),
/// werden die Tastaturtasten beim Start des Spiels an die vorhandenen Aktionen angehängt. Der Controller bleibt unverändert.
/// Aufruf: einmal in Player.Awake, direkt nach "playerInput = new PlayerInput();" und vor dem Aktivieren der Eingabe.
///
/// Belegung: A/D oder Pfeil links/rechts = laufen, S oder Pfeil unten = nach unten (Spin Dash), Leertaste = springen, Shift = rennen.
/// </summary>
public static class KeyboardInput
{
    public static void AddTo(global::PlayerInput input)
    {
        // Bewegung: zwei Vektor-Kompositionen (WASD und Pfeiltasten). mode=1 liefert Digitalwerte ohne Normalisierung der Diagonalen.
        input.ingame.Movement.AddCompositeBinding("2DVector(mode=1)")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        input.ingame.Movement.AddCompositeBinding("2DVector(mode=1)")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");

        input.ingame.Jump.AddBinding("<Keyboard>/space");

        input.ingame.speedUp.AddBinding("<Keyboard>/leftShift");
        input.ingame.speedUp.AddBinding("<Keyboard>/rightShift");
    }
}
