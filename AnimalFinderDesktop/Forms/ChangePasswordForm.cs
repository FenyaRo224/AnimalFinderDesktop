using System;
using System.Windows.Forms;
using AnimalFinderDesktop.Services;

namespace AnimalFinderDesktop.Forms
{
    public partial class ChangePasswordForm : Form
    {
        private TextBox tbOldPass, tbNewPass, tbConfirmPass;
        private Button btnSubmit, btnCancel;
        private Label lblStatus;

        public ChangePasswordForm()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Смена пароля";
            this.Size = new Size(400, 280);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
        }

        private void InitializeComponent()
        {
            int y = 20;
            int left = 20;
            int width = 340;

            var lblOld = new Label { Text = "Старый пароль:", Location = new Point(left, y), Size = new Size(100, 25) };
            tbOldPass = new TextBox { Location = new Point(left + 110, y), Size = new Size(230, 25), PasswordChar = '*' };
            y += 35;

            var lblNew = new Label { Text = "Новый пароль:", Location = new Point(left, y), Size = new Size(100, 25) };
            tbNewPass = new TextBox { Location = new Point(left + 110, y), Size = new Size(230, 25), PasswordChar = '*' };
            y += 35;

            var lblConfirm = new Label { Text = "Подтвердите:", Location = new Point(left, y), Size = new Size(100, 25) };
            tbConfirmPass = new TextBox { Location = new Point(left + 110, y), Size = new Size(230, 25), PasswordChar = '*' };
            y += 45;

            btnSubmit = new Button { Text = "Сменить", Location = new Point(left, y), Size = new Size(100, 30), BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White };
            btnSubmit.Click += BtnSubmit_Click;
            btnCancel = new Button { Text = "Отмена", Location = new Point(left + 120, y), Size = new Size(100, 30) };
            btnCancel.Click += (s, e) => this.Close();
            lblStatus = new Label { Text = "", Location = new Point(left, y + 40), Size = new Size(width, 25), ForeColor = Color.Red };

            this.Controls.Add(lblOld);
            this.Controls.Add(tbOldPass);
            this.Controls.Add(lblNew);
            this.Controls.Add(tbNewPass);
            this.Controls.Add(lblConfirm);
            this.Controls.Add(tbConfirmPass);
            this.Controls.Add(btnSubmit);
            this.Controls.Add(btnCancel);
            this.Controls.Add(lblStatus);
        }

        private async void BtnSubmit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbOldPass.Text) || string.IsNullOrEmpty(tbNewPass.Text) || tbNewPass.Text != tbConfirmPass.Text)
            {
                lblStatus.Text = "Проверьте правильность заполнения";
                return;
            }
            if (tbNewPass.Text.Length < 6)
            {
                lblStatus.Text = "Новый пароль должен быть не менее 6 символов";
                return;
            }
            btnSubmit.Enabled = false;
            var client = await SupabaseService.GetClient();
            var userId = client.Auth.CurrentUser?.Id;
            var success = await SupabaseService.ChangePassword(userId, tbOldPass.Text, tbNewPass.Text);
            if (success)
            {
                MessageBox.Show("Пароль изменён. Пожалуйста, войдите заново.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await client.Auth.SignOut();
                Application.Restart();
            }
            else
            {
                lblStatus.Text = "Неверный старый пароль";
                btnSubmit.Enabled = true;
            }
        }
    }
}