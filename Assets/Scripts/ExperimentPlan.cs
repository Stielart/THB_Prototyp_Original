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
    public string title;
    public string intro;
    public int capSeconds;
    /// <summary>0 = keine Begrenzung der Versuche.</summary>
    public int capAttempts;
}

/// <summary>
/// Ablaufplan des Versuchs (F8): Welche Szene mit welchem Profil und welcher Toleranz kommt in welcher Reihenfolge?
/// Alles Zahlenwerte und Texte stehen hier an einer Stelle, damit sie nach dem Pilottest leicht angepasst werden können.
///
/// Ablauf je Person: Übung, Toleranzvergleich (zwei Durchgänge im selben Level, Mittel-Profil, Reihenfolge wechselt),
/// Profilvergleich (drei Durchgänge im Präzisionslevel, Toleranz an, Reihenfolge der Profile wechselt).
/// Die Zuteilung ergibt sich aus der Teilnehmer-Nummer (Gegengewichtung): Je sechs aufeinanderfolgende Nummern decken alle sechs
/// Profilreihenfolgen ab, je zwölf Nummern zusätzlich beide Toleranzreihenfolgen.
/// </summary>
public static class ExperimentPlan
{
    // Namen der Szenen (müssen in den Build Settings stehen).
    public const string ScenePractice = "Uebung";
    public const string SceneTolerance = "Toleranz";
    public const string ScenePrecision = "Praezision";

    // Obergrenzen. Danach endet der Durchgang als "nicht geschafft".
    public const int PracticeCapSeconds = 150;
    public const int PracticeSkipAfterSeconds = 30;
    public const int ToleranceCapSeconds = 300;
    public const int ToleranceCapAttempts = 25;
    public const int ProfileCapSeconds = 240;
    public const int ProfileCapAttempts = 20;

    private static readonly int[][] ProfileOrders =
    {
        new[] { 0, 1, 2 },
        new[] { 0, 2, 1 },
        new[] { 1, 0, 2 },
        new[] { 1, 2, 0 },
        new[] { 2, 0, 1 },
        new[] { 2, 1, 0 },
    };

    private const string Controls =
        "Steuerung (Tastatur): A/D oder Pfeiltasten = laufen, Leertaste = springen (länger halten = höher), Shift = rennen.\n" +
        "Controller: Stick oder Steuerkreuz = laufen, A = springen, X = rennen.";

    public static int ProfileOrderIndex(int id) => (System.Math.Max(1, id) - 1) % 6;

    /// <summary>true: Im Toleranzvergleich kommt zuerst die Fassung mit Toleranz (Gruppe A), sonst zuerst ohne (Gruppe B).</summary>
    public static bool ToleranceFirst(int id)
    {
        int n = System.Math.Max(1, id) - 1;
        return (n + n / 6) % 2 == 0;
    }

    /// <summary>Kurzcode der Bedingung, z. B. "A132": Gruppe A/B und die Reihenfolge der Profile (1 Direkt, 2 Mittel, 3 Momentum).</summary>
    public static string ConditionCode(int id)
    {
        int[] order = ProfileOrders[ProfileOrderIndex(id)];
        return (ToleranceFirst(id) ? "A" : "B") + (order[0] + 1) + (order[1] + 1) + (order[2] + 1);
    }

