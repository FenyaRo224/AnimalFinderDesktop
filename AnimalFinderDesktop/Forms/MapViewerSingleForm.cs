using System;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace AnimalFinderDesktop.Forms
{
    public class MapViewerSingleForm : Form
    {
        private WebView2 webView;

        public MapViewerSingleForm(double lat, double lon, string title, string address)
        {
            this.Text = $"Местоположение: {title}";
            this.Size = new System.Drawing.Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;

            webView = new WebView2();
            webView.Dock = DockStyle.Fill;
            this.Controls.Add(webView);

            webView.CoreWebView2InitializationCompleted += async (s, e) =>
            {
                await webView.EnsureCoreWebView2Async(null);
                string html = GenerateHtml(lat, lon, title, address);
                webView.CoreWebView2.NavigateToString(html);
            };
            webView.Source = new Uri("about:blank");
        }

        private string GenerateHtml(double lat, double lon, string title, string address)
        {
            string latStr = lat.ToString().Replace(",", ".");
            string lonStr = lon.ToString().Replace(",", ".");
            string titleSafe = title.Replace("'", "\\'");
            string addressSafe = address.Replace("'", "\\'");

            return @"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
                <title>Местоположение</title>
                <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />
                <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
                <style>
                    html, body, #map { width: 100%; height: 100%; margin: 0; padding: 0; }
                </style>
            </head>
            <body>
                <div id='map'></div>
                <script>
                    var map = L.map('map').setView([" + latStr + ", " + lonStr + @"], 15);
                    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                        attribution: '© OpenStreetMap contributors'
                    }).addTo(map);
                    L.marker([" + latStr + ", " + lonStr + @"]).addTo(map)
                        .bindPopup('<b>" + titleSafe + @"</b><br>" + addressSafe + @"')
                        .openPopup();
                </script>
            </body>
            </html>";
        }
    }
}