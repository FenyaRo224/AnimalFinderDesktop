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
    [System.ComponentModel.DesignerCategory("")]
    public class ProfileForm : Form
    {
        private Label lblEmail, lblName, lblPhone, lblRole, lblVerified, lblBio, lblSocial, lblRating, lblStats;
        private TextBox tbName, tbPhone, tbBio, tbSocial;
        private Button btnEdit, btnSave, btnClose, btnRequestModerator, btnMyListings, btnVerifyUser, btnVerification, btnLogout, btnDeleteAccount;
        private Button btnChangeEmail, btnChangePassword;
        private CheckBox chkShowPhone;
        private PictureBox pbAvatar;
        private bool isEditing = false;
        private string _userId;
        private string _currentRole;
        private bool _isVerified;
        private string _viewerRole;
        private bool _isOwnProfile;
        private int _totalListings, _activeListings, _foundListings;
        private double _rating;

        public ProfileForm(string userId = null)
        {
            _userId = userId;
            _isOwnProfile = string.IsNullOrEmpty(userId);
            this.Text = _isOwnProfile ? "Мой профиль" : "Профиль пользователя";
            this.Size = new Size(650, 850);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            InitializeControls();
            LoadProfile();
        }

        private void InitializeControls()
        {
            int y = 30;
            int left = 30;
            int labelWidth = 140;
            int fieldWidth = 400;

            // Аватар
            pbAvatar = new PictureBox
            {
                Location = new Point(260, y),
                Size = new Size(100, 100),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(240, 242, 245),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(pbAvatar);
            y += 120;

            // Email
            var lblEmailTitle = new Label { Text = "Email:", Location = new Point(left, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            lblEmail = new Label { Text = "", Location = new Point(left + labelWidth + 10, y), Size = new Size(fieldWidth, 25), Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(0, 122, 204) };
            this.Controls.Add(lblEmailTitle);
            this.Controls.Add(lblEmail);
            y += 35;

            // Имя
            var lblNameTitle = new Label { Text = "Имя:", Location = new Point(left, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            lblName = new Label { Text = "", Location = new Point(left + labelWidth + 10, y), Size = new Size(fieldWidth, 25) };
            tbName = new TextBox { Location = new Point(left + labelWidth + 10, y), Size = new Size(fieldWidth, 25), Visible = false };
            this.Controls.Add(lblNameTitle);
            this.Controls.Add(lblName);
            this.Controls.Add(tbName);
            y += 35;

            // Телефон
            var lblPhoneTitle = new Label { Text = "Телефон:", Location = new Point(left, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            lblPhone = new Label { Text = "", Location = new Point(left + labelWidth + 10, y), Size = new Size(fieldWidth, 25) };
            tbPhone = new TextBox { Location = new Point(left + labelWidth + 10, y), Size = new Size(fieldWidth, 25), Visible = false };
            chkShowPhone = new CheckBox { Text = "Показывать телефон", Location = new Point(left + labelWidth + 10, y + 30), Size = new Size(150, 25), Visible = false };
            if (!_isOwnProfile)
            {
                lblPhoneTitle.ForeColor = Color.Gray;
                lblPhone.Text = "скрыт";
                tbPhone.Visible = false;
            }
            this.Controls.Add(lblPhoneTitle);
            this.Controls.Add(lblPhone);
            this.Controls.Add(tbPhone);
            this.Controls.Add(chkShowPhone);
            y += 65;

            // Роль
            var lblRoleTitle = new Label { Text = "Роль:", Location = new Point(left, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            lblRole = new Label { Text = "", Location = new Point(left + labelWidth + 10, y), Size = new Size(fieldWidth, 25), Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            this.Controls.Add(lblRoleTitle);
            this.Controls.Add(lblRole);
            y += 35;

            // Верификация
            var lblVerifiedTitle = new Label { Text = "Верификация:", Location = new Point(left, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            lblVerified = new Label { Text = "", Location = new Point(left + labelWidth + 10, y), Size = new Size(fieldWidth, 25) };
            this.Controls.Add(lblVerifiedTitle);
            this.Controls.Add(lblVerified);
            y += 35;

            // Рейтинг
            var lblRatingTitle = new Label { Text = "Рейтинг:", Location = new Point(left, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            lblRating = new Label { Text = "", Location = new Point(left + labelWidth + 10, y), Size = new Size(fieldWidth, 25) };
            this.Controls.Add(lblRatingTitle);
            this.Controls.Add(lblRating);
            y += 35;

            // Статистика
            var lblStatsTitle = new Label { Text = "Статистика:", Location = new Point(left, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            lblStats = new Label { Text = "", Location = new Point(left + labelWidth + 10, y), Size = new Size(fieldWidth, 25), ForeColor = Color.FromArgb(80, 80, 80) };
            this.Controls.Add(lblStatsTitle);
            this.Controls.Add(lblStats);
            y += 35;

            // Описание
            var lblBioTitle = new Label { Text = "О себе:", Location = new Point(left, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            lblBio = new Label { Text = "", Location = new Point(left + labelWidth + 10, y), Size = new Size(fieldWidth, 60) };
            tbBio = new TextBox { Location = new Point(left + labelWidth + 10, y), Size = new Size(fieldWidth, 60), Multiline = true, Visible = false };
            this.Controls.Add(lblBioTitle);
            this.Controls.Add(lblBio);
            this.Controls.Add(tbBio);
            y += 75;

            // Соцсети
            var lblSocialTitle = new Label { Text = "Соцсети:", Location = new Point(left, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            lblSocial = new Label { Text = "", Location = new Point(left + labelWidth + 10, y), Size = new Size(fieldWidth, 25) };
            tbSocial = new TextBox { Location = new Point(left + labelWidth + 10, y), Size = new Size(fieldWidth, 25), Visible = false, PlaceholderText = "Telegram, WhatsApp, VK (через запятую)" };
            this.Controls.Add(lblSocialTitle);
            this.Controls.Add(lblSocial);
            this.Controls.Add(tbSocial);
            y += 45;

            // Кнопки для своего профиля
            if (_isOwnProfile)
            {
                btnEdit = new Button { Text = "Редактировать", Location = new Point(left, y), Size = new Size(130, 35), BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                btnEdit.Click += (s, e) => ToggleEditMode(true);
                this.Controls.Add(btnEdit);

                btnSave = new Button { Text = "Сохранить", Location = new Point(left + 140, y), Size = new Size(130, 35), BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Visible = false };
                btnSave.Click += BtnSave_Click;
                this.Controls.Add(btnSave);

                btnChangeEmail = new Button { Text = "Сменить почту", Location = new Point(left + 280, y), Size = new Size(130, 35), BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                btnChangeEmail.Click += (s, e) => new ChangeEmailForm().ShowDialog();
                this.Controls.Add(btnChangeEmail);
                y += 50;

                btnChangePassword = new Button { Text = "Сменить пароль", Location = new Point(left, y), Size = new Size(130, 35), BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                btnChangePassword.Click += (s, e) => new ChangePasswordForm().ShowDialog();
                this.Controls.Add(btnChangePassword);

                btnRequestModerator = new Button { Text = "Запросить роль модератора", Location = new Point(left + 140, y), Size = new Size(190, 35), BackColor = Color.FromArgb(255, 193, 7), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                btnRequestModerator.Click += BtnRequestModerator_Click;
                this.Controls.Add(btnRequestModerator);
                y += 50;

                btnVerification = new Button
                {
                    Text = "🔐 Пройти верификацию",
                    Location = new Point(left, y),
                    Size = new Size(190, 35),
                    BackColor = Color.FromArgb(0, 122, 204),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                btnVerification.Click += (s, e) =>
                {
                    using var verifyForm = new VerificationForm();
                    verifyForm.ShowDialog();
                    LoadProfile();
                };
                this.Controls.Add(btnVerification);
                y += 50;

                btnMyListings = new Button { Text = "Мои объявления", Location = new Point(left, y), Size = new Size(180, 40), BackColor = Color.FromArgb(108, 117, 125), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                btnMyListings.Click += BtnMyListings_Click;
                this.Controls.Add(btnMyListings);
                y += 55;

                btnLogout = new Button
                {
                    Text = "🚪 Выйти из аккаунта",
                    Location = new Point(left, y),
                    Size = new Size(180, 40),
                    BackColor = Color.FromArgb(108, 117, 125),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                btnLogout.Click += BtnLogout_Click;
                this.Controls.Add(btnLogout);
                y += 50;

                btnDeleteAccount = new Button
                {
                    Text = "🗑️ Удалить аккаунт",
                    Location = new Point(left, y),
                    Size = new Size(180, 40),
                    BackColor = Color.FromArgb(220, 53, 69),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                btnDeleteAccount.Click += BtnDeleteAccount_Click;
                this.Controls.Add(btnDeleteAccount);
                y += 50;
            }

            btnClose = new Button { Text = "Закрыть", Location = new Point(left + 200, y), Size = new Size(120, 40), BackColor = Color.LightGray };
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);
        }

        private async void LoadProfile()
        {

            try
            {
                var client = await SupabaseService.GetClient();
                var userId = client.Auth.CurrentUser?.Id;
                if (string.IsNullOrEmpty(userId))
                {
                    MessageBox.Show("Не удалось получить ID пользователя. Вероятно, вы не авторизованы или не инициализирован Supabase-клиент.", "Ошибка профиля", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (_isOwnProfile)
                {
                    _userId = client.Auth.CurrentUser?.Id;
                    if (string.IsNullOrEmpty(_userId))
                    {
                        MessageBox.Show("Не удалось получить ID пользователя");
                        return;
                    }
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
                    bool isVerified = profile.ContainsKey("is_verified") && profile["is_verified"]?.ToString() == "True";
                    string avatarUrl = profile.ContainsKey("avatar_url") ? profile["avatar_url"]?.ToString() : "";
                    string bio = profile.ContainsKey("bio") ? profile["bio"]?.ToString() : "";
                    string socialLinks = profile.ContainsKey("social_links") ? profile["social_links"]?.ToString() : "";
                    bool showPhone = profile.ContainsKey("show_phone") && profile["show_phone"]?.ToString() == "True";
                    double rating = profile.ContainsKey("rating") ? Convert.ToDouble(profile["rating"]) : 0;
                    int ratingCount = profile.ContainsKey("rating_count") ? Convert.ToInt32(profile["rating_count"]) : 0;

                    _currentRole = role;
                    _isVerified = isVerified;
                    _rating = rating;

                    lblName.Text = string.IsNullOrEmpty(displayName) ? "Не указано" : displayName;
                    if (_isOwnProfile)
                    {
                        lblPhone.Text = string.IsNullOrEmpty(phone) ? "Не указан" : phone;
                        tbName.Text = displayName;
                        tbPhone.Text = phone;
                        chkShowPhone.Checked = showPhone;
                    }
                    else
                    {
                        if (showPhone && !string.IsNullOrEmpty(phone))
                            lblPhone.Text = phone;
                        else
                            lblPhone.Text = "скрыт";
                    }

                    string roleText = role == "admin" ? "Администратор" : (role == "moderator" ? "Модератор" : (role == "banned" ? "Заблокирован" : "Пользователь"));
                    lblRole.Text = roleText;
                    lblVerified.Text = isVerified ? "✅ Подтверждён" : "⏳ Не подтверждён";
                    lblRating.Text = ratingCount > 0 ? $"{rating:F1} ⭐ ({ratingCount} оценок)" : "Нет оценок";
                    lblBio.Text = string.IsNullOrEmpty(bio) ? "Не указано" : bio;
                    lblSocial.Text = string.IsNullOrEmpty(socialLinks) ? "Не указаны" : socialLinks;
                    tbBio.Text = bio;
                    tbSocial.Text = socialLinks;

                    if (!string.IsNullOrEmpty(avatarUrl))
                        pbAvatar.ImageLocation = avatarUrl;

                    // Статистика
                    _totalListings = await SupabaseService.GetUserTotalListingsCount(_userId);
                    _activeListings = await SupabaseService.GetUserActiveListingsCount(_userId);
                    _foundListings = await SupabaseService.GetUserFoundListingsCount(_userId);
                    lblStats.Text = $"Всего: {_totalListings} | Активных: {_activeListings} | Найдено: {_foundListings}";

                    // Скрываем ненужные кнопки
                    if (_isOwnProfile)
                    {
                        if (role == "admin" || role == "moderator")
                            btnRequestModerator.Visible = false;
                        if (isVerified)
                            btnVerification.Visible = false;
                    }

                    if (role == "banned" && _isOwnProfile)
                    {
                        MessageBox.Show("Ваш аккаунт заблокирован.", "Доступ ограничен", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            lblName.Visible = !editing;
            lblPhone.Visible = !editing;
            lblBio.Visible = !editing;
            lblSocial.Visible = !editing;
            tbName.Visible = editing;
            tbPhone.Visible = editing;
            tbBio.Visible = editing;
            tbSocial.Visible = editing;
            chkShowPhone.Visible = editing;
            btnEdit.Visible = !editing;
            btnSave.Visible = editing;
        }

        private async void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                var updates = new
                {
                    display_name = tbName.Text,
                    phone = tbPhone.Text,
                    bio = tbBio.Text,
                    social_links = tbSocial.Text,
                    show_phone = chkShowPhone.Checked
                };
                var success = await SupabaseService.UpdateProfile(_userId, updates);
                if (success)
                {
                    lblName.Text = string.IsNullOrEmpty(tbName.Text) ? "Не указано" : tbName.Text;
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
        }

        private async void BtnDeleteAccount_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Удалить аккаунт? Все объявления будут удалены.", "Удаление", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;
            try
            {
                var serviceRoleKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imh0dXN1eHNqeHhzdWR6eHdqbnZ0Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc2NjE2NzkyNywiZXhwIjoyMDgxNzQzOTI3fQ.oERnxKvFqXnVkfK_xWcYQBvzJeqjXn4yUy_iQOpYXJI";
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("apikey", serviceRoleKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {serviceRoleKey}");
                await httpClient.DeleteAsync($"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings?user_id=eq.{_userId}");
                await httpClient.DeleteAsync($"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/profiles?user_id=eq.{_userId}");
                await httpClient.DeleteAsync($"https://htusuxsjxxsudzxwjnvt.supabase.co/auth/v1/admin/users/{_userId}");
                MessageBox.Show("Аккаунт удалён", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private async void BtnRequestModerator_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Отправить заявку на роль модератора?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;
            using var httpClient = new HttpClient();
            var requestData = new { user_id = _userId, request_type = "moderator_role", status = "pending" };
            var json = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = "https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/moderation_requests";
            httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
            var response = await httpClient.PostAsync(url, content);
            if (response.IsSuccessStatusCode)
                MessageBox.Show("Заявка отправлена", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show("Ошибка", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void BtnMyListings_Click(object sender, EventArgs e)
        {
            using var myListingsForm = new MyListingsForm(_userId);
            myListingsForm.ShowDialog();
        }
    }
}