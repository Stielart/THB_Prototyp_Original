using System.Collections.Generic;

/// <summary>Art eines Schritts im Versuchsablauf.</summary>
public enum StepKind { Practice, Tolerance, Profile }

/// <summary>Ein Schritt des Versuchsablaufs: eine Szene mit festem Profil und fester Toleranz-Einstellung.</summary>
public class ExperimentStep
{
    /// <summary>0 = Übung, 1 bis 5 = Teile des Fragebogens.</summary>
    public int number;
    public string scene;
    public StepKind kind;
    /// <summary>0 = Direkt, 1 = Mittel, 2 = Momentum.</summary>
    public int profile;
    public bool tolerance;
    /// <summary>false: aktives Bremsen aus (Gegenrichtung wirkt wie Taste loslassen, nur passives Auslaufen).</summary>
    public bool activeBraking = true;
    /// <summary>Zusätzlicher Schalter für die Ledge Correction. Sie ist nur an, wenn auch tolerance an ist und dieser Wert true ist.</summary>
    public bool ledgeCorrection = true;
    /// <summary>Überschrift im Spiel. Sie entspricht dem Seitentitel im Fragebogen, damit die Person die Seite wiederfindet.</summary>
    public string title;
    /// <summary>Titel der zugehörigen Seite im Fragebogen (LamaPoll), genau so wie die Person ihn dort sieht.</summary>
    public string page;
    public string intro;
    public int capSeconds;
    /// <summary>0 = keine Begrenzung der Versuche.</summary>
    public int capAttempts;
    /// <summary>true: Zeit, Versuche und Früchte zählen erst ab dem ersten Checkpoint (davor liegt ein Übungsabschnitt im selben Level).</summary>
    public bool measureFromCheckpoint;
}

/// <summary>
/// Ablaufplan des Versuchs (F8): Welche Szene mit welchem Profil und welcher Toleranz kommt in welcher Reihenfolge?
/// Alles Zahlenwerte und Texte stehen hier an einer Stelle, damit sie nach dem Pilottest leicht angepasst werden können.
///
/// Ablauf je Person: Übung, Toleranzvergleich (zwei Durchgänge im selben Level, Mittel-Profil, Reihenfolge wechselt),
/// Profilvergleich (drei Profile im Präzisionslevel, Toleranz an, Reihenfolge der Profile wechselt).
/// Bremsvergleich: Zum Profil Mittel kommt ein zusätzlicher Durchgang ohne aktives Bremsen. Mittel mit und ohne Bremse stehen direkt
/// nacheinander an der Stelle von Mittel in der Profilreihenfolge, die Reihenfolge der beiden wechselt nach der Teilnehmer-Nummer.
/// Die Zuteilung ergibt sich aus der Teilnehmer-Nummer (Gegengewichtung): Je sechs aufeinanderfolgende Nummern decken alle sechs
/// Profilreihenfolgen ab, je zwölf Nummern zusätzlich beide Toleranzreihenfolgen.
/// </summary>
public static class ExperimentPlan
{
    // Namen der Szenen (müssen in den Build Settings stehen).
    public const string ScenePractice = "Uebung";
    public const string SceneTolerance = "Toleranz";
    // Präzisionslevel: je Profil eine eigene Szene (gleiche Geometrie, die Früchte zeigen den Weg des Profils). Index = Profil (0 Direkt, 1 Mittel, 2 Momentum).
    private static readonly string[] ScenePrecisionByProfile = { "Praezision_Direkt", "Praezision_Mittel", "Praezision_Momentum" };

    // Obergrenzen. Danach endet der Durchgang als "nicht geschafft".
    /// <summary>0 = kein Zeitlimit in der Übung. Beenden geht nach PracticeSkipAfterSeconds über einen Knopf.</summary>
    public const int PracticeCapSeconds = 0;
    public const int PracticeSkipAfterSeconds = 30;
    public const int ToleranceCapSeconds = 300;
    public const int ToleranceCapAttempts = 25;
    public const int ProfileCapSeconds = 240;
    public const int ProfileCapAttempts = 20;

    // Ledge Correction je Profil im Präzisionslevel (Index = Profil: 0 Direkt, 1 Mittel, 2 Momentum).
    // Momentum: aus, weil am Anfang des Levels ein Hindernis verlangt, den Dash aufzuladen (Ledge Correction würde es umgehen).
    private static readonly bool[] ProfileLedgeCorrection = { true, true, false };

