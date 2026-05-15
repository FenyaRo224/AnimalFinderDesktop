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
    public class DetailForm : Form
    {
        private Dictionary<string, object> _item;
        private string _currentUserId;
        private string _currentUserRole;
        private bool _isUserVerified;
        private Button btnMarkFound;
        private string _authorName;

        public DetailForm(Dictionary<string, object> item)
        {
            _item = item;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Информация о животном";
            this.Size = new Size(950, 850);
            this.MinimumSize = new Size(900, 800);
            this.BackColor = Color.White;
            LoadCurrentUser();
            LoadAuthorName();
            LoadData();
        }

        private async void LoadCurrentUser()
        {
            try
            {
                var client = await SupabaseService.GetClient();
                _currentUserId = client.Auth.CurrentUser?.Id;
                using var httpClient = new HttpClient();
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/profiles?user_id=eq.{_currentUserId}&select=role,is_verified";
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                var response = await httpClient.GetStringAsync(url);
                var profiles = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response);
                if (profiles != null && profiles.Count > 0)
                {
                    if (profiles[0].ContainsKey("role"))
                        _currentUserRole = profiles[0]["role"].ToString();
                    else
                        _currentUserRole = "user";

                    if (profiles[0].ContainsKey("is_verified"))
                        _isUserVerified = profiles[0]["is_verified"]?.ToString() == "True";
                    else
                        _isUserVerified = false;
                }
                else
                {
                    _currentUserRole = "user";
                    _isUserVerified = false;
                }
            }
            catch { _currentUserRole = "user"; _isUserVerified = false; }
        }

        private async void LoadAuthorName()
        {
            try
            {
                var authorId = GetString("user_id");
                if (!string.IsNullOrEmpty(authorId))
                {
                    using var httpClient = new HttpClient();
                    var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/profiles?user_id=eq.{authorId}&select=display_name,is_verified,rating";
                    httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                    var response = await httpClient.GetStringAsync(url);
                    var profiles = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response);
                    if (profiles != null && profiles.Count > 0)
                    {
                        var name = profiles[0].ContainsKey("display_name") ? profiles[0]["display_name"]?.ToString() : "";
                        _authorName = string.IsNullOrEmpty(name) ? "Пользователь" : name;

                        if (profiles[0].ContainsKey("is_verified") && profiles[0]["is_verified"]?.ToString() == "True")
                            _authorName += " ✅";

                        double rating = profiles[0].ContainsKey("rating") ? Convert.ToDouble(profiles[0]["rating"]) : 0;
                        if (rating > 0)
                            _authorName += $" ⭐ {rating:F1}";
                    }
                    else
                    {
                        _authorName = "Пользователь";
                    }
                }
                else
                {
                    _authorName = "Неизвестный автор";
                }
            }
            catch { _authorName = "Пользователь"; }
        }

        private string GetString(string key)
        {
            return _item.ContainsKey(key) && _item[key] != null ? _item[key].ToString() : "";
        }

        private int? GetInt(string key)
        {
            if (_item.ContainsKey(key) && _item[key] != null && int.TryParse(_item[key].ToString(), out var val))
                return val;
            return null;
        }

        private DateTime? GetDate(string key)
        {
            if (_item.ContainsKey(key) && _item[key] != null && DateTime.TryParse(_item[key].ToString(), out var date))
                return date;
            return null;
        }

        private async Task AddStatusHistory(string oldStatus, string newStatus)
        {
            try
            {
                var listingId = GetString("id");
                using var httpClient = new HttpClient();
                var historyData = new { listing_id = listingId, old_status = oldStatus, new_status = newStatus, changed_by = _currentUserId };
                var json = JsonConvert.SerializeObject(historyData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = "https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/listing_status_history";
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                await httpClient.PostAsync(url, content);
            }
            catch { }
        }

        private async Task MarkAsFound()
        {
            var result = MessageBox.Show("Отметить животное как НАЙДЕННОЕ? Объявление будет закрыто.", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            try
            {
                var id = GetString("id");
                var oldStatus = GetString("status");
                using var httpClient = new HttpClient();
                var updateData = new { status = "closed" };
                var json = JsonConvert.SerializeObject(updateData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings?id=eq.{id}";
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                var response = await httpClient.PatchAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    await AddStatusHistory(oldStatus, "closed");
                    MessageBox.Show("Объявление закрыто. Животное отмечено как найденное.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Ошибка при обновлении статуса.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private async void BtnVerifyAnimal_Click(object sender, EventArgs e)
        {
            var id = GetString("id");
            var result = MessageBox.Show("Верифицировать это животное? (Подтверждение, что животное действительно принадлежит владельцу)", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            try
            {
                using var httpClient = new HttpClient();
                var updateData = new { is_animal_verified = true };
                var json = JsonConvert.SerializeObject(updateData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings?id=eq.{id}";
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                var response = await httpClient.PatchAsync(url, content);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Животное верифицировано! Теперь в объявлении будет специальный значок.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Ошибка верификации", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void LoadData()
        {
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = Color.White
            };
            mainLayout.RowStyles.Clear();
            mainLayout.ColumnStyles.Clear();

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 320));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));

            // СТРОКА 0: СТАТУС
            var listingType = GetString("listing_type");
            string status = GetString("status");
            string statusTextRu = status == "active" ? "Активен" : (status == "pending" ? "На модерации" : (status == "closed" ? "Закрыт" : "Просрочен"));
            Color statusColor = status == "active" ? Color.FromArgb(40, 167, 69) : (status == "pending" ? Color.FromArgb(255, 193, 7) : Color.FromArgb(108, 117, 125));
            var typeColor = listingType == "lost" ? Color.FromArgb(220, 53, 69) : Color.FromArgb(40, 167, 69);
            var statusPanel = new Panel { Dock = DockStyle.Fill, BackColor = typeColor };
            var statusLabel = new Label
            {
                Text = listingType == "lost" ? "ПРОПАЛ(А)" : "НАЙДЕН(А)",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            statusPanel.Controls.Add(statusLabel);
            var subStatusPanel = new Panel { Dock = DockStyle.Fill, BackColor = statusColor };
            var subStatusLabel = new Label
            {
                Text = statusTextRu,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            subStatusPanel.Controls.Add(subStatusLabel);
            var topLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            topLayout.Controls.Add(statusPanel, 0, 0);
            topLayout.Controls.Add(subStatusPanel, 1, 0);
            mainLayout.Controls.Add(topLayout, 0, 0);

            // СТРОКА 1: ФОТО + ИНФОРМАЦИЯ
            var row1Layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0, 15, 0, 15)
            };
            row1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            row1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));

            var photoUrl = GetString("photo_url");
            var photoBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                ImageLocation = string.IsNullOrEmpty(photoUrl) ? null : photoUrl,
                BackColor = Color.FromArgb(245, 245, 245),
                BorderStyle = BorderStyle.FixedSingle
            };
            row1Layout.Controls.Add(photoBox, 0, 0);

            var infoPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15, 0, 0, 0), AutoScroll = true };
            int infoY = 0;

            var name = GetString("pet_name");
            var nameLabel = new Label
            {
                Text = string.IsNullOrEmpty(name) ? "Без имени" : name,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 122, 204),
                Location = new Point(0, infoY),
                AutoSize = true
            };
            infoPanel.Controls.Add(nameLabel);
            infoY += 38;

            // Значок верификации пользователя (автор)
            if (_isUserVerified)
            {
                var verifiedIcon = new Label
                {
                    Text = "✅",
                    Font = new Font("Segoe UI", 12),
                    ForeColor = Color.FromArgb(40, 167, 69),
                    Location = new Point(nameLabel.Right + 5, nameLabel.Top + 3),
                    AutoSize = true
                };
                infoPanel.Controls.Add(verifiedIcon);
            }

            // Значок верификации животного
            if (GetString("is_animal_verified") == "True")
            {
                var animalVerifiedIcon = new Label
                {
                    Text = " 🐾 Верифицировано",
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = Color.FromArgb(40, 167, 69),
                    Location = new Point(nameLabel.Right + (GetString("is_animal_verified") == "True" ? 30 : 0), nameLabel.Top + 8),
                    AutoSize = true
                };
                infoPanel.Controls.Add(animalVerifiedIcon);
            }

            var species = GetString("species");
            var breed = GetString("breed");
            var speciesLabel = new Label
            {
                Text = species,
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(80, 80, 80),
                Location = new Point(0, infoY),
                AutoSize = true
            };
            infoPanel.Controls.Add(speciesLabel);
            if (!string.IsNullOrEmpty(breed))
            {
                var breedLabel = new Label
                {
                    Text = $" • {breed}",
                    Font = new Font("Segoe UI", 11),
                    ForeColor = Color.FromArgb(80, 80, 80),
                    Location = new Point(speciesLabel.Right + 5, infoY),
                    AutoSize = true
                };
                infoPanel.Controls.Add(breedLabel);
            }
            infoY += 32;

            // Автор объявления
            if (!string.IsNullOrEmpty(_authorName))
            {
                var authorLabel = new Label
                {
                    Text = $"👤 Автор: {_authorName}",
                    Font = new Font("Segoe UI", 9),
                    ForeColor = Color.FromArgb(80, 80, 80),
                    Location = new Point(0, infoY),
                    AutoSize = true
                };
                infoPanel.Controls.Add(authorLabel);
                infoY += 28;
            }

            var sep = new Label
            {
                Text = "──────────────────────",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(200, 200, 200),
                Location = new Point(0, infoY),
                AutoSize = true
            };
            infoPanel.Controls.Add(sep);
            infoY += 22;

            // Характеристики
            var ageMonths = GetInt("age");
            string ageText = "не указан";
            if (ageMonths.HasValue)
            {
                int years = ageMonths.Value / 12;
                int months = ageMonths.Value % 12;
                if (years > 0 && months > 0) ageText = $"{years} лет {months} мес";
                else if (years > 0) ageText = $"{years} {GetYearWord(years)}";
                else if (months > 0) ageText = $"{months} {GetMonthWord(months)}";
            }
            var gender = GetString("gender");
            string genderText = gender == "male" ? "Мальчик" : (gender == "female" ? "Девочка" : "Не определён");
            var size = GetString("size");
            var color = GetString("color");
            var temperament = GetString("temperament");
            var microchip = GetString("microchip");
            var location = GetString("location");
            var searchRadius = GetInt("search_radius");
            var incidentDate = GetDate("incident_date");
            var createdAt = GetDate("created_at");

            infoY = AddInfoLine(infoPanel, "Возраст", ageText, infoY);
            infoY = AddInfoLine(infoPanel, "Пол", genderText, infoY);
            if (!string.IsNullOrEmpty(size)) infoY = AddInfoLine(infoPanel, "Размер", size, infoY);
            if (!string.IsNullOrEmpty(color)) infoY = AddInfoLine(infoPanel, "Окрас", color, infoY);
            if (!string.IsNullOrEmpty(temperament)) infoY = AddInfoLine(infoPanel, "Характер", temperament, infoY);
            if (!string.IsNullOrEmpty(microchip)) infoY = AddInfoLine(infoPanel, "Чип/клеймо", microchip, infoY);
            if (searchRadius.HasValue) infoY = AddInfoLine(infoPanel, "Радиус поиска", $"{searchRadius} км", infoY);
            if (incidentDate.HasValue) infoY = AddInfoLine(infoPanel, "Дата пропажи/находки", incidentDate.Value.ToString("dd.MM.yyyy"), infoY);

            row1Layout.Controls.Add(infoPanel, 1, 0);
            mainLayout.Controls.Add(row1Layout, 0, 1);

            // СТРОКА 2: КАРТОЧКИ (МЕСТО, КОНТАКТЫ, ОПИСАНИЕ)
            var row2Layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0, 5, 0, 5)
            };
            row2Layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            row2Layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            row2Layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            if (!string.IsNullOrEmpty(location))
            {
                var locPanel = CreateCardPanel("📍 Местоположение", location);
                row2Layout.Controls.Add(locPanel, 0, 0);
            }

            var contact = GetString("contact");
            var contactPhone = GetString("contact_phone");
            if (!string.IsNullOrEmpty(contact) || !string.IsNullOrEmpty(contactPhone))
            {
                string contactText = "";
                if (!string.IsNullOrEmpty(contact)) contactText += $"{contact}";
                if (!string.IsNullOrEmpty(contactPhone)) contactText += (!string.IsNullOrEmpty(contactText) ? "\n" : "") + $"Телефон: {contactPhone}";
                var contactPanel = CreateCardPanel("📞 Контакты", contactText);
                row2Layout.Controls.Add(contactPanel, 0, 1);
            }

            var description = GetString("description");
            if (!string.IsNullOrEmpty(description))
            {
                var descPanel = CreateCardPanel("📝 Описание", description);
                row2Layout.Controls.Add(descPanel, 0, 2);
            }

            mainLayout.Controls.Add(row2Layout, 0, 2);

            // СТРОКА 3: КНОПКИ
            var row3Layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(0, 10, 0, 0)
            };
            row3Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            row3Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
            row3Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));

            var dateLabel = new Label
            {
                Text = createdAt.HasValue ? $"📅 Опубликовано: {createdAt.Value:dd.MM.yyyy HH:mm}" : "",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(120, 120, 120),
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill
            };
            row3Layout.Controls.Add(dateLabel, 0, 0);

            // Определяем, что объявление активно, и автор или модератор
            bool canMarkFound = (GetString("user_id") == _currentUserId || _currentUserRole == "moderator" || _currentUserRole == "admin") && status == "active";
            bool canVerifyAnimal = (_currentUserRole == "moderator" || _currentUserRole == "admin") && GetString("is_animal_verified") != "True";

            if (canMarkFound)
            {
                btnMarkFound = new Button
                {
                    Text = "🐾 ОТМЕТИТЬ НАЙДЕННЫМ",
                    BackColor = Color.FromArgb(40, 167, 69),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    Dock = DockStyle.Fill
                };
                btnMarkFound.Click += async (s, e) => await MarkAsFound();
                row3Layout.Controls.Add(btnMarkFound, 1, 0);
            }
            else if (canVerifyAnimal)
            {
                var btnVerifyAnimal = new Button
                {
                    Text = "✅ ВЕРИФИЦИРОВАТЬ ЖИВОТНОЕ",
                    BackColor = Color.FromArgb(40, 167, 69),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    Dock = DockStyle.Fill
                };
                btnVerifyAnimal.Click += BtnVerifyAnimal_Click;
                row3Layout.Controls.Add(btnVerifyAnimal, 1, 0);
            }
            else
            {
                row3Layout.Controls.Add(new Panel(), 1, 0);
            }

            var btnClose = new Button
            {
                Text = "Закрыть",
                Width = 120,
                Height = 35,
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Anchor = AnchorStyles.Right
            };
            btnClose.Click += (s, e) => this.Close();
            var btnPanel = new Panel { Dock = DockStyle.Fill };
            btnPanel.Controls.Add(btnClose);
            btnClose.Location = new Point(btnPanel.Width - 130, 5);
            btnPanel.Resize += (s, e) => btnClose.Location = new Point(btnPanel.Width - 130, 5);
            row3Layout.Controls.Add(btnPanel, 2, 0);

            mainLayout.Controls.Add(row3Layout, 0, 3);
            this.Controls.Add(mainLayout);
        }

        private Panel CreateCardPanel(string title, string content)
        {
            var panel = new Panel
            {
                BackColor = Color.FromArgb(248, 249, 250),
                Padding = new Padding(15, 10, 15, 10),
                Margin = new Padding(0, 6, 0, 6),
                Dock = DockStyle.Top
            };

            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 122, 204),
                Dock = DockStyle.Top,
                AutoSize = true
            };
            panel.Controls.Add(titleLabel);

            var contentLabel = new Label
            {
                Text = content,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(60, 60, 60),
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(0, 8, 0, 5)
            };
            panel.Controls.Add(contentLabel);

            return panel;
        }

        private int AddInfoLine(Panel panel, string label, string value, int y)
        {
            var lbl = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 80, 80),
                Location = new Point(0, y),
                Size = new Size(100, 24)
            };
            var val = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(40, 40, 40),
                Location = new Point(105, y),
                Size = new Size(350, 24),
                AutoSize = true
            };
            panel.Controls.Add(lbl);
            panel.Controls.Add(val);
            return y + 30;
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
    }
}