using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using AnimalFinderDesktop.Services;

namespace AnimalFinderDesktop.Forms
{
    [System.ComponentModel.DesignerCategory("")]
    public class LoginForm : Form
    {
        private TextBox tbEmail, tbPassword;
        private Button btnLogin, btnRegister, btnCancel;
        private Label lblStatus;
        private CheckBox chkShowPassword;
        private LinkLabel llForgotPassword;

        public LoginForm()
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "AnimalFinder - Вход";
            this.Size = new Size(420, 360);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;
            InitializeControls();
        }

        private void InitializeControls()
        {
            int y = 30;
            int left = 50;
            int width = 300;

            // Заголовок
            var lblTitle = new Label
            {
                Text = "🐾 AnimalFinder",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 122, 204),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(width, 50),
                Location = new Point(left, y)
            };
            this.Controls.Add(lblTitle);
            y += 60;

            // Email
            var lblEmail = new Label { Text = "Email:", Location = new Point(left, y), Size = new Size(80, 25) };
            tbEmail = new TextBox { Location = new Point(left + 80, y), Size = new Size(220, 25) };
            this.Controls.Add(lblEmail);
            this.Controls.Add(tbEmail);
            y += 40;

            // Пароль
            var lblPassword = new Label { Text = "Пароль:", Location = new Point(left, y), Size = new Size(80, 25) };
            tbPassword = new TextBox { Location = new Point(left + 80, y), Size = new Size(220, 25), PasswordChar = '*' };
            this.Controls.Add(lblPassword);
            this.Controls.Add(tbPassword);
            y += 35;

