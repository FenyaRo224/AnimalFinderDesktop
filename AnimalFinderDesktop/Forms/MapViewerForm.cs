using AnimalFinderDesktop.Services;
using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AnimalFinderDesktop.Forms
{
    public class MapViewerForm : Form
    {
        private WebView2 webView;
        private List<Dictionary<string, object>> listingsData;

        public MapViewerForm(List<Dictionary<string, object>> listings)
        {
            listingsData = listings;
            Text = "Карта объявлений";
            Size = new System.Drawing.Size(1000, 700);  // полное имя, чтобы избежать конфликта с QuestPDF
            StartPosition = FormStartPosition.CenterParent;
            WindowState = FormWindowState.Maximized;

            if (listingsData == null || listingsData.Count == 0)
            {
                MessageBox.Show("Нет объявлений с координатами.", "Информация");
                Close();
                return;
            }

            webView = new WebView2();
            webView.Dock = DockStyle.Fill;
            Controls.Add(webView);
            webView.CoreWebView2InitializationCompleted += OnCoreWebView2InitializationCompleted;
            webView.Source = new Uri("about:blank");
        }

        private async void OnCoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            await webView.EnsureCoreWebView2Async(null);
            webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

            // Загружаем HTML из файла (файл должен быть в папке с программой)
            string htmlFilePath = Path.Combine(Application.StartupPath, "map_viewer.html");
            string html;
            if (File.Exists(htmlFilePath))
                html = File.ReadAllText(htmlFilePath);
            else
            {
                // Если файл не найден – показываем сообщение и закрываем форму
                MessageBox.Show("Не найден файл map_viewer.html", "Ошибка");
                Close();
                return;
            }

            webView.CoreWebView2.NavigateToString(html);

            // Подготавливаем данные для отправки в JavaScript
            var data = new List<object>();
            foreach (var item in listingsData)
            {
                string photoBase64 = "";
                if (item.ContainsKey("photo_urls") && item["photo_urls"] != null)
                {
                    string urls = item["photo_urls"].ToString();
                    string first = urls.Split(';')[0];
                    string fullPath = Path.Combine(Application.StartupPath, first);
                    if (File.Exists(fullPath))
                    {
                        byte[] bytes = File.ReadAllBytes(fullPath);
                        photoBase64 = "data:image/jpeg;base64," + Convert.ToBase64String(bytes);
                    }
                }

                data.Add(new
                {
                    id = item["id"]?.ToString(),
                    pet_name = item["pet_name"]?.ToString(),
                    species = item["species"]?.ToString(),
                    breed = item["breed"]?.ToString(),
                    listing_type = item["listing_type"]?.ToString(),
                    location = item["location"]?.ToString(),
                    photo = photoBase64,
                    lat = Convert.ToDouble(item["latitude"]),
                    lon = Convert.ToDouble(item["longitude"])
                });
            }

            string json = JsonConvert.SerializeObject(data);
            await Task.Delay(300);
            webView.CoreWebView2.PostWebMessageAsJson(json);
        }

        private void OnWebMessageReceived(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            string message = e.TryGetWebMessageAsString();
            if (!string.IsNullOrEmpty(message) && message.StartsWith("open:"))
            {
                string listingId = message.Substring(5);
                OpenDetailForm(listingId);
            }
        }

        private async void OpenDetailForm(string listingId)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("apikey", SupabaseService.SupabaseKey);
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseService.SupabaseKey}");
                    string url = $"https://htusuxsjxxsudzxwjnvt.supabase.co/rest/v1/pet_listings?id=eq.{listingId}&select=*";
                    string response = await httpClient.GetStringAsync(url);
                    var listings = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response);
                    if (listings != null && listings.Count > 0)
                    {
                        var detailForm = new DetailForm(listings[0]);
                        detailForm.ShowDialog();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии деталей: {ex.Message}");
            }
        }
    }
}