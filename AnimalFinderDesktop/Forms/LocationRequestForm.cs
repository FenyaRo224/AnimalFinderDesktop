using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.IO;
using System.Windows.Forms;

namespace AnimalFinderDesktop.Forms
{
    public class LocationRequestForm : Form
    {
        private WebView2 webView;
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public bool Success { get; private set; }

        public LocationRequestForm()
        {
            this.Text = "Определение местоположения";
            this.Size = new System.Drawing.Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            webView = new WebView2();
            webView.Dock = DockStyle.Fill;
            this.Controls.Add(webView);

            webView.CoreWebView2InitializationCompleted += OnWebViewInitialized;
            webView.Source = new Uri("about:blank");
        }

        private async void OnWebViewInitialized(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            await webView.EnsureCoreWebView2Async(null);
            webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

            string html = @"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
                <title>Геолокация</title>
                <style>
                    body { font-family: Arial; text-align: center; padding: 50px; }
                    button { padding: 10px 20px; font-size: 16px; background: #007bff; color: white; border: none; border-radius: 5px; cursor: pointer; }
                    #status { margin-top: 20px; color: #666; }
                </style>
            </head>
            <body>
                <h3>Определение вашего местоположения</h3>
                <button id='btnRequest' onclick='requestLocation()'>Разрешить и определить</button>
                <div id='status'></div>
                <script>
                    function requestLocation() {
                        document.getElementById('status').innerHTML = 'Запрос разрешения...';
                        if (navigator.geolocation) {
                            navigator.geolocation.getCurrentPosition(success, error);
                        } else {
                            document.getElementById('status').innerHTML = 'Геолокация не поддерживается вашим браузером.';
                        }
                    }
                    function success(position) {
                        var lat = position.coords.latitude;
                        var lon = position.coords.longitude;
                        window.chrome.webview.postMessage(JSON.stringify({ lat: lat, lon: lon, success: true }));
                    }
                    function error(err) {
                        window.chrome.webview.postMessage(JSON.stringify({ success: false, message: err.message }));
                    }
                </script>
            </body>
            </html>";

            webView.CoreWebView2.NavigateToString(html);
        }

        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string json = e.TryGetWebMessageAsString();
            dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
            if (data.success == true)
            {
                Latitude = data.lat;
                Longitude = data.lon;
                Success = true;
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                Success = false;
                MessageBox.Show("Не удалось определить местоположение: " + data.message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.Cancel;
            }
            this.Close();
        }
    }
}