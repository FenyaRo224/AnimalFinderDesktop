using System;
using System.Drawing;
using System.Windows.Forms;
using AnimalFinderDesktop.Services;

namespace AnimalFinderDesktop.Forms
{
    public partial class ChangePasswordForm : Form
    {
        private TextBox tbEmail;
        private Button btnSendResetLink, btnCancel;
        private Label lblStatus, lblInstruction;
        private bool _isAuthenticated;

        // Конструктор для НЕАВТОРИЗОВАННЫХ пользователей (с формы входа)
        public ChangePasswordForm()
        {
            _isAuthenticated = false;
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Восстановление пароля";
            this.Size = new Size(450, 280);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
        }

        // Конструктор для АВТОРИЗОВАННЫХ пользователей (из профиля)
        public ChangePasswordForm(string currentUserEmail)
        {
            _isAuthenticated = !string.IsNullOrEmpty(currentUserEmail);
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Смена пароля";
            this.Size = new Size(450, _isAuthenticated ? 200 : 280);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
        }

        private void InitializeComponent()
        {
            int y = 20;
            int left = 20;
            int width = 400;

            // Инструкция
            string instructionText = _isAuthenticated
                ? "Ссылка для сброса пароля будет отправлена на ваш email.\n" +
                  "Перейдите по ссылке в письме и задайте новый пароль."
                : "Введите email, указанный при регистрации.\n" +
                  "На него придёт ссылка для сброса пароля.";

            lblInstruction = new Label
            {
                Text = instructionText,
                Location = new Point(left, y),
                Size = new Size(width, 45),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblInstruction);
            y += 60;

            // Поле email (только для неавторизованных)
            if (!_isAuthenticated)
            {
                var lblEmail = new Label { Text = "Email:", Location = new Point(left, y), Size = new Size(80, 25) };
                tbEmail = new TextBox { Location = new Point(left + 85, y), Size = new Size(310, 25) };
                this.Controls.Add(lblEmail);
                this.Controls.Add(tbEmail);
                y += 45;
            }

            // Кнопка отправки
            btnSendResetLink = new Button
            {
                Text = "Отправить ссылку на почту",
                Location = new Point(left + 50, y),
                Size = new Size(300, 40),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnSendResetLink.Click += BtnSendResetLink_Click;
            this.Controls.Add(btnSendResetLink);
            y += 55;

            btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(left + 150, y),
                Size = new Size(100, 30),
                BackColor = Color.LightGray
            };
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);
            y += 45;

            lblStatus = new Label
            {
                Text = "",
                Location = new Point(left, y),
                Size = new Size(width, 25),
                ForeColor = Color.Red,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblStatus);
        }

        private async void BtnSendResetLink_Click(object sender, EventArgs e)
        {
            btnSendResetLink.Enabled = false;
            lblStatus.Text = "Отправка...";
            lblStatus.ForeColor = Color.Blue;

            try
            {
                if (_isAuthenticated)
                {
                    // Авторизованный пользователь
                    var success = await SupabaseService.RequestPasswordReset();
                    if (success)
                    {
                        lblStatus.Text = "Ссылка отправлена! Проверьте почту.";
                        lblStatus.ForeColor = Color.Green;
                        var timer = new System.Windows.Forms.Timer { Interval = 3000 };
                        timer.Tick += (s, ev) => { timer.Stop(); this.Close(); };
                        timer.Start();
                    }
                    else
                    {
                        lblStatus.Text = "Ошибка при отправке";
                        lblStatus.ForeColor = Color.Red;
                        btnSendResetLink.Enabled = true;
                    }
                }
                else
                {
                    // Неавторизованный пользователь
                    string email = tbEmail.Text.Trim();
                    if (string.IsNullOrEmpty(email) || !email.Contains("@"))
                    {
                        lblStatus.Text = "Введите корректный email";
                        lblStatus.ForeColor = Color.Red;
                        btnSendResetLink.Enabled = true;
                        return;
                    }

                    var success = await SupabaseService.ResetPasswordForEmail(email);
                    if (success)
                    {
                        lblStatus.Text = "Ссылка отправлена! Проверьте почту.";
                        lblStatus.ForeColor = Color.Green;
                        var timer = new System.Windows.Forms.Timer { Interval = 3000 };
                        timer.Tick += (s, ev) => { timer.Stop(); this.Close(); };
                        timer.Start();
                    }
                    else
                    {
                        lblStatus.Text = "Ошибка при отправке";
                        lblStatus.ForeColor = Color.Red;
                        btnSendResetLink.Enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Ошибка: {ex.Message}";
                lblStatus.ForeColor = Color.Red;
                btnSendResetLink.Enabled = true;
            }
        }
    }
}