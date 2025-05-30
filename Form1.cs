using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AiMailer
{
    public class IAConfig
    {
        public string Label { get; set; }       // Texte du bouton
        public string Prompt { get; set; }      // Prompt système à envoyer à l'IA
        public string Model { get; set; }       // Modèle IA à utiliser
    }

    public partial class Form1 : Form
    {
        private Dictionary<string, IAConfig> aiMailerIAActions = new Dictionary<string, IAConfig>
            {
            /*
                { "Traduire", "Traduire", "Traduis précisément ce texte en français.", "nous-hermes-2-mistral-7b-dpo" },
                { "Résumer", "Résumer", "Fais un résumé clair et concis du texte suivant.", "mistral-7b-instruct-v0.3@iq3_m" },
                { "Brainstorm", "Brainstorm", "Génère des idées créatives à partir de ce texte.", "deepseek-coder-6.7b-instruct" },
                { "Élaborer", "Élaborer", "Développe cette idée en détail.", "deepseek-coder-6.7b-instruct" },
                { "Style étudiant", "Style étudiant", "Reformule ce texte dans un style simple et accessible.", "nous-hermes-2-mistral-7b-dpo" },
                { "Style pro", "Style pro", "Reformule ce texte dans un style professionnel.", "nous-hermes-2-mistral-7b-dpo" },
                { "Rendez-vous", "Rendez-vous", "Rédige un message pour proposer un rendez-vous professionnel.", "openchat-3.5-1210" },
             */
                { "Traduire", new IAConfig { Label = "Traduire", Prompt = "Traduis précisément ce texte en français.", Model = "nous-hermes-2-mistral-7b-dpo" } },
                { "Résumer", new IAConfig { Label = "Résumer", Prompt = "Fais un résumé clair et concis du texte suivant.", Model = "mistral-7b-instruct-v0.3@iq3_m" } },
                { "Brainstorm", new IAConfig { Label = "Brainstorm", Prompt = "Génère des idées créatives à partir de ce texte.", Model = "deepseek-coder-6.7b-instruct" } },
                { "Élaborer", new IAConfig { Label = "Élaborer", Prompt = "Développe cette idée en détail.", Model = "deepseek-coder-6.7b-instruct" } },
                { "Style étudiant", new IAConfig { Label = "Style étudiant", Prompt = "Reformule ce texte dans un style simple et accessible.", Model = "nous-hermes-2-mistral-7b-dpo" } },
                { "Style pro", new IAConfig { Label = "Style pro", Prompt = "Reformule ce texte dans un style professionnel.", Model = "nous-hermes-2-mistral-7b-dpo" } },
                { "Rendez-vous", new IAConfig { Label = "Rendez-vous", Prompt = "Rédige un message pour proposer un rendez-vous professionnel.", Model = "openchat-3.5-1210" } }
            };

        private Dictionary<string, string> aiMailerErrorMsgs = new Dictionary<string, string>
        {
                { "ERROR_EDITOR_EMPTYSELECTION" , "Veuillez entrer ou sélectionner du texte." }
        };



        private string AIMailerName = "AIMailer";
        private string AIMailerEditorName = "AIMailerEditor";
        private string AIMailerActionPanelName = "AIMailerActionPanel";

        private TextBox AIMailerEditor;

        public Form1()
        {
            InitializeComponent();
            InitialiserInterface();
        }

        // 🖼️ Initialise l'interface graphique
        private void InitialiserInterface()
        {
            this.Size = new Size(800, 450);
            this.Text = AIMailerName;

            // Zone de texte principale
            AIMailerEditor = new TextBox
            {
                Multiline = true,
                Name = AIMailerEditorName,
                Size = new Size(550, 350),
                Location = new Point(200, 30),
                ScrollBars = ScrollBars.Vertical
            };
            this.Controls.Add(AIMailerEditor);

            // Panneau latéral pour les boutons IA
            Panel actionPanel = new Panel
            {
                Name = AIMailerActionPanelName,
                Size = new Size(180, 370),
                Location = new Point(10, 30),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.LightGray
            };
            this.Controls.Add(actionPanel);

            // Ajout dynamique des boutons
            AjouterTousLesBoutons(actionPanel);
        }

        // ➕ Ajoute tous les boutons définis dans aiMailerIAActions
        private void AjouterTousLesBoutons(Panel panel)
        {
            int y = 10;
            foreach (var config in aiMailerIAActions.Values)
            {
                Button btn = new Button
                {
                    Text = config.Label,
                    Size = new Size(150, 30),
                    Location = new Point(15, y),
                    Tag = config
                };

                btn.Click += async (s, e) => await AppelerIAAsync((IAConfig)((Button)s).Tag);
                panel.Controls.Add(btn);
                y += 40;
            }
        }

        private void AIMailerErrorShow(string msgKey)
        {
            string msgLabel = "";

            if( ! aiMailerErrorMsgs.TryGetValue(msgKey,out msgLabel) )
                msgLabel = "Unknown error label";
            MessageBox.Show(msgLabel);
        }

        // 🤖 Appelle l'IA avec le modèle et prompt définis dans config
        private async Task AppelerIAAsync(IAConfig config)
        {
            string userInput = string.IsNullOrWhiteSpace(AIMailerEditor.SelectedText) ? AIMailerEditor.Text : AIMailerEditor.SelectedText;
            if (string.IsNullOrWhiteSpace(userInput))
            {
                AIMailerErrorShow("ERROR_EDITOR_EMPTYSELECTION");
                return;
            }

            var requestBody = new
            {
                model = config.Model,
                messages = new[]
                {
                    new { role = "system", content = config.Prompt + " Réponds en français." },
                    new { role = "user", content = userInput }
                },
                temperature = 0.7
            };

            using (var client = new HttpClient())
            {
                try
                {
                    client.BaseAddress = new Uri("http://127.0.0.1:1234");
                    var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync("/v1/chat/completions", content);
                    response.EnsureSuccessStatusCode();

                    var responseJson = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(responseJson))
                    {
                        string reply = doc.RootElement
                            .GetProperty("choices")[0]
                            .GetProperty("message")
                            .GetProperty("content")
                            .GetString();

                        // 🔁 Remplacement ou affichage du résultat
                        if (!string.IsNullOrWhiteSpace(AIMailerEditor.SelectedText))
                        {
                            int selStart = AIMailerEditor.SelectionStart;
                            int selLength = AIMailerEditor.SelectionLength;
                            AIMailerEditor.Text = AIMailerEditor.Text.Substring(0, selStart) +
                                           reply +
                                           AIMailerEditor.Text.Substring(selStart + selLength);
                            AIMailerEditor.SelectionStart = selStart;
                            AIMailerEditor.SelectionLength = reply.Length;
                        }
                        else
                        {
                            AIMailerEditor.Text = reply;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur : " + ex.Message);
                }
            }
        }
    }
}