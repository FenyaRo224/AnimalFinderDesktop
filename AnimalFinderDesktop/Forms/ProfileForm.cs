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
    public class ProfileForm : Form
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

        private Label lblEmail, lblName, lblPhone, lblRole, lblBio, lblSocial, lblRating, lblStats;
        private TextBox tbName, tbPhone, tbBio, tbSocial;
        private Button btnEdit, btnSave, btnClose, btnMyListings, btnLogout, btnDeleteAccount;
        private Button btnChangeEmail, btnChangePassword, btnUploadAvatar;
        private CheckBox chkShowPhone, chkShowEmail;
        private PictureBox pbAvatar;
        private Panel avatarPanel, infoPanel, actionPanel;
        private bool isEditing = false;
        private string _userId;
        private string _currentRole;
        private bool _isOwnProfile;
        private int _totalListings, _activeListings, _foundListings;
        private double _rating;
        private int _ratingCount;
        private int _userVote = 0;

        public ProfileForm() : this(null) { }

        public ProfileForm(string userId)
        {
            _userId = userId;
            _isOwnProfile = string.IsNullOrEmpty(userId);
            this.Text = "AnimalFinder - Профиль";
            this.Size = new Size(850, 950);
            this.MinimumSize = new Size(850, 950);
            this.MaximumSize = new Size(850, 950);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = BackgroundColor;
            this.Font = new Font("Segoe UI", 9);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = true;

            InitializeControls();
            LoadProfile();
        }

        private void InitializeControls()
        {
            int y = 20;
            int leftMargin = 30;
            int labelWidth = 140;
            int fieldWidth = 450;

            // Заголовок
            var lblTitle = new Label
            {
                Text = _isOwnProfile ? "👤 Мой профиль" : "👤 Профиль пользователя",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = PrimaryColor,
                Location = new Point(leftMargin, y),
                AutoSize = true
            };
            this.Controls.Add(lblTitle);
            y += 50;

            // Аватар и основная информация
            avatarPanel = new Panel
            {
                Location = new Point(leftMargin, y),
                Size = new Size(780, 140),
                BackColor = CardColor
            };
            avatarPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, avatarPanel.Width - 1, avatarPanel.Height - 1);
            };

            pbAvatar = new PictureBox
            {
                Location = new Point(20, 20),
                Size = new Size(100, 100),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = BackgroundColor,
                BorderStyle = BorderStyle.FixedSingle
            };
            avatarPanel.Controls.Add(pbAvatar);

            var lblUserName = new Label
            {
                Text = "",
                Name = "lblUserName",
                Location = new Point(140, 25),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = PrimaryColor,
                AutoSize = true
            };
            avatarPanel.Controls.Add(lblUserName);

            var lblUserRole = new Label
            {
                Text = "",
                Name = "lblUserRole",
                Location = new Point(140, 50),
                Font = new Font("Segoe UI", 10),
                ForeColor = MutedColor,
                AutoSize = true
            };
            avatarPanel.Controls.Add(lblUserRole);

            lblRating = new Label
            {
                Text = "",
                Location = new Point(140, 75),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = WarningColor,
                AutoSize = true
            };
            avatarPanel.Controls.Add(lblRating);

            if (_isOwnProfile)
            {
                btnUploadAvatar = CreateModernButton("📷 Загрузить аватар", PrimaryColor, new Size(140, 32));
                btnUploadAvatar.Location = new Point(140, 100);
                btnUploadAvatar.Click += BtnUploadAvatar_Click;
                avatarPanel.Controls.Add(btnUploadAvatar);
            }

            this.Controls.Add(avatarPanel);
            y += 175;

            // Основная информация
            infoPanel = new Panel
            {
                Location = new Point(leftMargin, y),
                Size = new Size(780, 440),  // Уменьшил с 480 до 440
                BackColor = CardColor
            };
            infoPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, infoPanel.Width - 1, infoPanel.Height - 1);
            };

            var lblSectionInfo = new Label
            {
                Text = "📋 Основная информация",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = PrimaryColor,
                Location = new Point(20, 15),
                AutoSize = true
            };
            infoPanel.Controls.Add(lblSectionInfo);

            int fieldY = 50;
            int fieldSpacing = 42;

            // Email
            var lblEmailTitle = new Label { Text = "Email:", Location = new Point(20, fieldY), Size = new Size(labelWidth, 28), TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9) };
            lblEmail = new Label { Text = "", Location = new Point(20 + labelWidth + 10, fieldY), Size = new Size(fieldWidth, 28), Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = PrimaryColor };
            infoPanel.Controls.Add(lblEmailTitle);
            infoPanel.Controls.Add(lblEmail);
            fieldY += fieldSpacing;

            // Телефон
            var lblPhoneTitle = new Label { Text = "Телефон:", Location = new Point(20, fieldY), Size = new Size(labelWidth, 28), TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9) };
            lblPhone = new Label { Text = "", Location = new Point(20 + labelWidth + 10, fieldY), Size = new Size(fieldWidth, 28), Font = new Font("Segoe UI", 10) };
            tbPhone = new TextBox { Location = new Point(20 + labelWidth + 10, fieldY), Size = new Size(fieldWidth, 30), Visible = false, Font = new Font("Segoe UI", 10) };
            infoPanel.Controls.Add(lblPhoneTitle);
            infoPanel.Controls.Add(lblPhone);
            infoPanel.Controls.Add(tbPhone);
            fieldY += fieldSpacing;

            // Настройки видимости
            if (_isOwnProfile)
            {
                var lblVisibility = new Label { Text = "Видимость:", Location = new Point(20, fieldY), Size = new Size(labelWidth, 28), TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9) };
                chkShowPhone = new CheckBox { Text = "Показывать телефон", Location = new Point(20 + labelWidth + 10, fieldY), Size = new Size(180, 28), Checked = true };
                chkShowEmail = new CheckBox { Text = "Показывать email", Location = new Point(20 + labelWidth + 200, fieldY), Size = new Size(160, 28), Checked = true };
                infoPanel.Controls.Add(lblVisibility);
                infoPanel.Controls.Add(chkShowPhone);
                infoPanel.Controls.Add(chkShowEmail);
                fieldY += fieldSpacing;
            }

            // Роль
            var lblRoleTitle = new Label { Text = "Роль:", Location = new Point(20, fieldY), Size = new Size(labelWidth, 28), TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9) };
            lblRole = new Label { Text = "", Location = new Point(20 + labelWidth + 10, fieldY), Size = new Size(fieldWidth, 28), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            infoPanel.Controls.Add(lblRoleTitle);
            infoPanel.Controls.Add(lblRole);
            fieldY += fieldSpacing;

            // Статистика
            var lblStatsTitle = new Label { Text = "Активность:", Location = new Point(20, fieldY), Size = new Size(labelWidth, 28), TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9) };
            lblStats = new Label { Text = "", Location = new Point(20 + labelWidth + 10, fieldY), Size = new Size(fieldWidth, 28), ForeColor = MutedColor };
            infoPanel.Controls.Add(lblStatsTitle);
            infoPanel.Controls.Add(lblStats);
            fieldY += fieldSpacing;

            // О себе
            var lblBioTitle = new Label { Text = "О себе:", Location = new Point(20, fieldY), Size = new Size(labelWidth, 28), TextAlign = ContentAlignment.TopRight, Font = new Font("Segoe UI", 9) };
            lblBio = new Label { Text = "", Location = new Point(20 + labelWidth + 10, fieldY), Size = new Size(fieldWidth, 50), Font = new Font("Segoe UI", 9) };
            tbBio = new TextBox { Location = new Point(20 + labelWidth + 10, fieldY), Size = new Size(fieldWidth, 50), Multiline = true, Visible = false, Font = new Font("Segoe UI", 9) };
            infoPanel.Controls.Add(lblBioTitle);
            infoPanel.Controls.Add(lblBio);
            infoPanel.Controls.Add(tbBio);
            fieldY += 65;

            // Соцсети
            var lblSocialTitle = new Label { Text = "Соцсети:", Location = new Point(20, fieldY), Size = new Size(labelWidth, 28), TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9) };
            lblSocial = new Label { Text = "", Location = new Point(20 + labelWidth + 10, fieldY), Size = new Size(fieldWidth, 28), Font = new Font("Segoe UI", 9) };
            tbSocial = new TextBox { Location = new Point(20 + labelWidth + 10, fieldY), Size = new Size(fieldWidth, 28), Visible = false, PlaceholderText = "Telegram, WhatsApp, VK", Font = new Font("Segoe UI", 9) };
            infoPanel.Controls.Add(lblSocialTitle);
            infoPanel.Controls.Add(lblSocial);
            infoPanel.Controls.Add(tbSocial);
            fieldY += 42;

            // Дата регистрации (новая строка внизу)
            var lblRegDateTitle = new Label { Text = "На сайте с:", Location = new Point(20, fieldY), Size = new Size(labelWidth, 28), TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9) };
            var lblRegDate = new Label
            {
                Name = "lblRegDate",
                Text = "",
                Location = new Point(20 + labelWidth + 10, fieldY),
                Size = new Size(fieldWidth, 28),
                Font = new Font("Segoe UI", 10),
                ForeColor = MutedColor
            };
            infoPanel.Controls.Add(lblRegDateTitle);
            infoPanel.Controls.Add(lblRegDate);

            this.Controls.Add(infoPanel);
            y += 455; 

            // Кнопки действий
            actionPanel = new Panel
            {
                Location = new Point(leftMargin, y),
                Size = new Size(780, 150),
                BackColor = CardColor
            };
            actionPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, actionPanel.Width - 1, actionPanel.Height - 1);
            };

            var lblSectionActions = new Label
            {
                Text = "⚙️ Действия",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = PrimaryColor,
                Location = new Point(20, 15),
                AutoSize = true
            };
            actionPanel.Controls.Add(lblSectionActions);

            int btnY = 40;
            int btnX = 20;
            int btnSpacing = 10;

            if (_isOwnProfile)
            {
                // === КНОПКИ ДЛЯ СВОЕГО ПРОФИЛЯ ===

                // Кнопка Редактировать
                btnEdit = CreateModernButton("✏️ Редактировать", PrimaryColor, new Size(140, 36));
                btnEdit.Location = new Point(btnX, btnY);
                btnEdit.Click += (s, e) => ToggleEditMode(true);
                actionPanel.Controls.Add(btnEdit);
                btnX += 150 + btnSpacing;

                // Кнопка Сохранить (скрыта по умолчанию)
                btnSave = CreateModernButton("✓ Сохранить", SuccessColor, new Size(140, 36));
                btnSave.Location = new Point(btnX, btnY);
                btnSave.Visible = false;
                btnSave.Click += BtnSave_Click;
                actionPanel.Controls.Add(btnSave);
                btnX += 150 + btnSpacing;

                // Кнопка Сменить email
                btnChangeEmail = CreateModernButton("📧 Сменить email", PrimaryColor, new Size(150, 36));
                btnChangeEmail.Location = new Point(btnX, btnY);
                btnChangeEmail.Click += (s, e) => new ChangeEmailForm().ShowDialog();
                actionPanel.Controls.Add(btnChangeEmail);
                btnX += 160 + btnSpacing;

                // Кнопка Сменить пароль
                btnChangePassword = CreateModernButton("🔑 Сменить пароль", PrimaryColor, new Size(160, 36));
                btnChangePassword.Location = new Point(btnX, btnY);
                btnChangePassword.Click += (s, e) => new ChangePasswordForm().ShowDialog();
                actionPanel.Controls.Add(btnChangePassword);

                // Вторая строка
                btnY += 50;
                btnX = 20;

                // Кнопка Мои объявления
                btnMyListings = CreateModernButton("📋 Мои объявления", MutedColor, new Size(170, 36));
                btnMyListings.Location = new Point(btnX, btnY);
                btnMyListings.Click += BtnMyListings_Click;
                actionPanel.Controls.Add(btnMyListings);
                btnX += 180 + btnSpacing;

                // Кнопка Выйти
                btnLogout = CreateModernButton("🚪 Выйти", DangerColor, new Size(120, 36));
                btnLogout.Location = new Point(btnX, btnY);
                btnLogout.Click += BtnLogout_Click;
                actionPanel.Controls.Add(btnLogout);
                btnX += 130 + btnSpacing;

                // Кнопка Удалить аккаунт
                btnDeleteAccount = CreateModernButton("🗑️ Удалить аккаунт", DangerColor, new Size(160, 36));
                btnDeleteAccount.Location = new Point(btnX, btnY);
                btnDeleteAccount.Click += BtnDeleteAccount_Click;
                actionPanel.Controls.Add(btnDeleteAccount);
            }
            else
            {
                // === КНОПКИ ДЛЯ ЧУЖОГО ПРОФИЛЯ ===

                // Кнопка +1
                var btnUp = CreateModernButton("👍 +1", SuccessColor, new Size(80, 36));
                btnUp.Location = new Point(btnX, btnY);
                btnUp.Click += async (s, e) => await RateUser(1);
                actionPanel.Controls.Add(btnUp);
                btnX += 90 + btnSpacing;

                // Кнопка -1
                var btnDown = CreateModernButton("👎 -1", DangerColor, new Size(80, 36));
                btnDown.Location = new Point(btnX, btnY);
                btnDown.Click += async (s, e) => await RateUser(-1);
                actionPanel.Controls.Add(btnDown);
                btnX += 90 + btnSpacing;

                // Кнопка Написать
                var btnWrite = CreateModernButton("✉️ Написать", PrimaryColor, new Size(130, 36));
                btnWrite.Location = new Point(btnX, btnY);
                btnWrite.Click += (s, e) => { var chat = new ChatForm(_userId); chat.ShowDialog(); };
                actionPanel.Controls.Add(btnWrite);
                btnX += 140 + btnSpacing;

                // Кнопка Пожаловаться
                var btnReport = CreateModernButton("⚠️ Пожаловаться", WarningColor, new Size(140, 36));
                btnReport.Location = new Point(btnX, btnY);
                btnReport.Click += async (s, e) => await ReportUser();
                actionPanel.Controls.Add(btnReport);
            }

            // Кнопка Закрыть (всегда в конце)
            btnClose = CreateModernButton("✕ Закрыть", MutedColor, new Size(120, 36));
            btnClose.Location = new Point(640, btnY);
            btnClose.Click += (s, e) => this.Close();
            actionPanel.Controls.Add(btnClose);

            this.Controls.Add(actionPanel);
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
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, 0.1f);
            button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor, 0.1f);
            return button;
        }

        private async Task RateUser(int delta)
        {
            try
            {
                var client = await SupabaseService.GetClient();
                var currentUserId = client.Auth.CurrentUser?.Id;

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");

                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/user_ratings?rater_id=eq.{currentUserId}&rated_user_id=eq.{_userId}&select=*";
                var response = await httpClient.GetStringAsync(url);
                var ratings = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response);

                double newRating = _rating;
                int newCount = _ratingCount;

                if (ratings != null && ratings.Count > 0)
                {
                    int existingVote = Convert.ToInt32(ratings[0]["rating"]);
                    string ratingId = ratings[0]["id"].ToString();

                    if (existingVote == delta)
                    {
                        var deleteUrl = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/user_ratings?id=eq.{ratingId}";
                        await httpClient.DeleteAsync(deleteUrl);

                        newRating = _rating - existingVote;
                        newCount = Math.Max(0, _ratingCount - 1);
                        _userVote = 0;
                    }
                    else
                    {
                        var updateData = new { rating = delta, created_at = DateTime.UtcNow.ToString("o") };
                        var updateJson = JsonConvert.SerializeObject(updateData);
                        var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");
                        var updateUrl = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/user_ratings?id=eq.{ratingId}";
                        await httpClient.PatchAsync(updateUrl, updateContent);

                        newRating = _rating - existingVote + delta;
                        _userVote = delta;
                    }
                }
                else
                {
                    var newRatingData = new
                    {
                        rater_id = currentUserId,
                        rated_user_id = _userId,
                        rating = delta,
                        created_at = DateTime.UtcNow.ToString("o")
                    };
                    var newJson = JsonConvert.SerializeObject(newRatingData);
                    var newContent = new StringContent(newJson, Encoding.UTF8, "application/json");
                    await httpClient.PostAsync("https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/user_ratings", newContent);

                    newRating = _rating + delta;
                    newCount = _ratingCount + 1;
                    _userVote = delta;
                }

                var updates = new { rating = newRating, rating_count = newCount };
                await SupabaseService.UpdateProfile(_userId, updates);
                _rating = newRating;
                _ratingCount = newCount;

                if (_ratingCount > 0)
                    lblRating.Text = $"{_rating:F1} ⭐ ({_ratingCount} оценок)";
                else if (_rating > 0)
                    lblRating.Text = $"{_rating:F1} ⭐ (нет оценок)";
                else
                    lblRating.Text = "Нет оценок";

                UpdateVoteButtons();

                await SupabaseService.SendNotification(
                    _userId,
                    "Изменение рейтинга",
                    $"Ваш рейтинг изменён. Текущий рейтинг: {newRating:F1}",
                    "rating",
                    null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private async Task LoadUserVote()
        {
            try
            {
                var client = await SupabaseService.GetClient();
                var currentUserId = client.Auth.CurrentUser?.Id;

                using var httpClient = new HttpClient();
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/user_ratings?rater_id=eq.{currentUserId}&rated_user_id=eq.{_userId}&select=rating";
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");

                var response = await httpClient.GetStringAsync(url);
                var ratings = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response);

                if (ratings != null && ratings.Count > 0)
                {
                    _userVote = Convert.ToInt32(ratings[0]["rating"]);
                }
                else
                {
                    _userVote = 0;
                }

                UpdateVoteButtons();
            }
            catch
            {
                _userVote = 0;
                UpdateVoteButtons();
            }
        }

        private void UpdateVoteButtons()
        {
            foreach (Control ctrl in actionPanel.Controls)
            {
                if (ctrl is Button btn)
                {
                    if (btn.Text.Contains("+1"))
                    {
                        if (_userVote == 1)
                        {
                            btn.Text = "✓ +1";
                            btn.BackColor = SuccessColor;
                        }
                        else
                        {
                            btn.Text = "👍 +1";
                            btn.BackColor = SuccessColor;
                        }
                    }
                    else if (btn.Text.Contains("-1"))
                    {
                        if (_userVote == -1)
                        {
                            btn.Text = "✓ -1";
                            btn.BackColor = DangerColor;
                        }
                        else
                        {
                            btn.Text = "👎 -1";
                            btn.BackColor = DangerColor;
                        }
                    }
                }
            }
        }

        private async Task ReportUser()
        {
            var result = MessageBox.Show("Пожаловаться на пользователя?", "Жалоба", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;

            MessageBox.Show("Жалоба отправлена модератору.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void BtnUploadAvatar_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog();
            ofd.Filter = "Изображения|*.jpg;*.jpeg;*.png";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string avatarsDir = Path.Combine(Application.StartupPath, "Avatars");
                if (!Directory.Exists(avatarsDir)) Directory.CreateDirectory(avatarsDir);

                string ext = Path.GetExtension(ofd.FileName);
                string fileName = $"{_userId}{ext}";
                string destPath = Path.Combine(avatarsDir, fileName);
                File.Copy(ofd.FileName, destPath, true);
                string relativePath = $"Avatars/{fileName}";

                var updates = new { avatar_url = relativePath };
                await SupabaseService.UpdateProfile(_userId, updates);

                pbAvatar.Image = Image.FromFile(destPath);
                MessageBox.Show("Аватар обновлён", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async void LoadProfile()
        {
            try
            {
                var client = await SupabaseService.GetClient();
                if (_isOwnProfile)
                {
                    _userId = client.Auth.CurrentUser?.Id;
                    var email = client.Auth.CurrentUser?.Email;
                    lblEmail.Text = email;
                }
                else
                {
                    lblEmail.Text = "скрыт";
                }

                var profile = await SupabaseService.GetProfile(_userId);
                if (profile != null)
                {
                    string displayName = profile.ContainsKey("display_name") ? profile["display_name"]?.ToString() : "";
                    string phone = profile.ContainsKey("phone") ? profile["phone"]?.ToString() : "";
                    string role = profile.ContainsKey("role") ? profile["role"]?.ToString() : "user";
                    string avatarUrl = profile.ContainsKey("avatar_url") ? profile["avatar_url"]?.ToString() : "";
                    string bio = profile.ContainsKey("bio") ? profile["bio"]?.ToString() : "";
                    string socialLinks = profile.ContainsKey("social_links") ? profile["social_links"]?.ToString() : "";
                    bool showPhone = profile.ContainsKey("show_phone") && profile["show_phone"]?.ToString() == "True";
                    bool showEmail = profile.ContainsKey("show_email") && profile["show_email"]?.ToString() == "True";

                    _rating = profile.ContainsKey("rating") ? Convert.ToDouble(profile["rating"]) : 0;
                    _ratingCount = profile.ContainsKey("rating_count") ? Convert.ToInt32(profile["rating_count"]) : 0;

                    _currentRole = role;

                    var lblUserName = (Label)avatarPanel.Controls["lblUserName"];
                    var lblUserRole = (Label)avatarPanel.Controls["lblUserRole"];

                    lblUserName.Text = string.IsNullOrEmpty(displayName) ? "Пользователь" : displayName;

                    string roleText = role == "admin" ? "Администратор" : (role == "moderator" ? "Модератор" : (role == "banned" ? "Заблокирован" : "Пользователь"));
                    lblUserRole.Text = roleText;
                    lblRole.Text = roleText;

                    if (_isOwnProfile)
                    {
                        lblPhone.Text = string.IsNullOrEmpty(phone) ? "Не указан" : phone;
                        tbPhone.Text = phone;
                        if (chkShowPhone != null) chkShowPhone.Checked = showPhone;
                        if (chkShowEmail != null) chkShowEmail.Checked = showEmail;
                    }
                    else
                    {
                        if (showPhone && !string.IsNullOrEmpty(phone))
                            lblPhone.Text = phone;
                        else
                            lblPhone.Text = "скрыт";
                        if (!showEmail)
                            lblEmail.Text = "скрыт";
                    }

                    if (_ratingCount > 0)
                        lblRating.Text = $"{_rating:F1} ⭐ ({_ratingCount} оценок)";
                    else if (_rating > 0)
                        lblRating.Text = $"{_rating:F1} ⭐ (нет оценок)";
                    else
                        lblRating.Text = "Нет оценок";

                    lblBio.Text = string.IsNullOrEmpty(bio) ? "Не указано" : bio;
                    lblSocial.Text = string.IsNullOrEmpty(socialLinks) ? "Не указаны" : socialLinks;
                    tbBio.Text = bio;
                    tbSocial.Text = socialLinks;

                    if (!string.IsNullOrEmpty(avatarUrl) && File.Exists(Path.Combine(Application.StartupPath, avatarUrl)))
                        pbAvatar.Image = Image.FromFile(Path.Combine(Application.StartupPath, avatarUrl));

                    _totalListings = await SupabaseService.GetUserTotalListingsCount(_userId);
                    _activeListings = await SupabaseService.GetUserActiveListingsCount(_userId);
                    _foundListings = await SupabaseService.GetUserFoundListingsCount(_userId);
                    lblStats.Text = $"Всего: {_totalListings} | Активных: {_activeListings} | Найдено: {_foundListings}";

                    // Дата регистрации
                    var regDateLabel = infoPanel.Controls.Find("lblRegDate", true).FirstOrDefault() as Label;
                    if (regDateLabel != null)
                    {
                        if (profile.ContainsKey("created_at") && profile["created_at"] != null)
                        {
                            if (DateTime.TryParse(profile["created_at"].ToString(), out var regDate))
                            {
                                regDateLabel.Text = regDate.ToString("dd MMMM yyyy г.");
                            }
                            else
                            {
                                regDateLabel.Text = profile["created_at"].ToString();
                            }
                        }
                        else
                        {
                            regDateLabel.Text = "неизвестно";
                        }
                    }

                    if (!_isOwnProfile)
                    {
                        await LoadUserVote();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки профиля: {ex.Message}");
            }
        }

        private void ToggleEditMode(bool editing)
        {
            isEditing = editing;
            lblPhone.Visible = !editing;
            lblBio.Visible = !editing;
            lblSocial.Visible = !editing;
            tbPhone.Visible = editing;
            tbBio.Visible = editing;
            tbSocial.Visible = editing;
            if (chkShowPhone != null) chkShowPhone.Visible = editing;
            if (chkShowEmail != null) chkShowEmail.Visible = editing;
            btnEdit.Visible = !editing;
            btnSave.Visible = editing;
        }

        private async void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                var updates = new
                {
                    phone = tbPhone.Text,
                    bio = tbBio.Text,
                    social_links = tbSocial.Text,
                    show_phone = chkShowPhone?.Checked ?? true,
                    show_email = chkShowEmail?.Checked ?? true
                };
                var success = await SupabaseService.UpdateProfile(_userId, updates);
                if (success)
                {
                    lblPhone.Text = string.IsNullOrEmpty(tbPhone.Text) ? "Не указан" : tbPhone.Text;
                    lblBio.Text = string.IsNullOrEmpty(tbBio.Text) ? "Не указано" : tbBio.Text;
                    lblSocial.Text = string.IsNullOrEmpty(tbSocial.Text) ? "Не указаны" : tbSocial.Text;
                    MessageBox.Show("Профиль обновлён", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ToggleEditMode(false);
                }
                else
                {
                    MessageBox.Show("Ошибка сохранения", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private async void BtnLogout_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти?", "Выход", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            var client = await SupabaseService.GetClient();
            await client.Auth.SignOut();

            this.DialogResult = DialogResult.OK;
            this.Close();
            Application.Restart();
        }

        private async void BtnDeleteAccount_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Удалить аккаунт? Все объявления будут удалены.", "Удаление", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;

            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");

                await httpClient.DeleteAsync($"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings?user_id=eq.{_userId}");
                await httpClient.DeleteAsync($"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/profiles?user_id=eq.{_userId}");

                var serviceRoleKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imh0dXN1eHNqeHhzdWR6eHdqbnZ0Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc2NjE2NzkyNywiZXhwIjoyMDgxNzQzOTI3fQ.oERnxKvFqXnVkfK_xWcYQBvzJeqjXn4yUy_iQOpYXJI";
                var adminClient = new HttpClient();
                adminClient.DefaultRequestHeaders.Add("apikey", serviceRoleKey);
                adminClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {serviceRoleKey}");
                await adminClient.DeleteAsync($"https://htusuxsjxxsudzxwjnvt.supabase.co/auth/v1/admin/users/{_userId}");

                MessageBox.Show("Аккаунт удалён", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

                var client = await SupabaseService.GetClient();
                await client.Auth.SignOut();
                Application.Restart();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void BtnMyListings_Click(object sender, EventArgs e)
        {
            using var myListingsForm = new MyListingsForm(_userId);
            myListingsForm.ShowDialog();
        }
    }
}