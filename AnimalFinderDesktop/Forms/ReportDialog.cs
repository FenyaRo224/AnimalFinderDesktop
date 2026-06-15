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
        private static readonly Color PrimaryColor = Color.FromArgb(0, 122, 204);
        private static readonly Color SuccessColor = Color.FromArgb(40, 167, 69);
        private static readonly Color DangerColor = Color.FromArgb(220, 53, 69);
        private static readonly Color WarningColor = Color.FromArgb(255, 193, 7);
        private static readonly Color BackgroundColor = Color.FromArgb(245, 247, 250);
        private static readonly Color CardColor = Color.White;
        private static readonly Color TextColor = Color.FromArgb(51, 51, 51);
        private static readonly Color MutedColor = Color.FromArgb(108, 117, 125);
        private static readonly Color BorderColor = Color.FromArgb(226, 232, 240);

        private string _targetId;
        private string _targetType; // "listing" или "profile"
        private ComboBox cbReason;
        private TextBox tbComment;
        private Button btnSend, btnCancel;
        private Label lblTargetInfo;

        // Конструктор для жалобы на объявление
        public ReportDialog(string listingId) : this(listingId, "listing") { }

        // Универсальный конструктор
        public ReportDialog(string targetId, string targetType)
        {
            _targetId = targetId;
            _targetType = targetType;
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "AnimalFinder - Пожаловаться";
            this.Size = new System.Drawing.Size(500, 500);
            this.MinimumSize = new System.Drawing.Size(500, 500);
            this.MaximumSize = new System.Drawing.Size(500, 500);
            this.BackColor = BackgroundColor;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
        }

        private void InitializeComponent()
        {
            int y = 20;
            int left = 25;
            int width = 430;

            // Заголовок
            var lblTitle = new Label
            {
                Text = _targetType == "listing" ? "🚨 Пожаловаться на объявление" : "🚨 Пожаловаться на пользователя",
                Font = new System.Drawing.Font("Segoe UI", 14, System.Drawing.FontStyle.Bold),
                ForeColor = PrimaryColor,
                Location = new System.Drawing.Point(left, y),
                AutoSize = true
            };
            this.Controls.Add(lblTitle);
            y += 45;

            // Информация о цели
            lblTargetInfo = new Label
            {
                Text = _targetType == "listing" ? $"ID объявления: {_targetId}" : $"ID пользователя: {_targetId}",
                Font = new System.Drawing.Font("Segoe UI", 9),
                ForeColor = MutedColor,
                Location = new System.Drawing.Point(left, y),
                AutoSize = true
            };
            this.Controls.Add(lblTargetInfo);
            y += 30;

            // Карточка с формой
            var cardPanel = new Panel
            {
                Location = new System.Drawing.Point(left, y),
                Size = new System.Drawing.Size(width, 300),
                BackColor = CardColor
            };
            cardPanel.Paint += (s, e) =>
            {
                using var pen = new System.Drawing.Pen(BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, cardPanel.Width - 1, cardPanel.Height - 1);
            };
            this.Controls.Add(cardPanel);

            int innerY = 20;
            int innerLeft = 20;
            int innerWidth = width - 40;

            // Причина жалобы
            var lblReason = new Label
            {
                Text = "Причина жалобы:",
                Location = new System.Drawing.Point(innerLeft, innerY),
                Size = new System.Drawing.Size(innerWidth, 22),
                Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold),
                ForeColor = TextColor
            };
            cardPanel.Controls.Add(lblReason);
            innerY += 28;

            cbReason = new ComboBox
            {
                Location = new System.Drawing.Point(innerLeft, innerY),
                Size = new System.Drawing.Size(innerWidth, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new System.Drawing.Font("Segoe UI", 10)
            };

            // Разные причины для объявления и профиля
            if (_targetType == "listing")
            {
                cbReason.Items.AddRange(new[] {
                    "Спам или реклама",
                    "Оскорбительное содержание",
                    "Недостоверная информация",
                    "Мошенничество",
                    "Жестокое обращение с животным",
                    "Нарушение правил сервиса",
                    "Другое"
                });
            }
            else
            {
                cbReason.Items.AddRange(new[] {
                    "Оскорбительное поведение",
                    "Мошенничество",
                    "Спам",
                    "Неадекватное поведение",
                    "Нарушение правил сервиса",
                    "Другое"
                });
            }
            cbReason.SelectedIndex = 0;
            cardPanel.Controls.Add(cbReason);
            innerY += 45;

            // Комментарий
            var lblComment = new Label
            {
                Text = "Подробное описание (необязательно):",
                Location = new System.Drawing.Point(innerLeft, innerY),
                Size = new System.Drawing.Size(innerWidth, 22),
                Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold),
                ForeColor = TextColor
            };
            cardPanel.Controls.Add(lblComment);
            innerY += 28;

            tbComment = new TextBox
            {
                Location = new System.Drawing.Point(innerLeft, innerY),
                Size = new System.Drawing.Size(innerWidth, 100),
                Multiline = true,
                Font = new System.Drawing.Font("Segoe UI", 10),
                PlaceholderText = "Опишите ситуацию подробнее..."
            };
            cardPanel.Controls.Add(tbComment);
            innerY += 110;

            // Предупреждение
            var lblWarning = new Label
            {
                Text = "⚠️ Ложные жалобы могут привести к блокировке вашего аккаунта",
                Font = new System.Drawing.Font("Segoe UI", 8),
                ForeColor = WarningColor,
                Location = new System.Drawing.Point(innerLeft, innerY),
                Size = new System.Drawing.Size(innerWidth, 30)
            };
            cardPanel.Controls.Add(lblWarning);

            y += 315;

            // Кнопки
            btnSend = CreateModernButton("📨 Отправить жалобу", DangerColor, new System.Drawing.Size(220, 40));
            btnSend.Location = new System.Drawing.Point(left, y);
            btnSend.Click += BtnSend_Click;

            btnCancel = CreateModernButton("✕ Отмена", MutedColor, new System.Drawing.Size(140, 40));
            btnCancel.Location = new System.Drawing.Point(left + 235, y);
            btnCancel.Click += (s, e) => this.Close();

            this.Controls.Add(btnSend);
            this.Controls.Add(btnCancel);
        }

        private Button CreateModernButton(string text, System.Drawing.Color backColor, System.Drawing.Size size)
        {
            var button = new Button
            {
                Text = text,
                Size = size,
                BackColor = backColor,
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            return button;
        }

        private async void BtnSend_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(cbReason.Text))
            {
                MessageBox.Show("Выберите причину жалобы.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnSend.Enabled = false;
            btnSend.Text = "⏳ Отправка...";

            try
            {
                var client = await SupabaseService.GetClient();
                var userId = client.Auth.CurrentUser?.Id;

                var data = new
                {
                    // Для совместимости: listing_id = target_id (если объявление) или пустая строка
                    listing_id = _targetType == "listing" ? _targetId : (object)null,
                    user_id = userId,
                    reason = cbReason.Text,
                    comment = string.IsNullOrEmpty(tbComment.Text) ? "—" : tbComment.Text,
                    status = "pending",
                    report_type = _targetType, // "listing" или "profile"
                    target_id = _targetId
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
                    // Отправляем уведомление модераторам (опционально)
                    try
                    {
                        await NotifyModerators(userId, _targetType, _targetId, cbReason.Text);
                    }
                    catch { }

                    MessageBox.Show(
                        "✅ Жалоба успешно отправлена!\n\nМодератор рассмотрит её в ближайшее время.",
                        "Успех",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"❌ Ошибка отправки: {error}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSend.Enabled = true;
                btnSend.Text = "📨 Отправить жалобу";
            }
        }

        private async Task NotifyModerators(string reporterId, string targetType, string targetId, string reason)
        {
            // Находим всех модераторов и админов
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");

            var url = "https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/profiles?or=(role.eq.moderator,role.eq.admin)&select=user_id";
            var response = await httpClient.GetStringAsync(url);
            var moderators = JsonConvert.DeserializeObject<System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>>(response);

            if (moderators == null) return;

            string targetName = targetType == "listing" ? "объявление" : "пользователя";

            foreach (var mod in moderators)
            {
                var modId = mod["user_id"]?.ToString();
                if (string.IsNullOrEmpty(modId)) continue;

                var notifData = new
                {
                    user_id = modId,
                    title = "🚨 Новая жалоба",
                    message = $"Поступила жалоба на {targetName}. Причина: {reason}",
                    type = "report",
                    related_id = targetId,
                    is_read = false
                };

                var notifJson = JsonConvert.SerializeObject(notifData);
                var notifContent = new StringContent(notifJson, Encoding.UTF8, "application/json");
                await httpClient.PostAsync("https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/notifications", notifContent);
            }
        }
    }
}