using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Versuchszähler und Respawn (F6). Wird beim Start automatisch erzeugt, in der Szene muss nichts eingerichtet werden.
/// Ein Versuch endet mit der Berührung eines Stachels. Danach erscheint die Figur nach einer kurzen Pause (Player.respawnDelayFrames)
/// am letzten Checkpoint, ohne Checkpoint an der Startposition. Der Zähler beginnt bei 1 und erhöht sich bei jedem Treffer.
/// Er wird beim Laden einer Szene zurückgesetzt.
/// </summary>
public class AttemptManager : MonoBehaviour
{
    public static AttemptManager Instance { get; private set; }

    /// <summary>Wird ausgelöst, nachdem die Figur nach einem Tod zurückgesetzt wurde (z. B. damit Gegner zurückkehren).</summary>
    public static event System.Action Respawned;

    /// <summary>Wird ausgelöst, wenn die Figur einen neuen Checkpoint erreicht (z. B. damit Früchte als gesichert gelten).</summary>
    public static event System.Action CheckpointReached;

    /// <summary>Aktueller Versuch (beginnt bei 1).</summary>
    public int Attempts { get; private set; } = 1;

    private Player player;
    private Vector2 spawnPoint;
    private Checkpoint activeCheckpoint;
    private bool respawning;
    private GUIStyle rightStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (Instance != null)
            return;

        GameObject go = new GameObject("AttemptManager");
        Instance = go.AddComponent<AttemptManager>();
        DontDestroyOnLoad(go);
        Instance.ResetRun();
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ResetRun();

    /// <summary>Beginnt einen neuen Durchlauf: Zähler auf 1, kein Checkpoint, Startposition neu merken.</summary>
    public void ResetRun()
    {
        StopAllCoroutines();
        respawning = false;
        Attempts = 1;
        activeCheckpoint = null;
        player = FindFirstObjectByType<Player>();
        if (player != null)
            spawnPoint = player.transform.position;
    }

    public void SetCheckpoint(Checkpoint checkpoint)
    {
        if (activeCheckpoint == checkpoint)
            return;
        if (activeCheckpoint != null)
            activeCheckpoint.SetActiveVisual(false);
        activeCheckpoint = checkpoint;
        activeCheckpoint.SetActiveVisual(true);
        CheckpointReached?.Invoke();
    }

    /// <summary>Beendet den Versuch: zählt hoch, hält die Figur an und setzt sie nach der Pause zurück.</summary>
    public void KillPlayer(Player target)
    {
        if (respawning)
            return;
        respawning = true;
        Attempts++;
        StartCoroutine(RespawnRoutine(target));
    }

    private IEnumerator RespawnRoutine(Player target)
    {
        target.SetDead(true);
        int frames = Mathf.Max(1, target.respawnDelayFrames);
        for (int i = 0; i < frames; i++)
            yield return new WaitForFixedUpdate();

        Vector2 point = activeCheckpoint != null ? activeCheckpoint.RespawnPosition : spawnPoint;
        target.RespawnAt(point);
        respawning = false;
        Respawned?.Invoke();
    }

    private void OnGUI()
    {
        if (rightStyle == null)
            rightStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight, fontSize = 16 };
        GUI.Label(new Rect(Screen.width - 260, 10, 250, 28), $"Versuch {Attempts}", rightStyle);
    }
}
