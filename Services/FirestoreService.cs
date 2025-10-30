using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FoodBuilder.Models;
using System.Diagnostics;

namespace FoodBuilder.Services
{
    public sealed class FirestoreService
    {
        private readonly HttpClient _httpClient;
        private readonly string _projectId;
        private readonly FirebaseAuthService _auth;

        private const string DatabasePath = "(default)";
        private const string CategoriesCollection = "FoodBuilder-Cathegories";
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public FirestoreService(HttpClient httpClient, string projectId, FirebaseAuthService auth)
        {
            _httpClient = httpClient;
            _projectId = projectId;
            _auth = auth;
        }

        private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string url, object? body = null)
        {
            string idToken = await _auth.GetValidIdTokenAsync().ConfigureAwait(false);
            HttpRequestMessage req = new HttpRequestMessage(method, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", idToken);
            if (body != null)
            {
                req.Content = JsonContent.Create(body);
            }
            return req;
        }

        private string DocumentsBaseUrl => $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/{DatabasePath}/documents";

        public async Task<List<Category>> GetCategoriesAsync(int limit = 50)
        {
            string url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/{DatabasePath}/documents:runQuery";
            object query = new
            {
                structuredQuery = new
                {
                    from = new[] { new { collectionId = CategoriesCollection } },
                    orderBy = new[] { new { field = new { fieldPath = "name" }, direction = "ASCENDING" } },
                    limit = limit
                }
            };

            HttpRequestMessage req = await CreateRequestAsync(HttpMethod.Post, url, query).ConfigureAwait(false);
            Debug.WriteLine($"[Firestore] runQuery categories -> POST {url}");
            HttpResponseMessage resp = await _httpClient.SendAsync(req).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                string details = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                Debug.WriteLine($"[Firestore] runQuery FAILED {(int)resp.StatusCode} {resp.ReasonPhrase}: {details}");
                Console.WriteLine($"[Firestore] runQuery FAILED {(int)resp.StatusCode} {resp.ReasonPhrase}: {details}");
                throw new HttpRequestException($"Firestore runQuery failed: {(int)resp.StatusCode} {resp.ReasonPhrase} - {details}");
            }
            string raw = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            // The runQuery returns a JSON lines array; each item may contain document
            List<Category> categories = new List<Category>();
            using JsonDocument doc = JsonDocument.Parse(raw);
            foreach (JsonElement el in doc.RootElement.EnumerateArray())
            {
                if (!el.TryGetProperty("document", out JsonElement docEl)) continue;
                if (!docEl.TryGetProperty("name", out JsonElement namePath)) continue;
                string fullName = namePath.GetString() ?? string.Empty; // .../documents/categories/{id}
                string id = fullName.Substring(fullName.LastIndexOf('/') + 1);

                if (!docEl.TryGetProperty("fields", out JsonElement fields)) continue;
                Category cat = CategoryFromFields(id, fields);
                categories.Add(cat);
            }
            return categories;
        }

