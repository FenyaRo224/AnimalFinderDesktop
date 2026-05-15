using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using AnimalFinderDesktop.Services;

namespace AnimalFinderDesktop.Forms
{
    public class MyListingsForm : Form
    {
        private DataGridView dgvListings;
        private Button btnDelete, btnRefresh, btnClose;
        private Label lblStatus;
        private List<Dictionary<string, object>> _myListings = new();
        private string _userId;

        public MyListingsForm(string userId)
        {
            _userId = userId;
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(900, 500);
            this.Text = "Мои объявления";
            LoadListings();
        }

        private void InitializeComponent()
        {
            var topPanel = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(10), BackColor = Color.White };

            btnRefresh = new Button { Text = "Обновить", Size = new Size(100, 30), Location = new Point(10, 5) };
            btnRefresh.Click += async (s, e) => await LoadListings();

            lblStatus = new Label { Text = "Загрузка...", AutoSize = true, Location = new Point(120, 12), ForeColor = Color.Gray };

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
            dgvListings.Columns.Add("status", "Статус");
            dgvListings.Columns.Add("created_at", "Дата создания");
            dgvListings.Columns["id"].Visible = false;

            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(10), BackColor = Color.White };

            btnDelete = new Button { Text = "Удалить выбранное", Size = new Size(150, 35), BackColor = Color.FromArgb(220, 53, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Location = new Point(10, 8) };
            btnDelete.Click += async (s, e) => await DeleteSelected();

            btnClose = new Button { Text = "Закрыть", Size = new Size(100, 35), BackColor = Color.FromArgb(108, 117, 125), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Location = new Point(170, 8) };
            btnClose.Click += (s, e) => this.Close();

            bottomPanel.Controls.Add(btnDelete);
            bottomPanel.Controls.Add(btnClose);

            this.Controls.Add(dgvListings);
            this.Controls.Add(topPanel);
            this.Controls.Add(bottomPanel);
        }

        private async Task LoadListings()
        {
            try
            {
                lblStatus.Text = "Загрузка...";
                using var client = new HttpClient();
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings?user_id=eq.{_userId}&select=*";
                client.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                var response = await client.GetStringAsync(url);
                _myListings = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response) ?? new();

                dgvListings.Rows.Clear();
                foreach (var item in _myListings)
                {
                    string status = GetString(item, "status");
                    string statusText = status == "active" ? "Активен" : (status == "pending" ? "На модерации" : (status == "closed" ? "Закрыт" : "Просрочен"));
                    dgvListings.Rows.Add(
                        GetString(item, "id"),
                        GetString(item, "pet_name"),
                        GetString(item, "species"),
                        GetString(item, "listing_type") == "lost" ? "Пропал" : "Найден",
                        statusText,
                        GetDateString(item, "created_at")
                    );
                }
                lblStatus.Text = $"Всего: {_myListings.Count}";
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Ошибка: {ex.Message}";
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
            }
        }

        private async Task DeleteSelected()
        {
            if (dgvListings.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите объявление для удаления", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedRow = dgvListings.SelectedRows[0];
            var id = selectedRow.Cells["id"].Value.ToString();
            var petName = selectedRow.Cells["pet_name"].Value?.ToString() ?? "Без имени";

            var result = MessageBox.Show($"Удалить объявление о {petName}? Это действие нельзя отменить.", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;

            try
            {
                using var client = new HttpClient();
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings?id=eq.{id}";
                client.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                var response = await client.DeleteAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Объявление удалено", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await LoadListings();
                }
                else
                {
                    MessageBox.Show("Ошибка при удалении", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
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
                return date.ToString("dd.MM.yyyy");
            return "";
        }
    }
}