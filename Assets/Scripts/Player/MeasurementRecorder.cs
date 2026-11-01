#if UNITY_EDITOR // Nur im Editor: schreibt Dateien (nicht im Browser-Build erlaubt) und ist ein Prüfwerkzeug, kein Teil des Tests
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Messmodus (Anforderung F11). Mit der Taste M ein- und ausschalten.
/// Misst im Spiel automatisch:
///  - Frames bis zur Höchstgeschwindigkeit (Anlaufen aus dem Stand),
///  - Frames bis zum Stillstand (Taste loslassen aus Höchstgeschwindigkeit),
///  - Frames bis zum Nulldurchgang beim Bremsen / Wenden (Gegenrichtung aus Höchstgeschwindigkeit),
///  - den Verlauf jedes Sprungs (Höhe und Geschwindigkeit pro Physik-Frame).
/// Die Ergebnisse werden als CSV (Semikolon, Dezimalkomma, für deutsches Excel) in den Ordner "Messungen"
/// neben dem Ordner Assets geschrieben und kurz auf dem Bildschirm angezeigt.
/// Alle Zeiten sind Physik-Frames (60 Hz), damit sie mit den Frame-Werten der Analyse vergleichbar sind.
/// </summary>
[DefaultExecutionOrder(100)] // läuft nach Player.FixedUpdate, sieht also die Werte des aktuellen Frames
public class MeasurementRecorder : MonoBehaviour
{
    private enum RunPhase { Idle, Accelerating, AtMax, Coasting, Braking }

    private const string RunFile = "lauf_messung.csv";
    private const string JumpSummaryFile = "sprung_zusammenfassung.csv";
    private const string JumpCurveFile = "sprung_verlauf.csv";

    private static readonly CultureInfo De = new CultureInfo("de-DE");

    private Player player;
    private bool measuring;
    private string folder;
    private string sessionId;
    private readonly List<string> messages = new List<string>();

    // --- Laufmessung ---
    private RunPhase runPhase = RunPhase.Idle;
    private int runFrames;
    private float runTargetPx;
    private float runDirection;

    // --- Sprungaufzeichnung ---
    private bool jumpRecording;
    private int jumpNumber;
    private int jumpFrame;
    private float jumpStartY;
    private float jumpStartSpeedPx;
    private int heldFrames;
    private int releaseFrame;
    private float maxHeightPx;
    private int maxHeightFrame;
    private readonly List<string> jumpSamples = new List<string>();

