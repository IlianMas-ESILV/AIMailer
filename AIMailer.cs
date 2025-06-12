using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

/* Context Prompt pour mémo 
Tu es un assistant IA aussi bien francophone qu'anglophone expert en rédaction, traduction et synthèse de texte. 
Tu réponds toujours en français clair et précis, sans jamais expliquer tes actions, sauf si demandé. 
Adapte ta réponse au style du texte original si c’est un extrait, et respecte les consignes suivantes : 
ne commente jamais les instructions, ne cite pas le texte source, et reste concis si le contexte le demande.
*/

namespace AIMailer
{
    public partial class AIMailer : Form
    {

        // ***********************************************
        // ***** Noms et chaines de caractères ***********
        // ***********************************************
        private const string aiMailerConfigFile = "AIMailer.cfg";
        private const string aiMailerAutoSaveFile = "AIMailer.AutoSave.txt"; // 💾 AUTOSAVE : fichier de sauvegarde auto
        private const string aiMailerNotepadExe = "notepad.exe";
        private const string aiMailerName = "AIMailer";
        private const string aiMailerEditorName = "aiMailerEditor";
        private const string aiMailerActionPanelName = "aiMailerActionPanel";
        private const string aiMailerPaletteActionsTitle = "Actions IA";
        private const string aiMailerErrorShowTitle = "Erreur " + aiMailerName;
        private const string textFileMenuTextOpenLabel = "Ouvrir un fichier";
        private const string textFileMenuTextSaveLabel = "Enregistrer sous...";
        private const string textFileMenuConfigEditLabel = "Éditer la configuration";
        private const string textFileMenuRestartLabel = "Actualiser la configuration...";
        private const string textEditorActionsIAMenuLabel = aiMailerPaletteActionsTitle + "...";
        private const string textEditorAnnulerMenuLabel = "Annuler (Ctrl-Z)";
        private const string textEditorRefaireMenuLabel = "Rétablir (Ctrl-Y)";
        private const string textEditorEffacerMenuLabel = "Effacer";
        private const string textEditorCouperMenuLabel = "Couper (Ctrl+X)";
        private const string textEditorCopierMenuLabel = "Copier (Ctrl+C)";
        private const string textEditorCollerMenuLabel = "Coller (Ctrl+V)";
        private const string textEditorSelectionnerMenuLabel = "Tout sélectionner (Ctrl+A)";
        private const string textFontSliderLabel = "Police : ";
        private const string textFileMenuTextLabel = "Texte";
        private const string configMenuTextLabel = "Configuration";
        private const string textFileMenuModeleLabel = "Modèles";
        private const string btnConfigLabel = "\u2699"; // "⚙"; 
        private const string textFileMenuFilter = "Fichiers texte (*.txt)|*.txt|Tous les fichiers (*.*)|*.*";
        private const string aiMailerIACallTitle = "Appel à l’IA en cours…";
        private const string aiMailerRestartWarningTitle = "Confirmation de redémarrage";
        private const string aiMailerRestartAutoSaveWarning = "Le texte actuel ne peut pas être sauvegardé.\nVoulez-vous vraiment actualiser les actions et relancer l'application ?";
        private const string aiMailerServiceAbsent = "Service: N/C";        // Service AI absent
        private const string aiMailerModeleAbsent = "Modèle: N/C";          // Modèle AI absent
        private const string stringMaskServiceAndModel = "{0} | {1} | {2}"; // Service & Modèle string mask 
        private const string stringMaskCompletionPopupPrompt = "[Modèle] {0}\n\n[Type] {1}\n\n[Prompt] {2}\n\n[temperature] {3}\n\n[max_tokens] {4}\n\n";
        private const string stringMaskChatPopupPrompt = "[Modèle] {0}\n\n[Type] {1}\n\n[System] {2}\n\n[User] {3}\n\n[temperature] {4}\n\n[max_tokens] {5}\n\n";
        private const string aiMailerTripleClicSentenceCars = ".?!\n";    // Ponctuation de début de phrase
        private const string aiMailerAICallMsgBoxTitle = "Appel AI..."; // Timer Msg Box Titre        
        private const string actionPanelButtonCfgMenuLabel = "Configurer";
        private const string aiMailerActionCfgTitle = "Configuration : ";
        private const string aiMailerActionCfgName = "Nom :";
        private const string aiMailerActionCfgPrompt = "Prompt :";
        private const string aiMailerActionCfgTemperature = "Température :";
        private const string aiMailerActionCfgSvcModel = "Service / Modèle :";
        private const string aiMailerActionCfgModelDefault = "<Modèle par défaut>";
        private const int aiMailerErrorStringLenghtMax = 200;           // Long max d'une chaine d'erreur
        private const int aiMailerAICallMsgBoxTimer = 6000;             // Timer Msg Box Appel AI
        private int lastClickTime = 0;   // Temps du dernier clic en millisecondes
        private int clickCount = 0;     // Compteur de clics successifs


        // ******************************************************
        // ***** Caractéristiques des objets graphiques *********
        // ******************************************************
        // Font sizes
        private const string editeurTextFontFamily = "Inter"; // "Segoe UI"
        private const int editeurTextFontSize = 11;     // Taille de police initiale
        private const int buttonTextFontSize = editeurTextFontSize - 1;
        private const int editeurMenuFontSize = buttonTextFontSize;     // Taille de police menu
        private const int editeurTextFontSizeMin = 6, editeurTextFontSizeMax = 30;
        // Tailles
        private const int textFontSliderWidth = 200, textFontSliderHeight = 40;   // Taille du curseur de police
        private const int textXOffset = 10, textYOffset = 10, textXScrollbar = 25, textYScrollbar = 40;
        private const int textWidth = 800, textHeight = 400;
        private const int buttonXOffset = 5, buttonYOffset = 5, buttonYSpace = 5;
        private const int buttonWidth = 110, buttonHeight = 30;
        private const int buttonConfigXOffset = 1, buttonConfigWidth = 26;
        // Couleurs - FFFAFA snow, FFFAF0 Blanc cassé, FFF5EE orange, B0BEC5 gris, LightGray, 
        private static readonly Color MyColorBluePale1 = ColorTranslator.FromHtml("#F7F9FC");
        private static readonly Color MyColorBluePale2 = ColorTranslator.FromHtml("#E3EAF3");
        private static readonly Color MyColorBlueDark = ColorTranslator.FromHtml("#1B3A57");
        private static readonly Color MyColorSnow = ColorTranslator.FromHtml("#FFFAFA");
        private static readonly Color editeurBackColor = MyColorBluePale1;
        private static readonly Color editeurMenuBackColor = MyColorBluePale2;
        private static readonly Color editeurMenuForeColor = MyColorBlueDark;
        private static readonly Color editeurCurseurForeColor = MyColorBlueDark;
        private static readonly Color buttonPanelBackColor = Color.Empty;
        private static readonly Color buttonBackColor = MyColorBluePale2;
        private static readonly Color buttonForeColor = MyColorBlueDark;
        private static readonly BorderStyle buttonPanelBorderStyle = BorderStyle.None;

