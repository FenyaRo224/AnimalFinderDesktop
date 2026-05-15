using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using AnimalFinderDesktop.Services;

namespace AnimalFinderDesktop.Forms
{
    public class ModerationForm : Form
    {
        private DataGridView dgvListings;
        private Button btnApprove, btnReject, btnRefresh;
        private Label lblStatus;
        private List<Dictionary<string, object>> _pendingListings = new();

        public ModerationForm()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(1000, 600);
            this.Text = "Модерация объявлений";
            LoadPendingListings();
        }

        private void InitializeComponent()
        {
            var topPanel = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(10), BackColor = Color.White };

            btnRefresh = new Button { Text = "Обновить", Size = new Size(100, 30), Location = new Point(10, 10) };
            btnRefresh.Click += async (s, e) => await LoadPendingListings();

            lblStatus = new Label { Text = "Загрузка...", AutoSize = true, Location = new Point(120, 18), ForeColor = Color.Gray };

            topPanel.Controls.Add(btnRefresh);
            topPanel.Controls.Add(lblStatus);

            dgvListings = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                BackgroundColor = Color.White
            };
            dgvListings.Columns.Add("id", "ID");
            dgvListings.Columns.Add("pet_name", "Кличка");
            dgvListings.Columns.Add("species", "Вид");
            dgvListings.Columns.Add("listing_type", "Тип");
            dgvListings.Columns.Add("created_at", "Дата создания");
            dgvListings.Columns["id"].Visible = false;

            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(10), BackColor = Color.White };

            btnApprove = new Button { Text = "✅ Одобрить", Size = new Size(120, 35), BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Location = new Point(10, 8) };
            btnApprove.Click += async (s, e) => await ModerateListing(true);

            btnReject = new Button { Text = "❌ Отклонить (удалить)", Size = new Size(150, 35), BackColor = Color.FromArgb(220, 53, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Location = new Point(140, 8) };
            btnReject.Click += async (s, e) => await ModerateListing(false);

            bottomPanel.Controls.Add(btnApprove);
            bottomPanel.Controls.Add(btnReject);

            this.Controls.Add(dgvListings);
            this.Controls.Add(topPanel);
            this.Controls.Add(bottomPanel);
        }

        private async Task LoadPendingListings()
        {
            try
            {
                lblStatus.Text = "Загрузка...";
                using var client = new HttpClient();
                var url = "https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings?status=eq.pending&select=*";
                client.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                var response = await client.GetStringAsync(url);
                _pendingListings = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response) ?? new();

                dgvListings.Rows.Clear();
                foreach (var item in _pendingListings)
                {
                    dgvListings.Rows.Add(
                        GetString(item, "id"),
                        GetString(item, "pet_name"),
                        GetString(item, "species"),
                        GetString(item, "listing_type") == "lost" ? "Пропал" : "Найден",
                        GetDateString(item, "created_at")
                    );
                }
                lblStatus.Text = $"На модерации: {_pendingListings.Count}";
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Ошибка: {ex.Message}";
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
            }
        }

        private async Task ModerateListing(bool approve)
        {
            if (dgvListings.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите объявление для модерации", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedRow = dgvListings.SelectedRows[0];
            var id = selectedRow.Cells["id"].Value.ToString();
            var petName = selectedRow.Cells["pet_name"].Value?.ToString() ?? "Без имени";

            var result = MessageBox.Show(
                approve ? $"Одобрить объявление о {petName}?" : $"Удалить объявление о {petName}?",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            try
            {
                using var client = new HttpClient();
                if (approve)
                {
                    var updateData = new { status = "active" };
                    var json = JsonConvert.SerializeObject(updateData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings?id=eq.{id}";
                    client.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                    var response = await client.PatchAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Объявление одобрено и опубликовано", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Ошибка: {error}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    // Удаляем объявление
                    var deleteUrl = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings?id=eq.{id}";
                    client.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                    var response = await client.DeleteAsync(deleteUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Объявление удалено", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Ошибка: {error}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                await LoadPendingListings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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