            // Строка: Забыли пароль? слева, Показать пароль справа
            llForgotPassword = new LinkLabel
            {
                Text = "Забыли пароль?",
                LinkColor = Color.FromArgb(0, 122, 204),
                Location = new Point(left + 80, y),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 9)
            };
            llForgotPassword.Click += LlForgotPassword_Click;
            this.Controls.Add(llForgotPassword);

            chkShowPassword = new CheckBox
            {
                Text = "Показать пароль",
                Location = new Point(left + 200, y),
                Size = new Size(120, 20),
                Checked = false
            };
            chkShowPassword.CheckedChanged += (s, e) =>
            {
                tbPassword.PasswordChar = chkShowPassword.Checked ? '\0' : '*';
            };
            this.Controls.Add(chkShowPassword);
            y += 35;

            // Кнопки Войти / Регистрация
            btnLogin = new Button
            {
                Text = "Войти",
                Location = new Point(left, y),
                Size = new Size(140, 35),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnLogin.Click += BtnLogin_Click;
            this.Controls.Add(btnLogin);

            btnRegister = new Button
            {
                Text = "Регистрация",
                Location = new Point(left + 160, y),
                Size = new Size(140, 35),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnRegister.Click += BtnRegister_Click;
            this.Controls.Add(btnRegister);
            y += 50;

            // Статус
            lblStatus = new Label
            {
                Text = "",
                Location = new Point(left, y),
                Size = new Size(width, 30),
                ForeColor = Color.Red,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblStatus);

            // Кнопка Отмена
            btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(left + 100, y + 40),
                Size = new Size(100, 30),
                BackColor = Color.LightGray
            };
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);
        }

        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"=== ПОПЫТКА ВХОДА: {tbEmail.Text} ===");

            if (string.IsNullOrWhiteSpace(tbEmail.Text) || string.IsNullOrWhiteSpace(tbPassword.Text))
            {
                lblStatus.Text = "Заполните email и пароль";
                return;
            }

            btnLogin.Enabled = false;
            lblStatus.Text = "Вход...";
            lblStatus.ForeColor = Color.Blue;

            try
            {
                System.Diagnostics.Debug.WriteLine("Подключение к Supabase...");
                var client = await SupabaseService.GetClient();
                System.Diagnostics.Debug.WriteLine("Отправка запроса SignIn...");
                await client.Auth.SignIn(tbEmail.Text.Trim(), tbPassword.Text);
                System.Diagnostics.Debug.WriteLine("ВХОД УСПЕШЕН!");
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ОШИБКА ВХОДА: {ex.Message}");
                var msg = ex.Message.ToLower();
                if (msg.Contains("invalid") || msg.Contains("credentials"))
                    lblStatus.Text = "Неверный email или пароль";
                else if (msg.Contains("confirmed"))
                    lblStatus.Text = "Подтвердите email. Проверьте почту";
                else
                    lblStatus.Text = $"Ошибка: {ex.Message}";
                lblStatus.ForeColor = Color.Red;
                btnLogin.Enabled = true;
            }
        }

        private void BtnRegister_Click(object sender, EventArgs e)
        {
            using var registerForm = new RegisterForm();
            if (registerForm.ShowDialog() == DialogResult.OK)
            {
                lblStatus.Text = "Регистрация успешна! Проверьте почту.";
                lblStatus.ForeColor = Color.Green;
            }
        }

        private void LlForgotPassword_Click(object sender, EventArgs e)
        {
            using var resetForm = new ChangePasswordForm(); // неавторизованный режим
            resetForm.ShowDialog();
        }
    }

    // ========== КЛАСС REGISTERFORM ==========
    public class RegisterForm : Form
    {
        private TextBox tbEmail, tbPassword, tbConfirmPassword, tbDisplayName, tbPhone;
        private Button btnRegister, btnCancel;
        private Label lblStatus;
        private CheckBox chkShowPassword, chkShowConfirmPassword;

        public RegisterForm()
        {
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "AnimalFinder - Регистрация";
            this.Size = new Size(480, 560);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;
            InitializeControls();
        }

        private void InitializeControls()
        {
            int y = 20;
            int left = 30;
            int width = 400;

            var lblTitle = new Label
            {
                Text = "Создание аккаунта",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 122, 204),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(width, 40),
                Location = new Point(left, y)
            };
            this.Controls.Add(lblTitle);
            y += 55;

            var lblEmail = new Label { Text = "Email:*", Location = new Point(left, y), Size = new Size(100, 25) };
            tbEmail = new TextBox { Location = new Point(left + 110, y), Size = new Size(290, 25) };
            this.Controls.Add(lblEmail);
            this.Controls.Add(tbEmail);
            y += 40;

            var lblName = new Label { Text = "Как вас зовут?:", Location = new Point(left, y), Size = new Size(100, 25) };
            tbDisplayName = new TextBox { Location = new Point(left + 110, y), Size = new Size(290, 25) };
            this.Controls.Add(lblName);
            this.Controls.Add(tbDisplayName);
            y += 40;

            var lblPhone = new Label { Text = "Телефон:*", Location = new Point(left, y), Size = new Size(100, 25) };
            tbPhone = new TextBox { Location = new Point(left + 110, y), Size = new Size(290, 25), Text = "+7" };
            this.Controls.Add(lblPhone);
            this.Controls.Add(tbPhone);
            y += 40;

            var lblPassword = new Label { Text = "Пароль (мин. 6):*", Location = new Point(left, y), Size = new Size(110, 25) };
            tbPassword = new TextBox { Location = new Point(left + 110, y), Size = new Size(220, 25), PasswordChar = '*' };
            this.Controls.Add(lblPassword);
            this.Controls.Add(tbPassword);

            chkShowPassword = new CheckBox
            {
                Text = "👁",
                Location = new Point(left + 335, y),
                Size = new Size(40, 25),
                Checked = false
            };
            chkShowPassword.CheckedChanged += (s, e) => tbPassword.PasswordChar = chkShowPassword.Checked ? '\0' : '*';
            this.Controls.Add(chkShowPassword);
            y += 40;

            var lblConfirm = new Label { Text = "Повтор пароля:*", Location = new Point(left, y), Size = new Size(110, 25) };
            tbConfirmPassword = new TextBox { Location = new Point(left + 110, y), Size = new Size(220, 25), PasswordChar = '*' };
            this.Controls.Add(lblConfirm);
            this.Controls.Add(tbConfirmPassword);

            chkShowConfirmPassword = new CheckBox
            {
                Text = "👁",
                Location = new Point(left + 335, y),
                Size = new Size(40, 25),
                Checked = false
            };
            chkShowConfirmPassword.CheckedChanged += (s, e) => tbConfirmPassword.PasswordChar = chkShowConfirmPassword.Checked ? '\0' : '*';
            this.Controls.Add(chkShowConfirmPassword);
            y += 50;

            btnRegister = new Button
            {
                Text = "Зарегистрироваться",
                Location = new Point(left, y),
                Size = new Size(200, 40),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnRegister.Click += BtnRegister_Click;
            this.Controls.Add(btnRegister);

            btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(left + 220, y),
                Size = new Size(120, 40),
                BackColor = Color.LightGray
            };
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);
            y += 55;

            lblStatus = new Label
            {
                Text = "",
                Location = new Point(left, y),
                Size = new Size(width, 40),
                ForeColor = Color.Red,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblStatus);
        }

        private async void BtnRegister_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbEmail.Text))
            {
                lblStatus.Text = "Введите email";
                return;
            }
            if (string.IsNullOrWhiteSpace(tbDisplayName.Text))
            {
                lblStatus.Text = "Введите ваше имя";
                return;
            }
            if (string.IsNullOrWhiteSpace(tbPassword.Text))
            {
                lblStatus.Text = "Введите пароль";
                return;
            }
            if (tbPassword.Text != tbConfirmPassword.Text)
            {
                lblStatus.Text = "Пароли не совпадают";
                return;
            }
            if (tbPassword.Text.Length < 6)
            {
                lblStatus.Text = "Пароль должен быть не менее 6 символов";
                return;
            }

            btnRegister.Enabled = false;
            lblStatus.Text = "Регистрация...";
            lblStatus.ForeColor = Color.Blue;

            try
            {
                var client = await SupabaseService.GetClient();
                var options = new Supabase.Gotrue.SignUpOptions
                {
                    RedirectTo = "https://fenyaro224.github.io/animalfinder-confirm/confirm.html"
                };
                var response = await client.Auth.SignUp(tbEmail.Text.Trim(), tbPassword.Text, options);

                if (response?.User != null)
                {
                    bool profileCreated = await SupabaseService.InsertProfile(response.User.Id, tbDisplayName.Text.Trim(), tbPhone.Text.Trim());
                    if (!profileCreated)
                    {
                        MessageBox.Show("Пользователь создан, но не удалось сохранить дополнительные данные. Обратитесь к администратору.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    lblStatus.Text = "Письмо отправлено!";
                    lblStatus.ForeColor = Color.Green;
                    MessageBox.Show("Регистрация успешна! Проверьте почту для подтверждения.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    lblStatus.Text = "Ошибка регистрации";
                    lblStatus.ForeColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                var msg = ex.Message.ToLower();
                if (msg.Contains("already registered"))
                    lblStatus.Text = "Email уже зарегистрирован";
                else
                    lblStatus.Text = $"Ошибка: {ex.Message}";
                lblStatus.ForeColor = Color.Red;
            }
            finally
            {
                btnRegister.Enabled = true;
            }
        }
    }
}