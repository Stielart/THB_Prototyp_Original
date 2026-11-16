using System;
using UnityEngine;

/// <summary>
/// Ein Bewegungsprofil bündelt die Designphilosophie eines Spiels: die horizontale Bewegung
/// (Achse "direkt/Kontrolle" bis "Momentum") und das Sprungverhalten (Höhe, Dauer, Abbruch, Gravitation).
///
/// Alle Längen (Geschwindigkeiten, Beschleunigungen, Sprunghöhe) stehen in SPIELPIXELN des jeweiligen
/// Spiels, also direkt vergleichbar mit der Tabelle vergleich in Kapitel 5. Beim Anwenden eines Profils
/// rechnet Player.ApplyProfile sie mit dem Faktor (Figurenhöhe Prototyp / figureHeight) auf die Größe der
/// Prototyp-Figur um. Dadurch fühlen sich die Profile relativ zur Figurengröße so an wie die Originale.
/// Alle Werte sind im Inspector (Player -> Profiles) einzeln einstellbar.
/// </summary>
[Serializable]
public class MovementProfile
{
    public string name = "Neues Profil";

    [Tooltip("Höhe der stehenden Spielfigur im Originalspiel in Spielpixeln (Skalierungsbasis)")]
    public float figureHeight = 19f;

    // ---------- Horizontale Bewegung (Spielpixel) ----------

    [Tooltip("Höchstgeschwindigkeit ohne Lauftaste in Spielpixeln pro Sekunde")]
    public float walkSpeed = 75f;

    [Tooltip("Höchstgeschwindigkeit mit Lauftaste in Spielpixeln pro Sekunde")]
    public float runSpeed = 135f;

    [Tooltip("Beschleunigung am Boden in Spielpixeln pro Sekunde². Frames bis Höchstgeschwindigkeit = Höchstgeschwindigkeit / Beschleunigung * 60")]
    public float acceleration = 337.5f;

    [Tooltip("Passives Auslaufen am Boden (keine Eingabe) in Spielpixeln pro Sekunde²")]
    public float passiveDeceleration = 562.5f;

    [Tooltip("Aktives Bremsen / Wenden am Boden (Gegenrichtung gedrückt) in Spielpixeln pro Sekunde²")]
    public float activeBraking = 1125f;

    [Tooltip("Luftkontrolle: Faktor auf die Beschleunigung am Boden")]
    public float airAccelerationMultiplier = 0.75f;

    [Tooltip("Luftkontrolle: Faktor auf das passive Auslaufen am Boden")]
    public float airDecelerationMultiplier = 0.05f;

    [Tooltip("Luftkontrolle: Faktor auf das aktive Bremsen/Wenden am Boden")]
    public float airTurnMultiplier = 0.65f;

    // ---------- Sprung ----------

    [Tooltip("Maximale Sprunghöhe im Stand in Figurenhöhen (Tabelle vergleich, Zeile Sprunghöhe max.)")]
    public float jumpHeightFigures = 2.9f;

    [Tooltip("Frames vom Absprung bis zum Zenit (halbe Flugdauer des langen Sprungs)")]
    public float apexFrames = 18f;

    [Tooltip("Sprunghöhe wächst mit der Geschwindigkeit (Momentum Jump)")]
    public bool speedJumpBonus = false;

    [Tooltip("Sprungabbruch: Lässt man die Sprungtaste im Aufstieg los, wird die Aufwärtsgeschwindigkeit gekappt")]
    public bool jumpCut = true;

    [Tooltip("Auf diesen Anteil der Absprunggeschwindigkeit wird beim Loslassen gekappt. Kleiner Wert = Abbruch wirkt fast im ganzen Aufstieg (stufenlos), Wert nahe 1 = nur in den ersten Frames (zwei feste Höhen)")]
    public float jumpCutSpeedFraction = 0.27f;

