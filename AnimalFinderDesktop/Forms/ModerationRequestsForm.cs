using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using AnimalFinderDesktop.Services;

namespace AnimalFinderDesktop.Forms
{
    public class ModerationRequestsForm : Form
    {
        private DataGridView dgvRequests;
        private Button btnApprove, btnReject, btnRefresh;
        private Label lblStatus;
        private List<Dictionary<string, object>> _requests = new();

        public ModerationRequestsForm()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(800, 500);
            this.Text = "Заявки на модератора";
            LoadRequests();
        }

        private void InitializeComponent()
        {
            var topPanel = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(10), BackColor = Color.White };

            btnRefresh = new Button { Text = "Обновить", Size = new Size(100, 30), Location = new Point(10, 5) };
            btnRefresh.Click += async (s, e) => await LoadRequests();

            lblStatus = new Label { Text = "Загрузка...", AutoSize = true, Location = new Point(120, 12), ForeColor = Color.Gray };

            topPanel.Controls.Add(btnRefresh);
            topPanel.Controls.Add(lblStatus);

            dgvRequests = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                BackgroundColor = Color.White
            };
            dgvRequests.Columns.Add("id", "ID");
            dgvRequests.Columns.Add("user_id", "User ID");
            dgvRequests.Columns.Add("request_type", "Тип");
            dgvRequests.Columns.Add("status", "Статус");
            dgvRequests.Columns.Add("created_at", "Дата");
            dgvRequests.Columns["id"].Visible = false;
            dgvRequests.Columns["user_id"].Visible = false;

            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(10), BackColor = Color.White };

            btnApprove = new Button { Text = "✅ Одобрить", Size = new Size(120, 35), BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Location = new Point(10, 8) };
            btnApprove.Click += async (s, e) => await ProcessRequest(true);

            btnReject = new Button { Text = "❌ Отклонить", Size = new Size(120, 35), BackColor = Color.FromArgb(220, 53, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Location = new Point(140, 8) };
            btnReject.Click += async (s, e) => await ProcessRequest(false);

            bottomPanel.Controls.Add(btnApprove);
            bottomPanel.Controls.Add(btnReject);

            this.Controls.Add(dgvRequests);
            this.Controls.Add(topPanel);
            this.Controls.Add(bottomPanel);
        }

        private async Task LoadRequests()
        {
            try
            {
                lblStatus.Text = "Загрузка...";
                using var client = new HttpClient();
                var url = "https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/moderation_requests?status=eq.pending";
                client.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                var response = await client.GetStringAsync(url);
                _requests = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response) ?? new();

                dgvRequests.Rows.Clear();
                foreach (var item in _requests)
                {
                    dgvRequests.Rows.Add(
                        GetString(item, "id"),
                        GetString(item, "user_id"),
                        GetString(item, "request_type") == "moderator_role" ? "Запрос на модератора" : "Запрос на верификацию",
                        "На рассмотрении",
                        GetDateString(item, "created_at")
                    );
                }
                lblStatus.Text = $"Заявок: {_requests.Count}";
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Ошибка: {ex.Message}";
            }
        }

        private async Task ProcessRequest(bool approve)
        {
            if (dgvRequests.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите заявку", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedRow = dgvRequests.SelectedRows[0];
            var requestId = selectedRow.Cells["id"].Value.ToString();
            var userId = selectedRow.Cells["user_id"].Value.ToString();

            var result = MessageBox.Show(approve ? "Одобрить заявку?" : "Отклонить заявку?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            try
            {
                using var client = new HttpClient();
                // Обновляем статус заявки
                var updateData = new { status = approve ? "approved" : "rejected", reviewed_at = DateTime.UtcNow };
                var json = JsonConvert.SerializeObject(updateData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/moderation_requests?id=eq.{requestId}";
                client.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                await client.PatchAsync(url, content);

                if (approve)
                {
                    // Обновляем роль пользователя
                    var roleData = new { role = "moderator" };
                    var roleJson = JsonConvert.SerializeObject(roleData);
                    var roleContent = new StringContent(roleJson, Encoding.UTF8, "application/json");
                    var roleUrl = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/profiles?user_id=eq.{userId}";
                    await client.PatchAsync(roleUrl, roleContent);
                }

                MessageBox.Show(approve ? "Заявка одобрена" : "Заявка отклонена", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadRequests();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private string GetString(Dictionary<string, object> dict, string key)
        {
            return dict.ContainsKey(key) && dict[key] != null ? dict[key].ToString() : "";
        }

        private string GetDateString(Dictionary<string, object> dict, string key)
        {
            if (dict.ContainsKey(key) && dict[key] != null && DateTime.TryParse(dict[key].ToString(), out var date))
                return date.ToString("dd.MM.yyyy HH:mm");
            return "";
        }
    }
}