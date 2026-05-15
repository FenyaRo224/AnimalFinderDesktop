using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using AnimalFinderDesktop.Services;

namespace AnimalFinderDesktop.Forms
{
    public class VerificationForm : Form
    {
        private Label lblTitle, lblInstruction, lblPetName, lblMicrochip, lblDocuments, lblComment;
        private ComboBox cbPetName;
        private TextBox txtMicrochip, txtComment;
        private Button btnChooseDocument, btnSubmit, btnCancel;
        private PictureBox pbDocumentPreview;
        private string _documentPath = "";
        private List<dynamic> _userListings;
        private string _userId;

        public VerificationForm()
        {
            this.Text = "Верификация пользователя";
            this.Size = new Size(600, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            InitializeComponent();
            LoadUserListings();
        }

        private void InitializeComponent()
        {
            int y = 30;
            int left = 30;
            int width = 520;

            // Заголовок
            lblTitle = new Label
            {
                Text = "Верификация пользователя",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 122, 204),
                Location = new Point(left, y),
                Size = new Size(width, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblTitle);
            y += 55;

            // Инструкция
            lblInstruction = new Label
            {
                Text = "Верификация позволяет подтвердить вашу личность и право владения животным.\n" +
                       "После верификации ваши объявления будут публиковаться без премодерации,\n" +
                       "а в профиле появится значок ✅ 'Проверенный пользователь'.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(80, 80, 80),
                Location = new Point(left, y),
                Size = new Size(width, 60),
                TextAlign = ContentAlignment.TopLeft
            };
            this.Controls.Add(lblInstruction);
            y += 75;

            // Выбор питомца
            var lblPet = new Label
            {
                Text = "Выберите питомца для верификации:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(left, y),
                Size = new Size(width, 25)
            };
            this.Controls.Add(lblPet);
            y += 30;

            cbPetName = new ComboBox
            {
                Location = new Point(left, y),
                Size = new Size(width, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10)
            };
            cbPetName.Items.Add("Добавить нового питомца...");
            cbPetName.SelectedIndexChanged += (s, e) =>
            {
                if (cbPetName.SelectedItem?.ToString() == "Добавить нового питомца...")
                {
                    // Открыть форму создания объявления для нового питомца
                    using var addForm = new AddListingForm();
                    if (addForm.ShowDialog() == DialogResult.OK)
                    {
                        LoadUserListings();
                    }
                }
            };
            this.Controls.Add(cbPetName);
            y += 45;

            // Номер чипа
            var lblChip = new Label
            {
                Text = "Номер чипа / клейма:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(left, y),
                Size = new Size(width, 25)
            };
            this.Controls.Add(lblChip);
            y += 30;

            txtMicrochip = new TextBox
            {
                Location = new Point(left, y),
                Size = new Size(width, 30),
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "Введите номер чипа (если есть)"
            };
            this.Controls.Add(txtMicrochip);
            y += 45;

            // Загрузка документа
            var lblDoc = new Label
            {
                Text = "Загрузите документ (ветпаспорт, фото чипа, паспорт владельца):",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(left, y),
                Size = new Size(width, 25)
            };
            this.Controls.Add(lblDoc);
            y += 30;

            btnChooseDocument = new Button
            {
                Text = "Выбрать файл",
                Location = new Point(left, y),
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnChooseDocument.Click += BtnChooseDocument_Click;
            this.Controls.Add(btnChooseDocument);
            y += 40;

            pbDocumentPreview = new PictureBox
            {
                Location = new Point(left, y),
                Size = new Size(120, 80),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(240, 242, 245),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(pbDocumentPreview);
            y += 95;

            // Комментарий
            var lblComm = new Label
            {
                Text = "Комментарий к заявке:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(left, y),
                Size = new Size(width, 25)
            };
            this.Controls.Add(lblComm);
            y += 30;

            txtComment = new TextBox
            {
                Location = new Point(left, y),
                Size = new Size(width, 80),
                Multiline = true,
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "Дополнительная информация для модератора..."
            };
            this.Controls.Add(txtComment);
            y += 95;

            // Кнопки
            btnSubmit = new Button
            {
                Text = "Отправить заявку",
                Location = new Point(left, y),
                Size = new Size(200, 40),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnSubmit.Click += BtnSubmit_Click;
            this.Controls.Add(btnSubmit);

            btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(left + 220, y),
                Size = new Size(120, 40),
                BackColor = Color.LightGray
            };
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);
        }

        private async void LoadUserListings()
        {
            try
            {
                var client = await SupabaseService.GetClient();
                _userId = client.Auth.CurrentUser?.Id;
                using var httpClient = new HttpClient();
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings?user_id=eq.{_userId}&select=id,pet_name,species";
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                var response = await httpClient.GetStringAsync(url);
                _userListings = JsonConvert.DeserializeObject<List<dynamic>>(response) ?? new();

                cbPetName.Items.Clear();
                cbPetName.Items.Add("Добавить нового питомца...");
                foreach (var listing in _userListings)
                {
                    string name = listing.pet_name?.ToString() ?? "Без имени";
                    string species = listing.species?.ToString() ?? "";
                    cbPetName.Items.Add($"{name} ({species})");
                }
                if (cbPetName.Items.Count > 1)
                    cbPetName.SelectedIndex = 1;
            }
            catch { }
        }

        private void BtnChooseDocument_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog();
            ofd.Filter = "Изображения|*.jpg;*.jpeg;*.png;*.pdf";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                _documentPath = ofd.FileName;
                if (ofd.FileName.EndsWith(".pdf"))
                {
                    pbDocumentPreview.Image = null;
                    pbDocumentPreview.BackColor = Color.FromArgb(255, 248, 225);
                    var lblPdf = new Label
                    {
                        Text = "PDF файл",
                        Location = new Point(30, 30),
                        Font = new Font("Segoe UI", 8),
                        ForeColor = Color.Gray
                    };
                    pbDocumentPreview.Controls.Clear();
                    pbDocumentPreview.Controls.Add(lblPdf);
                }
                else
                {
                    pbDocumentPreview.Image = Image.FromFile(_documentPath);
                    pbDocumentPreview.Controls.Clear();
                }
            }
        }

