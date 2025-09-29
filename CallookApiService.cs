using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SimpleLogger
{
    public class CallookApiService
    {
        private readonly HttpClient _httpClient;

        public CallookApiService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<JObject?> GetCallsignInfoAsync(string callsign)
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"https://callook.info/{callsign}/json");
                return JObject.Parse(response);
            }
            catch (HttpRequestException)
            {
                // Handle cases where the callsign is not found (404) or other HTTP errors
                return null;
            }
            catch (Exception)
            {
                // Handle other potential errors like network issues
                return null;
            }
        }
    }
}