    /// <summary>
    /// Je Profil: Zählt die Messung erst ab dem ersten Checkpoint? Direkt und Momentum haben am Levelanfang einen kurzen Übungsabschnitt,
    /// Mittel hatte schon eine Übung und beginnt direkt. Index = Profil (0 Direkt, 1 Mittel, 2 Momentum).
    /// </summary>
    private static readonly bool[] ProfileMeasureFromCheckpoint = { true, false, true };

    private static readonly int[][] ProfileOrders =
    {
        new[] { 0, 1, 2 },
        new[] { 0, 2, 1 },
        new[] { 1, 0, 2 },
        new[] { 1, 2, 0 },
        new[] { 2, 0, 1 },
        new[] { 2, 1, 0 },
    };

    public static int ProfileOrderIndex(int id) => (System.Math.Max(1, id) - 1) % 6;

    /// <summary>true: Im Toleranzvergleich kommt zuerst die Fassung mit Toleranz (Gruppe A), sonst zuerst ohne (Gruppe B).</summary>
    public static bool ToleranceFirst(int id)
    {
        int n = System.Math.Max(1, id) - 1;
        return (n + n / 6) % 2 == 0;
    }

    /// <summary>
    /// true: Beim Bremsvergleich kommt Mittel zuerst mit aktivem Bremsen, sonst zuerst ohne. Je sechs aufeinanderfolgende Nummern teilen
    /// sich 3 zu 3, die Hälften wechseln von Block zu Block.
    /// </summary>
    public static bool BrakeFirst(int id)
    {
        int n = System.Math.Max(1, id) - 1;
        return (n / 3 + n / 6) % 2 == 0;
    }

    /// <summary>
    /// Kurzcode der Bedingung, z. B. "A132m": Gruppe A/B der Toleranz, Reihenfolge der Profile (1 Direkt, 2 Mittel, 3 Momentum)
    /// und beim Bremsvergleich m = Mittel zuerst mit Bremse, o = zuerst ohne.
    /// </summary>
    public static string ConditionCode(int id)
    {
        int[] order = ProfileOrders[ProfileOrderIndex(id)];
        return (ToleranceFirst(id) ? "A" : "B") + (order[0] + 1) + (order[1] + 1) + (order[2] + 1) + (BrakeFirst(id) ? "m" : "o");
    }

    public static List<ExperimentStep> Build(int id)
    {
        bool firstOn = ToleranceFirst(id);
        int[] order = ProfileOrders[ProfileOrderIndex(id)];
        List<ExperimentStep> steps = new List<ExperimentStep>();

        steps.Add(new ExperimentStep
        {
            number = 0, scene = ScenePractice, kind = StepKind.Practice, profile = 1, tolerance = firstOn,
            title = "Übung", page = "Übungslevel",
            intro = "Hier lernst du die Steuerung kennen. Versuche, ein Gefühl für den Charakter zu bekommen. "
                  + "Du siehst hier alle Hindernisse, die dich in diesem Test erwarten werden. "
                  + "Je länger du die Sprungtaste hältst, desto höher springst du.\n\n"
                  + $"Erreiche die Flagge, um die Übung abzuschließen. Nach {PracticeSkipAfterSeconds} Sekunden kannst du die Übung über einen Knopf beenden. "
                  + "Ansonsten gibt es hier kein Zeitlimit.",
            capSeconds = PracticeCapSeconds, capAttempts = 0
        });

        for (int i = 0; i < 2; i++)
        {
            bool on = (i == 0) == firstOn;
            steps.Add(new ExperimentStep
            {
                number = i + 1, scene = SceneTolerance, kind = StepKind.Tolerance, profile = 1, tolerance = on,
                title = $"Erster Test, Teil {i + 1}/2", page = $"Erster Test, Teil {i + 1}/2",
                intro = (i == 0
                          ? "Du spielst dieses Level jetzt zweimal hintereinander und beantwortest nach jedem Durchgang kurze Fragen.\n\n"
                          : "Dasselbe Level noch einmal.\n\n")
                      + "Wie in der Übung musst du die Flagge erreichen. Checkpoints speichern deinen bisherigen Stand.\n\n"
                      + LimitText(ToleranceCapSeconds, ToleranceCapAttempts),
                capSeconds = ToleranceCapSeconds, capAttempts = ToleranceCapAttempts
            });
        }

        // Profilteile: Mittel kommt zweimal (mit und ohne aktives Bremsen), direkt nacheinander
        int partNumber = 3;
        for (int i = 0; i < 3; i++)
        {
            int profile = order[i];
            if (profile == 1)
            {
                bool withBrakeFirst = BrakeFirst(id);
                steps.Add(MakeProfileStep(partNumber++, profile, withBrakeFirst));
                steps.Add(MakeProfileStep(partNumber++, profile, !withBrakeFirst));
            }
            else
            {
                steps.Add(MakeProfileStep(partNumber++, profile, true));
            }
        }
        return steps;
    }

