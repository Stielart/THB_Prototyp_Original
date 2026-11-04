// Zwischenablage für den Browser-Build. GUIUtility.systemCopyBuffer funktioniert in WebGL nicht.
// Erst der klassische Weg über ein verstecktes Textfeld (läuft auch in eingebetteten Seiten wie itch.io),
// danach die moderne Zwischenablage-Schnittstelle als Ersatz.
mergeInto(LibraryManager.library, {
  CopyTextToClipboard: function (textPtr) {
    var text = UTF8ToString(textPtr);
    var ok = 0;
    try {
      var ta = document.createElement('textarea');
      ta.value = text;
      ta.setAttribute('readonly', '');
      ta.style.position = 'fixed';
      ta.style.top = '0';
      ta.style.left = '0';
      ta.style.opacity = '0';
      document.body.appendChild(ta);
      ta.focus();
      ta.select();
      ok = document.execCommand('copy') ? 1 : 0;
      document.body.removeChild(ta);
      var canvas = document.querySelector('canvas');
      if (canvas) canvas.focus();
    } catch (e) {
      ok = 0;
    }
    if (!ok && navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(text).catch(function () {});
      ok = 1;
    }
    return ok;
  }
});
