using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Was passiert mit eingesammelten Früchten, wenn die Figur gestorben ist?</summary>
public enum FruitRespawnRule
{
    /// <summary>Früchte, die vor dem letzten Checkpoint eingesammelt wurden, bleiben weg. Später eingesammelte kehren zurück.</summary>
    KeepUntilCheckpoint,
    /// <summary>Alle Früchte kehren nach jedem Tod zurück. Der Zähler beginnt bei jedem Versuch neu.</summary>
    AllReturn,
    /// <summary>Eingesammelte Früchte bleiben für den ganzen Durchlauf weg.</summary>
    NeverReturn
}

/// <summary>
/// Zählt die Früchte der Szene und zeigt oben mittig "eingesammelt / gesamt" an.
/// Wird beim Start automatisch erzeugt (wie der AttemptManager), in der Szene muss nichts eingerichtet werden.
/// Die Gesamtzahl wird beim Laden der Szene aus allen platzierten Fruit-Objekten ermittelt, auch aus ausgeblendeten.
/// Ist keine Frucht in der Szene, bleibt die Anzeige leer.
/// </summary>
public class FruitManager : MonoBehaviour
{
    public static FruitManager Instance { get; private set; }

    /// <summary>Regel für den Respawn. Ist ein Feld statt einer Einstellung im Editor, weil das Objekt automatisch entsteht;
    /// die spätere Bedingungsauswahl (F8) kann sie setzen.</summary>
    public static FruitRespawnRule rule = FruitRespawnRule.KeepUntilCheckpoint;

    public int Total => fruits.Count;
    public int Collected { get; private set; }

    private readonly List<Fruit> fruits = new List<Fruit>();
    private Sprite icon;
    private GUIStyle textStyle;
    private GUIStyle shadowStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (Instance != null)
            return;

        GameObject go = new GameObject("FruitManager");
        Instance = go.AddComponent<FruitManager>();
        DontDestroyOnLoad(go);
        Instance.Scan();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        AttemptManager.Respawned += OnRespawned;
        AttemptManager.CheckpointReached += OnCheckpointReached;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        AttemptManager.Respawned -= OnRespawned;
        AttemptManager.CheckpointReached -= OnCheckpointReached;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Scan();

    /// <summary>Sammelt alle Früchte der Szene (auch ausgeblendete) und setzt den Zähler auf 0.</summary>
    public void Scan()
    {
        fruits.Clear();
        fruits.AddRange(FindObjectsByType<Fruit>(FindObjectsInactive.Include, FindObjectsSortMode.None));

        icon = null;
        foreach (Fruit fruit in fruits)
        {
            if (fruit.HudIcon != null)
            {
                icon = fruit.HudIcon;
                break;
            }
        }
        Recount();
    }

    public void Collect(Fruit fruit)
    {
        if (fruit.Collected)
            return;
        fruit.Take();
        Recount();
    }

    private void OnCheckpointReached()
    {
        // Alles, was bis hierher eingesammelt wurde, gilt als gesichert.
        foreach (Fruit fruit in fruits)
            if (fruit.Collected)
                fruit.Secured = true;
    }

    private void OnRespawned()
    {
        if (rule == FruitRespawnRule.NeverReturn)
            return;

        foreach (Fruit fruit in fruits)
        {
            if (!fruit.Collected)
                continue;
            if (rule == FruitRespawnRule.AllReturn || !fruit.Secured)
                fruit.Restore();
        }
        Recount();
    }

    private void Recount()
    {
        Collected = 0;
        foreach (Fruit fruit in fruits)
            if (fruit.Collected)
                Collected++;
    }

    private void OnGUI()
    {
        if (Total == 0)
            return;

        if (textStyle == null)
        {
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            textStyle.normal.textColor = Color.white;
            shadowStyle = new GUIStyle(textStyle);
            shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
        }

        const float iconSize = 36f;
        const float textWidth = 90f;
        float x = (Screen.width - (iconSize + textWidth)) * 0.5f;
        float y = 8f;

        if (icon != null)
        {
            Texture2D tex = icon.texture;
            Rect r = icon.textureRect;
            Rect uv = new Rect(r.x / tex.width, r.y / tex.height, r.width / tex.width, r.height / tex.height);
            GUI.DrawTextureWithTexCoords(new Rect(x, y, iconSize, iconSize), tex, uv);
        }

        Rect textRect = new Rect(x + iconSize + 4f, y, textWidth, iconSize);
        string text = $"{Collected}/{Total}";
        GUI.Label(new Rect(textRect.x + 2f, textRect.y + 2f, textRect.width, textRect.height), text, shadowStyle);
        GUI.Label(textRect, text, textStyle);
    }
}
