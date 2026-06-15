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

        private Label lblEmail, lblName, lblPhone, lblRole, lblBio, lblSocial, lblRating, lblStats, lblBanReason;
        private TextBox tbName, tbPhone, tbBio, tbSocial;
        private Button btnEdit, btnSave, btnClose, btnMyListings, btnLogout, btnDeleteAccount;
        private Button btnChangeEmail, btnChangePassword, btnUploadAvatar, btnBanUser, btnUnbanUser;
        private CheckBox chkShowPhone, chkShowEmail;
        private PictureBox pbAvatar;
        private Panel avatarPanel, infoPanel, actionPanel, banPanel;
        private bool isEditing = false;
        private string _userId;
        private string _currentRole;
        private string _currentUserRole; // Роль текущего пользователя
        private bool _isOwnProfile;
        private int _totalListings, _activeListings, _foundListings;
        private double _rating;
        private int _ratingCount;
        private int _userVote = 0;
        private string _bannedReason = "";
        private bool _isBanned = false;

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

            // ЗАГРУЖАЕМ РОЛЬ СИНХРОННО ПЕРЕД СОЗДАНИЕМ UI
            Task.Run(() => LoadCurrentUserRoleAsync()).Wait();

            InitializeControls();
            LoadProfile();
        }

        private async Task LoadCurrentUserRoleAsync()
        {
            try
            {
                var client = await SupabaseService.GetClient();
                var currentUserId = client.Auth.CurrentUser?.Id;

                using var httpClient = new HttpClient();
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/profiles?user_id=eq.{currentUserId}&select=role";
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                var response = await httpClient.GetStringAsync(url);
                var profiles = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response);
                if (profiles != null && profiles.Count > 0 && profiles[0].ContainsKey("role"))
                {
                    _currentUserRole = profiles[0]["role"].ToString();
                }
                else
                {
                    _currentUserRole = "user";
                }
            }
            catch { _currentUserRole = "user"; }
        }

        private bool IsModeratorOrAdmin()
        {
            return _currentUserRole == "moderator" || _currentUserRole == "admin";
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

            // Панель бана (скрыта по умолчанию)
            banPanel = new Panel
            {
                Location = new Point(leftMargin, y),
                Size = new Size(780, 80),
                BackColor = Color.FromArgb(255, 235, 235),
                Visible = false
            };
            banPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(DangerColor, 2);
                e.Graphics.DrawRectangle(pen, 0, 0, banPanel.Width - 1, banPanel.Height - 1);
            };

            var lblBanIcon = new Label
            {
                Text = "🚫",
                Font = new Font("Segoe UI", 24),
                Location = new Point(15, 15),
                Size = new Size(50, 50),
                TextAlign = ContentAlignment.MiddleCenter
            };
            banPanel.Controls.Add(lblBanIcon);

            var lblBanTitle = new Label
            {
                Text = "АККАУНТ ЗАБЛОКИРОВАН",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = DangerColor,
                Location = new Point(75, 10),
                AutoSize = true
            };
            banPanel.Controls.Add(lblBanTitle);

            lblBanReason = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 10),
                ForeColor = TextColor,
                Location = new Point(75, 38),
                Size = new Size(680, 40),
                AutoSize = false
            };
            banPanel.Controls.Add(lblBanReason);

            this.Controls.Add(banPanel);

            // Основная информация
            infoPanel = new Panel
            {
                Location = new Point(leftMargin, y),
                Size = new Size(780, 440),
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

            // Дата регистрации
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
                btnEdit = CreateModernButton("✏️ Редактировать", PrimaryColor, new Size(140, 36));
                btnEdit.Location = new Point(btnX, btnY);
                btnEdit.Click += (s, e) => ToggleEditMode(true);
                actionPanel.Controls.Add(btnEdit);
                btnX += 150 + btnSpacing;

                btnSave = CreateModernButton("✓ Сохранить", SuccessColor, new Size(140, 36));
                btnSave.Location = new Point(btnX, btnY);
                btnSave.Visible = false;
                btnSave.Click += BtnSave_Click;
                actionPanel.Controls.Add(btnSave);
                btnX += 150 + btnSpacing;

                btnChangeEmail = CreateModernButton("📧 Сменить email", PrimaryColor, new Size(150, 36));
                btnChangeEmail.Location = new Point(btnX, btnY);
                btnChangeEmail.Click += (s, e) => new ChangeEmailForm().ShowDialog();
                actionPanel.Controls.Add(btnChangeEmail);
                btnX += 160 + btnSpacing;

                btnChangePassword = CreateModernButton("🔑 Сменить пароль", PrimaryColor, new Size(160, 36));
                btnChangePassword.Location = new Point(btnX, btnY);
                btnChangePassword.Click += (s, e) => new ChangePasswordForm().ShowDialog();
                actionPanel.Controls.Add(btnChangePassword);

                btnY += 50;
                btnX = 20;

                btnMyListings = CreateModernButton("📋 Мои объявления", MutedColor, new Size(170, 36));
                btnMyListings.Location = new Point(btnX, btnY);
                btnMyListings.Click += BtnMyListings_Click;
                actionPanel.Controls.Add(btnMyListings);
                btnX += 180 + btnSpacing;

                btnLogout = CreateModernButton("🚪 Выйти", DangerColor, new Size(120, 36));
                btnLogout.Location = new Point(btnX, btnY);
                btnLogout.Click += BtnLogout_Click;
                actionPanel.Controls.Add(btnLogout);
                btnX += 130 + btnSpacing;

                btnDeleteAccount = CreateModernButton("🗑️ Удалить аккаунт", DangerColor, new Size(160, 36));
                btnDeleteAccount.Location = new Point(btnX, btnY);
                btnDeleteAccount.Click += BtnDeleteAccount_Click;
                actionPanel.Controls.Add(btnDeleteAccount);
            }
            else
            {
                // === КНОПКИ ДЛЯ ЧУЖОГО ПРОФИЛЯ ===
                var btnUp = CreateModernButton("👍 +1", SuccessColor, new Size(80, 36));
                btnUp.Location = new Point(btnX, btnY);
                btnUp.Click += async (s, e) => await RateUser(1);
                actionPanel.Controls.Add(btnUp);
                btnX += 90 + btnSpacing;

                var btnDown = CreateModernButton("👎 -1", DangerColor, new Size(80, 36));
                btnDown.Location = new Point(btnX, btnY);
                btnDown.Click += async (s, e) => await RateUser(-1);
                actionPanel.Controls.Add(btnDown);
                btnX += 90 + btnSpacing;

                var btnWrite = CreateModernButton("✉️ Написать", PrimaryColor, new Size(130, 36));
                btnWrite.Location = new Point(btnX, btnY);
                btnWrite.Click += (s, e) => { var chat = new ChatForm(_userId); chat.ShowDialog(); };
                actionPanel.Controls.Add(btnWrite);
                btnX += 140 + btnSpacing;

                var btnReport = CreateModernButton("⚠️ Пожаловаться", WarningColor, new Size(140, 36));
                btnReport.Location = new Point(btnX, btnY);
                btnReport.Click += async (s, e) => await ReportUser();
                actionPanel.Controls.Add(btnReport);
                btnX += 150 + btnSpacing;

                // Кнопка Заблокировать (только для модераторов/админов)
                if (IsModeratorOrAdmin())
                {
                    btnBanUser = CreateModernButton("🚫 Заблокировать", DangerColor, new Size(140, 36));
                    btnBanUser.Location = new Point(btnX, btnY);
                    btnBanUser.Click += async (s, e) => await BanUser();
                    actionPanel.Controls.Add(btnBanUser);
                    btnX += 160 + btnSpacing;

                    // Кнопка Разблокировать (только если пользователь забанен)
                    btnUnbanUser = CreateModernButton("✅ Разблокировать", SuccessColor, new Size(150, 36));
                    btnUnbanUser.Location = new Point(btnX, btnY);
                    btnUnbanUser.Click += async (s, e) => await UnbanUser();
                    btnUnbanUser.Visible = false; // Скрываем пока не загрузится профиль
                    actionPanel.Controls.Add(btnUnbanUser);
                }
            }

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

        private async Task BanUser()
        {
            // Диалог выбора причины бана
            using var banDialog = new Form
            {
                Text = "Заблокировать пользователя",
                Size = new Size(450, 300),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = BackgroundColor
            };

            var lblTitle = new Label
            {
                Text = "🚫 Блокировка пользователя",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = DangerColor,
                Location = new Point(20, 15),
                AutoSize = true
            };
            banDialog.Controls.Add(lblTitle);

            var lblReason = new Label
            {
                Text = "Причина блокировки:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(20, 60),
                AutoSize = true
            };
            banDialog.Controls.Add(lblReason);

            var cbReason = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(20, 85),
                Size = new Size(390, 28),
                Font = new Font("Segoe UI", 10)
            };
            cbReason.Items.AddRange(new[] {
                "Спам и реклама",
                "Оскорбления и угрозы",
                "Мошенничество",
                "Нарушение правил сервиса",
                "Жестокое обращение с животными",
                "Множественные жалобы",
                "Другое"
            });
            cbReason.SelectedIndex = 0;
            banDialog.Controls.Add(cbReason);

            var lblCustomReason = new Label
            {
                Text = "Дополнительный комментарий (необязательно):",
                Font = new Font("Segoe UI", 9),
                Location = new Point(20, 120),
                AutoSize = true
            };
            banDialog.Controls.Add(lblCustomReason);

            var tbCustomReason = new TextBox
            {
                Location = new Point(20, 145),
                Size = new Size(390, 60),
                Multiline = true,
                Font = new Font("Segoe UI", 9),
                PlaceholderText = "Опишите причину подробнее..."
            };
            banDialog.Controls.Add(tbCustomReason);

            var btnConfirm = new Button
            {
                Text = "🚫 Заблокировать",
                Size = new Size(180, 36),
                Location = new Point(20, 215),
                BackColor = DangerColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                DialogResult = DialogResult.OK
            };
            banDialog.Controls.Add(btnConfirm);

            var btnCancel = new Button
            {
                Text = "Отмена",
                Size = new Size(120, 36),
                Location = new Point(210, 215),
                BackColor = MutedColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                DialogResult = DialogResult.Cancel
            };
            banDialog.Controls.Add(btnCancel);

            banDialog.AcceptButton = btnConfirm;
            banDialog.CancelButton = btnCancel;

            if (banDialog.ShowDialog() != DialogResult.OK) return;

            string reason = cbReason.SelectedItem.ToString();
            if (!string.IsNullOrEmpty(tbCustomReason.Text))
            {
                reason += $" — {tbCustomReason.Text}";
            }

            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");

                // 1. Меняем роль на banned и сохраняем причину
                var updateData = new
                {
                    role = "banned",
                    ban_reason = reason,
                    phone = (string)null,
                    bio = (string)null,
                    social_links = (string)null,
                    avatar_url = (string)null
                };
                var json = JsonConvert.SerializeObject(updateData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/profiles?user_id=eq.{_userId}";
                await httpClient.PatchAsync(url, content);

                // 2. Удаляем все объявления пользователя
                var listingsUrl = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings?user_id=eq.{_userId}";
                await httpClient.DeleteAsync(listingsUrl);

                // 3. Отправляем уведомление пользователю
                await SupabaseService.SendNotification(
                    _userId,
                    "🚫 Аккаунт заблокирован",
                    $"Ваш аккаунт был заблокирован администратором.\n\nПричина: {reason}",
                    "system",
                    null);

                MessageBox.Show(
                    $"✅ Пользователь заблокирован!\n\n" +
                    $"Причина: {reason}\n\n" +
                    $"• Все объявления удалены\n" +
                    $"• Данные профиля очищены\n" +
                    $"• Пользователь уведомлён",
                    "Успех",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка блокировки: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task UnbanUser()
        {
            var result = MessageBox.Show(
                "✅ Разблокировать пользователя?\n\nПользователь сможет снова пользоваться сервисом.",
                "Разблокировка",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");

                var updateData = new
                {
                    role = "user",
                    ban_reason = (string)null
                };
                var json = JsonConvert.SerializeObject(updateData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/profiles?user_id=eq.{_userId}";
                await httpClient.PatchAsync(url, content);

                // Уведомляем пользователя
                await SupabaseService.SendNotification(
                    _userId,
                    "✅ Аккаунт разблокирован",
                    "Ваш аккаунт был разблокирован администратором. Вы снова можете пользоваться сервисом.",
                    "system",
                    null);

                MessageBox.Show("✅ Пользователь разблокирован!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                MessageBox.Show($"❌ Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    _userVote = Convert.ToInt32(ratings[0]["rating"]);
                else
                    _userVote = 0;

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
                        btn.Text = _userVote == 1 ? "✓ +1" : "👍 +1";
                        btn.BackColor = SuccessColor;
                    }
                    else if (btn.Text.Contains("-1"))
                    {
                        btn.Text = _userVote == -1 ? "✓ -1" : "👎 -1";
                        btn.BackColor = DangerColor;
                    }
                }
            }
        }

        private async Task ReportUser()
        {
            using var reportDialog = new ReportDialog(_userId, "profile");
            var result = reportDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                MessageBox.Show("✅ Жалоба на пользователя отправлена модераторам", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
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
                string email = "";

                if (_isOwnProfile)
                {
                    _userId = client.Auth.CurrentUser?.Id;
                    email = client.Auth.CurrentUser?.Email ?? "";
                }
                else
                {
                    // Получаем email из профиля (для модераторов)
                    using var httpClient = new HttpClient();
                    var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/profiles?user_id=eq.{_userId}&select=email";
                    httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                    var response = await httpClient.GetStringAsync(url);
                    var profiles = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response);
                    if (profiles != null && profiles.Count > 0 && profiles[0].ContainsKey("email"))
                    {
                        email = profiles[0]["email"]?.ToString() ?? "";
                    }
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

                    // Загружаем причину бана если есть
                    _bannedReason = profile.ContainsKey("ban_reason") ? profile["ban_reason"]?.ToString() : "";
                    _isBanned = role == "banned";

                    _currentRole = role;

                    var lblUserName = (Label)avatarPanel.Controls["lblUserName"];
                    var lblUserRole = (Label)avatarPanel.Controls["lblUserRole"];

                    lblUserName.Text = string.IsNullOrEmpty(displayName) ? "Пользователь" : displayName;

                    string roleText = role == "admin" ? "Администратор" :
                                     (role == "moderator" ? "Модератор" :
                                     (role == "banned" ? "🚫 Заблокирован" : "Пользователь"));
                    lblUserRole.Text = roleText;
                    lblRole.Text = roleText;

                    // Показываем/скрываем панель бана
                    if (_isBanned)
                    {
                        banPanel.Visible = true;
                        lblBanReason.Text = string.IsNullOrEmpty(_bannedReason)
                            ? "Причина не указана"
                            : $"Причина: {_bannedReason}";

                        if (btnUnbanUser != null)
                            btnUnbanUser.Visible = IsModeratorOrAdmin();

                        if (btnBanUser != null)
                            btnBanUser.Visible = false;
                    }
                    else
                    {
                        banPanel.Visible = false;
                        if (btnUnbanUser != null)
                            btnUnbanUser.Visible = false;
                        if (btnBanUser != null)
                            btnBanUser.Visible = IsModeratorOrAdmin();
                    }

                    // === EMAIL ===
                    if (_isOwnProfile)
                    {
                        lblEmail.Text = email;
                        lblEmail.ForeColor = PrimaryColor;
                    }
                    else if (IsModeratorOrAdmin())
                    {
                        // Модераторы и админы ВСЕГДА видят email
                        lblEmail.Text = string.IsNullOrEmpty(email) ? "не указан" : email;
                    }
                    else
                    {
                        // Обычные пользователи видят только если разрешено
                        if (showEmail)
                        {
                            lblEmail.Text = string.IsNullOrEmpty(email) ? "не указан" : email;
                            lblEmail.ForeColor = PrimaryColor;
                        }
                        else
                        {
                            lblEmail.Text = "скрыт";
                            lblEmail.ForeColor = MutedColor;
                        }
                    }

                    // === ТЕЛЕФОН ===
                    if (_isOwnProfile)
                    {
                        lblPhone.Text = string.IsNullOrEmpty(phone) ? "Не указан" : phone;
                        tbPhone.Text = phone;
                        if (chkShowPhone != null) chkShowPhone.Checked = showPhone;
                        if (chkShowEmail != null) chkShowEmail.Checked = showEmail;
                    }
                    else if (IsModeratorOrAdmin())
                    {
                        // Модераторы и админы ВСЕГДА видят телефон
                        lblPhone.Text = string.IsNullOrEmpty(phone) ? "Не указан" : phone;
                    }
                    else
                    {
                        // Обычные пользователи видят только если разрешено
                        if (showPhone && !string.IsNullOrEmpty(phone))
                        {
                            lblPhone.Text = phone;
                            lblPhone.ForeColor = TextColor;
                        }
                        else
                        {
                            lblPhone.Text = "скрыт";
                            lblPhone.ForeColor = MutedColor;
                        }
                    }

                    // Рейтинг
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
                                regDateLabel.Text = regDate.ToString("dd MMMM yyyy г.");
                            else
                                regDateLabel.Text = profile["created_at"].ToString();
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