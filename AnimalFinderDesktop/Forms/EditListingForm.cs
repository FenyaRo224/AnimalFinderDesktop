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
    public class EditListingForm : Form
    {
        private Panel contentPanel;
        private Dictionary<string, object> _listing;

        private static readonly Color PrimaryColor = Color.FromArgb(0, 122, 204);
        private static readonly Color SuccessColor = Color.FromArgb(40, 167, 69);
        private static readonly Color DangerColor = Color.FromArgb(220, 53, 69);
        private static readonly Color WarningColor = Color.FromArgb(255, 193, 7);
        private static readonly Color BackgroundColor = Color.FromArgb(245, 247, 250);
        private static readonly Color CardColor = Color.White;
        private static readonly Color TextColor = Color.FromArgb(51, 51, 51);
        private static readonly Color MutedColor = Color.FromArgb(108, 117, 125);
        private static readonly Color BorderColor = Color.FromArgb(222, 226, 230);

        private ComboBox cbSpecies, cbBreed, cbColor, cbGender, cbSize, cbTemperament, cbSubBreed;
        private ComboBox cbStatus;
        private TextBox txtPetName, txtOtherSpecies, txtOtherBreed, txtOtherColor, txtLocation, txtContactOther, txtMicrochip, txtSpecialMarks, txtStatusDescription;
        private MaskedTextBox txtContact;
        private NumericUpDown nudAgeYears, nudAgeMonths, nudSearchRadius;
        private DateTimePicker dtpIncidentDate;
        private Button btnSave, btnCancel, btnSelectOnMap;

        private double? _latitude = null;
        private double? _longitude = null;
        private string _currentUserId;
        private string _currentUserRole = "user";

        private Dictionary<string, List<string>> breedLists = new Dictionary<string, List<string>>
        {
            ["Собака"] = new List<string> { "Другая", "Лабрадор", "Немецкая овчарка", "Французский бульдог", "Йоркширский терьер", "Пудель", "Ротвейлер", "Джек-рассел-терьер", "Сиба-ину", "Хаски", "Чихуахуа", "Мопс", "Такса", "Корги", "Бигль" },
            ["Кошка"] = new List<string> { "Другая", "Британская", "Шотландская", "Мейн-кун", "Сиамская", "Персидская", "Сфинкс", "Бенгальская", "Абиссинская", "Русская голубая", "Норвежская лесная", "Рэгдолл" },
            ["Грызун"] = new List<string> { "Другой", "Хомяк", "Крыса", "Морская свинка", "Кролик", "Шиншилла", "Песчанка", "Декоративная мышь", "Белка", "Суслик" },
            ["Птица"] = new List<string> { "Другая", "Попугай", "Канарейка", "Воробей", "Голубь", "Ворона", "Ара", "Корелла", "Жако", "Нимфа", "Волнистый попугайчик" }
        };

        public EditListingForm(Dictionary<string, object> listing)
        {
            _listing = listing;
            InitializeComponent();
            LoadListingData();
            _ = LoadCurrentUserRole();

            // Проверяем может ли пользователь редактировать
            string listingAuthorId = GetField("user_id");
            bool isOwner = listingAuthorId == _currentUserId;
            bool isModerator = _currentUserRole == "moderator" || _currentUserRole == "admin";

            if (!isOwner && !isModerator)
            {
                MessageBox.Show("❌ У вас нет прав на редактирование этого объявления", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return;
            }

            // Если модератор редактирует чужое объявление - показываем предупреждение
            if (!isOwner && isModerator)
            {
                this.Text = "AnimalFinder - Редактирование (Режим модератора)";
            }

            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(950, 1050);
            this.MinimumSize = new Size(950, 1050);
            this.BackColor = BackgroundColor;
            this.Font = new Font("Segoe UI", 9);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.ControlBox = true;
            this.Text = "AnimalFinder - Редактирование объявления";
        }

        private async Task LoadCurrentUserRole()
        {
            try
            {
                var client = await SupabaseService.GetClient();
                _currentUserId = client.Auth.CurrentUser?.Id;

                using var httpClient = new HttpClient();
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/profiles?user_id=eq.{_currentUserId}&select=role";
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                var response = await httpClient.GetStringAsync(url);
                var profiles = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response);
                if (profiles != null && profiles.Count > 0 && profiles[0].ContainsKey("role"))
                {
                    _currentUserRole = profiles[0]["role"].ToString();
                }
            }
            catch { _currentUserRole = "user"; }
        }

        private void InitializeComponent()
        {
            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = BackgroundColor
            };

            contentPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(880, 1400),
                BackColor = BackgroundColor,
                AutoSize = false
            };

            int y = 30;
            int leftMargin = 40;
            int labelWidth = 160;
            int fieldWidth = 300;

            // Заголовок
            var lblTitle = new Label
            {
                Text = "✏️ Редактирование объявления",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = PrimaryColor,
                Location = new Point(leftMargin, y),
                AutoSize = true
            };
            contentPanel.Controls.Add(lblTitle);
            y += 55;

            // СЕКЦИЯ 1: Состояние поиска
            y = AddSectionHeader(contentPanel, y, leftMargin, "🔍 Состояние поиска");
            y += 15;

            var statusLabel = new Label
            {
                Text = "Статус поиска:",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 30),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            contentPanel.Controls.Add(statusLabel);

            cbStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = fieldWidth,
                Font = new Font("Segoe UI", 9),
                Location = new Point(leftMargin + labelWidth + 10, y)
            };
            cbStatus.Items.AddRange(new[] { "🟢 Активный поиск", "✅ Животное найдено!", "❌ Закрыт без результата" });
            cbStatus.SelectedIndex = 0;
            contentPanel.Controls.Add(cbStatus);
            y += 40;

            var statusDescLabel = new Label
            {
                Text = "Описание состояния:",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 30),
                TextAlign = ContentAlignment.TopRight,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            contentPanel.Controls.Add(statusDescLabel);

            txtStatusDescription = new TextBox
            {
                Location = new Point(leftMargin + labelWidth + 10, y),
                Size = new Size(550, 80),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                PlaceholderText = "Опишите текущее состояние поиска: где последний раз видели, какие действия предпринимаются...",
                Font = new Font("Segoe UI", 9)
            };
            contentPanel.Controls.Add(txtStatusDescription);
            y += 100;

            // СЕКЦИЯ 2: Основная информация
            y = AddSectionHeader(contentPanel, y, leftMargin, "Основная информация");
            y += 15;

            var nameLabel = new Label
            {
                Text = "Кличка:",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 30),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 9)
            };
            contentPanel.Controls.Add(nameLabel);

            txtPetName = new TextBox
            {
                Width = fieldWidth,
                PlaceholderText = "Введите кличку",
                Font = new Font("Segoe UI", 9),
                Location = new Point(leftMargin + labelWidth + 10, y)
            };
            contentPanel.Controls.Add(txtPetName);
            y += 40;

            var dateLabel = new Label
            {
                Text = "Дата пропажи/находки:",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 30),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 9)
            };
            contentPanel.Controls.Add(dateLabel);

            dtpIncidentDate = new DateTimePicker
            {
                Width = 220,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Now,
                Font = new Font("Segoe UI", 9),
                Location = new Point(leftMargin + labelWidth + 10, y)
            };
            contentPanel.Controls.Add(dtpIncidentDate);
            y += 40;

            // СЕКЦИЯ 3: Характеристики животного
            y = AddSectionHeader(contentPanel, y, leftMargin, "Характеристики животного");
            y += 15;

            var speciesLabel = new Label
            {
                Text = "Вид:",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 30),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 9)
            };
            contentPanel.Controls.Add(speciesLabel);

            cbSpecies = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 200,
                Font = new Font("Segoe UI", 9),
                Location = new Point(leftMargin + labelWidth + 10, y)
            };
            cbSpecies.Items.AddRange(new[] { "Собака", "Кошка", "Грызун", "Птица", "Другое" });
            cbSpecies.SelectedIndex = 0;
            cbSpecies.SelectedIndexChanged += (s, e) => UpdateBreedList();
            contentPanel.Controls.Add(cbSpecies);

            txtOtherSpecies = new TextBox
            {
                Width = 250,
                PlaceholderText = "Укажите вид",
                Font = new Font("Segoe UI", 9),
                Visible = false,
                Location = new Point(leftMargin + labelWidth + 220, y)
            };
            contentPanel.Controls.Add(txtOtherSpecies);
            y += 40;

            var breedLabel = new Label
            {
                Text = "Порода:",
                Name = "lblBreed",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 30),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 9)
            };
            contentPanel.Controls.Add(breedLabel);

            cbBreed = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 200,
                Font = new Font("Segoe UI", 9),
                Location = new Point(leftMargin + labelWidth + 10, y)
            };
            cbBreed.Items.Add("Другая");
            cbBreed.SelectedIndex = 0;
            cbBreed.SelectedIndexChanged += OnBreedChanged;
            contentPanel.Controls.Add(cbBreed);

            cbSubBreed = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 200,
                Font = new Font("Segoe UI", 9),
                Location = new Point(leftMargin + labelWidth + 10, y),
                Visible = false
            };
            cbSubBreed.SelectedIndexChanged += OnBreedChanged;
            contentPanel.Controls.Add(cbSubBreed);

            txtOtherBreed = new TextBox
            {
                Width = 250,
                PlaceholderText = "Укажите породу",
                Font = new Font("Segoe UI", 9),
                Visible = false,
                Location = new Point(leftMargin + labelWidth + 220, y)
            };
            contentPanel.Controls.Add(txtOtherBreed);
            y += 40;

            var ageLabel = new Label
            {
                Text = "Возраст:",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 30),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 9)
            };
            contentPanel.Controls.Add(ageLabel);

            nudAgeYears = new NumericUpDown
            {
                Width = 60,
                Minimum = 0,
                Maximum = 30,
                Value = 0,
                Font = new Font("Segoe UI", 9),
                Location = new Point(leftMargin + labelWidth + 10, y)
            };
            contentPanel.Controls.Add(nudAgeYears);

            var lblYears = new Label
            {
                Text = "лет",
                Location = new Point(leftMargin + labelWidth + 75, y + 7),
                AutoSize = true,
                Font = new Font("Segoe UI", 9)
            };
            contentPanel.Controls.Add(lblYears);

            nudAgeMonths = new NumericUpDown
            {
                Width = 60,
                Minimum = 0,
                Maximum = 11,
                Value = 0,
                Font = new Font("Segoe UI", 9),
                Location = new Point(leftMargin + labelWidth + 115, y)
            };
            contentPanel.Controls.Add(nudAgeMonths);

            var lblMonths = new Label
            {
                Text = "мес",
                Location = new Point(leftMargin + labelWidth + 180, y + 7),
                AutoSize = true,
                Font = new Font("Segoe UI", 9)
            };
            contentPanel.Controls.Add(lblMonths);
            y += 40;

            var genderLabel = new Label
            {
                Text = "Пол:",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 30),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 9)
            };
            contentPanel.Controls.Add(genderLabel);

            cbGender = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 180,
                Font = new Font("Segoe UI", 9),
                Location = new Point(leftMargin + labelWidth + 10, y)
            };
            cbGender.Items.AddRange(new[] { "Мальчик", "Девочка", "Не определён" });
            cbGender.SelectedIndex = 0;
            contentPanel.Controls.Add(cbGender);

            var sizeLabel = new Label
            {
                Text = "Размер:",
                Location = new Point(leftMargin + 420, y),
                Size = new Size(80, 30),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 9)
            };
            contentPanel.Controls.Add(sizeLabel);

            cbSize = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 180,
                Font = new Font("Segoe UI", 9),
                Location = new Point(leftMargin + 510, y)
            };
            cbSize.Items.AddRange(new[] { "Маленький", "Средний", "Большой" });
            cbSize.SelectedIndex = 0;
            contentPanel.Controls.Add(cbSize);
            y += 40;

            var colorLabel = new Label
            {
                Text = "Окрас:",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 30),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 9)
            };
            contentPanel.Controls.Add(colorLabel);

            cbColor = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 200,
                Font = new Font("Segoe UI", 9),
                Location = new Point(leftMargin + labelWidth + 10, y)
            };
            cbColor.Items.AddRange(new[] { "Белый", "Чёрный", "Рыжий", "Серый", "Коричневый", "Пятнистый", "Трёхцветный", "Другое" });
            cbColor.SelectedIndex = 0;
            cbColor.SelectedIndexChanged += (s, e) => { txtOtherColor.Visible = cbColor.SelectedItem?.ToString() == "Другое"; };
            contentPanel.Controls.Add(cbColor);

            txtOtherColor = new TextBox
            {
                Width = 250,
                PlaceholderText = "Укажите окрас",
                Font = new Font("Segoe UI", 9),
                Visible = false,
                Location = new Point(leftMargin + labelWidth + 220, y)
            };
            contentPanel.Controls.Add(txtOtherColor);
            y += 40;

            var tempLabel = new Label
            {
                Text = "Характер:",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 30),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 9)
            };
            contentPanel.Controls.Add(tempLabel);

            cbTemperament = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 550,
                Font = new Font("Segoe UI", 9),
                Location = new Point(leftMargin + labelWidth + 10, y)
            };
            cbTemperament.Items.AddRange(new[] { "Спокойный", "Игривый", "Активный", "Ласковый", "Пугливый", "Дружелюбный", "Независимый", "Агрессивный", "Осторожный" });
            cbTemperament.SelectedIndex = 0;
            contentPanel.Controls.Add(cbTemperament);
            y += 50;

            // СЕКЦИЯ 4: Местоположение
            y = AddSectionHeader(contentPanel, y, leftMargin, "Местоположение и поиск");
            y += 15;

            var locLabel = new Label
            {
                Text = "Местоположение:",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 30),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 9)
            };
            contentPanel.Controls.Add(locLabel);

            txtLocation = new TextBox
            {
                Width = 400,
                PlaceholderText = "Адрес или описание места",
                Font = new Font("Segoe UI", 9),
                Location = new Point(leftMargin + labelWidth + 10, y)
            };
            contentPanel.Controls.Add(txtLocation);

            btnSelectOnMap = CreateStyledButton("🗺️ Карта", PrimaryColor, new Size(100, 28));
            btnSelectOnMap.Location = new Point(leftMargin + labelWidth + 420, y);
            btnSelectOnMap.Click += BtnSelectOnMap_Click;
            contentPanel.Controls.Add(btnSelectOnMap);
            y += 40;

            var radiusLabel = new Label
            {
                Text = "Радиус поиска (км):",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 30),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 9)
            };
            contentPanel.Controls.Add(radiusLabel);

            nudSearchRadius = new NumericUpDown
            {
                Width = 100,
                Minimum = 1,
                Maximum = 500,
                Value = 10,
                Font = new Font("Segoe UI", 9),
                Location = new Point(leftMargin + labelWidth + 10, y)
            };
            contentPanel.Controls.Add(nudSearchRadius);
            y += 50;

            // СЕКЦИЯ 5: Дополнительная информация
            y = AddSectionHeader(contentPanel, y, leftMargin, "Дополнительная информация");
            y += 15;

            var chipLabel = new Label
            {
                Text = "Номер чипа / клейма:",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 30),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 9)
            };
            contentPanel.Controls.Add(chipLabel);

            txtMicrochip = new TextBox
            {
                Width = 550,
                PlaceholderText = "Введите номер",
                Font = new Font("Segoe UI", 9),
                Location = new Point(leftMargin + labelWidth + 10, y)
            };
            contentPanel.Controls.Add(txtMicrochip);
            y += 40;

            var marksLabel = new Label
            {
                Text = "Особые приметы:",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 30),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 9)
            };
            contentPanel.Controls.Add(marksLabel);

            txtSpecialMarks = new TextBox
            {
                Width = 550,
                PlaceholderText = "Шрамы, ошейник, особенности",
                Font = new Font("Segoe UI", 9),
                Location = new Point(leftMargin + labelWidth + 10, y)
            };
            contentPanel.Controls.Add(txtSpecialMarks);
            y += 50;

            // СЕКЦИЯ 6: Контакты
            y = AddSectionHeader(contentPanel, y, leftMargin, "Контактная информация");
            y += 15;

            var phoneLabel = new Label
            {
                Text = "Телефон для звонка:",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 30),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 9)
            };
            contentPanel.Controls.Add(phoneLabel);

            txtContact = new MaskedTextBox
            {
                Mask = "+7 (000) 000-00-00",
                Width = 550,
                Font = new Font("Segoe UI", 9),
                Location = new Point(leftMargin + labelWidth + 10, y),
                PromptChar = '_'
            };
            contentPanel.Controls.Add(txtContact);
            y += 40;

            var otherContactLabel = new Label
            {
                Text = "Другие способы связи:",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 30),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 9)
            };
            contentPanel.Controls.Add(otherContactLabel);

            txtContactOther = new TextBox
            {
                Width = 550,
                PlaceholderText = "Telegram, WhatsApp, соцсети",
                Font = new Font("Segoe UI", 9),
                Location = new Point(leftMargin + labelWidth + 10, y)
            };
            contentPanel.Controls.Add(txtContactOther);
            y += 60;

            // КНОПКИ ДЕЙСТВИЙ
            var btnPanel = new Panel
            {
                Location = new Point(leftMargin, y),
                Size = new Size(800, 70),
                BackColor = CardColor
            };
            btnPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, btnPanel.Width - 1, btnPanel.Height - 1);
            };

            btnSave = CreateStyledButton("💾 Сохранить изменения", SuccessColor, new Size(200, 42));
            btnSave.Location = new Point(50, 14);
            btnSave.Click += BtnSave_Click;

            btnCancel = CreateStyledButton("✕ Отмена", MutedColor, new Size(150, 42));
            btnCancel.Location = new Point(270, 14);
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            // КНОПКА: Удалить (только для владельца, модераторов и админов)
            string listingAuthorId = GetField("user_id");
            bool isOwner = listingAuthorId == _currentUserId;
            bool isModerator = _currentUserRole == "moderator" || _currentUserRole == "admin";

            if (isOwner || isModerator)
            {
                var btnDelete = CreateStyledButton("🗑️ Удалить объявление", DangerColor, new Size(200, 42));
                btnDelete.Location = new Point(450, 14);
                btnDelete.Click += async (s, e) => await DeleteListing();
                btnPanel.Controls.Add(btnDelete);
            }

            btnPanel.Controls.Add(btnSave);
            btnPanel.Controls.Add(btnCancel);
            contentPanel.Controls.Add(btnPanel);

            scrollPanel.Controls.Add(contentPanel);
            this.Controls.Add(scrollPanel);

            UpdateBreedList();
        }

        private void LoadListingData()
        {
            // Загружаем данные из объявления
            txtPetName.Text = GetField("pet_name");

            if (DateTime.TryParse(GetField("incident_date"), out var incidentDate))
                dtpIncidentDate.Value = incidentDate;

            // Вид
            string species = GetField("species");
            if (cbSpecies.Items.Contains(species))
                cbSpecies.SelectedItem = species;
            else
            {
                cbSpecies.SelectedItem = "Другое";
                txtOtherSpecies.Text = species;
                txtOtherSpecies.Visible = true;
            }

            // Порода
            string breed = GetField("breed");
            if (cbBreed.Visible)
            {
                if (cbBreed.Items.Contains(breed))
                    cbBreed.SelectedItem = breed;
                else
                {
                    cbBreed.SelectedItem = "Другая";
                    txtOtherBreed.Text = breed;
                    txtOtherBreed.Visible = true;
                }
            }

            // Возраст
            if (int.TryParse(GetField("age"), out int ageMonths))
            {
                nudAgeYears.Value = ageMonths / 12;
                nudAgeMonths.Value = ageMonths % 12;
            }

            // Пол
            string gender = GetField("gender");
            cbGender.SelectedIndex = gender == "male" ? 0 : (gender == "female" ? 1 : 2);

            // Размер
            string size = GetField("size");
            cbSize.SelectedIndex = size == "small" ? 0 : (size == "medium" ? 1 : 2);

            // Окрас
            string color = GetField("color");
            if (cbColor.Items.Contains(color))
                cbColor.SelectedItem = color;
            else
            {
                cbColor.SelectedItem = "Другое";
                txtOtherColor.Text = color;
                txtOtherColor.Visible = true;
            }

            // Характер
            string temperament = GetField("temperament");
            if (cbTemperament.Items.Contains(temperament))
                cbTemperament.SelectedItem = temperament;

            // Местоположение
            txtLocation.Text = GetField("location");

            if (double.TryParse(GetField("latitude"), out double lat))
                _latitude = lat;
            if (double.TryParse(GetField("longitude"), out double lon))
                _longitude = lon;

            // Радиус
            if (int.TryParse(GetField("search_radius"), out int radius))
                nudSearchRadius.Value = radius;

            // Чип
            txtMicrochip.Text = GetField("microchip");
            txtSpecialMarks.Text = GetField("special_marks");

            // Контакты
            txtContact.Text = GetField("contact_phone");
            txtContactOther.Text = GetField("contact");

            // Описание состояния поиска
            txtStatusDescription.Text = GetField("status_description");

            // Статус
            string status = GetField("status");
            cbStatus.SelectedIndex = status == "active" ? 0 : (status == "found" ? 1 : 2);
        }

        private string GetField(string key)
        {
            return _listing.ContainsKey(key) && _listing[key] != null ? _listing[key].ToString() : "";
        }

        private int AddSectionHeader(Panel parent, int y, int x, string title)
        {
            var lbl = new Label
            {
                Text = title,
                Location = new Point(x, y),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = PrimaryColor,
                AutoSize = true
            };
            parent.Controls.Add(lbl);

            var line = new Label
            {
                Location = new Point(x, y + 28),
                Size = new Size(800, 2),
                BackColor = BorderColor
            };
            parent.Controls.Add(line);

            return y + 38;
        }

        private Button CreateStyledButton(string text, Color backColor, Size size)
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

        private async void BtnSelectOnMap_Click(object sender, EventArgs e)
        {
            using var mapForm = new MapPickerForm(_latitude ?? 55.76, _longitude ?? 37.64);
            if (mapForm.ShowDialog() == DialogResult.OK && mapForm.IsLocationSelected)
            {
                _latitude = mapForm.Latitude;
                _longitude = mapForm.Longitude;
                txtLocation.Text = mapForm.Address;
                MessageBox.Show($"Выбрано место: {mapForm.Address}", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void UpdateBreedList()
        {
            if (cbSpecies == null) return;
            string selectedSpecies = cbSpecies.SelectedItem?.ToString();

            if (selectedSpecies == "Другое")
            {
                txtOtherSpecies.Visible = true;
                cbBreed.Visible = false;
                cbSubBreed.Visible = false;
                txtOtherBreed.Visible = false;
            }
            else
            {
                txtOtherSpecies.Visible = false;

                if (selectedSpecies == "Грызун" || selectedSpecies == "Птица")
                {
                    cbBreed.Visible = false;
                    cbSubBreed.Visible = true;
                    txtOtherBreed.Visible = false;
                    cbSubBreed.Items.Clear();
                    if (breedLists.ContainsKey(selectedSpecies))
                        cbSubBreed.Items.AddRange(breedLists[selectedSpecies].ToArray());
                    else
                        cbSubBreed.Items.Add("Другой");
                    cbSubBreed.SelectedIndex = 0;
                }
                else
                {
                    cbBreed.Visible = true;
                    cbSubBreed.Visible = false;
                    txtOtherBreed.Visible = false;
                    cbBreed.Items.Clear();
                    if (breedLists.ContainsKey(selectedSpecies))
                        cbBreed.Items.AddRange(breedLists[selectedSpecies].ToArray());
                    else
                        cbBreed.Items.Add("Другая");
                    cbBreed.SelectedIndex = 0;
                }
            }
        }

        private void OnBreedChanged(object sender, EventArgs e)
        {
            ComboBox currentBreed = sender as ComboBox;
            if (currentBreed == null) return;
            string selectedBreed = currentBreed.SelectedItem?.ToString();
            txtOtherBreed.Visible = selectedBreed == "Другая" || selectedBreed == "Другой";
        }

        private async void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtContact.Text) || txtContact.Text == "+7 (___) ___-__-__")
            {
                MessageBox.Show("Введите телефон для звонка.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnSave.Enabled = false;
            btnSave.Text = "⏳ Сохранение...";

            try
            {
                string listingId = GetField("id");
                string actualSpecies = (cbSpecies.SelectedItem?.ToString() == "Другое") ? txtOtherSpecies.Text.Trim() : cbSpecies.SelectedItem?.ToString();

                string breed = "";
                if (cbBreed.Visible)
                {
                    breed = cbBreed.SelectedItem?.ToString();
                    if (breed == "Другая") breed = txtOtherBreed.Text.Trim();
                }
                else if (cbSubBreed.Visible)
                {
                    breed = cbSubBreed.SelectedItem?.ToString();
                    if (breed == "Другой" || breed == "Другая") breed = txtOtherBreed.Text.Trim();
                }

                string color = cbColor.SelectedItem?.ToString();
                if (color == "Другое") color = txtOtherColor.Text.Trim();

                int ageYears = (int)nudAgeYears.Value;
                int ageMonths = (int)nudAgeMonths.Value;
                int? totalMonths = (ageYears > 0 || ageMonths > 0) ? ageYears * 12 + ageMonths : null;

                string gender = cbGender.SelectedIndex == 0 ? "male" : (cbGender.SelectedIndex == 1 ? "female" : "unknown");
                string size = cbSize.SelectedItem?.ToString() switch
                {
                    "Маленький" => "small",
                    "Средний" => "medium",
                    "Большой" => "large",
                    _ => "medium"
                };

                // Определяем статус
                string status = cbStatus.SelectedIndex == 0 ? "active" : (cbStatus.SelectedIndex == 1 ? "found" : "closed");

                var updates = new
                {
                    pet_name = string.IsNullOrWhiteSpace(txtPetName.Text) ? null : txtPetName.Text.Trim(),
                    species = actualSpecies,
                    breed = string.IsNullOrEmpty(breed) ? null : breed,
                    age = totalMonths,
                    gender = gender,
                    size = size,
                    color = string.IsNullOrEmpty(color) ? null : color,
                    temperament = cbTemperament.SelectedItem?.ToString(),
                    location = string.IsNullOrWhiteSpace(txtLocation.Text) ? null : txtLocation.Text.Trim(),
                    contact = string.IsNullOrWhiteSpace(txtContactOther.Text) ? null : txtContactOther.Text.Trim(),
                    contact_phone = txtContact.Text.Trim(),
                    microchip = string.IsNullOrWhiteSpace(txtMicrochip.Text) ? null : txtMicrochip.Text.Trim(),
                    special_marks = string.IsNullOrWhiteSpace(txtSpecialMarks.Text) ? null : txtSpecialMarks.Text.Trim(),
                    search_radius = (int)nudSearchRadius.Value,
                    incident_date = dtpIncidentDate.Value.ToUniversalTime(),
                    latitude = _latitude,
                    longitude = _longitude,
                    status = status,
                    status_description = string.IsNullOrWhiteSpace(txtStatusDescription.Text) ? null : txtStatusDescription.Text.Trim(),
                    last_updated = DateTime.UtcNow
                };

                using var httpClient = new HttpClient();
                var json = JsonConvert.SerializeObject(updates);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings?id=eq.{listingId}";
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                var response = await httpClient.PatchAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Ошибка сервера: {error}");
                }

                MessageBox.Show("✓ Объявление обновлено!", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                btnSave.Text = "💾 Сохранить изменения";
            }
        }

        private async Task DeleteListing()
        {
            string listingId = GetField("id");
            string listingAuthorId = GetField("user_id");

            // Проверяем права
            bool isOwner = listingAuthorId == _currentUserId;
            bool isModerator = _currentUserRole == "moderator" || _currentUserRole == "admin";

            if (!isOwner && !isModerator)
            {
                MessageBox.Show("❌ У вас нет прав на удаление этого объявления", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string confirmText = isModerator && !isOwner
                ? "⚠️ Вы удаляете ЧУЖОЕ объявление!\n\nЭто действие нельзя отменить."
                : "⚠️ Удалить объявление?\n\nЭто действие нельзя отменить.";

            var result = MessageBox.Show(
                confirmText,
                "Подтверждение удаления",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");

                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings?id=eq.{listingId}";
                var response = await httpClient.DeleteAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("✅ Объявление удалено!", "Успех",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"❌ Ошибка удаления: {error}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}