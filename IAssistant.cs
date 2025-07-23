/******* ---------------------------------------------------------------------
 * *****    IAssistant      Intelligence Artificial-powered Office Assistant
 * ***** ---------------------------------------------------------------------
 * *****   
 * *****    IAssistant.cs   Main source file - Editor windows
 * *****
 * ***** -- Author -----------------------------------------------------------
 * *****
 * ***** -- (c) Ilian Mas (ESILV A1) / June 2025
 * *****
 * ***** -- Major Changes ----------------------------------------------------
 * *****    16/07/25 - Ilian Mas - Renammed to IAssistant
 * *****    26/05/25 - Ilian Mas - Initial version
 * ***** - -------------------------------------------------------------------
 ******/

using System;
using System.Collections.Generic;
using System.Drawing;
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
using System.Threading.Tasks;
using System.Windows.Forms;
using WinForms = System.Windows.Forms;

/* Context Prompt pour mémo 
Tu es un assistant IA aussi bien francophone qu'anglophone expert en rédaction, traduction et synthèse de texte. 
Tu réponds toujours en français clair et précis, sans jamais expliquer tes actions, sauf si demandé. 
Adapte ta réponse au style du texte original si c’est un extrait, et respecte les consignes suivantes : 
ne commente jamais les instructions, ne cite pas le texte source, et reste concis si le contexte le demande.
*/

namespace IAssistant
{
    public partial class IAssistant : WinForms.Form
    {
        // ***********************************************
        // ***** Noms et chaines de caractères ***********
        // ***********************************************
        private const string iAssistantConfigFile = "IAssistant.cfg";                     // 💾 CONFIG: fichier de configuration
        private const string iAssistantAutoSaveFile = "IAssistant.AutoSave.txt";          // 💾 AUTOSAVE : fichier de sauvegarde auto
        private const string iAssistantNotesFileDefault = "IAssistant.db";                // 💾 NOTES : fichier NoSQL liteDB
        private const string iAssistantNotesKeyVarDefault = "IASSISTANT_LITEDB_PASSWORD"; // 💾 NOTES : Password NoSQL liteDB
        private const string iAssistantNotesCollectionDefault = "notes";                  // 💾 NOTES : Collection NoSQL liteDB
        private const string iAssistantNotepadExe = "notepad.exe";
        private const string iAssistantUser32dll = "user32.dll";
        private const string iAssistantName = "IAssistant";
        private const string iAssistantEditorName = "IAssistantEditor";
        private const string iAssistantPaletteActionsTitle = "AI Assistance";
        private const string iAssistantErrorShowTitle = "Error " + iAssistantName;
        private const string iAssistantTextFileMenuTextOpenLabel = "Open file";
        private const string iAssistantTextFileMenuTextSaveLabel = "Save file to...";
        private const string iAssistantTextFileMenuConfigEditLabel = "Edit configuration";
        private const string iAssistantTextFileMenuRestartLabel = "Apply configuration...";
        private const string iAssistantTextEditorActionsIAMenuLabel = iAssistantPaletteActionsTitle + "...";
        private const string iAssistantTextEditorAnnulerMenuLabel = "Undo (Ctrl-Z)";
        private const string iAssistantTextEditorRefaireMenuLabel = "Redo (Ctrl-Y)";
        private const string iAssistantTextEditorEffacerMenuLabel = "Erase";
        private const string iAssistantTextEditorCouperMenuLabel = "Cut (Ctrl+X)";
        private const string iAssistantTextEditorCopierMenuLabel = "Copy (Ctrl+C)";
        private const string iAssistantTextEditorCollerMenuLabel = "Paste (Ctrl+V)";
        private const string iAssistantTextEditorSelectionnerMenuLabel = "Select All (Ctrl+A)";
        private const string iAssistantTextFontSliderLabel = "Font : ";
        private const string iAssistantTextFontSliderTip = "Change Editor Text Size";      // Hovertip Clider Font Size
        private const string iAssistantTextFileMenuTextLabel = "Text";
        private const string iAssistantConfigMenuTextLabel = "Configuration";
        private const string iAssistantTextFileMenuModeleLabel = "Models";
        private const string iAssistantTextFileMenuFilter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
        private const string iAssistantOkButtonText = "Ok";
        private const string iAssistantCancelButtonText = "Cancel";
        private const string iAssistantIACallTitle = "AI Call pending…";
        private const string iAssistantRestartWarningTitle = "Restart confirmation";
        private const string iAssistantRestartAutoSaveWarning = "The current text can not be saved.\nDo you want to restart ?";
        private const string iAssistantServiceAbsent = "Unknown Service";         // Service AI absent
        private const string iAssistantModeleAbsent = "Unknown Model";            // Modèle AI absent
        private const string iAssistantStringMaskServiceAndModel = "{0} | {1} | {2}";     // Masque d'affichage du Service, Modèle, et Type de Modèle
        private const string iAssistantStringMaskCompletionPopupPrompt = "[Model] {0}\n\n[Type] {1}\n\n[Prompt] {2}\n\n[Temperature] {3}\n\n[max_tokens] {4}\n\n";
        private const string iAssistantStringMaskChatPopupPrompt = "[Model] {0}\n\n[Type] {1}\n\n[System] {2}\n\n[User] {3}\n\n[Temperature] {4}\n\n[max_tokens] {5}\n\n";
        private const string iAssistantStringMaskChatPopupPromptNA = "N/A";
        private const string iAssistantTripleClicSentenceCars = ".?!\n";           // Ponctuation de début de phrase
        private const string iAssistantAICallMsgBoxTitle = "AI Call...";           // Timer Msg Box Titre        
        private const string iAssistantActionPanelButtonCfgMenuLabel = "⚙ Edit";  // Label Menu Configuraiton Bouton IA
        private const string iAssistantActionCfgTitle = "Configuration: ";
        private const string iAssistantActionCfgName = "Name:";
        private const string iAssistantActionCfgPrompt = "Prompt:";
        private const string iAssistantActionCfgTemperature = "Temperature:";
        private const string iAssistantActionCfgSvcModel = "Service / Model:";
        private const string iAssistantActionCfgModelDefault = "<Default model>";
        private const string iAssistantErrorLevelLabel = "[Level {0}] ";
        private const string iAssistantStringMsgTrunc = "...";
        private const int iAssistantFctButtonIconSize = 32;
        private const string iAssistantDicteeButtonOffIcon = "RecordOff32.ico";          // Icon Bouton Dictee à l'arret
        private const string iAssistantDicteeButtonText = "🎤";                          // Label Bouton Dictee à l'arret
        private const string iAssistantDicteeButtonOnIcon = "RecordOn32.ico";            // Icon Bouton Dictee à l'enregistrement
        private const string iAssistantDicteeButtonRecordText = "⏹";                    // Label Bouton Dictee à l'enregistrement
        private const string iAssistantRdvButtonIcon = "Rendezvous32.ico";               // Icon Bouton Envoi Rdv    
        private const string iAssistantRdvButtonText = "📅";                             // Label Bouton Envoi Rdv 
        private const string iAssistantCourrielButtonIcon = "SendEmail32.ico";           // Label Bouton Envoi email
        private const string iAssistantCourrielButtonText = "📨";                        // Label Bouton Envoi email
        private const string iAssistantNotesButtonIcon = "Notes32.ico";                  // 💾 NOTES : Button Icon
        private const string iAssistantNotesButtonText = "📝";                           // 💾 NOTES : Label du bouton NoSQL liteDB
        private const string iAssistantCourrielButtonTip = "Open Outlook Email...";       // Label Configuration Email - Titre
        private const string iAssistantCourrielConfigObject = "Object:";                 // Label Configuration Email - Object
        private const string iAssistantOutlookEmailSubjectDefault = "A définir";         // Label Configuration Email - Subject Email
        private const string iAssistantOutlookMeetingSubjectDefault = "A définir";       // Label Configuration Email - Subject RDV
        private const string iAssistantOutlookMeetingLocationDefault = "A définir";      // Label Configuration Email - Location
        private const string iAssistantDicteeStartButtonTip = "Start Vocal dictation";   // Tip boutton Dictee vocale
        private const string iAssistantDicteeStopButtonTip = "Stop Vocal dictation";     // Tip boutton Dictee vocale
        private const string iAssistantRdvButtonTip = "Open Outlook Meeting...";         // Tip boutton Envoi Rdv 
        private const string iAssistantNotesButtonTip = "Open Notes...";                 // Tip boutton NoSQL liteDB
        public const string iAssistantNotesFormSave = "📤 Save Note";                    // Label bouton Save note
        public const string iAssistantNotesFormNew = "＋ New Note";                      // Label bouton New note
        public const string iAssistantNotesFormTitle = "IAssistant Notes";               // Titre fenetre note

        public const int iAssistantDefaultTextFontSize = 11;         // Taille de police initiale
        private const int iAssistantUndoStackMaxItemsDefault = 999;   // Pas plus de 999 Undos par defaut (si pas en fichier de Config)
        private const int iAssistantPromptToShowLengthMax = 999;      // Pas plus de 999 car de Texte Utilisateur dans la fenetre de trace
        private const int iAssistantErrorStringLenghtMax = 200;       // Pas plus de 200 car à chaque niveau de la fenetre d'erreurs
        private const int iAssistantTextEditorLeftMargin = 10;        // Marge gauche Editeur
        private const int iAssistantTextEditorRightMargin = 5;        // Marge droite Editeur
        private const int iAssistantActionPanelXOffset = 0;           // Déclalage X du panneau d'Actions
        private const int iAssistantActionPanelYOffset = 10;          // Déclalage Y du panneau d'Actions
        private const int iAssistantEditeurHitGroupTimeMax = 500;     // Limite de temps (msec) pour le regroupement de texte (Undo)
        private const int iAssistantFctButtonRigthMargin = 10;        // Marge droite Boutons de Fonctions 
        private const int iAssistantFctButtonBottomMargin = 10;       // Marge Bas Boutons de Fonctions 
        private const int iAssistantRegexTimeoutMsec = 5000;          // Time-out Regex (préco sonarqube)
        private const float iAssistantDictationDefaultConfidence = (float)0.6; // Defautl Dictation Confidence

