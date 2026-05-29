using AnimalFinderDesktop.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AnimalFinderDesktop.Forms
{
    public partial class MainForm : Form
    {
        // UI элементы
        private FlowLayoutPanel pnlListings;          // активные объявления
        private FlowLayoutPanel pnlClosedListings;    // закрытые
        private FlowLayoutPanel pnlHiddenListings;    // скрытые
        private TextBox txtSearch;
        private ComboBox cbTypeFilter;                // Пропал/Найден
        private ComboBox cbSpeciesFilter;             // Вид
        private ComboBox cbTemperamentFilter;
        private Button btnTemperamentFilter;
        private Label lblStatus;
        private Button btnAddListing, btnProfile, btnNotifications, btnReportsModeration;
        private System.Windows.Forms.Timer autoRefreshTimer;
        private Button btnToggleClosed, btnToggleHidden;
        private Panel pnlClosedHeader, pnlHiddenHeader;

        // Переключатели статуса (активен / на проверке / закрыт)
        private RadioButton rbActive, rbOnModeration, rbAllStatus;
        private string _currentStatusFilter = "active"; // active, on_moderation, all

        private List<Dictionary<string, object>> _currentListings = new();
        private List<string> _favorites = new();
        private List<string> _hiddenListings = new();
        private string _currentUserRole = "user";
        private bool _showClosed = false;
        private bool _showHidden = false;
        private List<string> _selectedTemperaments = new();

        public MainForm()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(1300, 800);
            this.Text = "AnimalFinder - Поиск пропавших животных";
            this.BackColor = Color.FromArgb(240, 242, 245);
            SetupUI();
            _ = LoadListingsAsync();
            _ = LoadCurrentUserRole();
            _ = LoadFavorites();
            _ = LoadHiddenListings();
            StartAutoRefresh();
        }

        private void InitializeComponent()
        {
            // Пустой, т.к. UI строится в SetupUI
        }

        private void SetupUI()
        {
            // Верхняя панель с фильтрами (первая строка)
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.White,
                Padding = new Padding(10)
            };

            txtSearch = new TextBox
            {
                Width = 200,
                Font = new Font("Segoe UI", 11),
                PlaceholderText = "Поиск по породе или кличке..."
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

            cbSpeciesFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 100,
                Font = new Font("Segoe UI", 10)
            };
            cbSpeciesFilter.Items.AddRange(new[] { "Все виды", "Собака", "Кошка", "Грызун", "Птица", "Другое" });
            cbSpeciesFilter.SelectedIndex = 0;
            cbSpeciesFilter.SelectedIndexChanged += (s, e) => FilterListings();

            btnTemperamentFilter = new Button
            {
                Text = "Характер: Все",
                Width = 120,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 242, 245),
                TextAlign = ContentAlignment.MiddleLeft
            };
            btnTemperamentFilter.Click += BtnTemperamentFilter_Click;

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
            };

            btnReportsModeration = new Button
            {
                Text = "⚠️ Жалобы",
                Width = 100,
                Height = 35,
                BackColor = Color.FromArgb(255, 193, 7),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Visible = false
            };
            btnReportsModeration.Click += (s, e) =>
            {
                using var reportsForm = new ModerationReportsForm();
                reportsForm.ShowDialog();
            };

            lblStatus = new Label
            {
                Text = "Загрузка...",
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9),
                AutoSize = true,
                Location = new Point(10, 45)
            };

            // Размещение первой строки
            topPanel.Controls.Add(txtSearch);
            topPanel.Controls.Add(cbTypeFilter);
            topPanel.Controls.Add(cbSpeciesFilter);
            topPanel.Controls.Add(btnTemperamentFilter);
            topPanel.Controls.Add(btnAddListing);
            topPanel.Controls.Add(btnProfile);
            topPanel.Controls.Add(btnNotifications);
            topPanel.Controls.Add(btnReportsModeration);
            topPanel.Controls.Add(lblStatus);

            txtSearch.Location = new Point(10, 12);
            cbTypeFilter.Location = new Point(220, 12);
            cbSpeciesFilter.Location = new Point(330, 12);
            btnTemperamentFilter.Location = new Point(440, 12);
            btnAddListing.Location = new Point(900, 10);
            btnProfile.Location = new Point(1070, 10);
            btnNotifications.Location = new Point(1180, 10);
            btnReportsModeration.Location = new Point(1070, 45);
            lblStatus.Location = new Point(10, 45);

            // Панель переключателей статуса (вторая строка)
            var statusPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.White,
                Padding = new Padding(10)
            };
            rbAllStatus = new RadioButton { Text = "Все", Location = new Point(10, 10), AutoSize = true, Checked = true };
            rbActive = new RadioButton { Text = "Активные", Location = new Point(70, 10), AutoSize = true };
            rbOnModeration = new RadioButton { Text = "На проверке", Location = new Point(160, 10), AutoSize = true };
            rbAllStatus.CheckedChanged += (s, e) => { if (rbAllStatus.Checked) _currentStatusFilter = "all"; FilterListings(); };
            rbActive.CheckedChanged += (s, e) => { if (rbActive.Checked) _currentStatusFilter = "active"; FilterListings(); };
            rbOnModeration.CheckedChanged += (s, e) => { if (rbOnModeration.Checked) _currentStatusFilter = "on_moderation"; FilterListings(); };
            statusPanel.Controls.Add(rbAllStatus);
            statusPanel.Controls.Add(rbActive);
            statusPanel.Controls.Add(rbOnModeration);

            // Панель для активных объявлений
            pnlListings = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(15),
                BackColor = Color.FromArgb(240, 242, 245)
            };

            // Заголовок для закрытых объявлений
            pnlClosedHeader = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.FromArgb(230, 230, 230),
                Cursor = Cursors.Hand
            };
            btnToggleClosed = new Button
            {
                Text = "▶ Показать закрытые объявления",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(230, 230, 230),
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 80, 80)
            };
            btnToggleClosed.Click += (s, e) => ToggleClosedListings();
            pnlClosedHeader.Controls.Add(btnToggleClosed);

            pnlClosedListings = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoScroll = true,
                Padding = new Padding(15),
                BackColor = Color.FromArgb(240, 242, 245),
                Visible = false,
                Height = 0
            };

            // Заголовок для скрытых объявлений
            pnlHiddenHeader = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.FromArgb(220, 220, 220),
                Cursor = Cursors.Hand
            };
            btnToggleHidden = new Button
            {
                Text = "▶ Показать скрытые объявления",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(220, 220, 220),
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 80, 80)
            };
            btnToggleHidden.Click += (s, e) => ToggleHiddenListings();
            pnlHiddenHeader.Controls.Add(btnToggleHidden);

            pnlHiddenListings = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoScroll = true,
                Padding = new Padding(15),
                BackColor = Color.FromArgb(240, 242, 245),
                Visible = false,
                Height = 0
            };

            this.Controls.Add(pnlListings);
            this.Controls.Add(pnlClosedListings);
            this.Controls.Add(pnlHiddenListings);
            this.Controls.Add(pnlClosedHeader);
            this.Controls.Add(pnlHiddenHeader);
            this.Controls.Add(statusPanel);
            this.Controls.Add(topPanel);
        }

        private void BtnTemperamentFilter_Click(object sender, EventArgs e)
        {
            var menu = new ContextMenuStrip();
            string[] temperaments = { "Спокойный", "Игривый", "Активный", "Ласковый", "Пугливый", "Дружелюбный", "Независимый", "Агрессивный", "Осторожный" };
            foreach (var temp in temperaments)
            {
                var item = new ToolStripMenuItem(temp);
                item.Checked = _selectedTemperaments.Contains(temp);
                item.Click += (s, ev) =>
                {
                    if (item.Checked)
                        _selectedTemperaments.Remove(temp);
                    else
                        _selectedTemperaments.Add(temp);
                    UpdateTemperamentButtonText();
                    FilterListings();
                };
                menu.Items.Add(item);
            }
            menu.Items.Add(new ToolStripSeparator());
            var clearItem = new ToolStripMenuItem("Сбросить все");
            clearItem.Click += (s, ev) =>
            {
                _selectedTemperaments.Clear();
                UpdateTemperamentButtonText();
                FilterListings();
            };
            menu.Items.Add(clearItem);
            menu.Show(btnTemperamentFilter, new Point(0, btnTemperamentFilter.Height));
        }

        private void UpdateTemperamentButtonText()
        {
            if (_selectedTemperaments.Count == 0)
                btnTemperamentFilter.Text = "Характер: Все";
            else if (_selectedTemperaments.Count == 1)
                btnTemperamentFilter.Text = $"Характер: {_selectedTemperaments[0]}";
            else
                btnTemperamentFilter.Text = $"Характер: {_selectedTemperaments.Count} выбрано";
        }

        private void StartAutoRefresh()
        {
            autoRefreshTimer = new System.Windows.Forms.Timer { Interval = 60000 };
            autoRefreshTimer.Tick += async (s, e) => await LoadListingsAsync();
            autoRefreshTimer.Start();
        }

        private async Task LoadFavorites()
        {
            try
            {
                var client = await SupabaseService.GetClient();
                var userId = client.Auth.CurrentUser?.Id;
                using var httpClient = new HttpClient();
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/favorites?user_id=eq.{userId}&select=listing_id";
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                var response = await httpClient.GetStringAsync(url);
                var favs = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response);
                _favorites = favs?.Select(f => f["listing_id"]?.ToString()).ToList() ?? new List<string>();
            }
            catch { _favorites = new List<string>(); }
        }

        private async Task LoadHiddenListings()
        {
            try
            {
                var client = await SupabaseService.GetClient();
                var userId = client.Auth.CurrentUser?.Id;
                using var httpClient = new HttpClient();
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/hidden_listings?user_id=eq.{userId}&select=listing_id";
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                var response = await httpClient.GetStringAsync(url);
                var hidden = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response);
                _hiddenListings = hidden?.Select(h => h["listing_id"]?.ToString()).ToList() ?? new List<string>();
            }
            catch { _hiddenListings = new List<string>(); }
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
                        btnReportsModeration.Visible = true;
                }
            }
            catch { }
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
                await LoadFavorites();
                await LoadHiddenListings();
                FilterListings();
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

            // Поиск
            string search = txtSearch.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(x =>
                {
                    string petName = GetString(x, "pet_name");
                    string breed = GetString(x, "breed");
                    string fullTitle = $"{breed} {petName}".ToLower();
                    return fullTitle.Contains(search) || petName.Contains(search);
                });
            }

            // Тип
            string typeFilter = cbTypeFilter.SelectedItem?.ToString();
            if (typeFilter == "Пропал")
                filtered = filtered.Where(x => GetString(x, "listing_type") == "lost");
            else if (typeFilter == "Найден")
                filtered = filtered.Where(x => GetString(x, "listing_type") == "found");

            // Вид
            string speciesFilter = cbSpeciesFilter.SelectedItem?.ToString();
            if (speciesFilter != "Все виды")
                filtered = filtered.Where(x => GetString(x, "species") == speciesFilter);

            // Характер
            if (_selectedTemperaments.Any())
            {
                filtered = filtered.Where(x =>
                {
                    string temp = GetString(x, "temperament");
                    return _selectedTemperaments.Contains(temp);
                });
            }

            // Статус (по переключателям)
            if (_currentStatusFilter == "active")
                filtered = filtered.Where(x => GetString(x, "status") == "active");
            else if (_currentStatusFilter == "on_moderation")
                filtered = filtered.Where(x => GetString(x, "status") == "on_moderation");
            // "all" ничего не делает

            // Разделяем на активные (не закрытые и не скрытые), закрытые, скрытые
            var activeListings = filtered.Where(x =>
                GetString(x, "status") != "closed" && GetString(x, "status") != "expired" &&
                !_hiddenListings.Contains(GetString(x, "id"))).ToList();

            var closedListings = filtered.Where(x =>
                (GetString(x, "status") == "closed" || GetString(x, "status") == "expired") &&
                !_hiddenListings.Contains(GetString(x, "id"))).ToList();

            var hiddenListings = filtered.Where(x => _hiddenListings.Contains(GetString(x, "id"))).ToList();

            // Сортировка: избранные в начало
            activeListings = activeListings.OrderByDescending(x => _favorites.Contains(GetString(x, "id")))
                                          .ThenByDescending(x => GetDateTime(x, "created_at")).ToList();
            closedListings = closedListings.OrderByDescending(x => GetDateTime(x, "created_at")).ToList();
            hiddenListings = hiddenListings.OrderByDescending(x => GetDateTime(x, "created_at")).ToList();

            DisplayListings(activeListings);
            DisplayClosedListings(closedListings);
            DisplayHiddenListings(hiddenListings);
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

        private void DisplayClosedListings(List<Dictionary<string, object>> listings)
        {
            pnlClosedListings.Controls.Clear();
            if (_showClosed && listings.Any())
            {
                foreach (var item in listings)
                {
                    var card = CreateCard(item);
                    pnlClosedListings.Controls.Add(card);
                }
                pnlClosedListings.Visible = true;
                pnlClosedListings.Height = 400;
            }
            else
            {
                pnlClosedListings.Visible = false;
                pnlClosedListings.Height = 0;
            }
            string countText = listings.Any() ? $" ({listings.Count})" : "";
            btnToggleClosed.Text = _showClosed ? $"▼ Скрыть закрытые объявления{countText}" : $"▶ Показать закрытые объявления{countText}";
        }

        private void DisplayHiddenListings(List<Dictionary<string, object>> listings)
        {
            pnlHiddenListings.Controls.Clear();
            if (_showHidden && listings.Any())
            {
                foreach (var item in listings)
                {
                    var card = CreateCard(item);
                    pnlHiddenListings.Controls.Add(card);
                }
                pnlHiddenListings.Visible = true;
                pnlHiddenListings.Height = 400;
            }
            else
            {
                pnlHiddenListings.Visible = false;
                pnlHiddenListings.Height = 0;
            }
            string countText = listings.Any() ? $" ({listings.Count})" : "";
            btnToggleHidden.Text = _showHidden ? $"▼ Скрыть скрытые объявления{countText}" : $"▶ Показать скрытые объявления{countText}";
        }

        private void ToggleClosedListings()
        {
            _showClosed = !_showClosed;
            FilterListings();
        }

        private void ToggleHiddenListings()
        {
            _showHidden = !_showHidden;
            FilterListings();
        }

        private async Task ToggleFavorite(string listingId, Button starButton)
        {
            try
            {
                var client = await SupabaseService.GetClient();
                var userId = client.Auth.CurrentUser?.Id;
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");

                if (starButton.Text == "☆")
                {
                    var data = new { user_id = userId, listing_id = listingId };
                    var json = JsonConvert.SerializeObject(data);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var url = "https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/favorites";
                    var response = await httpClient.PostAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        starButton.Text = "★";
                        _favorites.Add(listingId);
                        FilterListings();
                    }
                }
                else
                {
                    var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/favorites?user_id=eq.{userId}&listing_id=eq.{listingId}";
                    var response = await httpClient.DeleteAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        starButton.Text = "☆";
                        _favorites.Remove(listingId);
                        FilterListings();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private async Task HideListing(string listingId, Button starButton)
        {
            if (_hiddenListings.Contains(listingId))
                await ToggleHidden(listingId, starButton);
            else
                await ToggleHidden(listingId, starButton);
        }

        private async Task ToggleHidden(string listingId, Button starButton)
        {
            try
            {
                var client = await SupabaseService.GetClient();
                var userId = client.Auth.CurrentUser?.Id;
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey});

                if (_hiddenListings.Contains(listingId))
                {
                    var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/hidden_listings?user_id=eq.{userId}&listing_id=eq.{listingId}";
                    await httpClient.DeleteAsync(url);
                    _hiddenListings.Remove(listingId);
                }
                else
                {
                    var data = new { user_id = userId, listing_id = listingId };
                    var json = JsonConvert.SerializeObject(data);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var url = "https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/hidden_listings";
                    await httpClient.PostAsync(url, content);
                    _hiddenListings.Add(listingId);
                }
                FilterListings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private async Task ReportListing(string listingId)
        {
            // Проверка, не жаловался ли уже
            using var client = new HttpClient();
            var checkUrl = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/reports?listing_id=eq.{listingId}&user_id=eq.{SupabaseService.GetClient().Result.Auth.CurrentUser?.Id}&select=id";
            client.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
            var checkResponse = await client.GetStringAsync(checkUrl);
            var existing = JsonConvert.DeserializeObject<List<object>>(checkResponse);
            if (existing != null && existing.Count > 0)
            {
                MessageBox.Show("Вы уже отправляли жалобу на это объявление.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var reportDialog = new ReportDialog(listingId);
            reportDialog.ShowDialog();
        }

        private Panel CreateCard(Dictionary<string, object> item)
        {
            var card = new Panel
            {
                Width = 320,
                Height = 380,
                BackColor = Color.White,
                Margin = new Padding(12),
                Cursor = Cursors.Hand,
                BorderStyle = BorderStyle.None
            };
            card.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle,
                Color.LightGray, 1, ButtonBorderStyle.Solid,
                Color.LightGray, 1, ButtonBorderStyle.Solid,
                Color.LightGray, 1, ButtonBorderStyle.Solid,
                Color.LightGray, 1, ButtonBorderStyle.Solid);

            // Фото
            string photoUrl = GetPhotoUrl(item);
            var photo = new PictureBox
            {
                Width = 318,
                Height = 200,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(240, 242, 245),
                Location = new Point(1, 1)
            };
            if (!string.IsNullOrEmpty(photoUrl) && System.IO.File.Exists(Path.Combine(Application.StartupPath, photoUrl)))
            {
                try { photo.Image = Image.FromFile(Path.Combine(Application.StartupPath, photoUrl)); }
                catch { }
            }

            // Бейджи статуса
            string listingType = GetString(item, "listing_type");
            string status = GetString(item, "status");
            Color typeColor = listingType == "lost" ? Color.FromArgb(220, 53, 69) : Color.FromArgb(40, 167, 69);
            string typeText = listingType == "lost" ? "ПРОПАЛ" : "НАЙДЕН";

            var typeBadge = new Panel
            {
                Width = 70,
                Height = 24,
                BackColor = typeColor,
                Location = new Point(8, 8)
            };
            var typeLabel = new Label
            {
                Text = typeText,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            typeBadge.Controls.Add(typeLabel);

            Color statusColor = status == "on_moderation" ? Color.FromArgb(255, 193, 7) : (status == "active" ? Color.FromArgb(40, 167, 69) : Color.FromArgb(108, 117, 125));
            string statusText = status == "on_moderation" ? "НА ПРОВЕРКЕ" : (status == "active" ? "АКТИВЕН" : (status == "closed" ? "ЗАКРЫТ" : "ПРОСРОЧЕН"));
            var statusBadge = new Panel
            {
                Width = 90,
                Height = 24,
                BackColor = statusColor,
                Location = new Point(85, 8)
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
            photo.Controls.Add(typeBadge);
            photo.Controls.Add(statusBadge);

            // Кнопки: звезда (избранное) и три точки (меню)
            string listingId = GetString(item, "id");
            bool isFavorite = _favorites.Contains(listingId);
            var starButton = new Button
            {
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.Gold,
                Font = new Font("Segoe UI", 16),
                Text = isFavorite ? "★" : "☆",
                Size = new Size(32, 32),
                Location = new Point(270, 8),
                Cursor = Cursors.Hand,
                Tag = listingId
            };
            starButton.FlatAppearance.BorderSize = 0;
            starButton.Click += async (s, e) => await ToggleFavorite(listingId, starButton);

            var menuButton = new Button
            {
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 12),
                Text = "⋯",
                Size = new Size(32, 32),
                Location = new Point(235, 8),
                Cursor = Cursors.Hand,
                Tag = listingId
            };
            menuButton.FlatAppearance.BorderSize = 0;
            menuButton.Click += (s, e) =>
            {
                var menu = new ContextMenuStrip();
                // Отслеживание
                var favoriteItem = new ToolStripMenuItem(isFavorite ? "Убрать из отслеживаемых" : "Отслеживать");
                favoriteItem.Click += async (ev, arg) => await ToggleFavorite(listingId, starButton);
                menu.Items.Add(favoriteItem);
                // Скрыть / Показать
                bool isHidden = _hiddenListings.Contains(listingId);
                var hideItem = new ToolStripMenuItem(isHidden ? "Восстановить из скрытых" : "Скрыть");
                hideItem.Click += async (ev, arg) => await ToggleHidden(listingId, starButton);
                menu.Items.Add(hideItem);
                // Пожаловаться (только не на своё объявление)
                string authorId = GetString(item, "user_id");
                if (authorId != SupabaseService.GetClient().Result.Auth.CurrentUser?.Id)
                {
                    var reportItem = new ToolStripMenuItem("Пожаловаться");
                    reportItem.Click += async (ev, arg) => await ReportListing(listingId);
                    menu.Items.Add(reportItem);
                }
                menu.Show(menuButton, new Point(0, menuButton.Height));
            };

            photo.Controls.Add(starButton);
            photo.Controls.Add(menuButton);

            // Текстовая информация
            string petName = GetString(item, "pet_name");
            string breed = GetString(item, "breed");
            string species = GetString(item, "species");
            string gender = GetString(item, "gender");
            string genderSymbol = gender == "male" ? "♂" : (gender == "female" ? "♀" : "⚲");
            int? ageMonths = GetInt(item, "age");
            string ageStr = ageMonths.HasValue ? FormatAge(ageMonths.Value) : "возраст не указан";
            string size = GetString(item, "size");
            string sizeDisplay = size switch { "small" => "маленький", "medium" => "средний", "large" => "большой", _ => size };
            string color = GetString(item, "color");
            DateTime? incidentDate = GetDate(item, "incident_date");
            string incidentLabel = listingType == "lost" ? "пропажа:" : "находка:";
            string incidentStr = incidentDate.HasValue ? incidentDate.Value.ToString("dd.MM.yyyy") : "дата не указана";
            string location = GetString(item, "location");
            DateTime? createdDate = GetDate(item, "created_at");
            string createdStr = createdDate.HasValue ? createdDate.Value.ToString("dd.MM.yyyy") : "";
            bool isAnimalVerified = GetString(item, "is_animal_verified") == "True";

            int textY = 215;
            var nameLabel = new Label
            {
                Text = $"{breed} {petName}".Trim(),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 122, 204),
                Location = new Point(12, textY),
                AutoSize = true
            };
            textY += 22;
            var infoLabel = new Label
            {
                Text = $"{species} • {genderSymbol} • {ageStr}",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray,
                Location = new Point(12, textY),
                AutoSize = true
            };
            textY += 18;
            var detailsLabel = new Label
            {
                Text = string.IsNullOrEmpty(color) ? sizeDisplay : $"{sizeDisplay} • {color}",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray,
                Location = new Point(12, textY),
                AutoSize = true
            };
            textY += 18;
            var incidentLabelCtrl = new Label
            {
                Text = $"{incidentLabel} {incidentStr}",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray,
                Location = new Point(12, textY),
                AutoSize = true
            };
            textY += 18;
            var locationLabel = new Label
            {
                Text = location,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(80, 80, 80),
                Location = new Point(12, textY),
                AutoSize = true,
                MaximumSize = new Size(280, 0)
            };
            int dateY = textY + 22;
            var dateLabel = new Label
            {
                Text = $"создано: {createdStr}",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(12, dateY)
            };

            if (isAnimalVerified)
            {
                var verifiedIcon = new Label
                {
                    Text = "✓ Верифицирован",
                    Font = new Font("Segoe UI", 8, FontStyle.Bold),
                    ForeColor = Color.FromArgb(40, 167, 69),
                    AutoSize = true,
                    Location = new Point(200, dateY)
                };
                card.Controls.Add(verifiedIcon);
            }

            card.Controls.Add(photo);
            card.Controls.Add(nameLabel);
            card.Controls.Add(infoLabel);
            card.Controls.Add(detailsLabel);
            card.Controls.Add(incidentLabelCtrl);
            card.Controls.Add(locationLabel);
            card.Controls.Add(dateLabel);

            card.Click += (s, e) => ShowDetail(item);
            return card;
        }

        private string GetPhotoUrl(Dictionary<string, object> dict)
        {
            string photoUrls = GetString(dict, "photo_urls");
            if (!string.IsNullOrEmpty(photoUrls))
            {
                string first = photoUrls.Split(';')[0];
                if (System.IO.File.Exists(Path.Combine(Application.StartupPath, first)))
                    return first;
                return first;
            }
            return "";
        }

        private string GetString(Dictionary<string, object> dict, string key)
        {
            return dict.ContainsKey(key) && dict[key] != null ? dict[key].ToString() : "";
        }

        private int? GetInt(Dictionary<string, object> dict, string key)
        {
            if (dict.ContainsKey(key) && dict[key] != null && int.TryParse(dict[key].ToString(), out var val))
                return val;
            return null;
        }

        private DateTime? GetDate(Dictionary<string, object> dict, string key)
        {
            if (dict.ContainsKey(key) && dict[key] != null && DateTime.TryParse(dict[key].ToString(), out var date))
                return date;
            return null;
        }

        private DateTime GetDateTime(Dictionary<string, object> dict, string key)
        {
            if (dict.ContainsKey(key) && dict[key] != null && DateTime.TryParse(dict[key].ToString(), out var date))
                return date;
            return DateTime.MinValue;
        }

        private string FormatAge(int totalMonths)
        {
            int years = totalMonths / 12;
            int months = totalMonths % 12;
            if (years > 0 && months > 0) return $"{years} г {months} мес";
            if (years > 0) return $"{years} {GetYearWord(years)}";
            if (months > 0) return $"{months} {GetMonthWord(months)}";
            return "неизвестно";
        }

        private string GetYearWord(int years)
        {
            if (years % 10 == 1 && years % 100 != 11) return "год";
            if (years % 10 >= 2 && years % 10 <= 4 && (years % 100 < 10 || years % 100 >= 20)) return "года";
            return "лет";
        }

        private string GetMonthWord(int months)
        {
            if (months % 10 == 1 && months % 100 != 11) return "месяц";
            if (months % 10 >= 2 && months % 10 <= 4 && (months % 100 < 10 || months % 100 >= 20)) return "месяца";
            return "месяцев";
        }

        private void ShowDetail(Dictionary<string, object> item)
        {
            var detailForm = new DetailForm(item);
            detailForm.ShowDialog();
            _ = LoadListingsAsync();
        }
    }
}