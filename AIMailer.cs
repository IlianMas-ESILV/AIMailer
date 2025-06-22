using System;
using System.Collections.Generic;
using System.Drawing;
//using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
//using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;   // ← alias explicite


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
        private const string aiMailerUser32dll = "user32.dll";
        private const string aiMailerName = "AIMailer";
        private const string aiMailerEditorName = "aiMailerEditor";
        private const string aiMailerPaletteActionsTitle = "AI Actions";
        private const string aiMailerErrorShowTitle = "Error " + aiMailerName;
        private const string textFileMenuTextOpenLabel = "Open file";
        private const string textFileMenuTextSaveLabel = "Save file to...";
        private const string textFileMenuConfigEditLabel = "Edit configuration";
        private const string textFileMenuRestartLabel = "Apply configuration...";
        private const string textEditorActionsIAMenuLabel = aiMailerPaletteActionsTitle + "...";
        private const string textEditorAnnulerMenuLabel = "Undo (Ctrl-Z)";
        private const string textEditorRefaireMenuLabel = "Redo (Ctrl-Y)";
        private const string textEditorEffacerMenuLabel = "Erase";
        private const string textEditorCouperMenuLabel = "Cut (Ctrl+X)";
        private const string textEditorCopierMenuLabel = "Copy (Ctrl+C)";
        private const string textEditorCollerMenuLabel = "Paste (Ctrl+V)";
        private const string textEditorSelectionnerMenuLabel = "Select All (Ctrl+A)";
        private const string textFontSliderLabel = "Font : ";
        private const string textFileMenuTextLabel = "Text";
        private const string configMenuTextLabel = "Configuration";
        private const string textFileMenuModeleLabel = "Models";
        private const string textFileMenuFilter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
        private const string aiMailerOkButtonText = "Ok";
        private const string aiMailerCancelButtonText = "Cancel";
        private const string aiMailerIACallTitle = "AI Call pending…";
        private const string aiMailerRestartWarningTitle = "Restart confirmation";
        private const string aiMailerRestartAutoSaveWarning = "The current text can not be saved.\nDo you want to restart ?";
        private const string aiMailerServiceAbsent = "Unknown Service";         // Service AI absent
        private const string aiMailerModeleAbsent = "Unknown Model";            // Modèle AI absent
        private const string stringMaskServiceAndModel = "{0} | {1} | {2}";     // Masque d'affichage du Service, Modèle, et Type de Modèle
        private const string stringMaskCompletionPopupPrompt = "[Model] {0}\n\n[Type] {1}\n\n[Prompt] {2}\n\n[Temperature] {3}\n\n[max_tokens] {4}\n\n";
        private const string stringMaskChatPopupPrompt = "[Model] {0}\n\n[Type] {1}\n\n[System] {2}\n\n[User] {3}\n\n[Temperature] {4}\n\n[max_tokens] {5}\n\n";
        private const string stringMaskChatPopupPromptNA = "N/A";
        private const string aiMailerTripleClicSentenceCars = ".?!\n";    // Ponctuation de début de phrase
        private const string aiMailerAICallMsgBoxTitle = "AI Call..."; // Timer Msg Box Titre        
        private const string actionPanelButtonCfgMenuLabel = "⚙ Edit";
        private const string aiMailerActionCfgTitle = "Configuration: ";
        private const string aiMailerActionCfgName = "Name:";
        private const string aiMailerActionCfgPrompt = "Prompt:";
        private const string aiMailerActionCfgTemperature = "Temperature:";
        private const string aiMailerActionCfgSvcModel = "Service / Model:";
        private const string aiMailerActionCfgModelDefault = "<Default model>";
        private const string aiMailerErrorLevelLabel = "[Level {0}] ";
        private const string aiMailerStringMsgTrunc = "...";

        internal const int aiMailerUndoStackMaxItems = 25;          // Pas plus de 25 Undos
        private const int aiMailerPromptToShowLengthMax = 999;      // Pas plus de 999 car de Texte Utilisateur dans la fenetre de trace
        private const int aiMailerErrorStringLenghtMax = 200;       // Pas plus de 200 car à chaque niveau de la fenetre d'erreurs
        private const int aiMailerDefaultTextFontSize = 11;         // Taille de police initiale
        private const int textEditorLeftMargin = 10;                // Marge gauche Editeur
        private const int textEditorRightMargin = 5;                // Marge droite Editeur
        private const int actionPanelXOffset = 0;                   // Déclalage X du panneau d'Actions
        private const int actionPanelYOffset = 10;                  // Déclalage Y du panneau d'Actions
        private int aiMailerEditorlastClickTime = 0;                // Temps du dernier clic en msec (pour Triple clic)
        private int aiMailerEditorClickCount = 0;                   // Compteur de clics successifs (pour Triple clic)

        // ******************************************************
        // ***** Caractéristiques des objets graphiques *********
        // ******************************************************
        // Font sizes
        private const string editeurTextFontFamily = "Inter";                           // Police par défaut (ou "Segoe UI")
        private const int editeurDefaultTextFontSize = aiMailerDefaultTextFontSize;     // Taille de police Editeur initiale 
        private const int buttonTextFontSize = aiMailerDefaultTextFontSize - 1;         // Taille de police Boutons
        private const int editeurMenuFontSize = buttonTextFontSize;                     // Taille de police Menuq
        private const int editeurTextFontSizeMin = 6, editeurTextFontSizeMax = 30;      // Tailles de police min & max Curseur de Polices
        // Tailles
        private const int textWidth = 800, textHeight = 400;                            // Taille fenetre Editeur initiale
        private const int textFontSliderWidth = 200, textFontSliderHeight = 40;         // Taille du Curseur de police
        private const int textXOffset = 10, textYOffset = 10;
        private const int textXScrollbar = 25, textYScrollbar = 40;                     // Taille Scrollbar Editeur
        private const int buttonIconSize = 32;                                          // Taille Icones des Boutons
        private const int buttonXOffset = 5, buttonYOffset = 5;                         // Decalage Boutons
        private const int buttonXSpace = 5, buttonYSpace = 5;                           // Espacement Boutons
        private const int buttonWidth = buttonIconSize + 8, buttonHeight = buttonWidth;
        // Couleurs - FFFAFA snow, FFFAF0 Blanc cassé, FFF5EE orange, B0BEC5 gris, LightGray, 
        // private static readonly Color buttonPanelBackColor = Color.Empty;
        // private static readonly Color MyColorSnow = ColorTranslator.FromHtml("#FFFAFA");
        private static readonly Color MyColorBluePale1 = ColorTranslator.FromHtml("#F7F9FC");
        private static readonly Color MyColorBluePale2 = ColorTranslator.FromHtml("#E3EAF3");
        private static readonly Color MyColorBlueDark = ColorTranslator.FromHtml("#1B3A57");
        private static readonly Color editeurBackColor = MyColorBluePale1;
        private static readonly Color editeurMenuBackColor = MyColorBluePale2;
        private static readonly Color editeurMenuForeColor = MyColorBlueDark;
        private static readonly Color editeurCurseurForeColor = MyColorBlueDark;
        private static readonly Color buttonBackColor = MyColorBluePale2;
        private static readonly Color buttonForeColor = MyColorBlueDark;

        // ********************************
        // ***** Error Messages ***********
        // ***************** **************
        private const string maskErrorMsgUnknown = "Code Erreur inconnu : {0}"; // Recois le code inconnu
        private static readonly Dictionary<string, string> aiMailerErrorMsgs = new Dictionary<string, string>
        {
            { "ERROR_EDITOR_NOTEXT",           "Please enter text..." },
            { "ERROR_EDITOR_IACALL",           "Error while calling IA!" },
            { "ERROR_EDITOR_CFGFILEOPEN",      "Configuration file impossible to open!" },
            { "ERROR_EDITOR_CFGFILEBAD",       "Configuration file not compliant!" },
            { "ERROR_EDITOR_CFGFILEUNKNOWN",   "Configuration file impossible to find!" },
            { "ERROR_EDITOR_AUTOSAVEERR",      "Editor text impossible to save!" },
            { "ERROR_EDITOR_APPRESTART",       "Application impossible to restart !" },
            { "ERROR_EDITOR_IASERVICEUNKNOW",  "No AI service: AI Call impossible!" },
            { "ERROR_EDITOR_IAMODELUNKNOWN",   "Unknown AI model: AI call impossible!" }
        };

        // *************************************************
        // ***** Variables "Globales" graphiques ***********
        // *************************************************
        private static TextBox aiMailerEditor = null;                                     // Text Box Editeur
        private static Form aiMailerPaletteActions = null;                                // Palette d'action 
        // private static readonly Stack<string> aiMailerUndoStack = new Stack<string>(); // 🔁 Pile la fonction Undo
        // private static readonly Stack<string> aiMailerRedoStack = new Stack<string>(); // 🔁 Pile la fonction Redo
        private readonly LinkedList<string> aiMailerUndoStack = new LinkedList<string>(); // 🔁 Pile (doublement chaînée) pour la fonction Undo 
        private readonly LinkedList<string> aiMailerRedoStack = new LinkedList<string>(); // 🔁 Pile (doublement chaînée) pour la fonction Undo 

        // *****************************************************
        // ***** Variables "Globales" fonctionnelles ***********
        // *****************************************************
        private static List<AIService> aiMailerAIServices = null;               // Liste des Services IA configurés
        private static List<AIAction> aiMailerAIActions = new List<AIAction>(); // Liste des Modèles IA configurés
        private static AIService aiMailerAIServiceActif = null;                 // Ajout pour mémoriser le service actif
        private static AIModel aiMailerAIModeleActif = null;                     // Ajout pour mémoriser le modèle actif

        // ------------------------------------------------------------------
        // Permet de retrouver rapidement le service ou le modèle à partir
        // des seuls ServiceId et ModelId de l'action.
        // ------------------------------------------------------------------
        private AIService GetServiceFor(AIAction action)
            => aiMailerAIServices.FirstOrDefault(s => s.Id == action.ServiceId);

        private AIModel GetModelFor(AIAction action)
            => GetServiceFor(action).Models.FirstOrDefault(m => m.Id == action.ModelId);


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
            public string Key { get; set; }             // Clé d'Authentification (optionnelle)/ surcharge KeyVar
            public string KeyVar { get; set; }          // Nom de la Var d'Env portant la clé d'Authentification (optionnelle)
            public string Context { get; set; }         // Prompt de Contexte (selon le Type de Modèle)
            public List<AIModel> Models { get; set; }   // Modèles AI disponibles avec ce service
        }

        // Description des Actions (Boutons) possibles :
        private class AIAction
        {
            public string Id { get; set; }              // Id de l'action
            public string Name { get; set; }            // Libellé du bouton
            public string Icon { get; set; }            // Icone du bouton
            public string Prompt { get; set; }          // Prompt système à envoyer à l'IA
            public decimal Temperature { get; set; }      // Temperature
            public string ServiceId { get; set; }
            public string ModelId { get; set; }
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
            var svcLocal = string.IsNullOrEmpty(action.ServiceId) ? aiMailerAIServiceActif : GetServiceFor(action);
            var mdlLocal = string.IsNullOrEmpty(action.ModelId) ? aiMailerAIModeleActif : GetModelFor(action);

            // 2) Vérifications
            if (mdlLocal == null)
            {
                ErrorShow("ERROR_EDITOR_IAMODELUNKNOWN", action.Name);
                return;
            }
            if (svcLocal == null)
            {
                ErrorShow("ERROR_EDITOR_IASERVICEUNKNOW", action.Name);
                return;
            }

            string texteUtilisateur = string.IsNullOrWhiteSpace(aiMailerEditor.SelectedText)
                ? aiMailerEditor.Text
                : aiMailerEditor.SelectedText;
            if (string.IsNullOrWhiteSpace(texteUtilisateur))
            {
                ErrorShow("ERROR_EDITOR_NOTEXT", action.Name);
                return;
            }

            // 3) Construction du corps JSON (on passe svc et mdl)
            var (iaRequestBody, promptToShow) = AIMAilerAIModelPrompt(action, texteUtilisateur, svcLocal, mdlLocal);
            if (iaRequestBody == null) return;

            var iaRequestBodyJson = new StringContent(
                JsonSerializer.Serialize(iaRequestBody),
                Encoding.UTF8,
                "application/json");

            /// *******************************************************
            // 2) Fenêtre d’attente « Veuillez patienter »
            // *******************************************************
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

            // ─── Conteneur en grille 2 colonnes ─────────────────────────
            var layout = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 2,
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 15, 20, 15)
            };
            // Colonne 0 auto-sized pour l’icône, colonne 1 prend le reste
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // ─── Icône d’information ───────────────────────────────────
            var pic = new PictureBox
            {
                Image = SystemIcons.Information.ToBitmap(),
                SizeMode = PictureBoxSizeMode.AutoSize,
                Anchor = AnchorStyles.Top
            };
            // On veut qu’elle couvre les deux lignes
            layout.Controls.Add(pic, 0, 0);
            layout.SetRowSpan(pic, 2);

            // ─── Label de message ───────────────────────────────────────
            var lbl = new Label
            {
                Text = promptToShow,
                AutoSize = true,
                MaximumSize = new Size(700, 0),  // largeur maxi
                Anchor = AnchorStyles.Left | AnchorStyles.Top
            };
            layout.Controls.Add(lbl, 1, 0);

            // ─── Barre de progression indéterminée ─────────────────────
            // Barre animée 
            var bar = new MaterialMarquee
            {
                Width = 200,               // longueur visible
                Dock = DockStyle.Top,
                Margin = new Padding(0, 10, 0, 0)  // espace au-dessus
            };
            layout.Controls.Add(bar, 1, 1);
            // ─── On ajoute le layout puis on affiche ────────────────────
            waitDlg.Controls.Add(layout);
            this.Enabled = false;
            waitDlg.Show(this);
            waitDlg.Update();

            /// *******************************************************
            /// ***** Apppel http à LM Studio *************************
            // ********************************************************
            using (var client = new HttpClient())
            {
                try
                {
                    client.BaseAddress = new Uri(svcLocal.Uri);

                    // Ajout d'une éventuelle clé d'autorisation dans le Header de la Rqt
                    // 1. On prend le champ "Key" s'il existe
                    // 2. Sinon on va chercher la valeur de la var dont le nom est dans "KeyVar"
                    string bearerToken = null;
                    if (!string.IsNullOrEmpty(svcLocal.Key))
                        bearerToken = svcLocal.Key;
                    else if (!string.IsNullOrEmpty(svcLocal.KeyVar))
                        bearerToken = Environment.GetEnvironmentVariable(svcLocal.KeyVar);

                    if (!string.IsNullOrEmpty(bearerToken))
                        client.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", bearerToken);

                    // Appel asychrone au Modèle dans LM Studio
                    var response = await client.PostAsync(mdlLocal.Url, iaRequestBodyJson);
                    response.EnsureSuccessStatusCode();

                    // Deserialisation de la reponse de l'ia
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
            string aiModel = mdl.Model;
            string serviceAndModel = string.Format(stringMaskServiceAndModel,svc.Name,mdl.Name,mdl.Type);
            string typeString = mdl.Type.ToString();
            string actionPrompt = action.Prompt;
            string minPrompt = actionPrompt + " " + texteUtilisateur;
            string fullActionPrompt = svc.Context + " " + actionPrompt;
            string fullActionAndUserPrompt = fullActionPrompt + " " + texteUtilisateur;
            string notApplString = stringMaskChatPopupPromptNA;
            int notApplTokens = 0;
            string messageToShow = null;
            object returnedObject = null;

            // Enlever NewLine en doublons et tronquer "Texte Utilisateur" dans le message à afficher 
            string userTextShort = Regex.Replace(texteUtilisateur, @"(\r?\n){2,}", Environment.NewLine);
            userTextShort = userTextShort.Length > aiMailerPromptToShowLengthMax 
                            ? userTextShort.Substring(0, aiMailerPromptToShowLengthMax) + aiMailerStringMsgTrunc 
                            : userTextShort;

            // Enlever NewLine en doublons et tronquer "Full Action Prompt" dans le message à afficher 
            string fullActionAndUserTextShort = Regex.Replace(fullActionAndUserPrompt, @"(\r?\n){2,}", Environment.NewLine);
            fullActionAndUserTextShort = fullActionAndUserTextShort.Length > aiMailerPromptToShowLengthMax
                            ? fullActionAndUserTextShort.Substring(0, aiMailerPromptToShowLengthMax) + aiMailerStringMsgTrunc
                            : fullActionAndUserTextShort;

            // Build Prompt depending on Actif Model
            switch (mdl.Type)
            {
                case AIModelType.Chat:                // Modèle Chat : Roles System + User (standard)
                    messageToShow = string.Format(stringMaskChatPopupPrompt, serviceAndModel, typeString, fullActionPrompt, userTextShort, calcTemp, notApplTokens);
                    returnedObject = new
                    {
                        model = aiModel,
                        messages = new[] { new { role = "system", content = fullActionPrompt }, new { role = "user", content = texteUtilisateur } },
                        temperature = calcTemp
                    };
                    break;

                case AIModelType.ChatTokens:          // Modèle ChatTokens: Roles System + User + MaxTokens
                    messageToShow = string.Format(stringMaskChatPopupPrompt, serviceAndModel, typeString, fullActionPrompt, userTextShort, calcTemp, mdl.TokensMax);
                    returnedObject = new
                    {
                        model = aiModel,
                        messages = new[] { new { role = "system", content = fullActionPrompt }, new { role = "user", content = texteUtilisateur } },
                        temperature = calcTemp,
                        max_tokens = mdl.TokensMax
                    };
                    break;

                case AIModelType.ChatUser:            // Modèle ChatUser: Role User 
                    messageToShow = string.Format(stringMaskChatPopupPrompt, serviceAndModel, typeString, notApplString, fullActionAndUserTextShort, calcTemp, notApplTokens);
                    returnedObject = new
                    {
                        model = aiModel,
                        messages = new[] { new { role = "user", content = fullActionAndUserPrompt } },
                        temperature = calcTemp
                    };
                    break;

                case AIModelType.ChatUserTokens:      // Modèle ChatUserTokens: Roles User + MaxTokens
                    messageToShow = string.Format(stringMaskChatPopupPrompt, serviceAndModel, typeString, notApplString, fullActionAndUserTextShort, calcTemp, mdl.TokensMax);
                    returnedObject = new
                    {
                        model = aiModel,
                        messages = new[] { new { role = "user", content = fullActionAndUserPrompt } },
                        temperature = calcTemp,
                        max_tokens = mdl.TokensMax
                    };
                    break;

                case AIModelType.ChatUserMin:         // Modèle ChatTokens: Role User with min. Prompt (no Prompt Context)
                    messageToShow = string.Format(stringMaskChatPopupPrompt, serviceAndModel, typeString, notApplString, minPrompt, calcTemp, notApplTokens);
                    returnedObject = new
                    {
                        model = aiModel,
                        messages = new[] { new { role = "user", content = minPrompt } },
                        temperature = calcTemp
                    };
                    break;

                case AIModelType.Completion:          // Modèle Completion: Prompt 
                    messageToShow = string.Format(stringMaskCompletionPopupPrompt, serviceAndModel, typeString, fullActionAndUserTextShort, calcTemp, notApplTokens);
                    returnedObject = new { model = aiModel, prompt = fullActionAndUserPrompt, temperature = calcTemp };
                    break;

                case AIModelType.CompletionTokens:    // Modèle Completion: Prompt + MaxTokens
                    messageToShow = string.Format(stringMaskCompletionPopupPrompt, serviceAndModel, typeString, fullActionAndUserTextShort, calcTemp, mdl.TokensMax);
                    returnedObject = new { model = aiModel, prompt = fullActionAndUserPrompt, temperature = calcTemp, max_tokens = mdl.TokensMax };
                    break;

                case AIModelType.CompletionMin:       // Modèle Completion: Prompt (no Prompt Context) 
                    messageToShow = string.Format(stringMaskCompletionPopupPrompt, serviceAndModel, typeString, minPrompt, calcTemp, notApplTokens);
                    returnedObject = new { model = aiModel, prompt = minPrompt, temperature = calcTemp };
                    break;

                default:                    // Unknown Active Model error
                    ErrorShow("ERROR_EDITOR_IAMODELUNKNOWN", svc.Context, actionPrompt, texteUtilisateur, mdl.TokensMax.ToString());
                    break;
            }

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
                aiMailerAIModeleActif = aiMailerAIServices?.SelectMany(s => s.Models ?? Enumerable.Empty<AIModel>()).FirstOrDefault(m => m.Default)           // modèle “par défaut”
                   ?? aiMailerAIServices?.SelectMany(s => s.Models ?? Enumerable.Empty<AIModel>()).FirstOrDefault(); // sinon, le premier modèle

                // Trouve le Service correspondant au Modèle par défaut ou sélectionne le premier par défaut
                aiMailerAIServiceActif = aiMailerAIServices?.FirstOrDefault(s => s.Models != null && s.Models.Contains(aiMailerAIModeleActif))
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
            this.Font = new Font(editeurTextFontFamily, editeurDefaultTextFontSize);

            // Charte graphique / ergonomie
            this.BackColor = editeurBackColor;

            //this.FormBorderStyle = FormBorderStyle.SizableToolWindow;

            // Ajout du Menu de la fenêtre
            int menuStripYOffset = InitialiserInterfaceMenu();

            // Ajout de la Texte Box Editeur
            InitialiserInterfaceEditeur(menuStripYOffset); // Pas de bouton IA

            // Ajout du Curseur de Sélection de la taille de la police
            InitialiserInterfaceEditeurCurseurFonte();

        }

        /// **********************************************************************
        /// *** Initialisation Text Box Editeur **********************************
        /// **********************************************************************
        private void InitialiserInterfaceEditeur(int menuStripYOffset)
        {
            // Taille Textbox 
            this.Text = aiMailerName;
            this.Size = new Size(
                        textWidth + 2 * textXOffset + 20, 
                        menuStripYOffset + textFontSliderHeight + textHeight + 2 * textYOffset + textYScrollbar
);

            // Zone de texte principale
            aiMailerEditor = new TextBox
            {
                Multiline = true,
                Name = aiMailerEditorName,
                Size = new Size(textWidth, textHeight),
                Font = new Font(this.Font.FontFamily, editeurDefaultTextFontSize),
                Location = new Point(textXOffset, menuStripYOffset + textYOffset),
                ScrollBars = ScrollBars.Vertical,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            // ************************************************
            // 🔁 MENU CONTEXTUEL 
            // ************************************************
            ContextMenu contextMenu = new ContextMenu();

            // 🔁 Menu contextuel : Actions IA
            // === NOUVEL ITEM ======================================================
            MenuItem iaActionsMenuItem = new MenuItem(textEditorActionsIAMenuLabel);
            iaActionsMenuItem.Click += (s, e) => OuvrirPaletteActions(true);

            contextMenu.MenuItems.Add(iaActionsMenuItem);
            contextMenu.MenuItems.Add("-");           // séparateur visuel (facultatif)

            // 🔁 Menu contextuel : Undo/Redo
            MenuItem undoMenuItem = new MenuItem(textEditorAnnulerMenuLabel);
            undoMenuItem.Click += (s, e) => EditorUndoLastChange();
            contextMenu.MenuItems.Add(undoMenuItem);
            MenuItem redoMenuItem = new MenuItem(textEditorRefaireMenuLabel);
            redoMenuItem.Click += (s, e) => EditorRedoLastChange();
            contextMenu.MenuItems.Add(redoMenuItem);
            contextMenu.MenuItems.Add("-");

            // 🔁 Menu contextuel : Erase
            MenuItem clearMenuItem = new MenuItem(textEditorEffacerMenuLabel);
            clearMenuItem.Click += (s, e) => EditorEraseText();
            contextMenu.MenuItems.Add(clearMenuItem);
            contextMenu.MenuItems.Add("-");

            // 🔁 Menu contextuel : Couper, Coller, Paste, Select all
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
                    EditorRedoLastChange();
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

            SetTextBoxMargins(aiMailerEditor, textEditorLeftMargin, textEditorRightMargin);

            // Gestion du Triple click et des actions IA
            aiMailerEditor.MouseDown += AiMailerEditor_MouseDown;
            aiMailerEditor.MouseUp += AiMailerEditor_MouseUp;
            aiMailerEditor.KeyUp += AiMailerEditor_KeyUp;
        }

        // 🔁 AJOUT UNDO : méthode pour annuler la dernière modification IA
        private void EditorUndoLastChange()
        {
            // Empile l'Editeur sur le Redo et le remplace par un Dépile du Undo 
            if (aiMailerUndoStack.Count > 0)
            {
                aiMailerRedoStack.Push(aiMailerEditor.Text ?? string.Empty);
                aiMailerEditor.Text = aiMailerUndoStack.Pop();
            }
            else
                SystemSounds.Beep.Play(); // Aucun texte à annuler
        }
        /// 🔁 REDO : rétablir après un undo
        private void EditorRedoLastChange()
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
        /// Effacer le texte de l'éditeur
        private void EditorEraseText()
        {
            // Empile l'Editeur sur le Undo et le remplace par un Dépile du Redo
            aiMailerUndoStack.Push(aiMailerEditor.Text);
            aiMailerRedoStack.Clear();
            aiMailerEditor.Clear();
        }

        /// 🔁 GESTION CLAVIER Ctrl+Z / Ctrl+Y
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            bool rtn = true;
            if (keyData == (Keys.Control | Keys.Z))
                EditorUndoLastChange();
            else if (keyData == (Keys.Control | Keys.Y))
                EditorRedoLastChange();
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
                Value = editeurDefaultTextFontSize,
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
                Text = textFontSliderLabel + editeurDefaultTextFontSize,
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

        private void AiMailerEditor_KeyUp (object sender, EventArgs e)
        {
               OuvrirPaletteActions();   // affiche la palette
        }

        private void AiMailerEditor_MouseUp(object sender, MouseEventArgs e)
        {
                OuvrirPaletteActions();
        }

        /////=== Méthode de gestion des clics de souris sur le TextBox ===
        private void AiMailerEditor_MouseDown(object sender, MouseEventArgs e)
        {
            
            var now = Environment.TickCount;

            // Vérifie si le clic est rapproché du précédent (double/triple clic)
            if (now - aiMailerEditorlastClickTime < SystemInformation.DoubleClickTime)
                aiMailerEditorClickCount++;
            else
                aiMailerEditorClickCount = 1; // Trop espacé → on recommence le comptage

            aiMailerEditorlastClickTime = now;

            // Si triple clic détecté → sélectionner la phrase entière
            if (aiMailerEditorClickCount == 3)
            {
                TripleClicSelectSentence((TextBox)sender);
                aiMailerEditorClickCount = 0; // Réinitialisation après action
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
            ToolStripMenuItem menuEffacer = new ToolStripMenuItem(textEditorEffacerMenuLabel);
            ToolStripMenuItem menuOuvrir = new ToolStripMenuItem(textFileMenuTextOpenLabel);
            ToolStripMenuItem menuEnregistrer = new ToolStripMenuItem(textFileMenuTextSaveLabel);

            menuAnnuler.Click += (s, e) => EditorUndoLastChange();
            menuRefaire.Click += (s, e) => EditorRedoLastChange();
            menuEffacer.Click += (s, e) => EditorEraseText();
            menuOuvrir.Click += MenuOuvrir_Click;
            menuEnregistrer.Click += MenuEnregistrer_Click;

            menuFichier.DropDownItems.Add(menuAnnuler);
            menuFichier.DropDownItems.Add(menuRefaire);
            menuFichier.DropDownItems.Add(new ToolStripSeparator());
            menuFichier.DropDownItems.Add(menuEffacer);
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
                foreach (var service in aiMailerAIServices.Where(s => s.Models != null))
                {
                    if (service.Models == null) continue;
                    foreach (var model in service.Models)
                    {
                        var item = new ToolStripMenuItem($"{service.Name} | {model.Name}");
                        item.Tag = new Tuple<AIService, AIModel>(service, model);
                        item.Click += (s, e) =>
                        {
                            var tagData = (Tuple<AIService, AIModel>)((ToolStripMenuItem)s).Tag;
                            aiMailerAIServiceActif = tagData.Item1;
                            aiMailerAIModeleActif = tagData.Item2;
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
                (aiMailerAIServiceActif == null ? aiMailerServiceAbsent : aiMailerAIServiceActif.Name),
                (aiMailerAIModeleActif == null ? aiMailerModeleAbsent : aiMailerAIModeleActif.Name),
                (aiMailerAIModeleActif == null ? aiMailerModeleAbsent : aiMailerAIModeleActif.Type.ToString()));
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
                    if (result == DialogResult.No)
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
            string msgLabel;

            if (!aiMailerErrorMsgs.TryGetValue(msgKey, out msgLabel))
                msgLabel = string.Format(maskErrorMsgUnknown, msgKey);

            string FormatLevel(string level, string label)
            {
                if (string.IsNullOrWhiteSpace(level)) return "";
                string content = level.Length <= aiMailerErrorStringLenghtMax ? level : level.Substring(0, aiMailerErrorStringLenghtMax) + aiMailerStringMsgTrunc;
                return "\n\n" + string.Format(aiMailerErrorLevelLabel, label) + content;
            }

            string fullMessage = msgLabel
                               + FormatLevel(errorLevel1, "1")
                               + FormatLevel(errorLevel2, "2")
                               + FormatLevel(errorLevel3, "3")
                               + FormatLevel(errorLevel4, "4")
                               + "\n\n[Modèle] " + BuildServiceAndModelLabel();

            MessageBox.Show(
                fullMessage,
                aiMailerErrorShowTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
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
                var globalService = aiMailerAIServiceActif;
                var globalModel = aiMailerAIModeleActif;
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
                    Service = aiMailerAIServiceActif,      // ton champ global
                    Model = aiMailerAIModeleActif,      // ton champ global
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
                    Text = aiMailerOkButtonText,
                    DialogResult = DialogResult.OK,
                    Left = dlg.ClientSize.Width - 200,
                    Width = 80,
                    Top = y
                };
                Button btnCancel = new Button
                {
                    Text = aiMailerCancelButtonText,
                    DialogResult = DialogResult.Cancel,
                    Left = btnOK.Right + 10,
                    Width = 80,
                    Top = y
                };
                dlg.Controls.Add(btnOK);
                dlg.Controls.Add(btnCancel);
                dlg.AcceptButton = btnOK;
                dlg.CancelButton = btnCancel;

                // ---------- Affichage ----------
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    // ← voici tout ce qu’il faut remplacer
                    action.Name = txtName.Text;

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
        private void OuvrirPaletteActions( bool contextMenuItem = false)
        {
            // Si appel du Menu de Context
            if (contextMenuItem)
            {
                // Lorsque aucun text (Context menu only)
                if (aiMailerEditor.Text == null || aiMailerEditor.Text == "")
                {
                    ErrorShow("ERROR_EDITOR_NOTEXT");
                    return;
                }
            }
            // sinon Verifie s'il existe une sélection (appel de la Souris ou bouton)
            else if (aiMailerEditor.SelectionLength == 0)
                return;

            // Si Palette existante → on la met devant et on sort
            if (aiMailerPaletteActions != null && !aiMailerPaletteActions.IsDisposed)
            {
                aiMailerPaletteActions.BringToFront();
                aiMailerEditor.Focus();
                return;
            }

            // ─── Mémorise la sélection courante ──────────────────────────
            int selStart0 = aiMailerEditor.SelectionStart;
            int selLength0 = aiMailerEditor.SelectionLength;

            // ─── Création de la palette ──────────────────────────────────
            aiMailerPaletteActions = new Form
            {
                FormBorderStyle = FormBorderStyle.None,       // plus de bordure ni de titre
                Text = aiMailerPaletteActionsTitle,
                MaximizeBox = false,                           // (par sécurité)
                MinimizeBox = false,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                TopMost = true,
                Font = this.Font,
                BackColor = this.BackColor,
                Opacity = 0.8,
                Owner = this
            };

            /* ▼▼ NOUVEAU bloc de positionnement ▼▼ */
            Point p = Cursor.Position;     // coordonnées écran de la souris
            Rectangle work = Screen.FromPoint(p).WorkingArea;

            int posX = p.X + actionPanelXOffset; 
            int posY = p.Y + actionPanelYOffset;
            
            // Si la palette dépasserait à droite, on la décale à gauche
            if (posX + aiMailerPaletteActions.Width > work.Right)
                posX = work.Right - aiMailerPaletteActions.Width;

            // Si elle dépasserait en bas, on la met au-dessus du pointeur
            if (posY + aiMailerPaletteActions.Height > work.Bottom)
                posY = p.Y - 3 * actionPanelYOffset; // - aiMailerPaletteActions.Height;
            
            aiMailerPaletteActions.Location = new Point(posX, posY);
            /* ▲▲ FIN du nouveau bloc ▲▲ */
            
            // ─── Panneau et boutons ──────────────────────────────────
            FlowLayoutPanel panel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            aiMailerPaletteActions.Controls.Add(panel);
            // Point position = Cursor.Position;
            // aiMailerPaletteActions.Location = new Point(position.X, position.Y);

            // ─── Panneau et boutons ──────────────────────────────────────
            
            aiMailerPaletteActions.Controls.Add(panel);

            int x = buttonXOffset;
            foreach (var action in aiMailerAIActions)
            {
                // Lit le fichier Icone du bouton (null si probleme => Bouton sera en texte)
                Bitmap iconBmp = null;
                try
                {
                    // ① chemin absolu ?  (facultatif, selon où se trouvent tes icônes)
                    string path = Path.IsPathRooted(action.Icon)
                                  ? action.Icon
                                  : Path.Combine(Application.StartupPath, action.Icon);

                    // ② using : l’objet Icon est IDisposable → libère la ressource GDI+
                    using (var ico = new Icon(path, new Size(buttonIconSize, buttonIconSize)))
                    {
                        iconBmp = ico.ToBitmap();
                    }
                }
                catch
                {
                    iconBmp = null;   // pas d’icône → le bouton affichera simplement son texte
                }

                // Crée le bouton Action
                Button btn = new Button
                {
                    Image = iconBmp,
                    ImageAlign = ContentAlignment.MiddleCenter, // Centre l'icône
                    Text = (iconBmp == null ? action.Name : string.Empty),
                    TextImageRelation = TextImageRelation.ImageBeforeText,
                    FlatStyle = FlatStyle.Standard, // ou System

                    Size = new Size(buttonWidth, buttonHeight),
                    Tag = action,
                    Font = new Font(this.Font.FontFamily, buttonTextFontSize),
                    Location = new Point(x, buttonYOffset),
                    BackColor = buttonBackColor,
                    ForeColor = buttonForeColor
                };

                // Affichage par timer du Libellé du bouton au survol sans focus
                btn.MouseEnter += (s, e) =>
                {
                    var b = (Button)s;
                    // point écran juste sous le bouton (centre X)
                    Point screen = b.PointToScreen(new Point(b.Width / 2, b.Height));
                    // on convertit pour le ToolTip : coordonnées dans LA fenêtre propriétaire
                    Point local = aiMailerPaletteActions.PointToClient(screen);
                    /* ← La seule ligne nécessaire pour un tip “flottant” */
                    hoverTip.SetToolTip(btn, action.Name);
                };
                // Effacement du Libellé du bouton au survol 
                btn.MouseLeave += (s, e) =>
                {
                    if (hoverTip != null)
                    {
                        try
                        {
                            hoverTip.Hide(aiMailerPaletteActions);
                        }
                        catch (Exception)
                        {
                            // Le hoverTip a été disposé entre-temps, on ignore silencieusement
                        }
                    }
                };


                // Action du bouton : Apple de l'IA
                btn.Click += async (s, _) => await AIMAilerAIMethod((AIAction)((Button)s).Tag);

                // menu contextuel « Configuration »
                ContextMenu ctx = new ContextMenu();
                ctx.MenuItems.Add(new MenuItem(actionPanelButtonCfgMenuLabel,
                    (_, __) => AfficherPanneauConfig(action)));
                btn.ContextMenu = ctx;

                panel.Controls.Add(btn);
                x += buttonWidth + buttonXSpace;
            }
            panel.Size = new Size(x, buttonHeight + 2 * buttonYOffset);
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
                // On ferme seulement si la sélection disparaît complètement
                if (aiMailerPaletteActions != null && !aiMailerPaletteActions.IsDisposed && aiMailerEditor.SelectionLength == 0)
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
            aiMailerEditor.Focus();
        }


        /// <summary>
        /// Barre indéterminée inspirée Material Design :
        /// un “pavé” glisse en boucle sur le fond.
        /// </summary>
        internal sealed class MaterialMarquee : Control
        {
            private readonly Timer _timer = new Timer();
            private int _offset;

            public Color BarColor { get; set; } = Color.DodgerBlue;
            public int SpeedPx { get; set; } = 4;       // pixels par tick
            public int BarWidthPx { get; set; } = 80;   // largeur du pavé

            public MaterialMarquee()
            {
                DoubleBuffered = true;
                SetStyle(ControlStyles.AllPaintingInWmPaint
                       | ControlStyles.UserPaint
                       | ControlStyles.OptimizedDoubleBuffer, true);

                Height = 6;                 // hauteur “fine”
                _timer.Interval = 16;       // ~60 Hz
                _timer.Tick += (s, e) =>
                {
                    _offset = (_offset + SpeedPx) % (Width * 2);
                    Invalidate();
                };
                _timer.Start();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.Clear(BackColor);

                using (var b = new SolidBrush(BarColor))
                {
                    int x = _offset - BarWidthPx;
                    // deux copies pour la boucle visuelle
                    e.Graphics.FillRectangle(b, x, 0, BarWidthPx, Height);
                    e.Graphics.FillRectangle(b, x - Width, 0, BarWidthPx, Height);
                }
            }

            // adapte la largeur du pavé si le contrôle est redimensionné
            protected override void OnSizeChanged(EventArgs e)
            {
                base.OnSizeChanged(e);
                BarWidthPx = Width / 3;
            }
        }

        /// <summary>
        /// Libellé flottant par timer au survol des boutons sans focus
        /// </summary>
        private readonly ToolTip hoverTip = new ToolTip
        {
            ShowAlways = true,
            UseFading = true,
            UseAnimation = true,
            InitialDelay = 0,      // apparition immédiate
            ReshowDelay = 0,
            AutoPopDelay = 1000    // disparaît automatiquement au bout de 3 s
        };
 

        /// <summary>
        /// Définit la marge interne gauche et droite (en pixels) d’une TextBox WinForms.
        /// </summary>
        private void SetTextBoxMargins(TextBox txt, int textEditorLeftMargin, int textEditorRightMargin)
        {
            const int EM_SETMARGINS = 0x00D3;   //message pour fixer les marges internes.
            const int EC_LEFTMARGIN = 0x0001;   //flags pour dire “je veux régler la marge gauche/droite”.
            const int EC_RIGHTMARGIN = 0x0002;

            IntPtr wParam = (IntPtr)(EC_LEFTMARGIN | EC_RIGHTMARGIN);
            int lParamValue = (textEditorRightMargin << 16) | (textEditorLeftMargin & 0xFFFF);
            IntPtr lParam = (IntPtr)lParamValue;
            SendMessage(txt.Handle, EM_SETMARGINS, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(
            IntPtr hWnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam   //lParam (LOWORD) : largeur en pixels de la marge gauche, (HIWORD) la marge droite. Ici on met textEditorLeftMargin dans le low-word et on laisse la droite à 0.
        );

    }

    // -----------------------------------------------------------------------------
    // Extensions "Stack-like" pour LinkedList<T>
    // Conserve Push / Pop tout en limitant la taille à 25 éléments.
    // -----------------------------------------------------------------------------
    internal static class LinkedListStackExtensions
    {
        /// Ajoute un élément en fin de liste (top de pile)
        public static void Push(this LinkedList<string> list, string value)
        {
            list.AddLast(value ?? string.Empty);

            // tronque l'historique au-delà de aiMailerUndoStackMaxNb
            if (list.Count > AIMailer.aiMailerUndoStackMaxItems)
                list.RemoveFirst();
        }

        /// Retire et retourne le dernier élément (top de pile)
        public static string Pop(this LinkedList<string> list)
        {
            if (list.Count == 0)
                return string.Empty; // Aucun texte à annuler

            string value = list.Last.Value;
            list.RemoveLast();
            return value;
        }
    }
}