/******* ---------------------------------------------------------------------
 * *****    IAssistant      Intelligence Artificial-powered Office Assistant
 * ***** ---------------------------------------------------------------------
 * *****   
 * *****    IAssistantVoiceDictation.cs   Main source file - Editor windows
 * *****
 * ***** -- Author -----------------------------------------------------------
 * *****
 * ***** -- (c) Ilian Mas (ESILV A1) / June 2025
 * *****
 * ***** -- Major Changes ----------------------------------------------------
 * *****    05/07/25 - Ilian Mas - Initial version
 * ***** - -------------------------------------------------------------------
 ******/

using System;
using System.Speech.Recognition;
using System.Windows.Forms;

namespace IAssistant
{
    public class IAssistantVoiceDictation
    {
        private SpeechRecognitionEngine recognizer;
        private Action<string> callback;

        public IAssistantVoiceDictation(Action<string> callback)
        {
            this.callback = callback;
            recognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("fr-FR"));

            recognizer.SetInputToDefaultAudioDevice();

            recognizer.LoadGrammar(new DictationGrammar());

            recognizer.SpeechRecognized += (s, e) =>
            {
                if (e.Result != null && ! string.IsNullOrWhiteSpace(e.Result.Text) && e.Result.Confidence > IAssistant.iAssistantDictationConfidence) // ajustable
                {
                    callback(e.Result.Text); // texte dicté envoyé à l’éditeur
                }
            };
        }

        public void Start()
        {

            try
            {
                recognizer.RecognizeAsync(RecognizeMode.Multiple);
               
            }
            catch (Exception ex)
            {
                IAssistant.ErrorShow("ERROR_EDITOR_DICTATIONNOSTART", ex.Message);
            }
        }

        public void Stop()
        {
            recognizer.RecognizeAsyncStop();
        }
    }
}