        // ********************************
        // ***** Error Messages ***********
        // ***************** **************
        private const string maskErrorMsgUnknown = "Code Erreur inconnu : {0}"; // Recois le code inconnu
        private static readonly Dictionary<string, string> aiMailerErrorMsgs = new Dictionary<string, string>
        {
            { "ERROR_EDITOR_EMPTYSELECTION",   "Veuillez entrer ou sélectionner du texte..." },
            { "ERROR_EDITOR_IACALL",           "Erreur lors de l'appel à l'IA !" },
            { "ERROR_EDITOR_CFGFILEOPEN",      "Impossible d'ouvrir le fichier de configuration de l'application !" },
            { "ERROR_EDITOR_CFGFILEBAD",       "Fichier de configuration de l'application non conforme !" },
            { "ERROR_EDITOR_CFGFILEUNKNOWN",   "Impossible de trouver le fichier de configuration de l'application !" },
            { "ERROR_EDITOR_AUTOSAVEERR",      "Impossible d'enregistrer le contenu de l'éditeur !" },
            { "ERROR_EDITOR_APPRESTART",       "Impossible de redémarrer l'application !" },
            { "ERROR_EDITOR_IASERVICEUNKNOW",  "Appel impossible car aucun service d'IA n'est sélectionné !" },
            { "ERROR_EDITOR_IAMODELUNKNOWN",   "Appel impossible car type de modèle inconnu !" }
        };

        // *************************************************
        // ***** Variables "Globales" graphiques ***********
        // *************************************************
        private static TextBox aiMailerEditor = null;                                  // Text Box Editeur
        private static Form aiMailerPaletteActions = null;                                    // Palette d'action 
        private static Stack<string> aiMailerUndoStack = new Stack<string>();          // 🔁 Pile la fonction Undo
        private static Stack<string> aiMailerRedoStack = new Stack<string>();          // 🔁 Pile la fonction Redo

        // *****************************************************
        // ***** Variables "Globales" fonctionnelles ***********
        // *****************************************************
        private static List<AIService> aiMailerAIServices = null;               // Liste des Services IA configurés
        private static List<AIAction> aiMailerAIActions = new List<AIAction>(); // Liste des Modèles IA configurés
        private static AIService svc = null;                 // Ajout pour mémoriser le service actif
        private static AIModel mdl = null;                     // Ajout pour mémoriser le modèle actif

        // ------------------------------------------------------------------
        // Permet de retrouver rapidement le service ou le modèle à partir
        // des seuls ServiceId et ModelId de l'action.
        // ------------------------------------------------------------------
        private AIService GetServiceFor(AIAction action)
            => aiMailerAIServices.First(s => s.Id == action.ServiceId);

        private AIModel GetModelFor(AIAction action)
            => GetServiceFor(action).Models.First(m => m.Id == action.ModelId);


        ///// **********************************************************************
        ///// **********************************************************************
        ///// *****   Description des Services & Actions d'IA **********************
        ///// **********************************************************************
        ///// **********************************************************************

        // Description des Type de Modèles IA 
        public enum AIModelType
        {
            Chat,             // Utilise le format messages (avec rôles: system, user)
            ChatTokens,       // Idem Chat avec Max Tokens
            ChatUser,         // Idem Chat mais avec Role User uniquement (sans Role System)
            ChatUserMin,      // Idem ChatUser mais sans Contexte de prompt
            ChatUserTokens,   // Idem ChatUser avec Max Tokens
            Completion,       // Utilise le format prompt 
            CompletionMin,    // Idem Completion sans Contexte de prompt
            CompletionTokens, // Idem Completion avec Max Tokens
        }

        // Description des Services IA 
        private class AIModel
        {
            public string Id { get; set; }                  // Model Id - Eg. "Mist7B"
            public string Name { get; set; }                // Model Mane - Eg. "Mistral 7B"
            public AIModelType Type { get; set; }           // Model Type - Eg. "Chat", "Completion", "ChatTokens",...
            public string Url { get; set; }                 // URL - Eg. "/v1/chat/completions"
            public string Model { get; set; }               // Model package - Eg. "Mistral-7B-...."
            public decimal TemperatureRatio { get; set; }  // Ponderation de Temperature par Modèle
            public int TokensMax { get; set; }             // Max Tokens
            public bool Default { get; set; }               // Modèle par Défaut
        }

        // Description des Services possibles : Id URi, URL, DefaultTemperature, Model list
        private class AIService
        {
            public string Id { get; set; }              // Id du Service - Eg. LMS
            public string Name { get; set; }            // Nom du Service - Eg. LM Studio (Local)
            public string Uri { get; set; }             // Uri - Eg. "http://server:port"
            public string Key { get; set; }             // Clé Authentification (optionnelle)
            public string Context { get; set; }         // Prompt de Contexte (selon le Type de Modèle)
            public List<AIModel> Models { get; set; }   // Modèles AI disponibles avec ce service
        }

        public enum AIActionParametreType
        {
            String,
            DateTime,
            Email,
            Style
        }
        public class AIActionParametre
        {
            public string Name { get; set; }
            public AIActionParametreType Type { get; set; }
            public string Value { get; set; }
        }
        // Description des Actions (Boutons) possibles :
        private class AIAction
        {
            public string Id { get; set; }              // Id de l'action
            public string Name { get; set; }            // Libellé du bouton
            public string Prompt { get; set; }          // Prompt système à envoyer à l'IA
            public decimal Temperature { get; set; }      // Temperature
            public string ServiceId { get; set; }
            public string ModelId { get; set; }
            //public List<AIActionParametre> Parametres { get; set; }

        }

        ///// **********************************************************************
        ///// **********************************************************************
        ///// *****   Appel à l'IA à partir des boutons ****************************
        ///// **********************************************************************
        ///// **********************************************************************

        /// **********************************************************************
        /// ***** Méthode d'appel à l'IA et de prise en compte de sa réponse *****
        /// **********************************************************************
        /// 
        /// 

        /// Entrée pour la combo Service/Modèle fusionnée.
        private class ServiceModelEntry
        {
            public AIService Service { get; set; }
            public AIModel Model { get; set; }
            public string Text { get; set; }
        }

