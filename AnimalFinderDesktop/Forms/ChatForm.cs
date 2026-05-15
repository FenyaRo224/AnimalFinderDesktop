using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using AnimalFinderDesktop.Services;

namespace AnimalFinderDesktop.Forms
{
    public partial class ChatForm : Form
    {
        private string _toUserId;
        private string _listingId;
        private string _currentUserId;
        private ListView lvMessages;
        private TextBox tbMessage;
        private Button btnSend;
        private System.Windows.Forms.Timer refreshTimer;
        public ChatForm(string toUserId, string listingId = null)
        {
            _toUserId = toUserId;
            _listingId = listingId;
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(500, 600);
            this.Text = "Чат";
            LoadMessages();
            refreshTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            refreshTimer.Tick += async (s, e) => await LoadMessages();
            refreshTimer.Start();
        }

        private void InitializeComponent()
        {
            lvMessages = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = false,
                HeaderStyle = ColumnHeaderStyle.None,
                ShowGroups = false,
                LabelWrap = true
            };
            lvMessages.Columns.Add("", 450);
            lvMessages.OwnerDraw = true;
            lvMessages.DrawItem += LvMessages_DrawItem;

            tbMessage = new TextBox { Dock = DockStyle.Bottom, Height = 60, Multiline = true };
            btnSend = new Button { Text = "Отправить", Dock = DockStyle.Bottom, Height = 35, BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White };

            btnSend.Click += async (s, e) => await SendMessage();

            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 100 };
            bottomPanel.Controls.Add(tbMessage);
            bottomPanel.Controls.Add(btnSend);
            tbMessage.Dock = DockStyle.Top;
            btnSend.Dock = DockStyle.Bottom;

            this.Controls.Add(lvMessages);
            this.Controls.Add(bottomPanel);
        }

        private void LvMessages_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawBackground();
            var item = e.Item;
            var text = item.Text;
            var font = new Font("Segoe UI", 10);
            var color = item.ForeColor;
            TextRenderer.DrawText(e.Graphics, text, font, e.Bounds, color, TextFormatFlags.WordBreak);
        }

        private async Task LoadMessages()
        {
            try
            {
                var client = await SupabaseService.GetClient();
                _currentUserId = client.Auth.CurrentUser?.Id;
                var messages = await SupabaseService.GetMessages(_currentUserId, _toUserId, _listingId);

                lvMessages.Items.Clear();

                if (messages != null && messages.Count > 0)
                {
                    foreach (var msg in messages)
                    {
                        string fromId = msg.from_user_id;
                        string text = msg.message;
                        bool isMine = fromId == _currentUserId;

                        var item = new ListViewItem(text);
                        item.ForeColor = isMine ? Color.Blue : Color.Black;
                        item.Font = new Font("Segoe UI", 10);
                        lvMessages.Items.Add(item);
                    }

                    if (lvMessages.Items.Count > 0)
                        lvMessages.EnsureVisible(lvMessages.Items.Count - 1);
                }
                else
                {
                    lvMessages.Items.Add(new ListViewItem("Нет сообщений. Напишите первое!"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadMessages error: {ex.Message}");
                lvMessages.Items.Clear();
                lvMessages.Items.Add(new ListViewItem("Ошибка загрузки сообщений"));
            }
        }

        private async Task SendMessage()
        {
            if (string.IsNullOrWhiteSpace(tbMessage.Text)) return;
            var client = await SupabaseService.GetClient();
            _currentUserId = client.Auth.CurrentUser?.Id;
            var success = await SupabaseService.SendMessage(_currentUserId, _toUserId, tbMessage.Text, _listingId);
            if (success)
            {
                tbMessage.Clear();
                await LoadMessages();
                // Отправить уведомление получателю
                await SupabaseService.SendNotification(_toUserId, "Новое сообщение", $"Вам написал пользователь", "message");
            }
        }
    }
}