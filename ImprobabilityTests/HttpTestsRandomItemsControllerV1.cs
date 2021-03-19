using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ImprobabilityTests
{
    public class HttpTestsRandomItemsControllerV1
    {
        private const string Port = "45347";
        private const string Root = "http://localhost:" + Port + "/api/v1/randomitems/";
        private const string KeyUbarcturus = "5RE23H4JHQA2DVLVSEZ525UCRLWXUKGQ";
        private const string KeyPhilipp = "ZTVWQSDYXSF2UB6B46LKSIGA7GVWHZAQ";

        private static (HttpStatusCode httpStatusCode, string reponseContent) HttpGet(string itemId, string key)
        {
            var requestUri = new Uri($"{Root}{itemId}");
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Key", key);
            var response = httpClient.GetAsync(requestUri).Result;
            var reponseContent = response.Content.ReadAsStringAsync().Result;
            return (response.StatusCode, reponseContent);
        }

        private static (HttpStatusCode httpStatusCode, string reponseContent) HttpPut(string itemId, string key, string item)
        {
            var requestUri = new Uri($"{Root}{itemId}");
            using var httpClient = new HttpClient();
            using var requestContent = new StringContent(item, Encoding.UTF8, "application/json");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Key", key);
            var response = httpClient.PutAsync(requestUri, requestContent).Result;
            var reponseContent = response.Content.ReadAsStringAsync().Result;
            return (response.StatusCode, reponseContent);
        }

        private static (HttpStatusCode httpStatusCode, string reponseContent) HttpPost(string key, string items)
        {
            var requestUri = new Uri($"{Root}");
            using var httpClient = new HttpClient();
            using var requestContent = new StringContent(items, Encoding.UTF8, "application/json");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Key", key);
            var response = httpClient.PostAsync(requestUri, requestContent).Result;
            var reponseContent = response.Content.ReadAsStringAsync().Result;
            return (response.StatusCode, reponseContent);
        }

        private static (HttpStatusCode httpStatusCode, string reponseContent) HttpDelete(string itemId, string key)
        {
            var requestUri = new Uri($"{Root}{itemId}");
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Key", key);
            var response = httpClient.DeleteAsync(requestUri).Result;
            var reponseContent = response.Content.ReadAsStringAsync().Result;
            return (response.StatusCode, reponseContent);
        }

        [Fact]
        public void OneEqualsOne()
        {
            // is the test framework working?
            Assert.Equal(1, 1);
        }

        #region GET

        [Fact]
        public void GetItems()
        {
            var (httpStatusCode, _) = HttpGet("", KeyUbarcturus);
            Assert.Equal(HttpStatusCode.OK, httpStatusCode);
        }

        [Fact]
        public void GetItemsWithoutApiKey()
        {
            var (httpStatusCode, _) = HttpGet("", "");
            Assert.Equal(HttpStatusCode.Unauthorized, httpStatusCode);
        }

        [Fact]
        public void GetItemsWithFalseApiKey()
        {
            var (httpStatusCode, _) = HttpGet("", "ThisKeyIsSupposedDoNotExist");
            Assert.Equal(HttpStatusCode.Unauthorized, httpStatusCode);
        }

        [Fact]
        public void GetOneItem()
        {
            var (httpStatusCode, _) = HttpGet("1", KeyUbarcturus);
            Assert.Equal(HttpStatusCode.OK, httpStatusCode);
        }

        [Fact]
        public void GetItemFromOtherUser()
        {
            var (httpStatusCode, _) = HttpGet("1", KeyPhilipp);
            Assert.Equal(HttpStatusCode.Unauthorized, httpStatusCode);
        }

        [Fact]
        public void GetItemWithIdWhichNotExists()
        {
            var (httpStatusCode, _) = HttpGet("2147483647", KeyPhilipp);
            Assert.Equal(HttpStatusCode.NotFound, httpStatusCode);
        }

        [Fact]
        public void GetItemWithToBigId()
        {
            var (httpStatusCode, _) = HttpGet("9999999999", KeyPhilipp);
            Assert.Equal(HttpStatusCode.BadRequest, httpStatusCode);
        }

        [Fact]
        public void GetItemWithStringId()
        {
            var (httpStatusCode, _) = HttpGet("abc", KeyPhilipp);
            Assert.Equal(HttpStatusCode.BadRequest, httpStatusCode);
        }

        #endregion GET

        #region PUT

        [Fact]
        public void UpdateOneItem()
        {
            const string item = "{\"id\":1,\"name\":\"W10\",\"numberOfPossibleResults\":10,\"description\":\"PUT Seiten\"}";
            var (httpStatusCode, body) = HttpPut("1", KeyUbarcturus, item);
            Assert.Equal(HttpStatusCode.OK, httpStatusCode);
            Assert.Equal(item, body);
        }

        [Fact]
        public void UpdateWithDifferentIds()
        {
            const string item = "{\"id\":1,\"name\":\"W10\",\"numberOfPossibleResults\":10,\"description\":\"PUT Seiten\"}";
            var (httpStatusCode, _) = HttpPut("2", KeyUbarcturus, item);
            Assert.Equal(HttpStatusCode.BadRequest, httpStatusCode);
        }

        [Fact]
        public void UpdateItemWithIdWhichNotExists()
        {
            const string item = "{\"id\":2147483647,\"name\":\"W10\",\"numberOfPossibleResults\":10,\"description\":\"PUT Seiten\"}";
            var (httpStatusCode, _) = HttpPut("2147483647", KeyUbarcturus, item);
            Assert.Equal(HttpStatusCode.NotFound, httpStatusCode);
        }

        #endregion PUT

        #region POST

        [Fact]
        public void AddOneItem()
        {
            const string item = "{\"name\":\"W10\",\"numberOfPossibleResults\":10,\"description\":\" Zehn POST Seiten\"}";
            var (httpStatusCode, _) = HttpPost(KeyUbarcturus, item);
            Assert.Equal(HttpStatusCode.Created, httpStatusCode);
        }

        [Fact]
        public void AddManyItems()
        {
            const string w6 = "{\"name\":\"W6\",\"numberOfPossibleResults\":6,\"description\":\"Sechs POST Seiten\"}";
            const string w10 = "{\"name\":\"W10\",\"numberOfPossibleResults\":10,\"description\":\"Zehn POST Seiten\"}";
            const string w20 = "{\"name\":\"W20\",\"numberOfPossibleResults\":20,\"description\":\"Zwanzig POST Seiten\"}";
            var items = $"[{w6},{w10},{w20}]";
            var (httpStatusCode, _) = HttpPost(KeyUbarcturus, items);
            Assert.Equal(HttpStatusCode.Created, httpStatusCode);
        }

        #endregion POST

        #region DELETE

        [Fact]
        public void DeleteItem()
        {
            var (_, body) = HttpGet("", KeyUbarcturus);
            var itemId = JsonSerializer.Deserialize<JsonDocument>(body)
                ?.RootElement.EnumerateArray()
                .Last()
                .GetProperty("id")
                .ToString();
            var (httpStatusCode, _) = HttpDelete(itemId, KeyUbarcturus);
            Assert.Equal(HttpStatusCode.NoContent, httpStatusCode);
        }

        [Fact]
        public void DeleteItemWithIdWhichNotExists()
        {
            var (httpStatusCode, _) = HttpDelete("2147483647", KeyUbarcturus);
            Assert.Equal(HttpStatusCode.NotFound, httpStatusCode);
        }

        #endregion DELETE
    }
}