    public void Initialize(Player playerRef)
    {
        player = playerRef;
        folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Messungen");
        sessionId = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.mKey.wasPressedThisFrame)
        {
            measuring = !measuring;
            runPhase = RunPhase.Idle;
            jumpRecording = false;
            AddMessage(measuring ? "Messmodus an" : "Messmodus aus");
        }
    }

    private void FixedUpdate()
    {
        if (!measuring || player == null)
            return;

        TrackRun();
        TrackJump();
    }

    // ------------------------------------------------------------------ Laufen

    private void TrackRun()
    {
        if (player.currentState != player.groundState)
        {
            runPhase = RunPhase.Idle;
            return;
        }

        float speedPx = player.UnitsToPixels(Mathf.Abs(player.currentSpeed));
        float input = player.xInput;
        bool pressing = Mathf.Abs(input) > 0.7f;
        float maxPx = player.UnitsToPixels(player.isRunning ? player.runMaxSpeedUnit : player.walkMaxSpeedUnit);

        switch (runPhase)
        {
            case RunPhase.Idle:
                if (speedPx < 1f && pressing)
                {
                    // Der erste Beschleunigungsschritt ist in diesem Frame schon passiert, daher beginnt die Zählung bei 1.
                    runPhase = RunPhase.Accelerating;
                    runFrames = 0;
                    runTargetPx = maxPx;
                    runDirection = Mathf.Sign(input);
                    goto case RunPhase.Accelerating;
                }
                break;

            case RunPhase.Accelerating:
                runFrames++;
                if (!pressing || Mathf.Sign(input) != runDirection)
                {
                    runPhase = RunPhase.Idle;
                    break;
                }
                if (speedPx >= runTargetPx * 0.999f)
                {
                    LogRun("Beschleunigung", runTargetPx, runFrames, runTargetPx / player.pixelWalkAcceleration * 60f);
                    runPhase = RunPhase.AtMax;
                }
                break;

            case RunPhase.AtMax:
                if (pressing && Mathf.Sign(input) != Mathf.Sign(player.currentSpeed))
                {
                    runPhase = RunPhase.Braking;
                    runFrames = 0;
                    runDirection = Mathf.Sign(player.currentSpeed);
                    goto case RunPhase.Braking;
                }
                if (Mathf.Abs(input) < 0.1f)
                {
                    runPhase = RunPhase.Coasting;
                    runFrames = 0;
                    goto case RunPhase.Coasting;
                }
                if (speedPx < runTargetPx * 0.999f)
                    runPhase = RunPhase.Idle; // z. B. Lauftaste losgelassen, keine saubere Messung
                break;

            case RunPhase.Coasting:
                runFrames++;
                if (pressing)
                {
                    runPhase = RunPhase.Idle;
                    break;
                }
                if (speedPx <= 0.01f)
                {
                    LogRun("Auslaufen", runTargetPx, runFrames, runTargetPx / player.pixelWalkDeceleration * 60f);
                    runPhase = RunPhase.Idle;
                }
                break;

            case RunPhase.Braking:
                runFrames++;
                if (Mathf.Sign(player.currentSpeed) != runDirection || speedPx <= 0.01f)
                {
                    bool brakeOn = player.activeBrakingEnabled;
                    float rate = brakeOn ? player.pixelTurnDeceleration : player.pixelWalkDeceleration;
                    LogRun(brakeOn ? "Bremsen" : "Bremsen (aktiv aus)", runTargetPx, runFrames, runTargetPx / rate * 60f);
                    runPhase = RunPhase.Idle;
                    break;
                }
                if (!pressing)
                    runPhase = RunPhase.Idle;
                break;
        }
    }

    private void LogRun(string measurement, float targetPx, int frames, float expectedFrames)
    {
        string line = string.Join(";", new string[]
        {
            sessionId,
            player.ActiveProfile.name,
            player.activeBrakingEnabled ? "an" : "aus",
            measurement,
            F(targetPx, 1),
            frames.ToString(),
            F(expectedFrames, 1),
            F(frames - expectedFrames, 1)
        });
        Append(RunFile, "Sitzung;Profil;AktivesBremsen;Messung;Ziel_px_pro_s;Frames;Soll_Frames;Abweichung", line);
        AddMessage($"{player.ActiveProfile.name}: {measurement} {frames} Frames (Soll {expectedFrames:F1})");
    }

    // ------------------------------------------------------------------ Springen

    private void TrackJump()
    {
        if (!jumpRecording)
        {
            if (player.currentState != player.jumpState)
                return;
            BeginJump();
        }

        bool landed = player.currentState == player.groundState;
        bool wall = player.currentState == player.wallSlideState
                    || player.currentState == player.wallJumpState
                    || player.currentState == player.wallSlideFallState;
        if (landed || wall)
        {
            EndJump(landed ? "Landung" : "Wand");
            return;
        }

        SampleJump();
    }

    private void BeginJump()
    {
        jumpRecording = true;
        jumpNumber++;
        jumpFrame = 0;
        jumpStartY = player.rb.position.y;
        jumpStartSpeedPx = player.UnitsToPixels(player.currentSpeed);
        heldFrames = 0;
        releaseFrame = -1;
        maxHeightPx = 0f;
        maxHeightFrame = 0;
        jumpSamples.Clear();
    }

    private void SampleJump()
    {
        float yPx = player.UnitsToPixels(player.rb.position.y - jumpStartY);
        float vyPx = player.UnitsToPixels(player.rb.linearVelocity.y);
        float vxPx = player.UnitsToPixels(player.rb.linearVelocity.x);
        bool hold = player.isHoldingJump;

        if (releaseFrame < 0)
        {
            if (hold) heldFrames++;
            else releaseFrame = jumpFrame;
        }

        if (yPx > maxHeightPx)
        {
            maxHeightPx = yPx;
            maxHeightFrame = jumpFrame;
        }

        jumpSamples.Add(string.Join(";", new string[]
        {
            sessionId,
            jumpNumber.ToString(),
            player.ActiveProfile.name,
            jumpFrame.ToString(),
            F(yPx, 2),
            F(vyPx, 1),
            F(vxPx, 1),
            player.currentState.GetType().Name,
            hold ? "1" : "0"
        }));
        jumpFrame++;
    }

    private void EndJump(string reason)
    {
        jumpRecording = false;
        MovementProfile p = player.ActiveProfile;
        float heightFigures = maxHeightPx / player.figureHeightPx;

        string summary = string.Join(";", new string[]
        {
            sessionId,
            jumpNumber.ToString(),
            p.name,
            player.activeBrakingEnabled ? "an" : "aus",
            reason,
            heldFrames.ToString(),
            releaseFrame < 0 ? "nicht losgelassen" : releaseFrame.ToString(),
            F(jumpStartSpeedPx, 1),
            F(maxHeightPx, 1),
            F(heightFigures, 2),
            F(p.jumpHeightFigures, 2),
            maxHeightFrame.ToString(),
            F(p.apexFrames, 1),
            jumpFrame.ToString(),
            F(p.apexFrames * 2f, 1)
        });
        Append(JumpSummaryFile,
            "Sitzung;Sprung;Profil;AktivesBremsen;Ende;Haltedauer_Frames;Losgelassen_nach_Frames;Absprunggeschw_px_pro_s;Hoehe_px;Hoehe_Figurenhoehen;Soll_Hoehe_Figurenhoehen_lang;Frames_bis_Zenit;Soll_Frames_bis_Zenit;Flugdauer_Frames;Soll_Flugdauer_lang",
            summary);

        if (jumpSamples.Count > 0)
        {
            Append(JumpCurveFile,
                "Sitzung;Sprung;Profil;Frame;Hoehe_px;vy_px_pro_s;vx_px_pro_s;Zustand;Taste",
                jumpSamples.ToArray());
        }

        AddMessage($"{p.name}: Sprung {maxHeightPx:F0} px ({heightFigures:F2} Fig.), Zenit nach {maxHeightFrame}, Flug {jumpFrame} Frames, Taste {heldFrames} Frames gehalten");
    }

    // ------------------------------------------------------------------ Hilfen

    private static string F(float value, int decimals)
    {
        return value.ToString("F" + decimals, De);
    }

    private void Append(string fileName, string header, params string[] lines)
    {
        try
        {
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, fileName);
            StringBuilder sb = new StringBuilder();
            if (!File.Exists(path))
                sb.AppendLine(header);
            foreach (string line in lines)
                sb.AppendLine(line);
            File.AppendAllText(path, sb.ToString(), new UTF8Encoding(true));
        }
        catch (IOException e)
        {
            AddMessage("Messdatei nicht beschreibbar (in Excel geöffnet?): " + e.Message);
        }
    }

    private void AddMessage(string text)
    {
        messages.Add(text);
        if (messages.Count > 4)
            messages.RemoveAt(0);
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10, 74, 900, 22), $"Messmodus [M]: {(measuring ? "an (Dateien im Ordner Messungen)" : "aus")}");
        for (int i = 0; i < messages.Count; i++)
            GUI.Label(new Rect(10, 96 + i * 20, 1200, 22), messages[i]);
    }
}
#endif
