using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Diagnostics;

namespace FoodBuilder.Services
{
    public sealed class FirebaseAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        private string? _idToken;
        private string? _refreshToken;
        private DateTimeOffset _expiryUtc;

        public FirebaseAuthService(HttpClient httpClient, string apiKey)
        {
            _httpClient = httpClient;
            _apiKey = apiKey;
        }

        private sealed class SignInWithPasswordRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public bool ReturnSecureToken { get; set; } = true;
        }

        private sealed class SignInResponse
        {
            [JsonPropertyName("idToken")] public string IdToken { get; set; } = string.Empty;
            [JsonPropertyName("refreshToken")] public string RefreshToken { get; set; } = string.Empty;
            [JsonPropertyName("expiresIn")] public string ExpiresInSeconds { get; set; } = "3600";
            [JsonPropertyName("localId")] public string LocalId { get; set; } = string.Empty;
        }

        private sealed class RefreshResponse
        {
            [JsonPropertyName("id_token")] public string IdToken { get; set; } = string.Empty;
            [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; } = string.Empty;
            [JsonPropertyName("expires_in")] public string ExpiresInSeconds { get; set; } = "3600";
            [JsonPropertyName("user_id")] public string UserId { get; set; } = string.Empty;
        }

        public async Task SignInWithEmailAndPasswordAsync(string email, string password)
        {
            string url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_apiKey}";
            SignInWithPasswordRequest payload = new SignInWithPasswordRequest
            {
                Email = email,
                Password = password,
                ReturnSecureToken = true
            };

            Debug.WriteLine($"[Auth] POST {url}");
            HttpResponseMessage resp = await _httpClient.PostAsJsonAsync(url, payload).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                string details = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                Debug.WriteLine($"[Auth] signInWithPassword FAILED {(int)resp.StatusCode} {resp.ReasonPhrase}: {details}");
                Console.WriteLine($"[Auth] signInWithPassword FAILED {(int)resp.StatusCode} {resp.ReasonPhrase}: {details}");
                throw new HttpRequestException($"Firebase Auth signIn failed: {(int)resp.StatusCode} {resp.ReasonPhrase} - {details}");
            }
            SignInResponse data = (await resp.Content.ReadFromJsonAsync<SignInResponse>().ConfigureAwait(false))!;
            SetSession(data.IdToken, data.RefreshToken, int.Parse(data.ExpiresInSeconds));
        }

        public async Task SignInAnonymouslyAsync()
        {
            string url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={_apiKey}";
            // Empty JSON body for anonymous sign-up
            Debug.WriteLine($"[Auth] POST {url} (anonymous)");
            HttpResponseMessage resp = await _httpClient.PostAsJsonAsync(url, new { returnSecureToken = true }).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                string details = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                Debug.WriteLine($"[Auth] anonymous signUp FAILED {(int)resp.StatusCode} {resp.ReasonPhrase}: {details}");
                Console.WriteLine($"[Auth] anonymous signUp FAILED {(int)resp.StatusCode} {resp.ReasonPhrase}: {details}");
                throw new HttpRequestException($"Firebase Auth anonymous signUp failed: {(int)resp.StatusCode} {resp.ReasonPhrase} - {details}");
            }
            SignInResponse data = (await resp.Content.ReadFromJsonAsync<SignInResponse>().ConfigureAwait(false))!;
            SetSession(data.IdToken, data.RefreshToken, int.Parse(data.ExpiresInSeconds));
        }

        private void SetSession(string idToken, string refreshToken, int expiresInSeconds)
        {
            _idToken = idToken;
            _refreshToken = refreshToken;
            _expiryUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(0, expiresInSeconds - 60));
        }

        public async Task<string> GetValidIdTokenAsync()
        {
            if (!string.IsNullOrWhiteSpace(_idToken) && DateTimeOffset.UtcNow < _expiryUtc)
            {
                return _idToken!;
            }

            if (string.IsNullOrWhiteSpace(_refreshToken))
            {
                // If there's no refresh token, fall back to anonymous sign-in
                await SignInAnonymouslyAsync().ConfigureAwait(false);
                return _idToken!;
            }

            string url = $"https://securetoken.googleapis.com/v1/token?key={_apiKey}";
            Dictionary<string, string> body = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = _refreshToken!
            };
            Debug.WriteLine($"[Auth] POST {url} (refresh)");
            HttpResponseMessage resp = await _httpClient.PostAsync(url, new FormUrlEncodedContent(body)).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                string details = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                Debug.WriteLine($"[Auth] token refresh FAILED {(int)resp.StatusCode} {resp.ReasonPhrase}: {details}");
                Console.WriteLine($"[Auth] token refresh FAILED {(int)resp.StatusCode} {resp.ReasonPhrase}: {details}");
                throw new HttpRequestException($"Firebase Auth token refresh failed: {(int)resp.StatusCode} {resp.ReasonPhrase} - {details}");
            }
            RefreshResponse data = (await resp.Content.ReadFromJsonAsync<RefreshResponse>().ConfigureAwait(false))!;
            SetSession(data.IdToken, data.RefreshToken, int.Parse(data.ExpiresInSeconds));
            return _idToken!;
        }
    }
}


