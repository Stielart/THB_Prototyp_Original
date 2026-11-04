using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Text in die Zwischenablage kopieren. Im Editor und in Desktop-Builds über Unity, im Browser-Build über ein kleines
/// JavaScript-Plugin (Assets/Plugins/WebGL/ClipboardWebGL.jslib), weil Unity das im Browser nicht kann.
/// Der Aufruf muss direkt nach einem Mausklick des Nutzers erfolgen, sonst blockt der Browser das Kopieren.
/// </summary>
public static class WebClipboard
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int CopyTextToClipboard(string text);
#endif

    /// <summary>Gibt true zurück, wenn das Kopieren geklappt hat.</summary>
    public static bool Copy(string text)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return CopyTextToClipboard(text) == 1;
#else
        GUIUtility.systemCopyBuffer = text;
        return true;
#endif
    }
}
