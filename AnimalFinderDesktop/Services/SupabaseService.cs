using Supabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Windows;

namespace AnimalFinderDesktop.Services
{
    public static class SupabaseService
    {
        private static Client _client;
        private static readonly string SupabaseUrl = "https://htusuxsjxxsudzxwjnvt.supabase.co";
        public static readonly string SupabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imh0dXN1eHNqeHhzdWR6eHdqbnZ0Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjYxNjc5MjcsImV4cCI6MjA4MTc0MzkyN30.pBVCYJIGIYq71vQmBjxCrAYEOS8oqrphd16xCtTdABA";

        public static async Task<Client> GetClient()
        {
            if (_client == null)
            {
                var options = new SupabaseOptions { AutoRefreshToken = true };
                _client = new Client(SupabaseUrl, SupabaseKey, options);
                await _client.InitializeAsync();
            }
            return _client;
        }

        // ========== ПРОФИЛЬ ==========
        public static async Task<Dictionary<string, object>> GetProfile(string userId)
        {
            try
            {
                using var httpClient = new HttpClient();
                var url = $"{SupabaseUrl}/rest/v1/profiles?user_id=eq.{userId}";
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseKey}");
                var response = await httpClient.GetStringAsync(url);
                var array = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response);
                return array?.Count > 0 ? array[0] : null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetProfile error: {ex.Message}");
                return null;
            }
        }

        public static async Task<bool> UpdateProfile(string userId, object updates)
        {
            using var httpClient = new HttpClient();
            var json = JsonConvert.SerializeObject(updates);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"{SupabaseUrl}/rest/v1/profiles?user_id=eq.{userId}";
            httpClient.DefaultRequestHeaders.Add("apikey", SupabaseKey);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseKey}");
            var response = await httpClient.PatchAsync(url, content);
            return response.IsSuccessStatusCode;
        }

        // ========== ЧАТ ==========
        public static async Task<bool> SendMessage(string fromUserId, string toUserId, string message, string listingId = null)
        {
            var msg = new { from_user_id = fromUserId, to_user_id = toUserId, message, listing_id = listingId };
            using var httpClient = new HttpClient();
            var json = JsonConvert.SerializeObject(msg);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"{SupabaseUrl}/rest/v1/messages";
            httpClient.DefaultRequestHeaders.Add("apikey", SupabaseKey);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseKey}");
            var response = await httpClient.PostAsync(url, content);
            return response.IsSuccessStatusCode;
        }

        public static async Task<List<dynamic>> GetMessages(string userId1, string userId2, string listingId = null)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseKey}");

                var allMessages = new List<dynamic>();

                var url1 = $"{SupabaseUrl}/rest/v1/messages?from_user_id=eq.{userId1}&to_user_id=eq.{userId2}&order=created_at.asc";
                var response1 = await httpClient.GetStringAsync(url1);
                var messages1 = JsonConvert.DeserializeObject<List<dynamic>>(response1);
                if (messages1 != null && messages1.Count > 0)
                    allMessages.AddRange(messages1);

                var url2 = $"{SupabaseUrl}/rest/v1/messages?from_user_id=eq.{userId2}&to_user_id=eq.{userId1}&order=created_at.asc";
                var response2 = await httpClient.GetStringAsync(url2);
                var messages2 = JsonConvert.DeserializeObject<List<dynamic>>(response2);
                if (messages2 != null && messages2.Count > 0)
                    allMessages.AddRange(messages2);

                allMessages = allMessages.OrderBy(m => (DateTime)m.created_at).ToList();
                return allMessages;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetMessages error: {ex.Message}");
                return new List<dynamic>();
            }
        }

        // ========== УВЕДОМЛЕНИЯ ==========
        public static async Task SendNotification(string userId, string title, string message, string type, string relatedId = null)
        {
            var notif = new { user_id = userId, title, message, type, related_id = relatedId };
            using var httpClient = new HttpClient();
            var json = JsonConvert.SerializeObject(notif);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"{SupabaseUrl}/rest/v1/notifications";
            httpClient.DefaultRequestHeaders.Add("apikey", SupabaseKey);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseKey}");
            await httpClient.PostAsync(url, content);
        }

        public static async Task<List<dynamic>> GetUnreadNotifications(string userId)
        {
            using var httpClient = new HttpClient();
            var url = $"{SupabaseUrl}/rest/v1/notifications?user_id=eq.{userId}&is_read=eq.false&order=created_at.desc";
            httpClient.DefaultRequestHeaders.Add("apikey", SupabaseKey);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseKey}");
            var response = await httpClient.GetStringAsync(url);
            return JsonConvert.DeserializeObject<List<dynamic>>(response);
        }

        public static async Task<List<dynamic>> GetAllNotifications(string userId)
        {
            using var httpClient = new HttpClient();
            var url = $"{SupabaseUrl}/rest/v1/notifications?user_id=eq.{userId}&order=created_at.desc&limit=50";
            httpClient.DefaultRequestHeaders.Add("apikey", SupabaseKey);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseKey}");
            var response = await httpClient.GetStringAsync(url);
            return JsonConvert.DeserializeObject<List<dynamic>>(response);
        }

        public static async Task MarkNotificationRead(string notificationId)
        {
            using var httpClient = new HttpClient();
            var update = new { is_read = true };
            var json = JsonConvert.SerializeObject(update);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"{SupabaseUrl}/rest/v1/notifications?id=eq.{notificationId}";
            httpClient.DefaultRequestHeaders.Add("apikey", SupabaseKey);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseKey}");
            await httpClient.PatchAsync(url, content);
        }

        // ========== РЕЙТИНГ ==========
        public static async Task<bool> RateUser(string fromUserId, string toUserId, int rating)
        {
            var profile = await GetProfile(toUserId);
            if (profile == null) return false;
            int currentRatingCount = profile.ContainsKey("rating_count") ? Convert.ToInt32(profile["rating_count"]) : 0;
            double currentRating = profile.ContainsKey("rating") ? Convert.ToDouble(profile["rating"]) : 0;
            double newRating = (currentRating * currentRatingCount + rating) / (currentRatingCount + 1);
            int newCount = currentRatingCount + 1;

            var updates = new { rating = newRating, rating_count = newCount };
            return await UpdateProfile(toUserId, updates);
        }

        // ========== СТАТИСТИКА ==========
        public static async Task<int> GetUserActiveListingsCount(string userId)
        {
            using var httpClient = new HttpClient();
            var url = $"{SupabaseUrl}/rest/v1/pet_listings?user_id=eq.{userId}&status=eq.active&select=id";
            httpClient.DefaultRequestHeaders.Add("apikey", SupabaseKey);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseKey}");
            var response = await httpClient.GetStringAsync(url);
            var list = JsonConvert.DeserializeObject<List<object>>(response);
            return list?.Count ?? 0;
        }

        public static async Task<int> GetUserTotalListingsCount(string userId)
        {
            using var httpClient = new HttpClient();
            var url = $"{SupabaseUrl}/rest/v1/pet_listings?user_id=eq.{userId}&select=id";
            httpClient.DefaultRequestHeaders.Add("apikey", SupabaseKey);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseKey}");
            var response = await httpClient.GetStringAsync(url);
            var list = JsonConvert.DeserializeObject<List<object>>(response);
            return list?.Count ?? 0;
        }

        public static async Task<int> GetUserFoundListingsCount(string userId)
        {
            using var httpClient = new HttpClient();
            var url = $"{SupabaseUrl}/rest/v1/pet_listings?user_id=eq.{userId}&status=eq.closed&select=id";
            httpClient.DefaultRequestHeaders.Add("apikey", SupabaseKey);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseKey}");
            var response = await httpClient.GetStringAsync(url);
            var list = JsonConvert.DeserializeObject<List<object>>(response);
            return list?.Count ?? 0;
        }

        // ========== КОММЕНТАРИИ ==========
        public static async Task<bool> AddComment(string listingId, string userId, string message)
        {
            var comment = new { listing_id = listingId, user_id = userId, message };
            using var httpClient = new HttpClient();
            var json = JsonConvert.SerializeObject(comment);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"{SupabaseUrl}/rest/v1/comments";
            httpClient.DefaultRequestHeaders.Add("apikey", SupabaseKey);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseKey}");
            var response = await httpClient.PostAsync(url, content);
            return response.IsSuccessStatusCode;
        }

        public static async Task<List<dynamic>> GetComments(string listingId)
        {
            using var httpClient = new HttpClient();
            var url = $"{SupabaseUrl}/rest/v1/comments?listing_id=eq.{listingId}&order=created_at.asc";
            httpClient.DefaultRequestHeaders.Add("apikey", SupabaseKey);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseKey}");
            var response = await httpClient.GetStringAsync(url);
            return JsonConvert.DeserializeObject<List<dynamic>>(response);
        }

        // ========== СМЕНА EMAIL (БЕЗОПАСНО, ЧЕРЕЗ ПОДТВЕРЖДЕНИЕ) ==========
        public static async Task<bool> RequestEmailChange(string newEmail)
        {
            try
            {
                var client = await GetClient();
                var user = client.Auth.CurrentUser;

                if (user == null)
                {
                    MessageBox.Show("Пользователь не авторизован", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                var userAttributes = new Supabase.Gotrue.UserAttributes
                {
                    Email = newEmail
                };

                await client.Auth.Update(userAttributes);

                MessageBox.Show($"Письмо с подтверждением отправлено на {newEmail}. Перейдите по ссылке в письме, чтобы завершить смену email.",
                    "Подтверждение", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        public static async Task<bool> ResetPasswordForEmail(string email)
        {
            try
            {
                var client = await GetClient();
                await client.Auth.ResetPasswordForEmail(email);

                MessageBox.Show($"Ссылка для сброса пароля отправлена на {email}. Перейдите по ссылке в письме.",
                    "Проверьте почту", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // ========== СМЕНА ПАРОЛЯ (БЕЗОПАСНО, ЧЕРЕЗ ВОССТАНОВЛЕНИЕ) ==========
        // Этот метод отправляет ссылку на текущий email пользователя
        public static async Task<bool> RequestPasswordReset()
        {
            try
            {
                var client = await GetClient();
                var user = client.Auth.CurrentUser;

                if (user == null || string.IsNullOrEmpty(user.Email))
                {
                    MessageBox.Show("Пользователь не авторизован", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                await client.Auth.ResetPasswordForEmail(user.Email);

                MessageBox.Show($"Ссылка для сброса пароля отправлена на {user.Email}. Перейдите по ссылке в письме.",
                    "Проверьте почту", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // ========== ВСТАВКА ПРОФИЛЯ ==========
        public static async Task<bool> InsertProfile(string userId, string displayName, string phone)
        {
            try
            {
                using var httpClient = new HttpClient();
                var url = $"{SupabaseUrl}/rest/v1/profiles";
                var profileData = new
                {
                    user_id = userId,
                    display_name = displayName,
                    phone = phone,
                    role = "user",
                    is_verified = false
                };
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(profileData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                httpClient.DefaultRequestHeaders.Add("apikey", SupabaseKey);
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseKey}");

                var response = await httpClient.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"InsertProfile error: {response.StatusCode} - {responseBody}");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InsertProfile exception: {ex.Message}");
                return false;
            }
        }
    }
}