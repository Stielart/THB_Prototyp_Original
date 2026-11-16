using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Versuchsablauf (F8 bis F10). Führt eine Versuchsperson durch die Schritte aus ExperimentPlan:
/// Teilnehmer-Nummer eingeben, je Schritt Szene laden und Profil/Toleranz setzen, Anleitung zeigen, spielen lassen,
/// bei Ziel, Zeit- oder Versuchslimit beenden und einen Ergebnis-Code anzeigen, den die Person in den Fragebogen (LamaPoll) einträgt.
/// Es werden keine Dateien geschrieben, die Daten stecken nur im Code. Das funktioniert damit auch im Browser.
///
/// Der Manager erzeugt sich selbst (wie AttemptManager und FruitManager), in der Szene muss nichts eingerichtet werden.
/// Im fertigen Build startet er sofort mit der Nummern-Eingabe. Im Editor und in Development Builds bleibt er aus (freies Spielen
/// zum Testen); mit F10 wird er ein- und ausgeschaltet.
///
/// Außerhalb des Zustands "Playing" steht die Zeit still (timeScale 0) und die Spielereingabe ist gesperrt.
/// Gezählt wird in Physik-Schritten (60 pro Sekunde), unabhängig von der Bildrate. Die mittlere Bildrate wird mit gespeichert,
/// weil sie im Browser schwanken kann.
/// </summary>
public class ExperimentManager : MonoBehaviour
{
    public enum State { Off, CodeEntry, Loading, Ready, Playing, Interlude, Done, Error }

    public static ExperimentManager Instance { get; private set; }

    public State CurrentState => state;
    public bool IsRunning => state == State.Playing;

    private const float VirtualWidth = 1280f;
    private const float VirtualHeight = 720f;
    private const int PhysicsFps = 60;

    private State state = State.Off;
    private string idText = "";
    private string message = "";
    private int participantId;
    private List<ExperimentStep> steps;
    private int stepIndex;
    private readonly List<string> codes = new List<string>();
    private string lastStatus = "";
    private string copyInfo = "";

    // Messung im laufenden Schritt
    private int frames;
    private float unscaledTime;
    private int renderFrames;
    /// <summary>false: Der Schritt hat einen Übungsabschnitt. Zeit, Versuche und Früchte zählen erst ab dem ersten Checkpoint.</summary>
    private bool measuring = true;
    private int fruitBaseline;

    private ExperimentStep pendingStep;
    private Texture2D backdrop;
    private GUIStyle titleStyle, bodyStyle, buttonStyle, boxStyle, hudStyle, smallStyle, readyStyle;

    // Controller-Bilder auf dem Anleitungsbildschirm (Assets/Resources/ControllerHints, ein Bild je Profil: 0 Direkt, 1 Mittel, 2 Momentum).
    // Alle Bilder haben dieselbe Leinwand, damit der Controller von Bild zu Bild an derselben Stelle bleibt. Die Maße unten beziehen sich
    // auf die Grundgröße von 300 x 180 Pixeln; die Bilddateien selbst dürfen höher aufgelöst sein (aktuell 5-fach, 1500 x 900) und werden
    // dann weich herunterskaliert. Gemessen: oberste nicht transparente Zeile (Direkt 19, Mittel 6, Momentum 19; die unterste liegt bei
    // 121, 121 und 168) und Mitte der Breite (Pixel 32 bis 291).
    private static readonly string[] HintResourceNames =
        { "ControllerHints/Controller_Direkt", "ControllerHints/Controller_Mittel", "ControllerHints/Controller_Momentum" };
    private static readonly float[] HintContentTop = { 19f, 6f, 19f };
    private const float HintContentCenterX = 161.5f;
    private const float HintScale = 2f;   // Darstellungsgröße im virtuellen 1280 x 720 Bild: Grundgröße 300 x 180 mal 2 = 600 x 360
    private const float HintBaseWidth = 300f, HintBaseHeight = 180f;
    private const float HintY = 266f;     // obere Kante der Leinwand im virtuellen 1280 x 720 Bild (der Momentum-Controller endet bei 602, der Start-Knopf beginnt bei 610)
    private readonly Texture2D[] hintTextures = new Texture2D[3];
    private readonly bool[] hintTried = new bool[3];
    private int readyFontSize;

