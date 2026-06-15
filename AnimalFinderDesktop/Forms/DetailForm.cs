using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
        private static readonly Color PrimaryColor = Color.FromArgb(0, 122, 204);
        private static readonly Color SuccessColor = Color.FromArgb(40, 167, 69);
        private static readonly Color DangerColor = Color.FromArgb(220, 53, 69);
        private static readonly Color WarningColor = Color.FromArgb(255, 193, 7);
        private static readonly Color BackgroundColor = Color.FromArgb(245, 247, 250);
        private static readonly Color CardColor = Color.White;
        private static readonly Color TextColor = Color.FromArgb(0, 0, 0);
        private static readonly Color MutedColor = Color.FromArgb(80, 80, 80);
        private static readonly Color BorderColor = Color.FromArgb(226, 232, 240);

        private List<Dictionary<string, object>> _allListings;
        private int _currentIndex;
        private Dictionary<string, object> _item;
        private string _currentUserId;
        private string _currentUserRole;
        private string _authorName;
        private Dictionary<string, object> _verificationRequest;

        private PictureBox pbPhoto;
        private Label lblCounter;
        private List<string> _photoPaths;
        private int _currentPhotoIndex;
        private ToolTip toolTip;

        public DetailForm(Dictionary<string, object> item, List<Dictionary<string, object>> allListings = null)
        {
            _item = item;
            _allListings = allListings;
            _currentIndex = allListings?.FindIndex(x => GetString("id") == x["id"]?.ToString()) ?? 0;

            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "AnimalFinder - Просмотр объявления";
            this.Size = new Size(1350, 750);
            this.MinimumSize = new Size(1350, 750);
            this.MaximumSize = new Size(1350, 750);
            this.BackColor = BackgroundColor;
            this.Font = new Font("Segoe UI", 10);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            toolTip = new ToolTip();
            LoadCurrentUser();
            LoadAuthorName();
            LoadVerificationRequest();
            LoadPhotos();
            InitializeUI();
        }

        private void LoadPhotos()
        {
            _photoPaths = new List<string>();
            var photoUrlsRaw = GetString("photo_urls");
            if (!string.IsNullOrEmpty(photoUrlsRaw))
            {
                var urls = photoUrlsRaw.Split(';');
                foreach (var url in urls)
                {
                    string localPath = Path.Combine(Application.StartupPath, url);
                    if (File.Exists(localPath))
                        _photoPaths.Add(localPath);
                }
            }
            _currentPhotoIndex = 0;
        }

        private void LoadCurrentUser()
        {
            try
            {
                var client = SupabaseService.GetClient().Result;
                _currentUserId = client.Auth.CurrentUser?.Id;
                using var httpClient = new HttpClient();
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/profiles?user_id=eq.{_currentUserId}&select=role";
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                var response = httpClient.GetStringAsync(url).Result;
                var profiles = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response);
                if (profiles != null && profiles.Count > 0)
                {
                    _currentUserRole = profiles[0].ContainsKey("role") ? profiles[0]["role"].ToString() : "user";
                }
            }
            catch { _currentUserRole = "user"; }
        }

        private void LoadAuthorName()
        {
            try
            {
                var authorId = GetString("user_id");
                if (!string.IsNullOrEmpty(authorId))
                {
                    using var httpClient = new HttpClient();
                    var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/profiles?user_id=eq.{authorId}&select=display_name,rating";
                    httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                    var response = httpClient.GetStringAsync(url).Result;
                    var profiles = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response);
                    if (profiles != null && profiles.Count > 0)
                    {
                        var name = profiles[0].ContainsKey("display_name") ? profiles[0]["display_name"]?.ToString() : "";
                        _authorName = string.IsNullOrEmpty(name) ? "Пользователь" : name;
                        double rating = profiles[0].ContainsKey("rating") ? Convert.ToDouble(profiles[0]["rating"]) : 0;
                        if (rating > 0)
                            _authorName += $" ⭐ {rating:F1}";
                    }
                }
            }
            catch { _authorName = "Пользователь"; }
        }

        private void LoadVerificationRequest()
        {
            try
            {
                var listingId = GetString("id");
                using var httpClient = new HttpClient();
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/verification_requests?pet_listing_id=eq.{listingId}&select=*";
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                var response = httpClient.GetStringAsync(url).Result;
                var list = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response);
                if (list != null && list.Count > 0)
                    _verificationRequest = list[0];
            }
            catch { _verificationRequest = null; }
        }

        private void InitializeUI()
        {
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0),
                BackColor = BackgroundColor
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));

            mainLayout.Controls.Add(CreateMainContent(), 0, 0);
            mainLayout.Controls.Add(CreateInfoCards(), 0, 1);
            mainLayout.Controls.Add(CreateFooter(), 0, 2);

            this.Controls.Add(mainLayout);
            UpdatePhoto();
        }

        private Panel CreateMainContent()
        {
            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(25, 20, 25, 15) };

            var contentLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));

            // ЛЕВАЯ ЧАСТЬ - Фото
            var photoPanel = new Panel { Dock = DockStyle.Fill, BackColor = CardColor };
            photoPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, photoPanel.Width - 1, photoPanel.Height - 1);
            };

            pbPhoto = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = BackgroundColor };

            if (_photoPaths.Count > 1)
            {
                var navOverlay = new Panel { Dock = DockStyle.Bottom, Height = 50, BackColor = Color.FromArgb(0, 0, 0, 150) };
                var btnPrev = CreatePhotoNavButton("◀", 10, 8);
                btnPrev.Click += (s, e) => { _currentPhotoIndex = (_currentPhotoIndex - 1 + _photoPaths.Count) % _photoPaths.Count; UpdatePhoto(); };

                var lblPhotoCounter = new Label
                {
                    Text = $"{_currentPhotoIndex + 1} / {_photoPaths.Count}",
                    Location = new Point(photoPanel.Width / 2 - 35, 15),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    AutoSize = true
                };

                var btnNext = CreatePhotoNavButton("▶", photoPanel.Width - 50, 8);
                btnNext.Click += (s, e) => { _currentPhotoIndex = (_currentPhotoIndex + 1) % _photoPaths.Count; UpdatePhoto(); };

                photoPanel.Resize += (s, e) =>
                {
                    btnNext.Location = new Point(photoPanel.Width - 50, 8);
                    lblPhotoCounter.Location = new Point(photoPanel.Width / 2 - 35, 15);
                };

                navOverlay.Controls.Add(btnPrev);
                navOverlay.Controls.Add(lblPhotoCounter);
                navOverlay.Controls.Add(btnNext);
                photoPanel.Controls.Add(navOverlay);
            }

            photoPanel.Controls.Add(pbPhoto);
            pbPhoto.BringToFront();
            contentLayout.Controls.Add(photoPanel, 0, 0);

            // ПРАВАЯ ЧАСТЬ - Информация
            var infoPanel = new Panel { Dock = DockStyle.Fill, BackColor = CardColor, Padding = new Padding(25, 15, 25, 15) };
            infoPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, infoPanel.Width - 1, infoPanel.Height - 1);
            };

            int y = 10;

            // ЗАГОЛОВОК: Имя • Порода • Вид
            var petName = GetString("pet_name");
            var breed = GetString("breed");
            var species = GetString("species");
            var nameText = string.IsNullOrEmpty(petName) ? "Без имени" : petName;

            string fullTitle;
            if (species == "Грызун" || species == "Птица")
            {
                var subBreed = GetString("sub_breed");
                fullTitle = $"{nameText} • {(string.IsNullOrEmpty(subBreed) ? breed : subBreed)} • {species}";
            }
            else
            {
                fullTitle = $"{nameText} • {(string.IsNullOrEmpty(breed) ? species : breed)} • {species}";
            }

            var lblTitle = new Label
            {
                Text = fullTitle,
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = PrimaryColor,
                Location = new Point(0, y),
                AutoSize = true
            };
            infoPanel.Controls.Add(lblTitle);
            y += 45;

            // СТАТУСЫ: ПРОПАЛ(А) • На проверке • Верификация
            var listingType = GetString("listing_type");
            var status = GetString("status");
            var isVerified = GetString("is_animal_verified") == "True";

            var typeText = listingType == "lost" ? "❗ ПРОПАЛ(А)" : "✅ НАЙДЕН(А)";
            var typeColor = listingType == "lost" ? DangerColor : SuccessColor;

            var statusText = status == "active" ? "• Активно" : (status == "on_moderation" ? "• ⏳ На проверке" : "• Закрыто");
            var statusColor = status == "active" ? SuccessColor : (status == "on_moderation" ? WarningColor : MutedColor);

            string verifText = "";
            Color verifColor = MutedColor;
            if (listingType == "lost")
            {
                if (isVerified)
                {
                    verifText = " • ✅ Верифицировано";
                    verifColor = SuccessColor;
                }
                else if (_verificationRequest != null)
                {
                    verifText = " • ⏳ Ожидание верификации";
                    verifColor = WarningColor;
                }
                else
                {
                    verifText = " • ❌ Не верифицировано";
                    verifColor = MutedColor;
                }
            }

            var statusLabel = new Label
            {
                Text = $"{typeText} {statusText}{verifText}",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = typeColor,
                Location = new Point(0, y),
                AutoSize = true
            };
            infoPanel.Controls.Add(statusLabel);
            y += 40;

            // Дата инцидента
            var incidentDate = GetDate("incident_date");
            if (incidentDate.HasValue)
            {
                var incidentLabel = new Label
                {
                    Text = $"📅 Дата инцидента: {incidentDate.Value.ToString("dd.MM.yyyy")}",
                    Font = new Font("Segoe UI", 11),
                    ForeColor = TextColor,
                    Location = new Point(0, y),
                    AutoSize = true
                };
                infoPanel.Controls.Add(incidentLabel);
                y += 30;
            }

            // Автор
            var authorLabel = new Label
            {
                Text = $"👤 {_authorName}",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = PrimaryColor,
                Location = new Point(0, y),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            authorLabel.Click += (s, e) => OpenUserProfile(GetString("user_id"));
            infoPanel.Controls.Add(authorLabel);
            y += 45;

            // Разделитель
            var sep = new Label { Text = "────────────────────────", Font = new Font("Segoe UI", 8), ForeColor = BorderColor, Location = new Point(0, y), AutoSize = true };
            infoPanel.Controls.Add(sep);
            y += 25;

            // ХАРАКТЕРИСТИКИ (3 в ряд) - увеличенный шрифт
            var ageMonths = GetInt("age");
            string ageText = ageMonths.HasValue ? FormatAge(ageMonths.Value) : "не указан";
            var gender = GetString("gender");
            string genderText = gender == "male" ? "Мальчик" : (gender == "female" ? "Девочка" : "Не определён");
            var size = GetString("size");
            string sizeText = size switch { "small" => "Маленький", "medium" => "Средний", "large" => "Большой", _ => size };
            var color = GetString("color");
            var temperament = GetString("temperament");
            var searchRadius = GetInt("search_radius");

            // Первая строка: Возраст • Пол • Размер
            y = AddTripleInfoRow(infoPanel, "Возраст", ageText, "Пол", genderText, "Размер", sizeText, y);

            // Вторая строка: Окрас • Характер • Радиус
            if (!string.IsNullOrEmpty(color) && !string.IsNullOrEmpty(temperament))
            {
                y = AddTripleInfoRow(infoPanel, "Окрас", color, "Характер", temperament, "Радиус", searchRadius.HasValue ? $"{searchRadius} км" : "не указан", y);
            }
            else if (!string.IsNullOrEmpty(color))
            {
                y = AddInfoRow(infoPanel, "Окрас", color, y);
            }
            else if (!string.IsNullOrEmpty(temperament))
            {
                y = AddInfoRow(infoPanel, "Характер", temperament, y);
            }

            if (searchRadius.HasValue && string.IsNullOrEmpty(color) && string.IsNullOrEmpty(temperament))
            {
                y = AddInfoRow(infoPanel, "Радиус", $"{searchRadius} км", y);
            }

            // Отдельные строки для текста
            var microchip = GetString("microchip");
            var specialMarks = GetString("special_marks");

            if (!string.IsNullOrEmpty(microchip)) y = AddInfoRow(infoPanel, "Чип/клеймо", microchip, y);
            if (!string.IsNullOrEmpty(specialMarks)) y = AddInfoRow(infoPanel, "Особые приметы", specialMarks, y);

            // Кнопка верификации для модератора
            if ((_currentUserRole == "moderator" || _currentUserRole == "admin") && listingType == "lost" && !isVerified && _verificationRequest != null)
            {
                var btnVerify = CreateModernButton("📄 Проверка документов", PrimaryColor, new Size(180, 32));
                btnVerify.Location = new Point(0, y);
                btnVerify.Click += BtnVerify_Click;
                infoPanel.Controls.Add(btnVerify);
            }

            contentLayout.Controls.Add(infoPanel, 1, 0);
            mainPanel.Controls.Add(contentLayout);
            return mainPanel;
        }

        private Button CreatePhotoNavButton(string text, int x, int y)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(45, 35),
                BackColor = Color.FromArgb(200, 0, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
        }

        private int AddTripleInfoRow(Panel panel, string label1, string value1, string label2, string value2, string label3, string value3, int y)
        {
            int colWidth = 170;
            int labelWidth = 80;

            // Колонка 1 - НАЗВАНИЕ обычное, ЗНАЧЕНИЕ жирное
            var lbl1 = new Label
            {
                Text = label1 + ":",
                Font = new Font("Segoe UI", 10),
                ForeColor = MutedColor,
                Location = new Point(0, y),
                AutoSize = true
            };
            var val1 = new Label
            {
                Text = value1,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = TextColor,
                Location = new Point(labelWidth, y),
                AutoSize = true
            };
            panel.Controls.Add(lbl1);
            panel.Controls.Add(val1);

            // Колонка 2
            var lbl2 = new Label
            {
                Text = label2 + ":",
                Font = new Font("Segoe UI", 10),
                ForeColor = MutedColor,
                Location = new Point(colWidth, y),
                AutoSize = true
            };
            var val2 = new Label
            {
                Text = value2,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = TextColor,
                Location = new Point(colWidth + labelWidth, y),
                AutoSize = true
            };
            panel.Controls.Add(lbl2);
            panel.Controls.Add(val2);

            // Колонка 3
            var lbl3 = new Label
            {
                Text = label3 + ":",
                Font = new Font("Segoe UI", 10),
                ForeColor = MutedColor,
                Location = new Point(colWidth * 2, y),
                AutoSize = true
            };
            var val3 = new Label
            {
                Text = value3,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = TextColor,
                Location = new Point(colWidth * 2 + labelWidth, y),
                AutoSize = true
            };
            panel.Controls.Add(lbl3);
            panel.Controls.Add(val3);

            return y + 32;
        }

        private int AddInfoRow(Panel panel, string label, string value, int y)
        {
            var lbl = new Label
            {
                Text = label + ":",
                Font = new Font("Segoe UI", 10),
                ForeColor = MutedColor,
                Location = new Point(0, y),
                AutoSize = true
            };

            var val = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = TextColor,
                Location = new Point(140, y),
                AutoSize = true
            };

            panel.Controls.Add(lbl);
            panel.Controls.Add(val);
            return y + 32;
        }

        private Panel CreateInfoCards()
        {
            var cardsPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(25, 10, 25, 10) };
            var flowLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };

            var location = GetString("location");
            if (!string.IsNullOrEmpty(location))
            {
                flowLayout.Controls.Add(CreateInfoCard("📍 Местоположение", location, 380, true));
            }

            var contactPhone = GetString("contact_phone");
            var contactOther = GetString("contact");
            if (!string.IsNullOrEmpty(contactPhone) || !string.IsNullOrEmpty(contactOther))
            {
                string contactText = "";
                if (!string.IsNullOrEmpty(contactPhone)) contactText += $"📞 {contactPhone}";
                if (!string.IsNullOrEmpty(contactOther)) contactText += $"\n{contactOther}";
                flowLayout.Controls.Add(CreateInfoCard("📞 Контакты", contactText, 320, true));
            }

            var description = GetString("description");
            if (!string.IsNullOrEmpty(description))
            {
                flowLayout.Controls.Add(CreateInfoCard("📝 Описание", description, 520, false));
            }

            cardsPanel.Controls.Add(flowLayout);
            return cardsPanel;
        }

        private Panel CreateInfoCard(string title, string content, int maxWidth, bool canCopy)
        {
            var card = new Panel
            {
                BackColor = CardColor,
                Size = new Size(maxWidth, 160),
                Margin = new Padding(0, 0, 15, 0),
                Padding = new Padding(18)
            };

            card.Paint += (s, e) =>
            {
                using var pen = new Pen(BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = PrimaryColor,
                Location = new Point(18, 12),
                AutoSize = true
            };

            var lblContent = new Label
            {
                Text = content,
                Font = new Font("Segoe UI", 10),
                ForeColor = TextColor,
                Location = new Point(18, 42),
                Size = new Size(maxWidth - 36 - (canCopy ? 30 : 0), 100),
                AutoSize = false,
                Cursor = canCopy ? Cursors.IBeam : Cursors.Default
            };

            card.Controls.Add(lblTitle);
            card.Controls.Add(lblContent);

            if (canCopy)
            {
                var btnCopy = new Button
                {
                    Text = "📋",
                    Location = new Point(maxWidth - 45, 12),
                    Size = new Size(28, 28),
                    BackColor = PrimaryColor,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 10),
                    Cursor = Cursors.Hand
                };
                btnCopy.FlatAppearance.BorderSize = 0;
                btnCopy.Click += (s, e) =>
                {
                    Clipboard.SetText(content);
                    MessageBox.Show("Контакты скопированы в буфер обмена", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };
                card.Controls.Add(btnCopy);
            }

            return card;
        }

        private Panel CreateFooter()
        {
            var footer = new Panel { Dock = DockStyle.Fill, BackColor = CardColor };
            footer.Paint += (s, e) =>
            {
                using var pen = new Pen(BorderColor, 1);
                e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
            };

            var createdAt = GetDate("created_at");
            if (createdAt.HasValue)
            {
                var dateLabel = new Label
                {
                    Text = $"📅 Объявление создано: {createdAt.Value.ToString("dd.MM.yyyy в HH:mm")}",
                    Font = new Font("Segoe UI", 9),
                    ForeColor = MutedColor,
                    Location = new Point(25, 18),
                    AutoSize = true
                };
                footer.Controls.Add(dateLabel);
            }

            var buttonPanel = new FlowLayoutPanel
            {
                Location = new Point(500, 10),
                Size = new Size(830, 40),
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0),
                BackColor = Color.Transparent
            };

            var status = GetString("status");
            var isModerator = _currentUserRole == "moderator" || _currentUserRole == "admin";

            var btnClose = CreateModernButton("✕ Закрыть", MutedColor, new Size(120, 38));
            btnClose.Click += (s, e) => this.Close();
            buttonPanel.Controls.Add(btnClose);

            var btnPrint = CreateModernButton("🖨️ Печать", PrimaryColor, new Size(120, 38));
            btnPrint.Click += BtnPrint_Click;
            buttonPanel.Controls.Add(btnPrint);

            var btnReport = CreateIconButton("⚠️", 40);
            toolTip.SetToolTip(btnReport, "Пожаловаться на объявление");
            btnReport.Click += BtnReport_Click;
            buttonPanel.Controls.Add(btnReport);

            var btnHide = CreateIconButton("🙈", 40);
            toolTip.SetToolTip(btnHide, "Скрыть объявление");
            btnHide.Click += BtnHide_Click;
            buttonPanel.Controls.Add(btnHide);

            var btnTrack = CreateIconButton("⭐", 40);
            toolTip.SetToolTip(btnTrack, "Добавить в отслеживаемые");
            btnTrack.Click += BtnTrack_Click;
            buttonPanel.Controls.Add(btnTrack);

            string latStr = GetString("latitude");
            string lonStr = GetString("longitude");
            if (!string.IsNullOrEmpty(latStr) && !string.IsNullOrEmpty(lonStr))
            {
                var btnMap = CreateModernButton("🗺️ Карта", PrimaryColor, new Size(100, 38));
                btnMap.Click += (s, e) =>
                {
                    double lat = Convert.ToDouble(latStr);
                    double lon = Convert.ToDouble(lonStr);
                    var mapForm = new MapViewerSingleForm(lat, lon, GetString("pet_name"), GetString("location"));
                    mapForm.ShowDialog();
                };
                buttonPanel.Controls.Add(btnMap);
            }

            bool canWrite = GetString("user_id") != _currentUserId && (status == "active" || status == "on_moderation");
            if (canWrite)
            {
                var btnWrite = CreateModernButton("✉️ Написать", PrimaryColor, new Size(110, 38));
                btnWrite.Click += (s, e) =>
                {
                    var chatForm = new ChatForm(GetString("user_id"), GetString("id"), GetString("pet_name"));
                    chatForm.ShowDialog();
                };
                buttonPanel.Controls.Add(btnWrite);
            }

            if (isModerator && status == "on_moderation")
            {
                var btnApprove = CreateModernButton("✅ Одобрить", SuccessColor, new Size(110, 38));
                btnApprove.Click += async (s, e) => await ApproveListing();

                var btnReject = CreateModernButton("❌ Отклонить", DangerColor, new Size(110, 38));
                btnReject.Click += async (s, e) => await RejectListing();

                buttonPanel.Controls.Add(btnApprove);
                buttonPanel.Controls.Add(btnReject);
            }
            else if (isModerator && _verificationRequest != null && GetString("is_animal_verified") != "True")
            {
                var btnVerify = CreateModernButton("🐾 Верифицировать", PrimaryColor, new Size(140, 38));
                btnVerify.Click += async (s, e) => await ApproveVerification();
                buttonPanel.Controls.Add(btnVerify);
            }
            else
            {
                bool canMarkFound = (GetString("user_id") == _currentUserId || isModerator) && status == "active";
                if (canMarkFound)
                {
                    var btnFound = CreateModernButton("🐾 НАЙДЕН", SuccessColor, new Size(120, 38));
                    btnFound.Click += async (s, e) => await MarkAsFound();
                    buttonPanel.Controls.Add(btnFound);
                }
            }

            footer.Controls.Add(buttonPanel);
            return footer;
        }

        private Button CreateIconButton(string icon, int size)
        {
            return new Button
            {
                Text = icon,
                Size = new Size(size, 38),
                BackColor = BackgroundColor,
                ForeColor = TextColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 14),
                Cursor = Cursors.Hand,
                Margin = new Padding(3, 0, 3, 0)
            };
        }

        private Button CreateModernButton(string text, Color backColor, Size size)
        {
            return new Button
            {
                Text = text,
                Size = size,
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(5, 0, 5, 0)
            };
        }

        private void UpdatePhoto()
        {
            if (_photoPaths.Count > 0 && _currentPhotoIndex < _photoPaths.Count)
            {
                try
                {
                    pbPhoto.Image?.Dispose();
                    pbPhoto.Image = Image.FromFile(_photoPaths[_currentPhotoIndex]);
                }
                catch { pbPhoto.Image = null; }
            }
        }

        private void NavigateListing(int direction)
        {
            if (_allListings == null) return;

            _currentIndex += direction;
            if (_currentIndex < 0) _currentIndex = _allListings.Count - 1;
            if (_currentIndex >= _allListings.Count) _currentIndex = 0;

            _item = _allListings[_currentIndex];
            LoadPhotos();

            this.Controls.Clear();
            InitializeUI();
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

        private string FormatAge(int totalMonths)
        {
            int years = totalMonths / 12;
            int months = totalMonths % 12;
            if (years > 0 && months > 0) return $"{years} г {months} мес";
            if (years > 0) return $"{years} {GetYearWord(years)}";
            if (months > 0) return $"{months} {GetMonthWord(months)}";
            return "не указан";
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

        private void OpenUserProfile(string userId)
        {
            var profileForm = userId == _currentUserId ? new ProfileForm() : new ProfileForm(userId);
            profileForm.ShowDialog();
        }

        private async Task MarkAsFound()
        {
            var result = MessageBox.Show("Отметить животное как НАЙДЕННОЕ?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            try
            {
                var id = GetString("id");
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
                    MessageBox.Show("Животное отмечено как найденное!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private async Task ApproveListing()
        {
            var result = MessageBox.Show("Одобрить объявление?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            try
            {
                var id = GetString("id");
                using var client = new HttpClient();
                var updateData = new { status = "active" };
                var json = JsonConvert.SerializeObject(updateData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings?id=eq.{id}";
                client.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                await client.PatchAsync(url, content);

                MessageBox.Show("Объявление одобрено!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private async Task RejectListing()
        {
            var result = MessageBox.Show("Отклонить объявление?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;

            try
            {
                var id = GetString("id");
                using var client = new HttpClient();
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings?id=eq.{id}";
                client.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                await client.DeleteAsync(url);

                MessageBox.Show("Объявление удалено", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private async Task ApproveVerification()
        {
            var result = MessageBox.Show("Подтвердить владельца?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            try
            {
                var listingId = GetString("id");
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");

                var updateRequest = new { status = "approved", reviewed_at = DateTime.UtcNow };
                var json = JsonConvert.SerializeObject(updateRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/verification_requests?pet_listing_id=eq.{listingId}";
                await client.PatchAsync(url, content);

                var updateListing = new { is_animal_verified = true };
                var json2 = JsonConvert.SerializeObject(updateListing);
                var content2 = new StringContent(json2, Encoding.UTF8, "application/json");
                var url2 = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings?id=eq.{listingId}";
                await client.PatchAsync(url2, content2);

                MessageBox.Show("Владелец верифицирован!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private async void BtnPrint_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Функция печати", "Инфо", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnVerify_Click(object sender, EventArgs e)
        {
            if (_verificationRequest == null)
            {
                MessageBox.Show("Документы не загружены", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string docUrl = _verificationRequest.ContainsKey("document_url") ? _verificationRequest["document_url"]?.ToString() : "";
            string comment = _verificationRequest.ContainsKey("comment") ? _verificationRequest["comment"]?.ToString() : "";
            string microchip = _verificationRequest.ContainsKey("microchip") ? _verificationRequest["microchip"]?.ToString() : "";

            string message = "📄 ИНФОРМАЦИЯ О ВЕРИФИКАЦИИ\n\n";
            if (!string.IsNullOrEmpty(microchip)) message += $"Номер чипа: {microchip}\n";
            if (!string.IsNullOrEmpty(comment)) message += $"\nКомментарий владельца:\n{comment}";

            if (!string.IsNullOrEmpty(docUrl))
            {
                message += "\n\n📎 Документ загружен";
                var result = MessageBox.Show(message + "\n\nОткрыть документ?", "Верификация", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (result == DialogResult.Yes)
                {
                    string fullPath = Path.Combine(Application.StartupPath, docUrl);
                    if (File.Exists(fullPath))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(fullPath) { UseShellExecute = true });
                    }
                    else
                    {
                        MessageBox.Show("Файл не найден", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show(message, "Верификация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnReport_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Пожаловаться на это объявление?", "Жалоба", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                MessageBox.Show("Жалоба отправлена модераторам", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnHide_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Скрыть это объявление?", "Скрыть", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                MessageBox.Show("Объявление скрыто", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void BtnTrack_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Добавить в отслеживаемые?", "Отслеживать", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                MessageBox.Show("Объявление добавлено в отслеживаемые", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}