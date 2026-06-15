using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using AnimalFinderDesktop.Services;
using Newtonsoft.Json;

namespace AnimalFinderDesktop.Forms
{
    public class SavedCredential
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public DateTime LastUsed { get; set; }
    }

    public static class CredentialManager
    {
        private static readonly string CredentialsFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AnimalFinder",
            "credentials.json");

        static CredentialManager()
        {
            var dir = Path.GetDirectoryName(CredentialsFile);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        public static List<SavedCredential> GetCredentials()
        {
            try
            {
                if (File.Exists(CredentialsFile))
                {
                    var json = File.ReadAllText(CredentialsFile);
                    return JsonConvert.DeserializeObject<List<SavedCredential>>(json) ?? new List<SavedCredential>();
                }
            }
            catch { }
            return new List<SavedCredential>();
        }

        public static void SaveCredential(string email, string password)
        {
            try
            {
                var credentials = GetCredentials();
                var existing = credentials.FirstOrDefault(c => c.Email == email);

                if (existing != null)
                {
                    existing.Password = password;
                    existing.LastUsed = DateTime.Now;
                }
                else
                {
                    credentials.Add(new SavedCredential
                    {
                        Email = email,
                        Password = password,
                        LastUsed = DateTime.Now
                    });
                }

                var json = JsonConvert.SerializeObject(credentials, Formatting.Indented);
                File.WriteAllText(CredentialsFile, json);
            }
            catch { }
        }

        public static void RemoveCredential(string email)
        {
            try
            {
                var credentials = GetCredentials();
                var toRemove = credentials.FirstOrDefault(c => c.Email == email);
                if (toRemove != null)
                {
                    credentials.Remove(toRemove);
                    var json = JsonConvert.SerializeObject(credentials, Formatting.Indented);
                    File.WriteAllText(CredentialsFile, json);
                }
            }
            catch { }
        }
    }

    [System.ComponentModel.DesignerCategory("")]
    public class LoginForm : Form
    {
        private static readonly Color PrimaryColor = Color.FromArgb(0, 122, 204);
        private static readonly Color SuccessColor = Color.FromArgb(40, 167, 69);
        private static readonly Color DangerColor = Color.FromArgb(220, 53, 69);
        private static readonly Color BackgroundColor = Color.FromArgb(248, 249, 250);
        private static readonly Color CardColor = Color.White;
        private static readonly Color TextColor = Color.FromArgb(51, 51, 51);
        private static readonly Color MutedColor = Color.FromArgb(108, 117, 125);
        private static readonly Color BorderColor = Color.FromArgb(206, 212, 218);
        private static readonly Color FocusColor = Color.FromArgb(128, 189, 255);

        private TextBox tbEmail, tbPassword;
        private Button btnLogin, btnRegister, btnExit;
        private Label lblStatus, lblTitle, lblSubtitle;
        private CheckBox chkShowPassword, chkRememberMe;
        private LinkLabel llForgotPassword;
        private Panel emailPanel, passwordPanel;
        private Panel savedCredentialsPanel;
        private List<SavedCredential> savedCredentials;

        public LoginForm()
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "AnimalFinder - Вход";
            this.Size = new Size(450, 640);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.ControlBox = true;
            this.BackColor = BackgroundColor;
            this.Font = new Font("Segoe UI", 9);
            savedCredentials = CredentialManager.GetCredentials();
            InitializeControls();
        }

        private void InitializeControls()
        {
            var cardPanel = new Panel
            {
                Location = new Point(40, 30),
                Size = new Size(370, 560),
                BackColor = CardColor,
                BorderStyle = BorderStyle.None
            };
            cardPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, cardPanel.Width - 1, cardPanel.Height - 1);
            };
            this.Controls.Add(cardPanel);

            int y = 25;
            int left = 30;
            int width = 310;

            lblTitle = new Label
            {
                Text = "🐾 AnimalFinder",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = PrimaryColor,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(width, 40),
                Location = new Point(left, y)
            };
            cardPanel.Controls.Add(lblTitle);
            y += 45;

            lblSubtitle = new Label
            {
                Text = "Войдите в свой аккаунт",
                Font = new Font("Segoe UI", 10),
                ForeColor = MutedColor,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(width, 25),
                Location = new Point(left, y)
            };
            cardPanel.Controls.Add(lblSubtitle);
            y += 40;

            emailPanel = CreateModernInputPanel(left, y, width, "📧");
            tbEmail = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Location = new Point(40, 8),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.White,
                Text = "Email",
                ForeColor = MutedColor
            };
            tbEmail.GotFocus += TbEmail_GotFocus;
            tbEmail.LostFocus += TbEmail_LostFocus;
            tbEmail.TextChanged += TbEmail_TextChanged;
            emailPanel.Controls.Add(tbEmail);
            cardPanel.Controls.Add(emailPanel);

            savedCredentialsPanel = new Panel
            {
                Location = new Point(left + 10, y + 42),
                Size = new Size(290, 0),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false,
                AutoScroll = true
            };
            cardPanel.Controls.Add(savedCredentialsPanel);

            tbEmail.LostFocus += (s, e) =>
            {
                var timer = new System.Windows.Forms.Timer { Interval = 300 };
                timer.Tick += (s2, e2) => { savedCredentialsPanel.Visible = false; timer.Stop(); };
                timer.Start();
            };

            y += 50;

            passwordPanel = CreateModernInputPanel(left, y, width, "🔒");
            tbPassword = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Location = new Point(40, 8),
                Size = new Size(220, 25),
                Font = new Font("Segoe UI", 10),
                PasswordChar = '\0',
                BackColor = Color.White,
                Text = "Пароль",
                ForeColor = MutedColor
            };
            tbPassword.GotFocus += TbPassword_GotFocus;
            tbPassword.LostFocus += TbPassword_LostFocus;
            passwordPanel.Controls.Add(tbPassword);
            cardPanel.Controls.Add(passwordPanel);
            y += 42;

            chkRememberMe = new CheckBox
            {
                Text = "✓ Запомнить меня",
                Location = new Point(left, y),
                Size = new Size(150, 20),
                ForeColor = TextColor,
                Checked = true
            };
            cardPanel.Controls.Add(chkRememberMe);

            llForgotPassword = new LinkLabel
            {
                Text = "Забыли пароль?",
                LinkColor = PrimaryColor,
                Location = new Point(left + 160, y),
                Size = new Size(150, 20),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 9)
            };
            llForgotPassword.Click += LlForgotPassword_Click;
            cardPanel.Controls.Add(llForgotPassword);

            chkShowPassword = new CheckBox
            {
                Text = "👁 Показать",
                Location = new Point(left, y + 25),
                Size = new Size(120, 20),
                ForeColor = MutedColor
            };
            chkShowPassword.CheckedChanged += (s, e) =>
            {
                tbPassword.PasswordChar = chkShowPassword.Checked ? '\0' : '*';
            };
            cardPanel.Controls.Add(chkShowPassword);
            y += 55;

            btnLogin = CreateModernButton("🔐 Войти", PrimaryColor, new Size(width, 42));
            btnLogin.Location = new Point(left, y);
            btnLogin.Click += BtnLogin_Click;
            cardPanel.Controls.Add(btnLogin);
            y += 55;

            var divider = new Label
            {
                Location = new Point(left, y),
                Size = new Size(width, 1),
                BackColor = BorderColor
            };
            cardPanel.Controls.Add(divider);

            var lblOr = new Label
            {
                Text = "или",
                Location = new Point(left + (width / 2) - 15, y - 10),
                Size = new Size(30, 20),
                BackColor = CardColor,
                ForeColor = MutedColor,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9)
            };
            cardPanel.Controls.Add(lblOr);
            y += 20;

            btnRegister = CreateModernButton("📝 Создать аккаунт", SuccessColor, new Size(width, 42));
            btnRegister.Location = new Point(left, y);
            btnRegister.Click += BtnRegister_Click;
            cardPanel.Controls.Add(btnRegister);
            y += 70;

            lblStatus = new Label
            {
                Text = "",
                Location = new Point(left, y),
                Size = new Size(width, 30),
                ForeColor = DangerColor,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9)
            };
            cardPanel.Controls.Add(lblStatus);

            btnExit = new Button
            {
                Text = "✕ Выход",
                Location = new Point(left + (width / 2) - 75, y + 40),
                Size = new Size(150, 35),
                BackColor = DangerColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnExit.FlatAppearance.BorderSize = 0;
            btnExit.Click += (s, e) => Application.Exit();
            cardPanel.Controls.Add(btnExit);

            this.KeyPreview = true;
            this.KeyDown += LoginForm_KeyDown;
        }

        private void TbEmail_TextChanged(object sender, EventArgs e)
        {
            if (tbEmail.Text != "Email" && !string.IsNullOrWhiteSpace(tbEmail.Text))
            {
                savedCredentialsPanel.Visible = false;
            }
        }

        private void TbEmail_GotFocus(object sender, EventArgs e)
        {
            if (tbEmail.Text == "Email")
            {
                tbEmail.Text = "";
                tbEmail.ForeColor = TextColor;
            }
            emailPanel.BackColor = Color.FromArgb(240, 248, 255);

            if (savedCredentials.Any() && string.IsNullOrWhiteSpace(tbEmail.Text))
            {
                savedCredentialsPanel.Controls.Clear();
                int yPos = 5;
                foreach (var cred in savedCredentials.OrderByDescending(c => c.LastUsed))
                {
                    var itemPanel = new Panel
                    {
                        Location = new Point(5, yPos),
                        Size = new Size(275, 30),
                        BackColor = Color.White,
                        Cursor = Cursors.Hand
                    };

                    var emailLabel = new Label
                    {
                        Text = $"📧 {cred.Email}",
                        Location = new Point(5, 7),
                        Size = new Size(220, 18),
                        Font = new Font("Segoe UI", 9),
                        ForeColor = TextColor,
                        Cursor = Cursors.Hand
                    };

                    var deleteBtn = new Button
                    {
                        Text = "✕",
                        Location = new Point(235, 3),
                        Size = new Size(25, 22),
                        BackColor = Color.Transparent,
                        ForeColor = DangerColor,
                        FlatStyle = FlatStyle.Flat,
                        Font = new Font("Segoe UI", 9, FontStyle.Bold),
                        Cursor = Cursors.Hand
                    };
                    deleteBtn.FlatAppearance.BorderSize = 0;
                    deleteBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 230, 230);

                    emailLabel.Click += (s, ev) => SelectCredential(cred);
                    itemPanel.Click += (s, ev) => SelectCredential(cred);
                    deleteBtn.Click += (s, ev) => DeleteCredential(cred.Email);

                    itemPanel.Controls.Add(emailLabel);
                    itemPanel.Controls.Add(deleteBtn);
                    savedCredentialsPanel.Controls.Add(itemPanel);

                    yPos += 32;
                }
                savedCredentialsPanel.Height = Math.Min(yPos + 5, 150);
                savedCredentialsPanel.Visible = true;
            }
        }

        private void SelectCredential(SavedCredential cred)
        {
            tbEmail.Text = cred.Email;
            tbEmail.ForeColor = TextColor;
            tbPassword.Text = cred.Password;
            tbPassword.ForeColor = TextColor;
            tbPassword.PasswordChar = '*';
            savedCredentialsPanel.Visible = false;
            tbPassword.Focus();
        }

        private void DeleteCredential(string email)
        {
            CredentialManager.RemoveCredential(email);
            savedCredentials = CredentialManager.GetCredentials();
            savedCredentialsPanel.Visible = false;

            lblStatus.Text = $"✓ Удалено: {email}";
            lblStatus.ForeColor = SuccessColor;
            var timer = new System.Windows.Forms.Timer { Interval = 2000 };
            timer.Tick += (s, e) => { lblStatus.Text = ""; timer.Stop(); };
            timer.Start();
        }

        private void TbEmail_LostFocus(object sender, EventArgs e)
        {
            emailPanel.BackColor = Color.White;
            if (string.IsNullOrWhiteSpace(tbEmail.Text))
            {
                tbEmail.Text = "Email";
                tbEmail.ForeColor = MutedColor;
            }
        }

        private void TbPassword_GotFocus(object sender, EventArgs e)
        {
            if (tbPassword.Text == "Пароль")
            {
                tbPassword.Text = "";
                tbPassword.ForeColor = TextColor;
                tbPassword.PasswordChar = '*';
            }
            passwordPanel.BackColor = Color.FromArgb(240, 248, 255);
        }

        private void TbPassword_LostFocus(object sender, EventArgs e)
        {
            passwordPanel.BackColor = Color.White;
            if (string.IsNullOrWhiteSpace(tbPassword.Text))
            {
                tbPassword.Text = "Пароль";
                tbPassword.ForeColor = MutedColor;
                tbPassword.PasswordChar = '\0';
            }
        }

        private Panel CreateModernInputPanel(int x, int y, int width, string icon)
        {
            var panel = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, 40),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            panel.Paint += (s, e) =>
            {
                using var pen = new Pen(panel.Focused || (panel.Controls.Count > 0 && panel.Controls[0].Focused) ? FocusColor : BorderColor, 2);
                e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
            };

            var lblIcon = new Label
            {
                Text = icon,
                Location = new Point(10, 8),
                Size = new Size(25, 25),
                Font = new Font("Segoe UI", 12)
            };
            panel.Controls.Add(lblIcon);

            return panel;
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
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, 0.15f);
            button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor, 0.15f);
            return button;
        }

        private void LoginForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.Shift && e.KeyCode == Keys.T)
            {
                FillTestCredentials();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                BtnLogin_Click(sender, e);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                Application.Exit();
                e.Handled = true;
            }
        }

        private void FillTestCredentials()
        {
            tbEmail.Text = "dosytamurza@gmail.com";
            tbPassword.Text = "123321";
            tbPassword.ForeColor = TextColor;
            tbPassword.PasswordChar = '*';
        }

        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbEmail.Text) || tbEmail.Text == "Email" ||
                string.IsNullOrWhiteSpace(tbPassword.Text) || tbPassword.Text == "Пароль")
            {
                lblStatus.Text = "⚠ Заполните email и пароль";
                lblStatus.ForeColor = DangerColor;
                return;
            }

            btnLogin.Enabled = false;
            btnLogin.Text = "⏳ Вход...";
            lblStatus.Text = "Выполняется вход...";
            lblStatus.ForeColor = PrimaryColor;

            try
            {
                var client = await SupabaseService.GetClient();
                await client.Auth.SignIn(tbEmail.Text.Trim(), tbPassword.Text);

                if (chkRememberMe.Checked)
                {
                    CredentialManager.SaveCredential(tbEmail.Text.Trim(), tbPassword.Text);
                }

                lblStatus.Text = "✓ Успешный вход!";
                lblStatus.ForeColor = SuccessColor;

                await Task.Delay(500);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                var msg = ex.Message.ToLower();
                if (msg.Contains("invalid") || msg.Contains("credentials"))
                    lblStatus.Text = "✕ Неверный email или пароль";
                else if (msg.Contains("confirmed"))
                    lblStatus.Text = "⚠ Подтвердите email. Проверьте почту";
                else
                    lblStatus.Text = $"✕ Ошибка: {ex.Message}";
                lblStatus.ForeColor = DangerColor;
                btnLogin.Enabled = true;
                btnLogin.Text = "🔐 Войти";
            }
        }

        private void BtnRegister_Click(object sender, EventArgs e)
        {
            using var registerForm = new RegisterForm();
            if (registerForm.ShowDialog() == DialogResult.OK)
            {
                lblStatus.Text = "✓ Регистрация успешна! Проверьте почту.";
                lblStatus.ForeColor = SuccessColor;
            }
        }

        private void LlForgotPassword_Click(object sender, EventArgs e)
        {
            using var resetForm = new ChangePasswordForm();
            resetForm.ShowDialog();
        }
    }

    public class RegisterForm : Form
    {
        private static readonly Color PrimaryColor = Color.FromArgb(0, 122, 204);
        private static readonly Color SuccessColor = Color.FromArgb(40, 167, 69);
        private static readonly Color DangerColor = Color.FromArgb(220, 53, 69);
        private static readonly Color WarningColor = Color.FromArgb(255, 193, 7);
        private static readonly Color BackgroundColor = Color.FromArgb(248, 249, 250);
        private static readonly Color CardColor = Color.White;
        private static readonly Color TextColor = Color.FromArgb(51, 51, 51);
        private static readonly Color MutedColor = Color.FromArgb(108, 117, 125);
        private static readonly Color BorderColor = Color.FromArgb(206, 212, 218);
        private static readonly Color FocusColor = Color.FromArgb(128, 189, 255);

        private TextBox tbEmail, tbPassword, tbConfirmPassword, tbDisplayName;
        private MaskedTextBox tbPhone;
        private Button btnRegister, btnCancel;
        private Label lblStatus, lblTitle;
        private CheckBox chkShowPassword, chkShowConfirmPassword;
        private Panel emailPanel, passwordPanel, confirmPasswordPanel, namePanel, phonePanel;
        private Panel passwordStrengthBar;
        private Label lblPasswordStrength, lblPasswordRequirements;

        public RegisterForm()
        {
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "AnimalFinder - Регистрация";
            this.Size = new Size(480, 720);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.ControlBox = true;
            this.BackColor = BackgroundColor;
            this.Font = new Font("Segoe UI", 9);
            InitializeControls();
        }

        private void InitializeControls()
        {
            var cardPanel = new Panel
            {
                Location = new Point(30, 20),
                Size = new Size(400, 670),
                BackColor = CardColor,
                BorderStyle = BorderStyle.None
            };
            cardPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, cardPanel.Width - 1, cardPanel.Height - 1);
            };
            this.Controls.Add(cardPanel);

            int y = 20;
            int left = 30;
            int width = 340;

            lblTitle = new Label
            {
                Text = "🐾 Создание аккаунта",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = PrimaryColor,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(width, 35),
                Location = new Point(left, y)
            };
            cardPanel.Controls.Add(lblTitle);
            y += 45;

            // Email
            emailPanel = CreateModernInputPanel(left, y, width, "📧");
            tbEmail = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Location = new Point(40, 8),
                Size = new Size(280, 25),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.White,
                Text = "Email",
                ForeColor = MutedColor
            };
            tbEmail.GotFocus += (s, e) => { if (tbEmail.Text == "Email") { tbEmail.Text = ""; tbEmail.ForeColor = TextColor; } emailPanel.BackColor = Color.FromArgb(240, 248, 255); };
            tbEmail.LostFocus += (s, e) => { emailPanel.BackColor = Color.White; if (string.IsNullOrWhiteSpace(tbEmail.Text)) { tbEmail.Text = "Email"; tbEmail.ForeColor = MutedColor; } };
            emailPanel.Controls.Add(tbEmail);
            cardPanel.Controls.Add(emailPanel);
            y += 50;

            // Имя
            namePanel = CreateModernInputPanel(left, y, width, "👤");
            tbDisplayName = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Location = new Point(40, 8),
                Size = new Size(280, 25),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.White,
                Text = "Ваше имя",
                ForeColor = MutedColor
            };
            tbDisplayName.GotFocus += (s, e) => { if (tbDisplayName.Text == "Ваше имя") { tbDisplayName.Text = ""; tbDisplayName.ForeColor = TextColor; } namePanel.BackColor = Color.FromArgb(240, 248, 255); };
            tbDisplayName.LostFocus += (s, e) => { namePanel.BackColor = Color.White; if (string.IsNullOrWhiteSpace(tbDisplayName.Text)) { tbDisplayName.Text = "Ваше имя"; tbDisplayName.ForeColor = MutedColor; } };
            namePanel.Controls.Add(tbDisplayName);
            cardPanel.Controls.Add(namePanel);
            y += 50;

            // Телефон
            phonePanel = CreateModernInputPanel(left, y, width, "📱");
            tbPhone = new MaskedTextBox
            {
                Mask = "+7 (000) 000-00-00",
                BorderStyle = BorderStyle.None,
                Location = new Point(40, 8),
                Size = new Size(280, 25),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.White,
                PromptChar = '_'
            };
            tbPhone.GotFocus += (s, e) => phonePanel.BackColor = Color.FromArgb(240, 248, 255);
            tbPhone.LostFocus += (s, e) => phonePanel.BackColor = Color.White;
            phonePanel.Controls.Add(tbPhone);
            cardPanel.Controls.Add(phonePanel);
            y += 50;

            // Пароль
            passwordPanel = CreateModernInputPanel(left, y, width, "🔒");
            tbPassword = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Location = new Point(40, 8),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 10),
                PasswordChar = '\0',
                BackColor = Color.White,
                Text = "Пароль",
                ForeColor = MutedColor
            };
            tbPassword.GotFocus += (s, e) => {
                if (tbPassword.Text == "Пароль")
                {
                    tbPassword.Text = "";
                    tbPassword.ForeColor = TextColor;
                }
                tbPassword.PasswordChar = '*';
                passwordPanel.BackColor = Color.FromArgb(240, 248, 255);
            };
            tbPassword.LostFocus += (s, e) => {
                passwordPanel.BackColor = Color.White;
                if (string.IsNullOrWhiteSpace(tbPassword.Text))
                {
                    tbPassword.Text = "Пароль";
                    tbPassword.ForeColor = MutedColor;
                    tbPassword.PasswordChar = '\0';
                }
            };
            tbPassword.TextChanged += (s, e) => UpdatePasswordStrength(tbPassword.Text);
            passwordPanel.Controls.Add(tbPassword);
            cardPanel.Controls.Add(passwordPanel);

            // Кнопка показа пароля
            chkShowPassword = new CheckBox
            {
                Text = "👁 Показать пароль",
                Location = new Point(left + 20, y + 45),
                Size = new Size(150, 20),
                BackColor = Color.Transparent,
                Checked = false,
                Cursor = Cursors.Hand,
                ForeColor = MutedColor
            };
            chkShowPassword.CheckedChanged += (s, e) => {
                tbPassword.PasswordChar = chkShowPassword.Checked ? '\0' : '*';
            };
            cardPanel.Controls.Add(chkShowPassword);
            y += 65;

            passwordStrengthBar = new Panel
            {
                Location = new Point(left, y),
                Size = new Size(0, 5),
                BackColor = Color.Transparent
            };
            cardPanel.Controls.Add(passwordStrengthBar);

            lblPasswordStrength = new Label
            {
                Text = "",
                Location = new Point(left + width - 120, y - 20),
                Size = new Size(120, 18),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = MutedColor,
                TextAlign = ContentAlignment.MiddleRight
            };
            cardPanel.Controls.Add(lblPasswordStrength);

            lblPasswordRequirements = new Label
            {
                Text = "💡 Минимум 6 символов, заглавная буква, цифра",
                Location = new Point(left, y + 5),
                Size = new Size(width, 18),
                Font = new Font("Segoe UI", 8),
                ForeColor = MutedColor
            };
            cardPanel.Controls.Add(lblPasswordRequirements);
            y += 35;

            // Подтверждение пароля
            confirmPasswordPanel = CreateModernInputPanel(left, y, width, "🔒");
            tbConfirmPassword = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Location = new Point(40, 8),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 10),
                PasswordChar = '\0',
                BackColor = Color.White,
                Text = "Подтвердить пароль",
                ForeColor = MutedColor
            };
            tbConfirmPassword.GotFocus += (s, e) => {
                if (tbConfirmPassword.Text == "Подтвердить пароль")
                {
                    tbConfirmPassword.Text = "";
                    tbConfirmPassword.ForeColor = TextColor;
                }
                tbConfirmPassword.PasswordChar = '*';
                confirmPasswordPanel.BackColor = Color.FromArgb(240, 248, 255);
            };
            tbConfirmPassword.LostFocus += (s, e) => {
                confirmPasswordPanel.BackColor = Color.White;
                if (string.IsNullOrWhiteSpace(tbConfirmPassword.Text))
                {
                    tbConfirmPassword.Text = "Подтвердить пароль";
                    tbConfirmPassword.ForeColor = MutedColor;
                    tbConfirmPassword.PasswordChar = '\0';
                }
            };
            confirmPasswordPanel.Controls.Add(tbConfirmPassword);
            cardPanel.Controls.Add(confirmPasswordPanel);

            // Кнопка показа подтвержденного пароля
            chkShowConfirmPassword = new CheckBox
            {
                Text = "👁 Показать пароль",
                Location = new Point(left + 20, y + 43),
                Size = new Size(150, 20),
                BackColor = Color.Transparent,
                Checked = false,
                Cursor = Cursors.Hand,
                ForeColor = MutedColor
            };
            chkShowConfirmPassword.CheckedChanged += (s, e) => {
                tbConfirmPassword.PasswordChar = chkShowConfirmPassword.Checked ? '\0' : '*';
            };
            cardPanel.Controls.Add(chkShowConfirmPassword);
            y += 65;

            btnRegister = CreateModernButton("✓ Зарегистрироваться", SuccessColor, new Size(width, 42));
            btnRegister.Location = new Point(left, y);
            btnRegister.Click += BtnRegister_Click;
            cardPanel.Controls.Add(btnRegister);
            y += 55;

            btnCancel = CreateModernButton("✕ Отмена", MutedColor, new Size(width, 38));
            btnCancel.Location = new Point(left, y);
            btnCancel.Click += (s, e) => this.Close();
            cardPanel.Controls.Add(btnCancel);
            y += 50;

            lblStatus = new Label
            {
                Text = "",
                Location = new Point(left, y),
                Size = new Size(width, 30),
                ForeColor = DangerColor,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9)
            };
            cardPanel.Controls.Add(lblStatus);

            this.Shown += (s, e) => tbEmail.Focus();
        }

        private Panel CreateModernInputPanel(int x, int y, int width, string icon)
        {
            var panel = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, 40),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            panel.Paint += (s, e) =>
            {
                using var pen = new Pen(panel.Focused || (panel.Controls.Count > 0 && panel.Controls[0].Focused) ? FocusColor : BorderColor, 2);
                e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
            };

            var lblIcon = new Label
            {
                Text = icon,
                Location = new Point(10, 8),
                Size = new Size(25, 25),
                Font = new Font("Segoe UI", 12)
            };
            panel.Controls.Add(lblIcon);

            return panel;
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
            button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, 0.15f);
            button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor, 0.15f);
            return button;
        }

        private void UpdatePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password) || password == "Пароль")
            {
                passwordStrengthBar.Size = new Size(0, 5);
                passwordStrengthBar.BackColor = Color.Transparent;
                lblPasswordStrength.Text = "";
                return;
            }

            int strength = 0;
            string strengthText = "";
            Color barColor;

            if (password.Length >= 6) strength++;
            if (password.Length >= 10) strength++;

            bool hasUpper = Regex.IsMatch(password, @"[A-ZА-Я]");
            bool hasLower = Regex.IsMatch(password, @"[a-zа-я]");
            bool hasDigit = Regex.IsMatch(password, @"\d");

            if (hasUpper && hasLower) strength++;
            if (hasDigit) strength++;

            if (strength <= 1)
            {
                barColor = DangerColor;
                strengthText = "🔴 Очень слабый";
            }
            else if (strength == 2)
            {
                barColor = WarningColor;
                strengthText = "🟠 Слабый";
            }
            else if (strength == 3)
            {
                barColor = Color.FromArgb(255, 165, 0);
                strengthText = "🟡 Средний";
            }
            else
            {
                barColor = SuccessColor;
                strengthText = "🟢 Надёжный";
            }

            int barWidth = (passwordStrengthBar.Parent.Width * strength) / 4;
            passwordStrengthBar.Size = new Size(barWidth, 5);
            passwordStrengthBar.BackColor = barColor;
            lblPasswordStrength.Text = strengthText;
            lblPasswordStrength.ForeColor = barColor;
        }

        private async void BtnRegister_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "";

            if (string.IsNullOrWhiteSpace(tbEmail.Text) || tbEmail.Text == "Email")
            {
                lblStatus.Text = "⚠ Введите email";
                lblStatus.ForeColor = DangerColor;
                emailPanel.BackColor = Color.FromArgb(255, 230, 230);
                tbEmail.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(tbDisplayName.Text) || tbDisplayName.Text == "Ваше имя")
            {
                lblStatus.Text = "⚠ Введите ваше имя";
                lblStatus.ForeColor = DangerColor;
                namePanel.BackColor = Color.FromArgb(255, 230, 230);
                tbDisplayName.Focus();
                return;
            }

            var phoneText = tbPhone.Text.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("_", "");
            if (phoneText.Length < 11)
            {
                lblStatus.Text = "⚠ Введите корректный номер телефона";
                lblStatus.ForeColor = DangerColor;
                phonePanel.BackColor = Color.FromArgb(255, 230, 230);
                tbPhone.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(tbPassword.Text) || tbPassword.Text == "Пароль")
            {
                lblStatus.Text = "⚠ Введите пароль";
                lblStatus.ForeColor = DangerColor;
                passwordPanel.BackColor = Color.FromArgb(255, 230, 230);
                tbPassword.Focus();
                return;
            }

            if (tbPassword.Text.Length < 6)
            {
                lblStatus.Text = "⚠ Пароль должен содержать минимум 6 символов";
                lblStatus.ForeColor = DangerColor;
                passwordPanel.BackColor = Color.FromArgb(255, 230, 230);
                tbPassword.Focus();
                return;
            }

            bool hasUpper = Regex.IsMatch(tbPassword.Text, @"[A-ZА-Я]");
            bool hasDigit = Regex.IsMatch(tbPassword.Text, @"\d");

            if (!hasUpper || !hasDigit)
            {
                lblStatus.Text = "⚠ Пароль должен содержать заглавную букву и цифру";
                lblStatus.ForeColor = DangerColor;
                passwordPanel.BackColor = Color.FromArgb(255, 230, 230);
                tbPassword.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(tbConfirmPassword.Text) || tbConfirmPassword.Text == "Подтвердить пароль")
            {
                lblStatus.Text = "⚠ Подтвердите пароль";
                lblStatus.ForeColor = DangerColor;
                confirmPasswordPanel.BackColor = Color.FromArgb(255, 230, 230);
                tbConfirmPassword.Focus();
                return;
            }

            if (tbPassword.Text != tbConfirmPassword.Text)
            {
                lblStatus.Text = "✕ Пароли не совпадают";
                lblStatus.ForeColor = DangerColor;
                confirmPasswordPanel.BackColor = Color.FromArgb(255, 230, 230);
                tbConfirmPassword.Focus();
                return;
            }

            btnRegister.Enabled = false;
            btnRegister.Text = "⏳ Регистрация...";
            lblStatus.Text = "Создание аккаунта...";
            lblStatus.ForeColor = PrimaryColor;

            try
            {
                var client = await SupabaseService.GetClient();
                var options = new Supabase.Gotrue.SignUpOptions
                {
                    RedirectTo = "https://fenyaro224.github.io/AnimalFinderDesktop/callback.html"
                };
                var response = await client.Auth.SignUp(tbEmail.Text.Trim(), tbPassword.Text, options);

                if (response?.User != null)
                {
                    bool profileCreated = await SupabaseService.InsertProfile(
                        response.User.Id,
                        tbDisplayName.Text.Trim(),
                        phoneText);

                    lblStatus.Text = "✓ Письмо отправлено!";
                    lblStatus.ForeColor = SuccessColor;
                    MessageBox.Show("✓ Регистрация успешна! Проверьте почту для подтверждения.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    lblStatus.Text = "✕ Ошибка регистрации";
                    lblStatus.ForeColor = DangerColor;
                }
            }
            catch (Exception ex)
            {
                var msg = ex.Message.ToLower();
                if (msg.Contains("already registered") || msg.Contains("user already exists"))
                    lblStatus.Text = "✕ Email уже зарегистрирован";
                else
                    lblStatus.Text = $"✕ Ошибка: {ex.Message}";
                lblStatus.ForeColor = DangerColor;
            }
            finally
            {
                btnRegister.Enabled = true;
                btnRegister.Text = "✓ Зарегистрироваться";
            }
        }
    }
}