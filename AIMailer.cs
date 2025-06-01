using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AIMailer
{
    public partial class AIMailer : Form
    {
        /* Context Prompt pour mémo 
            Tu es un assistant IA francophone expert en rédaction, traduction et synthèse de texte. 
            Tu réponds toujours en français clair et précis, sans jamais expliquer tes actions, sauf si demandé. 
            Adapte ta réponse au style du texte original si c’est un extrait, et respecte les consignes suivantes : 
            ne commente jamais les instructions, ne cite pas le texte source, et reste concis si le contexte le demande.
        */

        // Noms et textes des objets graphiques
        private const string aiMailerConfigFile = "AIMailer.json";
        private const string aiMailerNotepadExe = "notepad.exe";
        private const string aiMailerName = "AIMailer";
        private const string aiMailerEditorName = "aiMailerEditor";
        private const string aiMailerActionPanelName = "aiMailerActionPanel";
        private const string textFileMenuTextOpenLabel = "Ouvrir un fichier";
        private const string textFileMenuTextSaveLabel = "Enregistrer sous...";
        private const string textFileMenuConfigEditLabel = "Éditer la configuration";
        private const string textFileMenuRestartLabel = "Actualiser la configuration...";
        private const string textFontSliderLabel = "Police : ";
        private const string textFileMenuTextLabel = "Texte";
        private const string configMenuTextLabel = "Config";
        private const string textFileMenuModeleLabel = "Modèle";
        private const string textFileMenuFilter = "Fichiers texte (*.txt)|*.txt|Tous les fichiers (*.*)|*.*";
        private const string aiMailerRestartWarningTitle = "Confirmation de redémarrage";
        private const string aiMailerRestartWarning = "Le texte actuel sera perdu.Voulez-vous vraiment actualiser les actions et relancer l'application ?";
        private const string aiMailerServiceAbsent = "Service: N/C";        // Service AI absent
        private const string aiMailerModeleAbsent = "Modèle: N/C";          // Modèle AI absent
        private const string stringMaskServiceAndModel = "{0} | {1} | {2}"; // Service & Modèle string mask 
        private const string stringMaskCompletionPopupPrompt = "Appel à l'IA en cours... \n\n\n\n[Modèle] '{0}'\n\n[Type] '{1}'\n\n[Prompt] {2}'\n\n[temperature] {3}\n\n[max_tokens] {4}";
        private const string stringMaskChatPopupPrompt = "Appel à l'IA en cours... \n\n\n\n[Modèle] '{0}'\n\n[Type] {1}\n\n[System] {2}\n\n[User] {3}\n\n[temperature] {4}\n\n[max_tokens] {5}";

        private const string aiMailerErrorStringEmpty = "<vide>";
        private const int aiMailerErrorStringLenghtMax = 150;           // Long max d'une chaine d'erreur
        private const int aiMailerAICallMsgBoxTimer = 6000;             // Timer Msg Box Appel AI        
        private const string aiMailerAICallMsgBoxTitle = "Appel AI..."; // Timer Msg Box Titre        

        /// Caractéristiques de la zone de texte et des boutons
        // Font sizes
        private const string editeurTextFontFamily = "Inter"; // "Segoe UI"
        private const int editeurTextFontSize = 11;     // Taille de police initiale
        private const int buttonTextFontSize = editeurTextFontSize - 1;
        private const int editeurMenuFontSize = buttonTextFontSize;     // Taille de police menu
        private const int editeurTextFontSizeMin = 6, editeurTextFontSizeMax = 30;
        // Tailles
        private const int textFontSliderWidth = 200, textFontSliderHeight = 40;   // Taille du curseur de police
        private const int textXOffset = 10, textYOffset = 10, textXScrollbar = 25, textYScrollbar = 40;
        private const int textWidth = 600, textHeight = 400;
        private const int buttonXOffset = 1, buttonYOffset = 10, buttonYSpace = 10;
        private const int buttonWidth = 110, buttonHeight = 30;
        // Couleurs - FFFAFA snow, FFFAF0 Blanc cassé, FFF5EE orange, B0BEC5 gris, LightGray, 
        private static readonly  Color MyColorBluePale1 = ColorTranslator.FromHtml("#F7F9FC");
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
        private const string maskErrorMsgUnknown = "Code Erreur inconnu : {0}"; // Recois le code inconnu
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
        // Description des Services possibles 
        private class AIModel
        {
            public string Id { get; set; }         // Model Id - Eg. "Mist7B"
            public string Name { get; set; }       // Model Mane - Eg. "Mistral 7B"
            public string ModelType { get; set; }  // Model Type - Eg. "Chat", "Completion", "ChatTokens",... 
            public string Url { get; set; }        // URL - Eg. "/v1/chat/completions"
            public string Model { get; set; }      // Model package - Eg. "Mistral-7B-...."
            public string ServiceId { get; set; }  // LMS, GPT, ....
            public double TemperatureDelta { get; set; }    // Temperature
            public int TokensMax { get; set; }              // Max Tokens

        }

        // Liste des Services IA possibles
        // private static List<AIModel> aiMailerAIModels = null;
        private AIModel aiMailerAIModelActif = null;                   // Ajout pour mémoriser le modèle actif


        // Description des Services possibles : Id URi, URL, DefaultTemperature, Model list
        private class AIService
        {
            public string Id { get; set; }       // Id du Service - Eg. LMS
            public string Name { get; set; }     // Nom du Service - Eg. LM Studio (Local)
            public string Uri { get; set; }      // Uri - Eg. "http://server:port"
            public string Key { get; set; }      // Clé Authentification 
            public string Context { get; set; }  // Prompt de Contexte (selon le Type de Modèle)
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

            // Erreur bloquante si aucun texte à traiter
            if (string.IsNullOrWhiteSpace(texteUtilisateur))
            {
                ErrorShow("ERROR_EDITOR_EMPTYSELECTION", action.Name, texteUtilisateur);
                return;
            }

            // Erreur bloquante si aucun service
            if (aiMailerAIServiceActif == null)
            {
                ErrorShow("ERROR_EDITOR_IASERVICEUNKNOW", action.Name, texteUtilisateur);
                return;
            }

            /// **********************************************************************
            /// ***** Construction du corps de la requête à envoyer à l'IA ***********
            /// **********************************************************************
            object iaRequestBody = AIMAilerAIModelPrompt(action, texteUtilisateur);
            if (iaRequestBody == null)
                return;
            var iaRequestBodyJson = new StringContent(JsonSerializer.Serialize(iaRequestBody), Encoding.UTF8, "application/json");

            /// **********************************************************************
            /// ***** Appel synchrone à l'IA avec vérification du code de retour *****
            /// **********************************************************************
            using (var client = new HttpClient())
            {
                try
                {
                    // Spécification de l'URi à appeler avec rajout de la clé si nécessaire
                    client.BaseAddress = new Uri(aiMailerAIServiceActif.Uri);
                    if (aiMailerAIServiceActif.Key != "")
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", aiMailerAIServiceActif.Key);

                    // Appel synchrone à l'IA
                    var response = await client.PostAsync(aiMailerAIModelActif.Url, iaRequestBodyJson);

                    // Vérification du code retour http
                    response.EnsureSuccessStatusCode();

                    // Parsing de la réponse selon le type de Modèle (Chat vs Completion) avec remplacement des \n par NewLine
                    var responseJson = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(responseJson))
                        if (aiMailerAIModelActif.ModelType.Substring(0, 4) == "Chat")
                            AIMAilerAIReplyReplace(doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()?.Replace("\n", Environment.NewLine));
                        else
                            AIMAilerAIReplyReplace(doc.RootElement.GetProperty("choices")[0].GetProperty("text").GetString()?.Replace("\n", Environment.NewLine));
                }
                catch (Exception ex)
                {
                    ErrorShow("ERROR_EDITOR_IACALL", ex.Message, iaRequestBody.ToString());
                }
               // aiMailerAIModelActif.ModelType, model, fullActionAndUserPrompt, texteUtilisateur
            }
        }

        /// *************************************************************************
        /// ***** Construction du Prompt à envoyer à l'IA selon le Modèle actif *****
        /// *************************************************************************
        //        private const string stringMaskChatPopupPrompt = "model='{0}' [System]'{1}' [User]'{2}'temperature={3}, max_tokens={4}";
        //        private const string stringMaskCompletionPopupPrompt = "model='{0}' prompt='{1}' temperature={2}, max_tokens={3}";
        private object AIMAilerAIModelPrompt(AIAction action, string texteUtilisateur)
        {
            // Temperature with model ratio
            double calcTemp = action.Temperature * (aiMailerAIModelActif.TemperatureDelta > 0 ? aiMailerAIModelActif.TemperatureDelta : 1);
            string model = aiMailerAIModelActif.Model;
            string modelType = aiMailerAIModelActif.ModelType;
            string actionPrompt = action.Prompt;
            string minPrompt = actionPrompt + " " + texteUtilisateur;
            string fullActionPrompt = aiMailerAIServiceActif.Context + " " + actionPrompt;
            string fullActionAndUserPrompt = fullActionPrompt + " " + texteUtilisateur;
            string notApplString = "N/A";
            int notApplTokens = 0;
            string returnedPrompt = null;
            object returnedObject = null;

            // Build Prompt depending on Actif Model
            switch (modelType)
            {
                case "Chat":                // Modèle Chat : Roles System + User (standard)
                    returnedPrompt = string.Format(stringMaskChatPopupPrompt, model, modelType, fullActionPrompt, texteUtilisateur, calcTemp, notApplTokens);
                    returnedObject = new
                    {
                        model = model,
                        messages = new[] { new { role = "system", content = fullActionPrompt }, new { role = "user", content = texteUtilisateur } },
                        temperature = calcTemp
                    };
                    break;

                case "ChatTokens":          // Modèle ChatTokens: Roles System + User + MaxTokens
                    returnedPrompt = string.Format(stringMaskChatPopupPrompt, model, modelType, fullActionPrompt, texteUtilisateur, calcTemp, aiMailerAIModelActif.TokensMax);
                    returnedObject = new
                    {
                        model = model,
                        messages = new[] { new { role = "system", content = fullActionPrompt }, new { role = "user", content = texteUtilisateur } },
                        temperature = calcTemp,
                        max_tokens = aiMailerAIModelActif.TokensMax
                    };
                    break;

                case "ChatUser":            // Modèle ChatUser: Role User 
                    returnedPrompt = string.Format(stringMaskChatPopupPrompt, model, modelType, notApplString, fullActionAndUserPrompt, calcTemp, notApplTokens);
                    returnedObject = new
                    {
                        model = model,
                        messages = new[] { new { role = "user", content = fullActionAndUserPrompt } },
                        temperature = calcTemp
                    };
                    break;

                case "ChatUserTokens":      // Modèle ChatUserTokens: Roles User + MaxTokens
                    returnedPrompt = string.Format(stringMaskChatPopupPrompt, model, modelType, notApplString, fullActionAndUserPrompt, calcTemp, aiMailerAIModelActif.TokensMax);
                    returnedObject = new
                    {
                        model = model,
                        messages = new[] { new { role = "user", content = fullActionAndUserPrompt } },
                        temperature = calcTemp, max_tokens = aiMailerAIModelActif.TokensMax
                    };
                    break;

                case "ChatUserMin":         // Modèle ChatTokens: Role User with min. Prompt (no Prompt Context)
                    returnedPrompt = string.Format(stringMaskChatPopupPrompt, model, modelType, notApplString, minPrompt, calcTemp, notApplTokens);
                    returnedObject = new
                    {
                        model = model, messages = new[] { new { role = "user", content = minPrompt } }, temperature = calcTemp
                    };
                    break;

                case "Completion":          // Modèle Completion: Prompt 
                    returnedPrompt = string.Format(stringMaskCompletionPopupPrompt, model, modelType, fullActionAndUserPrompt, calcTemp, notApplTokens);
                    returnedObject = new { model = model, prompt = fullActionAndUserPrompt, temperature = calcTemp };
                    break;

                case "CompletionTokens":    // Modèle Completion: Prompt + MaxTokens
                    returnedPrompt = string.Format(stringMaskCompletionPopupPrompt, model, modelType, fullActionAndUserPrompt, calcTemp, aiMailerAIModelActif.TokensMax);
                    returnedObject = new { model = model, prompt = fullActionAndUserPrompt, temperature = calcTemp, max_tokens = aiMailerAIModelActif.TokensMax };
                    break;

                case "CompletionMin":       // Modèle Completion: Prompt (no Prompt Context) 
                    returnedPrompt = string.Format(stringMaskCompletionPopupPrompt, model, modelType, minPrompt, calcTemp, notApplTokens);
                    returnedObject = new { model = model, prompt = minPrompt, temperature = calcTemp };
                    break;

                default:                    // Unknown Active Model error
                    ErrorShow("ERROR_EDITOR_IAMODELUNKNOWN", fullActionAndUserPrompt, texteUtilisateur);
                    break;
            }

            // Affichage d'une fenetre d'affichage de l'appel avec le message
            MsgBoxTools.ShowAutoClose(returnedPrompt);

            // Return built Object (or null on error)
            return (returnedObject);
        }

        /// **********************************************************************
        /// ***** Prise en compte de la réponse de l'IA dans l'Editeur ***********
        /// **********************************************************************
        private void AIMAilerAIReplyReplace(string aiReponseTexte)
        {
            // Remplacement de l'intégralité du texte (si aucun texte n'est sélectionné)
            if (string.IsNullOrWhiteSpace(aiMailerEditor.SelectedText))
                aiMailerEditor.Text = aiReponseTexte;
            else
            // ou Remplacement du texte n'est sélectionné
            {
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
        public AIMailer()
        {
            InitializeComponent();       // Fonction générée par VS dans Form1.Designer
        }

        // lancement de l'application par la fct appelée après création de la fenêtre
        private void AIMailer_Load(object sender, EventArgs e)
        {
            LoadConfigurationFromFile(); // Lecture de la configuration de l'appli
            InitialiserInterface();      // Adaptation de la fenêtre
        }

        ///// **********************************************************************
        ///// **********************************************************************
        ///// *** Lecture de la configuration de l'application *********************
        ///// **********************************************************************
        ///// **********************************************************************
        private void LoadConfigurationFromFile()
        {
            string configFilePath = Path.Combine(Application.StartupPath, aiMailerConfigFile);
            aiMailerAIActions = new List<AIAction>(); // Pour eviter les erreurs si pas de fichier

            // Lecture du fichier s'il existe
            if (File.Exists(configFilePath))
            {
                try
                {
                    // Lecture et parsing du fichier json
                    string json = File.ReadAllText(configFilePath);
                    var config = JsonSerializer.Deserialize<AIMailerConfigurationFile>(json,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    // Parsing des Actions et des Services
                    aiMailerAIActions = config.Actions ?? new List<AIAction>();
                    aiMailerAIServices = config.Services ?? new List<AIService>();

                    // 
                    aiMailerAIServiceActif = aiMailerAIServices.FirstOrDefault(); // ✅ Premier service disponible
                    aiMailerAIModelActif = aiMailerAIServiceActif?.Models?.FirstOrDefault(); // ✅ Premier modèle de ce service

                }
                catch (Exception ex)    // Erreur Fichier mal formatté
                {
                    ErrorShow("ERROR_EDITOR_CFGFILEBAD", ex.Message, Application.StartupPath, aiMailerConfigFile);
                }
            }
            // Erreur Fichier absent ou non accessible (droits)
            else ErrorShow("ERROR_EDITOR_CFGFILEUNKNOWN", Application.StartupPath, aiMailerConfigFile);
        } 
        // Structure de Parsing du fichier de configuration
        private class AIMailerConfigurationFile
        {
            public List<AIService> Services { get; set; }   // AI Services 
            public List<AIModel> Models { get; set; }       // AI Modèle
            public List<AIAction> Actions { get; set; }     // AI Actions
        }

        ///// **********************************************************************
        ///// **********************************************************************
        ///// *** Construction Interface graphique  ********************************
        ///// **********************************************************************
        ///// **********************************************************************
        private void InitialiserInterface()
        {
            // Charte graphique / ergonomie
            this.BackColor = editeurBackColor;
            this.Font = new Font(editeurTextFontFamily, editeurTextFontSize);
            //this.FormBorderStyle = FormBorderStyle.SizableToolWindow;

            // Ajout du Menu de la fenêtre
            int menuStripYOffset = InitialiserInterfaceMenu();

            // Ajout de la Texte Box Editeur
            InitialiserInterfaceEditeur(menuStripYOffset);

            // Ajout du Curseur de Sélection de la taille de la police
            InitialiserInterfaceEditeurCurseurFonte();

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
            ToolStripMenuItem menuConfig = new ToolStripMenuItem(configMenuTextLabel);
            ToolStripMenuItem menuEditerConfig = new ToolStripMenuItem(textFileMenuConfigEditLabel);
            ToolStripMenuItem menuActualiserConfig = new ToolStripMenuItem(textFileMenuRestartLabel);

            menuEditerConfig.Click += MenuEditerConfig_Click;
            menuActualiserConfig.Click += MenuActualiserConfig_Click;

            menuConfig.DropDownItems.Add(menuEditerConfig);
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
                    // Créer une entrée de menu Service
                    ToolStripMenuItem serviceItem = new ToolStripMenuItem(service.Name);
                    if (service.Models != null)
                    {
                        // Pour chaque Modèle
                        foreach (var model in service.Models)
                        {
                            // Créer une sous-entrée de menu Modèle
                            ToolStripMenuItem modelItem = new ToolStripMenuItem(model.Name);
                            modelItem.Tag = new List<object> { service, model };
                            // Raffraichit la Zone Modèle et Service à la sélection
                            modelItem.Click += (s, e) =>
                            {
                                var tagData = (List<object>)((ToolStripMenuItem)s).Tag;
                                aiMailerAIServiceActif = (AIService)tagData[0];
                                aiMailerAIModelActif = (AIModel)tagData[1];
                                labelServiceModel.Text = BuildServiceAndModelLabel();
                            };
                            serviceItem.DropDownItems.Add(modelItem);
                        }
                    }
                    // Ajouter l'entrée Service
                    menuService.DropDownItems.Add(serviceItem);
                }
            }
            // Ajouter le Menu Service
            menuStrip.Items.Add(menuService);

            /// *************************************************************
            /// ***** Création du Label de Menu Service et Modèle ***********
            /// *************************************************************
            labelServiceModel = new ToolStripLabel
            {
                Text = BuildServiceAndModelLabel(),
                Font = new Font(this.Font.FontFamily, editeurMenuFontSize - 1),
                ForeColor = editeurMenuForeColor,
                Alignment = ToolStripItemAlignment.Right,
                Margin = new Padding(0, 0, textXOffset, 0)
            };
            menuStrip.Items.Add(labelServiceModel);

            // Ajout de l'ensemble du Menu à la fenêtre
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);

            // Retourne la taille de la ligne de menu
            return (menuStrip.Height);
        }

        /// ********************************************************
        /// ***** Rafraichissement Zone Service et Modèle **********
        /// ********************************************************
        private string BuildServiceAndModelLabel()
        {
            return string.Format(stringMaskServiceAndModel,
                (aiMailerAIServiceActif == null ? aiMailerServiceAbsent : aiMailerAIServiceActif.Name),
                (aiMailerAIModelActif == null ? aiMailerModeleAbsent : aiMailerAIModelActif.Name),
                (aiMailerAIModelActif == null ? aiMailerModeleAbsent : aiMailerAIModelActif.ModelType) );
        }

        /// ********************************************************
        /// ***** Action des Menus *********************************
        /// ********************************************************
        /// 
        // Menu Fichier : Ouvrir un fichier texte et le copier dans l'Editeur
        private void MenuOuvrir_Click(object sender, EventArgs e)
        {
            // Choisir et Ouvrir le fichier 
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = textFileMenuFilter };
            // Copier son contenu dans l'Editeur
            if (openFileDialog.ShowDialog() == DialogResult.OK)
                aiMailerEditor.Text = System.IO.File.ReadAllText(openFileDialog.FileName);
        }

        // Menu Fichier : Enregistrer le contenu de l'Editeur dans un fichier
        private void MenuEnregistrer_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog { Filter = textFileMenuFilter };
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                System.IO.File.WriteAllText(saveFileDialog.FileName, aiMailerEditor.Text);
        }

        // Menu Config : Editer le Fichier de Configuration avec un notepad externe
        private void MenuEditerConfig_Click(object sender, EventArgs e)
        {
            // Vérifie si le fichier existe 
            string configFilePath = Path.Combine(Application.StartupPath, aiMailerConfigFile);
            if (File.Exists(configFilePath))
            {
                // Lancer le notepad externe avec le fichier
                try
                {
                    System.Diagnostics.Process.Start(aiMailerNotepadExe, configFilePath);
                }
                catch (Exception ex)
                {
                    ErrorShow("ERROR_EDITOR_CFGFILEOPEN", ex.Message, aiMailerNotepadExe, Application.StartupPath, aiMailerConfigFile);
                }
            }
            // Erreur sur absence de fichier de configuration
            else ErrorShow("ERROR_EDITOR_CFGFILEUNKNOWN", Application.StartupPath, aiMailerConfigFile);
        }

        // Menu Config : Relancer l'application pour relire la configuration
        private void MenuActualiserConfig_Click(object sender, EventArgs e)
        {
            // Demander une confirmation de relance si l'éditeur contient du texte
            if (!string.IsNullOrWhiteSpace(aiMailerEditor.Text))
            {
                DialogResult result = MessageBox.Show(aiMailerRestartWarning, aiMailerRestartWarningTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result != DialogResult.Yes)
                    return; // Annuler le redémarrage si refus de l'utilisateur
            }
            // Relancer l'application 
            try
            {
                Application.Restart();
            }
            catch (Exception ex)
            {
                // Erreur sur relance
                ErrorShow("ERROR_EDITOR_APPRESTART", ex.Message);
            }
        }

        ///// **********************************************************************
        ///// **********************************************************************
        ///// *** Sous-Fonctions génériques ****************************************
        ///// **********************************************************************
        ///// **********************************************************************

        /// *******************************************************
        /// ***** Fonction générique d'affichage des erreurs ******
        /// *******************************************************
        private void ErrorShow(string msgKey, string errorLevel1 = "", string errorLevel2 = "", string errorLevel3 = "", string errorLevel4 = "")
        {
            const string cut = "...";
            string msgLabel;

            if (!aiMailerErrorMsgs.TryGetValue(msgKey, out msgLabel))
                msgLabel = string.Format(maskErrorMsgUnknown,msgKey);
            MessageBox.Show(msgLabel
                   + (errorLevel1 == "" ? aiMailerErrorStringEmpty : "\n\n[Level1] " +
                            (errorLevel1.Length < aiMailerErrorStringLenghtMax ? errorLevel1 : 
                                errorLevel1.Substring(0, aiMailerErrorStringLenghtMax) + cut))
                   + (errorLevel2 == "" ? aiMailerErrorStringEmpty : "\n\n[Level2] " +
                            (errorLevel2.Length < aiMailerErrorStringLenghtMax ? errorLevel2 :
                                errorLevel1.Substring(0, aiMailerErrorStringLenghtMax) + cut))
                   + (errorLevel3 == "" ? aiMailerErrorStringEmpty : "\n\n[Level3] " +
                            (errorLevel3.Length < aiMailerErrorStringLenghtMax ? errorLevel3 :
                                errorLevel1.Substring(0, aiMailerErrorStringLenghtMax) + cut))
                   + (errorLevel4 == "" ? aiMailerErrorStringEmpty : "\n\n[Level4] " +
                            (errorLevel4.Length < aiMailerErrorStringLenghtMax ? errorLevel4 :
                                errorLevel1.Substring(0, aiMailerErrorStringLenghtMax) + cut))

                   + "\n\n[Modèle] " + BuildServiceAndModelLabel());
        }

        /// *************************************************************
        /// ***** Fonction générique d'affichage d'une sous-fenetre *****
        /// ***** pendant: aiMailerAICallMsgBoxTimer millisec       *****
        /// *************************************************************
        public static class MsgBoxTools
        {
            private const int WM_CLOSE = 0x0010;
            private const string DIALOG_CLASS = "#32770";      // classe Win32 d'un MessageBox

            [DllImport("user32.dll", SetLastError = true)]
            private static extern IntPtr FindWindow(string lpClass, string lpTitle);

            [DllImport("user32.dll", SetLastError = true)]
            private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr w, IntPtr l);

            /// <summary>
            /// Affiche un MessageBox non bloquant qui se ferme après "durationMs" millisecondes.
            /// </summary>
            public static void ShowAutoClose(string text,
                                             string title = aiMailerAICallMsgBoxTitle,
                                             int durationMs = aiMailerAICallMsgBoxTimer,
                                             MessageBoxIcon icon = MessageBoxIcon.Information)
            {
                // ▶ Le MessageBox doit tourner dans son propre thread STA
                Thread t = new Thread(() =>
                {
                    // ⏱️ Timer : ferme la boîte au bout de durationMs
                    var _ = Task.Delay(durationMs).ContinueWith(__ =>
                    {
                        IntPtr hWnd = FindWindow(DIALOG_CLASS, title);
                        if (hWnd != IntPtr.Zero) SendMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                    });

                    // 🚪 MessageBox "modale" pour CE thread, mais pas pour l'UI principale
                    MessageBox.Show(text, title, MessageBoxButtons.OK, icon);
                });

                t.SetApartmentState(ApartmentState.STA); // indispensable pour WinForms
                t.IsBackground = true;
                t.Start();
            }
        }

    }
}