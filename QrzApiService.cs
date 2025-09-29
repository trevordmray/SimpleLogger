using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SimpleLogger
{
    public class QrzApiService
    {
        private readonly HttpClient _httpClient;
        private const string ApiUrl = "https://xmldata.qrz.com/xml/current/";
        private string? _sessionKey;
        private readonly XNamespace _qrzNs = "http://xmldata.qrz.com";

        public QrzApiService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("SimpleLogger/1.0");
        }

        public bool IsLoggedIn => !string.IsNullOrEmpty(_sessionKey);

        private async Task<(string response, string error)> ExecuteRequestAsync(string url)
        {
            try
            {
                HttpResponseMessage httpResponse = await _httpClient.GetAsync(url);
                string rawResponse = await httpResponse.Content.ReadAsStringAsync();
                return !httpResponse.IsSuccessStatusCode ? (string.Empty, $"HTTP Error {httpResponse.StatusCode}: {rawResponse}") : (rawResponse, string.Empty);
            }
            catch (Exception ex)
            {
                return (string.Empty, $"A network-level exception occurred: {ex.Message}");
            }
        }

        public async Task<(bool success, string errorMessage)> LoginAsync(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return (false, "Username or password cannot be empty.");

            var encodedUsername = WebUtility.UrlEncode(username);
            var encodedPassword = WebUtility.UrlEncode(password);
            var loginUrl = $"{ApiUrl}?username={encodedUsername}&password={encodedPassword}";

            var (response, error) = await ExecuteRequestAsync(loginUrl);

            if (!string.IsNullOrEmpty(error)) return (false, error);

            try
            {
                var xml = XDocument.Parse(response);
                var keyElement = xml.Descendants(_qrzNs + "Key").FirstOrDefault();
                var errorElement = xml.Descendants(_qrzNs + "Error").FirstOrDefault();

                if (errorElement != null)
                {
                    _sessionKey = null;
                    string errorValue = errorElement.Value;
                    if (string.IsNullOrWhiteSpace(errorValue))
                    {
                        errorValue = "The server returned an empty error. This typically means the account does not have an active QRZ.com XML Data subscription.";
                    }
                    return (false, errorValue);
                }

                if (keyElement == null)
                {
                    _sessionKey = null;
                    return (false, $"The server response was invalid. It contained no key and no error. Raw response: {response}");
                }

                _sessionKey = keyElement.Value;
                return (IsLoggedIn, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, $"Failed to parse the server response. Exception: {ex.Message}. Full response: {response}");
            }
        }

        public async Task<XDocument> GetCallsignInfoAsync(string callsign)
        {
            if (!IsLoggedIn)
            {
                return new XDocument(new XElement("QRZDatabase", new XElement("Session", new XElement("Error", "Not logged in to QRZ service."))));
            }

            var lookupUrl = $"{ApiUrl}?s={_sessionKey}&callsign={callsign}";
            var (response, error) = await ExecuteRequestAsync(lookupUrl);

            if (!string.IsNullOrEmpty(error))
            {
                return new XDocument(new XElement("QRZDatabase", new XElement("Session", new XElement("Error", error))));
            }

            try
            {
                var xml = XDocument.Parse(response);
                var sessionError = xml.Descendants(_qrzNs + "Error").FirstOrDefault()?.Value;
                if (sessionError != null && (sessionError.Contains("Session Timeout") || sessionError.Contains("Invalid session key")))
                {
                    Logout();
                }
                return xml;
            }
            catch (Exception ex)
            {
                return new XDocument(new XElement("QRZDatabase", new XElement("Session", new XElement("Error", $"Failed to parse XML response: {ex.Message}"))));
            }
        }

        public void Logout()
        {
            _sessionKey = null;
        }
    }
}