        private async Task AIMAilerAIMethod(AIAction action)
        {
            // 1) Lookup dynamique ou valeurs globales si override "Default"
            var svcLocal = string.IsNullOrEmpty(action.ServiceId) ? svc : GetServiceFor(action);
            var mdlLocal = string.IsNullOrEmpty(action.ModelId) ? mdl : GetModelFor(action);

            // 2) Vérifications
            if (svcLocal == null || mdlLocal == null)
            {
                ErrorShow("ERROR_EDITOR_IASERVICEUNKNOW", action.Name);
                return;
            }
            string texteUtilisateur = string.IsNullOrWhiteSpace(aiMailerEditor.SelectedText)
                ? aiMailerEditor.Text
                : aiMailerEditor.SelectedText;
            if (string.IsNullOrWhiteSpace(texteUtilisateur))
            {
                ErrorShow("ERROR_EDITOR_EMPTYSELECTION", action.Name);
                return;
            }

            


            // 3) Construction du corps JSON (on passe svc et mdl)
            var (iaRequestBody, promptToShow) = AIMAilerAIModelPrompt(action, texteUtilisateur, svcLocal, mdlLocal);
            if (iaRequestBody == null) return;

            var iaRequestBodyJson = new StringContent(
                JsonSerializer.Serialize(iaRequestBody),
                Encoding.UTF8,
                "application/json");

            // ───────────────────────────────────────────────────────────────
            // 2) Fenêtre d’attente « Veuillez patienter »
            // ───────────────────────────────────────────────────────────────
            Form waitDlg = new Form
            {
                Text = aiMailerIACallTitle,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                ControlBox = false,
                StartPosition = FormStartPosition.CenterParent,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Font = this.Font,
                TopMost = true
            };
            // Contenu : un label + une barre de progression indéterminée
            var lbl = new Label
            {
                Text = promptToShow,
                AutoSize = true,                 // le contrôle trouve tout seul sa hauteur
                MaximumSize = new Size(760, 0),     // largeur maxi (hauteur illimitée : 0)
                Padding = new Padding(20, 15, 20, 5)
            };
            var bar = new ProgressBar { Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 30, Width = 200, Dock = DockStyle.Bottom };
            waitDlg.Controls.Add(lbl);
            waitDlg.Controls.Add(bar);

            // Affiche la boîte (modeless mais parent désactivé → effet « modale »)
            this.Enabled = false;
            waitDlg.Show(this);
            waitDlg.Update();           // force rendu immédiat

            // 4) Appel HTTP
            using (var client = new HttpClient())
            {
                try
                {
                    client.BaseAddress = new Uri(svcLocal.Uri);
                    if (!string.IsNullOrEmpty(svcLocal.Key))
                        client.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", svcLocal.Key);

                    var response = await client.PostAsync(mdlLocal.Url, iaRequestBodyJson);
                    response.EnsureSuccessStatusCode();

                    var responseJson = await response.Content.ReadAsStringAsync();
                    using (var doc = JsonDocument.Parse(responseJson))
                    {
                        string result = mdlLocal.Type.ToString().StartsWith("Chat")
                             ? doc.RootElement
                                 .GetProperty("choices")[0]
                                 .GetProperty("message")
                                 .GetProperty("content")
                                 .GetString()
                            : doc.RootElement
                                 .GetProperty("choices")[0]
                                 .GetProperty("text")
                                 .GetString();

                        AIMAilerAIReplyReplace(result?.Replace("\n", Environment.NewLine));
                    }
                }
                catch (Exception ex)
                {
                    ErrorShow("ERROR_EDITOR_IACALL", ex.Message, iaRequestBody.ToString());
                }
                finally
                {
                    // ─── Nettoyage : fermeture de la boîte et ré-activation de l’appli
                    if (!waitDlg.IsDisposed) waitDlg.Close();
                    this.Enabled = true;
                    this.Activate();    // remet la fenêtre au premier plan
                    aiMailerEditor.Focus();
                }

            }
        }

        /// *************************************************************************
        /// ***** Construction du Prompt à envoyer à l'IA selon le Modèle actif *****
        /// *************************************************************************
        private (object Body, string Prompt) AIMAilerAIModelPrompt(AIAction action, string texteUtilisateur, AIService svc, AIModel mdl)
        {
            // Temperature with model ratio
            decimal calcTemp = action.Temperature * (mdl.TemperatureRatio > 0 ? mdl.TemperatureRatio : 1);
            string model = mdl.Model;
            string serviceAndModel = string.Format(stringMaskServiceAndModel,svc.Name,mdl.Name,mdl.Type);
            string typeString = mdl.Type.ToString();
            string actionPrompt = action.Prompt;
            string minPrompt = actionPrompt + " " + texteUtilisateur;
            string fullActionPrompt = svc.Context + " " + actionPrompt;
            string fullActionAndUserPrompt = fullActionPrompt + " " + texteUtilisateur;
            string notApplString = "N/A";
            int notApplTokens = 0;
            string messageToShow = null;
            object returnedObject = null;

            // Build Prompt depending on Actif Model
            switch (mdl.Type)
            {
                case AIModelType.Chat:                // Modèle Chat : Roles System + User (standard)
                    messageToShow = string.Format(stringMaskChatPopupPrompt, serviceAndModel, typeString, fullActionPrompt, texteUtilisateur, calcTemp, notApplTokens);
                    returnedObject = new
                    {
                        model = model,
                        messages = new[] { new { role = "system", content = fullActionPrompt }, new { role = "user", content = texteUtilisateur } },
                        temperature = calcTemp
                    };
                    break;

                case AIModelType.ChatTokens:          // Modèle ChatTokens: Roles System + User + MaxTokens
                    messageToShow = string.Format(stringMaskChatPopupPrompt, serviceAndModel, typeString, fullActionPrompt, texteUtilisateur, calcTemp, mdl.TokensMax);
                    returnedObject = new
                    {
                        model = model,
                        messages = new[] { new { role = "system", content = fullActionPrompt }, new { role = "user", content = texteUtilisateur } },
                        temperature = calcTemp,
                        max_tokens = mdl.TokensMax
                    };
                    break;

                case AIModelType.ChatUser:            // Modèle ChatUser: Role User 
                    messageToShow = string.Format(stringMaskChatPopupPrompt, serviceAndModel, typeString, notApplString, fullActionAndUserPrompt, calcTemp, notApplTokens);
                    returnedObject = new
                    {
                        model = model,
                        messages = new[] { new { role = "user", content = fullActionAndUserPrompt } },
                        temperature = calcTemp
                    };
                    break;

                case AIModelType.ChatUserTokens:      // Modèle ChatUserTokens: Roles User + MaxTokens
                    messageToShow = string.Format(stringMaskChatPopupPrompt, serviceAndModel, typeString, notApplString, fullActionAndUserPrompt, calcTemp, mdl.TokensMax);
                    returnedObject = new
                    {
                        model = model,
                        messages = new[] { new { role = "user", content = fullActionAndUserPrompt } },
                        temperature = calcTemp,
                        max_tokens = mdl.TokensMax
                    };
                    break;

                case AIModelType.ChatUserMin:         // Modèle ChatTokens: Role User with min. Prompt (no Prompt Context)
                    messageToShow = string.Format(stringMaskChatPopupPrompt, serviceAndModel, typeString, notApplString, minPrompt, calcTemp, notApplTokens);
                    returnedObject = new
                    {
                        model = model,
                        messages = new[] { new { role = "user", content = minPrompt } },
                        temperature = calcTemp
                    };
                    break;

                case AIModelType.Completion:          // Modèle Completion: Prompt 
                    messageToShow = string.Format(stringMaskCompletionPopupPrompt, serviceAndModel, typeString, fullActionAndUserPrompt, calcTemp, notApplTokens);
                    returnedObject = new { model = model, prompt = fullActionAndUserPrompt, temperature = calcTemp };
                    break;

                case AIModelType.CompletionTokens:    // Modèle Completion: Prompt + MaxTokens
                    messageToShow = string.Format(stringMaskCompletionPopupPrompt, serviceAndModel, typeString, fullActionAndUserPrompt, calcTemp, mdl.TokensMax);
                    returnedObject = new { model = model, prompt = fullActionAndUserPrompt, temperature = calcTemp, max_tokens = mdl.TokensMax };
                    break;

                case AIModelType.CompletionMin:       // Modèle Completion: Prompt (no Prompt Context) 
                    messageToShow = string.Format(stringMaskCompletionPopupPrompt, serviceAndModel, typeString, minPrompt, calcTemp, notApplTokens);
                    returnedObject = new { model = model, prompt = minPrompt, temperature = calcTemp };
                    break;

                default:                    // Unknown Active Model error
                    ErrorShow("ERROR_EDITOR_IAMODELUNKNOWN", svc.Context, actionPrompt, texteUtilisateur, mdl.TokensMax.ToString());
                    break;
            }

            // Affichage d'une fenetre d'affichage de l'appel avec le message
            // MsgBoxTools.ShowAutoClose(messageToShow);

            // Return built Object (or null on error)
            return (returnedObject, messageToShow);
        }

