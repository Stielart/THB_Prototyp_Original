using UnityEngine;

/// <summary>
/// Zielflagge (F8/F9). Berührt die Figur den Trigger, ist der Durchgang geschafft.
/// Im Versuchsablauf meldet sie das dem ExperimentManager (der Zeit, Versuche und Früchte festhält).
/// Im freien Spiel (Editor ohne Versuchsablauf) zeigt sie nur kurz "Ziel erreicht" an.
/// Der Trigger ist ein BoxCollider2D; Größe und Lage stehen im Prefab (2 x 3 Einheiten über dem Boden).
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class LevelGoal : MonoBehaviour
{
    private bool reached;
    private float messageUntil;
    private GUIStyle style;

    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (reached)
            return;
        if (other.GetComponentInParent<Player>() == null)
            return;

        reached = true;
        ExperimentManager manager = ExperimentManager.Instance;
        if (manager != null && manager.IsRunning)
        {
            manager.GoalReached();
        }
        else
        {
            messageUntil = Time.unscaledTime + 3f;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<Player>() != null)
            reached = false;
    }

    private void OnGUI()
    {
        if (Time.unscaledTime >= messageUntil)
            return;
        if (style == null)
            style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 32, fontStyle = FontStyle.Bold };
        int attempts = AttemptManager.Instance != null ? AttemptManager.Instance.Attempts : 0;
        GUI.Label(new Rect(0, Screen.height * 0.3f, Screen.width, 50), $"Ziel erreicht!  (Versuch {attempts})", style);
    }
}
