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
    public class AddListingForm : Form
    {
        private ComboBox cbType, cbSpecies, cbBreed, cbColor, cbGender, cbSize, cbTemperament;
        private TextBox txtPetName, txtOtherSpecies, txtOtherBreed, txtOtherColor, txtLocation, txtContact, txtContactOther, txtMicrochip, txtSpecialMarks, txtDescription;
        private NumericUpDown nudAgeYears, nudAgeMonths, nudSearchRadius;
        private DateTimePicker dtpIncidentDate;
        private Button btnChoosePhotos, btnSave, btnCancel, btnFillFromProfile;
        private FlowLayoutPanel flpPhotos;
        private List<string> _photoPaths = new List<string>();

        // Поля для верификации
        private CheckBox chkVerify;
        private TextBox txtDocComment;
        private string selectedDocPath = null;

        private Dictionary<string, List<string>> breedLists = new Dictionary<string, List<string>>
        {
            ["Собака"] = new List<string> { "Другая", "Лабрадор", "Немецкая овчарка", "Французский бульдог", "Йоркширский терьер", "Пудель", "Ротвейлер", "Джек-рассел-терьер", "Сиба-ину", "Хаски", "Чихуахуа", "Мопс", "Такса", "Корги", "Бигль" },
            ["Кошка"] = new List<string> { "Другая", "Британская", "Шотландская", "Мейн-кун", "Сиамская", "Персидская", "Сфинкс", "Бенгальская", "Абиссинская", "Русская голубая", "Норвежская лесная", "Рэгдолл" }
        };

        public AddListingForm()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(850, 700); // Было 1020, теперь 700
            this.MinimumSize = new Size(850, 600);
            this.AutoScroll = true; 
        }

        private void InitializeComponent()
        {
            this.Text = "Создание объявления";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;

            int y = 20;
            int left = 20;
            int labelWidth = 160;
            int fieldWidth = 580;

            var lblType = new Label { Text = "Тип объявления:", Location = new Point(left, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            cbType = new ComboBox { Location = new Point(left + labelWidth + 10, y), Size = new Size(fieldWidth, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cbType.Items.AddRange(new[] { "Пропал(а)", "Найден(а)" });
            cbType.SelectedIndex = 0;
            cbType.SelectedIndexChanged += (s, e) => UpdatePetNameRequirement();
            this.Controls.Add(lblType);
            this.Controls.Add(cbType);
            y += 40;

            var lblPetName = new Label { Text = "Кличка:", Location = new Point(left, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            txtPetName = new TextBox { Location = new Point(left + labelWidth + 10, y), Size = new Size(fieldWidth, 25) };
            this.Controls.Add(lblPetName);
            this.Controls.Add(txtPetName);
            y += 40;

            var lblIncidentDate = new Label { Text = "Дата пропажи/находки:", Location = new Point(left, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            dtpIncidentDate = new DateTimePicker { Location = new Point(left + labelWidth + 10, y), Size = new Size(200, 25), Format = DateTimePickerFormat.Short };
            dtpIncidentDate.Value = DateTime.Now;
            this.Controls.Add(lblIncidentDate);
            this.Controls.Add(dtpIncidentDate);
            y += 40;

            var lblSpecies = new Label { Text = "Вид *:", Location = new Point(left, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            cbSpecies = new ComboBox { Location = new Point(left + labelWidth + 10, y), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cbSpecies.Items.AddRange(new[] { "Собака", "Кошка", "Грызун", "Птица", "Другое" });
            cbSpecies.SelectedIndex = 0;
            cbSpecies.SelectedIndexChanged += (s, e) => UpdateBreedList();
            txtOtherSpecies = new TextBox { Location = new Point(left + labelWidth + 230, y), Size = new Size(350, 25), Visible = false, PlaceholderText = "Укажите вид" };
            this.Controls.Add(lblSpecies);
            this.Controls.Add(cbSpecies);
            this.Controls.Add(txtOtherSpecies);
            y += 40;

            var lblBreed = new Label { Text = "Порода:", Location = new Point(left, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            cbBreed = new ComboBox { Location = new Point(left + labelWidth + 10, y), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cbBreed.Items.Add("Другая");
            cbBreed.SelectedIndex = 0;
            txtOtherBreed = new TextBox { Location = new Point(left + labelWidth + 230, y), Size = new Size(350, 25), Visible = false, PlaceholderText = "Укажите породу" };
            this.Controls.Add(lblBreed);
            this.Controls.Add(cbBreed);
            this.Controls.Add(txtOtherBreed);
            y += 40;

            var lblAge = new Label { Text = "Возраст:", Location = new Point(left, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            nudAgeYears = new NumericUpDown { Location = new Point(left + labelWidth + 10, y), Size = new Size(80, 25), Minimum = 0, Maximum = 30, Value = 0 };
            var lblYears = new Label { Text = "лет", Location = new Point(left + labelWidth + 100, y), Size = new Size(30, 25), TextAlign = ContentAlignment.MiddleLeft };
            nudAgeMonths = new NumericUpDown { Location = new Point(left + labelWidth + 140, y), Size = new Size(80, 25), Minimum = 0, Maximum = 11, Value = 0 };
            var lblMonths = new Label { Text = "мес", Location = new Point(left + labelWidth + 230, y), Size = new Size(40, 25), TextAlign = ContentAlignment.MiddleLeft };
            this.Controls.Add(lblAge);
            this.Controls.Add(nudAgeYears);
            this.Controls.Add(lblYears);
            this.Controls.Add(nudAgeMonths);
            this.Controls.Add(lblMonths);
            y += 40;

            var lblGender = new Label { Text = "Пол:", Location = new Point(left, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            cbGender = new ComboBox { Location = new Point(left + labelWidth + 10, y), Size = new Size(fieldWidth, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cbGender.Items.AddRange(new[] { "Мальчик", "Девочка", "Не определён" });
            cbGender.SelectedIndex = 0;
            this.Controls.Add(lblGender);
            this.Controls.Add(cbGender);
            y += 40;

            var lblSize = new Label { Text = "Размер:", Location = new Point(left, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            cbSize = new ComboBox { Location = new Point(left + labelWidth + 10, y), Size = new Size(fieldWidth, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cbSize.Items.AddRange(new[] { "Маленький", "Средний", "Большой" });
            cbSize.SelectedIndex = 0;
            this.Controls.Add(lblSize);
            this.Controls.Add(cbSize);
            y += 40;

            var lblColor = new Label { Text = "Окрас:", Location = new Point(left, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            cbColor = new ComboBox { Location = new Point(left + labelWidth + 10, y), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cbColor.Items.AddRange(new[] { "Белый", "Чёрный", "Рыжий", "Серый", "Коричневый", "Пятнистый", "Трёхцветный", "Другое" });
            cbColor.SelectedIndex = 0;
            txtOtherColor = new TextBox { Location = new Point(left + labelWidth + 230, y), Size = new Size(350, 25), Visible = false, PlaceholderText = "Укажите окрас" };
            cbColor.SelectedIndexChanged += (s, e) => { txtOtherColor.Visible = cbColor.SelectedItem?.ToString() == "Другое"; };
            this.Controls.Add(lblColor);
            this.Controls.Add(cbColor);
            this.Controls.Add(txtOtherColor);
            y += 40;

            var lblTemperament = new Label { Text = "Характер:", Location = new Point(left, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            cbTemperament = new ComboBox { Location = new Point(left + labelWidth + 10, y), Size = new Size(fieldWidth, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cbTemperament.Items.AddRange(new[] { "Спокойный", "Игривый", "Активный", "Ласковый", "Пугливый", "Дружелюбный", "Независимый", "Агрессивный", "Осторожный" });
            cbTemperament.SelectedIndex = 0;
            this.Controls.Add(lblTemperament);
            this.Controls.Add(cbTemperament);
            y += 40;

            var lblLocation = new Label { Text = "Местоположение:", Location = new Point(left, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            txtLocation = new TextBox { Location = new Point(left + labelWidth + 10, y), Size = new Size(fieldWidth, 25) };
            this.Controls.Add(lblLocation);
            this.Controls.Add(txtLocation);
            y += 40;

            var lblRadius = new Label { Text = "Радиус поиска (км):", Location = new Point(left, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            nudSearchRadius = new NumericUpDown { Location = new Point(left + labelWidth + 10, y), Size = new Size(100, 25), Minimum = 1, Maximum = 500, Value = 10 };
            this.Controls.Add(lblRadius);
            this.Controls.Add(nudSearchRadius);
            y += 40;

            var lblMicrochip = new Label { Text = "Номер чипа / клейма:", Location = new Point(left, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            txtMicrochip = new TextBox { Location = new Point(left + labelWidth + 10, y), Size = new Size(fieldWidth, 25) };
            this.Controls.Add(lblMicrochip);
            this.Controls.Add(txtMicrochip);
            y += 40;

            var lblSpecialMarks = new Label { Text = "Особые приметы:", Location = new Point(left, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            txtSpecialMarks = new TextBox { Location = new Point(left + labelWidth + 10, y), Size = new Size(fieldWidth, 25) };
            this.Controls.Add(lblSpecialMarks);
            this.Controls.Add(txtSpecialMarks);
            y += 40;

            var lblPhone = new Label { Text = "Телефон для звонка *:", Location = new Point(left, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            txtContact = new TextBox { Location = new Point(left + labelWidth + 10, y), Size = new Size(fieldWidth, 25) };
            this.Controls.Add(lblPhone);
            this.Controls.Add(txtContact);

            var lblContactOther = new Label { Text = "Другие способы связи:", Location = new Point(left, y + 35), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            txtContactOther = new TextBox { Location = new Point(left + labelWidth + 10, y + 35), Size = new Size(fieldWidth, 25), PlaceholderText = "Telegram, WhatsApp, соцсети..." };
            this.Controls.Add(lblContactOther);
            this.Controls.Add(txtContactOther);
            y += 80;

            var lblDesc = new Label { Text = "Описание:", Location = new Point(left, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.TopRight };
            txtDescription = new TextBox { Location = new Point(left + labelWidth + 10, y), Size = new Size(fieldWidth, 80), Multiline = true, ScrollBars = ScrollBars.Vertical };
            this.Controls.Add(lblDesc);
            this.Controls.Add(txtDescription);
            y += 100;

            var lblPhotos = new Label { Text = "Фото:", Location = new Point(left, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.TopRight };
            btnChoosePhotos = new Button { Text = "Добавить фото", Location = new Point(left + labelWidth + 10, y), Size = new Size(120, 30) };
            btnChoosePhotos.Click += BtnChoosePhotos_Click;
            flpPhotos = new FlowLayoutPanel { Location = new Point(left + labelWidth + 10, y + 35), Size = new Size(fieldWidth, 100), AutoScroll = true, BorderStyle = BorderStyle.FixedSingle };
            this.Controls.Add(lblPhotos);
            this.Controls.Add(btnChoosePhotos);
            this.Controls.Add(flpPhotos);
            y += 155;

            // ========== БЛОК ВЕРИФИКАЦИИ (опционально) ==========
            var lblVerification = new Label
            {
                Text = "Подтверждение владельца (опционально):",
                Location = new Point(left, y),
                Size = new Size(labelWidth, 25),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 167, 69)
            };
            this.Controls.Add(lblVerification);
            y += 40;

            chkVerify = new CheckBox
            {
                Text = "Подтвердить, что животное принадлежит мне",
                Location = new Point(left + labelWidth + 10, y),
                Size = new Size(300, 25),
                Checked = false
            };
            this.Controls.Add(chkVerify);

            var verifyPanel = new Panel
            {
                Location = new Point(left + labelWidth + 10, y + 30),
                Size = new Size(fieldWidth, 80),
                Visible = false
            };

            var lblDoc = new Label { Text = "Загрузите документ (ветпаспорт, совместное фото):", Location = new Point(0, 0), Size = new Size(300, 20) };
            var btnChooseDoc = new Button { Text = "Выбрать файл", Location = new Point(0, 25), Size = new Size(120, 30) };
            var lblDocFile = new Label { Text = "Файл не выбран", Location = new Point(130, 30), Size = new Size(300, 20), ForeColor = Color.Gray };
            var lblDocComment = new Label { Text = "Комментарий:", Location = new Point(0, 60), Size = new Size(80, 20) };
            txtDocComment = new TextBox { Location = new Point(90, 58), Size = new Size(300, 25), PlaceholderText = "Необязательно" };

            verifyPanel.Controls.Add(lblDoc);
            verifyPanel.Controls.Add(btnChooseDoc);
            verifyPanel.Controls.Add(lblDocFile);
            verifyPanel.Controls.Add(lblDocComment);
            verifyPanel.Controls.Add(txtDocComment);
            this.Controls.Add(verifyPanel);

            btnChooseDoc.Click += (s, ev) =>
            {
                using var ofd = new OpenFileDialog();
                ofd.Filter = "Изображения|*.jpg;*.jpeg;*.png|PDF|*.pdf";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    selectedDocPath = ofd.FileName;
                    lblDocFile.Text = Path.GetFileName(selectedDocPath);
                    lblDocFile.ForeColor = Color.Green;
                }
            };

            chkVerify.CheckedChanged += (s, ev) => verifyPanel.Visible = chkVerify.Checked;
            y += 80;
            // ========== КОНЕЦ БЛОКА ВЕРИФИКАЦИИ ==========

            btnFillFromProfile = new Button { Text = "Заполнить из профиля", Location = new Point(left + 100, y), Size = new Size(160, 40), BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnFillFromProfile.Click += BtnFillFromProfile_Click;
            this.Controls.Add(btnFillFromProfile);

            btnSave = new Button { Text = "Опубликовать", Location = new Point(left + 280, y), Size = new Size(150, 40), BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            btnCancel = new Button { Text = "Отмена", Location = new Point(left + 450, y), Size = new Size(120, 40), BackColor = Color.LightGray };
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
            this.Controls.Add(btnCancel);

            UpdatePetNameRequirement();
            UpdateBreedList();
        }

        private void UpdatePetNameRequirement()
        {
            bool isRequired = cbType.SelectedItem?.ToString() == "Пропал(а)";
            var lbl = this.Controls.OfType<Label>().FirstOrDefault(l => l.Text == "Кличка:");
            if (lbl != null) lbl.Text = isRequired ? "Кличка *:" : "Кличка:";
        }

        private void UpdateBreedList()
        {
            if (cbSpecies == null || cbBreed == null) return;
            string selectedSpecies = cbSpecies.SelectedItem?.ToString();
            if (selectedSpecies == "Другое")
            {
                if (txtOtherSpecies != null) txtOtherSpecies.Visible = true;
                if (cbBreed != null) cbBreed.Visible = false;
                if (txtOtherBreed != null) txtOtherBreed.Visible = false;
            }
            else
            {
                if (txtOtherSpecies != null) txtOtherSpecies.Visible = false;
                if (cbBreed != null) cbBreed.Visible = true;
                cbBreed.Items.Clear();
                if (breedLists.ContainsKey(selectedSpecies))
                    cbBreed.Items.AddRange(breedLists[selectedSpecies].ToArray());
                else
                    cbBreed.Items.Add("Другая");
                cbBreed.SelectedIndex = 0;
            }
            cbBreed.SelectedIndexChanged -= OnBreedChanged;
            cbBreed.SelectedIndexChanged += OnBreedChanged;
        }

        private void OnBreedChanged(object sender, EventArgs e)
        {
            if (txtOtherBreed != null)
                txtOtherBreed.Visible = cbBreed.SelectedItem?.ToString() == "Другая";
        }

        private async void BtnFillFromProfile_Click(object sender, EventArgs e)
        {
            try
            {
                var client = await SupabaseService.GetClient();
                var userId = client.Auth.CurrentUser?.Id;
                var profile = await SupabaseService.GetProfile(userId);
                if (profile != null)
                {
                    string phone = profile.ContainsKey("phone") ? profile["phone"]?.ToString() : "";
                    string social = profile.ContainsKey("social_links") ? profile["social_links"]?.ToString() : "";
                    if (!string.IsNullOrEmpty(phone)) txtContact.Text = phone;
                    if (!string.IsNullOrEmpty(social)) txtContactOther.Text = social;
                    else MessageBox.Show("В профиле не указаны контакты", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Профиль не найден", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void BtnChoosePhotos_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog();
            ofd.Multiselect = true;
            ofd.Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                foreach (var file in ofd.FileNames)
                {
                    _photoPaths.Add(file);
                    var pb = new PictureBox { Width = 80, Height = 80, SizeMode = PictureBoxSizeMode.Zoom, Image = Image.FromFile(file), Margin = new Padding(3) };
                    flpPhotos.Controls.Add(pb);
                }
            }
        }

        private async void BtnSave_Click(object sender, EventArgs e)
        {
            if (cbType.SelectedItem?.ToString() == "Пропал(а)" && string.IsNullOrWhiteSpace(txtPetName.Text))
            {
                MessageBox.Show("Для пропавшего животного укажите кличку.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtContact.Text))
            {
                MessageBox.Show("Введите телефон для звонка.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string species = cbSpecies.SelectedItem?.ToString();
            if (species == "Другое" && string.IsNullOrWhiteSpace(txtOtherSpecies.Text))
            {
                MessageBox.Show("Укажите вид животного.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnSave.Enabled = false;
            btnSave.Text = "Сохранение...";

            try
            {
                var client = await SupabaseService.GetClient();
                var userId = client.Auth.CurrentUser?.Id;

                string listingType = cbType.SelectedIndex == 0 ? "lost" : "found";
                string actualSpecies = (species == "Другое") ? txtOtherSpecies.Text.Trim() : species;
                string breed = "";
                if (cbBreed.Visible)
                {
                    breed = cbBreed.SelectedItem?.ToString();
                    if (breed == "Другая") breed = txtOtherBreed.Text.Trim();
                }
                else if (txtOtherBreed.Visible)
                {
                    breed = txtOtherBreed.Text.Trim();
                }
                string color = cbColor.SelectedItem?.ToString();
                if (color == "Другое") color = txtOtherColor.Text.Trim();
                string temperament = cbTemperament.SelectedItem?.ToString();
                int ageYears = (int)nudAgeYears.Value;
                int ageMonths = (int)nudAgeMonths.Value;
                int? totalMonths = null;
                if (ageYears > 0 || ageMonths > 0)
                    totalMonths = ageYears * 12 + ageMonths;

                string gender = cbGender.SelectedIndex == 0 ? "male" : (cbGender.SelectedIndex == 1 ? "female" : "unknown");
                string size = cbSize.SelectedItem?.ToString() switch
                {
                    "Маленький" => "small",
                    "Средний" => "medium",
                    "Большой" => "large",
                    _ => "medium"
                };
                int searchRadius = (int)nudSearchRadius.Value;
                DateTime incidentDate = dtpIncidentDate.Value.ToUniversalTime();

                var newListing = new
                {
                    id = Guid.NewGuid().ToString(),
                    listing_type = listingType,
                    pet_name = txtPetName.Text.Trim(),
                    species = actualSpecies,
                    breed = string.IsNullOrEmpty(breed) ? null : breed,
                    age = totalMonths,
                    gender = gender,
                    size = size,
                    color = string.IsNullOrEmpty(color) ? null : color,
                    temperament = temperament,
                    location = string.IsNullOrEmpty(txtLocation.Text) ? null : txtLocation.Text.Trim(),
                    contact = string.IsNullOrEmpty(txtContactOther.Text) ? null : txtContactOther.Text.Trim(),
                    contact_phone = txtContact.Text.Trim(),
                    microchip = string.IsNullOrEmpty(txtMicrochip.Text) ? null : txtMicrochip.Text.Trim(),
                    special_marks = string.IsNullOrEmpty(txtSpecialMarks.Text) ? null : txtSpecialMarks.Text.Trim(),
                    description = string.IsNullOrEmpty(txtDescription.Text) ? null : txtDescription.Text.Trim(),
                    search_radius = searchRadius,
                    incident_date = incidentDate,
                    user_id = userId,
                    created_at = DateTime.UtcNow,
                    status = "on_moderation"
                };

                using var httpClient = new HttpClient();
                var json = JsonConvert.SerializeObject(newListing);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = "https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings";
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");
                var response = await httpClient.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode) throw new Exception($"Ошибка сервера: {responseBody}");

                var inserted = JsonConvert.DeserializeObject<List<dynamic>>(responseBody);
                string insertedId = inserted?[0]?.id;

                // Локальное сохранение фото
                if (_photoPaths.Any() && !string.IsNullOrEmpty(insertedId))
                {
                    string photosDir = Path.Combine(Application.StartupPath, "Photos");
                    if (!Directory.Exists(photosDir)) Directory.CreateDirectory(photosDir);
                    var localPaths = new List<string>();
                    foreach (var photoPath in _photoPaths)
                    {
                        string ext = Path.GetExtension(photoPath);
                        string newName = $"{insertedId}_{Guid.NewGuid()}{ext}";
                        string destPath = Path.Combine(photosDir, newName);
                        File.Copy(photoPath, destPath, true);
                        localPaths.Add($"Photos/{newName}");
                    }
                    string combined = string.Join(";", localPaths);
                    var updateData = new { photo_urls = combined };
                    var updateJson = JsonConvert.SerializeObject(updateData);
                    var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");
                    var updateUrl = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings?id=eq.{insertedId}";
                    await httpClient.PatchAsync(updateUrl, updateContent);
                }

                // СОХРАНЕНИЕ ЗАЯВКИ НА ВЕРИФИКАЦИЮ (если пользователь выбрал)
                if (chkVerify != null && chkVerify.Checked && !string.IsNullOrEmpty(selectedDocPath))
                {
                    string docsDir = Path.Combine(Application.StartupPath, "VerificationDocs");
                    if (!Directory.Exists(docsDir)) Directory.CreateDirectory(docsDir);

                    string ext = Path.GetExtension(selectedDocPath);
                    string newName = $"{insertedId}_verif_{Guid.NewGuid()}{ext}";
                    string destPath = Path.Combine(docsDir, newName);
                    File.Copy(selectedDocPath, destPath, true);
                    string relativePath = $"VerificationDocs/{newName}";

                    var verifyData = new
                    {
                        user_id = userId,
                        pet_listing_id = insertedId,
                        request_type = "animal_verification",
                        microchip = txtMicrochip.Text.Trim(),
                        document_url = relativePath,
                        comment = txtDocComment != null ? txtDocComment.Text.Trim() : "",
                        status = "pending",
                        created_at = DateTime.UtcNow
                    };
                    var verifyJson = JsonConvert.SerializeObject(verifyData);
                    var verifyContent = new StringContent(verifyJson, Encoding.UTF8, "application/json");
                    var verifyUrl = "https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/verification_requests";
                    await httpClient.PostAsync(verifyUrl, verifyContent);
                }

                MessageBox.Show("Объявление отправлено на модерацию.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSave.Enabled = true;
                btnSave.Text = "Опубликовать";
            }
        }
    }
}