    [Tooltip("Zwei feste Sprunghöhen (statt stufenlosem Abbruch): Wird die Taste innerhalb des Fensters losgelassen, folgt der kurze Sprung, sonst der lange. Ersetzt dann den Abbruch über Jump Cut Speed Fraction")]
    public bool twoStageJump = false;

    [Tooltip("Höhe des kurzen Sprungs in Figurenhöhen (nur bei Two Stage Jump)")]
    public float shortJumpHeightFigures = 1.7f;

    [Tooltip("Entscheidungsfenster in Frames nach dem Absprung (nur bei Two Stage Jump). Loslassen innerhalb = kurzer Sprung, später = langer Sprung")]
    public int twoStageWindowFrames = 11;

    [Tooltip("Zenit-Verlangsamung: Faktor auf die Gravitation nahe dem Zenit, solange die Taste gehalten wird. 1 = aus, 0,5 = halbiert")]
    public float apexHangMultiplier = 0.2f;

    [Tooltip("Zenit-Bereich: Beträgt die vertikale Geschwindigkeit weniger als dieser Wert (Spielpixel pro Sekunde), greift die Verlangsamung")]
    public float apexHangSpeed = 80f;

    [Tooltip("Fallgravitation im Verhältnis zur Gravitation im Aufstieg (1 = gleich)")]
    public float fallGravityMultiplier = 1.5f;

    [Tooltip("Gravitation im Fall hängt von der Sprungtaste ab (gehalten = schwächer, wie bei Super Mario World)")]
    public bool holdFallGravity = true;

    [Tooltip("Faktor auf die Fallgravitation bei gehaltener Taste (nur wenn Hold Fall Gravity an ist, 0,5 = halbiert)")]
    public float holdFallGravityFactor = 0.47f;

    // ---------- Spin Dash (Verkürzung des Beschleunigungsaufbaus, Prinzip 3) ----------

    [Tooltip("Spin Dash: Im Stand nach unten halten und mit der Sprungtaste aufladen, beim Loslassen von unten schießt die Figur los")]
    public bool spinDash = false;

    [Tooltip("Anzahl Sprungtasten-Drücke bis zur vollen Ladung (Anzeige 'bereit')")]
    public int spinDashPressesForFull = 3;

    [Tooltip("Abschussgeschwindigkeit bei einem einzigen Druck, als Faktor auf die Höchstgeschwindigkeit")]
    public float spinDashMinSpeedFactor = 1.0f;

    [Tooltip("Abschussgeschwindigkeit bei voller Ladung, als Faktor auf die Höchstgeschwindigkeit")]
    public float spinDashMaxSpeedFactor = 1.5f;

    // ---------- Wand (Zusatzfaktor für das Momentum; nur beim einfachen Wandsprung, Taste V) ----------

    [Tooltip("Wandsprung in diesem Profil möglich")]
    public bool wallJump = true;

    [Tooltip("Wandrutschen in diesem Profil (der bisherige Slide: Hochlaufen, kurzes Haften, Abrutschen). Beim alten Wandsprung gilt der Slide immer. Aus: kein Slide, der Wandsprung geht trotzdem bei Wandkontakt in der Luft")]
    public bool wallSlide = true;

    [Tooltip("Höhe in Pixeln, die die Figur beim Wandkontakt im Aufstieg erst an der Wand hochrutscht (Slide-Phase, bevor sie haftet). Ohne Anlauftempo (Stillstand) gilt dieser Wert. 0 = kein Hochrutschen, die Figur haftet sofort. Nur wirksam bei Wall Slide an.")]
    public float wallRunUpHeight = 80f;

    [Tooltip("Höhe in Pixeln bei vollem Anlauftempo (siehe 'Wall Run Up Full Speed'). Dazwischen wird nach dem Tempo beim Wandkontakt geregelt (quadratisch: bei mittlerem Tempo deutlich näher am Stillstandswert), unabhängig von der Richtungstaste. 0 = nicht tempoabhängig, es gilt immer 'Wall Run Up Height'.")]
    public float wallRunUpHeightAtFullSpeed = 0f;

