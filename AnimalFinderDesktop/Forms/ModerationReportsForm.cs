using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AnimalFinderDesktop.Services;
using Newtonsoft.Json;

namespace AnimalFinderDesktop.Forms
{
    public partial class ModerationReportsForm : Form
    {
        private DataGridView dgvReports;
        private Button btnResolve, btnBan, btnDeleteListing;
        private List<dynamic> _reports;

        public ModerationReportsForm()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(900, 500);
            this.Text = "Модерация жалоб";
            LoadReports();
        }

        private void InitializeComponent()
        {
            dgvReports = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                BackgroundColor = Color.White
            };
            dgvReports.Columns.Add("id", "ID");
            dgvReports.Columns.Add("listing_id", "ID объявления");
            dgvReports.Columns.Add("reason", "Причина");
            dgvReports.Columns.Add("comment", "Комментарий");
            dgvReports.Columns.Add("created_at", "Дата");
            dgvReports.Columns["id"].Visible = false;
            dgvReports.Columns["listing_id"].Visible = false;

            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(10), BackColor = Color.White };
            btnResolve = new Button { Text = "Отклонить жалобу", Size = new Size(150, 35), BackColor = Color.FromArgb(108, 117, 125), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Location = new Point(10, 8) };
            btnResolve.Click += BtnResolve_Click;
            btnBan = new Button { Text = "Забанить пользователя", Size = new Size(180, 35), BackColor = Color.FromArgb(220, 53, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Location = new Point(170, 8) };
            btnBan.Click += BtnBan_Click;
            btnDeleteListing = new Button { Text = "Удалить объявление", Size = new Size(160, 35), BackColor = Color.FromArgb(255, 193, 7), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat, Location = new Point(360, 8) };
            btnDeleteListing.Click += BtnDeleteListing_Click;
            bottomPanel.Controls.Add(btnResolve);
            bottomPanel.Controls.Add(btnBan);
            bottomPanel.Controls.Add(btnDeleteListing);

            this.Controls.Add(dgvReports);
            this.Controls.Add(bottomPanel);
        }

        private async Task LoadReports()
        {
            try
            {
                using var client = new HttpClient();
                var url = "https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/reports?status=eq.pending&order=created_at.desc";
                client.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                var response = await client.GetStringAsync(url);
                _reports = JsonConvert.DeserializeObject<List<dynamic>>(response) ?? new List<dynamic>();
                dgvReports.Rows.Clear();
                foreach (var r in _reports)
                {
                    dgvReports.Rows.Add(
                        (string)r.id,
                        (string)r.listing_id,
                        (string)r.reason,
                        (string)r.comment,
                        ((DateTime)r.created_at).ToString("dd.MM.yyyy HH:mm")
                    );
                }
                if (_reports.Count == 0)
                    MessageBox.Show("Нет новых жалоб.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
            }
        }

        private async Task UpdateReportStatus(string reportId, string status)
        {
            using var client = new HttpClient();
            var update = new { status = status };
            var json = JsonConvert.SerializeObject(update);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/reports?id=eq.{reportId}";
            client.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
            await client.PatchAsync(url, content);
        }

        private async Task BanUser(string userId)
        {
            using var client = new HttpClient();
            var update = new { role = "banned" };
            var json = JsonConvert.SerializeObject(update);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/profiles?user_id=eq.{userId}";
            client.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
            await client.PatchAsync(url, content);
        }

        private async Task DeleteListing(string listingId)
        {
            using var client = new HttpClient();
            var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings?id=eq.{listingId}";
            client.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
            await client.DeleteAsync(url);
        }

        private async void BtnResolve_Click(object sender, EventArgs e)
        {
            if (dgvReports.SelectedRows.Count == 0) return;
            var reportId = dgvReports.SelectedRows[0].Cells["id"].Value.ToString();
            await UpdateReportStatus(reportId, "resolved");
            MessageBox.Show("Жалоба отклонена.");
            await LoadReports();
        }

        private async void BtnBan_Click(object sender, EventArgs e)
        {
            if (dgvReports.SelectedRows.Count == 0) return;
            var listingId = dgvReports.SelectedRows[0].Cells["listing_id"].Value.ToString();
            // Получить user_id объявления
            using var client = new HttpClient();
            var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings?id=eq.{listingId}&select=user_id";
            client.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
            var response = await client.GetStringAsync(url);
            var listings = JsonConvert.DeserializeObject<List<dynamic>>(response);
            if (listings != null && listings.Count > 0)
            {
                string userId = listings[0].user_id;
                await BanUser(userId);
                MessageBox.Show("Пользователь забанен.");
            }
            await UpdateReportStatus(dgvReports.SelectedRows[0].Cells["id"].Value.ToString(), "resolved");
            await LoadReports();
        }

        private async void BtnDeleteListing_Click(object sender, EventArgs e)
        {
            if (dgvReports.SelectedRows.Count == 0) return;
            var listingId = dgvReports.SelectedRows[0].Cells["listing_id"].Value.ToString();
            await DeleteListing(listingId);
            MessageBox.Show("Объявление удалено.");
            await UpdateReportStatus(dgvReports.SelectedRows[0].Cells["id"].Value.ToString(), "resolved");
            await LoadReports();
        }
    }
}