using UnityEngine;

namespace Duzhe.UI
{
    /// Access to TXT_UI.json strings (via DialogueLibrary). Every UI text in
    /// code goes through here so shipped copy lives in the delivery JSON.
    public static class UiText
    {
        public static string Get(string key, string fallback)
        {
            Dialogue.DialogueLibrary lib = Dialogue.DialogueLibrary.Instance;
            if (lib == null)
            {
                Dialogue.DialogueLibrary.Load();
                lib = Dialogue.DialogueLibrary.Instance;
            }
            if (lib == null) return fallback;
            return lib.GetUiString(key, fallback);
        }
    }
}
