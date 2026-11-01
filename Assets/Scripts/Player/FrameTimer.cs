using UnityEngine;

/// <summary>
/// Zähler in Physik-Frames (statt Sekunden). Wird für Coyote Time und Jump Buffer benutzt, damit die Fenster
/// exakt n Frames lang sind und sich auf die Frame-Werte der Analyse beziehen lassen.
/// Start(n) öffnet ein Fenster, in dem n Prüfungen (je Physik-Frame eine) IsActive erhalten.
/// Tick() muss einmal pro FixedUpdate NACH den Prüfungen aufgerufen werden.
/// </summary>
[System.Serializable]
public struct FrameTimer
{
    [SerializeField] private int framesLeft;

    public void Start(int frames) => framesLeft = frames;

    public void Tick()
    {
        if (framesLeft > 0)
            framesLeft--;
    }

    public bool IsActive => framesLeft > 0;

    public void Consume() => framesLeft = 0;
}