        /// **********************************************************************
        /// ***** Prise en compte de la réponse de l'IA dans l'Editeur ***********
        /// **********************************************************************
        private void AIMAilerAIReplyReplace(string aiReponseTexte)
        {
            // 🔁 UNDO/REDO : sauvegarde l'état actuel, vide le redo
            aiMailerUndoStack.Push(aiMailerEditor.Text);
            aiMailerRedoStack.Clear();

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
            LoadConfigurationFile();              // Lecture de la configuration de l'appli
            InitialiserInterface();               // Adaptation de la fenêtre
            RestoreEditorAutoSave();              // 💾 Restaure Autosave
            this.FormClosing += AIMailer_Close;
        }

        private void AIMailer_Close(object sender, EventArgs e)
        {
            EditorAutoSave(); // Ajoute AutoSave à la fermeture de la fenetre 
        }

        ///// **********************************************************************
        ///// **********************************************************************
        ///// *** Lecture de la configuration de l'application *********************
        ///// **********************************************************************
        ///// **********************************************************************
        private void LoadConfigurationFile()
        {
            string configFilePath = Path.Combine(Application.StartupPath, aiMailerConfigFile);
            aiMailerAIActions = new List<AIAction>(); // Pour eviter les erreurs si pas de fichier

            // Erreur Fichier absent ou non accessible (droits)
            if (!File.Exists(configFilePath))
            {
                ErrorShow("ERROR_EDITOR_CFGFILEUNKNOWN", Application.StartupPath, aiMailerConfigFile);
                return;
            }

            // Lecture et désérialisation du fichier de configuration
            try
            {
                // Lecture et parsing du fichier json
                string json = File.ReadAllText(configFilePath);

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                options.Converters.Add(new JsonStringEnumConverter()); // Lecture Enumeration (pr ModelType)

                var config = JsonSerializer.Deserialize<AIMailerConfigurationFile>(json, options);
                // Parsing des Actions et des Services
                aiMailerAIActions = config.Actions ?? new List<AIAction>();
                aiMailerAIServices = config.Services ?? new List<AIService>();

                // Trouve le Modèle par défaut ou sélectionne le premier par défaut
                mdl = aiMailerAIServices?.SelectMany(s => s.Models ?? Enumerable.Empty<AIModel>()).FirstOrDefault(m => m.Default)           // modèle “par défaut”
                   ?? aiMailerAIServices?.SelectMany(s => s.Models ?? Enumerable.Empty<AIModel>()).FirstOrDefault(); // sinon, le premier modèle

                // Trouve le Service correspondant au Modèle par défaut ou sélectionne le premier par défaut
                svc = aiMailerAIServices?.FirstOrDefault(s => s.Models != null && s.Models.Contains(mdl))
                    ?? aiMailerAIServices?.FirstOrDefault();
            }
            catch (Exception ex)    // Erreur Fichier mal formatté
            {
                ErrorShow("ERROR_EDITOR_CFGFILEBAD", ex.Message, Application.StartupPath, aiMailerConfigFile);
            }
        }

        // Structure de Parsing du fichier de configuration
        private class AIMailerConfigurationFile
        {
            public List<AIAction> Actions { get; set; }     // AI Actions
            public List<AIService> Services { get; set; }   // AI Services 
                                                            //            public List<AIModel> Models { get; set; }       // AI Modèle
        }

        /// <summary>
        /// (Ré)écrit le fichier de configuration JSON de l’application
        /// à partir des listes en mémoire aiMailerAIServices et aiMailerAIActions.
        /// </summary>
        private void SaveConfigurationFile()
        {
            // 1. Prépare l’objet « racine » à sérialiser
            var config = new AIMailerConfigurationFile
            {
                Actions = aiMailerAIActions,
                Services = aiMailerAIServices   // déjà null-safe
                // Si vous aviez aussi la propriété Models à la racine,
                // ajoutez-la ici le cas échéant (par exemple pour un cache global).
            };

            // 2. Options de sérialisation
            var options = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true,                                // JSON lisible
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            options.Converters.Add(new JsonStringEnumConverter());   // enums → chaînes

            try
            {
                // 3. Sérialise en mémoire
                string json = JsonSerializer.Serialize(config, options);

                // 4. Écrit sur disque (remplace le fichier existant)
                string cfgPath = Path.Combine(Application.StartupPath, aiMailerConfigFile);
                File.WriteAllText(cfgPath, json, Encoding.UTF8);

            }
            catch (Exception ex)
            {
                // Gestion d’erreur la plus simple : réutilise votre boîte générique
                ErrorShow("ERROR_EDITOR_CFGFILEOPEN", ex.Message);
            }
        }


        ///// **********************************************************************
        ///// **********************************************************************
        ///// *** Construction Interface graphique  ********************************
        ///// **********************************************************************
        ///// **********************************************************************
        private void InitialiserInterface()
        {
            bool aiBoutonsP = false; // Pas de bouton IA

            // Charte graphique / ergonomie
            this.BackColor = editeurBackColor;
            this.Font = new Font(editeurTextFontFamily, editeurTextFontSize);
            //this.FormBorderStyle = FormBorderStyle.SizableToolWindow;

            // Ajout du Menu de la fenêtre
            int menuStripYOffset = InitialiserInterfaceMenu();

            // Ajout de la Texte Box Editeur
            InitialiserInterfaceEditeur(menuStripYOffset, aiBoutonsP); // Pas de bouton IA

            // Ajout du Curseur de Sélection de la taille de la police
            InitialiserInterfaceEditeurCurseurFonte();

            // Ajout des Boutons d'Actions
            if (aiBoutonsP)
                InitialiserInterfaceActionButtons(menuStripYOffset);
        }

