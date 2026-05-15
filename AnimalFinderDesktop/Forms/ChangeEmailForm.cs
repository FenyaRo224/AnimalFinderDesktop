using System;
using System.Windows.Forms;
using AnimalFinderDesktop.Services;

namespace AnimalFinderDesktop.Forms
{
    public partial class ChangeEmailForm : Form
    {
        private TextBox tbNewEmail, tbPassword;
        private Button btnSubmit, btnCancel;
        private Label lblStatus;

        public ChangeEmailForm()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Смена email";
            this.Size = new Size(400, 220);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
        }

        private void InitializeComponent()
        {
            int y = 20;
            int left = 20;
            int width = 340;

            var lblNew = new Label { Text = "Новый email:", Location = new Point(left, y), Size = new Size(100, 25) };
            tbNewEmail = new TextBox { Location = new Point(left + 110, y), Size = new Size(230, 25) };
            y += 35;

            var lblPass = new Label { Text = "Пароль:", Location = new Point(left, y), Size = new Size(100, 25) };
            tbPassword = new TextBox { Location = new Point(left + 110, y), Size = new Size(230, 25), PasswordChar = '*' };
            y += 45;

            btnSubmit = new Button { Text = "Сменить", Location = new Point(left, y), Size = new Size(100, 30), BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White };
            btnSubmit.Click += BtnSubmit_Click;
            btnCancel = new Button { Text = "Отмена", Location = new Point(left + 120, y), Size = new Size(100, 30) };
            btnCancel.Click += (s, e) => this.Close();
            lblStatus = new Label { Text = "", Location = new Point(left, y + 40), Size = new Size(width, 25), ForeColor = Color.Red };

            this.Controls.Add(lblNew);
            this.Controls.Add(tbNewEmail);
            this.Controls.Add(lblPass);
            this.Controls.Add(tbPassword);
            this.Controls.Add(btnSubmit);
            this.Controls.Add(btnCancel);
            this.Controls.Add(lblStatus);
        }

        private async void BtnSubmit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbNewEmail.Text) || string.IsNullOrEmpty(tbPassword.Text))
            {
                lblStatus.Text = "Заполните все поля";
                return;
            }
            btnSubmit.Enabled = false;
            var client = await SupabaseService.GetClient();
            var userId = client.Auth.CurrentUser?.Id;
            var success = await SupabaseService.ChangeEmail(userId, tbNewEmail.Text, tbPassword.Text);
            if (success)
            {
                MessageBox.Show("Письмо с подтверждением отправлено на новый email. После подтверждения войдите заново.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            else
            {
                lblStatus.Text = "Неверный пароль или ошибка";
                btnSubmit.Enabled = true;
            }
        }
    }
}