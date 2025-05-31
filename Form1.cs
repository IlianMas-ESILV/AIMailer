using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AiMailer
{

    public partial class Form1 : Form
    {
        // aiMailerAIActions : Nom de l'action, Libellé du Bouton, Prompt système à envoyer à l'IA, Modèle IA à utiliser
        /*
        private string[,] aiMailerAIActions = new string[,]
            {
                { "Traduire", "Traduire", "Traduis précisément ce texte en français.", "nous-hermes-2-mistral-7b-dpo" },
                { "Resumer", "Résumer", "Fais un résumé clair et concis du texte suivant.", "mistral-7b-instruct-v0.3@iq3_m" },
                { "Brainstorm", "Brainstorm", "Génère des idées créatives à partir de ce texte.", "deepseek-coder-6.7b-instruct" },
                { "Elaborer", "Élaborer", "Développe cette idée en détail.", "deepseek-coder-6.7b-instruct" },
                { "StyleEtudiant", "Style étudiant", "Reformule ce texte dans un style simple et accessible.", "nous-hermes-2-mistral-7b-dpo" },
                { "StylePro", "Style pro", "Reformule ce texte dans un style professionnel.", "nous-hermes-2-mistral-7b-dpo" },
                { "RendezVous", "Rendez-vous", "Rédige un message pour proposer un rendez-vous professionnel.", "openchat-3.5-1210" }
        };
        */

        // Error messages 
        private Dictionary<string, string> aiMailerErrorMsgs = new Dictionary<string, string>
        {
                { "ERROR_EDITOR_EMPTYSELECTION" , "Veuillez entrer ou sélectionner du texte." },
                { "ERROR_EDITOR_IACALL" , "Erreur lors de l'appel à l'IA." }
        };

        // Noms et textes des objets graphiques
        private string AIMAilerConfigFile = "AIMailer.json";
        private string AIMailerName = "AIMailer";
        private string AIMailerEditorName = "AIMailerEditor";
        private string AIMailerActionPanelName = "AIMailerActionPanel";
        private string textFontSliderLabel = "Taille : ";

        // Caractéristiques de la zone de texte et des boutons
        private int textFontSize = 12, textFontSizeMin = 8, textFontSizeMax = 32, textFontSliderWidth = 200, textFontSliderHeight = 40;
        private int textXOffset = 10, textYOffset = 10, textXScrollbar = 25, textYScrollbar = 40;
        private int textWidth = 600, textHeight = 400  ;
        private int buttonXOffset = 1, buttonYOffset = 10, buttonYSpace = 10;
        private int buttonWidth = 100, buttonHeight = 30;
        private BorderStyle buttonBorderStyle = BorderStyle.None; // BorderStyle.None BorderStyle.FixedSingle BorderStyle.Fixed3D
        private Color buttonBackColor = Color.Empty; // Color.LightGray;
        private Color buttonTextColor = Color.MidnightBlue; // Color.Empty;

        private TextBox AIMailerEditor;

        ///// **********************************************************************
        ///// *** Sous-Fonctions génériques ****************************************
        ///// **********************************************************************

        /// Fonction générique d'affichage des erreurs
        private void AIMailerErrorShow(string msgKey, string errorDetails = "")
        {
            string msgLabel = "";

            if (!aiMailerErrorMsgs.TryGetValue(msgKey, out msgLabel))
                msgLabel = "Code Erreur inconnu : " + msgKey + ".";
            MessageBox.Show(msgLabel + (errorDetails == "" ? "" : "\n[" + errorDetails + "]"));
        }

        // Structure de Tag des boutons d'action
        public class aiActionButtonTag
        {
            public string Id { get; set; }          // Id de l'action
            public string Label { get; set; }       // Texte du bouton
            public string Prompt { get; set; }      // Prompt système à envoyer à l'IA
            public string Model { get; set; }       // Modèle IA à utiliser
        }

        // aiMailerAIActions : Nom de l'action, Libellé du Bouton, Prompt système à envoyer à l'IA, Modèle IA à utiliser
        private List<aiActionButtonTag> aiMailerAIActions = new List<aiActionButtonTag>();

        private void MenuOuvrir_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Fichiers texte (*.txt)|*.txt|Tous les fichiers (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string contenu = System.IO.File.ReadAllText(openFileDialog.FileName);
                AIMailerEditor.Text = contenu;
            }
        }

        private void MenuEnregistrer_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Fichiers texte (*.txt)|*.txt|Tous les fichiers (*.*)|*.*"
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                System.IO.File.WriteAllText(saveFileDialog.FileName, AIMailerEditor.Text);
            }
        }
        private void MenuEditerConfig_Click(object sender, EventArgs e)
        {
            string configPath = Path.Combine(Application.StartupPath, "AIMAiler.json");

            if (File.Exists(configPath))
            {
                try
                {
                    System.Diagnostics.Process.Start("notepad.exe", configPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Impossible d’ouvrir le fichier de config :\n" + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Fichier de configuration introuvable :\n" + configPath);
            }
        }
        private void MenuActualiserConfig_Click(object sender, EventArgs e)
        {
            try
            {
                // Redémarre le programme
                Application.Restart();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Impossible de redémarrer l'application :\n" + ex.Message);
            }
        }


        ///// **********************************************************************
        ///// *** Form Editeur *****************************************************
        ///// **********************************************************************
        public Form1()
        {
            InitializeConfiguration();
            InitializeComponent();
            InitialiserInterface();
        }

        private void InitializeConfiguration()
        {
            string configPath = AIMAilerConfigFile;  // ⚠️ vérifier l'orthographe ici
            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                aiMailerAIActions = JsonSerializer.Deserialize<List<aiActionButtonTag>>(json);
            }
            else
            {
                MessageBox.Show("Fichier de configuration '" + AIMAilerConfigFile +"' introuvable dans " + Application.StartupPath);
                aiMailerAIActions = new List<aiActionButtonTag>(); // évite un crash          
            }
        }

        // 🖼️ Initialise l'interface graphique
        private void InitialiserInterface()
        {
            // Création de la barre de menu
            MenuStrip menuStrip = new MenuStrip();

            // Création du menu "Fichier"
            ToolStripMenuItem menuFichier = new ToolStripMenuItem("Fichier");
            ToolStripMenuItem menuOuvrir = new ToolStripMenuItem("Ouvrir...");
            ToolStripMenuItem menuEnregistrer = new ToolStripMenuItem("Enregistrer");
            ToolStripMenuItem menuEditerConfig = new ToolStripMenuItem("Éditer les actions...");
            ToolStripMenuItem menuActualiserConfig = new ToolStripMenuItem("Actualiser les actions");

            // Ajout des événements
            menuOuvrir.Click += MenuOuvrir_Click;
            menuEnregistrer.Click += MenuEnregistrer_Click;
            menuEditerConfig.Click += MenuEditerConfig_Click;
            menuActualiserConfig.Click += MenuActualiserConfig_Click;

            // Organisation des menus
            menuFichier.DropDownItems.Add(menuOuvrir);
            menuFichier.DropDownItems.Add(menuEnregistrer);
            menuFichier.DropDownItems.Add(new ToolStripSeparator()); // Ligne de séparation visuelle
            menuFichier.DropDownItems.Add(menuEditerConfig);
            menuFichier.DropDownItems.Add(menuActualiserConfig);
            menuStrip.Items.Add(menuFichier);

            // Ajout à la fenêtre
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
            int menuStripYOffset = menuStrip.Height;


            // Taille Textbox 
            this.Text = AIMailerName;
            this.Size = new Size(textWidth + 2* textXOffset + buttonWidth + 2 * buttonXOffset + textXScrollbar,
                                 menuStripYOffset + textFontSliderHeight + textHeight + 2 * textYOffset + textYScrollbar);

            // Zone de texte principale
            AIMailerEditor = new TextBox
            {
                Multiline = true,
                Name = AIMailerEditorName,
                Size = new Size(textWidth, textHeight),
                Font = new Font(this.Font.FontFamily, textFontSize),
                Location = new Point(textXOffset, menuStripYOffset + textYOffset),
                ScrollBars = ScrollBars.Vertical,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.Add(AIMailerEditor);

            // Curseur pour la taille du texte
            TrackBar fontSizeSlider = new TrackBar
            {
                Minimum = textFontSizeMin, Maximum = textFontSizeMax, Value = textFontSize,
                TickFrequency = 2, SmallChange = 1, LargeChange = 2,
                Orientation = Orientation.Horizontal,
                Location = new Point(textXOffset, AIMailerEditor.Bottom + 10),
                Width = textFontSliderWidth,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };

            // Étiquette pour afficher la taille actuelle
            Label fontSizeLabel = new Label
            {
                Text = textFontSliderLabel + textFontSize,
                Location = new Point(fontSizeSlider.Right + 10, fontSizeSlider.Top + 5),
                AutoSize = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };

            // Événement : met à jour la taille de la police
            fontSizeSlider.Scroll += (s, e) =>
            {
                int newSize = fontSizeSlider.Value;
                AIMailerEditor.Font = new Font(AIMailerEditor.Font.FontFamily, newSize);
                fontSizeLabel.Text = textFontSliderLabel + newSize;
            };

            // Ajout à la fenêtre
            this.Controls.Add(fontSizeSlider);
            this.Controls.Add(fontSizeLabel);


            // Panneau latéral pour les boutons IA
            Panel actionPanel = new Panel
            {
                Name = AIMailerActionPanelName,
                Size = new Size(buttonWidth + 2* buttonXOffset,
                                aiMailerAIActions.Count * (buttonHeight + buttonYSpace) + 2 *buttonYOffset - buttonYSpace),
                Location = new Point(textWidth + 2 * textXOffset, menuStripYOffset + textYOffset),
                BorderStyle = buttonBorderStyle,
                BackColor = buttonBackColor,
                ForeColor = buttonTextColor,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            this.Controls.Add(actionPanel);

            // Ajout dynamique des boutons
            AjouterTousLesBoutons(actionPanel);
        }

        // ➕ Ajoute tous les boutons définis dans aiMailerAIActions
        private void AjouterTousLesBoutons(Panel panel)
        {

            // Pour chaque descritpino de bouton d'action
            for (int i = 0, x = buttonXOffset, y = buttonYOffset; i < aiMailerAIActions.Count; y += buttonHeight + buttonYSpace, i++)
            {
                var action = aiMailerAIActions[i];

                Button btn = new Button
                {
                    Text = action.Label,
                    Size = new Size(buttonWidth, buttonHeight),
                    Location = new Point(x, y),
                    BackColor = buttonBackColor,
                    ForeColor = buttonTextColor,

                    Tag = new aiActionButtonTag { Label = action.Label, Prompt = action.Prompt, Model = action.Model }
                };

                btn.Click += async (s, e) => await AppelerIAAsync((aiActionButtonTag)((Button)s).Tag);
                panel.Controls.Add(btn);
            }

        }

        // 🤖 Appelle l'IA avec le modèle et prompt définis dans config
        private async Task AppelerIAAsync(aiActionButtonTag config)
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
                            AIMailerEditor.Text = AIMailerEditor.Text.Substring(0, selStart) + reply +
                                           AIMailerEditor.Text.Substring(selStart + selLength);
                            AIMailerEditor.SelectionStart = selStart;
                            AIMailerEditor.SelectionLength = reply.Length;
                        }
                        else
                            AIMailerEditor.Text = reply;
                    }
                }
                catch (Exception ex)
                {
                    AIMailerErrorShow("ERROR_EDITOR_IACALL",ex.Message);
                }
            }
        }
    }
}