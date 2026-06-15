using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace AnimalFinderDesktop.Forms
{
    public class MapPickerForm : Form
    {
        private WebView2 webView;
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public string Address { get; private set; }
        public bool IsLocationSelected { get; private set; }

        public MapPickerForm(double initialLat = 55.76, double initialLon = 37.64)
        {
            this.Text = "Выберите местоположение на карте";
            this.Size = new System.Drawing.Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;

            webView = new WebView2();
            webView.Dock = DockStyle.Fill;
            this.Controls.Add(webView);

            webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;

            string htmlPath = Path.Combine(Application.StartupPath, "map_picker.html");
            if (File.Exists(htmlPath))
                webView.Source = new Uri(htmlPath);
            else
                MessageBox.Show("Файл map_picker.html не найден!");
        }

        private void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
        }

        private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string json = e.TryGetWebMessageAsString();
                dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                Latitude = data.lat;
                Longitude = data.lon;
                Address = data.address;
                IsLocationSelected = true;

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
    }
}