    [Tooltip("Tempo in Pixeln pro Sekunde, ab dem die volle Höhe gilt. 0 = Höchstgeschwindigkeit des Profils. Beim Momentum-Profil das Tempo des vollen Spin-Dashs (Höchsttempo mal Spin-Dash-Faktor).")]
    public float wallRunUpFullSpeed = 0f;

    [Tooltip("Winkel des Wandsprungs in Grad, von der Wand aus gemessen: 0 = senkrecht an der Wand hoch, 90 = waagerecht von der Wand weg")]
    [Range(0f, 90f)] public float wallJumpAngle = 51f;

    [Tooltip("Absprunggeschwindigkeit (Betrag) in Spielpixeln pro Sekunde. Sie wird nach dem Winkel in waagerecht und senkrecht zerlegt")]
    public float wallJumpSpeed = 167f;

    [Tooltip("Frames nach dem Wandsprung, in denen die Figur nicht steuerbar ist (die Geschwindigkeit bleibt konstant). Verhindert, dass man an derselben Wand sofort wieder abspringt")]
    public int wallJumpLockFrames = 10;

    /// <summary>
    /// Startwerte aus der Analyse (Tabelle vergleich, Kapitel 5), unter der Annahme einer
    /// linearen Rampe (Beschleunigung = Höchstgeschwindigkeit / Zeit). Werden im Messmodus geprüft.
    /// </summary>
    public static MovementProfile[] CreateDefaults()
    {
        return new MovementProfile[]
        {
            // Celeste: Höchstgeschwindigkeit nach ca. 4 bis 5 Frames, kaum Trägheit.
            // Sprung 2,3 Figurenhöhen, Flugdauer lang 39 Frames. Gravitation am Zenit bei gehaltener Taste halbiert (Thorson),
            // solange die vertikale Geschwindigkeit unter 40 Spielpixeln/s liegt (Wert aus Celestes veröffentlichtem Quelltext, am Graphen prüfen).
            // Luft: 0,18 gegenüber 0,3 Spielpixel/Frame² am Boden = 0,6. Figurenhöhe 11,9 (offen: Sprite 16 bis 17).
            new MovementProfile
            {
                name = "Direkt", figureHeight = 11.9f,
                // Tempo 70 statt Celestes 90 Spielpixel/s: Mit 90 war die Figur nach der Umrechnung auf die Figurengröße viel zu schnell
                // (14,7 statt 11,4 Tiles/s), bewusste Abweichung vom Original.
                walkSpeed = 70f, runSpeed = 70f,
                acceleration = 1080f, passiveDeceleration = 1080f, activeBraking = 1080f,
                airAccelerationMultiplier = 0.6f, airDecelerationMultiplier = 0.05f, airTurnMultiplier = 0.6f,
                jumpHeightFigures = 2.3f, apexFrames = 19.5f, speedJumpBonus = false,
                jumpCut = true, jumpCutSpeedFraction = 0.27f,
                apexHangMultiplier = 0.5f, apexHangSpeed = 40f,
                fallGravityMultiplier = 1f, holdFallGravity = false,
                // Wandsprung nach Celeste: waagerecht 130, senkrecht 105 Spielpixel/s (Quelltext, am Original prüfen) = 167 bei 51 Grad, 0,16 s (10 Frames) keine Steuerung
                wallJump = true, wallSlide = true, wallJumpAngle = 51f, wallJumpSpeed = 167f, wallJumpLockFrames = 10
            },
            // Super Mario World (Rennen): ca. 22 Frames Beschleunigung, 37 Frames Auslaufen, 5 Frames Bremsen.
            // Sprung 2,9 bis 3,4 Figurenhöhen (mit Tempo), Flugdauer lang 52 Frames. Gravitation 0,1875 bei gehaltener Taste
            // und 0,375 sonst: Aufstieg und Fall mit Taste gleich (Faktor 1), Fall ohne Taste doppelt (Faktor 2, gehalten 0,5).
            // Luft: Beschleunigen gleich wie am Boden (1,0), Abbremsen 0,30 gegenüber Boden 0,45 = 0,67. Figurenhöhe 19.
            new MovementProfile
            {
                name = "Mittel", figureHeight = 19f,
                walkSpeed = 75f, runSpeed = 135f,
                acceleration = 368f, passiveDeceleration = 219f, activeBraking = 1620f,
                airAccelerationMultiplier = 1f, airDecelerationMultiplier = 0.05f, airTurnMultiplier = 0.67f,
                jumpHeightFigures = 2.9f, apexFrames = 26f, speedJumpBonus = true,
                jumpCut = true, jumpCutSpeedFraction = 0.27f,
                apexHangMultiplier = 1f,
                fallGravityMultiplier = 2f, holdFallGravity = true, holdFallGravityFactor = 0.5f,
                // Wandsprung: Eigene Wahl (Super Mario World hat keinen Wandsprung), angelehnt an New Super Mario Bros.:
                // steiler Winkel (20 Grad von der Senkrechten), senkrechte Geschwindigkeit etwa 90 % des Absprungs, danach 20 Frames (0,33 s)
                // ohne Steuerung, in denen die Figur gut 3 Tiles von der Wand wegfliegt
                wallJump = true, wallSlide = true, wallRunUpHeight = 0f, wallJumpAngle = 20f, wallJumpSpeed = 280f, wallJumpLockFrames = 20
            },
            // Sonic Mania: ca. 127 Frames Beschleunigung, 117 Frames Auslaufen, 10 Frames Bremsen.
            // Sprung 2,7 Figurenhöhen, Flugdauer lang 63 Frames, Gravitation konstant. Genau zwei Höhen (2,7 und 1,7):
            // Wird die Taste in den ersten 11 Frames losgelassen, steigt die Figur nur bis 1,7 Figurenhöhen, sonst bis 2,7.
            // Das Fenster entspricht dem klassischen Sonic: Abbruchgrenze -4 bei Absprung -6,5 und Gravitation 0,21875 = 11,4 Frames (am Original prüfen).
            // Luft: Beschleunigen 0,10 gegenüber Boden 0,05 = 2,0, Abbremsen 0,165 gegenüber Boden 0,6 = 0,275. Figurenhöhe 39,7.
            new MovementProfile
            {
                name = "Momentum", figureHeight = 39.7f,
                walkSpeed = 360f, runSpeed = 360f,
                acceleration = 170f, passiveDeceleration = 185f, activeBraking = 2160f,
                airAccelerationMultiplier = 2f, airDecelerationMultiplier = 0.05f, airTurnMultiplier = 0.275f,
                jumpHeightFigures = 2.7f, apexFrames = 31.5f, speedJumpBonus = false,
                jumpCut = true, jumpCutSpeedFraction = 0.66f,
                twoStageJump = true, shortJumpHeightFigures = 1.7f, twoStageWindowFrames = 11,
                apexHangMultiplier = 1f,
                fallGravityMultiplier = 1f, holdFallGravity = false,
                // Spin Dash: verkürzt den langen Beschleunigungsaufbau (Sonic: Rolle am Boden). Zusatz des Prototyps, nicht in der Anforderungsliste.
                spinDash = true, spinDashPressesForFull = 3, spinDashMinSpeedFactor = 1.0f, spinDashMaxSpeedFactor = 1.5f,
                // Wandsprung: Eigene Wahl (Sonic Mania hat keinen Wandsprung): senkrecht etwa 90 % des Absprungs, waagerecht knapp unter der Höchstgeschwindigkeit, längere Sperre wegen des höheren Tempos
                wallJump = true, wallSlide = true, wallRunUpHeight = 10f, wallRunUpHeightAtFullSpeed = 120f, wallRunUpFullSpeed = 540f, wallJumpAngle = 40f, wallJumpSpeed = 480f, wallJumpLockFrames = 12
            }
        };
    }
}
