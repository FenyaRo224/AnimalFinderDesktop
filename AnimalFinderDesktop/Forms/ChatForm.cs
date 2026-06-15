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
    public class ChatForm : Form
    {
        private string _toUserId;
        private string _listingId;
        private string _listingName;
        private string _currentUserId;
        private string _toUserName;
        private string _toUserAvatar;

        private Panel pnlHeader;
        private PictureBox pbAvatar;
        private Label lblName;
        private Button btnBack;
        private Button btnMenu;
        private FlowLayoutPanel flpMessages;
        private Panel pnlInput;
        private TextBox tbMessage;
        private Button btnSend;
        private Button btnAttach;
        private System.Windows.Forms.Timer refreshTimer;

        public ChatForm(string toUserId, string listingId = null, string listingName = null)
        {
            _toUserId = toUserId;
            _listingId = listingId;
            _listingName = listingName;
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(500, 700);
            this.MinimumSize = new Size(500, 700);
            this.MaximumSize = new Size(500, 700);
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.ControlBox = true;
            this.Text = "Загрузка...";
            this.Shown += async (s, e) => await LoadChatData();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.White;

            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(0, 122, 204),
                Padding = new Padding(10)
            };

            btnBack = new Button
            {
                Text = "←",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(40, 40),
                Location = new Point(10, 15)
            };
            btnBack.Click += (s, e) => this.Close();

            pbAvatar = new PictureBox
            {
                Size = new Size(40, 40),
                Location = new Point(60, 15),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(0, 100, 180),
                Cursor = Cursors.Hand
            };
            pbAvatar.Click += (s, e) => OpenUserProfile();

            lblName = new Label
            {
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(110, 25),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            lblName.Click += (s, e) => OpenUserProfile();

            // Кнопка меню (три точки)
            btnMenu = new Button
            {
                Text = "⋮",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(40, 40),
                Location = new Point(440, 15),
                Cursor = Cursors.Hand
            };
            btnMenu.FlatAppearance.BorderSize = 0;
            btnMenu.Click += BtnMenu_Click;

            pnlHeader.Controls.Add(btnBack);
            pnlHeader.Controls.Add(pbAvatar);
            pnlHeader.Controls.Add(lblName);
            pnlHeader.Controls.Add(btnMenu);

            flpMessages = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(240, 242, 245)
            };

            pnlInput = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                BackColor = Color.White,
                Padding = new Padding(5)
            };

            btnAttach = new Button
            {
                Text = "📎",
                Font = new Font("Segoe UI", 12),
                Size = new Size(40, 40),
                Location = new Point(5, 20),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White
            };
            btnAttach.Click += BtnAttach_Click;

            tbMessage = new TextBox
            {
                Location = new Point(50, 15),
                Size = new Size(340, 50),
                Font = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.FixedSingle,
                Multiline = true,
                MaxLength = 500
            };
            tbMessage.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !e.Shift)
                {
                    e.SuppressKeyPress = true;
                    _ = SendMessage();
                }
            };

            btnSend = new Button
            {
                Text = "Отправить",
                Size = new Size(80, 50),
                Location = new Point(395, 15),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnSend.Click += async (s, e) => await SendMessage();

            pnlInput.Controls.Add(btnAttach);
            pnlInput.Controls.Add(tbMessage);
            pnlInput.Controls.Add(btnSend);

            this.Controls.Add(flpMessages);
            this.Controls.Add(pnlInput);
            this.Controls.Add(pnlHeader);
        }

        private void OpenUserProfile()
        {
            var profileForm = new ProfileForm(_toUserId);
            profileForm.ShowDialog();
        }

        private void BtnMenu_Click(object sender, EventArgs e)
        {
            var menu = new ContextMenuStrip();

            var deleteItem = new ToolStripMenuItem("🗑️ Удалить всю переписку");
            deleteItem.Click += async (s, ev) => await DeleteChatHistory();
            menu.Items.Add(deleteItem);

            menu.Show(btnMenu, new Point(0, btnMenu.Height));
        }

        private async Task DeleteChatHistory()
        {
            var result = MessageBox.Show(
                "⚠️ Удалить всю переписку с этим пользователем?\n\nЭто действие нельзя отменить!",
                "Удаление переписки",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");

                // Удаляем все сообщения между этими пользователями
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/messages?or=(and(from_user_id.eq.{_currentUserId},to_user_id.eq.{_toUserId}),and(from_user_id.eq.{_toUserId},to_user_id.eq.{_currentUserId}))";
                var response = await httpClient.DeleteAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("✅ Переписка удалена", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Ошибка удаления: {error}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private async void BtnAttach_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog();
            ofd.Filter = "Изображения|*.jpg;*.jpeg;*.png;*.gif;*.bmp";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                await SendImage(ofd.FileName);
            }
        }

        private async Task SendImage(string imagePath)
        {
            try
            {
                string chatImagesDir = Path.Combine(Application.StartupPath, "ChatImages");
                if (!Directory.Exists(chatImagesDir)) Directory.CreateDirectory(chatImagesDir);

                string ext = Path.GetExtension(imagePath);
                string fileName = $"{Guid.NewGuid()}{ext}";
                string destPath = Path.Combine(chatImagesDir, fileName);
                File.Copy(imagePath, destPath, true);
                string relativePath = $"ChatImages/{fileName}";

                var msg = new
                {
                    from_user_id = _currentUserId,
                    to_user_id = _toUserId,
                    message = $"[IMAGE]{relativePath}[/IMAGE]",
                    listing_id = string.IsNullOrEmpty(_listingId) ? null : _listingId,
                    is_read = false,
                    created_at = DateTime.UtcNow
                };

                using var httpClient = new HttpClient();
                var json = JsonConvert.SerializeObject(msg);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = "https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/messages";
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                await httpClient.PostAsync(url, content);

                await LoadMessages();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отправки изображения: {ex.Message}");
            }
        }

        private async Task LoadChatData()
        {
            try
            {
                var client = await SupabaseService.GetClient();
                _currentUserId = client.Auth.CurrentUser?.Id;

                if (string.IsNullOrEmpty(_currentUserId))
                {
                    MessageBox.Show("Ошибка: пользователь не авторизован");
                    this.Close();
                    return;
                }

                await LoadUserInfo();

                string title = string.IsNullOrEmpty(_listingName) ? _toUserName : $"{_listingName}";
                this.Text = title;
                lblName.Text = _toUserName;

                await LoadAvatar();
                await LoadMessages();

                refreshTimer = new System.Windows.Forms.Timer { Interval = 3000 };
                refreshTimer.Tick += async (s, e) => await LoadMessages();
                refreshTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки чата: {ex.Message}");
            }
        }

        private async Task LoadUserInfo()
        {
            try
            {
                using var httpClient = new HttpClient();
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/profiles?user_id=eq.{_toUserId}&select=display_name,avatar_url";
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                var response = await httpClient.GetStringAsync(url);
                var profiles = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response);
                if (profiles != null && profiles.Count > 0)
                {
                    _toUserName = profiles[0].ContainsKey("display_name") ? profiles[0]["display_name"]?.ToString() : "Пользователь";
                    _toUserAvatar = profiles[0].ContainsKey("avatar_url") ? profiles[0]["avatar_url"]?.ToString() : "";
                }
                else
                {
                    _toUserName = "Пользователь";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadUserInfo error: {ex.Message}");
                _toUserName = "Пользователь";
            }
        }

        private async Task LoadAvatar()
        {
            if (!string.IsNullOrEmpty(_toUserAvatar) && File.Exists(Path.Combine(Application.StartupPath, _toUserAvatar)))
            {
                try
                {
                    pbAvatar.Image = Image.FromFile(Path.Combine(Application.StartupPath, _toUserAvatar));
                    return;
                }
                catch { }
            }
            pbAvatar.Image = GetDefaultAvatar();
        }

        private Image GetDefaultAvatar()
        {
            Bitmap bmp = new Bitmap(40, 40);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(0, 100, 180));
                using (Font font = new Font("Segoe UI", 16, FontStyle.Bold))
                {
                    g.DrawString("🐾", font, Brushes.White, 8, 5);
                }
            }
            return bmp;
        }

        private async Task LoadMessages()
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");

                var allMessages = new List<dynamic>();

                var url1 = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/messages?from_user_id=eq.{_currentUserId}&to_user_id=eq.{_toUserId}&order=created_at.asc";
                var response1 = await httpClient.GetStringAsync(url1);
                var messages1 = JsonConvert.DeserializeObject<List<dynamic>>(response1);
                if (messages1 != null && messages1.Count > 0)
                    allMessages.AddRange(messages1);

                var url2 = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/messages?from_user_id=eq.{_toUserId}&to_user_id=eq.{_currentUserId}&order=created_at.asc";
                var response2 = await httpClient.GetStringAsync(url2);
                var messages2 = JsonConvert.DeserializeObject<List<dynamic>>(response2);
                if (messages2 != null && messages2.Count > 0)
                    allMessages.AddRange(messages2);

                allMessages = allMessages.OrderBy(m => (DateTime)m.created_at).ToList();

                flpMessages.Controls.Clear();

                if (allMessages.Count == 0)
                {
                    var emptyLabel = new Label
                    {
                        Text = "Нет сообщений. Напишите первое!",
                        Font = new Font("Segoe UI", 12),
                        ForeColor = Color.Gray,
                        AutoSize = true,
                        Margin = new Padding(10, 20, 0, 0)
                    };
                    flpMessages.Controls.Add(emptyLabel);
                }
                else
                {
                    foreach (var msg in allMessages)
                    {
                        bool isMine = msg.from_user_id.ToString() == _currentUserId;
                        string messageText = msg.message.ToString();
                        Control messageControl;

                        if (messageText.StartsWith("[IMAGE]") && messageText.EndsWith("[/IMAGE]"))
                        {
                            string imagePath = messageText.Substring(7, messageText.Length - 15);
                            messageControl = CreateImageBubble(imagePath, isMine, (DateTime)msg.created_at);
                        }
                        else
                        {
                            messageControl = CreateMessageBubble(messageText, isMine, (DateTime)msg.created_at);
                        }
                        flpMessages.Controls.Add(messageControl);
                    }

                    flpMessages.ScrollControlIntoView(flpMessages.Controls[flpMessages.Controls.Count - 1]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadMessages error: {ex.Message}");
            }
        }

        private Control CreateMessageBubble(string text, bool isMine, DateTime time)
        {
            var container = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0),
                Margin = new Padding(5, 3, 5, 3)
            };
            container.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            container.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var messageLabel = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 10),
                MaximumSize = new Size(300, 0),
                AutoSize = true,
                BackColor = isMine ? Color.FromArgb(0, 122, 204) : Color.White,
                ForeColor = isMine ? Color.White : Color.Black,
                Padding = new Padding(10, 8, 10, 8),
                Margin = new Padding(0)
            };

            var timeLabel = new Label
            {
                Text = time.ToString("HH:mm"),
                Font = new Font("Segoe UI", 7),
                ForeColor = Color.Gray,
                AutoSize = true,
                Margin = new Padding(5, 0, 5, 0)
            };

            if (isMine)
            {
                container.Controls.Add(timeLabel, 0, 0);
                container.Controls.Add(messageLabel, 1, 0);
                container.Dock = DockStyle.Right;
            }
            else
            {
                container.Controls.Add(messageLabel, 0, 0);
                container.Controls.Add(timeLabel, 1, 0);
                container.Dock = DockStyle.Left;
            }

            return container;
        }

        private Control CreateImageBubble(string imagePath, bool isMine, DateTime time)
        {
            var container = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0),
                Margin = new Padding(5, 3, 5, 3)
            };
            container.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            container.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            string fullPath = Path.Combine(Application.StartupPath, imagePath);
            var pictureBox = new PictureBox
            {
                Size = new Size(200, 150),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.LightGray
            };

            if (File.Exists(fullPath))
            {
                try { pictureBox.Image = Image.FromFile(fullPath); }
                catch { pictureBox.BackColor = Color.Gray; }
            }

            var timeLabel = new Label
            {
                Text = time.ToString("HH:mm"),
                Font = new Font("Segoe UI", 7),
                ForeColor = Color.Gray,
                AutoSize = true,
                Margin = new Padding(5, 0, 5, 0)
            };

            if (isMine)
            {
                container.Controls.Add(timeLabel, 0, 0);
                container.Controls.Add(pictureBox, 1, 0);
                container.Dock = DockStyle.Right;
            }
            else
            {
                container.Controls.Add(pictureBox, 0, 0);
                container.Controls.Add(timeLabel, 1, 0);
                container.Dock = DockStyle.Left;
            }

            return container;
        }

        private async Task SendMessage()
        {
            if (string.IsNullOrWhiteSpace(tbMessage.Text)) return;

            string messageText = tbMessage.Text.Trim();

            var msg = new
            {
                from_user_id = _currentUserId,
                to_user_id = _toUserId,
                message = messageText,
                listing_id = string.IsNullOrEmpty(_listingId) ? null : _listingId,
                is_read = false,
                created_at = DateTime.UtcNow
            };

            using var httpClient = new HttpClient();
            var json = JsonConvert.SerializeObject(msg);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = "https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/messages";
            httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
            var response = await httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                tbMessage.Text = "";
                await LoadMessages();

                await SupabaseService.SendNotification(_toUserId, "Новое сообщение", messageText, "message", _listingId);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                MessageBox.Show($"Ошибка отправки: {error}");
            }
        }
    }
}