        // ******************************************************
        // ***** Caractéristiques des objets graphiques *********
        // ******************************************************
        // Font sizes
        public const string iAssistantEditeurTextFontFamily = "Inter";                                // Police par défaut (ou "Segoe UI")
        private const int iAssistantButtonTextFontSize = iAssistantDefaultTextFontSize - 1;            // Taille de police Boutons
        private const int iAssistantEditeurMenuFontSize = iAssistantButtonTextFontSize;                // Taille de police Menuq
        private const int iAssistantEditeurTextFontSizeMin = 6, iAssistantEditeurTextFontSizeMax = 30; // Tailles de police min & max Curseur de Polices
        private const int iAssistantDicteeButtonFontSize = iAssistantButtonTextFontSize + 10;          // Taille de police Texte Boutton Dictee
        // Tailles
        private const int iAssistantEditeurTextWidth = 800, iAssistantEditeurTextHeight = 400;         // Taille fenetre Editeur initiale
        private const int iAssistantTextFontSliderWidth = 200, iAssistantTextFontSliderHeight = 40;    // Taille du Curseur de police
        private const int iAssistantTextXOffset = 10, iAssistantTextYOffset = 10;
        private const int iAssistantTextXScrollbar = 25, iAssistantTextYScrollbar = 40;                // Taille Scrollbar Editeur
        private const int iAssistantIAButtonIconSize = 32;                                             // Taille Icones des Boutons
        private const int iAssistantButtonXOffset = 5, buttonYOffset = 5;                              // Decalage Boutons
        private const int iAssistantButtonXSpace = 5, buttonYSpace = 5;                                // Boutons IA - Espacement 
        private const int iAssistantButtonWidth = iAssistantIAButtonIconSize + 8;                      // Boutons IA - Largeur
        private const int iAssistantButtonHeight = iAssistantButtonWidth;                              // Boutons IA - Hauteur
        private static readonly Color iAssistantMyColorBluePale1 = ColorTranslator.FromHtml("#F7F9FC");
        private static readonly Color iAssistantMyColorBluePale2 = ColorTranslator.FromHtml("#E3EAF3");
        private static readonly Color iAssistantMyColorBlueDark = ColorTranslator.FromHtml("#1B3A57");
        private static readonly Color iAssistantEditeurMenuBackColor = iAssistantMyColorBluePale2;
        private static readonly Color iAssistantEditeurMenuForeColor = iAssistantMyColorBlueDark;
        private static readonly Color iAssistantEditeurCurseurForeColor = iAssistantMyColorBlueDark;
        public static readonly Color iAssistantEditeurBackColor = iAssistantMyColorBluePale1;
        public static readonly Color iAssistantButtonBackColor = iAssistantMyColorBluePale2;
        public static readonly Color iAssistantButtonForeColor = iAssistantMyColorBlueDark;

        // ********************************
        // ***** Error Messages ***********
        // ***************** **************
        private const string maskErrorMsgUnknown = "Code Erreur inconnu : {0}"; // Recois le code inconnu
        private static readonly Dictionary<string, string> iAssistantErrorMsgs = new Dictionary<string, string>
        {
            { "ERROR_EDITOR_NOTEXT",            "Please enter text..." },
            { "ERROR_EDITOR_IACALL",            "Error while calling IA!" },
            { "ERROR_EDITOR_IACALLTOKENUNKNOWN","Authentication token unknown!\nPlease set environment variable referenced below..." },
            { "ERROR_EDITOR_IACALLSERVICE",     "Error while calling IA : Service not running or not accessible!" },
            { "ERROR_EDITOR_IACALLTIMEOUT",     "Error while calling IA : Time-out whil calling Service!" },
            { "ERROR_EDITOR_NOTEDBKEY",         "Impossible to open DB file for Notes - Please check password (see Env. Var. below)!" },
            { "ERROR_EDITOR_NOTEDBOPEN",        "Error while trying to open liteDB file for Notes!" },
            { "ERROR_EDITOR_CFGFILEOPEN",       "Configuration file impossible to open!" },
            { "ERROR_EDITOR_CFGFILEBAD",        "Configuration file not compliant!" },
            { "ERROR_EDITOR_CFGFILEUNKNOWN",    "Configuration file impossible to find!" },
            { "ERROR_EDITOR_AUTOSAVEERR",       "Editor text impossible to save!" },
            { "ERROR_EDITOR_APPRESTART",        "Application impossible to restart !" },
            { "ERROR_EDITOR_IASERVICEUNKNOW",   "No AI service: AI Call impossible!" },
            { "ERROR_EDITOR_IAMODELUNKNOWN",    "Unknown AI model: AI call impossible!" },
            { "ERROR_EDITOR_REGEXTIMEOUT",      "Internal Error : Time-out on Regex call!" },
            { "ERROR_EDITOR_DICTATIONNOSTART",  "Error while trying to start vocal dictation!" },
            { "ERROR_EDITOR_OUTLOOKNOTRUNNING", "Please launch Outlook in order to allow interactions!" },
            { "ERROR_EDITOR_OUTLOOKSENDDIRECT", "Error while sending email : Please check that Outlook is running fine!" },
            { "ERROR_EDITOR_OUTLOOKSAVEDRAFT",  "Error while saving draft email : Please check that Outlook is running fine!" }
        };

        // *************************************************
        // ***** Variables "Globales" graphiques ***********
        // *************************************************
        public static TextBox iAssistantEditor = null;                                            // Text Box Editeur
        private static Form iAssistantPaletteActions = null;                                // Palette d'action 
        private IAssistantNoteForm iAssistantNoteForm = null;                               // 💾 fenêtre Notes

        // *****************************************************
        // ***** Variables "Globales" fonctionnelles ***********
        // *****************************************************
        private List<AIService> iAssistantAIServices = null;                                // Liste des Services IA configurés
        private List<AIAction> iAssistantAIActions = null;                                  // Liste des Modèles IA configurés
        private AIAppConfiguration iAssistantAppConfiguration = null;                         // Configuraiton interne
        private static AIService iAssistantAIServiceActif = null;                           // Ajout pour mémoriser le service actif
        private static AIModel iAssistantAIModeleActif = null;                              // Ajout pour mémoriser le modèle actif
        private readonly LinkedList<string> iAssistantUndoStack = new LinkedList<string>(); // 🔁 Pile (doublement chaînée) pour la fonction Undo 
        private readonly LinkedList<string> iAssistantRedoStack = new LinkedList<string>(); // 🔁 Pile (doublement chaînée) pour la fonction Redo
        private readonly Timer iAssistantEditeurHitGroupTimer = new Timer();                // Timer de regroupement de Texte ppour le Undo
        private bool iAssistantEditeurHitGroupActive = false;                               // L'utilisateur est-il en train de taper du texte ?
        private IAssistantVoiceDictation iAssistantDictationInstance = null;
        private bool iAssistantIsDictating = false;
        private Point iAssistantPaletteActionOffset = Point.Empty;                                  // Poisiton de la Palette d'Actions IA
        private string iAssistantNotesFile = iAssistantNotesFileDefault;                            // 💾 NOTES : fichier NoSQL liteDB
        private string iAssistantNotesKeyVar = iAssistantNotesKeyVarDefault;                        // 💾 NOTES : Password NoSQL liteDB
        private string iAssistantNotesCollection = iAssistantNotesCollectionDefault;               // 💾 NOTES : Collection NoSQL liteDB
        private int iAssistantEditeurDefaultTextFontSize = iAssistantDefaultTextFontSize;           // Taille de police Editeur initiale 
        private string iAssistantOutlookEmailSubject = iAssistantOutlookEmailSubjectDefault;        // Outlook : Subject Email
        private string iAssistantOutlookMeetingSubject = iAssistantOutlookMeetingSubjectDefault;    // Outlook : Subject Rdv
        private string iAssistantOutlookMeetingLocation = iAssistantOutlookMeetingLocationDefault;  // Outlook : Location
        private int iAssistantEditorlastClickTime = 0;                // Temps du dernier clic en msec (pour Triple clic)
        private int iAssistantEditorClickCount = 0;                   // Compteur de clics successifs (pour Triple clic)

        public static float iAssistantDictationConfidence = iAssistantDictationDefaultConfidence;   // Voice Dictation Confidence (0.0 - 1.0)
        public static int iAssistantUndoStackMaxItems = iAssistantUndoStackMaxItemsDefault;           // Undos Max

        // ------------------------------------------------------------------
        // Permet de retrouver rapidement le service ou le modèle à partir
        // des seuls ServiceId et ModelId de l'action.
        // ------------------------------------------------------------------
        private AIService GetServiceFor(AIAction action)
            => iAssistantAIServices.FirstOrDefault(s => s.Id == action.ServiceId);

        private AIModel GetModelFor(AIAction action)
            => GetServiceFor(action).Models.FirstOrDefault(m => m.Id == action.ModelId);


        ///// **********************************************************************
        ///// **********************************************************************
        ///// *****   Description des Services & Actions d'IA (Fichier de Config) **
        ///// *****   et configuration interne *************************************
        ///// **********************************************************************
        ///// **********************************************************************

