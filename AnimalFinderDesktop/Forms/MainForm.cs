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
        private static readonly Color PrimaryColor = Color.FromArgb(0, 122, 204);
        private static readonly Color SuccessColor = Color.FromArgb(40, 167, 69);
        private static readonly Color DangerColor = Color.FromArgb(220, 53, 69);
        private static readonly Color WarningColor = Color.FromArgb(255, 193, 7);
        private static readonly Color BackgroundColor = Color.FromArgb(245, 247, 250);
        private static readonly Color CardColor = Color.White;
        private static readonly Color TextColor = Color.FromArgb(51, 51, 51);
        private static readonly Color MutedColor = Color.FromArgb(108, 117, 125);
        private static readonly Color BorderColor = Color.FromArgb(226, 232, 240);

        private FlowLayoutPanel pnlListings;
        private TextBox txtSearch;
        private ComboBox cbTypeFilter, cbSpeciesFilter, cbStatusFilter, cbViewFilter;
        private Button btnTemperamentFilter;
        private Label lblStatus, lblGreeting, lblLocationText;
        private Button btnAddListing, btnProfile, btnNotifications, btnReportsModeration, btnChats, btnLocation;
        private Panel pnlNotificationBadge, pnlChatBadge;
        private System.Windows.Forms.Timer autoRefreshTimer;
        private System.Windows.Forms.Timer blinkTimer;
        private bool isBlinking = false;
        private List<string> _selectedTemperaments = new();

        private RadioButton rbNearest, rbFarthest, rbNewest;
        private ComboBox cbRadiusFilter, cbCardWidth;
        private string _sortMode = "nearest";
        private int _cardsPerRow = 4;
        private int _unreadNotifications = 0;
        private int _unreadChats = 0;

        private List<Dictionary<string, object>> _currentListings = new();
        private List<string> _favorites = new();
        private List<string> _hiddenListings = new();
        private string _currentUserRole = "user";
        private string _currentUserId;
        private string _currentUserName = "Пользователь";

        private double _userLat = 0;
        private double _userLon = 0;
        private bool _hasUserLocation = false;
        private string _userFullAddress = "";

        public MainForm()
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(1400, 900);
            this.MinimumSize = new Size(1200, 700);
            this.Text = "AnimalFinder - Главное Меню";
            this.BackColor = BackgroundColor;
            this.Font = new Font("Segoe UI", 9);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.ControlBox = true;

            LoadSavedLocation();
            SetupUI();
            this.Shown += async (s, e) => await LoadAllData();
            StartAutoRefresh();
        }

        private void LoadSavedLocation()
        {
            if (Properties.Settings.Default.HasLocation)
            {
                _userLat = Properties.Settings.Default.UserLat;
                _userLon = Properties.Settings.Default.UserLon;
                _hasUserLocation = true;
                _userFullAddress = Properties.Settings.Default.UserAddress ?? "";
            }
        }

        private async Task LoadAllData()
        {
            await LoadCurrentUser();
            await LoadListingsAsync();
            await LoadFavorites();
            await LoadHiddenListings();
            await LoadCurrentUserRole();
            UpdateGreeting();
            FilterListings();
            await UpdateNotificationsBadge();
        }

        private async Task LoadCurrentUser()
        {
            var client = await SupabaseService.GetClient();
            _currentUserId = client.Auth.CurrentUser?.Id;

            try
            {
                using var httpClient = new HttpClient();
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/profiles?user_id=eq.{_currentUserId}&select=display_name";
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                var response = await httpClient.GetStringAsync(url);
                var profiles = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response);
                if (profiles != null && profiles.Count > 0 && profiles[0].ContainsKey("display_name"))
                {
                    _currentUserName = profiles[0]["display_name"].ToString();
                }
            }
            catch { }
        }

        private void UpdateGreeting()
        {
            if (lblGreeting != null)
            {
                lblGreeting.Text = $"Здравствуйте, {_currentUserName}!";
            }
        }

        private async Task LoadFavorites()
        {
            try
            {
                using var httpClient = new HttpClient();
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/favorites?user_id=eq.{_currentUserId}&select=listing_id";
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
                using var httpClient = new HttpClient();
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/hidden_listings?user_id=eq.{_currentUserId}&select=listing_id";
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
                using var httpClient = new HttpClient();
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/profiles?user_id=eq.{_currentUserId}&select=role";
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
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Ошибка";
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
            }
        }

        private double GetDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371;
            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLon = (lon2 - lon1) * Math.PI / 180;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private void FilterListings()
        {
            var filtered = _currentListings.AsEnumerable();

            string search = txtSearch.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(x =>
                {
                    string petName = GetString(x, "pet_name");
                    string breed = GetString(x, "breed");
                    return $"{breed} {petName}".ToLower().Contains(search) || petName.Contains(search);
                });
            }

            string type = cbTypeFilter.SelectedItem?.ToString();
            if (type == "Пропал") filtered = filtered.Where(x => GetString(x, "listing_type") == "lost");
            else if (type == "Найден") filtered = filtered.Where(x => GetString(x, "listing_type") == "found");

            string species = cbSpeciesFilter.SelectedItem?.ToString();
            if (species != "Все виды") filtered = filtered.Where(x => GetString(x, "species") == species);

            if (_selectedTemperaments.Any())
                filtered = filtered.Where(x => _selectedTemperaments.Contains(GetString(x, "temperament")));

            string statusFilter = cbStatusFilter.SelectedItem?.ToString();
            if (statusFilter == "Активные") filtered = filtered.Where(x => GetString(x, "status") == "active");
            else if (statusFilter == "На проверке") filtered = filtered.Where(x => GetString(x, "status") == "on_moderation");

            string viewFilter = cbViewFilter.SelectedItem?.ToString();
            var all = filtered.ToList();

            if (viewFilter == "Отслеживаемые")
            {
                all = all.Where(x => _favorites.Contains(GetString(x, "id"))).ToList();
            }
            else if (viewFilter == "Закрытые")
            {
                all = all.Where(x => GetString(x, "status") == "closed" || GetString(x, "status") == "expired").ToList();
            }
            else if (viewFilter == "Скрытые")
            {
                all = all.Where(x => _hiddenListings.Contains(GetString(x, "id"))).ToList();
            }
            else
            {
                all = all.Where(x => !_hiddenListings.Contains(GetString(x, "id"))).ToList();
            }

            int radiusKm = 0;
            if (cbRadiusFilter?.SelectedItem != null && cbRadiusFilter.SelectedItem.ToString() != "Все")
            {
                radiusKm = int.Parse(cbRadiusFilter.SelectedItem.ToString().Replace(" км", ""));
            }

            if (radiusKm > 0 && _hasUserLocation)
            {
                all = all.Where(x =>
                {
                    if (x.ContainsKey("latitude") && x["latitude"] != null && x.ContainsKey("longitude") && x["longitude"] != null)
                    {
                        double lat = Convert.ToDouble(x["latitude"]);
                        double lon = Convert.ToDouble(x["longitude"]);
                        return GetDistance(_userLat, _userLon, lat, lon) <= radiusKm;
                    }
                    return false;
                }).ToList();
            }

            if (_sortMode == "nearest" && _hasUserLocation)
            {
                all = all.OrderBy(x =>
                {
                    if (x.ContainsKey("latitude") && x["latitude"] != null && x.ContainsKey("longitude") && x["longitude"] != null)
                    {
                        double lat = Convert.ToDouble(x["latitude"]);
                        double lon = Convert.ToDouble(x["longitude"]);
                        return GetDistance(_userLat, _userLon, lat, lon);
                    }
                    return double.MaxValue;
                }).ToList();
            }
            else if (_sortMode == "farthest" && _hasUserLocation)
            {
                all = all.OrderByDescending(x =>
                {
                    if (x.ContainsKey("latitude") && x["latitude"] != null && x.ContainsKey("longitude") && x["longitude"] != null)
                    {
                        double lat = Convert.ToDouble(x["latitude"]);
                        double lon = Convert.ToDouble(x["longitude"]);
                        return GetDistance(_userLat, _userLon, lat, lon);
                    }
                    return -1;
                }).ToList();
            }
            else
            {
                all = all.OrderByDescending(x => GetDateTime(x, "created_at")).ToList();
            }

            DisplayListings(all);
        }

        private void DisplayListings(List<Dictionary<string, object>> listings)
        {
            pnlListings.Controls.Clear();

            foreach (var item in listings)
                pnlListings.Controls.Add(CreateCard(item));

            if (listings.Count == 0)
                pnlListings.Controls.Add(new Label { Text = "📭 Нет объявлений", Font = new Font("Segoe UI", 14), ForeColor = MutedColor, AutoSize = true, Margin = new Padding(10, 60, 0, 0) });
        }

        private async Task ToggleFavorite(string listingId)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");

                if (_favorites.Contains(listingId))
                {
                    var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/favorites?user_id=eq.{_currentUserId}&listing_id=eq.{listingId}";
                    await httpClient.DeleteAsync(url);
                    _favorites.Remove(listingId);
                }
                else
                {
                    var data = new { user_id = _currentUserId, listing_id = listingId };
                    var json = JsonConvert.SerializeObject(data);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    await httpClient.PostAsync("https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/favorites", content);
                    _favorites.Add(listingId);
                }
                await LoadFavorites();
                FilterListings();
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
        }

        private async Task ToggleHidden(string listingId)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");

                if (_hiddenListings.Contains(listingId))
                {
                    var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/hidden_listings?user_id=eq.{_currentUserId}&listing_id=eq.{listingId}";
                    await httpClient.DeleteAsync(url);
                    _hiddenListings.Remove(listingId);
                }
                else
                {
                    var data = new { user_id = _currentUserId, listing_id = listingId };
                    var json = JsonConvert.SerializeObject(data);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    await httpClient.PostAsync("https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/hidden_listings", content);
                    _hiddenListings.Add(listingId);
                }
                await LoadHiddenListings();
                FilterListings();
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
        }

        private async Task ReportListing(string listingId)
        {
            using var dialog = new ReportDialog(listingId);
            dialog.ShowDialog();
        }

        private void StartBlinking()
        {
            if (blinkTimer == null)
            {
                blinkTimer = new System.Windows.Forms.Timer { Interval = 500 };
                blinkTimer.Tick += (s, e) =>
                {
                    isBlinking = !isBlinking;
                    if (btnNotifications != null)
                    {
                        btnNotifications.BackColor = isBlinking ? WarningColor : PrimaryColor;
                    }
                    if (btnChats != null)
                    {
                        btnChats.BackColor = isBlinking ? WarningColor : PrimaryColor;
                    }
                };
            }
            blinkTimer.Start();
            isBlinking = true;
        }

        private void StopBlinking()
        {
            if (blinkTimer != null)
            {
                blinkTimer.Stop();
                isBlinking = false;
            }
            if (btnNotifications != null) btnNotifications.BackColor = PrimaryColor;
            if (btnChats != null) btnChats.BackColor = PrimaryColor;
        }

        private void UpdateBadges()
        {
            if (pnlNotificationBadge != null)
            {
                pnlNotificationBadge.Visible = _unreadNotifications > 0;
                if (pnlNotificationBadge.Controls[0] is Label lbl)
                    lbl.Text = _unreadNotifications > 9 ? "9+" : _unreadNotifications.ToString();
            }

            if (pnlChatBadge != null)
            {
                pnlChatBadge.Visible = _unreadChats > 0;
                if (pnlChatBadge.Controls[0] is Label lbl)
                    lbl.Text = _unreadChats > 9 ? "9+" : _unreadChats.ToString();
            }
        }

        private async Task UpdateNotificationsBadge()
        {
            try
            {
                var client = await SupabaseService.GetClient();
                var userId = client.Auth.CurrentUser?.Id;
                var unread = await SupabaseService.GetUnreadNotifications(userId);
                _unreadNotifications = unread.Count;
                _unreadChats = 0;

                UpdateBadges();

                if ((_unreadNotifications > 0 || _unreadChats > 0) && !isBlinking)
                {
                    StartBlinking();
                }
                else if (_unreadNotifications == 0 && _unreadChats == 0 && isBlinking)
                {
                    StopBlinking();
                }
            }
            catch { }
        }

        private async void BtnLocation_Click(object sender, EventArgs e)
        {
            if (!_hasUserLocation)
            {
                var result = MessageBox.Show(
                    "📍 Определить ваше местоположение?\n\n" +
                    "Это нужно для сортировки объявлений по расстоянию.",
                    "Геолокация",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    btnLocation.Enabled = false;
                    btnLocation.Text = "⏳ Определение...";
                    await GetLocationByIPAsync();
                    btnLocation.Enabled = true;
                }
            }
            else
            {
                MessageBox.Show($"📍 Ваше местоположение:\n\n🌐 Координаты: {_userLat:F4}, {_userLon:F4}",
                                "Моё местоположение", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async Task GetLocationByIPAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                var response = await client.GetStringAsync("http://ip-api.com/json/");
                dynamic data = JsonConvert.DeserializeObject(response);

                if (data.status == "success")
                {
                    _userLat = data.lat;
                    _userLon = data.lon;
                    _hasUserLocation = true;

                    string city = data.city ?? "";
                    string region = data.regionName ?? "";
                    string country = data.country ?? "";
                    string address = $"{country}, {region}, {city}".Trim(' ', ',');

                    Properties.Settings.Default.UserLat = _userLat;
                    Properties.Settings.Default.UserLon = _userLon;
                    Properties.Settings.Default.HasLocation = true;
                    Properties.Settings.Default.UserAddress = address;
                    Properties.Settings.Default.Save();

                    if (lblLocationText != null)
                    {
                        lblLocationText.Text = address;
                        lblLocationText.Visible = !string.IsNullOrEmpty(address);
                    }

                    btnLocation.Text = "📍 Местоположение определено";

                    string message = $"✅ Местоположение определено!\n\n🌐 Координаты: {_userLat:F4}, {_userLon:F4}";
                    if (!string.IsNullOrEmpty(address))
                        message += $"\n\n📍 {address}";

                    MessageBox.Show(message, "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    FilterListings();
                }
                else
                {
                    MessageBox.Show("❌ Не удалось определить местоположение.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btnLocation.Text = "📍 Моё местоположение";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnLocation.Text = "📍 Моё местоположение";
            }
        }

        private Panel CreateCard(Dictionary<string, object> item)
        {
            int availableWidth = this.ClientSize.Width - 80;
            int cardWidth = (availableWidth / _cardsPerRow) - 30;
            cardWidth = Math.Min(cardWidth, 420);
            cardWidth = Math.Max(cardWidth, 280);
            int cardHeight = 420;

            var card = new Panel { Width = cardWidth, Height = cardHeight, BackColor = CardColor, Margin = new Padding(12), Cursor = Cursors.Hand };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            string photoUrl = GetPhotoUrl(item);
            var photo = new PictureBox { Width = cardWidth - 2, Height = 230, SizeMode = PictureBoxSizeMode.Zoom, BackColor = BackgroundColor, Location = new Point(1, 1), Cursor = Cursors.Hand };
            if (!string.IsNullOrEmpty(photoUrl) && File.Exists(Path.Combine(Application.StartupPath, photoUrl)))
                try { photo.Image = Image.FromFile(Path.Combine(Application.StartupPath, photoUrl)); } catch { }

            string listingType = GetString(item, "listing_type");
            string status = GetString(item, "status");

            var typeBadge = new Panel { Width = 85, Height = 28, BackColor = listingType == "lost" ? DangerColor : SuccessColor, Location = new Point(10, 10) };
            typeBadge.Controls.Add(new Label { Text = listingType == "lost" ? "ПРОПАЛ" : "НАЙДЕН", ForeColor = Color.White, Font = new Font("Segoe UI", 8, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter });

            var statusBadge = new Panel { Width = 105, Height = 28, BackColor = status == "on_moderation" ? WarningColor : (status == "active" ? SuccessColor : MutedColor), Location = new Point(100, 10) };
            statusBadge.Controls.Add(new Label { Text = status == "on_moderation" ? "ПРОВЕРКА" : (status == "active" ? "АКТИВЕН" : "ЗАКРЫТ"), ForeColor = Color.White, Font = new Font("Segoe UI", 8, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter });

            photo.Controls.Add(typeBadge);
            photo.Controls.Add(statusBadge);

            string listingId = GetString(item, "id");
            bool isFav = _favorites.Contains(listingId);
            var menuBtn = new Button { FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = MutedColor, Font = new Font("Segoe UI", 16), Text = "⋯", Size = new Size(36, 36), Location = new Point(cardWidth - 50, 8), Cursor = Cursors.Hand };
            menuBtn.FlatAppearance.BorderSize = 0;
            menuBtn.Click += (s, e) =>
            {
                var menu = new ContextMenuStrip();
                menu.Items.Add(new ToolStripMenuItem(isFav ? "⭐ Убрать из отслеживаемых" : "☆ Отслеживать", null, async (_, _) => await ToggleFavorite(listingId)));
                menu.Items.Add(new ToolStripMenuItem(_hiddenListings.Contains(listingId) ? "👁️ Восстановить" : "🙈 Скрыть", null, async (_, _) => await ToggleHidden(listingId)));
                if (GetString(item, "user_id") != _currentUserId)
                    menu.Items.Add(new ToolStripMenuItem("⚠️ Пожаловаться", null, async (_, _) => await ReportListing(listingId)));
                menu.Show(menuBtn, new Point(0, menuBtn.Height));
            };
            photo.Controls.Add(menuBtn);

            string petName = GetString(item, "pet_name"), breed = GetString(item, "breed"), species = GetString(item, "species");
            string gender = GetString(item, "gender") == "male" ? "♂" : (GetString(item, "gender") == "female" ? "♀" : "⚲");
            int? ageMonths = GetInt(item, "age");
            string ageStr = ageMonths.HasValue ? FormatAge(ageMonths.Value) : "возраст не указан";
            string size = GetString(item, "size") switch { "small" => "маленький", "medium" => "средний", "large" => "большой", _ => GetString(item, "size") };
            string color = GetString(item, "color");
            string incidentLabel = listingType == "lost" ? "пропажа:" : "находка:";
            string incidentStr = GetDate(item, "incident_date")?.ToString("dd.MM.yyyy") ?? "дата не указана";
            string location = GetString(item, "location");
            string createdStr = GetDate(item, "created_at")?.ToString("dd.MM.yyyy") ?? "";
            bool isAnimalVerified = GetString(item, "is_animal_verified") == "True";

            int y = 245;
            var nameLabel = new Label { Text = $"{breed} {petName}".Trim(), Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = PrimaryColor, Location = new Point(15, y), AutoSize = true, Cursor = Cursors.Hand };
            y += 28;

            var infoLabel = new Label { Text = $"{species} • {gender} • {ageStr}", Font = new Font("Segoe UI", 9), ForeColor = MutedColor, Location = new Point(15, y), AutoSize = true, Cursor = Cursors.Hand };
            y += 24;

            var detailsLabel = new Label { Text = string.IsNullOrEmpty(color) ? size : $"{size} • {color}", Font = new Font("Segoe UI", 9), ForeColor = MutedColor, Location = new Point(15, y), AutoSize = true, Cursor = Cursors.Hand };
            y += 24;

            var incidentLabelCtrl = new Label { Text = $"{incidentLabel} {incidentStr}", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = TextColor, Location = new Point(15, y), AutoSize = true, Cursor = Cursors.Hand };
            y += 24;

            var locationLabel = new Label { Text = $"📍 {location}", Font = new Font("Segoe UI", 9), ForeColor = TextColor, Location = new Point(15, y), AutoSize = true, MaximumSize = new Size(cardWidth - 40, 0), Cursor = Cursors.Hand };

            int dateY = y + 28;
            var dateLabel = new Label { Text = $"создано: {createdStr}", Font = new Font("Segoe UI", 8), ForeColor = MutedColor, AutoSize = true, Location = new Point(15, dateY), Cursor = Cursors.Hand };

            if (isAnimalVerified)
                card.Controls.Add(new Label { Text = "✓ Верифицирован", Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = SuccessColor, AutoSize = true, Location = new Point(cardWidth - 130, dateY) });

            photo.Click += (s, e) => ShowDetail(item);
            nameLabel.Click += (s, e) => ShowDetail(item);
            infoLabel.Click += (s, e) => ShowDetail(item);
            detailsLabel.Click += (s, e) => ShowDetail(item);
            incidentLabelCtrl.Click += (s, e) => ShowDetail(item);
            locationLabel.Click += (s, e) => ShowDetail(item);
            dateLabel.Click += (s, e) => ShowDetail(item);
            card.Click += (s, e) => ShowDetail(item);

            card.Controls.Add(photo);
            card.Controls.Add(nameLabel);
            card.Controls.Add(infoLabel);
            card.Controls.Add(detailsLabel);
            card.Controls.Add(incidentLabelCtrl);
            card.Controls.Add(locationLabel);
            card.Controls.Add(dateLabel);

            card.MouseEnter += (s, e) => card.BackColor = Color.FromArgb(250, 252, 254);
            card.MouseLeave += (s, e) => card.BackColor = CardColor;

            return card;
        }

        private void ShowDetail(Dictionary<string, object> item)
        {
            using var detailForm = new DetailForm(item);
            detailForm.ShowDialog();
            _ = LoadAllData();
        }

        private void SetupUI()
        {
            var headerPanel = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.White };

            var lblLogo = new Label
            {
                Text = "🐾 AnimalFinder",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = PrimaryColor,
                Location = new Point(30, 15),
                AutoSize = true
            };

            lblGreeting = new Label
            {
                Text = $"Здравствуйте, {_currentUserName}!",
                Font = new Font("Segoe UI", 13),
                ForeColor = TextColor,
                Location = new Point(250, 18),
                AutoSize = true
            };

            btnProfile = CreateModernButton("👤 Профиль", PrimaryColor, new Size(110, 36));
            btnProfile.Location = new Point(1050, 12);
            btnProfile.Click += async (s, e) => { using var profileForm = new ProfileForm(); profileForm.ShowDialog(); await LoadAllData(); };

            var notifPanel = new Panel { Location = new Point(1170, 12), Size = new Size(40, 36) };
            btnNotifications = new Button
            {
                Text = "🔔",
                Size = new Size(40, 36),
                BackColor = PrimaryColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 14)
            };
            btnNotifications.FlatAppearance.BorderSize = 0;
            btnNotifications.Click += async (s, e) => { using var notifForm = new NotificationsForm(); notifForm.ShowDialog(); await UpdateNotificationsBadge(); };

            pnlNotificationBadge = new Panel
            {
                BackColor = DangerColor,
                Size = new Size(18, 18),
                Location = new Point(24, 0),
                Cursor = Cursors.Hand
            };

            var lblNotifCount = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 7, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false
            };
            pnlNotificationBadge.Controls.Add(lblNotifCount);
            pnlNotificationBadge.Visible = false;

            notifPanel.Controls.Add(btnNotifications);
            notifPanel.Controls.Add(pnlNotificationBadge);

            var chatPanel = new Panel { Location = new Point(1220, 12), Size = new Size(40, 36) };
            btnChats = new Button
            {
                Text = "💬",
                Size = new Size(40, 36),
                BackColor = PrimaryColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 14)
            };
            btnChats.FlatAppearance.BorderSize = 0;
            btnChats.Click += async (s, e) => { using var chatsForm = new ChatsListForm(); chatsForm.ShowDialog(); await UpdateNotificationsBadge(); };

            pnlChatBadge = new Panel
            {
                BackColor = DangerColor,
                Size = new Size(18, 18),
                Location = new Point(24, 0),
                Cursor = Cursors.Hand
            };

            var lblChatCount = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 7, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false
            };
            pnlChatBadge.Controls.Add(lblChatCount);
            pnlChatBadge.Visible = false;

            chatPanel.Controls.Add(btnChats);
            chatPanel.Controls.Add(pnlChatBadge);

            headerPanel.Controls.Add(lblLogo);
            headerPanel.Controls.Add(lblGreeting);
            headerPanel.Controls.Add(btnProfile);
            headerPanel.Controls.Add(notifPanel);
            headerPanel.Controls.Add(chatPanel);

            var filterPanel = new Panel { Dock = DockStyle.Top, Height = 140, BackColor = Color.White };

            txtSearch = new TextBox
            {
                Width = 200,
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "🔍 Поиск...",
                Location = new Point(25, 10)
            };
            txtSearch.TextChanged += (s, e) => FilterListings();

            cbTypeFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 130,
                Font = new Font("Segoe UI", 10),
                Location = new Point(245, 10)
            };
            cbTypeFilter.Items.AddRange(new[] { "Все типы", "Пропал", "Найден" });
            cbTypeFilter.SelectedIndex = 0;
            cbTypeFilter.SelectedIndexChanged += (s, e) => FilterListings();

            cbSpeciesFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 130,
                Font = new Font("Segoe UI", 10),
                Location = new Point(395, 10)
            };
            cbSpeciesFilter.Items.AddRange(new[] { "Все виды", "Собака", "Кошка", "Грызун", "Птица", "Другое" });
            cbSpeciesFilter.SelectedIndex = 0;
            cbSpeciesFilter.SelectedIndexChanged += (s, e) => FilterListings();

            btnTemperamentFilter = new Button
            {
                Text = "🎭 Все",
                Width = 120,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = BackgroundColor,
                Location = new Point(545, 10)
            };
            btnTemperamentFilter.Click += BtnTemperamentFilter_Click;

            cbStatusFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 130,
                Font = new Font("Segoe UI", 10),
                Location = new Point(685, 10)
            };
            cbStatusFilter.Items.AddRange(new[] { "Все статусы", "Активные", "На проверке" });
            cbStatusFilter.SelectedIndex = 0;
            cbStatusFilter.SelectedIndexChanged += (s, e) => FilterListings();

            cbViewFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 160,
                Font = new Font("Segoe UI", 10),
                Location = new Point(835, 10)
            };
            cbViewFilter.Items.AddRange(new[] { "Все объявления", "Отслеживаемые", "Закрытые", "Скрытые" });
            cbViewFilter.SelectedIndex = 0;
            cbViewFilter.SelectedIndexChanged += (s, e) => FilterListings();

            btnAddListing = CreateModernButton("➕ Добавить", SuccessColor, new Size(150, 36));
            btnAddListing.Location = new Point(1015, 8);
            btnAddListing.Click += async (s, e) => { using var addForm = new AddListingForm(); if (addForm.ShowDialog() == DialogResult.OK) await LoadAllData(); };

            btnReportsModeration = CreateModernButton("⚠️ Жалобы", WarningColor, new Size(110, 36));
            btnReportsModeration.Location = new Point(1175, 8);
            btnReportsModeration.Visible = false;
            btnReportsModeration.Click += (s, e) => { using var reportsForm = new ModerationReportsForm(); reportsForm.ShowDialog(); };

            btnLocation = CreateModernButton("📍 Моё местоположение", PrimaryColor, new Size(200, 32));
            btnLocation.Location = new Point(25, 55);
            btnLocation.Click += BtnLocation_Click;

            lblLocationText = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8),
                ForeColor = MutedColor,
                Location = new Point(25, 92),
                AutoSize = false,
                Size = new Size(450, 20),
                Visible = false,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblSort = new Label
            {
                Text = "Сортировка:",
                Location = new Point(245, 60),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = TextColor
            };

            rbNearest = new RadioButton
            {
                Text = "Ближайшие",
                Location = new Point(325, 57),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 9)
            };

            rbFarthest = new RadioButton
            {
                Text = "Дальние",
                Location = new Point(415, 57),
                AutoSize = true,
                Font = new Font("Segoe UI", 9)
            };

            rbNewest = new RadioButton
            {
                Text = "Новые",
                Location = new Point(495, 57),
                AutoSize = true,
                Font = new Font("Segoe UI", 9)
            };

            rbNearest.CheckedChanged += (s, e) => { if (rbNearest.Checked) _sortMode = "nearest"; FilterListings(); };
            rbFarthest.CheckedChanged += (s, e) => { if (rbFarthest.Checked) _sortMode = "farthest"; FilterListings(); };
            rbNewest.CheckedChanged += (s, e) => { if (rbNewest.Checked) _sortMode = "newest"; FilterListings(); };

            var lblRadius = new Label
            {
                Text = "Радиус:",
                Location = new Point(585, 60),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = TextColor
            };

            cbRadiusFilter = new ComboBox
            {
                Width = 90,
                Location = new Point(640, 57),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
            cbRadiusFilter.Items.AddRange(new object[] { "Все", "1 км", "5 км", "10 км", "20 км", "50 км", "100 км" });
            cbRadiusFilter.SelectedIndex = 0;
            cbRadiusFilter.SelectedIndexChanged += (s, e) => FilterListings();

            var lblCards = new Label
            {
                Text = "В ряд:",
                Location = new Point(745, 60),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = TextColor
            };

            cbCardWidth = new ComboBox
            {
                Width = 60,
                Location = new Point(795, 57),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
            cbCardWidth.Items.AddRange(new object[] { "3", "4" });
            cbCardWidth.SelectedIndex = 1;
            cbCardWidth.SelectedIndexChanged += (s, e) => { _cardsPerRow = int.Parse(cbCardWidth.SelectedItem.ToString()); AdjustCardWidth(); FilterListings(); };

            lblStatus = new Label
            {
                Text = "Загрузка...",
                ForeColor = MutedColor,
                Font = new Font("Segoe UI", 9),
                AutoSize = true,
                Location = new Point(875, 60)
            };

            filterPanel.Controls.Add(txtSearch);
            filterPanel.Controls.Add(cbTypeFilter);
            filterPanel.Controls.Add(cbSpeciesFilter);
            filterPanel.Controls.Add(btnTemperamentFilter);
            filterPanel.Controls.Add(cbStatusFilter);
            filterPanel.Controls.Add(cbViewFilter);
            filterPanel.Controls.Add(btnAddListing);
            filterPanel.Controls.Add(btnReportsModeration);
            filterPanel.Controls.Add(btnLocation);
            filterPanel.Controls.Add(lblLocationText);
            filterPanel.Controls.Add(lblSort);
            filterPanel.Controls.Add(rbNearest);
            filterPanel.Controls.Add(rbFarthest);
            filterPanel.Controls.Add(rbNewest);
            filterPanel.Controls.Add(lblRadius);
            filterPanel.Controls.Add(cbRadiusFilter);
            filterPanel.Controls.Add(lblCards);
            filterPanel.Controls.Add(cbCardWidth);
            filterPanel.Controls.Add(lblStatus);

            var mainContainer = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = BackgroundColor };

            pnlListings = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20, 5, 20, 20),
                BackColor = BackgroundColor,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };

            mainContainer.Controls.Add(pnlListings);

            this.Controls.Add(mainContainer);
            this.Controls.Add(filterPanel);
            this.Controls.Add(headerPanel);
        }

        private void AdjustCardWidth()
        {
            int availableWidth = this.ClientSize.Width - 80;
            int cardWidth = (availableWidth / _cardsPerRow) - 30;

            cardWidth = Math.Min(cardWidth, 420);
            cardWidth = Math.Max(cardWidth, 280);

            if (pnlListings != null)
            {
                foreach (Control ctrl in pnlListings.Controls)
                {
                    if (ctrl is Panel card)
                    {
                        card.Width = cardWidth;
                        foreach (Control child in card.Controls)
                        {
                            if (child is PictureBox photo)
                            {
                                photo.Width = cardWidth - 2;
                            }
                        }
                    }
                }
            }
        }

        private Button CreateModernButton(string text, Color backColor, Size size)
        {
            var button = new Button
            {
                Text = text,
                Size = size,
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, 0.1f);
            button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor, 0.1f);
            return button;
        }

        private void BtnTemperamentFilter_Click(object sender, EventArgs e)
        {
            var menu = new ContextMenuStrip();
            foreach (var t in new[] { "Спокойный", "Игривый", "Активный", "Ласковый", "Пугливый", "Дружелюбный", "Независимый", "Агрессивный", "Осторожный" })
            {
                var item = new ToolStripMenuItem(t) { Checked = _selectedTemperaments.Contains(t) };
                item.Click += (_, _) => { if (item.Checked) _selectedTemperaments.Remove(t); else _selectedTemperaments.Add(t); UpdateTemperamentText(); FilterListings(); };
                menu.Items.Add(item);
            }
            menu.Items.Add(new ToolStripSeparator());
            var clear = new ToolStripMenuItem("Сбросить все");
            clear.Click += (_, _) => { _selectedTemperaments.Clear(); UpdateTemperamentText(); FilterListings(); };
            menu.Items.Add(clear);
            menu.Show(btnTemperamentFilter, new Point(0, btnTemperamentFilter.Height));
        }

        private void UpdateTemperamentText()
        {
            if (_selectedTemperaments.Count == 0)
                btnTemperamentFilter.Text = "🎭 Все";
            else if (_selectedTemperaments.Count == 1)
                btnTemperamentFilter.Text = $"🎭 {_selectedTemperaments[0]}";
            else
                btnTemperamentFilter.Text = $"🎭 {_selectedTemperaments.Count}";
        }

        private void StartAutoRefresh()
        {
            autoRefreshTimer = new System.Windows.Forms.Timer { Interval = 60000 };
            autoRefreshTimer.Tick += async (s, e) => await LoadAllData();
            autoRefreshTimer.Start();
        }

        private string GetPhotoUrl(Dictionary<string, object> dict)
        {
            string photoUrls = GetString(dict, "photo_urls");
            if (!string.IsNullOrEmpty(photoUrls))
            {
                string first = photoUrls.Split(';')[0];
                if (File.Exists(Path.Combine(Application.StartupPath, first)))
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

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            AdjustCardWidth();
        }
    }
}