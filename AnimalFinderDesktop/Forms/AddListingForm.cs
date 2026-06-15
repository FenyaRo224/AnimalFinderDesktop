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
        private Panel contentPanel;

        private static readonly Color PrimaryColor = Color.FromArgb(0, 122, 204);
        private static readonly Color SuccessColor = Color.FromArgb(40, 167, 69);
        private static readonly Color DangerColor = Color.FromArgb(220, 53, 69);
        private static readonly Color BackgroundColor = Color.FromArgb(245, 247, 250);
        private static readonly Color CardColor = Color.White;
        private static readonly Color TextColor = Color.FromArgb(51, 51, 51);
        private static readonly Color MutedColor = Color.FromArgb(108, 117, 125);
        private static readonly Color BorderColor = Color.FromArgb(222, 226, 230);

        private ComboBox cbType, cbSpecies, cbBreed, cbColor, cbGender, cbSize, cbTemperament, cbSubBreed;
        private TextBox txtPetName, txtOtherSpecies, txtOtherBreed, txtOtherColor, txtLocation, txtContactOther, txtMicrochip, txtSpecialMarks, txtDescription;
        private MaskedTextBox txtContact;
        private NumericUpDown nudAgeYears, nudAgeMonths, nudSearchRadius;
        private DateTimePicker dtpIncidentDate;
        private Button btnChoosePhotos, btnSave, btnCancel, btnFillFromProfile, btnSelectOnMap;
        private FlowLayoutPanel flpPhotos;
        private List<string> _photoPaths = new List<string>();

        private CheckBox chkVerify;
        private TextBox txtDocComment;
        private string selectedDocPath = null;
        private Panel verifyPanel;
        private Label verifyNote;
        private Label verifySectionHeader;
        private Label verifySectionLine;

        private double? _latitude = null;
        private double? _longitude = null;

        private Dictionary<string, List<string>> breedLists = new Dictionary<string, List<string>>
        {
            ["Собака"] = new List<string> { "Другая", "Лабрадор", "Немецкая овчарка", "Французский бульдог", "Йоркширский терьер", "Пудель", "Ротвейлер", "Джек-рассел-терьер", "Сиба-ину", "Хаски", "Чихуахуа", "Мопс", "Такса", "Корги", "Бигль" },
            ["Кошка"] = new List<string> { "Другая", "Британская", "Шотландская", "Мейн-кун", "Сиамская", "Персидская", "Сфинкс", "Бенгальская", "Абиссинская", "Русская голубая", "Норвежская лесная", "Рэгдолл" },
            ["Грызун"] = new List<string> { "Другой", "Хомяк", "Крыса", "Морская свинка", "Кролик", "Шиншилла", "Песчанка", "Декоративная мышь", "Белка", "Суслик" },
            ["Птица"] = new List<string> { "Другая", "Попугай", "Канарейка", "Воробей", "Голубь", "Ворона", "Ара", "Корелла", "Жако", "Нимфа", "Волнистый попугайчик" }
        };

        public AddListingForm()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(950, 1000);
            this.MinimumSize = new Size(950, 1000);
            this.BackColor = BackgroundColor;
            this.Font = new Font("Segoe UI", 9);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.ControlBox = true;
            this.Text = "AnimalFinder - Создание объявления";
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
                Size = new Size(880, 1700),
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
                Text = " Создание объявления",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = PrimaryColor,
                Location = new Point(leftMargin, y),
                AutoSize = true
            };
            contentPanel.Controls.Add(lblTitle);
            y += 55;

            // СЕКЦИЯ 1: Основная информация
            y = AddSectionHeader(contentPanel, y, leftMargin, "Основная информация");
            y += 15;

            var typeLabel = new Label
            {
                Text = "Тип объявления:",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 30),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 9)
            };
            contentPanel.Controls.Add(typeLabel);

            cbType = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = fieldWidth,
                Font = new Font("Segoe UI", 9),
                Location = new Point(leftMargin + labelWidth + 10, y)
            };
            cbType.Items.AddRange(new[] { "Пропал(а)", "Найден(а)" });
            cbType.SelectedIndex = 0;
            cbType.SelectedIndexChanged += (s, e) => { UpdatePetNameRequirement(); UpdateVerifyVisibility(); };
            contentPanel.Controls.Add(cbType);
            y += 40;

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

            var nameNote = new Label
            {
                Text = "(необязательное поле)",
                Location = new Point(leftMargin + labelWidth + 10, y + 28),
                AutoSize = true,
                Font = new Font("Segoe UI", 7),
                ForeColor = MutedColor
            };
            contentPanel.Controls.Add(nameNote);
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

            // СЕКЦИЯ 2: Характеристики животного
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

            var ageNote = new Label
            {
                Text = "(необязательное поле)",
                Location = new Point(leftMargin + labelWidth + 10, y + 28),
                AutoSize = true,
                Font = new Font("Segoe UI", 7),
                ForeColor = MutedColor
            };
            contentPanel.Controls.Add(ageNote);
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

            var genderNote = new Label
            {
                Text = "(необязательное поле)",
                Location = new Point(leftMargin + labelWidth + 10, y + 28),
                AutoSize = true,
                Font = new Font("Segoe UI", 7),
                ForeColor = MutedColor
            };
            contentPanel.Controls.Add(genderNote);
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

            var colorNote = new Label
            {
                Text = "(необязательное поле)",
                Location = new Point(leftMargin + labelWidth + 10, y + 28),
                AutoSize = true,
                Font = new Font("Segoe UI", 7),
                ForeColor = MutedColor
            };
            contentPanel.Controls.Add(colorNote);
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

            var tempNote = new Label
            {
                Text = "(необязательное поле)",
                Location = new Point(leftMargin + labelWidth + 10, y + 28),
                AutoSize = true,
                Font = new Font("Segoe UI", 7),
                ForeColor = MutedColor
            };
            contentPanel.Controls.Add(tempNote);
            y += 50;

            // СЕКЦИЯ 3: Местоположение
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

            var radiusNote = new Label
            {
                Text = "(необязательное поле)",
                Location = new Point(leftMargin + labelWidth + 10, y + 28),
                AutoSize = true,
                Font = new Font("Segoe UI", 7),
                ForeColor = MutedColor
            };
            contentPanel.Controls.Add(radiusNote);
            y += 50;

            // СЕКЦИЯ 4: Дополнительная информация
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

            var chipNote = new Label
            {
                Text = "(необязательное поле)",
                Location = new Point(leftMargin + labelWidth + 10, y + 28),
                AutoSize = true,
                Font = new Font("Segoe UI", 7),
                ForeColor = MutedColor
            };
            contentPanel.Controls.Add(chipNote);
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

            var marksNote = new Label
            {
                Text = "(необязательное поле)",
                Location = new Point(leftMargin + labelWidth + 10, y + 28),
                AutoSize = true,
                Font = new Font("Segoe UI", 7),
                ForeColor = MutedColor
            };
            contentPanel.Controls.Add(marksNote);
            y += 50;

            // СЕКЦИЯ 5: Контакты
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

            var otherContactNote = new Label
            {
                Text = "(необязательное поле)",
                Location = new Point(leftMargin + labelWidth + 10, y + 28),
                AutoSize = true,
                Font = new Font("Segoe UI", 7),
                ForeColor = MutedColor
            };
            contentPanel.Controls.Add(otherContactNote);
            y += 45;  // ← было 50, стало 40 (меньше отступ)

            // Кнопка заполнить из профиля (как поле с меткой слева)
            var fillProfileLabel = new Label
            {
                Text = "Заполнить данные из профиля:",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 30),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 9),
            };
            contentPanel.Controls.Add(fillProfileLabel);

            btnFillFromProfile = CreateStyledButton("📋 Заполнить", PrimaryColor, new Size(160, 32));
            btnFillFromProfile.Location = new Point(leftMargin + labelWidth + 10, y);
            btnFillFromProfile.Click += BtnFillFromProfile_Click;
            contentPanel.Controls.Add(btnFillFromProfile);

            y += 40;  // ← отступ до следующей секции


            // СЕКЦИЯ 6: Описание
            y = AddSectionHeader(contentPanel, y, leftMargin, "Дополнительное Описание");
            y += 15;

            var descLabel = new Label
            {
                Text = "Описание:",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 30),
                TextAlign = ContentAlignment.TopRight,
                Font = new Font("Segoe UI", 9)
            };
            contentPanel.Controls.Add(descLabel);

            txtDescription = new TextBox
            {
                Location = new Point(leftMargin + labelWidth + 10, y),
                Size = new Size(550, 100),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                PlaceholderText = "Опишите внешность, характер, особые приметы, обстоятельства пропажи/находки...",
                Font = new Font("Segoe UI", 9)
            };
            contentPanel.Controls.Add(txtDescription);

            var descNote = new Label
            {
                Text = "(необязательное поле)",
                Location = new Point(leftMargin + labelWidth + 10, y + 98),
                AutoSize = true,
                Font = new Font("Segoe UI", 7),
                ForeColor = MutedColor
            };
            contentPanel.Controls.Add(descNote);
            y += 120;

            // СЕКЦИЯ 7: Фотографии
            y = AddSectionHeader(contentPanel, y, leftMargin, "Фотографии");
            y += 15;

            var photosLabel = new Label
            {
                Text = "Фото:",
                Location = new Point(leftMargin, y),
                Size = new Size(labelWidth, 30),
                TextAlign = ContentAlignment.TopRight,
                Font = new Font("Segoe UI", 9)
            };
            contentPanel.Controls.Add(photosLabel);

            btnChoosePhotos = CreateStyledButton("📷 Добавить фото", PrimaryColor, new Size(160, 32));
            btnChoosePhotos.Location = new Point(leftMargin + labelWidth + 10, y);
            btnChoosePhotos.Click += BtnChoosePhotos_Click;
            contentPanel.Controls.Add(btnChoosePhotos);

            flpPhotos = new FlowLayoutPanel
            {
                Location = new Point(leftMargin + labelWidth + 10, y + 40),
                Size = new Size(550, 100),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = CardColor
            };
            contentPanel.Controls.Add(flpPhotos);
            y += 160;

            // СЕКЦИЯ 8: Верификация
            verifySectionHeader = new Label
            {
                Text = "Подтверждение владельца",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = PrimaryColor,
                Location = new Point(leftMargin, y),
                AutoSize = true,
                Visible = false
            };
            contentPanel.Controls.Add(verifySectionHeader);

            verifySectionLine = new Label
            {
                Location = new Point(leftMargin, y + 28),
                Size = new Size(800, 2),
                BackColor = BorderColor,
                Visible = false
            };
            contentPanel.Controls.Add(verifySectionLine);
            y += 50;

            chkVerify = new CheckBox
            {
                Text = "Подтвердить, что животное принадлежит мне",
                Location = new Point(leftMargin + labelWidth + 10, y),
                AutoSize = true,
                ForeColor = SuccessColor,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Visible = false
            };
            contentPanel.Controls.Add(chkVerify);

            verifyPanel = new Panel
            {
                Location = new Point(leftMargin + labelWidth + 10, y + 30),
                Size = new Size(550, 100),
                Visible = false,
                BackColor = Color.FromArgb(248, 249, 250)
            };

            var lblDoc = new Label { Text = "Загрузите документ:", Location = new Point(0, 10), AutoSize = true, Font = new Font("Segoe UI", 9) };
            var btnChooseDoc = CreateStyledButton("📎 Выбрать файл", PrimaryColor, new Size(140, 28));
            btnChooseDoc.Location = new Point(0, 35);
            var lblDocFile = new Label { Text = "Файл не выбран", Location = new Point(150, 40), AutoSize = true, ForeColor = MutedColor };
            txtDocComment = new TextBox { Location = new Point(0, 70), Size = new Size(400, 25), PlaceholderText = "Комментарий", Font = new Font("Segoe UI", 9) };

            verifyPanel.Controls.Add(lblDoc);
            verifyPanel.Controls.Add(btnChooseDoc);
            verifyPanel.Controls.Add(lblDocFile);
            verifyPanel.Controls.Add(txtDocComment);

            btnChooseDoc.Click += (s, ev) =>
            {
                using var ofd = new OpenFileDialog();
                ofd.Filter = "Изображения|*.jpg;*.jpeg;*.png|PDF|*.pdf";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    selectedDocPath = ofd.FileName;
                    lblDocFile.Text = Path.GetFileName(selectedDocPath);
                    lblDocFile.ForeColor = SuccessColor;
                }
            };

            chkVerify.CheckedChanged += (s, ev) => verifyPanel.Visible = chkVerify.Checked;
            contentPanel.Controls.Add(verifyPanel);

            verifyNote = new Label
            {
                Text = "(необязательное поле)",
                Location = new Point(leftMargin + labelWidth + 10, y + 28),
                AutoSize = true,
                Font = new Font("Segoe UI", 7),
                ForeColor = MutedColor,
                Visible = false
            };
            contentPanel.Controls.Add(verifyNote);
            y += 150;

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

            btnSave = CreateStyledButton("✓ Опубликовать", SuccessColor, new Size(180, 42));
            btnSave.Location = new Point(100, 14);  
            btnSave.Click += BtnSave_Click;

            btnCancel = CreateStyledButton("✕ Отмена", MutedColor, new Size(150, 42));
            btnCancel.Location = new Point(310, 14);  
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            btnPanel.Controls.Add(btnSave);
            btnPanel.Controls.Add(btnCancel);
            contentPanel.Controls.Add(btnPanel);

            // Поднимаем btnPanel наверх
            contentPanel.Controls.SetChildIndex(btnPanel, contentPanel.Controls.Count - 1);

            scrollPanel.Controls.Add(contentPanel);
            this.Controls.Add(scrollPanel);

            UpdatePetNameRequirement();
            UpdateBreedList();
            UpdateVerifyVisibility();
        }

        private void UpdateVerifyVisibility()
        {
            bool isLost = cbType.SelectedItem?.ToString() == "Пропал(а)";
            verifySectionHeader.Visible = isLost;
            verifySectionLine.Visible = isLost;
            chkVerify.Visible = isLost;
            verifyPanel.Visible = isLost && chkVerify.Checked;
            verifyNote.Visible = isLost;
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

        private void UpdatePetNameRequirement() { }

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
                var lblBreed = contentPanel?.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "lblBreed");
                if (lblBreed != null) lblBreed.Text = "Порода:";
            }
            else
            {
                txtOtherSpecies.Visible = false;

                if (selectedSpecies == "Грызун" || selectedSpecies == "Птица")
                {
                    cbBreed.Visible = false;
                    cbSubBreed.Visible = true;
                    txtOtherBreed.Visible = false;
                    var lblBreed = contentPanel?.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "lblBreed");
                    if (lblBreed != null) lblBreed.Text = "Подвид:";
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
                    var lblBreed = contentPanel?.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "lblBreed");
                    if (lblBreed != null) lblBreed.Text = "Порода:";
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
                    var photoPanel = new Panel { Width = 80, Height = 80, Margin = new Padding(3) };
                    var pb = new PictureBox { Width = 70, Height = 70, SizeMode = PictureBoxSizeMode.Zoom, Image = Image.FromFile(file), Location = new Point(0, 0), BorderStyle = BorderStyle.FixedSingle };
                    var btnRemove = new Button { Text = "✕", Size = new Size(20, 20), Location = new Point(60, 0), BackColor = DangerColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8, FontStyle.Bold) };
                    btnRemove.FlatAppearance.BorderSize = 0;
                    int photoIndex = _photoPaths.Count - 1;
                    btnRemove.Click += (s, ev) => { _photoPaths.RemoveAt(photoIndex); photoPanel.Parent.Controls.Remove(photoPanel); };
                    photoPanel.Controls.Add(pb);
                    photoPanel.Controls.Add(btnRemove);
                    flpPhotos.Controls.Add(photoPanel);
                }
            }
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
                var client = await SupabaseService.GetClient();
                var userId = client.Auth.CurrentUser?.Id;

                string listingType = cbType.SelectedIndex == 0 ? "lost" : "found";
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

                var newListing = new
                {
                    id = Guid.NewGuid().ToString(),
                    listing_type = listingType,
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
                    description = string.IsNullOrWhiteSpace(txtDescription.Text) ? null : txtDescription.Text.Trim(),
                    search_radius = (int)nudSearchRadius.Value,
                    incident_date = dtpIncidentDate.Value.ToUniversalTime(),
                    user_id = userId,
                    created_at = DateTime.UtcNow,
                    status = "on_moderation",
                    latitude = _latitude,
                    longitude = _longitude
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
                    var updateData = new { photo_urls = string.Join(";", localPaths) };
                    var updateJson = JsonConvert.SerializeObject(updateData);
                    var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");
                    var updateUrl = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings?id=eq.{insertedId}";
                    await httpClient.PatchAsync(updateUrl, updateContent);
                }

                if (chkVerify.Checked && !string.IsNullOrEmpty(selectedDocPath))
                {
                    string docsDir = Path.Combine(Application.StartupPath, "VerificationDocs");
                    if (!Directory.Exists(docsDir)) Directory.CreateDirectory(docsDir);
                    string ext = Path.GetExtension(selectedDocPath);
                    string newName = $"{insertedId}_verif_{Guid.NewGuid()}{ext}";
                    string destPath = Path.Combine(docsDir, newName);
                    File.Copy(selectedDocPath, destPath, true);

                    var verifyData = new
                    {
                        user_id = userId,
                        pet_listing_id = insertedId,
                        request_type = "animal_verification",
                        document_url = $"VerificationDocs/{newName}",
                        comment = txtDocComment?.Text.Trim() ?? "",
                        status = "pending",
                        created_at = DateTime.UtcNow
                    };
                    var verifyJson = JsonConvert.SerializeObject(verifyData);
                    var verifyContent = new StringContent(verifyJson, Encoding.UTF8, "application/json");
                    var verifyUrl = "https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/verification_requests";
                    await httpClient.PostAsync(verifyUrl, verifyContent);
                }

                MessageBox.Show("✓ Объявление отправлено на модерацию.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                btnSave.Text = "✓ Опубликовать";
            }
        }
    }
}