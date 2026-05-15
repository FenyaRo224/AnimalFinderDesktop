using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using AnimalFinderDesktop.Services;

namespace AnimalFinderDesktop.Forms
{
    public partial class NotificationsForm : Form
    {
        private ListView lvNotifications;
        private Button btnMarkAllRead;

        public NotificationsForm()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(600, 500);
            this.Text = "Уведомления";
            LoadNotifications();
        }

        private void InitializeComponent()
        {
            lvNotifications = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.None
            };
            lvNotifications.Columns.Add("", 550);
            lvNotifications.MouseDoubleClick += async (s, e) => await MarkAsRead();

            btnMarkAllRead = new Button { Text = "Отметить все как прочитанные", Dock = DockStyle.Bottom, Height = 40 };
            btnMarkAllRead.Click += async (s, e) => await MarkAllRead();

            this.Controls.Add(lvNotifications);
            this.Controls.Add(btnMarkAllRead);
        }

        private async Task LoadNotifications()
        {
            var client = await SupabaseService.GetClient();
            var userId = client.Auth.CurrentUser?.Id;
            var notifs = await SupabaseService.GetAllNotifications(userId);
            lvNotifications.Items.Clear();
            foreach (var n in notifs)
            {
                string title = n.title;
                string message = n.message;
                bool isRead = n.is_read;
                var item = new ListViewItem($"{title}: {message}");
                if (!isRead) item.BackColor = Color.LightYellow;
                item.Tag = n.id;
                lvNotifications.Items.Add(item);
            }
        }

        private async Task MarkAsRead()
        {
            if (lvNotifications.SelectedItems.Count == 0) return;
            var id = lvNotifications.SelectedItems[0].Tag.ToString();
            await SupabaseService.MarkNotificationRead(id);
            await LoadNotifications();
        }

        private async Task MarkAllRead()
        {
            var client = await SupabaseService.GetClient();
            var userId = client.Auth.CurrentUser?.Id;
            var notifs = await SupabaseService.GetUnreadNotifications(userId);
            foreach (var n in notifs)
                await SupabaseService.MarkNotificationRead(n.id);
            await LoadNotifications();
        }
    }
}