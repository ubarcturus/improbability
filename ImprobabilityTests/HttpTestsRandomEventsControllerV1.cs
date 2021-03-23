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
    public class HttpTestsRandomEventsControllerV1
    {
        private const string Port = "45347";
        private const string Root = "http://localhost:" + Port + "/api/v1/randomevents/";
        private const string KeyUbarcturus = "5RE23H4JHQA2DVLVSEZ525UCRLWXUKGQ";
        private const string KeyPhilipp = "ZTVWQSDYXSF2UB6B46LKSIGA7GVWHZAQ";

        private static (HttpStatusCode httpStatusCode, string reponseContent) HttpGet(string randomEventId, string randomItemIdParameter, string key)
        {
            var requestUri = new Uri($"{Root}{randomEventId}{randomItemIdParameter}");
            using var httpClient = new HttpClient { DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Key", key) } };
            var response = httpClient.GetAsync(requestUri).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
            return (response.StatusCode, responseContent);
        }

        private static (HttpStatusCode httpStatusCode, string reponseContent) HttpPut(string randomEventId, string key, string randomEvent)
        {
            var requestUri = new Uri($"{Root}{randomEventId}");
            using var httpClient = new HttpClient { DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Key", key) } };
            using var requestContent = new StringContent(randomEvent, Encoding.UTF8, "application/json");
            var response = httpClient.PutAsync(requestUri, requestContent).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
            return (response.StatusCode, responseContent);
        }

        private static (HttpStatusCode httpStatusCode, string reponseContent) HttpPost(string randomItemIdParameter, string key, string randomEvents)
        {
            var requestUri = new Uri($"{Root}{randomItemIdParameter}");
            using var httpClient = new HttpClient { DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Key", key) } };
            using var requestContent = new StringContent(randomEvents, Encoding.UTF8, "application/json");
            var response = httpClient.PostAsync(requestUri, requestContent).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
            return (response.StatusCode, responseContent);
        }

        private static (HttpStatusCode httpStatusCode, string responseContent) HttpPostCsv(string randomItemIdParameter, string key, string name, string fileName, string data)
        {
            var requestUri = new Uri($"{Root}csv{randomItemIdParameter}");
            using var httpClient = new HttpClient { DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Key", key) } };
            using var streamContent = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(data)));
            const string boundary = "AnythingThatIsNotWithinData";
            using var requestContent = new MultipartFormDataContent(boundary) { { streamContent, name, fileName } };
            var response = httpClient.PostAsync(requestUri, requestContent).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
            return (response.StatusCode, responseContent);
        }

        private static (HttpStatusCode httpStatusCode, string reponseContent) HttpDelete(string randomEventId, string key)
        {
            var requestUri = new Uri($"{Root}{randomEventId}");
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
        public void GetEvents()
        {
            var (httpStatusCode, _) = HttpGet("", "", KeyUbarcturus);
            Assert.Equal(HttpStatusCode.OK, httpStatusCode);
        }

        [Fact]
        public void GetEventsWithoutApiKey()
        {
            var (httpStatusCode, _) = HttpGet("", "", "");
            Assert.Equal(HttpStatusCode.Unauthorized, httpStatusCode);
        }

        [Fact]
        public void GetEventsWithFalseApiKey()
        {
            var (httpStatusCode, _) = HttpGet("", "", "ThisKeyIsSupposedDoNotExist");
            Assert.Equal(HttpStatusCode.Unauthorized, httpStatusCode);
        }

        [Fact]
        public void GetEventsFromOneItem()
        {
            var (httpStatusCode, _) = HttpGet("", "?randomItemId=1", KeyUbarcturus);
            Assert.Equal(HttpStatusCode.OK, httpStatusCode);
        }

        [Fact]
        public void GetEventsFromItemFromOtherUser()
        {
            var (httpStatusCode, _) = HttpGet("", "?randomItemId=1", KeyPhilipp);
            Assert.Equal(HttpStatusCode.Unauthorized, httpStatusCode);
        }

        [Fact]
        public void GetEventsFromItemWithIdWhichNotExists()
        {
            var (httpStatusCode, _) = HttpGet("", "?randomItemId=2147483647", KeyUbarcturus);
            Assert.Equal(HttpStatusCode.NotFound, httpStatusCode);
        }

        [Fact]
        public void GetEventsFromItemWithToBigId()
        {
            var (httpStatusCode, _) = HttpGet("", "?randomItemId=9999999999", KeyUbarcturus);
            Assert.Equal(HttpStatusCode.BadRequest, httpStatusCode);
        }

        [Fact]
        public void GetEventsFromItemWithStringId()
        {
            var (httpStatusCode, _) = HttpGet("", "?randomItemId=abc", KeyUbarcturus);
            Assert.Equal(HttpStatusCode.BadRequest, httpStatusCode);
        }

        [Fact]
        public void GetOneEvent()
        {
            var (httpStatusCode, _) = HttpGet("1", "", KeyUbarcturus);
            Assert.Equal(HttpStatusCode.OK, httpStatusCode);
        }

        [Fact]
        public void GetEventFromOtherUser()
        {
            var (httpStatusCode, _) = HttpGet("1", "", KeyPhilipp);
            Assert.Equal(HttpStatusCode.Unauthorized, httpStatusCode);
        }

        [Fact]
        public void GetEventWithIdWhichNotExists()
        {
            var (httpStatusCode, _) = HttpGet("2147483647", "", KeyUbarcturus);
            Assert.Equal(HttpStatusCode.NotFound, httpStatusCode);
        }

        [Fact]
        public void GetEventWithToBigId()
        {
            var (httpStatusCode, _) = HttpGet("9999999999", "", KeyUbarcturus);
            Assert.Equal(HttpStatusCode.BadRequest, httpStatusCode);
        }

        [Fact]
        public void GetEventWithStringId()
        {
            var (httpStatusCode, _) = HttpGet("abc", "", KeyUbarcturus);
            Assert.Equal(HttpStatusCode.BadRequest, httpStatusCode);
        }

        #endregion GET

        #region PUT

        [Fact]
        public void UpdateOneEvent()
        {
            var date = DateTime.Now.ToString("O");
            var randomEvent = $"{{\"id\":1,\"name\":\"Test\",\"time\":\"{date}\",\"result\":3,\"description\":\"{date.GetHashCode(StringComparison.InvariantCulture)}\",\"randomItemId\":1}}";
            var (httpStatusCode, body) = HttpPut("1", KeyUbarcturus, randomEvent);
            Assert.Equal(HttpStatusCode.OK, httpStatusCode);
            Assert.Equal(randomEvent, body);
        }

        [Fact]
        public void UpdateWithDifferentIds()
        {
            const string randomEvent = "{\"id\":2,\"name\":\"Test\",\"time\":\"2021-03-23T14:00:22+01:00\",\"result\":58,\"description\":\"Test 1613480422\",\"randomItemId\":1}";
            var (httpStatusCode, _) = HttpPut("1", KeyUbarcturus, randomEvent);
            Assert.Equal(HttpStatusCode.BadRequest, httpStatusCode);
        }

        [Fact]
        public void UpdateEventWithIdWhichNotExists()
        {
            const string randomEvent = "{\"id\":2147483647,\"name\":\"Test\",\"time\":\"2021-03-23T14:00:22+01:00\",\"result\":58,\"description\":\"Test 1613480422\",\"randomItemId\":1}";
            var (httpStatusCode, _) = HttpPut("2147483647", KeyUbarcturus, randomEvent);
            Assert.Equal(HttpStatusCode.NotFound, httpStatusCode);
        }

        #endregion PUT

        #region POST

        [Fact]
        public void AddOneEvent()
        {
            var date = DateTime.Now.ToString("O");
            var randomEvent = $"[{{\"name\":\"Test\",\"time\":\"{date}\",\"result\":3,\"description\":\"{date.GetHashCode(StringComparison.InvariantCulture)}\",\"randomItemId\":1}}]";
            var (httpStatusCode, _) = HttpPost("?randomItemId=1", KeyUbarcturus, randomEvent);
            Assert.Equal(HttpStatusCode.Created, httpStatusCode);
        }

        [Fact]
        public void AddManyEvents()
        {
            var date = DateTime.Now.ToString("O");
            var description = date.GetHashCode(StringComparison.InvariantCulture);
            var first = $"{{\"name\":\"Test first\",\"time\":\"{date}\",\"result\":3,\"description\":\"{description}\",\"randomItemId\":1}}";
            var second = $"{{\"name\":\"Test second\",\"time\":\"{date}\",\"result\":3,\"description\":\"{description}\",\"randomItemId\":1}}";
            var third = $"{{\"name\":\"Test third\",\"time\":\"{date}\",\"result\":3,\"description\":\"{description}\",\"randomItemId\":1}}";
            var randomEvents = $"[{first},{second},{third}]";
            var (httpStatusCode, _) = HttpPost("?randomItemId=1", KeyUbarcturus, randomEvents);
            Assert.Equal(HttpStatusCode.Created, httpStatusCode);
        }

        [Fact]
        public void AddEmptyEventArray()
        {
            const string randomEvent = "[]";
            var (httpStatusCode, _) = HttpPost("?randomItemId=1", KeyUbarcturus, randomEvent);
            Assert.Equal(HttpStatusCode.BadRequest, httpStatusCode);
        }

        [Fact]
        public void AddEmptyEvent()
        {
            const string randomEvent = "";
            var (httpStatusCode, _) = HttpPost("?randomItemId=1", KeyUbarcturus, randomEvent);
            Assert.Equal(HttpStatusCode.BadRequest, httpStatusCode);
        }

        #endregion POST

        #region DELETE

        [Fact]
        public void DeleteEvent()
        {
            var (_, body) = HttpGet("", "", KeyUbarcturus);
            var randomEventId = JsonSerializer.Deserialize<JsonDocument>(body)
                ?.RootElement.EnumerateArray()
                .Last()
                .GetProperty("id")
                .ToString();
            var (httpStatusCode, _) = HttpDelete(randomEventId, KeyUbarcturus);
            Assert.Equal(HttpStatusCode.NoContent, httpStatusCode);
        }

        #endregion DELETE
    }
}