        /// **********************************************************************
        /// *** Initialisation Text Box Editeur **********************************
        /// **********************************************************************
        private void InitialiserInterfaceEditeur(int menuStripYOffset, bool aiBoutonsP)
        {
            // Taille Textbox 
            this.Text = aiMailerName;
            this.Size = new Size(
                        textWidth + 2 * textXOffset
                        + (aiBoutonsP ? buttonWidth + 2 * buttonXOffset + 30 : 0) + 20, // ← espace plus large à droite
                        menuStripYOffset + textFontSliderHeight + textHeight + 2 * textYOffset + textYScrollbar
);

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

            // ************************************************
            // 🔁 MENU CONTEXTUEL 
            // ************************************************
            ContextMenu contextMenu = new ContextMenu();

            // ************************************************
            // 🔁 MENU CONTEXTUEL avec Actions IA
            // ************************************************
            if (!aiBoutonsP)
            {
                // === NOUVEL ITEM ======================================================
                MenuItem iaActionsMenuItem = new MenuItem(textEditorActionsIAMenuLabel);
                iaActionsMenuItem.Click += (s, e) => OuvrirPaletteActions();
                contextMenu.MenuItems.Add(iaActionsMenuItem);
                contextMenu.MenuItems.Add("-");           // séparateur visuel (facultatif)

            }

            // ************************************************
            // 🔁 MENU CONTEXTUEL avec Undo/Redo
            // ************************************************
            MenuItem undoMenuItem = new MenuItem(textEditorAnnulerMenuLabel);
            undoMenuItem.Click += (s, e) => UndoLastChange();
            contextMenu.MenuItems.Add(undoMenuItem);

            MenuItem redoMenuItem = new MenuItem(textEditorRefaireMenuLabel);
            redoMenuItem.Click += (s, e) => RedoLastChange();
            contextMenu.MenuItems.Add(redoMenuItem);
            contextMenu.MenuItems.Add("-");

            MenuItem clearMenuItem = new MenuItem(textEditorEffacerMenuLabel);
            clearMenuItem.Click += (s, e) =>
            {
                aiMailerUndoStack.Push(aiMailerEditor.Text);
                aiMailerRedoStack.Clear();
                aiMailerEditor.Clear();
            };
            contextMenu.MenuItems.Add(clearMenuItem);
            contextMenu.MenuItems.Add("-");

            MenuItem cutMenuItem = new MenuItem(textEditorCouperMenuLabel);
            cutMenuItem.Click += (s, e) =>
            {
                aiMailerUndoStack.Push(aiMailerEditor.Text);
                aiMailerRedoStack.Clear();
                aiMailerEditor.Cut();
            };
            contextMenu.MenuItems.Add(cutMenuItem);
            MenuItem copyMenuItem = new MenuItem(textEditorCopierMenuLabel);
            copyMenuItem.Click += (s, e) => aiMailerEditor.Copy();
            contextMenu.MenuItems.Add(copyMenuItem);
            MenuItem pasteMenuItem = new MenuItem(textEditorCollerMenuLabel);
            pasteMenuItem.Click += (s, e) =>
            {
                aiMailerUndoStack.Push(aiMailerEditor.Text);
                aiMailerRedoStack.Clear();
                aiMailerEditor.Paste();
            };
            contextMenu.MenuItems.Add(pasteMenuItem);
            MenuItem selectAllMenuItem = new MenuItem(textEditorSelectionnerMenuLabel);

            selectAllMenuItem.Click += (s, e) => aiMailerEditor.SelectAll();
            contextMenu.MenuItems.Add(selectAllMenuItem);

            // Gestion du Undo pour l'écriture 
            aiMailerEditor.KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.Y)
                {
                    RedoLastChange();
                    e.SuppressKeyPress = true;
                }
                else if (!e.Control && !e.Alt && e.KeyCode != Keys.ShiftKey)
                {
                    aiMailerUndoStack.Push(aiMailerEditor.Text);
                    aiMailerRedoStack.Clear();
                }
            };

