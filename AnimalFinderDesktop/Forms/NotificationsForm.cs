using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AnimalFinderDesktop.Services;

namespace AnimalFinderDesktop.Forms
{
    public class NotificationsForm : Form
    {
        private FlowLayoutPanel flpNotifications;
        private Button btnMarkAllRead;
        private List<dynamic> _notifications;

        public NotificationsForm()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(500, 600);
            this.MinimumSize = new Size(500, 600);
            this.MaximumSize = new Size(500, 600);
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.ControlBox = true;
            this.Text = "AnimalFinder - Уведомления";
            this.BackColor = Color.White;
            this.Load += async (s, e) => await LoadNotifications();
        }

        private void InitializeComponent()
        {
            flpNotifications = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(240, 242, 245)
            };

            btnMarkAllRead = new Button
            {
                Text = "✓ Отметить все как прочитанные",
                Dock = DockStyle.Bottom,
                Height = 45,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnMarkAllRead.Click += async (s, e) => await MarkAllRead();

            this.Controls.Add(flpNotifications);
            this.Controls.Add(btnMarkAllRead);
        }

        private async Task LoadNotifications()
        {
            try
            {
                var client = await SupabaseService.GetClient();
                var userId = client.Auth.CurrentUser?.Id;

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");

                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/notifications?user_id=eq.{userId}&order=created_at.desc&limit=50";
                var response = await httpClient.GetStringAsync(url);
                _notifications = Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(response) ?? new List<dynamic>();

                flpNotifications.Controls.Clear();

                foreach (var n in _notifications)
                {
                    string type = n.type;
                    // Пропускаем уведомления о сообщениях в чате
                    if (type == "message") continue;

                    var notificationCard = CreateNotificationCard(n);
                    flpNotifications.Controls.Add(notificationCard);
                }

                if (flpNotifications.Controls.Count == 0)
                {
                    var emptyLabel = new Label
                    {
                        Text = "📭 Нет уведомлений",
                        Font = new Font("Segoe UI", 14),
                        ForeColor = Color.Gray,
                        AutoSize = true,
                        Margin = new Padding(10, 20, 0, 0)
                    };
                    flpNotifications.Controls.Add(emptyLabel);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private Panel CreateNotificationCard(dynamic notification)
        {
            string id = notification.id;
            string title = notification.title;
            string message = notification.message;
            string type = notification.type;
            string relatedId = notification.related_id;
            bool isRead = notification.is_read;
            DateTime createdAt = notification.created_at;

            var card = new Panel
            {
                Width = 440,
                AutoSize = true,
                BackColor = isRead ? Color.White : Color.FromArgb(255, 255, 230),
                Margin = new Padding(0, 0, 0, 8),
                Padding = new Padding(10),
                Cursor = Cursors.Hand
            };

            card.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle,
                    Color.LightGray, 1, ButtonBorderStyle.Solid,
                    Color.LightGray, 0, ButtonBorderStyle.Solid,
                    Color.LightGray, 0, ButtonBorderStyle.Solid,
                    Color.LightGray, 0, ButtonBorderStyle.Solid);
            };

            var iconLabel = new Label
            {
                Text = GetIconByType(type),
                Font = new Font("Segoe UI", 18),
                Location = new Point(10, 10),
                Size = new Size(40, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(60, 10),
                AutoSize = true,
                MaximumSize = new Size(280, 0)
            };

            var messageLabel = new Label
            {
                Text = message,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray,
                Location = new Point(60, 32),
                AutoSize = true,
                MaximumSize = new Size(280, 0)
            };

            var timeLabel = new Label
            {
                Text = FormatTime(createdAt),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray,
                Location = new Point(340, 10),
                AutoSize = true
            };

            var markReadBtn = new Button
            {
                Text = "✓",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Size = new Size(30, 30),
                Location = new Point(390, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            markReadBtn.Click += async (s, e) =>
            {
                await MarkAsRead(id);
                await LoadNotifications();
            };

            card.Controls.Add(iconLabel);
            card.Controls.Add(titleLabel);
            card.Controls.Add(messageLabel);
            card.Controls.Add(timeLabel);
            card.Controls.Add(markReadBtn);

            card.Click += async (s, e) =>
            {
                await OnNotificationClick(id, type, relatedId);
            };

            return card;
        }

        private string GetIconByType(string type)
        {
            switch (type)
            {
                case "moderation": return "🛡️";      // Модерация объявления (одобрено/отклонено)
                case "verification": return "✅";      // Верификация животного
                case "rating": return "⭐";           // Изменение рейтинга
                case "report": return "⚠️";           // Жалоба на объявление или профиль
                default: return "🔔";
            }
        }

        private string FormatTime(DateTime time)
        {
            var diff = DateTime.Now - time;
            if (diff.TotalMinutes < 1) return "только что";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} мин назад";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} ч назад";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} д назад";
            return time.ToString("dd.MM.yyyy");
        }

        private async Task OnNotificationClick(string id, string type, string relatedId)
        {
            await MarkAsRead(id);

            // Обработка клика по разным типам уведомлений
            switch (type)
            {
                case "moderation":
                    // Открываем объявление связанное с модерацией
                    if (!string.IsNullOrEmpty(relatedId))
                    {
                        await OpenListingById(relatedId);
                    }
                    break;

                case "verification":
                    // Открываем объявление связанное с верификацией
                    if (!string.IsNullOrEmpty(relatedId))
                    {
                        await OpenListingById(relatedId);
                    }
                    break;

                case "rating":
                    // Открываем профиль чтобы увидеть новый рейтинг
                    var profileForm = new ProfileForm();
                    profileForm.ShowDialog();
                    break;

                case "report":
                    // Жалоба на объявление или профиль - открываем связанное объявление
                    if (!string.IsNullOrEmpty(relatedId))
                    {
                        await OpenListingById(relatedId);
                    }
                    break;
            }

            await LoadNotifications();
        }

        private async Task OpenListingById(string listingId)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");

                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings?id=eq.{listingId}&select=*";
                var response = await httpClient.GetStringAsync(url);
                var listings = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response);

                if (listings != null && listings.Count > 0)
                {
                    var detailForm = new DetailForm(listings[0]);
                    detailForm.ShowDialog();
                }
                else
                {
                    MessageBox.Show("Объявление не найдено или было удалено", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private async Task MarkAsRead(string notificationId)
        {
            await SupabaseService.MarkNotificationRead(notificationId);
        }

        private async Task MarkAllRead()
        {
            var client = await SupabaseService.GetClient();
            var userId = client.Auth.CurrentUser?.Id;

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");

            var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/notifications?user_id=eq.{userId}&is_read=eq.false";
            var updateData = new { is_read = true };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(updateData);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            await httpClient.PatchAsync(url, content);

            await LoadNotifications();
        }
    }
}