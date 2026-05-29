using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AnimalFinderDesktop.Services;
using Newtonsoft.Json;

namespace AnimalFinderDesktop.Forms
{
    public partial class ReportDialog : Form
    {
        private string _listingId;
        private ComboBox cbReason;
        private TextBox tbComment;
        private Button btnSend, btnCancel;

        public ReportDialog(string listingId)
        {
            _listingId = listingId;
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Пожаловаться на объявление";
            this.Size = new System.Drawing.Size(400, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
        }

        private void InitializeComponent()
        {
            int y = 20;
            int left = 20;
            int width = 340;

            var lblReason = new Label { Text = "Причина жалобы:", Location = new System.Drawing.Point(left, y), Size = new System.Drawing.Size(100, 25) };
            cbReason = new ComboBox { Location = new System.Drawing.Point(left + 110, y), Size = new System.Drawing.Size(230, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cbReason.Items.AddRange(new[] { "Спам", "Оскорбительное содержание", "Недостоверная информация", "Другое" });
            cbReason.SelectedIndex = 0;
            this.Controls.Add(lblReason);
            this.Controls.Add(cbReason);
            y += 40;

            var lblComment = new Label { Text = "Комментарий:", Location = new System.Drawing.Point(left, y), Size = new System.Drawing.Size(100, 25) };
            tbComment = new TextBox { Location = new System.Drawing.Point(left + 110, y), Size = new System.Drawing.Size(230, 60), Multiline = true };
            this.Controls.Add(lblComment);
            this.Controls.Add(tbComment);
            y += 80;

            btnSend = new Button { Text = "Отправить", Location = new System.Drawing.Point(left, y), Size = new System.Drawing.Size(120, 30), BackColor = System.Drawing.Color.FromArgb(40, 167, 69), ForeColor = System.Drawing.Color.White, FlatStyle = FlatStyle.Flat };
            btnSend.Click += BtnSend_Click;
            btnCancel = new Button { Text = "Отмена", Location = new System.Drawing.Point(left + 140, y), Size = new System.Drawing.Size(100, 30), BackColor = System.Drawing.Color.LightGray };
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnSend);
            this.Controls.Add(btnCancel);
        }

        private async void BtnSend_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(cbReason.Text))
            {
                MessageBox.Show("Выберите причину жалобы.");
                return;
            }
            btnSend.Enabled = false;
            try
            {
                var client = await SupabaseService.GetClient();
                var userId = client.Auth.CurrentUser?.Id;
                var data = new
                {
                    listing_id = _listingId,
                    user_id = userId,
                    reason = cbReason.Text,
                    comment = tbComment.Text,
                    status = "pending"
                };
                using var httpClient = new HttpClient();
                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = "https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/reports";
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                var response = await httpClient.PostAsync(url, content);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Жалоба отправлена. Модератор рассмотрит её.");
                    this.Close();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Ошибка: {error}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
            finally { btnSend.Enabled = true; }
        }
    }
}