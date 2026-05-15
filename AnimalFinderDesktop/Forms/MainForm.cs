using AnimalFinderDesktop.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AnimalFinderDesktop.Forms
{
    public partial class MainForm : Form
    {
        private FlowLayoutPanel pnlListings;
        private TextBox txtSearch;
        private ComboBox cbTypeFilter, cbGenderFilter, cbSizeFilter, cbStatusFilter;
        private Label lblStatus;
        private List<Dictionary<string, object>> _currentListings = new();
        private Button btnAddListing, btnRefresh, btnProfile, btnModeration, btnNotifications;
        private string _currentUserRole = "user";
        private System.Windows.Forms.Timer _notificationTimer;
        public MainForm()
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(1300, 800);
            this.Text = "AnimalFinder - Поиск пропавших животных";
            this.BackColor = Color.FromArgb(240, 242, 245);
            SetupUI();
            _ = LoadListingsAsync();
            _ = LoadCurrentUserRole();
            _ = ExpireOldListings();
            StartNotificationTimer();
        }

        private void StartNotificationTimer()
        {
            _notificationTimer = new System.Windows.Forms.Timer { Interval = 30000 };
            _notificationTimer.Tick += async (s, e) => await CheckNotifications();
            _notificationTimer.Start();
        }

        private async Task CheckNotifications()
        {
            try
            {
                var client = await SupabaseService.GetClient();
                var userId = client.Auth.CurrentUser?.Id;
                if (!string.IsNullOrEmpty(userId))
                {
                    var unread = await SupabaseService.GetUnreadNotifications(userId);
                    if (unread.Count > 0 && btnNotifications != null)
                    {
                        btnNotifications.Text = $"🔔 Уведомления ({unread.Count})";
                        btnNotifications.BackColor = Color.Orange;
                    }
                    else if (btnNotifications != null)
                    {
                        btnNotifications.Text = "🔔 Уведомления";
                        btnNotifications.BackColor = Color.FromArgb(0, 122, 204);
                    }
                }
            }
            catch { }
        }

        private async Task ExpireOldListings()
        {
            try
            {
                using var client = new HttpClient();
                var url = "https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings?status=eq.active&select=id,created_at";
                client.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                var response = await client.GetStringAsync(url);
                var listings = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response) ?? new();

                var expiredIds = new List<string>();
                foreach (var item in listings)
                {
                    if (item.ContainsKey("created_at") && item["created_at"] != null)
                    {
                        if (DateTime.TryParse(item["created_at"].ToString(), out var createdDate))
                        {
                            if ((DateTime.UtcNow - createdDate).TotalDays > 30)
                            {
                                expiredIds.Add(item["id"].ToString());
                            }
                        }
                    }
                }

                foreach (var id in expiredIds)
                {
                    var updateData = new { status = "expired" };
                    var json = JsonConvert.SerializeObject(updateData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var updateUrl = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings?id=eq.{id}";
                    client.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                    await client.PatchAsync(updateUrl, content);
                }

                if (expiredIds.Any())
                {
                    await LoadListingsAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ExpireOldListings error: {ex.Message}");
            }
        }

        private async Task LoadCurrentUserRole()
        {
            try
            {
                var client = await SupabaseService.GetClient();
                var userId = client.Auth.CurrentUser?.Id;
                using var httpClient = new HttpClient();
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/profiles?user_id=eq.{userId}&select=role";
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                var response = await httpClient.GetStringAsync(url);
                var profiles = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response);
                if (profiles != null && profiles.Count > 0 && profiles[0].ContainsKey("role"))
                {
                    _currentUserRole = profiles[0]["role"].ToString();
                    if (_currentUserRole == "moderator" || _currentUserRole == "admin")
                    {
                        btnModeration.Visible = true;
                    }
                }
            }
            catch { }
        }

        private void SetupUI()
        {
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.White,
                Padding = new Padding(10)
            };

            txtSearch = new TextBox
            {
                Width = 250,
                Font = new Font("Segoe UI", 11),
                PlaceholderText = "Поиск по кличке или породе..."
            };
            txtSearch.TextChanged += (s, e) => FilterListings();

            cbTypeFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 100,
                Font = new Font("Segoe UI", 10)
            };
            cbTypeFilter.Items.AddRange(new[] { "Все", "Пропал", "Найден" });
            cbTypeFilter.SelectedIndex = 0;
            cbTypeFilter.SelectedIndexChanged += (s, e) => FilterListings();

            cbGenderFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 100,
                Font = new Font("Segoe UI", 10)
            };
            cbGenderFilter.Items.AddRange(new[] { "Любой", "Мальчик", "Девочка" });
            cbGenderFilter.SelectedIndex = 0;
            cbGenderFilter.SelectedIndexChanged += (s, e) => FilterListings();

            cbSizeFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 100,
                Font = new Font("Segoe UI", 10)
            };
            cbSizeFilter.Items.AddRange(new[] { "Любой", "Маленький", "Средний", "Большой" });
            cbSizeFilter.SelectedIndex = 0;
            cbSizeFilter.SelectedIndexChanged += (s, e) => FilterListings();

            cbStatusFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 120,
                Font = new Font("Segoe UI", 10)
            };
            cbStatusFilter.Items.AddRange(new[] { "Все", "Активные", "На модерации", "Закрытые" });
            cbStatusFilter.SelectedIndex = 0;
            cbStatusFilter.SelectedIndexChanged += (s, e) => FilterListings();

            btnRefresh = new Button
            {
                Text = "Обновить",
                Width = 80,
                Height = 35,
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnRefresh.Click += async (s, e) => await LoadListingsAsync();

            btnAddListing = new Button
            {
                Text = "Добавить объявление",
                Width = 160,
                Height = 35,
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnAddListing.Click += (s, e) =>
            {
                using var addForm = new AddListingForm();
                if (addForm.ShowDialog() == DialogResult.OK)
                    _ = LoadListingsAsync();
            };

            btnProfile = new Button
            {
                Text = "👤 Профиль",
                Width = 100,
                Height = 35,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnProfile.Click += (s, e) =>
            {
                using var profileForm = new ProfileForm();
                profileForm.ShowDialog();
                _ = LoadListingsAsync();
            };

            btnModeration = new Button
            {
                Text = "🔧 Модерация",
                Width = 100,
                Height = 35,
                BackColor = Color.FromArgb(255, 193, 7),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Visible = false
            };
            btnModeration.Click += (s, e) =>
            {
                using var modForm = new ModerationForm();
                modForm.ShowDialog();
                _ = LoadListingsAsync();
            };

            btnNotifications = new Button
            {
                Text = "🔔 Уведомления",
                Width = 120,
                Height = 35,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnNotifications.Click += (s, e) =>
            {
                using var notifForm = new NotificationsForm();
                notifForm.ShowDialog();
                _ = CheckNotifications();
            };

            lblStatus = new Label
            {
                Text = "Загрузка...",
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9),
                AutoSize = true
            };

            topPanel.Controls.Add(txtSearch);
            topPanel.Controls.Add(cbTypeFilter);
            topPanel.Controls.Add(cbGenderFilter);
            topPanel.Controls.Add(cbSizeFilter);
            topPanel.Controls.Add(cbStatusFilter);
            topPanel.Controls.Add(btnRefresh);
            topPanel.Controls.Add(btnAddListing);
            topPanel.Controls.Add(btnProfile);
            topPanel.Controls.Add(btnModeration);
            topPanel.Controls.Add(btnNotifications);
            topPanel.Controls.Add(lblStatus);

            txtSearch.Location = new Point(10, 15);
            cbTypeFilter.Location = new Point(270, 15);
            cbGenderFilter.Location = new Point(380, 15);
            cbSizeFilter.Location = new Point(490, 15);
            cbStatusFilter.Location = new Point(600, 15);
            btnRefresh.Location = new Point(730, 12);
            btnAddListing.Location = new Point(820, 12);
            btnProfile.Location = new Point(990, 12);
            btnModeration.Location = new Point(1100, 12);
            btnNotifications.Location = new Point(1210, 12);
            lblStatus.Location = new Point(1340, 22);

            pnlListings = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(15),
                BackColor = Color.FromArgb(240, 242, 245)
            };

            this.Controls.Add(pnlListings);
            this.Controls.Add(topPanel);
        }

        private async Task LoadListingsAsync()
        {
            try
            {
                lblStatus.Text = "Загрузка...";
                using var client = new HttpClient();
                var url = "https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings?select=*";
                client.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                var response = await client.GetStringAsync(url);
                _currentListings = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response) ?? new();
                lblStatus.Text = $"Найдено: {_currentListings.Count}";
                FilterListings();
                await CheckNotifications();
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Ошибка: {ex.Message}";
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
            }
        }

        private void FilterListings()
        {
            var filtered = _currentListings.AsEnumerable();

            var search = txtSearch.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(x =>
                    GetString(x, "pet_name").ToLower().Contains(search) ||
                    GetString(x, "breed").ToLower().Contains(search));
            }

            if (cbTypeFilter.SelectedItem?.ToString() == "Пропал")
                filtered = filtered.Where(x => GetString(x, "listing_type") == "lost");
            else if (cbTypeFilter.SelectedItem?.ToString() == "Найден")
                filtered = filtered.Where(x => GetString(x, "listing_type") == "found");

            if (cbGenderFilter.SelectedItem?.ToString() == "Мальчик")
                filtered = filtered.Where(x => GetString(x, "gender") == "male");
            else if (cbGenderFilter.SelectedItem?.ToString() == "Девочка")
                filtered = filtered.Where(x => GetString(x, "gender") == "female");

            if (cbSizeFilter.SelectedItem?.ToString() == "Маленький")
                filtered = filtered.Where(x => GetString(x, "size") == "small");
            else if (cbSizeFilter.SelectedItem?.ToString() == "Средний")
                filtered = filtered.Where(x => GetString(x, "size") == "medium");
            else if (cbSizeFilter.SelectedItem?.ToString() == "Большой")
                filtered = filtered.Where(x => GetString(x, "size") == "large");

            string statusFilter = cbStatusFilter.SelectedItem?.ToString();
            if (statusFilter == "Активные")
                filtered = filtered.Where(x => GetString(x, "status") == "active");
            else if (statusFilter == "На модерации")
                filtered = filtered.Where(x => GetString(x, "status") == "pending");
            else if (statusFilter == "Закрытые")
                filtered = filtered.Where(x => GetString(x, "status") == "closed" || GetString(x, "status") == "expired");

            DisplayListings(filtered.ToList());
        }

        private string GetString(Dictionary<string, object> dict, string key)
        {
            return dict.ContainsKey(key) && dict[key] != null ? dict[key].ToString() : "";
        }

        private void DisplayListings(List<Dictionary<string, object>> listings)
        {
            pnlListings.Controls.Clear();

            foreach (var item in listings)
            {
                var card = CreateCard(item);
                pnlListings.Controls.Add(card);
            }

            if (listings.Count == 0)
            {
                var lblEmpty = new Label
                {
                    Text = "Ничего не найдено",
                    Font = new Font("Segoe UI", 14),
                    ForeColor = Color.Gray,
                    AutoSize = true
                };
                pnlListings.Controls.Add(lblEmpty);
            }
        }

        private Panel CreateCard(Dictionary<string, object> item)
        {
            var card = new Panel
            {
                Width = 280,
                Height = 420,
                BackColor = Color.White,
                Margin = new Padding(12),
                Cursor = Cursors.Hand,
                BorderStyle = BorderStyle.None
            };

            card.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle,
                    Color.LightGray, 1, ButtonBorderStyle.Solid,
                    Color.LightGray, 1, ButtonBorderStyle.Solid,
                    Color.LightGray, 1, ButtonBorderStyle.Solid,
                    Color.LightGray, 1, ButtonBorderStyle.Solid);
            };

            var photoUrl = GetString(item, "photo_url");
            var photo = new PictureBox
            {
                Width = 280,
                Height = 240,
                SizeMode = PictureBoxSizeMode.Zoom,
                ImageLocation = string.IsNullOrEmpty(photoUrl) ? null : photoUrl,
                BackColor = Color.FromArgb(240, 242, 245)
            };

            string status = GetString(item, "status");
            Color statusColor = Color.Gray;
            string statusText = "";
            if (status == "active") { statusColor = Color.FromArgb(40, 167, 69); statusText = "Активен"; }
            else if (status == "pending") { statusColor = Color.FromArgb(255, 193, 7); statusText = "На модерации"; }
            else if (status == "closed") { statusColor = Color.FromArgb(23, 162, 184); statusText = "Закрыт"; }
            else if (status == "expired") { statusColor = Color.FromArgb(108, 117, 125); statusText = "Просрочен"; }
            else { statusText = "Активен"; statusColor = Color.FromArgb(40, 167, 69); }

            var statusBadge = new Panel
            {
                Width = 100,
                Height = 26,
                BackColor = statusColor,
                Location = new Point(10, 10)
            };
            var statusLabel = new Label
            {
                Text = statusText,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            statusBadge.Controls.Add(statusLabel);
            photo.Controls.Add(statusBadge);

            var listingType = GetString(item, "listing_type");
            var typeBadge = new Panel
            {
                Width = 80,
                Height = 26,
                BackColor = listingType == "lost" ? Color.FromArgb(220, 53, 69) : Color.FromArgb(40, 167, 69),
                Location = new Point(10, 40)
            };
            var typeLabel = new Label
            {
                Text = listingType == "lost" ? "ПРОПАЛ" : "НАЙДЕН",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            typeBadge.Controls.Add(typeLabel);
            photo.Controls.Add(typeBadge);

            var nameLabel = new Label
            {
                Text = string.IsNullOrEmpty(GetString(item, "pet_name")) ? "Безымянный" : GetString(item, "pet_name"),
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 122, 204),
                Location = new Point(12, 255),
                Size = new Size(256, 35)
            };

            var infoLabel = new Label
            {
                Text = $"🐾 {GetString(item, "species")}  •  {(GetString(item, "gender") == "male" ? "♂" : "♀")}",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.Gray,
                Location = new Point(12, 295),
                Size = new Size(256, 25)
            };

            var locationLabel = new Label
            {
                Text = $"📍 {GetString(item, "location")}",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray,
                Location = new Point(12, 325),
                Size = new Size(256, 30)
            };

            var separator = new Label
            {
                Text = "━━━━━━━━━━━━━━━━━━━━━━━━━━",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.LightGray,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(12, 365),
                Size = new Size(256, 20)
            };

            var dateLabel = new Label
            {
                Text = GetDate(GetString(item, "created_at")).ToString("dd.MM.yyyy"),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleRight,
                Location = new Point(12, 385),
                Size = new Size(256, 20)
            };

            card.Controls.Add(photo);
            card.Controls.Add(nameLabel);
            card.Controls.Add(infoLabel);
            card.Controls.Add(locationLabel);
            card.Controls.Add(separator);
            card.Controls.Add(dateLabel);

            card.Click += (s, e) => ShowDetail(item);
            photo.Click += (s, e) => ShowDetail(item);
            nameLabel.Click += (s, e) => ShowDetail(item);
            infoLabel.Click += (s, e) => ShowDetail(item);
            locationLabel.Click += (s, e) => ShowDetail(item);

            return card;
        }

        private DateTime GetDate(string dateStr)
        {
            if (DateTime.TryParse(dateStr, out var date))
                return date;
            return DateTime.Now;
        }

        private void ShowDetail(Dictionary<string, object> item)
        {
            var detailForm = new DetailForm(item);
            detailForm.ShowDialog();
        }
    }
}