    private static readonly Key[] DigitKeys =
        { Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
    private static readonly Key[] NumpadKeys =
        { Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9 };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (Instance != null)
            return;

        GameObject go = new GameObject("ExperimentManager");
        Instance = go.AddComponent<ExperimentManager>();
        DontDestroyOnLoad(go);
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        Instance.EnterCodeEntry();
#endif
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        AttemptManager.CheckpointReached += OnCheckpointReached;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        AttemptManager.CheckpointReached -= OnCheckpointReached;
    }

    /// <summary>Erster Checkpoint in einem Schritt mit Übungsabschnitt: Ab hier beginnt die Messung (Zeit, Versuche, Früchte, Bildrate).</summary>
    private void OnCheckpointReached()
    {
        if (state != State.Playing || measuring)
            return;
        measuring = true;
        frames = 0;
        unscaledTime = 0f;
        renderFrames = 0;
        if (AttemptManager.Instance != null)
            AttemptManager.Instance.ResetAttempts();
        fruitBaseline = FruitManager.Instance != null ? FruitManager.Instance.Collected : 0;
    }

    // ------------------------------------------------------------------ Ablauf

    private void EnterCodeEntry()
    {
        StopAllCoroutines();
        idText = "";
        message = "";
        codes.Clear();
        state = State.CodeEntry;
        Freeze(true);
    }

    private void Abort()
    {
        StopAllCoroutines();
        state = State.Off;
        Freeze(false);
    }

    private void SubmitId()
    {
        if (!int.TryParse(idText, out int id) || id < 1 || id > 999)
        {
            message = "Bitte eine Zahl von 1 bis 999 eingeben.";
            return;
        }
        participantId = id;
        steps = ExperimentPlan.Build(id);
        codes.Clear();
        message = "";
        BeginStep(0);
    }

    private void BeginStep(int index)
    {
        stepIndex = index;
        pendingStep = steps[index];
        copyInfo = "";
        readyFontSize = 0;

        if (!Application.CanStreamedLevelBeLoaded(pendingStep.scene))
        {
            message = $"Die Szene \"{pendingStep.scene}\" ist nicht in den Build Settings eingetragen.";
            state = State.Error;
            Freeze(true);
            return;
        }

        state = State.Loading;
        Freeze(true);
        StartCoroutine(LoadRoutine(pendingStep));
    }