    public static List<ExperimentStep> Build(int id)
    {
        bool firstOn = ToleranceFirst(id);
        int[] order = ProfileOrders[ProfileOrderIndex(id)];
        List<ExperimentStep> steps = new List<ExperimentStep>();

        steps.Add(new ExperimentStep
        {
            number = 0, scene = ScenePractice, kind = StepKind.Practice, profile = 1, tolerance = firstOn,
            title = "Übung",
            intro = "Hier lernst du die Steuerung kennen. Dieser Teil wird nicht bewertet, probiere alles in Ruhe aus: laufen, springen, "
                  + "Früchte sammeln, einen Checkpoint berühren, einen Pilz von oben anspringen. Stacheln und Abgründe setzen dich zum letzten "
                  + "Checkpoint zurück.\n\n" + Controls + "\n\n"
                  + $"Ziel ist die Zielflagge. Nach {PracticeSkipAfterSeconds} Sekunden kannst du die Übung über einen Knopf beenden, "
                  + $"spätestens endet sie nach {FormatMinutes(PracticeCapSeconds)} Minuten.",
            capSeconds = PracticeCapSeconds, capAttempts = 0
        });

        for (int i = 0; i < 2; i++)
        {
            bool on = (i == 0) == firstOn;
            steps.Add(new ExperimentStep
            {
                number = i + 1, scene = SceneTolerance, kind = StepKind.Tolerance, profile = 1, tolerance = on,
                title = $"Teil {i + 1}",
                intro = "Erreiche die Zielflagge am Ende des Levels. Stacheln und Abgründe setzen dich zum letzten Checkpoint zurück. "
                      + "Früchte sind optional.\n\n"
                      + LimitText(ToleranceCapSeconds, ToleranceCapAttempts),
                capSeconds = ToleranceCapSeconds, capAttempts = ToleranceCapAttempts
            });
        }

        for (int i = 0; i < 3; i++)
        {
            int profile = order[i];
            steps.Add(new ExperimentStep
            {
                number = 3 + i, scene = ScenePrecision, kind = StepKind.Profile, profile = profile, tolerance = true,
                title = $"Teil {3 + i}",
                intro = "Erreiche die Zielflagge. In diesem Level kannst du dich an Wänden abstoßen: in der Luft gegen die Wand drücken und springen. "
                      + "Das Level ist in allen drei Teilen gleich, die Figur fühlt sich aber jedes Mal etwas anders an.\n\n"
                      + ProfileHint(profile) + LimitText(ProfileCapSeconds, ProfileCapAttempts),
                capSeconds = ProfileCapSeconds, capAttempts = ProfileCapAttempts
            });
        }
        return steps;
    }

    private static string ProfileHint(int profile)
    {
        // Momentum hat als einziges Profil eine Zusatzbewegung. Die anderen brauchen keinen Hinweis.
        if (profile == 2)
            return "Zusatz in diesem Teil: Spin-Dash. Nach unten halten und mehrmals Sprung drücken, dann nach unten loslassen: "
                 + "die Figur rollt schnell los.\n\n";
        return "";
    }

    private static string LimitText(int seconds, int attempts)
    {
        string text = $"Der Durchgang endet automatisch nach {FormatMinutes(seconds)} Minuten";
        if (attempts > 0)
            text += $" oder nach {attempts} Versuchen";
        return text + ". Das ist normal und kein Fehler von dir.";
    }

    private static string FormatMinutes(int seconds)
    {
        float m = seconds / 60f;
        return (m == (int)m) ? ((int)m).ToString() : m.ToString("0.#", System.Globalization.CultureInfo.GetCultureInfo("de-DE"));
    }

    // Texte für die Zwischenbildschirme.
    public static string QuestionnairePrompt(int number)
    {
        if (number == 0)
            return "Die Übung ist beendet. Trage bitte diesen Code im Fragebogen im Abschnitt zur Übung ein. Danach geht es weiter.";
        return $"Bitte öffne jetzt den Fragebogen, fülle Teil {number} aus und trage dort diesen Code ein. Danach klickst du hier auf \"Weiter\".";
    }

    public static string StatusText(string status)
    {
        switch (status)
        {
            case "OK": return "Geschafft!";
            case "ZEIT": return "Die Zeit ist abgelaufen. Das ist normal und kein Fehler von dir.";
            case "VERS": return "Die Versuche sind aufgebraucht. Das ist normal und kein Fehler von dir.";
            case "UEB": return "Übung beendet.";
            default: return "";
        }
    }
}
