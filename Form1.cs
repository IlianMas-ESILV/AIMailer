using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AIMailer
{
    public partial class Form1 : Form
    {
        // Context Prompte to be added to all actions
        // private string aiMailerAIContextPrompt = "Réponds en français.";
        private static readonly string aiMailerAIContextPrompt =
            "Tu es un assistant IA francophone expert en rédaction, traduction et synthèse de texte. " +
            "Tu réponds toujours en français clair et précis, sans jamais expliquer tes actions, sauf si demandé. " +
            "Adapte ta réponse au style du texte original si c’est un extrait, et respecte les consignes suivantes : " +
            "ne commente jamais les instructions, ne cite pas le texte source, et reste concis si le contexte le demande.";
        // + "Voici ma demande : ";

        // private static readonly string aiMailerAIPromptUser = ". Applique cette demande à ce texte : ";

        // private static readonly string aiMailerAIServiceURi = "http://127.0.0.1:1234";
        // private static readonly string aiMailerAIServiceURL = "/v1/chat/completions";
        // private static readonly double aiMailerAITemperature = 0.7;

        // Noms et textes des objets graphiques
        private static readonly string aiMailerConfigFile = "AIMailer.json";
        private static readonly string aiMailerNotepadExe = "notepad.exe";
        private static readonly string aiMailerName = "AIMailer";
        private static readonly string aiMailerEditorName = "aiMailerEditor";
        private static readonly string aiMailerActionPanelName = "aiMailerActionPanel";
        private static readonly string textFileMenuTextOpenLabel = "Ouvrir fichier";
        private static readonly string textFileMenuTextSaveLabel = "Enregistrer fichier...";
        private static readonly string textFileMenuConfigEditLabel = "Éditer la configuration...";
        private static readonly string textFileMenuRestartLabel = "Actualiser la configuration";
        private static readonly string textFontSliderLabel = "Police : ";
        private static readonly string textFileMenuTextLabel = "Texte";
        private static readonly string configMenuTextLabel = "Config";
        private static readonly string textFileMenuModeleLabel = "Modèle";
        private static readonly string textFileMenuFilter = "Fichiers texte (*.txt)|*.txt|Tous les fichiers (*.*)|*.*";
        private static readonly string aiMailerRestartWarningTitle = "Confirmation de redémarrage";
        private static readonly string aiMailerRestartWarning = "Le texte actuel sera perdu.Voulez-vous vraiment actualiser les actions et relancer l'application ?";

        /// Caractéristiques de la zone de texte et des boutons
        // Font sizes
        private static readonly string editeurTextFontFamily = "Inter"; // "Segoe UI"
        private static readonly int editeurTextFontSize = 11;     // Taille de police initiale
        private static readonly int buttonTextFontSize = editeurTextFontSize -1;
        private static readonly int editeurMenuFontSize = buttonTextFontSize;     // Taille de police menu
        private static readonly int editeurTextFontSizeMin = 6, editeurTextFontSizeMax = 30;
        // Tailles
        private static readonly int textFontSliderWidth = 200, textFontSliderHeight = 40;   // Taille du curseur de police
        private static readonly int textXOffset = 10, textYOffset = 10, textXScrollbar = 25, textYScrollbar = 40;
        private static readonly int textWidth = 600, textHeight = 400;
        private static readonly int buttonXOffset = 1, buttonYOffset = 10, buttonYSpace = 10;
        private static readonly int buttonWidth = 110, buttonHeight = 30;
        // Couleurs - FFFAFA snow, FFFAF0 Blanc cassé, FFF5EE orange, B0BEC5 gris, LightGray, 
        private static readonly Color MyColorBluePale1 = ColorTranslator.FromHtml("#F7F9FC");
        private static readonly Color MyColorBluePale2 = ColorTranslator.FromHtml("#E3EAF3");
        private static readonly Color MyColorBlueDark = ColorTranslator.FromHtml("#1B3A57");
        private static readonly Color editeurBackColor = MyColorBluePale1;
        private static readonly Color editeurMenuBackColor = MyColorBluePale2;
        private static readonly Color editeurMenuForeColor = MyColorBlueDark;
        private static readonly Color editeurCurseurForeColor = MyColorBlueDark;
        private static readonly BorderStyle buttonPanelBorderStyle = BorderStyle.None;
        private static readonly Color buttonPanelBackColor = Color.Empty;
        private static readonly Color buttonBackColor = MyColorBluePale2;
        private static readonly Color buttonForeColor = MyColorBlueDark;

        // Error messages 
        private static readonly string aiMailerErrorMsgUnknown = "Code Erreur inconnu : ";
        private static readonly Dictionary<string, string> aiMailerErrorMsgs = new Dictionary<string, string>
        {
                { "ERROR_EDITOR_EMPTYSELECTION",   "Veuillez entrer ou sélectionner du texte..." },
                { "ERROR_EDITOR_IACALL",           "Erreur lors de l'appel à l'IA !" },
                { "ERROR_EDITOR_CFGFILEOPEN",      "Impossible d'ouvrir le fichier de configuration de l'application !" },
                { "ERROR_EDITOR_CFGFILEBAD",       "Fichier de configuration de l'application non conforme !" },
                { "ERROR_EDITOR_CFGFILEUNKNOWN",   "Impossible de trouver le fichier de configuration de l'application !" },
                { "ERROR_EDITOR_APPRESTART",       "Impossible de redémarrer l'application !" },
                { "ERROR_EDITOR_IASERVICEUNKNOW",  "Appel impossible car aucun service d'IA n'est sélectionné !" },
                { "ERROR_EDITOR_IAMODELUNKNOWN",   "Appel impossible car type de modèle inconnu !" }
                
        };

        // Application Editor Text Box 
        private TextBox aiMailerEditor;
        private ToolStripLabel labelServiceModel; // 🆗 Déclaration correcte

        ///// **********************************************************************
        ///// **********************************************************************
        ///// *****   Description des Services & Actions d'IA **********************
        ///// **********************************************************************
        ///// **********************************************************************
        /*
        public enum AIModelType
        {
            "Chat",         // Utilise le format messages[] (avec rôle : system, user, assistant)
            "Completion"    // Utilise le format textuel classique (prompt + réponse)
        }
        */
        // Description des Services possibles : Id URi, URL, DefaultTemperature, Model list
        private class AIModel
        {
            public string Id { get; set; }         // Model Name - Eg. Mist7B
            public string Name { get; set; }       // Model Mane - Eg. Mistral 7B
            public string ModelType { get; set; }  // URi - Eg. "Mistral", "Llama",...
            public string URL { get; set; }        // URL - Eg. "/v1/chat/completions"
            public string Model { get; set; }      // URL - Eg. "Mistral-7B-...."
            public string ServiceId { get; set; }  // LMS, GPT, ....
            public double TemperatureDelta { get; set; }    // Temperature
            public int TokensMax { get; set; }    // Max Tokens

        }

        // Liste des Services IA possibles
        // private static List<AIModel> aiMailerAIModels = null;
        private AIModel aiMailerAIModelActif = null;                   // Ajout pour mémoriser le modèle actif


        // Description des Services possibles : Id URi, URL, DefaultTemperature, Model list
        private class AIService
        {
            public string Id { get; set; }       // Id du Service - Eg. LMS
            public string Name { get; set; }       // Nom du Service - Eg. LM Studio (Local)
            public string URi { get; set; }      // URi - Eg. "http://server:port"
            public string Key { get; set; }      // Clé
            public List<AIModel> Models { get; set; } // Modèles AI disponibles avec ce service
        }

        // Liste des Services IA possibles
        private static List<AIService> aiMailerAIServices = null;
        private AIService aiMailerAIServiceActif = null; // Ajout pour mémoriser le service actif

        // Description des Actions (Boutons) possibles :
        // Id de l'action, Libellé du Bouton, Prompt système à envoyer à l'IA, Modèle IA à utiliser
        private class AIAction
        {
            public string Id { get; set; }          // Id de l'action
            public string Name { get; set; }       // Libellé du bouton
            public string Prompt { get; set; }      // Prompt système à envoyer à l'IA
            public double Temperature { get; set; }    // Temperature

        }

        // Liste des Actions (Boutons) possibles
        private List<AIAction> aiMailerAIActions = new List<AIAction>();

        ///// **********************************************************************
        ///// **********************************************************************
        ///// *****   Appel à l'IA à partir des boutons ****************************
        ///// **********************************************************************
        ///// **********************************************************************

        /// **********************************************************************
        /// ***** Methode d'appel à l'IA et de prise en compte de sa réponse *****
        /// **********************************************************************
        private async Task AIMAilerAIMethod(AIAction action)
        {
            // Extraction du texte à traiter : texte sélectionné à la souris ou contenu de la Text box
            string texteUtilisateur = string.IsNullOrWhiteSpace(aiMailerEditor.SelectedText) ? aiMailerEditor.Text : aiMailerEditor.SelectedText;

            // Vérification qu'il existe bien un texte à traiter
            if (string.IsNullOrWhiteSpace(texteUtilisateur))
            {
                ErrorShow("ERROR_EDITOR_EMPTYSELECTION");
                return;
            }

            if (aiMailerAIServiceActif == null)
            {
                ErrorShow("ERROR_EDITOR_IASERVICEUNKNOW");
                return;
            }


            // Construction de la requête à envoyer à l'IA 
            double aiTemp = action.Temperature * (aiMailerAIModelActif.TemperatureDelta > 0 ? aiMailerAIModelActif.TemperatureDelta : 1);
            object iaRequestBody = AIMAilerAIModelPrompt(aiMailerAIModelActif.ModelType, action.Prompt, texteUtilisateur, aiMailerAIModelActif.Model, aiTemp);
            var iaRequestBodyJson = new StringContent(JsonSerializer.Serialize(iaRequestBody), Encoding.UTF8, "application/json");

            // Appel synchrone à l'IA avec vérification du code de retour
            using (var client = new HttpClient())
            {
                try
                {
                    // Spécification de l'URi à appeler
                    client.BaseAddress = new Uri(aiMailerAIServiceActif.URi);
                    if (aiMailerAIServiceActif.Key  != "")
                    {
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", aiMailerAIServiceActif.Key);
                    }
                    // Appel synchrone à l'IA
                    var response = await client.PostAsync(aiMailerAIModelActif.URL, iaRequestBodyJson);
                    // Vérification du code retour http
                    response.EnsureSuccessStatusCode();
                    // Parsing de la réponse json
                    var responseJson = await response.Content.ReadAsStringAsync();

                    using (JsonDocument doc = JsonDocument.Parse(responseJson))
                        // Traitement dans l'Editeur de la réponse de l'IA 
                        if (aiMailerAIModelActif.ModelType.Substring(0, 4) == "Chat")
                            AIMAilerAIReplyReplace(doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString());
                         else
                            AIMAilerAIReplyReplace(doc.RootElement.GetProperty("choices")[0].GetProperty("text").GetString());
                }
                catch (Exception ex)
                {
                    ErrorShow("ERROR_EDITOR_IACALL", ex.Message + "\n\n" + action.Name + "\n" + action.Prompt);
                }
            }
        }
        private object AIMAilerAIModelPrompt(string modelType, string actionPrompt, string texteUtilisateur, string aiModel, double aiTemp)
        {
            switch (modelType)
            {
                case "Chat":
                    return new
                    {
                        model = aiModel,
                        messages = new[] { new { role = "system", content = aiMailerAIContextPrompt + actionPrompt },
                                                                   new { role = "user", content = texteUtilisateur } },
                        temperature = aiTemp
                    };
                case "ChatTokens":
                    return new
                    {
                        model = aiModel,
                        messages = new[] { new { role = "system", content = aiMailerAIContextPrompt + actionPrompt },
                                                                   new { role = "user", content = texteUtilisateur } },
                        temperature = aiTemp,
                        max_tokens = aiMailerAIModelActif.TokensMax
                    };

                case "ChatUser":
                    return new
                    {
                        model = aiModel,
                        messages = new[] { new { role = "user", content = aiMailerAIContextPrompt + actionPrompt + texteUtilisateur } },
                        temperature = aiTemp
                    };
                case "ChatUserTokens":
                    return new
                    {
                        model = aiModel,
                        messages = new[] { new { role = "user", content = aiMailerAIContextPrompt + actionPrompt + texteUtilisateur } },
                        temperature = aiTemp, max_tokens = aiMailerAIModelActif.TokensMax
                    };
                case "ChatUserMin":
                    return new
                    {
                        model = aiModel,
                        messages = new[] { new { role = "user", content = actionPrompt + texteUtilisateur } },
                        temperature = aiTemp
                    };

                case "Completion":
                    return new { model = aiModel, prompt = aiMailerAIContextPrompt + actionPrompt + texteUtilisateur, temperature = aiTemp };

                case "CompletionTokens":
                    return new { model = aiModel, prompt = aiMailerAIContextPrompt + actionPrompt + texteUtilisateur, temperature = aiTemp, max_tokens = aiMailerAIModelActif.TokensMax };

                case "CompletionMin":
                    return new { model = aiModel, prompt = actionPrompt + texteUtilisateur, temperature = aiTemp };
            }

            ErrorShow("ERROR_EDITOR_IAMODELUNKNOWN", modelType);
            return (null);
        }

        /// **********************************************************************
        /// ***** Prise en compte de la réponse de l'IA dans l'Editeur ***********
        /// **********************************************************************
        private void AIMAilerAIReplyReplace (string aiReponseTexte)
        {
            // Si aucun texte n'est sélectionné
            if (string.IsNullOrWhiteSpace(aiMailerEditor.SelectedText))
                // Remplacement de l'intégralité du texte
                aiMailerEditor.Text = aiReponseTexte;
            else
            {
                // ou Remplacement seulement du texte sélectionné
                int selStart = aiMailerEditor.SelectionStart;
                int selLength = aiMailerEditor.SelectionLength;
                aiMailerEditor.Text = aiMailerEditor.Text.Substring(0, selStart) + aiReponseTexte +
                               aiMailerEditor.Text.Substring(selStart + selLength);
                aiMailerEditor.SelectionStart = selStart;
                aiMailerEditor.SelectionLength = aiReponseTexte.Length;
            }
        }

        ///// **********************************************************************
        ///// **********************************************************************
        ///// *** Initialisation Form Editeur **************************************
        ///// **********************************************************************
        ///// **********************************************************************

        // Initialisation de la fenêtre par appel à la fonction générée par Visual Studio
        public Form1()
        {
            InitializeComponent();      // Fonction générée par VS dans Form1.Designer
        }

        // lancement de l'applicartion par la fct appelée après création de la fenêtre
        private void Form1_Load(object sender, EventArgs e)
        {
            LoadConfigurationFromFile();  // Lecture de la configuration de l'appli
            InitialiserInterface();     // Adaptation de la fenêtre
        }

        ///// **********************************************************************
        ///// **********************************************************************
        ///// *** Lecture de la configuration de l'application *********************
        ///// **********************************************************************
        ///// **********************************************************************
        private void LoadConfigurationFromFile()
        {
            string configFilePath = Path.Combine(Application.StartupPath, aiMailerConfigFile);
            aiMailerAIActions = new List<AIAction>();

            if (File.Exists(configFilePath))
            {
                try
                {
                    string json = File.ReadAllText(configFilePath);
                    var config = JsonSerializer.Deserialize<AIMailerConfiguration>(json, 
                            new JsonSerializerOptions {PropertyNameCaseInsensitive = true});

                    aiMailerAIActions = config.Actions ?? new List<AIAction>();
                    aiMailerAIServices = config.Services ?? new List<AIService>();

                    aiMailerAIServiceActif = aiMailerAIServices.FirstOrDefault(); // ✅ Premier service disponible
                    aiMailerAIModelActif = aiMailerAIServiceActif?.Models?.FirstOrDefault(); // ✅ Premier modèle de ce service

                }
                catch (Exception ex)
                {
                    ErrorShow("ERROR_EDITOR_CFGFILEBAD", ex.Message + "\n\"n" +configFilePath);
                }
            }
            else
                ErrorShow("ERROR_EDITOR_CFGFILEUNKNOWN", configFilePath);
            }

            private class AIMailerConfiguration
            {
            public List<AIService> Services { get; set; }
            public List<AIModel> Models { get; set; }
            public List<AIAction> Actions { get; set; }
            }

            ///// **********************************************************************
            ///// **********************************************************************
            ///// *** Initialisation Interface graphique  ******************************
            ///// **********************************************************************
            ///// **********************************************************************
            private void InitialiserInterface()
            {
            // Charte graphique
            this.BackColor = editeurBackColor;
            this.Font = new Font(editeurTextFontFamily, editeurTextFontSize);
            //this.FormBorderStyle = FormBorderStyle.SizableToolWindow;

            // Ajout du Menu de la fenêtre
            int menuStripYOffset = InitialiserInterfaceMenu();

            // Ajout de la Texte Box Editeur
            InitialiserInterfaceEditeur(menuStripYOffset);

            // Ajout des Boutons d'Actions
            InitialiserInterfaceActionButtons(menuStripYOffset);
            }

            /// **********************************************************************
            /// *** Initialisation Text Box Editeur **********************************
            /// **********************************************************************
            private void InitialiserInterfaceEditeur(int menuStripYOffset)
            {
            // Taille Textbox 
            this.Text = aiMailerName;
            this.Size = new Size(textWidth + 2 * textXOffset + buttonWidth + 2 * buttonXOffset + textXScrollbar,
                                 menuStripYOffset + textFontSliderHeight + textHeight + 2 * textYOffset + textYScrollbar);

            // Zone de texte principale
            aiMailerEditor = new TextBox
            {
                Multiline = true,
                Name = aiMailerEditorName,
                Size = new Size(textWidth, textHeight),
                // Font = new Font(textFontFamily != null ? editeurFontFamily :this.Font.FontFamily, editeurTextFontSize),
                Font = new Font(this.Font.FontFamily, editeurTextFontSize),
                Location = new Point(textXOffset, menuStripYOffset + textYOffset),
                ScrollBars = ScrollBars.Vertical,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.Add(aiMailerEditor);

            InitialiserInterfaceEditeurCurseurFonte();
            }

            // Curseur de changement de taille de fonte
            private void InitialiserInterfaceEditeurCurseurFonte()
            {

            // Curseur pour la taille du texte
            TrackBar fontSizeSlider = new TrackBar
            {
                Minimum = editeurTextFontSizeMin, Maximum = editeurTextFontSizeMax, Value = editeurTextFontSize,
                TickFrequency = 2, SmallChange = 1, LargeChange = 2,
                Orientation = Orientation.Horizontal,
                Location = new Point(textXOffset, aiMailerEditor.Bottom + 10),
                Width = textFontSliderWidth,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };

            // Étiquette pour afficher la taille actuelle
            Label fontSizeLabel = new Label
            {
                Text = textFontSliderLabel + editeurTextFontSize,
                Font = new Font(this.Font.FontFamily, editeurMenuFontSize),
                ForeColor = editeurCurseurForeColor,
                Location = new Point(fontSizeSlider.Right + 10, fontSizeSlider.Top + 5),
                AutoSize = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };

            // Événement : met à jour la taille de la police
            fontSizeSlider.Scroll += (s, e) =>
            {
                int newSize = fontSizeSlider.Value;
                aiMailerEditor.Font = new Font(aiMailerEditor.Font.FontFamily, newSize);
                fontSizeLabel.Text = textFontSliderLabel + newSize;
            };

            // Ajout à la fenêtre
            this.Controls.Add(fontSizeSlider);
            this.Controls.Add(fontSizeLabel);
            }

            /// **********************************************************************
            /// *** Initialisation du Panneau avec les Boutons d'Actions *************
            /// **********************************************************************
            private void InitialiserInterfaceActionButtons(int menuStripYOffset)
            {
            // Création Panneau latéral pour les boutons d'actions
            Panel actionPanel = new Panel
            {
                Name = aiMailerActionPanelName,
                Size = new Size(buttonWidth + 2 * buttonXOffset,
                                aiMailerAIActions.Count * (buttonHeight + buttonYSpace) + 2 * buttonYOffset - buttonYSpace),
                Location = new Point(textWidth + 2 * textXOffset, menuStripYOffset + textYOffset),
                BorderStyle = buttonPanelBorderStyle,
                BackColor = buttonPanelBackColor,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            this.Controls.Add(actionPanel);

            // Création des Boutons d'actions
            for (int i = 0, x = buttonXOffset, y = buttonYOffset; i < aiMailerAIActions.Count; y += buttonHeight + buttonYSpace, i++)
            {
                var action = aiMailerAIActions[i];
                Font fonte = new Font(this.Font.FontFamily, buttonTextFontSize);

                // Création du bouton d'action
                Button btn = new Button
                {
                    Text = action.Name,
                    // Ajout des caractéristiques de l'Action au bouton 
                    Tag = new AIAction { Id = action.Id, Name = action.Name, Prompt = action.Prompt, Temperature = action.Temperature},

                    Font = fonte,
                    Location = new Point(x, y),
                    BackColor = buttonBackColor,
                    ForeColor = buttonForeColor,
                    Size = new Size(buttonWidth, buttonHeight),
                };

                btn.Click += async (s, e) => await AIMAilerAIMethod((AIAction)((Button)s).Tag);
                actionPanel.Controls.Add(btn);
            }
        }


        /// **********************************************************************
        /// *** Initialisation Menu de la fenêtre ********************************
        /// **********************************************************************
        private int InitialiserInterfaceMenu()
        {
        Font fonte = new Font(this.Font.FontFamily, editeurMenuFontSize);

        // Création de la barre de menu
        MenuStrip menuStrip = new MenuStrip() { Font = fonte, BackColor = editeurMenuBackColor, ForeColor = editeurMenuForeColor };

        // Création du menu "Fichier"
        ToolStripMenuItem menuFichier = new ToolStripMenuItem(textFileMenuTextLabel);
        ToolStripMenuItem menuOuvrir = new ToolStripMenuItem(textFileMenuTextOpenLabel);
        ToolStripMenuItem menuEnregistrer = new ToolStripMenuItem(textFileMenuTextSaveLabel);

        menuOuvrir.Click += MenuOuvrir_Click;
        menuEnregistrer.Click += MenuEnregistrer_Click;

        menuFichier.DropDownItems.Add(menuOuvrir);
        menuFichier.DropDownItems.Add(menuEnregistrer);
        menuStrip.Items.Add(menuFichier);

         // Création du menu "Config"
         // menuFichier.DropDownItems.Add(new ToolStripSeparator()); // Ligne de séparation visuelle
            ToolStripMenuItem menuConfig = new ToolStripMenuItem(configMenuTextLabel);
        ToolStripMenuItem menuEditerConfig = new ToolStripMenuItem(textFileMenuConfigEditLabel);
        ToolStripMenuItem menuActualiserConfig = new ToolStripMenuItem(textFileMenuRestartLabel);

        menuEditerConfig.Click += MenuEditerConfig_Click;
        menuActualiserConfig.Click += MenuActualiserConfig_Click;        menuConfig.DropDownItems.Add(menuEditerConfig);

        menuConfig.DropDownItems.Add(menuActualiserConfig);
        menuStrip.Items.Add(menuConfig);

            /// ********************************************************
            /// ***** Création du menu "Services et Modèles" ***********
            /// ********************************************************

            ToolStripMenuItem menuService = new ToolStripMenuItem(textFileMenuModeleLabel);
            // Si la liste n'est pas vide
            if (aiMailerAIServices != null && aiMailerAIServices.Count > 0)
            {
                // Pour chaque Service
                foreach (var service in aiMailerAIServices)
                {
                    ToolStripMenuItem serviceItem = new ToolStripMenuItem(service.Name);
                    if (service.Models != null)
                    {
                        // Pour chaque Modèle
                        foreach (var model in service.Models)
                        {
                            ToolStripMenuItem modelItem = new ToolStripMenuItem(model.Name);
                            modelItem.Tag = new List<object> { service, model };
                            modelItem.Click += (s, e) =>
                            {
                                var tagData = (List<object>)((ToolStripMenuItem)s).Tag;
                                aiMailerAIServiceActif = (AIService)tagData[0];
                                aiMailerAIModelActif = (AIModel)tagData[1];
                                UpdateLabelServiceModel();
                            };
                            serviceItem.DropDownItems.Add(modelItem);
                        }
                    }
                    menuService.DropDownItems.Add(serviceItem);
                }
            }
            menuStrip.Items.Add(menuService);

            /// ***********************************************************
            /// ***** Création Zone Affichage Service et Modèle ***********
            /// ***********************************************************

            labelServiceModel = new ToolStripLabel
            {
                Text = (aiMailerAIServiceActif != null ? aiMailerAIServiceActif.Name : "") + " | " +
                       (aiMailerAIModelActif != null ? aiMailerAIModelActif.Name : ""),
                Font = new Font(this.Font.FontFamily, editeurMenuFontSize - 1),
                ForeColor = editeurMenuForeColor,
                Alignment = ToolStripItemAlignment.Right,
                Margin = new Padding(0, 0, textXOffset, 0)
            };
            menuStrip.Items.Add(labelServiceModel);


            // Ajout Menu à la fenêtre
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);

            // Retourne la taille de la ligne de menu
            return (menuStrip.Height);
        }

        // Rafraichissement Zone Affichage Service et Modèle
        private void UpdateLabelServiceModel()
        {
            if (labelServiceModel != null && aiMailerAIServiceActif != null && aiMailerAIModelActif != null)
            {
                labelServiceModel.Text = aiMailerAIServiceActif.Name + " | " + aiMailerAIModelActif.Name;
            }
        }

        // Menu Fichier : Ouvrir
        private void MenuOuvrir_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = textFileMenuFilter };
            if (openFileDialog.ShowDialog() == DialogResult.OK)
                aiMailerEditor.Text = System.IO.File.ReadAllText(openFileDialog.FileName);
        }

        // Menu Fichier : Enregistrer 
        private void MenuEnregistrer_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog { Filter = textFileMenuFilter };
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                System.IO.File.WriteAllText(saveFileDialog.FileName, aiMailerEditor.Text);
        }

        // Menu Config : Editer
        private void MenuEditerConfig_Click(object sender, EventArgs e)
        {
            // Vérifie si le fichier existe et qu'il est lisible avec le notepad
            string configFilePath = Path.Combine(Application.StartupPath, aiMailerConfigFile);
            if (File.Exists(configFilePath))
            {
                try
                {
                    System.Diagnostics.Process.Start(aiMailerNotepadExe, configFilePath);
                }
                catch (Exception ex)
                {
                    ErrorShow("ERROR_EDITOR_CFGFILEOPEN", ex.Message + "\n" + aiMailerNotepadExe + "\n" + configFilePath);
                }
            }
            else ErrorShow("ERROR_EDITOR_CFGFILEUNKNOWN", configFilePath);
        }

        // Menu Config : Actualiser
        private void MenuActualiserConfig_Click(object sender, EventArgs e)
        {
            // Demander une confirmation si l'éditeur contient du texte
            if (!string.IsNullOrWhiteSpace(aiMailerEditor.Text))
            {
                DialogResult result = MessageBox.Show(aiMailerRestartWarning, aiMailerRestartWarningTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result != DialogResult.Yes)
                    return; // Annule le redémarrage
            }
            // Relance l'application en vérifiant
            try
            {
                Application.Restart();
            }
            catch (Exception ex)
            {
                ErrorShow("ERROR_EDITOR_APPRESTART", ex.Message);
            }
        }

        ///// **********************************************************************
        ///// **********************************************************************
        ///// *** Sous-Fonctions génériques ****************************************
        ///// **********************************************************************
        ///// **********************************************************************

        /// Fonction générique d'affichage des erreurs
        private void ErrorShow(string msgKey, string errorDetails = "")
        {
            string msgLabel;

            if (!aiMailerErrorMsgs.TryGetValue(msgKey, out msgLabel))
                msgLabel = aiMailerErrorMsgUnknown + msgKey;
            MessageBox.Show(msgLabel + (errorDetails == "" ? "" : "\n\n[" + errorDetails + "]"));
        }
    }
}