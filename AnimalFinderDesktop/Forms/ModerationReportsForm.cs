using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AnimalFinderDesktop.Services;
using Newtonsoft.Json;

namespace AnimalFinderDesktop.Forms
{
    public partial class ModerationReportsForm : Form
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

        private FlowLayoutPanel flpReports;
        private Button btnRefresh;
        private TabControl tabControl;
        private List<dynamic> _reports;

        public ModerationReportsForm()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(1100, 750);
            this.MinimumSize = new Size(1100, 750);
            this.Text = "AnimalFinder - Модерация жалоб";
            this.BackColor = BackgroundColor;
            this.Font = new Font("Segoe UI", 9);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Load += async (s, e) => await LoadReports();
        }

        private void InitializeComponent()
        {
            // Верхняя панель с заголовком
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = CardColor
            };
            headerPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(BorderColor, 1);
                e.Graphics.DrawLine(pen, 0, headerPanel.Height - 1, headerPanel.Width, headerPanel.Height - 1);
            };

            var lblTitle = new Label
            {
                Text = "🛡️ Модерация жалоб",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = PrimaryColor,
                Location = new Point(20, 15),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblTitle);

            btnRefresh = CreateModernButton("🔄 Обновить", PrimaryColor, new Size(130, 36));
            btnRefresh.Location = new Point(940, 12);
            btnRefresh.Click += async (s, e) => await LoadReports();
            headerPanel.Controls.Add(btnRefresh);

            // Вкладки
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10)
            };

            // Вкладка "Жалобы на объявления"
            var tabListings = new TabPage("📋 На объявления");
            tabListings.BackColor = BackgroundColor;
            tabListings.Padding = new Padding(15);

            flpReports = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(10)
            };
            tabListings.Controls.Add(flpReports);

            // Вкладка "Жалобы на пользователей"
            var tabProfiles = new TabPage("👤 На пользователей");
            tabProfiles.BackColor = BackgroundColor;
            tabProfiles.Padding = new Padding(15);

            var flpProfileReports = new FlowLayoutPanel
            {
                Name = "flpProfileReports",
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(10)
            };
            tabProfiles.Controls.Add(flpProfileReports);

            // Вкладка "Рассмотренные"
            var tabResolved = new TabPage("✓ Рассмотренные");
            tabResolved.BackColor = BackgroundColor;
            tabResolved.Padding = new Padding(15);

            var flpResolved = new FlowLayoutPanel
            {
                Name = "flpResolved",
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(10)
            };
            tabResolved.Controls.Add(flpResolved);

            tabControl.TabPages.Add(tabListings);
            tabControl.TabPages.Add(tabProfiles);
            tabControl.TabPages.Add(tabResolved);

            tabControl.SelectedIndexChanged += (s, e) =>
            {
                if (tabControl.SelectedIndex == 2) // Рассмотренные
                {
                    _ = LoadResolvedReports();
                }
            };

            this.Controls.Add(tabControl);
            this.Controls.Add(headerPanel);
        }

        private async Task LoadReports()
        {
            try
            {
                btnRefresh.Enabled = false;
                btnRefresh.Text = "⏳ Загрузка...";

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");

                var url = "https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/reports?status=eq.pending&order=created_at.desc&limit=100";
                var response = await client.GetStringAsync(url);
                _reports = JsonConvert.DeserializeObject<List<dynamic>>(response) ?? new List<dynamic>();

                // Получаем информацию о пользователях и объявлениях
                var listingIds = new List<string>();
                var userIds = new List<string>();
                var reporterIds = new List<string>();

                foreach (var r in _reports)
                {
                    string targetId = r.target_id != null ? (string)r.target_id : ((string)r.listing_id ?? "");
                    string reportType = r.report_type != null ? (string)r.report_type : "listing";

                    if (reportType == "listing" && !string.IsNullOrEmpty(targetId))
                        listingIds.Add(targetId);
                    else if (reportType == "profile" && !string.IsNullOrEmpty(targetId))
                        userIds.Add(targetId);

                    if (r.user_id != null)
                        reporterIds.Add((string)r.user_id);
                }

                var listingsInfo = await GetListingsInfo(listingIds.Distinct().ToList());
                var usersInfo = await GetUsersInfo(userIds.Distinct().ToList());
                var reportersInfo = await GetUsersInfo(reporterIds.Distinct().ToList());

                // Очищаем вкладки
                flpReports.Controls.Clear();
                var flpProfileReports = tabControl.TabPages[1].Controls.Find("flpProfileReports", true).FirstOrDefault() as FlowLayoutPanel;
                flpProfileReports?.Controls.Clear();

                int listingCount = 0;
                int profileCount = 0;

                foreach (var r in _reports)
                {
                    string reportType = r.report_type != null ? (string)r.report_type : "listing";
                    string targetId = r.target_id != null ? (string)r.target_id : ((string)r.listing_id ?? "");

                    if (reportType == "listing")
                    {
                        var card = CreateListingReportCard(r, listingsInfo, reportersInfo);
                        if (card != null)
                        {
                            flpReports.Controls.Add(card);
                            listingCount++;
                        }
                    }
                    else if (reportType == "profile")
                    {
                        var card = CreateProfileReportCard(r, usersInfo, reportersInfo);
                        if (card != null && flpProfileReports != null)
                        {
                            flpProfileReports.Controls.Add(card);
                            profileCount++;
                        }
                    }
                }

                // Если нет жалоб
                if (listingCount == 0)
                {
                    flpReports.Controls.Add(CreateEmptyLabel("✅ Нет жалоб на объявления"));
                }
                if (profileCount == 0 && flpProfileReports != null)
                {
                    flpProfileReports.Controls.Add(CreateEmptyLabel("✅ Нет жалоб на пользователей"));
                }

                // Обновляем заголовки вкладок
                tabControl.TabPages[0].Text = $"📋 На объявления ({listingCount})";
                tabControl.TabPages[1].Text = $"👤 На пользователей ({profileCount})";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRefresh.Enabled = true;
                btnRefresh.Text = "🔄 Обновить";
            }
        }

        private async Task LoadResolvedReports()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");

                var url = "https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/reports?status=eq.resolved&order=created_at.desc&limit=50";
                var response = await client.GetStringAsync(url);
                var reports = JsonConvert.DeserializeObject<List<dynamic>>(response) ?? new List<dynamic>();

                var flpResolved = tabControl.TabPages[2].Controls.Find("flpResolved", true).FirstOrDefault() as FlowLayoutPanel;
                if (flpResolved == null) return;

                flpResolved.Controls.Clear();

                if (reports.Count == 0)
                {
                    flpResolved.Controls.Add(CreateEmptyLabel("📭 Нет рассмотренных жалоб"));
                    return;
                }

                foreach (var r in reports)
                {
                    var card = CreateResolvedReportCard(r);
                    flpResolved.Controls.Add(card);
                }

                tabControl.TabPages[2].Text = $"✓ Рассмотренные ({reports.Count})";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private Panel CreateListingReportCard(dynamic report, Dictionary<string, dynamic> listingsInfo, Dictionary<string, dynamic> reportersInfo)
        {
            string reportId = report.id;
            string listingId = report.target_id != null ? (string)report.target_id : ((string)report.listing_id ?? "");
            string reason = report.reason;
            string comment = report.comment;
            string reporterId = report.user_id;
            DateTime createdAt = report.created_at;

            // Получаем информацию об объявлении
            string listingInfo = "Объявление не найдено";
            string petName = "";
            if (listingsInfo.ContainsKey(listingId))
            {
                var listing = listingsInfo[listingId];
                petName = listing.pet_name ?? "";
                string breed = listing.breed ?? "";
                string species = listing.species ?? "";
                string location = listing.location ?? "";
                listingInfo = $"{petName} • {breed} • {species}\n📍 {location}";
            }

            // Получаем информацию о заявителе
            string reporterName = "Неизвестный";
            if (reportersInfo.ContainsKey(reporterId))
            {
                var reporter = reportersInfo[reporterId];
                reporterName = reporter.display_name ?? "Пользователь";
            }

            var card = new Panel
            {
                Width = 1020,
                Height = 180,
                BackColor = CardColor,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(15)
            };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            // Иконка
            var iconLabel = new Label
            {
                Text = "🚨",
                Font = new Font("Segoe UI", 24),
                Location = new Point(15, 15),
                Size = new Size(50, 50),
                TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(iconLabel);

            // Заголовок
            var lblTitle = new Label
            {
                Text = $"Жалоба на объявление: {petName}",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = PrimaryColor,
                Location = new Point(75, 15),
                AutoSize = true
            };
            card.Controls.Add(lblTitle);

            // Время
            var lblTime = new Label
            {
                Text = FormatTime(createdAt),
                Font = new Font("Segoe UI", 9),
                ForeColor = MutedColor,
                Location = new Point(850, 18),
                AutoSize = true
            };
            card.Controls.Add(lblTime);

            // Причина
            var lblReason = new Label
            {
                Text = $"⚠️ Причина: {reason}",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = DangerColor,
                Location = new Point(75, 45),
                AutoSize = true
            };
            card.Controls.Add(lblReason);

            // Комментарий
            var lblComment = new Label
            {
                Text = $"💬 Комментарий: {comment}",
                Font = new Font("Segoe UI", 9),
                ForeColor = TextColor,
                Location = new Point(75, 70),
                Size = new Size(600, 40),
                AutoSize = false
            };
            card.Controls.Add(lblComment);

            // Заявитель
            var lblReporter = new Label
            {
                Text = $"📨 Заявитель: {reporterName}",
                Font = new Font("Segoe UI", 9),
                ForeColor = MutedColor,
                Location = new Point(75, 115),
                AutoSize = true
            };
            card.Controls.Add(lblReporter);

            // Информация об объявлении
            var lblListingInfo = new Label
            {
                Text = listingInfo,
                Font = new Font("Segoe UI", 9),
                ForeColor = MutedColor,
                Location = new Point(75, 135),
                AutoSize = true
            };
            card.Controls.Add(lblListingInfo);

            // Кнопки действий
            int btnY = 130;
            int btnX = 680;

            var btnOpenListing = CreateModernButton("📋 Открыть", PrimaryColor, new Size(100, 32));
            btnOpenListing.Location = new Point(btnX, btnY);
            btnOpenListing.Click += async (s, e) => await OpenListing(listingId);
            card.Controls.Add(btnOpenListing);
            btnX += 110;

            var btnRejectReport = CreateModernButton("✕ Отклонить", MutedColor, new Size(110, 32));
            btnRejectReport.Location = new Point(btnX, btnY);
            btnRejectReport.Click += async (s, e) => await RejectReport(reportId);
            card.Controls.Add(btnRejectReport);
            btnX += 120;

            var btnDeleteListing = CreateModernButton("🗑️ Удалить объявление", DangerColor, new Size(160, 32));
            btnDeleteListing.Location = new Point(btnX, btnY);
            btnDeleteListing.Click += async (s, e) => await DeleteListing(reportId, listingId);
            card.Controls.Add(btnDeleteListing);

            return card;
        }

        private Panel CreateProfileReportCard(dynamic report, Dictionary<string, dynamic> usersInfo, Dictionary<string, dynamic> reportersInfo)
        {
            string reportId = report.id;
            string profileId = report.target_id != null ? (string)report.target_id : "";
            string reason = report.reason;
            string comment = report.comment;
            string reporterId = report.user_id;
            DateTime createdAt = report.created_at;

            // Получаем информацию о пользователе
            string userName = "Пользователь";
            string userEmail = "";
            string userRole = "user";
            if (usersInfo.ContainsKey(profileId))
            {
                var user = usersInfo[profileId];
                userName = user.display_name ?? "Пользователь";
                userEmail = user.email ?? "";
                userRole = user.role ?? "user";
            }

            // Получаем информацию о заявителе
            string reporterName = "Неизвестный";
            if (reportersInfo.ContainsKey(reporterId))
            {
                var reporter = reportersInfo[reporterId];
                reporterName = reporter.display_name ?? "Пользователь";
            }

            var card = new Panel
            {
                Width = 1020,
                Height = 180,
                BackColor = CardColor,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(15)
            };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            // Иконка
            var iconLabel = new Label
            {
                Text = "👤",
                Font = new Font("Segoe UI", 24),
                Location = new Point(15, 15),
                Size = new Size(50, 50),
                TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(iconLabel);

            // Заголовок
            var lblTitle = new Label
            {
                Text = $"Жалоба на пользователя: {userName}",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = PrimaryColor,
                Location = new Point(75, 15),
                AutoSize = true
            };
            card.Controls.Add(lblTitle);

            // Время
            var lblTime = new Label
            {
                Text = FormatTime(createdAt),
                Font = new Font("Segoe UI", 9),
                ForeColor = MutedColor,
                Location = new Point(850, 18),
                AutoSize = true
            };
            card.Controls.Add(lblTime);

            // Причина
            var lblReason = new Label
            {
                Text = $"⚠️ Причина: {reason}",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = DangerColor,
                Location = new Point(75, 45),
                AutoSize = true
            };
            card.Controls.Add(lblReason);

            // Комментарий
            var lblComment = new Label
            {
                Text = $"💬 Комментарий: {comment}",
                Font = new Font("Segoe UI", 9),
                ForeColor = TextColor,
                Location = new Point(75, 70),
                Size = new Size(600, 40),
                AutoSize = false
            };
            card.Controls.Add(lblComment);

            // Заявитель
            var lblReporter = new Label
            {
                Text = $"📨 Заявитель: {reporterName}",
                Font = new Font("Segoe UI", 9),
                ForeColor = MutedColor,
                Location = new Point(75, 115),
                AutoSize = true
            };
            card.Controls.Add(lblReporter);

            // Email пользователя
            var lblEmail = new Label
            {
                Text = $"📧 Email: {userEmail} | Роль: {userRole}",
                Font = new Font("Segoe UI", 9),
                ForeColor = MutedColor,
                Location = new Point(75, 135),
                AutoSize = true
            };
            card.Controls.Add(lblEmail);

            // Кнопки действий
            int btnY = 130;
            int btnX = 680;

            var btnOpenProfile = CreateModernButton("👤 Открыть профиль", PrimaryColor, new Size(140, 32));
            btnOpenProfile.Location = new Point(btnX, btnY);
            btnOpenProfile.Click += (s, e) => OpenProfile(profileId);
            card.Controls.Add(btnOpenProfile);
            btnX += 150;

            var btnRejectReport = CreateModernButton("✕ Отклонить", MutedColor, new Size(110, 32));
            btnRejectReport.Location = new Point(btnX, btnY);
            btnRejectReport.Click += async (s, e) => await RejectReport(reportId);
            card.Controls.Add(btnRejectReport);
            btnX += 120;

            var btnBanUser = CreateModernButton("🚫 Забанить", DangerColor, new Size(120, 32));
            btnBanUser.Location = new Point(btnX, btnY);
            btnBanUser.Click += async (s, e) => await BanUser(reportId, profileId);
            card.Controls.Add(btnBanUser);

            return card;
        }

        private Panel CreateResolvedReportCard(dynamic report)
        {
            string reportId = report.id;
            string reason = report.reason;
            string comment = report.comment;
            string reportType = report.report_type != null ? (string)report.report_type : "listing";
            DateTime createdAt = report.created_at;

            var card = new Panel
            {
                Width = 1020,
                Height = 100,
                BackColor = Color.FromArgb(248, 249, 250),
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(15)
            };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            var iconLabel = new Label
            {
                Text = "✓",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = SuccessColor,
                Location = new Point(15, 15),
                Size = new Size(50, 50),
                TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(iconLabel);

            var lblTitle = new Label
            {
                Text = $"Жалоба на {(reportType == "listing" ? "объявление" : "пользователя")}: {reason}",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = TextColor,
                Location = new Point(75, 15),
                AutoSize = true
            };
            card.Controls.Add(lblTitle);

            var lblComment = new Label
            {
                Text = comment,
                Font = new Font("Segoe UI", 9),
                ForeColor = MutedColor,
                Location = new Point(75, 45),
                Size = new Size(700, 40),
                AutoSize = false
            };
            card.Controls.Add(lblComment);

            var lblTime = new Label
            {
                Text = $"Рассмотрено: {createdAt.ToString("dd.MM.yyyy HH:mm")}",
                Font = new Font("Segoe UI", 9),
                ForeColor = MutedColor,
                Location = new Point(800, 15),
                AutoSize = true
            };
            card.Controls.Add(lblTime);

            return card;
        }

        private Label CreateEmptyLabel(string text)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 14),
                ForeColor = MutedColor,
                AutoSize = true,
                Margin = new Padding(10, 40, 0, 0)
            };
        }

        private async Task<Dictionary<string, dynamic>> GetListingsInfo(List<string> ids)
        {
            var result = new Dictionary<string, dynamic>();
            if (ids.Count == 0) return result;

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");

                var idList = string.Join(",", ids.Select(id => $"\"{id}\""));
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings?id=in.({idList})&select=id,pet_name,breed,species,location,user_id";
                var response = await client.GetStringAsync(url);
                var listings = JsonConvert.DeserializeObject<List<dynamic>>(response) ?? new List<dynamic>();

                foreach (var l in listings)
                {
                    result[(string)l.id] = l;
                }
            }
            catch { }
            return result;
        }

        private async Task<Dictionary<string, dynamic>> GetUsersInfo(List<string> ids)
        {
            var result = new Dictionary<string, dynamic>();
            if (ids.Count == 0) return result;

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");

                var idList = string.Join(",", ids.Select(id => $"\"{id}\""));
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/profiles?user_id=in.({idList})&select=user_id,display_name,role,email";
                var response = await client.GetStringAsync(url);
                var users = JsonConvert.DeserializeObject<List<dynamic>>(response) ?? new List<dynamic>();

                foreach (var u in users)
                {
                    result[(string)u.user_id] = u;
                }
            }
            catch { }
            return result;
        }

        private async Task OpenListing(string listingId)
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");

                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings?id=eq.{listingId}&select=*";
                var response = await client.GetStringAsync(url);
                var listings = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response);

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

        private void OpenProfile(string userId)
        {
            var profileForm = new ProfileForm(userId);
            profileForm.ShowDialog();
        }

        private async Task RejectReport(string reportId)
        {
            var result = MessageBox.Show("Отклонить жалобу? (Жалоба будет помечена как рассмотренная)", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            await UpdateReportStatus(reportId, "resolved");
            MessageBox.Show("✅ Жалоба отклонена", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadReports();
        }

        private async Task DeleteListing(string reportId, string listingId)
        {
            var result = MessageBox.Show(
                "⚠️ Удалить объявление?\n\nЭто действие нельзя отменить!",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");

                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings?id=eq.{listingId}";
                await client.DeleteAsync(url);
                await UpdateReportStatus(reportId, "resolved");

                MessageBox.Show("✅ Объявление удалено", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadReports();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private async Task BanUser(string reportId, string userId)
        {
            var result = MessageBox.Show(
                "🚫 ЗАБАНИТЬ ПОЛЬЗОВАТЕЛЯ?\n\n" +
                "• Все объявления будут удалены\n" +
                "• Пользователь не сможет войти\n" +
                "• Это действие нельзя отменить!\n\n" +
                "Вы уверены?",
                "Блокировка пользователя",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;

            try
            {
                // 1. Меняем роль на banned
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");

                var update = new { role = "banned" };
                var json = JsonConvert.SerializeObject(update);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/profiles?user_id=eq.{userId}";
                await client.PatchAsync(url, content);

                // 2. Удаляем все объявления пользователя
                var listingsUrl = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings?user_id=eq.{userId}";
                await client.DeleteAsync(listingsUrl);

                // 3. Удаляем пользователя из auth (через service_role key)
                try
                {
                    var serviceRoleKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imh0dXN1eHNqeHhzdWR6eHdqbnZ0Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc2NjE2NzkyNywiZXhwIjoyMDgxNzQzOTI3fQ.oERnxKvFqXnVkfK_xWcYQBvzJeqjXn4yUy_iQOpYXJI";
                    using var adminClient = new HttpClient();
                    adminClient.DefaultRequestHeaders.Add("apikey", serviceRoleKey);
                    adminClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {serviceRoleKey}");
                    await adminClient.DeleteAsync($"https://htusuxsjxxsudzxwjnvt.supabase.co/auth/v1/admin/users/{userId}");
                }
                catch { }

                await UpdateReportStatus(reportId, "resolved");
                MessageBox.Show("✅ Пользователь заблокирован и удалён", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadReports();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private async Task UpdateReportStatus(string reportId, string status)
        {
            using var client = new HttpClient();
            var update = new { status = status };
            var json = JsonConvert.SerializeObject(update);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/reports?id=eq.{reportId}";
            client.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
            await client.PatchAsync(url, content);
        }

        private string FormatTime(DateTime time)
        {
            var diff = DateTime.Now - time;
            if (diff.TotalMinutes < 1) return "только что";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} мин назад";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} ч назад";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} д назад";
            return time.ToString("dd.MM.yyyy HH:mm");
        }

        private Button CreateModernButton(string text, Color backColor, Size size)
        {
            var button = new Button
            {
                Text = text,
                Size = size,
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            return button;
        }
    }
}