        private async void BtnSubmit_Click(object sender, EventArgs e)
        {
            if (cbPetName.SelectedIndex <= 0)
            {
                MessageBox.Show("Выберите питомца для верификации", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrEmpty(_documentPath))
            {
                MessageBox.Show("Загрузите документ для верификации", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnSubmit.Enabled = false;
            btnSubmit.Text = "Отправка...";

            try
            {
                string documentUrl = null;
                // Загружаем документ в Storage
                if (!string.IsNullOrEmpty(_documentPath) && File.Exists(_documentPath))
                {
                    var fileName = $"verification/{_userId}_{Guid.NewGuid()}{Path.GetExtension(_documentPath)}";
                    var fileBytes = File.ReadAllBytes(_documentPath);
                    using var storageClient = new HttpClient();
                    storageClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                    storageClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                    var storageUrl = $"https://htusuxsjxxsudzxwjnvt.supabase.co/storage/v1/object/verification/{fileName}";
                    var byteContent = new ByteArrayContent(fileBytes);
                    byteContent.Headers.Add("Content-Type", GetContentType(_documentPath));
                    var storageResponse = await storageClient.PostAsync(storageUrl, byteContent);
                    if (storageResponse.IsSuccessStatusCode)
                    {
                        documentUrl = $"https://htusuxsjxxsudzxwjnvt.supabase.co/storage/v1/object/public/verification/{fileName}";
                    }
                }

                // Создаём заявку на верификацию
                using var httpClient = new HttpClient();
                var requestData = new
                {
                    user_id = _userId,
                    request_type = "verification",
                    status = "pending",
                    pet_listing_id = _userListings[cbPetName.SelectedIndex - 1]?.id?.ToString(),
                    microchip = txtMicrochip.Text.Trim(),
                    document_url = documentUrl,
                    comment = txtComment.Text.Trim()
                };
                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = "https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/verification_requests";
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                var response = await httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show(
                        "Заявка на верификацию отправлена!\n\n" +
                        "Модератор рассмотрит её в ближайшее время.\n" +
                        "После подтверждения в вашем профиле появится значок верификации.",
                        "Успех",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Ошибка при отправке заявки: {error}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSubmit.Enabled = true;
                btnSubmit.Text = "Отправить заявку";
            }
        }

        private string GetContentType(string path)
        {
            var ext = Path.GetExtension(path).ToLower();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
        }
    }
}