    private static ExperimentStep MakeProfileStep(int number, int profile, bool activeBraking)
    {
        return new ExperimentStep
        {
            number = number, scene = ScenePrecisionByProfile[profile], kind = StepKind.Profile, profile = profile, tolerance = true,
            activeBraking = activeBraking,
            ledgeCorrection = ProfileLedgeCorrection[profile],
            measureFromCheckpoint = ProfileMeasureFromCheckpoint[profile],
            title = $"Zweiter Test, Teil {number - 2}/4", page = $"Zweiter Test, Teil {number - 2}/4",
            intro = (number == 3 ? "Jetzt beginnt der zweite Test mit vier Durchgängen. Nach jedem Teil beantwortest du wieder kurze Fragen. " : "")
                  + (profile == 0 ? "" : "In diesem Level musst du von Wänden abspringen. ") // Direkt hat keinen Wandsprung (Profil wallJump aus), sein Level braucht ihn nicht
                  + "Das Level ist in allen Teilen im Kern gleich aufgebaut, die Figur fühlt sich aber jedes Mal etwas anders an. Früchte zeigen dir einen möglichen Weg."
                  + (ProfileMeasureFromCheckpoint[profile] ? " Am Anfang kommt ein kurzer Übungsabschnitt. Zeit und Versuche zählen erst ab dem ersten Checkpoint." : "")
                  + "\n\n"
                  + ProfileHint(profile) + BrakeHint(activeBraking) + LimitText(ProfileCapSeconds, ProfileCapAttempts),
            capSeconds = ProfileCapSeconds, capAttempts = ProfileCapAttempts
        };
    }

    private static string BrakeHint(bool activeBraking)
    {
        if (activeBraking)
            return "";
        return "In diesem Teil bremst die Figur nicht von selbst, wenn du in die Gegenrichtung drückst: Sie rutscht erst aus. Plane deine Landungen entsprechend.\n\n";
    }

    private static string ProfileHint(int profile)
    {
        // Momentum hat als einziges Profil eine Zusatzbewegung. Die anderen brauchen keinen Hinweis.
        if (profile == 2)
            return "Neu: der Spin-Dash. Nach unten halten, mehrmals Sprung drücken und dann nach unten loslassen. Die Figur rollt schnell los.\n\n";
        return "";
    }

    private static string LimitText(int seconds, int attempts)
    {
        string text = $"Der Durchgang endet automatisch nach {FormatMinutes(seconds)} Minuten";
        if (attempts > 0)
            text += $" oder nach {attempts} Versuchen";
        return text + ".";
    }

    private static string FormatMinutes(int seconds)
    {
        float m = seconds / 60f;
        return (m == (int)m) ? ((int)m).ToString() : m.ToString("0.#", System.Globalization.CultureInfo.GetCultureInfo("de-DE"));
    }

    // Texte für die Zwischenbildschirme.
    /// <summary>page = Seitentitel im Fragebogen (ExperimentStep.page).</summary>
    public static string QuestionnairePrompt(string page)
    {
        return $"Bitte wechsle jetzt in den Fragebogen (anderer Tab), öffne die Seite \"{page}\" und füge dort diesen Code ein. "
             + "Danach kommst du hierher zurück und klickst auf \"Weiter\".";
    }

    public static string StatusText(string status)
    {
        switch (status)
        {
            case "OK": return "Geschafft!";
            case "ZEIT": return "Die Zeit ist abgelaufen. Die vorzeitige Beendigung stellt für den Test ein valides Ergebnis dar. Also keine Sorge.";
            case "VERS": return "Die Versuche sind aufgebraucht. Die vorzeitige Beendigung stellt für den Test ein valides Ergebnis dar. Also keine Sorge.";
            case "UEB": return "Übung beendet.";
            default: return "";
        }
    }
}