            aiMailerEditor.ContextMenu = contextMenu;
            this.Controls.Add(aiMailerEditor);
            aiMailerEditor.MouseDown += TextBox_MouseDown;

        }

        // 🔁 AJOUT UNDO : méthode pour annuler la dernière modification IA
        private void UndoLastChange()
        {
            // Empile l'Editeur sur le Redo et le remplace par un Dépile du Undo 
            if (aiMailerUndoStack.Count > 0)
            {
                aiMailerRedoStack.Push(aiMailerEditor.Text);
                aiMailerEditor.Text = aiMailerUndoStack.Pop();
            }
            else
                SystemSounds.Beep.Play(); // Aucun texte à annuler
        }
        /// 🔁 REDO : rétablir après un undo
        private void RedoLastChange()
        {
            // Empile l'Editeur sur le Undo et le remplace par un Dépile du Redo
            if (aiMailerRedoStack.Count > 0)
            {
                aiMailerUndoStack.Push(aiMailerEditor.Text);
                aiMailerEditor.Text = aiMailerRedoStack.Pop();
            }
            else
                SystemSounds.Beep.Play();
        }

        /// 🔁 GESTION CLAVIER Ctrl+Z / Ctrl+Y
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            bool rtn = true;
            if (keyData == (Keys.Control | Keys.Z))
                UndoLastChange();
            else if (keyData == (Keys.Control | Keys.Y))
                RedoLastChange();
            else rtn = base.ProcessCmdKey(ref msg, keyData);
            return rtn;
        }

        /// 💾 RESTAURER AUTO SAUVEGARDE
        private void RestoreEditorAutoSave()
        {
            string autosavePath = Path.Combine(Application.StartupPath, aiMailerAutoSaveFile);
            if (File.Exists(autosavePath))
            {
                aiMailerEditor.Text = File.ReadAllText(autosavePath);
            }
        }

        // Curseur de changement de taille de fonte
        private void InitialiserInterfaceEditeurCurseurFonte()
        {

            // Curseur pour la taille du texte
            TrackBar fontSizeSlider = new TrackBar
            {
                Minimum = editeurTextFontSizeMin,
                Maximum = editeurTextFontSizeMax,
                Value = editeurTextFontSize,
                TickFrequency = 2,
                SmallChange = 1,
                LargeChange = 2,
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



        /////=== Méthode de gestion des clics de souris sur le TextBox ===

        private void TextBox_MouseDown(object sender, MouseEventArgs e)
        {
            var now = Environment.TickCount;

            // Vérifie si le clic est rapproché du précédent (double/triple clic)
            if (now - lastClickTime < SystemInformation.DoubleClickTime)
                clickCount++;
            else
                clickCount = 1; // Trop espacé → on recommence le comptage

            lastClickTime = now;

            // Si triple clic détecté → sélectionner la phrase entière
            if (clickCount == 3)
            {
                TripleClicSelectSentence((TextBox)sender);
                clickCount = 0; // Réinitialisation après action
            }
        }

        // === Méthode pour sélectionner automatiquement une phrase entière autour du curseur ===
        private void TripleClicSelectSentence(TextBox box)
        {
            int pos = box.SelectionStart;
            string text = box.Text;

            // Recherche du début de la phrase (jusqu'à une ponctuation ou début de texte)
            int start = pos;
            while (start > 0 && !aiMailerTripleClicSentenceCars.Contains(text[start - 1]))
                start--;

            // Recherche de la fin de la phrase (jusqu'à une ponctuation ou fin de texte)
            int end = pos;
            while (end < text.Length && !aiMailerTripleClicSentenceCars.Contains(text[end]))
                end++;

            // Rajouter la ponctuation de fin de phrase
            if (end < text.Length) end++;

            // Inclut l'espace ou retour ligne après la ponctuation
            //while (end < text.Length && char.IsWhiteSpace(text[end]))
            //    end++;

            // Sélectionne la portion de texte détectée
            box.Select(start, end - start);
        }
        ///

        ///
        /// **********************************************************************
        /// *** Initialisation du Panneau avec les Boutons d'Actions *************
        /// **********************************************************************
        private void InitialiserInterfaceActionButtons(int menuStripYOffset)
        {
            // Création du panneau latéral pour les boutons d'actions
            Panel actionPanel = new Panel
            {
                Name = aiMailerActionPanelName,
                Size = new Size(buttonWidth + 2 * buttonXOffset + buttonConfigXOffset + buttonConfigWidth, // Ajout espace pour bouton config
                                aiMailerAIActions.Count * (buttonHeight + buttonYSpace) + 2 * buttonYOffset - buttonYSpace),
                Location = new Point(aiMailerEditor.Right + 10, menuStripYOffset + textYOffset),
                BorderStyle = buttonPanelBorderStyle,
                BackColor = buttonPanelBackColor,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            this.Controls.Add(actionPanel);

            // Création des boutons d'action et boutons config ⚙
            for (int i = 0, x = buttonXOffset, y = buttonYOffset; i < aiMailerAIActions.Count; y += buttonHeight + buttonYSpace, i++)
            {
                var action = aiMailerAIActions[i];
                Font fonteAction = new Font(this.Font.FontFamily, buttonTextFontSize);
                Font fonteConfig = new Font(this.Font.FontFamily, buttonTextFontSize - 1);

                // Bouton IA
                Button btnAction = new Button
                {
                    Text = action.Name,
                    Tag = action, // On lie directement l'action
                    Font = fonteAction,
                    Location = new Point(x, y),
                    BackColor = buttonBackColor,
                    ForeColor = buttonForeColor,
                    Size = new Size(buttonWidth, buttonHeight)
                };
                btnAction.Click += async (s, e) => await AIMAilerAIMethod((AIAction)((Button)s).Tag);
                actionPanel.Controls.Add(btnAction);

                // Bouton Config ⚙
                Button btnConfig = new Button
                {
                    Text = btnConfigLabel,
                    Tag = action,
                    Font = fonteConfig,
                    Location = new Point(x + buttonWidth + buttonConfigXOffset, y),
                    Size = new Size(buttonConfigWidth, buttonHeight),
                    // BackColor = Color.LightGray,
                    // ForeColor = Color.Black
                    BackColor = MyColorSnow, // buttonBackColor,
                    ForeColor = buttonForeColor
                };

                btnConfig.Click += (s, e) =>
                {
                    Button sourceBtn = (Button)s;
                    Point screenPosition = sourceBtn.PointToScreen(Point.Empty);
                    AfficherPanneauConfig(action);
                };
                actionPanel.Controls.Add(btnConfig);
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
            ToolStripMenuItem menuAnnuler = new ToolStripMenuItem(textEditorAnnulerMenuLabel);
            ToolStripMenuItem menuRefaire = new ToolStripMenuItem(textEditorRefaireMenuLabel);
            ToolStripMenuItem menuOuvrir = new ToolStripMenuItem(textFileMenuTextOpenLabel);
            ToolStripMenuItem menuEnregistrer = new ToolStripMenuItem(textFileMenuTextSaveLabel);

            menuAnnuler.Click += (s, e) => UndoLastChange();
            menuRefaire.Click += (s, e) => RedoLastChange();

            menuOuvrir.Click += MenuOuvrir_Click;
            menuEnregistrer.Click += MenuEnregistrer_Click;

            menuFichier.DropDownItems.Add(menuAnnuler);
            menuFichier.DropDownItems.Add(menuRefaire);
            menuFichier.DropDownItems.Add(new ToolStripSeparator());
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

            /// *************************************************************
            /// ***** Création du Label de Menu Service et Modèle ***********
            /// *************************************************************
            ToolStripLabel labelServiceModel = new ToolStripLabel
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

            /// ********************************************************
            /// ***** Création du menu "Services et Modèles" ***********
            /// ********************************************************
            // ——— Menu "Modèles" unifié ———
            ToolStripMenuItem menuService = new ToolStripMenuItem(textFileMenuModeleLabel);
            if (aiMailerAIServices != null)
            {
                foreach (var service in aiMailerAIServices)
                {
                    if (service.Models == null) continue;
                    foreach (var model in service.Models)
                    {
                        var item = new ToolStripMenuItem($"{model.Name} ({service.Name})");
                        item.Tag = new List<object> { service, model };
                        item.Click += (s, e) =>
                        {
                            var tagData = (List<object>)((ToolStripMenuItem)s).Tag;
                            // Remplace ces deux lignes :
                            // aiMailerAIServiceActif = (AIService)tagData[0];
                            // aiMailerAIModelActif   = (AIModel)  tagData[1];
                            // Par celles-ci :
                            svc = (AIService)tagData[0];
                            mdl = (AIModel)tagData[1];
                            labelServiceModel.Text = BuildServiceAndModelLabel();
                        };
                        menuService.DropDownItems.Add(item);
                    }
                }
            }
            menuStrip.Items.Add(menuService);


            // Retourne la taille de la ligne de menu
            return (menuStrip.Height);
        }

        /// ********************************************************
        /// ***** Rafraichissement Zone Service et Modèle **********
        /// ********************************************************
        private string BuildServiceAndModelLabel()
        {
            return string.Format(stringMaskServiceAndModel,
                (svc == null ? aiMailerServiceAbsent : svc.Name),
                (mdl == null ? aiMailerModeleAbsent : mdl.Name),
                (mdl == null ? aiMailerModeleAbsent : mdl.Type.ToString()));
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
            {
                aiMailerUndoStack.Push(aiMailerEditor.Text);
                aiMailerEditor.Text = System.IO.File.ReadAllText(openFileDialog.FileName);
            }
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

        // Sauvegarde du texte dans le fichier AutoSave
        private bool EditorAutoSave( bool signalerErreurP = true )
        {
            bool okP = true;
            try
            {
                File.WriteAllText(Path.Combine(Application.StartupPath, aiMailerAutoSaveFile), aiMailerEditor.Text);
            }
            catch (Exception ex)
            {
                okP = false;
                if (signalerErreurP)
                    ErrorShow("ERROR_EDITOR_AUTOSAVEERR", ex.Message, Application.StartupPath, aiMailerAutoSaveFile);
            }
            return okP; 
        }

        // Menu Config : Relancer l'application pour relire la configuration
        private void MenuActualiserConfig_Click(object sender, EventArgs e)
        {
            // Demander une confirmation de relance si l'éditeur contient du texte
            if (!string.IsNullOrWhiteSpace(aiMailerEditor.Text))
            {
                // Sauvegarde du contenu de l'éditeur dans un fichier local
                if (! EditorAutoSave(false) )
                { 
                    // Si impossible demande de confirmation à l'utilisateur
                    DialogResult result = MessageBox.Show(aiMailerRestartAutoSaveWarning, aiMailerRestartWarningTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (result != DialogResult.No)
                        return; // Annuler le redémarrage si refus de l'utilisateur
                }
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
                msgLabel = string.Format(maskErrorMsgUnknown, msgKey);
            MessageBox.Show(msgLabel
                   + (errorLevel1 == "" ? "" : "\n\n[Level1] " +
                            (errorLevel1.Length < aiMailerErrorStringLenghtMax ? errorLevel1 :
                                errorLevel1.Substring(0, aiMailerErrorStringLenghtMax) + cut))
                   + (errorLevel2 == "" ? "" : "\n\n[Level2] " +
                            (errorLevel2.Length < aiMailerErrorStringLenghtMax ? errorLevel2 :
                                errorLevel2.Substring(0, aiMailerErrorStringLenghtMax) + cut))
                   + (errorLevel3 == "" ? "" : "\n\n[Level3] " +
                            (errorLevel3.Length < aiMailerErrorStringLenghtMax ? errorLevel3 :
                                errorLevel3.Substring(0, aiMailerErrorStringLenghtMax) + cut))
                   + (errorLevel4 == "" ? "" : "\n\n[Level4] " +
                            (errorLevel4.Length < aiMailerErrorStringLenghtMax ? errorLevel4 :
                                errorLevel4.Substring(0, aiMailerErrorStringLenghtMax) + cut))

                   + "\n\n[Modèle] " + BuildServiceAndModelLabel(),
                     aiMailerErrorShowTitle, 
                     MessageBoxButtons.OK,
                     MessageBoxIcon.Error
                );
        }

        /// *************************************************************
        /// ***** Fonction générique d'affichage d'une sous-fenetre *****
        /// ***** pendant: aiMailerAICallMsgBoxTimer millisec       *****
        /// ***** avec Bouton "Ok"                                  *****
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
        /// <summary>
        /// Ouvre une fenêtre modale permettant d’éditer les propriétés d’une action IA
        /// (ordre : Name, Service, Modèle, Prompt, Température, Paramètres).
        /// </summary>
        private void AfficherPanneauConfig(AIAction action)
        {
            // ---------- Fenêtre modale ----------
            using (Form dlg = new Form())
            {
                var globalService = svc;
                var globalModel = mdl;
                dlg.Text = $"{aiMailerActionCfgTitle}{action.Name}";
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.ShowInTaskbar = false;
                dlg.AutoSize = true;
                dlg.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                dlg.Font = this.Font;

                int ctrlW = 500;
                int y = 15;

                // Helper créant un label + retourne sa hauteur
                int AddLabel(string text)
                {
                    var lbl = new Label { Text = text, AutoSize = true, Left = 15, Top = y + 4 };
                    dlg.Controls.Add(lbl);
                    return lbl.Height;
                }

                // Name ----------------------------------------------------------
                AddLabel(aiMailerActionCfgName);
                TextBox txtName = new TextBox
                {
                    Left = 140,
                    Top = y,
                    Width = ctrlW,
                    Text = action.Name
                };
                dlg.Controls.Add(txtName);
                y += txtName.Height + 15;

                /* Service et modele séparer 999
                 * // Service -------------------------------------------------------
                AddLabel("Service :");
                ComboBox cmbService = new ComboBox
                {
                    Left = 140,
                    Top = y,
                    Width = ctrlW,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                dlg.Controls.Add(cmbService);

                // Modèle --------------------------------------------------------
                y += cmbService.Height + 10;
                AddLabel("Modèle :");
                ComboBox cmbModel = new ComboBox
                {
                    Left = 140,
                    Top = y,
                    Width = ctrlW,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                dlg.Controls.Add(cmbModel);
                y += cmbModel.Height + 15;*/

                // Service / Modèle fusionné -------------------------
                AddLabel(aiMailerActionCfgSvcModel);
                ComboBox cmbServiceModel = new ComboBox
                {
                    Left = 140,
                    Top = y,
                    Width = ctrlW,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                dlg.Controls.Add(cmbServiceModel);
                y += cmbServiceModel.Height + 15;

                // ——— Prépare les items Service/Modèle ———
                var entries = new List<ServiceModelEntry>();

                // 1) “Default” → utilise le service/modèle global sélectionné en haut
                entries.Add(new ServiceModelEntry
                {
                    Service = svc,      // ton champ global
                    Model = mdl,      // ton champ global
                    Text = aiMailerActionCfgModelDefault
                });

                // 2) Tous les autres couples (Model (Service))
                foreach (var s in aiMailerAIServices.Where(sv => sv.Models != null))
                    foreach (var m in s.Models)
                        entries.Add(new ServiceModelEntry
                        {
                            Service = s,
                            Model = m,
                            Text = $"{m.Name} ({s.Name})"
                        });

                // 3) Lie la combo à cette liste
                cmbServiceModel.DataSource = entries;
                cmbServiceModel.DisplayMember = "Text";

                // 4) Prérenselectionne la ligne correspondant à l’action
                int idx;
                if (string.IsNullOrEmpty(action.ServiceId) && string.IsNullOrEmpty(action.ModelId))
                {
                    idx = 0; // “Default”
                }
                else
                {
                    idx = entries.FindIndex(e =>
                        e.Service.Id == action.ServiceId &&
                        e.Model.Id == action.ModelId);
                    if (idx < 0) idx = 0;
                }
                cmbServiceModel.SelectedIndex = idx;



                // Prompt --------------------------------------------------------
                AddLabel(aiMailerActionCfgPrompt);
                TextBox txtPrompt = new TextBox
                {
                    Left = 140,
                    Top = y,
                    Width = ctrlW,
                    Text = action.Prompt,
                    Multiline = true,
                    Height = 60,
                    ScrollBars = ScrollBars.Vertical
                };
                dlg.Controls.Add(txtPrompt);
                y += txtPrompt.Height + 15;

                // Température ---------------------------------------------------
                AddLabel(aiMailerActionCfgTemperature);
                NumericUpDown nudTemp = new NumericUpDown
                {
                    Left = 140,
                    Top = y,
                    Width = 80,
                    DecimalPlaces = 2,
                    Increment = 0.05M,
                    Minimum = 0,
                    Maximum = 2,
                    Value = action.Temperature
                };
                dlg.Controls.Add(nudTemp);
                y += nudTemp.Height + 20;



                // ---------- Boutons OK / Annuler ----------
                Button btnOK = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Left = dlg.ClientSize.Width - 200,
                    Width = 80,
                    Top = y
                };
                Button btnCancel = new Button
                {
                    Text = "Annuler",
                    DialogResult = DialogResult.Cancel,
                    Left = btnOK.Right + 10,
                    Width = 80,
                    Top = y
                };
                dlg.Controls.Add(btnOK);
                dlg.Controls.Add(btnCancel);
                dlg.AcceptButton = btnOK;
                dlg.CancelButton = btnCancel;

                /*
                // ---------- Logique Service / Modèle ----------
                // Remplit la liste des services
                cmbService.Items.AddRange(aiMailerAIServices.ToArray());
                cmbService.DisplayMember = "Name";
                // Sélectionne le service actuel

                // Positionne selon action.ServiceId OU service actif par défaut
                cmbService.SelectedItem = aiMailerAIServices
                    .FirstOrDefault(s => s.Id == action.ServiceId)
                    ?? svc;
                */



                /*
                // Méthode interne : alimente la liste de modèles selon service
                void RefreshModels()
                {
                    cmbModel.Items.Clear();
                    var svc = cmbService.SelectedItem as AIService;
                    if (svc?.Models != null)
                    {
                        cmbModel.Items.AddRange(svc.Models.ToArray());
                        cmbModel.DisplayMember = "Name";
                        // sélectionne l'AIModel dont l'Id == action.ModelId, ou le premier
                        cmbModel.SelectedItem =
                            svc.Models.FirstOrDefault(m => m.Id == action.ModelId)
                            ?? svc.Models.FirstOrDefault();
                    }
                }
                

                cmbService.SelectedIndexChanged += (_, __) => RefreshModels();
                RefreshModels();
                */

                // ---------- Affichage ----------
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    // ← voici tout ce qu’il faut remplacer
                    action.Name = txtName.Text;

                    // Avant : on lisait cmbService / cmbModel
                    // var svc = (AIService)cmbService.SelectedItem;
                    // var mdl = (AIModel)  cmbModel.SelectedItem;
                    // action.ServiceId = svc.Id;
                    // action.ModelId   = mdl.Id;

                    // Après : on lit cmbServiceModel
                    var sel = (ServiceModelEntry)cmbServiceModel.SelectedItem;
                    if (cmbServiceModel.SelectedIndex == 0)
                    {
                        // “Default” choisi → on vide l’override
                        action.ServiceId = null;
                        action.ModelId = null;
                    }
                    else
                    {
                        action.ServiceId = sel.Service.Id;
                        action.ModelId = sel.Model.Id;
                    }

                    action.Prompt = txtPrompt.Text;
                    action.Temperature = nudTemp.Value;

                    // … le reste (paramètres, SaveConfigurationFile)
                    SaveConfigurationFile();
                }
                this.Activate();          // remet la fenêtre principale devant
                aiMailerEditor.Focus();   // place le curseur dans la zone de texte

            }
        }
        /// Affiche (ou ramène) la palette d’actions IA.
        /// • Replace le focus dans l’éditeur dès qu’elle s’affiche.
        /// • Se ferme automatiquement si la sélection de l’éditeur change.
        /// </summary>
        private void OuvrirPaletteActions()
        {
            // Palette déjà présente → on la met devant et on sort
            if (aiMailerPaletteActions != null && !aiMailerPaletteActions.IsDisposed)
            {
                aiMailerPaletteActions.BringToFront();
                return;
            }

            // ─── Mémorise la sélection courante ──────────────────────────
            int selStart0 = aiMailerEditor.SelectionStart;
            int selLength0 = aiMailerEditor.SelectionLength;

            // ─── Création de la palette ──────────────────────────────────
            aiMailerPaletteActions = new Form
            {
                Text = aiMailerPaletteActionsTitle,
                FormBorderStyle = FormBorderStyle.FixedToolWindow, // ← non redimensionnable
                MaximizeBox = false,                           // (par sécurité)
                MinimizeBox = false,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                TopMost = true,
                Font = this.Font,
                BackColor = this.BackColor,
                Opacity = 0.80,
                Owner = this
            };

            // Position du panneau d'Actions
            aiMailerPaletteActions.Location = new Point(this.Right -15, this.Top);
            // Point position = Cursor.Position;
            // aiMailerPaletteActions.Location = new Point(position.X, position.Y);

            // ─── Panneau et boutons ──────────────────────────────────────
            Panel panel = new Panel { BackColor = Color.Transparent };
            aiMailerPaletteActions.Controls.Add(panel);

            int y = buttonYOffset;
            foreach (var action in aiMailerAIActions)
            {
                Button btn = new Button
                {
                    Text = action.Name,
                    Tag = action,
                    Font = new Font(this.Font.FontFamily, buttonTextFontSize),
                    Size = new Size(buttonWidth, buttonHeight),
                    Location = new Point(buttonXOffset, y),
                    BackColor = buttonBackColor,
                    ForeColor = buttonForeColor
                };
                btn.Click += async (s, _) =>
                    await AIMAilerAIMethod((AIAction)((Button)s).Tag);

                // menu contextuel « Configuration »
                ContextMenu ctx = new ContextMenu();
                ctx.MenuItems.Add(new MenuItem(actionPanelButtonCfgMenuLabel,
                    (_, __) => AfficherPanneauConfig(action)));
                btn.ContextMenu = ctx;

                panel.Controls.Add(btn);
                y += buttonHeight + buttonYSpace;
            }
            panel.Size = new Size(buttonWidth + 2 * buttonXOffset, y + buttonYOffset - buttonYSpace);
            aiMailerPaletteActions.ClientSize = panel.Size;

            // ─── Gestion du focus après affichage ────────────────────────
            aiMailerPaletteActions.Shown += (_, __) =>
            {
                // Rend la main à la fenêtre principale puis à l’éditeur
                this.Activate();
                aiMailerEditor.Focus();
            };

            // ─── Fermeture auto si la sélection change ───────────────────
            KeyEventHandler keyHandler = null;
            MouseEventHandler mouseHandler = null;

            void checkSelectionChange()
            {
                if (aiMailerPaletteActions != null && !aiMailerPaletteActions.IsDisposed)
                    if (aiMailerEditor.SelectionStart != selStart0 || aiMailerEditor.SelectionLength != selLength0)
                        aiMailerPaletteActions.Close();
            }

            keyHandler = (_, __) => checkSelectionChange();
            mouseHandler = (_, __) => checkSelectionChange();

            aiMailerEditor.KeyUp += keyHandler;
            aiMailerEditor.MouseUp += mouseHandler;

            // Nettoyage : détache les écouteurs quand la palette se ferme
            aiMailerPaletteActions.FormClosed += (_, __) =>
            {
                aiMailerPaletteActions = null;
                aiMailerEditor.KeyUp -= keyHandler;
                aiMailerEditor.MouseUp -= mouseHandler;
            };

            // ─── Petit utilitaire pour redonner immédiatement le focus ───
            void GiveBackFocus()
                => BeginInvoke((MethodInvoker)(() =>
                {
                    this.Activate();          // remet la fenêtre principale devant
                    aiMailerEditor.Focus();   // et place le curseur dans le texte
                }));

            // ▸ Quand on **déplace** la palette
            aiMailerPaletteActions.Move += (_, __) => GiveBackFocus();

            // ▸ Quand on **clique** n’importe où dans la palette (hors boutons)
            aiMailerPaletteActions.Click += (_, __) => GiveBackFocus();

            // ▸ Quand on clique sur le panneau translucide
            panel.Click += (_, __) => GiveBackFocus();

            aiMailerPaletteActions.Show();   // non modale
        }



    }
}