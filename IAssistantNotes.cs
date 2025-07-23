/******* ---------------------------------------------------------------------
 * *****    IAssistant      Intelligence Artificial-powered Office Assistant
 * ***** ---------------------------------------------------------------------
 * *****   
 * *****    IAssistantNotes.cs   Notes management trough LiteDB - Experimental 
 * *****                         To be finalized
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

using LiteDB;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace IAssistant
{
    // ────────────────────────────────────────────────────────────
    // 1) Modèle de note LiteDB
    // ────────────────────────────────────────────────────────────
    public class IAssistantNote
    {
        public ObjectId Id { get; set; }                            // ID technique (auto)
        public string Note { get; set; }                            // Contenu de la Note
        public string[] Tags { get; set; }                          // Liste des Tags
        public DateTime ModifiedOn { get; set; } = DateTime.UtcNow; // Date de Modification
    }

    // ────────────────────────────────────────────────────────────
    // 2) Accès LiteDB
    // ────────────────────────────────────────────────────────────
    public class IAssistantNoteRepository : IDisposable
    {
        private readonly LiteDatabase _db;
        private readonly ILiteCollection<IAssistantNote> _col;

        public IAssistantNoteRepository(string dbFilePath, string pwdEnvVar, string collection)
        {
            string pwd = !string.IsNullOrWhiteSpace(pwdEnvVar)
                         ? Environment.GetEnvironmentVariable(pwdEnvVar)
                         : null;

            string conn = $"Filename={dbFilePath};" +
                          (string.IsNullOrWhiteSpace(pwd) ? "" : $"Password={pwd};") +
                          "Mode=Exclusive";

            try
            {
                _db = new LiteDatabase(conn);
                _col = _db.GetCollection<IAssistantNote>(collection);

                _col.EnsureIndex(x => x.Tags, false);
                _col.EnsureIndex(x => x.ModifiedOn);
            }
            catch (LiteException lex) when (lex.Message != null &&
                    ( (lex.Message.IndexOf("password", StringComparison.OrdinalIgnoreCase) >= 0)
                  || (lex.Message.IndexOf("chiffré", StringComparison.OrdinalIgnoreCase) >= 0) ))
            {
                // Mot de passe incorrect
                IAssistant.ErrorShow("ERROR_EDITOR_NOTEDBKEY", dbFilePath, pwdEnvVar, collection);
                throw;
            }
            catch (Exception ex)
            {
                IAssistant.ErrorShow("ERROR_EDITOR_NOTEDBOPEN", ex.Message, dbFilePath, pwdEnvVar);
                throw;
            }
        }

        public ObjectId Insert(string note, params string[] tags)
        {
            var n = new IAssistantNote { Note = note, Tags = tags, ModifiedOn = DateTime.UtcNow };
            _col.Insert(n);
            return n.Id;
        }

        public bool Update(IAssistantNote n) { n.ModifiedOn = DateTime.UtcNow; return _col.Update(n); }
        public bool Delete(ObjectId id) => _col.Delete(id);
        public ILiteQueryable<IAssistantNote> Query() => _col.Query();
        public void Dispose() => _db?.Dispose();
    }

    // ────────────────────────────────────────────────────────────
    // 3) Fenêtre principale de gestion des notes
    // ────────────────────────────────────────────────────────────
    public class IAssistantNoteForm : Form
    {
        // — Champs / contrôles ───────────────────────────────
        private readonly IAssistantNoteRepository _repo;
        private IAssistantNote _editing = null;

        private readonly TextBox _txtNote = new TextBox();
        private readonly FlowLayoutPanel _flowTags = new FlowLayoutPanel();
        private readonly TextBox _txtTags = new TextBox();
        private readonly Button _btnPublish = new Button();
        private readonly Button _btnNew = new Button();
        private readonly Button _btnSearch = new Button();
        private readonly Button _btnInsertEditor = new Button();
        private readonly ListView _lv = new ListView();
        private readonly ContextMenuStrip _ctx = new ContextMenuStrip();

        private static readonly Color ColBackForm = IAssistant.iAssistantEditeurBackColor;
        private static readonly Color ColButtonBack = IAssistant.iAssistantButtonBackColor;
        private static readonly Color ColButtonFore = IAssistant.iAssistantButtonForeColor;

        // — Constructeur ─────────────────────────────────────
        public IAssistantNoteForm(string dbPath, string pwdVar, string collection)
        {
            Text = "IAssistant Notes";
            Width = 700;
            Height = 600    ;
            Font = new Font(IAssistant.iAssistantEditeurTextFontFamily, IAssistant.iAssistantDefaultTextFontSize);
            BackColor = ColBackForm;
            Icon = IAssistant.iAssistantEditor.FindForm()?.Icon;      // même icône que la form principale

            // ─── Réservoir de données ───────────────────────
            try { _repo = new IAssistantNoteRepository(dbPath, pwdVar, collection); }
            catch { Close(); return; }

            // ─── Pré‑config des contrôles ───────────────────
            _txtNote.Multiline = true;
            _txtNote.Dock = DockStyle.Fill;
            _txtNote.Height = 150;
            _txtNote.BorderStyle = BorderStyle.FixedSingle;
            _txtNote.SetCueBanner("Add contents…");

            _txtTags.Dock = DockStyle.Top;
            _txtTags.Height = 28;
            _txtTags.BorderStyle = BorderStyle.FixedSingle;
            _txtTags.SetCueBanner("Comma‑separated Tags");

            _flowTags.Dock = DockStyle.Top;
            _flowTags.AutoSize = true;
            _flowTags.WrapContents = true;
            _flowTags.Margin = new Padding(0, 6, 0, 6);

            ConfigureButton(_btnNew, "📝 New", new Padding(12, 4, 0, 0));
            ConfigureButton(_btnPublish, "🖫 Save  ", new Padding(12, 0, 0, 4));
            ConfigureButton(_btnNew, "📝 New", new Padding(12, 4, 0, 0));
            ConfigureButton(_btnSearch, "🔍 Search", new Padding(8, 0, 0, 0));
            ConfigureButton(_btnInsertEditor, "⇩ Insert into Editor…", new Padding(0, 12, 0, 0));
            _btnPublish.Enabled = false;   // au départ

            // ListView
            _lv.Dock = DockStyle.Fill;
            _lv.View = View.Details;
            _lv.FullRowSelect = true;
            _lv.HideSelection = false;
            _lv.BorderStyle = BorderStyle.FixedSingle;
            _lv.Columns.Add("Contents", -2);  // ajusté dynamiquement
            _lv.Columns.Add("Tags", 230);
            _lv.OwnerDraw = true;
            _lv.DrawColumnHeader += ListView_DrawColumnHeader; // couleur + centrage
            _lv.DrawItem += (_, __) => { };                   // indispensable en mode OwnerDraw_lv.DrawItem += ListView_DrawItem;
            _lv.DrawSubItem += ListView_DrawSubItem;


            // Contextuel
            _ctx.Items.Add("Modify", null, (_, __) => BeginEditSelected());
            _ctx.Items.Add("Delete", null, (_, __) => DeleteSelected());
            _ctx.Items.Add("Duplicate", null, (_, __) => DuplicateSelected());
            _lv.ContextMenuStrip = _ctx;

            // ─── Mise en page (TableLayoutPanels) ───────────
            var main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(24, 0, 24, 24)
            };
            main.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // pile haute
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));     // liste
            main.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // bouton Insert
            Controls.Add(main);

            // Pile d’édition
            var editStack = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1
            };
            main.Controls.Add(editStack, 0, 0);

            // ── Contenu ────────────────────────────────────
            editStack.Controls.Add(new Label { Text = "Contents", AutoSize = true, Margin = new Padding(0, 10, 0, 2) });
            var noteRow = TwoColumnRow();
            var btnStack = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, RowCount = 2, Dock = DockStyle.Top };
            btnStack.Controls.Add(_btnPublish, 0, 0);
            btnStack.Controls.Add(_btnNew, 0, 1);
            noteRow.Controls.Add(_txtNote, 0, 0);
            noteRow.Controls.Add(btnStack, 1, 0);
            editStack.Controls.Add(noteRow);

            // ── Tags ───────────────────────────────────────
            editStack.Controls.Add(_flowTags);
            editStack.Controls.Add(new Label { Text = "Tags (Enter to validate)", AutoSize = true, Margin = new Padding(0, 10, 0, 2) });

            var tagRow = TwoColumnRow();
            tagRow.Controls.Add(_txtTags, 0, 0);
            tagRow.Controls.Add(_btnSearch, 1, 0);
            editStack.Controls.Add(tagRow);

            // ── Liste ──────────────────────────────────────
            var listPanel = new Panel { Dock = DockStyle.Fill };
            listPanel.Controls.Add(_lv);
            main.Controls.Add(listPanel, 0, 1);

            // ── Insert Editor Button ───────────────────────
            _btnInsertEditor.Dock = DockStyle.Right;
            main.Controls.Add(_btnInsertEditor, 0, 2);

            // ─── Événements principaux ─────────────────────
            _txtNote.TextChanged += (_, __) => UpdateSaveButtonEnabled();
            _btnPublish.Click += (_, __) => SaveCurrent();
            _btnNew.Click += (_, __) => ClearEdit();
            _btnSearch.Click += (_, __) => SearchByTag();
            _txtTags.KeyDown += TxtTags_KeyDown;
            _lv.DoubleClick += (_, __) => BeginEditSelected();
            _lv.Resize += (_, __) => AdjustListColumns();
            _btnInsertEditor.Click += (_, __) => InsertIntoEditor();

            // ─── Initialisation liste ──────────────────────
            LoadList();
            RefreshFlowTags();
            AdjustListColumns();
        }

        // — Helpers visuels ——————————————————————————
        private static void ConfigureButton(Button btn, string text, Padding margin)
        {
            btn.Text = text;
            btn.AutoSize = true;
            btn.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btn.Margin = margin;
            btn.BackColor = ColButtonBack;
            btn.ForeColor = ColButtonFore;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
        }
        private static TableLayoutPanel TwoColumnRow()
        {
            var tl = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true };
            tl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            tl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            return tl;
        }

        // — Ajustement colonne Contents ——————————————————
        private void AdjustListColumns()
        {
            if (_lv.Columns.Count < 2) return;

            const int tagsWidth = 230;          // largeur fixe « Tags »
            int scWidth = 0;                    // largeur scrollbar (si visible)

            // — Détermination de la barre verticale ————————————
            if (_lv.Items.Count > 0)
            {
                // Hauteur d’une ligne (premier item suffit)
                int itemHeight = _lv.Items[0].Bounds.Height;

                // Lignes visibles dans la zone cliente
                int visibleRows = _lv.ClientSize.Height / itemHeight;

                // Barre présente si + de lignes que de place
                if (_lv.Items.Count > visibleRows)
                    scWidth = SystemInformation.VerticalScrollBarWidth;
            }

            // — Calcul largeur « Contents » ————————————————
            int contentsWidth = _lv.ClientSize.Width
                                - tagsWidth
                                - scWidth
                                - 2;   // petite marge

            _lv.Columns[0].Width = Math.Max(50, contentsWidth); // « Contents »
            _lv.Columns[1].Width = tagsWidth;                   // « Tags » (ancré à droite)
        }


        private void UpdateSaveButtonEnabled() =>
            _btnPublish.Enabled = !string.IsNullOrWhiteSpace(_txtNote.Text);

        // — Gestion rapide des tags via ENTER ———————————
        private void TxtTags_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;

            e.SuppressKeyPress = true;
            string tag = _txtTags.Text.Trim();
            if (!string.IsNullOrEmpty(tag))
            {
                AddTagBadge(tag);
                _txtTags.Clear();
            }
        }

        private void AddTagBadge(string tag)
        {
            bool exists = _flowTags.Controls.OfType<Label>().Any(lbl => (string)lbl.Tag == tag);
            if (exists) return;

            var badge = new Label
            {
                Text = tag + "  ✖",
                Tag = tag,
                AutoSize = true,
                Padding = new Padding(6, 2, 6, 2),
                Margin = new Padding(2),
                BackColor = ColButtonBack,
                ForeColor = ColButtonFore,
                Cursor = Cursors.Hand
            };
            badge.Click += (s, __) => _flowTags.Controls.Remove(badge);
            _flowTags.Controls.Add(badge);
        }

        private void RefreshFlowTags()
        {
            _flowTags.Controls.Clear();
            if (_editing?.Tags == null) return;
            foreach (string t in _editing.Tags) AddTagBadge(t);
        }

        // — Chargement / rafraîchissement ListView —————————
        private void LoadList(IEnumerable<IAssistantNote> src = null)
        {
            int carMax = 500; // Longueur max de la preview   
            var data = src ??
                       _repo.Query()
                            .OrderByDescending(x => x.ModifiedOn)
                            .ToEnumerable();

            _lv.BeginUpdate();
            _lv.Items.Clear();
            foreach (IAssistantNote n in data)
            {
                // 1) Aplatit tous les sauts de ligne et tabulations
                string flat = System.Text.RegularExpressions
                              .Regex.Replace(n.Note, @"\s+", " ").Trim();

                // 2) Coupe plus loin (ici 200 car.) pour voir un maximum
                string preview = flat.Length <= carMax ? flat
                                                    : flat.Substring(0, carMax - 3) + "…";

                var it = new ListViewItem(preview);
                it.SubItems.Add(string.Join(", ", n.Tags ?? Array.Empty<string>()));
                it.Tag = n;
                _lv.Items.Add(it);
            }
            _lv.EndUpdate();
            AdjustListColumns();
        }

        // — Sauvegarde / édition —————————————————────────—
        private void SaveCurrent()
        {
            var tags = _flowTags.Controls
                                .OfType<Label>()
                                .Select(lbl => (string)lbl.Tag)
                                .ToArray();

            if (_editing == null)
            {
                _repo.Insert(_txtNote.Text, tags);
            }
            else
            {
                _editing.Note = _txtNote.Text;
                _editing.Tags = tags;
                _repo.Update(_editing);
            }
            ClearEdit();
            LoadList();
        }

        private void ClearEdit()
        {
            _editing = null;
            _txtNote.Clear();
            _txtTags.Clear();
            _flowTags.Controls.Clear();
            UpdateSaveButtonEnabled();
        }

        private void BeginEditSelected()
        {
            if (_lv.SelectedItems.Count == 0) return;
            _editing = (IAssistantNote)_lv.SelectedItems[0].Tag;
            _txtNote.Text = _editing.Note;
            _txtTags.Clear();
            RefreshFlowTags();
        }

        private void DeleteSelected()
        {
            if (_lv.SelectedItems.Count == 0) return;

            var note = (IAssistantNote)_lv.SelectedItems[0].Tag;
            var res = MessageBox.Show("Delete this note?",
                                       "Confirm",
                                       MessageBoxButtons.YesNo,
                                       MessageBoxIcon.Warning);
            if (res == DialogResult.Yes)
            {
                _repo.Delete(note.Id);
                LoadList();
            }
        }

        private void DuplicateSelected()
        {
            if (_lv.SelectedItems.Count == 0) return;
            var note = (IAssistantNote)_lv.SelectedItems[0].Tag;
            _repo.Insert(note.Note, note.Tags);
            LoadList();
        }

        // ——— Recherche par Tag (sans exception LiteDB) ——­
        private void SearchByTag()
        {
            string filter = _txtTags.Text?.Trim();

            // Champ vide → tout afficher
            if (string.IsNullOrEmpty(filter))
            {
                LoadList();
                return;
            }

            // Requête LiteDB minimale (Tags != null && non vides) ...
            var all = _repo.Query()
                           .Where(x => x.Tags != null && x.Tags.Any())
                           .ToEnumerable();

            // ... puis filtrage in‑memory, insensible à la casse
            var match = all.Where(n => n.Tags.Any(t =>
                              !string.IsNullOrEmpty(t) &&
                              t.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0))
                           .OrderByDescending(n => n.ModifiedOn);

            LoadList(match);
        }

        // ——— Insertion dans l’éditeur principal —————————
        private void InsertIntoEditor()
        {
            if (_lv.SelectedItems.Count == 0) return;                   // rien de sélectionné

            var noteObj = (IAssistantNote)_lv.SelectedItems[0].Tag;     // note courante
            if (noteObj == null || string.IsNullOrWhiteSpace(noteObj.Note)) return;

            string noteText = noteObj.Note;

            // Trouve la fenêtre principale
            var assistantForm = Application
                .OpenForms
                .OfType<IAssistant>()
                .FirstOrDefault();
            if (assistantForm == null) return;

            // Appelle directement la méthode d’insertion
            assistantForm.IAssistantTextInsert(noteText);
        }

        // — Nettoyage ————————————————————————————————
        protected override void Dispose(bool disposing)
        {
            if (disposing) _repo?.Dispose();
            base.Dispose(disposing);
        }
        private void ListView_DrawColumnHeader(object sender,
                                       DrawListViewColumnHeaderEventArgs e)
        {
            // Fond = ColBackForm (I.e. même couleur que la fenêtre)
            using (var backBrush = new SolidBrush(ColBackForm))
            using (var textBrush = new SolidBrush(ColButtonFore))   // texte = couleur boutons
            using (var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            {
                e.Graphics.FillRectangle(backBrush, e.Bounds);               // fond
                e.Graphics.DrawString(e.Header.Text, _lv.Font,               // texte
                                      textBrush, e.Bounds, sf);
            }
            e.DrawDefault = false;    // on gère tout nous‑mêmes
        }

        private void ListView_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            bool selected = e.Item.Selected;

            /* ▸ Couleur de fond :
               - surbrillance si sélectionné
               - sinon alternance pair / impair                 */
            Color back = selected
                         ? SystemColors.Highlight
                         : (e.ItemIndex % 2 == 0
                               ? _lv.BackColor                   // ligne paire
                               : ColBackForm);                  // ligne impaire (léger contraste)

            Color fore = selected ? SystemColors.HighlightText : _lv.ForeColor;

            using (var backBrush = new SolidBrush(back))
                e.Graphics.FillRectangle(backBrush, e.Bounds);

            // ▸ Alignement GAUCHE + ellipsis en fin de cellule
            TextFormatFlags flags = TextFormatFlags.Left
                                  | TextFormatFlags.VerticalCenter
                                  | TextFormatFlags.EndEllipsis;

            TextRenderer.DrawText(e.Graphics,
                                  e.SubItem.Text,
                                  _lv.Font,
                                  e.Bounds,
                                  fore,
                                  flags);
        }
    }

    // ────────────────────────────────────────────────────────────
    // 4) Helpers WinForms – extensions utilitaires
    // ────────────────────────────────────────────────────────────
    internal static class WinFormsExtensions
    {
        private const int EM_SETCUEBANNER = 0x1501;
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        public static void SetCueBanner(this TextBox tb, string text)
        {
            if (tb?.IsHandleCreated == true)
                SendMessage(tb.Handle, EM_SETCUEBANNER, (IntPtr)1, text);
        }

        public static string Truncate(this string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s;
            return s.Substring(0, max) + "…";
        }
    }
}
