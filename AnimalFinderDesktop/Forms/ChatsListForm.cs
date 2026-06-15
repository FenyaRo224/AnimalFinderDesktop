using AnimalFinderDesktop.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AnimalFinderDesktop.Forms
{
    public class ChatsListForm : Form
    {
        private FlowLayoutPanel flpChats;
        private System.Windows.Forms.Timer refreshTimer;

        public ChatsListForm()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(450, 600);
            this.MinimumSize = new Size(450, 600);
            this.MaximumSize = new Size(450, 600);
            this.Text = "AnimalFinder - Сообщения";
            this.BackColor = Color.White;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.ControlBox = true;
            this.Shown += async (s, e) => await LoadDialogs();

            refreshTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            refreshTimer.Tick += async (s, e) => await LoadDialogs();
            refreshTimer.Start();
        }

        private void InitializeComponent()
        {
            flpChats = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(240, 242, 245)
            };

            this.Controls.Add(flpChats);
        }

        private async Task LoadDialogs()
        {
            try
            {
                var client = await SupabaseService.GetClient();
                var userId = client.Auth.CurrentUser?.Id;

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");

                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/messages?or=(from_user_id.eq.{userId},to_user_id.eq.{userId})&order=created_at.desc&limit=100";
                var response = await httpClient.GetStringAsync(url);
                var messages = JsonConvert.DeserializeObject<List<dynamic>>(response) ?? new List<dynamic>();

                var dialogs = messages
                    .GroupBy(m => (string)(m.from_user_id.ToString() == userId ? m.to_user_id.ToString() : m.from_user_id.ToString()))
                    .Select(g => new
                    {
                        UserId = g.Key,
                        LastMessage = g.First().message.ToString(),
                        LastDate = (DateTime)g.First().created_at,
                        UnreadCount = g.Count(m => m.is_read == false && m.to_user_id.ToString() == userId),
                        ListingId = g.First().listing_id?.ToString()
                    })
                    .OrderByDescending(d => d.LastDate)
                    .ToList();

                flpChats.Controls.Clear();
                foreach (var d in dialogs)
                {
                    var userInfo = await GetUserInfo(d.UserId);
                    var card = CreateChatCard(d.UserId, d.LastMessage, d.LastDate, d.UnreadCount, userInfo.name, userInfo.avatar);
                    flpChats.Controls.Add(card);
                }

                if (dialogs.Count == 0)
                {
                    var emptyLabel = new Label
                    {
                        Text = "📭 Нет сообщений",
                        Font = new Font("Segoe UI", 14),
                        ForeColor = Color.Gray,
                        AutoSize = true,
                        Margin = new Padding(10, 20, 0, 0)
                    };
                    flpChats.Controls.Add(emptyLabel);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadDialogs error: {ex.Message}");
            }
        }

        private async Task<(string name, string avatar)> GetUserInfo(string userId)
        {
            try
            {
                using var httpClient = new HttpClient();
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/profiles?user_id=eq.{userId}&select=display_name,avatar_url";
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                var response = await httpClient.GetStringAsync(url);
                var profiles = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response);
                if (profiles != null && profiles.Count > 0)
                {
                    string name = profiles[0].ContainsKey("display_name") ? profiles[0]["display_name"]?.ToString() : "Пользователь";
                    string avatar = profiles[0].ContainsKey("avatar_url") ? profiles[0]["avatar_url"]?.ToString() : "";
                    return (name, avatar);
                }
                return ("Пользователь", "");
            }
            catch { return ("Пользователь", ""); }
        }

        private Panel CreateChatCard(string userId, string lastMessage, DateTime lastDate, int unreadCount, string userName, string avatarPath)
        {
            var card = new Panel
            {
                Width = 400,
                Height = 70,
                BackColor = unreadCount > 0 ? Color.FromArgb(255, 255, 200) : Color.White,
                Margin = new Padding(0, 0, 0, 8),
                Padding = new Padding(10),
                Cursor = Cursors.Hand,
                Tag = userId
            };

            card.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle,
                    Color.LightGray, 1, ButtonBorderStyle.Solid,
                    Color.LightGray, 0, ButtonBorderStyle.Solid,
                    Color.LightGray, 0, ButtonBorderStyle.Solid,
                    Color.LightGray, 0, ButtonBorderStyle.Solid);
            };

            // Аватар
            var pbAvatar = new PictureBox
            {
                Size = new Size(50, 50),
                Location = new Point(10, 10),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(0, 122, 204)
            };

            if (!string.IsNullOrEmpty(avatarPath) && File.Exists(Path.Combine(Application.StartupPath, avatarPath)))
            {
                try { pbAvatar.Image = Image.FromFile(Path.Combine(Application.StartupPath, avatarPath)); }
                catch { pbAvatar.Image = GetDefaultAvatar(); }
            }
            else
            {
                pbAvatar.Image = GetDefaultAvatar();
            }

            // Имя
            var lblName = new Label
            {
                Text = userName,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Location = new Point(70, 10),
                AutoSize = true
            };

            // Последнее сообщение (обрезаем длинные)
            string displayMessage = lastMessage.Length > 50 ? lastMessage.Substring(0, 47) + "..." : lastMessage;
            if (displayMessage.StartsWith("[IMAGE]")) displayMessage = "📷 Изображение";

            var lblMessage = new Label
            {
                Text = displayMessage,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray,
                Location = new Point(70, 32),
                AutoSize = true,
                MaximumSize = new Size(240, 0)
            };

            // Время
            var lblTime = new Label
            {
                Text = FormatTime(lastDate),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray,
                Location = new Point(340, 10),
                AutoSize = true
            };

            card.Controls.Add(pbAvatar);
            card.Controls.Add(lblName);
            card.Controls.Add(lblMessage);
            card.Controls.Add(lblTime);

            // Счётчик непрочитанных
            if (unreadCount > 0)
            {
                var unreadBadge = new Label
                {
                    Text = unreadCount.ToString(),
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(220, 53, 69),
                    Size = new Size(24, 24),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Location = new Point(365, 35),
                    FlatStyle = FlatStyle.Flat
                };
                card.Controls.Add(unreadBadge);
            }

            card.Click += async (s, e) =>
            {
                // Отмечаем сообщения как прочитанные
                await MarkMessagesAsRead(userId);
                var chatForm = new ChatForm(userId);
                if (chatForm.ShowDialog() == DialogResult.OK)
                {
                    // Если переписка была удалена, обновляем список
                    await LoadDialogs();
                }
                else
                {
                    await LoadDialogs();
                }
            };

            return card;
        }

        private async Task MarkMessagesAsRead(string fromUserId)
        {
            try
            {
                var client = await SupabaseService.GetClient();
                var userId = client.Auth.CurrentUser?.Id;

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");

                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/messages?from_user_id=eq.{fromUserId}&to_user_id=eq.{userId}&is_read=eq.false";
                var updateData = new { is_read = true };
                var json = JsonConvert.SerializeObject(updateData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await httpClient.PatchAsync(url, content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MarkMessagesAsRead error: {ex.Message}");
            }
        }

        private Image GetDefaultAvatar()
        {
            Bitmap bmp = new Bitmap(50, 50);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(0, 122, 204));
                using (Font font = new Font("Segoe UI", 20, FontStyle.Bold))
                {
                    g.DrawString("🐾", font, Brushes.White, 12, 10);
                }
            }
            return bmp;
        }

        private string FormatTime(DateTime time)
        {
            var diff = DateTime.Now - time;
            if (diff.TotalMinutes < 1) return "только что";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} мин";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} ч";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} д";
            return time.ToString("dd.MM");
        }
    }
}