        public async Task CreateOrUpdateCategoryAsync(Category category)
        {
            if (string.IsNullOrWhiteSpace(category.Id))
            {
                throw new ArgumentException("Category.Id est requis pour un set via REST direct");
            }

            string url = $"{DocumentsBaseUrl}/{CategoriesCollection}/{category.Id}";
            object payload = new
            {
                fields = CategoryToFields(category)
            };

            HttpRequestMessage req = await CreateRequestAsync(HttpMethod.Patch, url, payload).ConfigureAwait(false);
            Debug.WriteLine($"[Firestore] set category -> PATCH {url}");
            HttpResponseMessage resp = await _httpClient.SendAsync(req).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                string details = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                Debug.WriteLine($"[Firestore] set category FAILED {(int)resp.StatusCode} {resp.ReasonPhrase}: {details}");
                Console.WriteLine($"[Firestore] set category FAILED {(int)resp.StatusCode} {resp.ReasonPhrase}: {details}");
                throw new HttpRequestException($"Firestore set category failed: {(int)resp.StatusCode} {resp.ReasonPhrase} - {details}");
            }
        }

        public async Task DeleteCategoryAsync(string id)
        {
            string url = $"{DocumentsBaseUrl}/{CategoriesCollection}/{id}";
            HttpRequestMessage req = await CreateRequestAsync(HttpMethod.Delete, url).ConfigureAwait(false);
            Debug.WriteLine($"[Firestore] delete category -> DELETE {url}");
            HttpResponseMessage resp = await _httpClient.SendAsync(req).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                string details = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                Debug.WriteLine($"[Firestore] delete category FAILED {(int)resp.StatusCode} {resp.ReasonPhrase}: {details}");
                Console.WriteLine($"[Firestore] delete category FAILED {(int)resp.StatusCode} {resp.ReasonPhrase}: {details}");
                throw new HttpRequestException($"Firestore delete category failed: {(int)resp.StatusCode} {resp.ReasonPhrase} - {details}");
            }
        }

        public async Task<List<Category>> QueryCategoriesByNameAsync(string startWith, int limit = 20)
        {
            string url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/{DatabasePath}/documents:runQuery";
            object query = new
            {
                structuredQuery = new
                {
                    from = new[] { new { collectionId = CategoriesCollection } },
                    where = new
                    {
                        fieldFilter = new
                        {
                            field = new { fieldPath = "name" },
                            op = "GREATER_THAN_OR_EQUAL",
                            value = new { stringValue = startWith }
                        }
                    },
                    orderBy = new[] { new { field = new { fieldPath = "name" }, direction = "ASCENDING" } },
                    limit = limit
                }
            };

            HttpRequestMessage req = await CreateRequestAsync(HttpMethod.Post, url, query).ConfigureAwait(false);
            Debug.WriteLine($"[Firestore] runQuery by name -> POST {url}");
            HttpResponseMessage resp = await _httpClient.SendAsync(req).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                string details = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                Debug.WriteLine($"[Firestore] runQuery(name) FAILED {(int)resp.StatusCode} {resp.ReasonPhrase}: {details}");
                Console.WriteLine($"[Firestore] runQuery(name) FAILED {(int)resp.StatusCode} {resp.ReasonPhrase}: {details}");
                throw new HttpRequestException($"Firestore runQuery(name) failed: {(int)resp.StatusCode} {resp.ReasonPhrase} - {details}");
            }
            string raw = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            List<Category> categories = new List<Category>();
            using JsonDocument doc = JsonDocument.Parse(raw);
            foreach (JsonElement el in doc.RootElement.EnumerateArray())
            {
                if (!el.TryGetProperty("document", out JsonElement docEl)) continue;
                string fullName = docEl.GetProperty("name").GetString() ?? string.Empty;
                string id = fullName.Substring(fullName.LastIndexOf('/') + 1);
                JsonElement fields = docEl.GetProperty("fields");
                categories.Add(CategoryFromFields(id, fields));
            }
            return categories;
        }

        private static object CategoryToFields(Category cat)
        {
            return new
            {
                name = new { stringValue = cat.Name ?? string.Empty },
                description = cat.Description is null ? null : new { stringValue = cat.Description },
                imageUrl = cat.ImageUrl is null ? null : new { stringValue = cat.ImageUrl }
            };
        }

        private static Category CategoryFromFields(string id, JsonElement fields)
        {
            string? name = fields.TryGetProperty("name", out JsonElement nv) && nv.TryGetProperty("stringValue", out JsonElement nvs)
                ? nvs.GetString()
                : null;
            string? description = fields.TryGetProperty("description", out JsonElement dv) && dv.TryGetProperty("stringValue", out JsonElement dvs)
                ? dvs.GetString()
                : null;
            string? imageUrl = fields.TryGetProperty("imageUrl", out JsonElement iv) && iv.TryGetProperty("stringValue", out JsonElement ivs)
                ? ivs.GetString()
                : null;

            return new Category
            {
                Id = id,
                Name = name ?? string.Empty,
                Description = description,
                ImageUrl = imageUrl
            };
        }
    }
}


