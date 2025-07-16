using System;
using System.Speech.Recognition;
using System.Windows.Forms;

namespace AIMailer
{
    public class AIMailerVoiceDictation
    {
        private SpeechRecognitionEngine recognizer;
        private Action<string> callback;

        public AIMailerVoiceDictation(Action<string> callback)
        {
            this.callback = callback;
            recognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("fr-FR"));

            recognizer.SetInputToDefaultAudioDevice();

            recognizer.LoadGrammar(new DictationGrammar());

            recognizer.SpeechRecognized += (s, e) =>
            {
                if (e.Result != null && ! string.IsNullOrWhiteSpace(e.Result.Text) && e.Result.Confidence > AIMailer.aiMailerDictationConfidence) // ajustable
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
                MessageBox.Show("Erreur de démarrage de la dictée : " + ex.Message);
            }
        }

        public void Stop()
        {
            recognizer.RecognizeAsyncStop();
        }
    }
}