        // Description du Fichier de Configuration
        private class IAssistantConfigFile
        {
            public List<AIAction> Actions { get; set; }            // Liste des Actions IA
            public List<AIService> Services { get; set; }          // Liste des Services IA
            public AIAppConfiguration Configuration { get; set; }     // Configuration interne de l'application
        }

        // Description des Types de Modèles IA 
        public enum AIModelType
        {
            Chat,             // Utilise le format messages (avec rôles: system, user)
            ChatTokens,       // Idem Chat avec Max Tokens
            ChatUser,         // Idem Chat mais avec Role User uniquement (sans Role System)
            ChatUserMin,      // Idem ChatUser mais sans Contexte de prompt
            ChatUserTokens,   // Idem ChatUser avec Max Tokens
            ChatAzure,        // Azure OpenAI Service - Champs d'authentification spécifique
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

        // Configuration interne Application
        private class AIAppConfiguration
        {
            public OutlookConfiguration Outlook { get; set; }
            public NotesConfiguration Notes{ get; set; }
            public EditorConfiguration Editor { get; set; }
        }
        private class OutlookConfiguration
        {
            public string EmailSubject { get; set; }
            public string MeetingSubject { get; set; }
            public string MeetingLocation { get; set; }
        }
        private class NotesConfiguration
        {
            public string File { get; set; }
            public string KeyVar { get; set; }
        }
        private class EditorConfiguration
        {
            public int UndoMax { get; set; }
            public int TextFontSize { get; set; }
            public float DictationConfidence { get; set; }
            public char TagPrefix { get; set; }
            public List<ModelConfiguration> Tags { get; set; }
        }
        private class ModelConfiguration
        {
            public string Tag { get; set; }
            public List<TagConfiguration> Models { get; set; }
        }
        private class TagConfiguration
        {
            public string Tag { get; set; }
            public string Text { get; set; }
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

        private async Task IAssistantAIMethod(AIAction action)
        {
            // 1) Lookup dynamique ou valeurs globales si override "Default"
            var svcLocal = string.IsNullOrEmpty(action.ServiceId) ? iAssistantAIServiceActif : GetServiceFor(action);
            var mdlLocal = string.IsNullOrEmpty(action.ModelId) ? iAssistantAIModeleActif : GetModelFor(action);

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

            string texteUtilisateur = string.IsNullOrWhiteSpace(iAssistantEditor.SelectedText)
                ? iAssistantEditor.Text
                : iAssistantEditor.SelectedText;
            if (string.IsNullOrWhiteSpace(texteUtilisateur))
            {
                ErrorShow("ERROR_EDITOR_NOTEXT", action.Name);
                return;
            }

            // 3) Construction du corps JSON (on passe svc et mdl)
            var (iaRequestBody, promptToShow) = IAssistantAIModelPrompt(action, texteUtilisateur, svcLocal, mdlLocal);
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
                Text = iAssistantIACallTitle,
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
                    { 
                        bearerToken = Environment.GetEnvironmentVariable(svcLocal.KeyVar);
                        // Erreur si la variable d'environnement est nulle
                        if (bearerToken == null)
                        { 
                            ErrorShow("ERROR_EDITOR_IACALLTOKENUNKNOWN", svcLocal.KeyVar);
                            return;
                        }
                    }

                    // Affectation de la Clé d'autorisation dans le Header de la requête
                    if (!string.IsNullOrEmpty(bearerToken))
                        // Au format attendu par Azure
                        if (mdlLocal.Type == AIModelType.ChatAzure)
                        {
                            client.DefaultRequestHeaders.Clear();
                            client.DefaultRequestHeaders.Add("api-key", bearerToken);
                        }
                        // Ou pour le format standard pour tous les autres services
                        else client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                    // Appel asychrone au Modèle du Service IA
                    var response = await client.PostAsync(mdlLocal.Url, iaRequestBodyJson);
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        ErrorShow("ERROR_EDITOR_IACALL", $"HTTP {(int)response.StatusCode} - {response.ReasonPhrase}");
                        return;
                    }

                    // Deserialisation de la reponse de l'IA
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

