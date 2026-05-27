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

        private Dictionary<string, List<string>> breedLists = new Dictionary<string, List<string>>
        {
            ["Собака"] = new List<string> { "Другая", "Лабрадор", "Немецкая овчарка", "Французский бульдог", "Йоркширский терьер", "Пудель", "Ротвейлер", "Джек-рассел-терьер", "Сиба-ину", "Хаски", "Чихуахуа", "Мопс", "Такса", "Корги", "Бигль" },
            ["Кошка"] = new List<string> { "Другая", "Британская", "Шотландская", "Мейн-кун", "Сиамская", "Персидская", "Сфинкс", "Бенгальская", "Абиссинская", "Русская голубая", "Норвежская лесная", "Рэгдолл" }
        };

        public AddListingForm()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(900, 800);
            this.MinimumSize = new Size(850, 700);
            this.Text = "Создание объявления";
            this.BackColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
        }

        private void InitializeComponent()
        {
            // Основная панель с отступами
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                AutoScroll = true,
                ColumnCount = 1,
                RowCount = 5
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));  // заголовок
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // основная информация
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // детали животного
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // контакты и фото
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 80)); // кнопки

            // Заголовок
            var lblTitle = new Label
            {
                Text = "🐾 Новое объявление",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 122, 204),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainPanel.Controls.Add(lblTitle, 0, 0);

            // ---- Блок 1: Основная информация (тип, дата, кличка) ----
            var groupBasic = new GroupBox { Text = "Основная информация", Font = new Font("Segoe UI", 10, FontStyle.Bold), Dock = DockStyle.Top, Height = 150, Padding = new Padding(10) };
            var basicLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2, Padding = new Padding(5) };
            basicLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            basicLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            basicLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            basicLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

            // Тип объявления
            basicLayout.Controls.Add(new Label { Text = "Тип объявления:", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
            cbType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cbType.Items.AddRange(new[] { "Пропал(а)", "Найден(а)" });
            cbType.SelectedIndex = 0;
            cbType.SelectedIndexChanged += (s, e) => UpdatePetNameRequirement();
            basicLayout.Controls.Add(cbType, 1, 0);

            // Дата инцидента
            basicLayout.Controls.Add(new Label { Text = "Дата пропажи/находки:", TextAlign = ContentAlignment.MiddleRight }, 2, 0);
            dtpIncidentDate = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Now };
            basicLayout.Controls.Add(dtpIncidentDate, 3, 0);

            // Кличка (со звездочкой для пропавших)
            basicLayout.Controls.Add(new Label { Text = "Кличка:", TextAlign = ContentAlignment.MiddleRight }, 0, 1);
            txtPetName = new TextBox();
            basicLayout.Controls.Add(txtPetName, 1, 1);
            // Пустая ячейка
            basicLayout.Controls.Add(new Label(), 2, 1);
            basicLayout.Controls.Add(new Label(), 3, 1);

            groupBasic.Controls.Add(basicLayout);
            mainPanel.Controls.Add(groupBasic, 0, 1);

            // ---- Блок 2: Детали животного (два столбца) ----
            var groupDetails = new GroupBox { Text = "Детали животного", Font = new Font("Segoe UI", 10, FontStyle.Bold), Dock = DockStyle.Top, Height = 320, Padding = new Padding(10) };
            var detailsLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 8, Padding = new Padding(5) };
            detailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            detailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            // Вид
            detailsLayout.Controls.Add(new Label { Text = "Вид *:", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
            cbSpecies = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cbSpecies.Items.AddRange(new[] { "Собака", "Кошка", "Грызун", "Птица", "Другое" });
            cbSpecies.SelectedIndex = 0;
            cbSpecies.SelectedIndexChanged += (s, e) => UpdateBreedList();
            detailsLayout.Controls.Add(cbSpecies, 1, 0);

            // Порода
            detailsLayout.Controls.Add(new Label { Text = "Порода:", TextAlign = ContentAlignment.MiddleRight }, 0, 1);
            var breedPanel = new Panel { Height = 30 };
            cbBreed = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
            cbBreed.Items.Add("Другая");
            cbBreed.SelectedIndex = 0;
            txtOtherBreed = new TextBox { Width = 150, Visible = false, PlaceholderText = "Укажите породу" };
            breedPanel.Controls.Add(cbBreed);
            breedPanel.Controls.Add(txtOtherBreed);
            detailsLayout.Controls.Add(breedPanel, 1, 1);

            // Окрас
            detailsLayout.Controls.Add(new Label { Text = "Окрас:", TextAlign = ContentAlignment.MiddleRight }, 0, 2);
            var colorPanel = new Panel { Height = 30 };
            cbColor = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
            cbColor.Items.AddRange(new[] { "Белый", "Чёрный", "Рыжий", "Серый", "Коричневый", "Пятнистый", "Трёхцветный", "Другое" });
            cbColor.SelectedIndex = 0;
            txtOtherColor = new TextBox { Width = 150, Visible = false, PlaceholderText = "Укажите окрас" };
            cbColor.SelectedIndexChanged += (s, e) => txtOtherColor.Visible = cbColor.SelectedItem?.ToString() == "Другое";
            colorPanel.Controls.Add(cbColor);
            colorPanel.Controls.Add(txtOtherColor);
            detailsLayout.Controls.Add(colorPanel, 1, 2);

            // Возраст
            detailsLayout.Controls.Add(new Label { Text = "Возраст:", TextAlign = ContentAlignment.MiddleRight }, 0, 3);
            var agePanel = new FlowLayoutPanel { Height = 30 };
            nudAgeYears = new NumericUpDown { Minimum = 0, Maximum = 30, Width = 60 };
            nudAgeMonths = new NumericUpDown { Minimum = 0, Maximum = 11, Width = 60 };
            agePanel.Controls.Add(nudAgeYears);
            agePanel.Controls.Add(new Label { Text = "лет" });
            agePanel.Controls.Add(nudAgeMonths);
            agePanel.Controls.Add(new Label { Text = "мес" });
            detailsLayout.Controls.Add(agePanel, 1, 3);

            // Пол
            detailsLayout.Controls.Add(new Label { Text = "Пол:", TextAlign = ContentAlignment.MiddleRight }, 0, 4);
            cbGender = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cbGender.Items.AddRange(new[] { "Мальчик", "Девочка", "Не определён" });
            cbGender.SelectedIndex = 0;
            detailsLayout.Controls.Add(cbGender, 1, 4);

            // Размер
            detailsLayout.Controls.Add(new Label { Text = "Размер:", TextAlign = ContentAlignment.MiddleRight }, 0, 5);
            cbSize = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cbSize.Items.AddRange(new[] { "Маленький", "Средний", "Большой" });
            cbSize.SelectedIndex = 0;
            detailsLayout.Controls.Add(cbSize, 1, 5);

            // Характер
            detailsLayout.Controls.Add(new Label { Text = "Характер:", TextAlign = ContentAlignment.MiddleRight }, 0, 6);
            cbTemperament = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cbTemperament.Items.AddRange(new[] { "Спокойный", "Игривый", "Активный", "Ласковый", "Пугливый", "Дружелюбный", "Независимый", "Агрессивный", "Осторожный" });
            cbTemperament.SelectedIndex = 0;
            detailsLayout.Controls.Add(cbTemperament, 1, 6);

            // Чип/клеймо
            detailsLayout.Controls.Add(new Label { Text = "Номер чипа/клейма:", TextAlign = ContentAlignment.MiddleRight }, 0, 7);
            txtMicrochip = new TextBox();
            detailsLayout.Controls.Add(txtMicrochip, 1, 7);

            groupDetails.Controls.Add(detailsLayout);
            mainPanel.Controls.Add(groupDetails, 0, 2);

            // ---- Блок 3: Местоположение, контакты, описание, фото ----
            var groupContact = new GroupBox { Text = "Контакты и описание", Font = new Font("Segoe UI", 10, FontStyle.Bold), Dock = DockStyle.Top, Height = 280, Padding = new Padding(10) };
            var contactLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 6, Padding = new Padding(5) };
            contactLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            contactLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // Местоположение
            contactLayout.Controls.Add(new Label { Text = "Местоположение:", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
            txtLocation = new TextBox();
            contactLayout.Controls.Add(txtLocation, 1, 0);

            // Радиус поиска
            contactLayout.Controls.Add(new Label { Text = "Радиус поиска (км):", TextAlign = ContentAlignment.MiddleRight }, 0, 1);
            nudSearchRadius = new NumericUpDown { Minimum = 1, Maximum = 500, Value = 10 };
            contactLayout.Controls.Add(nudSearchRadius, 1, 1);

            // Телефон
            contactLayout.Controls.Add(new Label { Text = "Телефон для звонка *:", TextAlign = ContentAlignment.MiddleRight }, 0, 2);
            txtContact = new TextBox();
            contactLayout.Controls.Add(txtContact, 1, 2);

            // Другие контакты
            contactLayout.Controls.Add(new Label { Text = "Другие способы связи:", TextAlign = ContentAlignment.MiddleRight }, 0, 3);
            txtContactOther = new TextBox { PlaceholderText = "Telegram, WhatsApp, соцсети..." };
            contactLayout.Controls.Add(txtContactOther, 1, 3);

            // Описание
            contactLayout.Controls.Add(new Label { Text = "Описание:", TextAlign = ContentAlignment.TopRight }, 0, 4);
            txtDescription = new TextBox { Multiline = true, Height = 60 };
            contactLayout.Controls.Add(txtDescription, 1, 4);

            // Фото
            contactLayout.Controls.Add(new Label { Text = "Фото:", TextAlign = ContentAlignment.TopRight }, 0, 5);
            var photoPanel = new FlowLayoutPanel { Height = 80 };
            btnChoosePhotos = new Button { Text = "Добавить фото", Width = 120 };
            btnChoosePhotos.Click += BtnChoosePhotos_Click;
            flpPhotos = new FlowLayoutPanel { Width = 400, Height = 80, AutoScroll = true, BorderStyle = BorderStyle.FixedSingle };
            photoPanel.Controls.Add(btnChoosePhotos);
            photoPanel.Controls.Add(flpPhotos);
            contactLayout.Controls.Add(photoPanel, 1, 5);

            groupContact.Controls.Add(contactLayout);
            mainPanel.Controls.Add(groupContact, 0, 3);

            // ---- Блок 4: Кнопки ----
            var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(10) };
            btnSave = new Button { Text = "Опубликовать", Width = 150, Height = 40, BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnSave.Click += BtnSave_Click;
            btnCancel = new Button { Text = "Отмена", Width = 120, Height = 40, BackColor = Color.LightGray };
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
            btnFillFromProfile = new Button { Text = "Заполнить из профиля", Width = 160, Height = 40, BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnFillFromProfile.Click += BtnFillFromProfile_Click;
            buttonPanel.Controls.Add(btnSave);
            buttonPanel.Controls.Add(btnCancel);
            buttonPanel.Controls.Add(btnFillFromProfile);
            mainPanel.Controls.Add(buttonPanel, 0, 4);

            this.Controls.Add(mainPanel);
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
            string selectedSpecies = cbSpecies.SelectedItem?.ToString();
            if (selectedSpecies == "Другое")
            {
                txtOtherSpecies.Visible = true;
                cbBreed.Visible = false;
                txtOtherBreed.Visible = false;
            }
            else
            {
                txtOtherSpecies.Visible = false;
                cbBreed.Visible = true;
                cbBreed.Items.Clear();
                if (breedLists.ContainsKey(selectedSpecies))
                {
                    cbBreed.Items.AddRange(breedLists[selectedSpecies].ToArray());
                }
                else
                {
                    cbBreed.Items.Add("Другая");
                }
                cbBreed.SelectedIndex = 0;
            }
            cbBreed.SelectedIndexChanged += (s, e) =>
            {
                txtOtherBreed.Visible = cbBreed.SelectedItem?.ToString() == "Другая";
            };
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
                    status = "pending"
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

                // ========== ЛОКАЛЬНОЕ СОХРАНЕНИЕ ФОТО ==========
                if (_photoPaths.Any() && !string.IsNullOrEmpty(insertedId))
                {
                    // Создаём папку Photos если её нет
                    string photosDir = Path.Combine(Application.StartupPath, "Photos");
                    if (!Directory.Exists(photosDir))
                        Directory.CreateDirectory(photosDir);

                    var localPhotoPaths = new List<string>();
                    foreach (var photoPath in _photoPaths)
                    {
                        string ext = Path.GetExtension(photoPath);
                        string newFileName = $"{insertedId}_{Guid.NewGuid()}{ext}";
                        string destPath = Path.Combine(photosDir, newFileName);
                        File.Copy(photoPath, destPath, true);
                        localPhotoPaths.Add($"Photos/{newFileName}");
                    }

                    // Сохраняем пути в БД (в поле photo_urls)
                    string combinedPaths = string.Join(";", localPhotoPaths);
                    var updateData = new { photo_urls = combinedPaths };
                    var updateJson = JsonConvert.SerializeObject(updateData);
                    var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");
                    var updateUrl = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings?id=eq.{insertedId}";
                    await httpClient.PatchAsync(updateUrl, updateContent);
                }
                // =============================================

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