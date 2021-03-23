using System;
using System.IO;
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

        private static (HttpStatusCode httpStatusCode, string responseContent) HttpGet(string randomItemId, string key)
        {
            var requestUri = new Uri($"{Root}{randomItemId}");
            using var httpClient = new HttpClient { DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Key", key) } };
            var response = httpClient.GetAsync(requestUri).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
            return (response.StatusCode, responseContent);
        }

        private static (HttpStatusCode httpStatusCode, string responseContent) HttpPut(string randomItemId, string key, string randomItem)
        {
            var requestUri = new Uri($"{Root}{randomItemId}");
            using var httpClient = new HttpClient { DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Key", key) } };
            using var requestContent = new StringContent(randomItem, Encoding.UTF8, "application/json");
            var response = httpClient.PutAsync(requestUri, requestContent).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
            return (response.StatusCode, responseContent);
        }

        private static (HttpStatusCode httpStatusCode, string responseContent) HttpPost(string key, string randomItems)
        {
            var requestUri = new Uri($"{Root}");
            using var httpClient = new HttpClient { DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Key", key) } };
            using var requestContent = new StringContent(randomItems, Encoding.UTF8, "application/json");
            var response = httpClient.PostAsync(requestUri, requestContent).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
            return (response.StatusCode, responseContent);
        }

        private static (HttpStatusCode httpStatusCode, string responseContent) HttpPostCsv(string key, string name, string fileName, string data)
        {
            var requestUri = new Uri($"{Root}csv");
            using var httpClient = new HttpClient { DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Key", key) } };
            using var streamContent = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(data)));
            const string boundary = "AnythingThatIsNotWithinData";
            using var requestContent = new MultipartFormDataContent(boundary) { { streamContent, name, fileName } };
            var response = httpClient.PostAsync(requestUri, requestContent).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
            return (response.StatusCode, responseContent);
        }

        private static (HttpStatusCode httpStatusCode, string responseContent) HttpDelete(string randomItemId, string key)
        {
            var requestUri = new Uri($"{Root}{randomItemId}");
            using var httpClient = new HttpClient { DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Key", key) } };
            var response = httpClient.DeleteAsync(requestUri).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
            return (response.StatusCode, responseContent);
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
            const string randomItem = "{\"id\":1,\"name\":\"W10\",\"numberOfPossibleResults\":10,\"description\":\"PUT Seiten\"}";
            var (httpStatusCode, body) = HttpPut("1", KeyUbarcturus, randomItem);
            Assert.Equal(HttpStatusCode.OK, httpStatusCode);
            Assert.Equal(randomItem, body);
        }

        [Fact]
        public void UpdateWithDifferentIds()
        {
            const string randomItem = "{\"id\":1,\"name\":\"W10\",\"numberOfPossibleResults\":10,\"description\":\"PUT Seiten\"}";
            var (httpStatusCode, _) = HttpPut("2", KeyUbarcturus, randomItem);
            Assert.Equal(HttpStatusCode.BadRequest, httpStatusCode);
        }

        [Fact]
        public void UpdateItemWithIdWhichNotExists()
        {
            const string randomItem = "{\"id\":2147483647,\"name\":\"W10\",\"numberOfPossibleResults\":10,\"description\":\"PUT Seiten\"}";
            var (httpStatusCode, _) = HttpPut("2147483647", KeyUbarcturus, randomItem);
            Assert.Equal(HttpStatusCode.NotFound, httpStatusCode);
        }

        #endregion PUT

        #region POST

        [Fact]
        public void AddOneItem()
        {
            const string randomItem = "[{\"name\":\"W10\",\"numberOfPossibleResults\":10,\"description\":\"Zehn POST Seiten\"}]";
            var (httpStatusCode, _) = HttpPost(KeyUbarcturus, randomItem);
            Assert.Equal(HttpStatusCode.Created, httpStatusCode);
        }

        [Fact]
        public void AddManyItems()
        {
            const string w6 = "{\"name\":\"W6\",\"numberOfPossibleResults\":6,\"description\":\"Sechs POST Seiten\"}";
            const string w10 = "{\"name\":\"W10\",\"numberOfPossibleResults\":10,\"description\":\"Zehn POST Seiten\"}";
            const string w20 = "{\"name\":\"W20\",\"numberOfPossibleResults\":20,\"description\":\"Zwanzig POST Seiten\"}";
            var randomItems = $"[{w6},{w10},{w20}]";
            var (httpStatusCode, _) = HttpPost(KeyUbarcturus, randomItems);
            Assert.Equal(HttpStatusCode.Created, httpStatusCode);
        }

        [Fact]
        public void AddEmptyItemArray()
        {
            const string randomItem = "[]";
            var (httpStatusCode, _) = HttpPost(KeyUbarcturus, randomItem);
            Assert.Equal(HttpStatusCode.BadRequest, httpStatusCode);
        }

        [Fact]
        public void AddEmptyItem()
        {
            const string randomItem = "";
            var (httpStatusCode, _) = HttpPost(KeyUbarcturus, randomItem);
            Assert.Equal(HttpStatusCode.BadRequest, httpStatusCode);
        }

        [Fact]
        public void AddItemsFromCsv()
        {
            const string w30 = "W30,30,30 CSV Seiten";
            const string w8 = "W8,8";
            const string w12 = "W12,12,";
            const string w2 = "W2,2,\"Zwei, CSV Seiten\"";
            const string name = "csv";
            const string fileName = "test.csv";
            var data = $"{w30}\n{w8}\n{w12}\n{w2}";
            var (httpStatusCode, _) = HttpPostCsv(KeyUbarcturus, name, fileName, data);
            Assert.Equal(HttpStatusCode.Created, httpStatusCode);
        }

        [Fact]
        public void AddEmptyCsv()
        {
            const string name = "csv";
            const string fileName = "test.csv";
            const string data = "";
            var (httpStatusCode, _) = HttpPostCsv(KeyUbarcturus, name, fileName, data);
            Assert.Equal(HttpStatusCode.BadRequest, httpStatusCode);
        }

        [Fact]
        public void AddNotConformCsv()
        {
            const string name = "csv";
            const string fileName = "test.csv";
            const string data = "NotConform";
            var (httpStatusCode, _) = HttpPostCsv(KeyUbarcturus, name, fileName, data);
            Assert.Equal(HttpStatusCode.BadRequest, httpStatusCode);
        }

        #endregion POST

        #region DELETE

        [Fact]
        public void DeleteItem()
        {
            var (_, body) = HttpGet("", KeyUbarcturus);
            var randomItemId = JsonSerializer.Deserialize<JsonDocument>(body)
                ?.RootElement.EnumerateArray()
                .Last()
                .GetProperty("id")
                .ToString();
            var (httpStatusCode, _) = HttpDelete(randomItemId, KeyUbarcturus);
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
