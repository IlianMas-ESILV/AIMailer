// Form1.cs
using System;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AiMailer
{
    public partial class Form1 : Form
    {
        private string promptContext = "Tu es un assistant intelligent spécialisé dans la rédaction d’e-mails professionnels, personnels et académiques. Ton rôle est d’aider l’utilisateur à formuler des messages clairs, polis, structurés et adaptés au contexte. Tu t’adaptes au ton souhaité (formel, amical, direct, diplomatique, etc.), à la relation entre l’expéditeur et le destinataire (collègue, supérieur, ami, recruteur…), ainsi qu’à l’objectif du mail (demande, relance, remerciement, proposition…). ";
        private Panel submenuPanel;
        private TextBox textBox1;

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Création de la TextBox principale
            textBox1 = new TextBox
            {
                Multiline = true,
                Name = "textBox1",
                Size = new Size(400, 300),
                Location = new Point(200, 10),
                ScrollBars = ScrollBars.Vertical
            };
            this.Controls.Add(textBox1);

            // Création du bouton principal "Action"
            Button mainButton = new Button
            {
                Text = "Action",
                Size = new Size(160, 30),
                Location = new Point(10, 10)
            };
            mainButton.Click += ToggleSubmenu;
            this.Controls.Add(mainButton);

            // Création du panneau caché pour les sous-boutons
            submenuPanel = new Panel
            {
                Visible = false,
                Location = new Point(10, 50),
                Size = new Size(160, 250),
                BorderStyle = BorderStyle.FixedSingle,
               // BackColor = Color.LightCyan
            };
            this.Controls.Add(submenuPanel);

            // Ajout des boutons IA au sous-menu
            AddSubmenuButton("Traduire", "Traduis ce texte en français .", 10);
            AddSubmenuButton("Résumer", "Fais un résumé clair de ce texte.", 50);
            AddSubmenuButton("Brainstorm", "Fais un brainstorming d'idées sur ce sujet.", 90);
            AddSubmenuButton("Élaborer", "Développe plus en détail cette idée.", 130);
            AddSubmenuButton("Style pro", "Réécris ce texte dans un style professionnel.", 170);
            AddSubmenuButton("Rendez-vous", "Propose une prise de rendez-vous basée sur ce texte.", 210);
        }

        private void ToggleSubmenu(object sender, EventArgs e)
        {
            submenuPanel.Visible = !submenuPanel.Visible;
        }

        private void AddSubmenuButton(string label, string prompt, int y)
        {
            Button btn = new Button
            {
                Text = label,
                Size = new Size(130, 30),
                Location = new Point(10, y),
                Tag = prompt
            };
            btn.Click += async (s, e) => await SendToLLM(prompt);
            submenuPanel.Controls.Add(btn);
        }

        private async Task SendToLLM(string systemPrompt)
        {
            string selectedText = string.IsNullOrWhiteSpace(textBox1.SelectedText) ? textBox1.Text : textBox1.SelectedText;

            if (string.IsNullOrWhiteSpace(selectedText))
            {
                MessageBox.Show("Aucun texte dans la zone de texte.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var requestBody = new
            {
                //model = "nous-hermes-2-mistral-7b-dpo",
                model = "llama-3.2-1b-instruct",
                messages = new[]
                {
                    new { role = "system", content = promptContext + systemPrompt + " Réponds en français." },
                    new { role = "user", content = selectedText }
                },
                temperature = 0.7
            };

            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("http://127.0.0.1:1234");
                    var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync("/v1/chat/completions", content);
                    response.EnsureSuccessStatusCode();

                    var responseJson = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(responseJson))
                    {
                        string reply = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                        if (!string.IsNullOrWhiteSpace(textBox1.SelectedText))
                        {
                            int selStart = textBox1.SelectionStart;
                            int selLength = textBox1.SelectionLength;
                            textBox1.Text = textBox1.Text.Substring(0, selStart) + reply + textBox1.Text.Substring(selStart + selLength);
                            textBox1.SelectionStart = selStart;
                            textBox1.SelectionLength = reply.Length;
                        }
                        else
                        {
                            textBox1.Text = reply;
                        }
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