    private IEnumerator LoadRoutine(ExperimentStep step)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(step.scene);
        while (op != null && !op.isDone)
            yield return null;
        yield return null; // ein Bild warten, damit Start() der Szenenobjekte gelaufen ist
        state = State.Ready;
        Freeze(true);
    }

    /// <summary>Wird nach dem Laden einer Szene aufgerufen (vor Start): Profil und Toleranz dieses Schritts setzen.</summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (state != State.Loading || pendingStep == null)
            return;

        Player player = FindFirstObjectByType<Player>();
        if (player == null)
            return;

        bool t = pendingStep.tolerance;
        player.activeProfileIndex = pendingStep.profile;
        player.coyoteEnabled = t;
        player.jumpBufferEnabled = t;
        player.ledgeCorrectionEnabled = t && pendingStep.ledgeCorrection;
        player.cornerCorrectionEnabled = t;
        player.hitboxToleranceEnabled = t;
        player.activeBrakingEnabled = pendingStep.activeBraking;
        // Start() der Figur ruft ApplyProfile(activeProfileIndex) auf.
        SetPlayerInput(false);
    }

    private void StartPlaying()
    {
        frames = 0;
        unscaledTime = 0f;
        renderFrames = 0;
        lastStatus = "";
        measuring = !steps[stepIndex].measureFromCheckpoint;
        fruitBaseline = 0;
        if (AttemptManager.Instance != null)
            AttemptManager.Instance.ResetRun();
        state = State.Playing;
        Freeze(false);
    }

    /// <summary>Von der Zielflagge aufgerufen.</summary>
    public void GoalReached()
    {
        if (state == State.Playing)
            Finish("OK");
    }

    private void Finish(string status)
    {
        ExperimentStep s = steps[stepIndex];
        int attempts = AttemptManager.Instance != null ? AttemptManager.Instance.Attempts : 1;
        if (status == "VERS")
            attempts = Mathf.Max(1, attempts - 1); // der letzte Tod hat den Zähler schon erhöht
        int seconds = Mathf.RoundToInt(frames / (float)PhysicsFps);
        int fruits = FruitManager.Instance != null ? Mathf.Max(0, FruitManager.Instance.Collected - fruitBaseline) : 0;
        int fps = unscaledTime > 0f ? Mathf.RoundToInt(renderFrames / unscaledTime) : 0;

        string body = $"ID{participantId:D2}-{ExperimentPlan.ConditionCode(participantId)}-S{s.number}-T{(s.tolerance ? 1 : 0)}-P{s.profile + 1}-AB{(s.activeBraking ? 1 : 0)}"
                    + $"-V{attempts}-Z{seconds}-F{fruits}-{status}-B{fps}";
        int sum = 0;
        foreach (char c in body)
            sum += c;
        string code = body + "-K" + (sum % 97).ToString("D2");

        while (codes.Count <= stepIndex)
            codes.Add("");
        codes[stepIndex] = code;
        lastStatus = status;

        state = stepIndex + 1 < steps.Count ? State.Interlude : State.Done;
        Freeze(true);
    }

    private void NextStep()
    {
        if (stepIndex + 1 < steps.Count)
            BeginStep(stepIndex + 1);
    }

    // ------------------------------------------------------------------ Hilfen

    private void Freeze(bool frozen)
    {
        Time.timeScale = frozen ? 0f : 1f;
        SetPlayerInput(!frozen);
    }

    private void SetPlayerInput(bool on)
    {
        Player player = FindFirstObjectByType<Player>();
        if (player == null || player.playerInput == null)
            return;
        if (on)
            player.playerInput.Enable();
        else
            player.playerInput.Disable();
    }

    private static bool ConfirmPressed()
    {
        Keyboard kb = Keyboard.current;
        Gamepad gp = Gamepad.current;
        return (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
            || (gp != null && gp.startButton.wasPressedThisFrame);
    }

    private string AllCodesText()
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < codes.Count; i++)
        {
            if (string.IsNullOrEmpty(codes[i]))
                continue;
            sb.Append(steps[i].title).Append(": ").Append(codes[i]).Append('\n');
        }
        return sb.ToString();
    }

    private void CopyToClipboard(string text, string info)
    {
        bool ok = WebClipboard.Copy(text);
        copyInfo = ok ? info : "Kopieren hat nicht geklappt.";
    }

    // ------------------------------------------------------------------ Update

    private void FixedUpdate()
    {
        if (state == State.Playing && measuring)
            frames++;
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (kb != null && kb.f10Key.wasPressedThisFrame)
        {
            if (state == State.Off)
                EnterCodeEntry();
            else
                Abort();
            return;
        }
#endif
        switch (state)
        {
            case State.CodeEntry:
                if (kb != null)
                {
                    for (int d = 0; d < 10; d++)
                        if (kb[DigitKeys[d]].wasPressedThisFrame || kb[NumpadKeys[d]].wasPressedThisFrame)
                            AppendDigit(d);
                    if (kb.backspaceKey.wasPressedThisFrame)
                        Backspace();
                }
                if (ConfirmPressed())
                    SubmitId();
                break;

            case State.Ready:
                if (ConfirmPressed())
                    StartPlaying();
                break;

            case State.Interlude:
                if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                    NextStep();
                break;

            case State.Playing:
                unscaledTime += Time.unscaledDeltaTime;
                renderFrames++;
                ExperimentStep s = steps[stepIndex];
                if (!measuring)
                    break; // Übungsabschnitt: noch keine Grenzen
                if (s.capSeconds > 0 && frames >= s.capSeconds * PhysicsFps)
                    Finish("ZEIT");
                else if (s.capAttempts > 0 && AttemptManager.Instance != null && AttemptManager.Instance.Attempts > s.capAttempts)
                    Finish("VERS");
                break;
        }
    }

    private void AppendDigit(int d)
    {
        if (idText.Length < 3)
            idText += d.ToString();
        message = "";
    }

    private void Backspace()
    {
        if (idText.Length > 0)
            idText = idText.Substring(0, idText.Length - 1);
        message = "";
    }

    // ------------------------------------------------------------------ Oberfläche (IMGUI, skaliert auf 1280 x 720)

    private void EnsureStyles()
    {
        if (titleStyle != null)
            return;
        backdrop = new Texture2D(1, 1);
        backdrop.SetPixel(0, 0, new Color(0.08f, 0.1f, 0.16f, 1f));
        backdrop.Apply();

        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 44, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        titleStyle.normal.textColor = Color.white;
        bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, wordWrap = true, alignment = TextAnchor.UpperLeft };
        bodyStyle.normal.textColor = new Color(0.92f, 0.94f, 1f);
        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 28, fontStyle = FontStyle.Bold };
        boxStyle = new GUIStyle(GUI.skin.box) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        boxStyle.normal.textColor = Color.white;
        hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.LowerLeft };
        hudStyle.normal.textColor = new Color(1f, 1f, 1f, 0.85f);
        smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, wordWrap = true, alignment = TextAnchor.UpperLeft };
        smallStyle.normal.textColor = new Color(1f, 0.85f, 0.5f);
        readyStyle = new GUIStyle(bodyStyle);
    }

    /// <summary>Controller-Bild des Profils (null, wenn die Datei fehlt: dann zeigt der Bildschirm nur den Text).</summary>
    private Texture2D GetHintTexture(int profile)
    {
        if (profile < 0 || profile >= hintTextures.Length)
            return null;
        if (!hintTried[profile])
        {
            hintTried[profile] = true;
            Texture2D tex = Resources.Load<Texture2D>(HintResourceNames[profile]);
            if (tex != null)
                tex.filterMode = FilterMode.Trilinear; // Bild ist höher aufgelöst als die Anzeige: weich verkleinern statt Pixel zu überspringen
            hintTextures[profile] = tex;
        }
        return hintTextures[profile];
    }

    private void OnGUI()
    {
        if (state == State.Off)
            return;
        GUI.depth = -1000; // über den anderen Anzeigen zeichnen
        EnsureStyles();

        Matrix4x4 old = GUI.matrix;
        float scale = Mathf.Min(Screen.width / VirtualWidth, Screen.height / VirtualHeight);
        Vector3 offset = new Vector3((Screen.width - VirtualWidth * scale) * 0.5f, (Screen.height - VirtualHeight * scale) * 0.5f, 0f);

        if (state != State.Playing)
        {
            GUI.matrix = Matrix4x4.identity;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), backdrop);
        }

        GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, new Vector3(scale, scale, 1f));
        switch (state)
        {
            case State.Playing: DrawHud(); break;
            case State.CodeEntry: DrawCodeEntry(); break;
            case State.Loading: GUI.Label(new Rect(0, 300, VirtualWidth, 80), "Lade …", titleStyle); break;
            case State.Ready: DrawReady(); break;
            case State.Interlude: DrawInterlude(); break;
            case State.Done: DrawDone(); break;
            case State.Error: DrawError(); break;
        }
        GUI.matrix = old;
    }

    private void DrawHud()
    {
        ExperimentStep s = steps[stepIndex];
        int sec = frames / PhysicsFps;
        string timeText = measuring ? $"{sec / 60}:{sec % 60:D2}" : "Übungsabschnitt, Zeit startet am ersten Checkpoint";
        GUI.Label(new Rect(20, VirtualHeight - 50, 900, 40), $"{s.title}   {timeText}", hudStyle);

        if (s.kind == StepKind.Practice && sec >= ExperimentPlan.PracticeSkipAfterSeconds)
        {
            if (GUI.Button(new Rect(VirtualWidth - 260, VirtualHeight - 64, 240, 48), "Übung beenden", buttonStyle))
                Finish("UEB");
        }
    }

    private void DrawCodeEntry()
    {
        GUI.Label(new Rect(0, 90, VirtualWidth, 70), "Willkommen", titleStyle);
        GUI.Label(new Rect(240, 170, 800, 80), "Bitte gib deine Teilnehmer-Nummer ein (steht in der Einladung) und bestätige mit Enter oder \"OK\".", bodyStyle);
        GUI.Box(new Rect(490, 260, 300, 70), idText + "_", boxStyle);

        float x0 = 495f, y0 = 350f, w = 90f, h = 56f, gap = 10f;
        for (int i = 0; i < 12; i++)
        {
            int col = i % 3, row = i / 3;
            Rect r = new Rect(x0 + col * (w + gap), y0 + row * (h + gap), w, h);
            if (i < 9)
            {
                if (GUI.Button(r, (i + 1).ToString(), buttonStyle))
                    AppendDigit(i + 1);
            }
            else if (i == 9)
            {
                if (GUI.Button(r, "←", buttonStyle))
                    Backspace();
            }
            else if (i == 10)
            {
                if (GUI.Button(r, "0", buttonStyle))
                    AppendDigit(0);
            }
            else if (GUI.Button(r, "OK", buttonStyle))
                SubmitId();
        }
        if (!string.IsNullOrEmpty(message))
            GUI.Label(new Rect(240, 650, 800, 40), message, smallStyle);
    }

    private void DrawReady()
    {
        ExperimentStep s = steps[stepIndex];
        GUI.Label(new Rect(0, 20, VirtualWidth, 64), s.title, titleStyle);

        // Controller des Profils unter dem Text. Der Text bekommt den Platz darüber und wird bei Bedarf kleiner, damit er nicht in das Bild ragt.
        int profile = Mathf.Clamp(s.profile, 0, HintResourceNames.Length - 1);
        Texture2D hint = GetHintTexture(profile);
        const float textX = 90f, textW = 1100f, textTop = 88f;
        float textBottom = 600f;
        if (hint != null)
        {
            textBottom = HintY + HintContentTop[profile] * HintScale - 10f;
            GUI.DrawTexture(new Rect(VirtualWidth * 0.5f - HintContentCenterX * HintScale, HintY,
                                     HintBaseWidth * HintScale, HintBaseHeight * HintScale), hint, ScaleMode.StretchToFill, true);
        }
        if (readyFontSize == 0)
        {
            readyFontSize = 16;
            for (int size = 22; size >= 16; size -= 2)
            {
                readyStyle.fontSize = size;
                if (readyStyle.CalcHeight(new GUIContent(s.intro), textW) <= textBottom - textTop)
                {
                    readyFontSize = size;
                    break;
                }
            }
        }
        readyStyle.fontSize = readyFontSize;
        GUI.Label(new Rect(textX, textTop, textW, textBottom - textTop), s.intro, readyStyle);

        if (GUI.Button(new Rect(490, 610, 300, 70), "Start", buttonStyle))
            StartPlaying();
        GUI.Label(new Rect(0, 684, VirtualWidth, 30), "oder Enter drücken", new GUIStyle(smallStyle) { alignment = TextAnchor.UpperCenter });
    }

    private void DrawInterlude()
    {
        ExperimentStep s = steps[stepIndex];
        string code = codes[stepIndex];
        GUI.Label(new Rect(0, 60, VirtualWidth, 70), s.title + " beendet", titleStyle);
        GUI.Label(new Rect(190, 150, 900, 60), ExperimentPlan.StatusText(lastStatus), bodyStyle);
        GUI.Label(new Rect(190, 215, 900, 120), ExperimentPlan.QuestionnairePrompt(s.page), bodyStyle);
        GUI.Box(new Rect(140, 350, 1000, 80), code, new GUIStyle(boxStyle) { fontSize = 30 });
        if (GUI.Button(new Rect(490, 450, 300, 60), "Code kopieren", buttonStyle))
            CopyToClipboard(code, "Code kopiert.");
        if (!string.IsNullOrEmpty(copyInfo))
            GUI.Label(new Rect(190, 515, 900, 60), copyInfo + " Falls das Einfügen nicht klappt: bitte abtippen (Groß-/Kleinschreibung egal).", smallStyle);
        if (GUI.Button(new Rect(490, 610, 300, 70), "Weiter", buttonStyle))
            NextStep();
    }

    private void DrawDone()
    {
        GUI.Label(new Rect(0, 40, VirtualWidth, 70), "Geschafft – vielen Dank!", titleStyle);
        GUI.Label(new Rect(190, 120, 900, 120),
            "Bitte wechsle jetzt in den Fragebogen und öffne die Seite \"Abschlussfragen\". Hier sind noch einmal alle Codes, falls du sie dort einfügen möchtest. Vielen Dank fürs Spielen :)", bodyStyle);
        GUI.Box(new Rect(100, 250, 1080, 300), AllCodesText(), new GUIStyle(boxStyle) { fontSize = 20, alignment = TextAnchor.UpperLeft });
        if (GUI.Button(new Rect(490, 590, 300, 60), "Alle Codes kopieren", buttonStyle))
            CopyToClipboard(AllCodesText(), "Alle Codes kopiert.");
        if (!string.IsNullOrEmpty(copyInfo))
            GUI.Label(new Rect(190, 660, 900, 40), copyInfo, smallStyle);
    }

    private void DrawError()
    {
        GUI.Label(new Rect(0, 150, VirtualWidth, 70), "Fehler", titleStyle);
        GUI.Label(new Rect(190, 250, 900, 200), message, bodyStyle);
        if (GUI.Button(new Rect(490, 500, 300, 70), "Zurück", buttonStyle))
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Abort();
#else
            EnterCodeEntry();
#endif
        }
    }
}