                        IAssistantAIReplyReplace(result?.Replace("\n", Environment.NewLine));
                    }
                }
                catch (HttpRequestException ex)  // Service not running / responding
                {
                    ErrorShow("ERROR_EDITOR_IACALLSERVICE", ex.Message, iaRequestBody.ToString());
                }
                catch (TaskCanceledException)    // Time-out on call
                {
                    ErrorShow("ERROR_EDITOR_IACALLTIMEOUT", iaRequestBody.ToString());
                }
                catch (SystemException ex)       // Other technical errors
                {
                    ErrorShow("ERROR_EDITOR_IACALL", ex.Message, iaRequestBody.ToString());
                }
                finally
                {
                    // ─── Nettoyage : fermeture de la boîte et ré-activation de l’appli
                    if (!waitDlg.IsDisposed) waitDlg.Close();
                    this.Enabled = true;
                    this.Activate();    // remet la fenêtre au premier plan
                    iAssistantEditor.Focus();
                }

            }
        }

        /// *************************************************************************
        /// ***** Construction du Prompt à envoyer à l'IA selon le Modèle actif *****
        /// *************************************************************************
        private (object Body, string Prompt) IAssistantAIModelPrompt(AIAction action, string texteUtilisateur, AIService svc, AIModel mdl)
        {
            // Temperature with model ratio
            decimal calcTemp = action.Temperature * (mdl.TemperatureRatio > 0 ? mdl.TemperatureRatio : 1);
            string aiModel = mdl.Model;
            string serviceAndModel = string.Format(iAssistantStringMaskServiceAndModel, svc.Name, mdl.Name, mdl.Type);
            string typeString = mdl.Type.ToString();
            string actionPrompt = action.Prompt;
            string minPrompt = actionPrompt + " " + texteUtilisateur;
            string fullActionPrompt = svc.Context + " " + actionPrompt;
            string fullActionAndUserPrompt = fullActionPrompt + " " + texteUtilisateur;
            string notApplString = iAssistantStringMaskChatPopupPromptNA;
            int notApplTokens = 0;
            string messageToShow = null;
            object returnedObject = null;

            // Enlever NewLine en doublons et tronquer "Texte Utilisateur" dans le message à afficher 
            //string userTextShort = Regex.Replace(texteUtilisateur, @"(\r?\n){2,}", Environment.NewLine);
            string userTextShort = RegexSafeReplace(texteUtilisateur, @"(\r?\n){2,}", Environment.NewLine);
            userTextShort = userTextShort.Length > iAssistantPromptToShowLengthMax
                            ? userTextShort.Substring(0, iAssistantPromptToShowLengthMax) + iAssistantStringMsgTrunc
                            : userTextShort;

            // Enlever NewLine en doublons et tronquer "Full Action Prompt" dans le message à afficher 
            // Timeout
            //string fullActionAndUserTextShort = Regex.Replace(fullActionAndUserPrompt, @"(\r?\n){2,}", Environment.NewLine);
            string fullActionAndUserTextShort = RegexSafeReplace(fullActionAndUserPrompt, @"(\r?\n){2,}", Environment.NewLine);
            fullActionAndUserTextShort = fullActionAndUserTextShort.Length > iAssistantPromptToShowLengthMax
                ? fullActionAndUserTextShort.Substring(0, iAssistantPromptToShowLengthMax) + iAssistantStringMsgTrunc
                : fullActionAndUserTextShort;

            // Build Prompt depending on Actif Model
            switch (mdl.Type)
            {
                case AIModelType.ChatAzure:           // Modèle Azure Chat
                case AIModelType.Chat:                // Modèle Chat : Roles System + User (standard)
                    messageToShow = string.Format(iAssistantStringMaskChatPopupPrompt, serviceAndModel, typeString, fullActionPrompt, userTextShort, calcTemp, notApplTokens);
                    returnedObject = new
                    {
                        model = aiModel,
                        messages = new[] { new { role = "system", content = fullActionPrompt }, new { role = "user", content = texteUtilisateur } },
                        temperature = calcTemp
                    };
                    break;

                case AIModelType.ChatTokens:          // Modèle ChatTokens: Roles System + User + MaxTokens
                    messageToShow = string.Format(iAssistantStringMaskChatPopupPrompt, serviceAndModel, typeString, fullActionPrompt, userTextShort, calcTemp, mdl.TokensMax);
                    returnedObject = new
                    {
                        model = aiModel,
                        messages = new[] { new { role = "system", content = fullActionPrompt }, new { role = "user", content = texteUtilisateur } },
                        temperature = calcTemp,
                        max_tokens = mdl.TokensMax
                    };
                    break;

                case AIModelType.ChatUser:            // Modèle ChatUser: Role User 
                    messageToShow = string.Format(iAssistantStringMaskChatPopupPrompt, serviceAndModel, typeString, notApplString, fullActionAndUserTextShort, calcTemp, notApplTokens);
                    returnedObject = new
                    {
                        model = aiModel,
                        messages = new[] { new { role = "user", content = fullActionAndUserPrompt } },
                        temperature = calcTemp
                    };
                    break;

                case AIModelType.ChatUserTokens:      // Modèle ChatUserTokens: Roles User + MaxTokens
                    messageToShow = string.Format(iAssistantStringMaskChatPopupPrompt, serviceAndModel, typeString, notApplString, fullActionAndUserTextShort, calcTemp, mdl.TokensMax);
                    returnedObject = new
                    {
                        model = aiModel,
                        messages = new[] { new { role = "user", content = fullActionAndUserPrompt } },
                        temperature = calcTemp,
                        max_tokens = mdl.TokensMax
                    };
                    break;

                case AIModelType.ChatUserMin:         // Modèle ChatTokens: Role User with min. Prompt (no Prompt Context)
                    messageToShow = string.Format(iAssistantStringMaskChatPopupPrompt, serviceAndModel, typeString, notApplString, minPrompt, calcTemp, notApplTokens);
                    returnedObject = new
                    {
                        model = aiModel,
                        messages = new[] { new { role = "user", content = minPrompt } },
                        temperature = calcTemp
                    };
                    break;

                case AIModelType.Completion:          // Modèle Completion: Prompt 
                    messageToShow = string.Format(iAssistantStringMaskCompletionPopupPrompt, serviceAndModel, typeString, fullActionAndUserTextShort, calcTemp, notApplTokens);
                    returnedObject = new { model = aiModel, prompt = fullActionAndUserPrompt, temperature = calcTemp };
                    break;

                case AIModelType.CompletionTokens:    // Modèle Completion: Prompt + MaxTokens
                    messageToShow = string.Format(iAssistantStringMaskCompletionPopupPrompt, serviceAndModel, typeString, fullActionAndUserTextShort, calcTemp, mdl.TokensMax);
                    returnedObject = new { model = aiModel, prompt = fullActionAndUserPrompt, temperature = calcTemp, max_tokens = mdl.TokensMax };
                    break;

                case AIModelType.CompletionMin:       // Modèle Completion: Prompt (no Prompt Context) 
                    messageToShow = string.Format(iAssistantStringMaskCompletionPopupPrompt, serviceAndModel, typeString, minPrompt, calcTemp, notApplTokens);
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
        private void IAssistantAIReplyReplace(string aiReponseTexte)
        {
            // 🔁 UNDO/REDO : sauvegarde l'état actuel, vide le redo
            iAssistantUndoStack.Push(iAssistantEditor.Text);
            iAssistantRedoStack.Clear();

            // Remplacement de l'intégralité du texte (si aucun texte n'est sélectionné)
            if (string.IsNullOrWhiteSpace(iAssistantEditor.SelectedText))
                iAssistantEditor.Text = aiReponseTexte;
            else
            // ou Remplacement du texte sélectionné
            {
                int selStart = iAssistantEditor.SelectionStart;
                int selLength = iAssistantEditor.SelectionLength;
                iAssistantEditor.Text = iAssistantEditor.Text.Substring(0, selStart) + aiReponseTexte +
                               iAssistantEditor.Text.Substring(selStart + selLength);
                iAssistantEditor.SelectionStart = selStart;
                iAssistantEditor.SelectionLength = aiReponseTexte.Length;
            }
        }

        /// **********************************************************************
        /// ***** Prise en compte de la réponse de l'IA dans l'Editeur ***********
        /// **********************************************************************
        public void IAssistantTextInsert(string textToInsert)
        {
            // 🔁 UNDO/REDO : sauvegarde l’état actuel, vide le redo
            iAssistantUndoStack.Push(iAssistantEditor.Text);
            iAssistantRedoStack.Clear();

            // Position et longueur de la sélection courante
            int selStart = iAssistantEditor.SelectionStart;
            int selLength = iAssistantEditor.SelectionLength;

            // Remplace la sélection (0 ⇒ simple insertion à la position du curseur)
            iAssistantEditor.Text =
                iAssistantEditor.Text.Substring(0, selStart) +
                textToInsert +
                iAssistantEditor.Text.Substring(selStart + selLength);

            // Place le curseur juste après le texte inséré
            iAssistantEditor.SelectionStart = selStart + textToInsert.Length;
            iAssistantEditor.SelectionLength = 0;                // aucune sélection
            iAssistantEditor.Focus();
        }

        ///// **********************************************************************
        ///// **********************************************************************
        ///// *** Initialisation Form Editeur **************************************
        ///// **********************************************************************
        ///// **********************************************************************

        // Initialisation de la fenêtre par appel à la fonction générée par Visual Studio
        public IAssistant()
        {
            InitializeComponent();       // Fonction générée par VS dans Form1.Designer
        }

        // lancement de l'application par la fct appelée après création de la fenêtre
        private void IAssistant_Load(object sender, EventArgs e)
        {
            LoadConfigurationFile();              // Lecture de la configuration de l'appli
            InitialiserInterface();               // Adaptation de la fenêtre
            RestoreEditorAutoSave();              // 💾 Restaure Autosave
            this.FormClosing += IAssistant_Close;
        }

        private void IAssistant_Close(object sender, EventArgs e)
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
            string configFilePath = Path.Combine(WinForms.Application.StartupPath, iAssistantConfigFile);
            //iAssistantAIActions = new List<AIAction>(); // Pour eviter les erreurs si pas de fichier

            // Erreur Fichier absent ou non accessible (droits)
            if (!File.Exists(configFilePath))
            {
                ErrorShow("ERROR_EDITOR_CFGFILEUNKNOWN", WinForms.Application.StartupPath, iAssistantConfigFile);
                return;
            }

            // Lecture et désérialisation du fichier de configuration
            try
            {
                // Lecture et parsing du fichier json
                string json = File.ReadAllText(configFilePath);

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                options.Converters.Add(new JsonStringEnumConverter()); // Lecture Enumeration (pr ModelType)

                var config = JsonSerializer.Deserialize<IAssistantConfigurationFile>(json, options);
                // Parsing des Actions et des Services
                iAssistantAIActions = config.Actions ?? new List<AIAction>();
                iAssistantAIServices = config.Services ?? new List<AIService>();

                /////////////////////////////////////////////////
                // Parsing de la configuration interne
                iAssistantAppConfiguration = config.Configuration ?? new AIAppConfiguration();
                /////////////////////////////////////////////////
                // Parsing Config outlook
                OutlookConfiguration emailCfg = iAssistantAppConfiguration.Outlook;
                iAssistantOutlookEmailSubject = ((emailCfg == null) || string.IsNullOrWhiteSpace(emailCfg.EmailSubject)) ? iAssistantOutlookEmailSubjectDefault : emailCfg.EmailSubject;
                iAssistantOutlookMeetingSubject = ((emailCfg == null) || string.IsNullOrWhiteSpace(emailCfg.MeetingSubject)) ? iAssistantOutlookMeetingSubjectDefault : emailCfg.MeetingSubject;
                iAssistantOutlookMeetingLocation = ((emailCfg == null) || string.IsNullOrWhiteSpace(emailCfg.MeetingLocation)) ? iAssistantOutlookMeetingLocationDefault : emailCfg.MeetingLocation;

                // Parsing Config Editeur
                EditorConfiguration editorCfg = iAssistantAppConfiguration.Editor;
                iAssistantEditeurDefaultTextFontSize = (editorCfg == null)
                                                    || (editorCfg.TextFontSize < iAssistantEditeurTextFontSizeMin)
                                                    || (editorCfg.TextFontSize > iAssistantEditeurTextFontSizeMax) ? iAssistantDefaultTextFontSize : editorCfg.TextFontSize;
                iAssistantDictationConfidence = (editorCfg == null) ? iAssistantDictationDefaultConfidence : editorCfg.DictationConfidence;
                iAssistantUndoStackMaxItems = (editorCfg == null) || (editorCfg.UndoMax < 0) || (editorCfg.UndoMax > iAssistantUndoStackMaxItemsDefault ) ? iAssistantUndoStackMaxItemsDefault : editorCfg.UndoMax;

                // Parsing Config Notes DB
                NotesConfiguration notesCfg = iAssistantAppConfiguration.Notes;
                iAssistantNotesFile = (notesCfg == null) || string.IsNullOrWhiteSpace(notesCfg.File) ? iAssistantNotesFileDefault : notesCfg.File;
                iAssistantNotesKeyVar = (notesCfg == null) || string.IsNullOrWhiteSpace(notesCfg.KeyVar) ? iAssistantNotesKeyVarDefault : notesCfg.KeyVar;

                // Trouve le Modèle par défaut ou sélectionne le premier par défaut
                iAssistantAIModeleActif = iAssistantAIServices?.SelectMany(s => s.Models ?? Enumerable.Empty<AIModel>()).FirstOrDefault(m => m.Default)           // modèle “par défaut”
                   ?? iAssistantAIServices?.SelectMany(s => s.Models ?? Enumerable.Empty<AIModel>()).FirstOrDefault(); // sinon, le premier modèle

                // Trouve le Service correspondant au Modèle par défaut ou sélectionne le premier par défaut
                iAssistantAIServiceActif = iAssistantAIServices?.FirstOrDefault(s => s.Models != null && s.Models.Contains(iAssistantAIModeleActif))
                    ?? iAssistantAIServices?.FirstOrDefault();
            }
            catch (SystemException ex)    // Erreur Fichier mal formatté
            {
                ErrorShow("ERROR_EDITOR_CFGFILEBAD", ex.Message, WinForms.Application.StartupPath, iAssistantConfigFile);
            }
        }

        // Structure de Parsing du fichier de configuration
        private class IAssistantConfigurationFile
        {
            public List<AIAction> Actions { get; set; }             // AI Actions
            public List<AIService> Services { get; set; }           // AI Services 
            public AIAppConfiguration Configuration { get; set; }   // Configuration interne

        }

        /// <summary>
        /// (Ré)écrit le fichier de configuration JSON de l’application
        /// à partir des listes en mémoire iAssistantAIServices et iAssistantAIActions.
        /// </summary>
        private void SaveConfigurationFile()
        {
            // 1. Prépare l’objet « racine » à sérialiser
            var config = new IAssistantConfigurationFile
            {
                Actions = iAssistantAIActions,
                Services = iAssistantAIServices,
                Configuration = iAssistantAppConfiguration
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
                string cfgPath = Path.Combine(WinForms.Application.StartupPath, iAssistantConfigFile);
                File.WriteAllText(cfgPath, json, Encoding.UTF8);

            }
            catch (SystemException ex)
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
            this.Font = new Font(iAssistantEditeurTextFontFamily, iAssistantEditeurDefaultTextFontSize);

            // Charte graphique / ergonomie
            this.BackColor = iAssistantEditeurBackColor;

            //this.FormBorderStyle = FormBorderStyle.SizableToolWindow;

            // Ajout du Menu de la fenêtre
            int menuStripYOffset = InitialiserInterfaceMenu();

            // Ajout de la Texte Box Editeur
            InitialiserInterfaceEditeur(menuStripYOffset); // Pas de bouton IA

            // Ajout du Curseur de Sélection de la taille de la police
            InitialiserInterfaceEditeurCurseurFonte();

            // Ajout des Boutons de Fonctions (dictee expérimentale, Email)
            InitialiserInterfaceEditeurBoutonsFonctions();

            // Initialiser le Timer de groupement de saisie (undo/redo)
            InitialiserInterfaceHitGroupTimer();



        }

        /// **********************************************************************
        /// *** Initialisation Text Box Editeur **********************************
        /// **********************************************************************
        private void InitialiserInterfaceEditeur(int menuStripYOffset)
        {
            // Taille Textbox 
            this.Text = iAssistantName;
            this.Size = new Size(iAssistantEditeurTextWidth + 2 * iAssistantTextXOffset + 20,
                                menuStripYOffset + iAssistantTextFontSliderHeight + iAssistantEditeurTextHeight
                                + 2 * iAssistantTextYOffset + iAssistantTextYScrollbar);

            // ************************************************
            // 🔁 Zone de texte principale
            // ************************************************
            iAssistantEditor = new TextBox
            {
                Multiline = true,
                Name = iAssistantEditorName,
                Size = new Size(iAssistantEditeurTextWidth, iAssistantEditeurTextHeight),
                Font = new Font(this.Font.FontFamily, iAssistantEditeurDefaultTextFontSize),
                Location = new Point(iAssistantTextXOffset, menuStripYOffset + iAssistantTextYOffset),
                ScrollBars = ScrollBars.Vertical,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            // ************************************************
            // 🔁 MENU CONTEXTUEL 
            // ************************************************
            ContextMenu contextMenu = new ContextMenu();

            // 🔁 Menu contextuel : Actions IA
            // === NOUVEL ITEM ======================================================
            MenuItem iaActionsMenuItem = new MenuItem(iAssistantTextEditorActionsIAMenuLabel);
            iaActionsMenuItem.Click += (s, e) => OuvrirPaletteActions(true);

            contextMenu.MenuItems.Add(iaActionsMenuItem);
            contextMenu.MenuItems.Add("-");           // séparateur visuel (facultatif)

            // 🔁 Menu contextuel : Undo/Redo
            MenuItem undoMenuItem = new MenuItem(iAssistantTextEditorAnnulerMenuLabel);
            undoMenuItem.Click += (s, e) => EditorUndoLastChange();
            contextMenu.MenuItems.Add(undoMenuItem);
            MenuItem redoMenuItem = new MenuItem(iAssistantTextEditorRefaireMenuLabel);
            redoMenuItem.Click += (s, e) => EditorRedoLastChange();
            contextMenu.MenuItems.Add(redoMenuItem);
            contextMenu.MenuItems.Add("-");

            // 🔁 Menu contextuel : Erase
            MenuItem clearMenuItem = new MenuItem(iAssistantTextEditorEffacerMenuLabel);
            clearMenuItem.Click += (s, e) => EditorEraseText();
            contextMenu.MenuItems.Add(clearMenuItem);
            contextMenu.MenuItems.Add("-");

            // 🔁 Menu contextuel : Couper, Coller, Paste, Select all
            MenuItem cutMenuItem = new MenuItem(iAssistantTextEditorCouperMenuLabel);
            cutMenuItem.Click += (s, e) =>
            {
                iAssistantUndoStack.Push(iAssistantEditor.Text);
                iAssistantRedoStack.Clear();
                iAssistantEditor.Cut();
            };
            contextMenu.MenuItems.Add(cutMenuItem);
            MenuItem copyMenuItem = new MenuItem(iAssistantTextEditorCopierMenuLabel);
            copyMenuItem.Click += (s, e) => iAssistantEditor.Copy();
            contextMenu.MenuItems.Add(copyMenuItem);
            MenuItem pasteMenuItem = new MenuItem(iAssistantTextEditorCollerMenuLabel);
            pasteMenuItem.Click += (s, e) =>
            {
                iAssistantUndoStack.Push(iAssistantEditor.Text);
                iAssistantRedoStack.Clear();
                iAssistantEditor.Paste();
            };
            contextMenu.MenuItems.Add(pasteMenuItem);
            MenuItem selectAllMenuItem = new MenuItem(iAssistantTextEditorSelectionnerMenuLabel);

            selectAllMenuItem.Click += (s, e) => iAssistantEditor.SelectAll();
            contextMenu.MenuItems.Add(selectAllMenuItem);

            // Gestion du Undo pour l'écriture 
            iAssistantEditor.KeyDown += AiMailerEditor_KeyDown;

            iAssistantEditor.ContextMenu = contextMenu;
            this.Controls.Add(iAssistantEditor);

            SetTextBoxMargins(iAssistantEditor, iAssistantTextEditorLeftMargin, iAssistantTextEditorRightMargin);

            // Gestion du Triple click et des actions IA
            iAssistantEditor.MouseDown += AiMailerEditor_MouseDown;
            iAssistantEditor.MouseUp += AiMailerEditor_MouseUp;
            iAssistantEditor.KeyUp += AiMailerEditor_KeyUp;
        }

        // Gestion des frappes clavier pour le Undo / Redo 
        private void AiMailerEditor_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+Y → Redo
            if (e.Control && e.KeyCode == Keys.Y)
            {
                EditorRedoLastChange();
                e.SuppressKeyPress = true;
            }
            // Ctrl+Z → Undo
            else if (e.Control && e.KeyCode == Keys.Z)
            {
                EditorUndoLastChange();
                e.SuppressKeyPress = true;
            }
            // Gestion du regroupement des touches pour le Undo 
            // Toute autre frappe (pas Ctrl, Alt ou Shift seul) → possible début ou poursuite d'un bloc d'Undo
            else if (!e.Control && !e.Alt && e.KeyCode != Keys.ShiftKey)
            {
                // Si on n'est pas déjà dans un bloc, on en crée un (push initial)
                if (!iAssistantEditeurHitGroupActive)
                {
                    iAssistantUndoStack.Push(iAssistantEditor.Text);
                    iAssistantRedoStack.Clear();
                    iAssistantEditeurHitGroupActive = true;
                }
                // On redémarre le timer pour prolonger le bloc
                iAssistantEditeurHitGroupTimer.Stop();
                iAssistantEditeurHitGroupTimer.Start();
            }
        }

        // 🔁 AJOUT UNDO : méthode pour annuler la dernière modification IA
        private void EditorUndoLastChange()
        {
            // Empile l'Editeur sur le Redo et le remplace par un Dépile du Undo 
            if (iAssistantUndoStack.Count > 0)
            {
                iAssistantRedoStack.Push(iAssistantEditor.Text ?? string.Empty);
                iAssistantEditor.Text = iAssistantUndoStack.Pop();
            }
            else
                SystemSounds.Beep.Play(); // Aucun texte à annuler
        }
        /// 🔁 REDO : rétablir après un undo
        private void EditorRedoLastChange()
        {
            // Empile l'Editeur sur le Undo et le remplace par un Dépile du Redo
            if (iAssistantRedoStack.Count > 0)
            {
                iAssistantUndoStack.Push(iAssistantEditor.Text);
                iAssistantEditor.Text = iAssistantRedoStack.Pop();
            }
            else
                SystemSounds.Beep.Play();
        }
        /// Effacer le texte de l'éditeur
        private void EditorEraseText()
        {
            // Empile l'Editeur sur le Undo et le remplace par un Dépile du Redo
            iAssistantUndoStack.Push(iAssistantEditor.Text);
            iAssistantRedoStack.Clear();
            iAssistantEditor.Clear();
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
            string autosavePath = Path.Combine(WinForms.Application.StartupPath, iAssistantAutoSaveFile);
            if (File.Exists(autosavePath))
            {
                iAssistantEditor.Text = File.ReadAllText(autosavePath);
            }
        }

        /// Curseur de changement de taille de fonte
        private void InitialiserInterfaceEditeurCurseurFonte()
        {

            // Curseur pour la taille du texte
            TrackBar fontSizeSlider = new TrackBar
            {
                Minimum = iAssistantEditeurTextFontSizeMin,
                Maximum = iAssistantEditeurTextFontSizeMax,
                Value = iAssistantEditeurDefaultTextFontSize,
                TickFrequency = 2,
                SmallChange = 1,
                LargeChange = 2,
                Orientation = Orientation.Horizontal,
                Location = new Point(iAssistantTextXOffset, iAssistantEditor.Bottom + 10),
                Width = iAssistantTextFontSliderWidth,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };

            // Étiquette pour afficher la taille actuelle
            Label fontSizeLabel = new Label
            {
                Text = iAssistantTextFontSliderLabel + iAssistantEditeurDefaultTextFontSize,
                Font = new Font(this.Font.FontFamily, iAssistantEditeurMenuFontSize),
                ForeColor = iAssistantEditeurCurseurForeColor,
                Location = new Point(fontSizeSlider.Right + 10, fontSizeSlider.Top + 5),
                AutoSize = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };

            // Événement : met à jour la taille de la police
            fontSizeSlider.Scroll += (s, e) =>
            {
                int newSize = fontSizeSlider.Value;
                iAssistantEditor.Font = new Font(iAssistantEditor.Font.FontFamily, newSize);
                fontSizeLabel.Text = iAssistantTextFontSliderLabel + newSize;
            };


            // Ajout à la fenêtre
            this.Controls.Add(fontSizeSlider);
            this.Controls.Add(fontSizeLabel);
            hoverTip.SetToolTip(fontSizeSlider, iAssistantTextFontSliderTip);
        }

        /// Bouton de dictée (expérimental) & Email
        private void InitialiserInterfaceEditeurBoutonsFonctions()
        {
            Font btnFont = new Font(this.Font.FontFamily, iAssistantDicteeButtonFontSize);
            int spacing = iAssistantFctButtonRigthMargin;

            ///
            /// Bouton de Dictee Vocale 
            ///
            // Lit le fichier Icone du bouton (null si probleme => Bouton sera en texte)
            var ico = new Icon(Path.Combine(WinForms.Application.StartupPath, iAssistantDicteeButtonOffIcon),
                                            new Size(iAssistantFctButtonIconSize, iAssistantFctButtonIconSize));
            Bitmap iconBmp = ico.ToBitmap();
            Button btnDictee = new Button
            {
                Image = iconBmp,
                ImageAlign = ContentAlignment.BottomCenter, // Centre l'icône
                Text = (iconBmp == null ? iAssistantDicteeButtonText : string.Empty),
                Width = iAssistantFctButtonIconSize + 8,
                Height = iAssistantFctButtonIconSize + 8,
                Font = btnFont,
                FlatStyle = FlatStyle.Flat,
                // On ancre à droite ET en bas
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnDictee.FlatAppearance.BorderSize = 0;

            // Quand la Form est initialisée, ClientSize est déjà défini
            btnDictee.Left = this.ClientSize.Width - btnDictee.Width - iAssistantFctButtonRigthMargin;
            btnDictee.Top = this.ClientSize.Height - btnDictee.Height - iAssistantFctButtonBottomMargin + 5;


            btnDictee.Click += (s, e) => EditeurDemarrerOuArreterDictee((Button)s);
            this.Controls.Add(btnDictee);
            hoverTip.SetToolTip(btnDictee, iAssistantDicteeStartButtonTip);

            ///
            /// Bouton Notes             
            ///
            ico = new Icon(Path.Combine(WinForms.Application.StartupPath, iAssistantNotesButtonIcon),
                                                new Size(iAssistantFctButtonIconSize, iAssistantFctButtonIconSize));
            iconBmp = ico.ToBitmap();
            Button btnNotes = new Button
            {
                Image = iconBmp,
                ImageAlign = ContentAlignment.BottomCenter, // Centre l'icône
                Text = (iconBmp == null ? iAssistantNotesButtonText : string.Empty),
                Width = iAssistantFctButtonIconSize + 8,
                Height = iAssistantFctButtonIconSize + 8,
                Font = btnFont,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnNotes.FlatAppearance.BorderSize = 0;

            // On le place juste à gauche de btnDictee
            btnNotes.Left = btnDictee.Left - btnNotes.Width - spacing;
            btnNotes.Top = this.ClientSize.Height - btnDictee.Height - iAssistantFctButtonBottomMargin + 5;  //btnDictee.Top;

            // Associez ici votre méthode d'envoi
            btnNotes.Click += (s, e) => {
                // Par exemple, récupérer le contenu et appeler votre SMTP/EWS
                if (iAssistantNoteForm != null && !iAssistantNoteForm.IsDisposed)
                    iAssistantNoteForm.BringToFront();
                else { 
                    iAssistantNoteForm = new IAssistantNoteForm(iAssistantNotesFile, iAssistantNotesKeyVar, iAssistantNotesCollection);
                    if (iAssistantNoteForm != null && !iAssistantNoteForm.IsDisposed) 
                        iAssistantNoteForm.Show();
                }
            };
            this.Controls.Add(btnNotes);
            hoverTip.SetToolTip(btnNotes, iAssistantNotesButtonTip);

            ///
            /// Bouton d'envoi d'email             
            ///
            ico = new Icon(Path.Combine(WinForms.Application.StartupPath, iAssistantCourrielButtonIcon),
                                                new Size(iAssistantFctButtonIconSize, iAssistantFctButtonIconSize));
            iconBmp = ico.ToBitmap();
            Button btnEnvoyer = new Button
            {
                Image = iconBmp,
                ImageAlign = ContentAlignment.BottomCenter, // Centre l'icône
                Text = (iconBmp == null ? iAssistantCourrielButtonText : string.Empty),
                Width = iAssistantFctButtonIconSize + 8,
                Height = iAssistantFctButtonIconSize + 8,
                Font = btnFont,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnEnvoyer.FlatAppearance.BorderSize = 0;

            // On le place juste à gauche de btnDictee
            btnEnvoyer.Left = btnNotes.Left - btnEnvoyer.Width - spacing;
            btnEnvoyer.Top = this.ClientSize.Height - btnNotes.Height - iAssistantFctButtonBottomMargin + 5;  //btnDictee.Top;

            // Associez ici votre méthode d'envoi
            btnEnvoyer.Click += (s, e) => {
                // Par exemple, récupérer le contenu et appeler votre SMTP/EWS
                var contenu = iAssistantEditor.Text;
                IAssistantOutlook.OpenEmail(contenu, iAssistantOutlookEmailSubject);
            };
            this.Controls.Add(btnEnvoyer);
            hoverTip.SetToolTip(btnEnvoyer, iAssistantCourrielButtonTip);

            ///
            /// Bouton d'envoi de Rendez-vous             
            ///
            ico = new Icon(Path.Combine(WinForms.Application.StartupPath, iAssistantRdvButtonIcon),
                                            new Size(iAssistantFctButtonIconSize, iAssistantFctButtonIconSize));
            iconBmp = ico.ToBitmap();
            Button btnRdv = new Button
            {
                Image = iconBmp,
                ImageAlign = ContentAlignment.BottomCenter, // Centre l'icône
                Text = (iconBmp == null ? iAssistantRdvButtonText : string.Empty),
                Width = iAssistantFctButtonIconSize + 8,
                Height = iAssistantFctButtonIconSize + 8,
                Font = btnFont,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnRdv.FlatAppearance.BorderSize = 0;

            // On le place juste à gauche de btnEnvoyer
            btnRdv.Left = btnEnvoyer.Left - btnRdv.Width - spacing;
            btnRdv.Top = this.ClientSize.Height - btnRdv.Height - iAssistantFctButtonBottomMargin + 5;  //btnEnvoyer.Top;

            // Associez ici votre méthode d'envoi
            btnRdv.Click += (s, e) => {
                // Par exemple, récupérer le contenu et appeler votre SMTP/EWS
                var contenu = iAssistantEditor.Text;
                IAssistantOutlook.OpenRdv(contenu, iAssistantOutlookMeetingSubject, iAssistantOutlookMeetingLocation);
            };
            this.Controls.Add(btnRdv);
            hoverTip.SetToolTip(btnRdv, iAssistantRdvButtonTip);

        }

        /// Initialisation du Timer pour regroupement du texte entré (fonction Undo)
        private void InitialiserInterfaceHitGroupTimer()
        {
            iAssistantEditeurHitGroupTimer.Interval = iAssistantEditeurHitGroupTimeMax; // Set timer
            iAssistantEditeurHitGroupTimer.Tick += (s, e) =>
            {
                iAssistantEditeurHitGroupTimer.Stop();
                iAssistantEditeurHitGroupActive = false;  // le prochain caractère redémarrera un nouveau groupe
            };

        }
        private void AiMailerEditor_KeyUp(object sender, EventArgs e)
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
            if (now - iAssistantEditorlastClickTime < SystemInformation.DoubleClickTime)
                iAssistantEditorClickCount++;
            else
                iAssistantEditorClickCount = 1; // Trop espacé → on recommence le comptage

            iAssistantEditorlastClickTime = now;

            // Si triple clic détecté → sélectionner la phrase entière
            if (iAssistantEditorClickCount == 3)
            {
                TripleClicSelectSentence((TextBox)sender);
                iAssistantEditorClickCount = 0; // Réinitialisation après action
            }

        }

        // === Méthode pour sélectionner automatiquement une phrase entière autour du curseur ===
        private void TripleClicSelectSentence(TextBox box)
        {
            int pos = box.SelectionStart;
            string text = box.Text;

            // Recherche du début de la phrase (jusqu'à une ponctuation ou début de texte)
            int start = pos;
            while (start > 0 && !iAssistantTripleClicSentenceCars.Contains(text[start - 1]))
                start--;

            // Recherche de la fin de la phrase (jusqu'à une ponctuation ou fin de texte)
            int end = pos;
            while (end < text.Length && !iAssistantTripleClicSentenceCars.Contains(text[end]))
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
            Font fonte = new Font(this.Font.FontFamily, iAssistantEditeurMenuFontSize);

            // Création de la barre de menu
            MenuStrip menuStrip = new MenuStrip() { Font = fonte, BackColor = iAssistantEditeurMenuBackColor, ForeColor = iAssistantEditeurMenuForeColor };

            // Création du menu "Fichier"
            ToolStripMenuItem menuFichier = new ToolStripMenuItem(iAssistantTextFileMenuTextLabel);
            ToolStripMenuItem menuAnnuler = new ToolStripMenuItem(iAssistantTextEditorAnnulerMenuLabel);
            ToolStripMenuItem menuRefaire = new ToolStripMenuItem(iAssistantTextEditorRefaireMenuLabel);
            ToolStripMenuItem menuEffacer = new ToolStripMenuItem(iAssistantTextEditorEffacerMenuLabel);
            ToolStripMenuItem menuOuvrir = new ToolStripMenuItem(iAssistantTextFileMenuTextOpenLabel);
            ToolStripMenuItem menuEnregistrer = new ToolStripMenuItem(iAssistantTextFileMenuTextSaveLabel);

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
            ToolStripMenuItem menuConfig = new ToolStripMenuItem(iAssistantConfigMenuTextLabel);
            ToolStripMenuItem menuEditerConfig = new ToolStripMenuItem(iAssistantTextFileMenuConfigEditLabel);
            ToolStripMenuItem menuActualiserConfig = new ToolStripMenuItem(iAssistantTextFileMenuRestartLabel);

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
                Font = new Font(this.Font.FontFamily, iAssistantEditeurMenuFontSize - 1),
                ForeColor = iAssistantEditeurMenuForeColor,
                Alignment = ToolStripItemAlignment.Right,
                Margin = new Padding(0, 0, iAssistantTextXOffset, 0)
            };
            menuStrip.Items.Add(labelServiceModel);

            // Ajout de l'ensemble du Menu à la fenêtre
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);

            /// ********************************************************
            /// ***** Création du menu "Services et Modèles" ***********
            /// ********************************************************
            // ——— Menu "Modèles" unifié ———
            ToolStripMenuItem menuService = new ToolStripMenuItem(iAssistantTextFileMenuModeleLabel);
            if (iAssistantAIServices != null)
            {
                bool firstService = true;
                foreach (var service in iAssistantAIServices.Where(s => s.Models != null))
                {
                    if (service.Models == null) continue;

                    // Ajoute un séparateur avant chaque service sauf le premier
                    if (!firstService)
                        menuService.DropDownItems.Add(new ToolStripSeparator());
                    firstService = false;

                    foreach (var model in service.Models)
                    {
                        var item = new ToolStripMenuItem($"{service.Name} | {model.Name}");
                        item.Tag = new Tuple<AIService, AIModel>(service, model);
                        item.Click += (s, e) =>
                        {
                            var tagData = (Tuple<AIService, AIModel>)((ToolStripMenuItem)s).Tag;
                            iAssistantAIServiceActif = tagData.Item1;
                            iAssistantAIModeleActif = tagData.Item2;
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
        private static string BuildServiceAndModelLabel()
        {
            return string.Format(iAssistantStringMaskServiceAndModel,
                (iAssistantAIServiceActif == null ? iAssistantServiceAbsent : iAssistantAIServiceActif.Name),
                (iAssistantAIModeleActif == null ? iAssistantModeleAbsent : iAssistantAIModeleActif.Name),
                (iAssistantAIModeleActif == null ? iAssistantModeleAbsent : iAssistantAIModeleActif.Type.ToString()));
        }

        /// ********************************************************
        /// ***** Action des Menus *********************************
        /// ********************************************************
        /// 
        // Menu Fichier : Ouvrir un fichier texte et le copier dans l'Editeur
        private void MenuOuvrir_Click(object sender, EventArgs e)
        {
            // Choisir et Ouvrir le fichier 
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = iAssistantTextFileMenuFilter };
            // Copier son contenu dans l'Editeur
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                iAssistantUndoStack.Push(iAssistantEditor.Text);
                iAssistantEditor.Text = System.IO.File.ReadAllText(openFileDialog.FileName);
            }
        }

        // Menu Fichier : Enregistrer le contenu de l'Editeur dans un fichier
        private void MenuEnregistrer_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog { Filter = iAssistantTextFileMenuFilter };
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                System.IO.File.WriteAllText(saveFileDialog.FileName, iAssistantEditor.Text);
        }

        // Menu Config : Editer le Fichier de Configuration avec un notepad externe
        private void MenuEditerConfig_Click(object sender, EventArgs e)
        {
            // Vérifie si le fichier existe 
            string configFilePath = Path.Combine(WinForms.Application.StartupPath, iAssistantConfigFile);
            if (File.Exists(configFilePath))
            {
                // Lancer le notepad externe avec le fichier
                try
                {
                    System.Diagnostics.Process.Start(iAssistantNotepadExe, configFilePath);
                }
                catch (SystemException ex)
                {
                    ErrorShow("ERROR_EDITOR_CFGFILEOPEN", ex.Message, iAssistantNotepadExe, WinForms.Application.StartupPath, iAssistantConfigFile);
                }
            }
            // Erreur sur absence de fichier de configuration
            else ErrorShow("ERROR_EDITOR_CFGFILEUNKNOWN", WinForms.Application.StartupPath, iAssistantConfigFile);
        }

        // Sauvegarde du texte dans le fichier AutoSave
        private bool EditorAutoSave(bool signalerErreurP = true)
        {
            bool okP = true;
            try
            {
                File.WriteAllText(Path.Combine(WinForms.Application.StartupPath, iAssistantAutoSaveFile), iAssistantEditor.Text);
            }
            catch (SystemException ex)
            {
                okP = false;
                if (signalerErreurP)
                    ErrorShow("ERROR_EDITOR_AUTOSAVEERR", ex.Message, WinForms.Application.StartupPath, iAssistantAutoSaveFile);
            }
            return okP;
        }

        // Menu Config : Relancer l'application pour relire la configuration
        private void MenuActualiserConfig_Click(object sender, EventArgs e)
        {
            // Demander une confirmation de relance si l'éditeur contient du texte
            if (!string.IsNullOrWhiteSpace(iAssistantEditor.Text))
            {
                // Sauvegarde du contenu de l'éditeur dans un fichier local
                if (!EditorAutoSave(false))
                {
                    // Si impossible demande de confirmation à l'utilisateur
                    DialogResult result = MessageBox.Show(iAssistantRestartAutoSaveWarning, iAssistantRestartWarningTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (result == DialogResult.No)
                        return; // Annuler le redémarrage si refus de l'utilisateur
                }
            }
            // Relancer l'application 
            try
            {
                WinForms.Application.Restart();
            }
            catch (SystemException ex)
            {
                // Erreur sur relance
                ErrorShow("ERROR_EDITOR_APPRESTART", ex.Message);
            }
        }

        /// Gestion de la Dictée Vocale
        private void EditeurDemarrerOuArreterDictee(Button sourceButton = null)
        {

            if (iAssistantIsDictating)
            {
                iAssistantDictationInstance?.Stop();
                iAssistantIsDictating = false;
                if (sourceButton != null)
                {
                    var ico = new Icon(Path.Combine(WinForms.Application.StartupPath, iAssistantDicteeButtonOffIcon),
                                new Size(iAssistantFctButtonIconSize, iAssistantFctButtonIconSize));
                    Bitmap iconBmp = ico.ToBitmap();
                    sourceButton.Image = iconBmp;
                    hoverTip.SetToolTip(sourceButton, iAssistantDicteeStartButtonTip);
                }
                return;
            }

            iAssistantDictationInstance = new IAssistantVoiceDictation(texteReconnu =>
            {
                this.Invoke((MethodInvoker)(() =>
                {
                    iAssistantUndoStack.Push(iAssistantEditor.Text);
                    iAssistantRedoStack.Clear();

                    int start = iAssistantEditor.SelectionStart;
                    iAssistantEditor.Text = iAssistantEditor.Text.Insert(start, texteReconnu);
                    iAssistantEditor.SelectionStart = start + texteReconnu.Length;
                }));
            });

            iAssistantDictationInstance.Start();
            iAssistantIsDictating = true;
            if (sourceButton != null)
            {
                var ico = new Icon(Path.Combine(WinForms.Application.StartupPath, iAssistantDicteeButtonOnIcon),
                new Size(iAssistantFctButtonIconSize, iAssistantFctButtonIconSize));
                Bitmap iconBmp = ico.ToBitmap();
                sourceButton.Image = iconBmp;
                hoverTip.SetToolTip(sourceButton, iAssistantDicteeStopButtonTip);
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
        public static void ErrorShow(string msgKey, string errorLevel1 = "", string errorLevel2 = "", string errorLevel3 = "", string errorLevel4 = "")
        {
            string msgLabel;

            if (!iAssistantErrorMsgs.TryGetValue(msgKey, out msgLabel))
                msgLabel = string.Format(maskErrorMsgUnknown, msgKey);

            string FormatLevel(string level, string label)
            {
                if (string.IsNullOrWhiteSpace(level)) return "";
                string content = level.Length <= iAssistantErrorStringLenghtMax ? level : level.Substring(0, iAssistantErrorStringLenghtMax) + iAssistantStringMsgTrunc;
                return "\n\n" + string.Format(iAssistantErrorLevelLabel, label) + content;
            }

            string fullMessage = msgLabel
                               + FormatLevel(errorLevel1, "1")
                               + FormatLevel(errorLevel2, "2")
                               + FormatLevel(errorLevel3, "3")
                               + FormatLevel(errorLevel4, "4")
                               + "\n\n[Modèle] " + BuildServiceAndModelLabel();

            MessageBox.Show(
                fullMessage,
                iAssistantErrorShowTitle,
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
                var globalService = iAssistantAIServiceActif;
                var globalModel = iAssistantAIModeleActif;
                dlg.Text = $"{iAssistantActionCfgTitle}{action.Name}";
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
                AddLabel(iAssistantActionCfgName);
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
                AddLabel(iAssistantActionCfgSvcModel);
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
                    Service = iAssistantAIServiceActif,   
                    Model = iAssistantAIModeleActif,      
                    Text = iAssistantActionCfgModelDefault
                });

                // 2) Tous les autres couples (Model (Service))
                foreach (var s in iAssistantAIServices.Where(sv => sv.Models != null))
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
                AddLabel(iAssistantActionCfgPrompt);
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
                AddLabel(iAssistantActionCfgTemperature);
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
                    Text = iAssistantOkButtonText,
                    DialogResult = DialogResult.OK,
                    Left = dlg.ClientSize.Width - 200,
                    Width = 80,
                    Top = y
                };
                Button btnCancel = new Button
                {
                    Text = iAssistantCancelButtonText,
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
                iAssistantEditor.Focus();   // place le curseur dans la zone de texte

            }
        }
        /// Affiche (ou ramène) la palette d’actions IA.
        /// • Replace le focus dans l’éditeur dès qu’elle s’affiche.
        /// • Se ferme automatiquement si la sélection de l’éditeur change.
        /// </summary>
        private void OuvrirPaletteActions(bool contextMenuItem = false)
        {
            // Si appel du Menu de Context
            if (contextMenuItem)
            {
                // Lorsque aucun text (Context menu only)
                if (iAssistantEditor.Text == null || iAssistantEditor.Text == "")
                {
                    ErrorShow("ERROR_EDITOR_NOTEXT");
                    return;
                }
            }
            // sinon Verifie s'il existe une sélection (appel de la Souris ou bouton)
            else if (iAssistantEditor.SelectionLength == 0)
                return;

            // Si Palette existante → on la met devant et on sort
            if (iAssistantPaletteActions != null && !iAssistantPaletteActions.IsDisposed)
            {
                iAssistantPaletteActions.BringToFront();
                iAssistantEditor.Focus();
                return;
            }

            // ─── Mémorise la sélection courante ──────────────────────────
            int selStart0 = iAssistantEditor.SelectionStart;
            int selLength0 = iAssistantEditor.SelectionLength;

            // ─── Création de la palette ──────────────────────────────────
            iAssistantPaletteActions = new Form
            {
                FormBorderStyle = FormBorderStyle.None, // plus de bordure ni de titre
                Text = iAssistantPaletteActionsTitle,
                StartPosition = FormStartPosition.Manual,
                MaximizeBox = false,                         
                MinimizeBox = false,
                ShowInTaskbar = false,
                // TopMost = true, // Au dessus de toutes les fenetres du bureau
                Font = this.Font,
                BackColor = this.BackColor,
                Opacity = 0.8,
                Owner = this
            };

            /* ▼▼ NOUVEAU bloc de positionnement ▼▼ */
            Point p = Cursor.Position;     // coordonnées écran de la souris
            Rectangle work = Screen.FromPoint(p).WorkingArea;

            int posX = p.X + iAssistantActionPanelXOffset;
            int posY = p.Y + iAssistantActionPanelYOffset;

            // Si la palette dépasserait à droite, on la décale à gauche
            if (posX + iAssistantPaletteActions.Width > work.Right)
                posX = work.Right - iAssistantPaletteActions.Width;

            // Si elle dépasserait en bas, on la met au-dessus du pointeur
            if (posY + iAssistantPaletteActions.Height > work.Bottom)
                posY = p.Y - 3 * iAssistantActionPanelYOffset; // - iAssistantPaletteActions.Height;

            iAssistantPaletteActions.Location = new Point(posX, posY);
            iAssistantPaletteActionOffset = new Point(iAssistantPaletteActions.Left - this.Left, iAssistantPaletteActions.Top - this.Top);

            this.Move += (s, e) =>
            {
                if (iAssistantPaletteActions != null && !iAssistantPaletteActions.IsDisposed)
                    iAssistantPaletteActions.Location = new Point(this.Left + iAssistantPaletteActionOffset.X,this.Top + iAssistantPaletteActionOffset.Y);
            };

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
            iAssistantPaletteActions.Controls.Add(panel);

            // ─── Panneau et boutons ──────────────────────────────────────
            iAssistantPaletteActions.Controls.Add(panel);
            int x = iAssistantButtonXOffset;
            foreach (var action in iAssistantAIActions)
            {
                // Lit le fichier Icone du bouton (null si probleme => Bouton sera en texte)
                Bitmap iconBmp = null;
                try
                {
                    // ① chemin absolu ?  (facultatif, selon où se trouvent tes icônes)
                    string path = Path.IsPathRooted(action.Icon)
                                  ? action.Icon
                                  : Path.Combine(WinForms.Application.StartupPath, action.Icon);

                    // ② using : l’objet Icon est IDisposable → libère la ressource GDI+
                    using (var ico = new Icon(path, new Size(iAssistantIAButtonIconSize, iAssistantIAButtonIconSize)))
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

                    Size = new Size(iAssistantButtonWidth, iAssistantButtonHeight),
                    Tag = action,
                    Font = new Font(this.Font.FontFamily, iAssistantButtonTextFontSize),
                    Location = new Point(x, buttonYOffset),
                    BackColor = iAssistantButtonBackColor,
                    ForeColor = iAssistantButtonForeColor
                };

                // Affichage par timer du Libellé du bouton au survol sans focus
                btn.MouseEnter += (s, e) =>
                {
                    var b = (Button)s;
                    // point écran juste sous le bouton (centre X)
                    Point screen = b.PointToScreen(new Point(b.Width / 2, b.Height));
                    // on convertit pour le ToolTip : coordonnées dans LA fenêtre propriétaire
                    Point local = iAssistantPaletteActions.PointToClient(screen);
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
                            hoverTip.Hide(iAssistantPaletteActions);
                        }
                        catch (SystemException)
                        {
                            // Le hoverTip a été disposé entre-temps, on ignore silencieusement
                        }
                    }
                };

                // Action du bouton : Apple de l'IA
                btn.Click += async (s, _) => await IAssistantAIMethod((AIAction)((Button)s).Tag);

                // menu contextuel « Configuration »
                ContextMenu ctx = new ContextMenu();
                ctx.MenuItems.Add(new MenuItem(iAssistantActionPanelButtonCfgMenuLabel,
                    (_, __) => AfficherPanneauConfig(action)));
                btn.ContextMenu = ctx;

                panel.Controls.Add(btn);
                x += iAssistantButtonWidth + iAssistantButtonXSpace;
            }
            panel.Size = new Size(x, iAssistantButtonHeight + 2 * buttonYOffset);
            iAssistantPaletteActions.ClientSize = panel.Size;

            // ─── Gestion du focus après affichage ────────────────────────
            iAssistantPaletteActions.Shown += (_, __) =>
            {
                // Rend la main à la fenêtre principale puis à l’éditeur
                this.Activate();
                iAssistantEditor.Focus();
            };

            // ─── Fermeture auto si la sélection change ───────────────────
            KeyEventHandler keyHandler = null;
            MouseEventHandler mouseHandler = null;

            void checkSelectionChange()
            {
                // On ferme seulement si la sélection disparaît complètement
                if (iAssistantPaletteActions != null && !iAssistantPaletteActions.IsDisposed && iAssistantEditor.SelectionLength == 0)
                    iAssistantPaletteActions.Close();
            }

            keyHandler = (_, __) => checkSelectionChange();
            mouseHandler = (_, __) => checkSelectionChange();

            iAssistantEditor.KeyUp += keyHandler;
            iAssistantEditor.MouseUp += mouseHandler;

            // Nettoyage : détache les écouteurs quand la palette se ferme
            iAssistantPaletteActions.FormClosed += (_, __) =>
            {
                iAssistantPaletteActions = null;
                iAssistantEditor.KeyUp -= keyHandler;
                iAssistantEditor.MouseUp -= mouseHandler;
            };

            // ─── Petit utilitaire pour redonner immédiatement le focus ───
            void GiveBackFocus()
                => BeginInvoke((MethodInvoker)(() =>
                {
                    this.Activate();          // remet la fenêtre principale devant
                    iAssistantEditor.Focus();   // et place le curseur dans le texte
                }));

            // ▸ Quand on **déplace** la palette
            iAssistantPaletteActions.Move += (_, __) => GiveBackFocus();

            // ▸ Quand on **clique** n’importe où dans la palette (hors boutons)
            iAssistantPaletteActions.Click += (_, __) => GiveBackFocus();

            // ▸ Quand on clique sur le panneau translucide
            panel.Click += (_, __) => GiveBackFocus();

            iAssistantPaletteActions.Show();   // non modale
            iAssistantEditor.Focus();
        }

        /// <summary>
        /// Remplacement Regex avec timeout maximal
        /// </summary>

        private static string RegexSafeReplace(string input, string pattern, string replacement, int timeoutMs = iAssistantRegexTimeoutMsec)
        {
            try
            {
                var regex = new Regex(pattern, RegexOptions.None, TimeSpan.FromMilliseconds(timeoutMs));
                return regex.Replace(input, replacement);
            }
            catch (RegexMatchTimeoutException ex)
            {
                ErrorShow("ERROR_EDITOR_REGEXTIMEOUT", ex.Message, pattern, input, replacement);
                return input;
            }
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

        [DllImport(iAssistantUser32dll, CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(
            IntPtr hWnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam   //lParam (LOWORD) : largeur en pixels de la marge gauche, (HIWORD) la marge droite. Ici on met textEditorLeftMargin dans le low-word et on laisse la droite à 0.
        );

    }

    // -----------------------------------------------------------------------------
    // Extensions "Stack-like" pour LinkedList<T>
    // Conserve Push / Pop tout en limitant la taille 
    // -----------------------------------------------------------------------------
    internal static class LinkedListStackExtensions
    {
        /// Ajoute un élément en fin de liste (top de pile)
        public static void Push(this LinkedList<string> list, string value)
        {
            list.AddLast(value ?? string.Empty);

            // tronque l'historique au-delà de iAssistantUndoStackMaxNb
            if (list.Count > IAssistant.iAssistantUndoStackMaxItems)
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
