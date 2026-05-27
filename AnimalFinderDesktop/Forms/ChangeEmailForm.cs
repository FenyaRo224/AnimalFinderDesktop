using System;
using System.Drawing;
using System.Windows.Forms;
using AnimalFinderDesktop.Services;

namespace AnimalFinderDesktop.Forms
{
    public partial class ChangeEmailForm : Form
    {
        private TextBox tbNewEmail;
        private Button btnSend, btnCancel;
        private Label lblStatus;

        public ChangeEmailForm()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Смена email";
            this.Size = new Size(450, 220);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
        }

        private void InitializeComponent()
        {
            int y = 20;
            int left = 20;
            int width = 400;

            var lblInstruction = new Label
            {
                Text = "Введите новый email. На него придёт письмо с подтверждением.\n" +
                       "Перейдите по ссылке в письме, чтобы завершить смену.",
                Location = new Point(left, y),
                Size = new Size(width, 45),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblInstruction);
            y += 55;

            var lblNew = new Label { Text = "Новый email:", Location = new Point(left, y), Size = new Size(90, 25) };
            tbNewEmail = new TextBox { Location = new Point(left + 95, y), Size = new Size(290, 25) };
            this.Controls.Add(lblNew);
            this.Controls.Add(tbNewEmail);
            y += 45;

            btnSend = new Button
            {
                Text = "Отправить подтверждение",
                Location = new Point(left + 50, y),
                Size = new Size(300, 40),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnSend.Click += BtnSend_Click;
            this.Controls.Add(btnSend);
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

        private async void BtnSend_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbNewEmail.Text) || !tbNewEmail.Text.Contains("@"))
            {
                lblStatus.Text = "Введите корректный email";
                return;
            }

            btnSend.Enabled = false;
            lblStatus.Text = "Отправка...";
            lblStatus.ForeColor = Color.Blue;

            var success = await SupabaseService.RequestEmailChange(tbNewEmail.Text.Trim());

            if (success)
            {
                lblStatus.Text = "Письмо отправлено! Проверьте почту.";
                lblStatus.ForeColor = Color.Green;
                var timer = new System.Windows.Forms.Timer { Interval = 3000 };
                timer.Tick += (s, ev) => { timer.Stop(); this.Close(); };
                timer.Start();
            }
            else
            {
                lblStatus.Text = "Ошибка при отправке";
                lblStatus.ForeColor = Color.Red;
                btnSend.Enabled = true;
            }
        }
    }
}