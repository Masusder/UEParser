using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace UEParser.Network;

public class NetAPI
{
    private static readonly string cookieFilePath = Path.Combine(GlobalVariables.rootDir, "Dependencies", "cookies.json");
    private static readonly CookieContainer cookieContainer = new();
    private static readonly HttpClientHandler handler = new()
    {
        CookieContainer = cookieContainer
    };

    private static readonly HttpClient client = new(handler);

    public class ApiResponse(bool success, string data, string errorMessage)
    {
        public bool Success { get; } = success;
        public string Data { get; } = data;
        public string? ErrorMessage { get; } = errorMessage;
    }

    public static async Task<ApiResponse> FetchUrl(string url)
    {
        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            return new ApiResponse(true, responseBody, "");
        }
        catch (HttpRequestException e)
        {
            return new ApiResponse(false, "", $"Error: {e.Message}");
        }
    }

    public static async Task<byte[]> FetchFileBytesAsync(string fileUrl)
    {
        try
        {
            HttpResponseMessage response = await client.GetAsync(fileUrl);
            response.EnsureSuccessStatusCode(); 

            byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();
            return fileBytes;
        }
        catch
        {
            throw;
        }
    }

    public static async Task<ApiResponse> PostRequest(
        string url,
        Dictionary<string, string>? headers = null,
        object? payload = null)
    {
        try
        {
            // Clear any existing headers
            client.DefaultRequestHeaders.Clear();

            // Add headers if provided
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Send POST request
            HttpResponseMessage response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode(); // Throws if not successful

            // Capture and add cookies from the response headers
            if (response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders))
            {
                foreach (var cookieHeader in cookieHeaders)
                {
                    var cookies = ParseCookies(cookieHeader, url);
                    foreach (var cookie in cookies)
                    {
                        // Add cookie to the container and save cookies locally
                        SetCookie(new Uri(url), cookie);
                    }
                }
            }

            string responseBody = await response.Content.ReadAsStringAsync();

            return new ApiResponse(true, responseBody, "");
        }
        catch (HttpRequestException e)
        {
            return new ApiResponse(false, "", $"Error: {e.Message}");
        }
    }

    private static List<Cookie> ParseCookies(string setCookieHeader, string baseUrl)
    {
        var cookies = new List<Cookie>();
        var cookieParts = setCookieHeader.Split([';'], StringSplitOptions.RemoveEmptyEntries);

        if (cookieParts.Length == 0) return cookies;

        var cookieKeyValue = cookieParts[0].Split(['='], 2);
        if (cookieKeyValue.Length != 2) return cookies; // Skip invalid cookies

        var cookieName = cookieKeyValue[0].Trim();
        var cookieValue = cookieKeyValue[1].Trim();

        var cookie = new Cookie(cookieName, cookieValue)
        {
            Domain = new Uri(baseUrl).Host,
            Path = "/"
        };

        // Process additional attributes
        for (int i = 1; i < cookieParts.Length; i++)
        {
            var attribute = cookieParts[i].Trim();
            if (string.IsNullOrEmpty(attribute)) continue;

            var attributeParts = attribute.Split(['='], 2);
            var attributeName = attributeParts[0].Trim().ToLower();

            switch (attributeName)
            {
                case "domain":
                    if (attributeParts.Length > 1)
                    {
                        cookie.Domain = attributeParts[1].Trim();
                    }
                    break;
                case "path":
                    if (attributeParts.Length > 1)
                    {
                        cookie.Path = attributeParts[1].Trim();
                    }
                    break;
                case "expires":
                    if (attributeParts.Length > 1 && DateTime.TryParse(attributeParts[1].Trim(), out var expires))
                    {
                        cookie.Expires = expires;
                    }
                    break;
                //case "secure":
                //    cookie.Secure = true;
                //    break;
                //case "httponly":
                //    cookie.HttpOnly = true;
                //    break;
            }
        }

        cookies.Add(cookie);
        return cookies;
    }


    public static void SetCookie(Uri baseUrl, Cookie cookie)
    {
        cookieContainer.Add(baseUrl, cookie);
        SaveCookies();
    }

    private static void SaveCookies()
    {
        var cookieList = new List<SerializableCookie>();
        var allDomains = cookieContainer.GetAllCookies().Select(c => c.Domain).Distinct();

        foreach (var domain in allDomains)
        {
            Uri domainUri = new($"https://{domain}/"); // I assume domain is always secure

            foreach (Cookie cookie in cookieContainer.GetCookies(domainUri))
            {
                // Check if the cookie is already in the list
                if (!cookieList.Any(c => c.Name == cookie.Name && c.Domain == cookie.Domain && c.Path == cookie.Path))
                {
                    cookieList.Add(new SerializableCookie(cookie));
                }
            }
        }

        File.WriteAllText(cookieFilePath, JsonConvert.SerializeObject(cookieList));
    }

    public static void LoadAndValidateCookies()
    {
        if (File.Exists(cookieFilePath))
        {
            var cookieData = File.ReadAllText(cookieFilePath);
            var cookieList = JsonConvert.DeserializeObject<List<SerializableCookie>>(cookieData);

            if (cookieList != null)
            {
                foreach (var serializableCookie in cookieList)
                {
                    // Check if the cookie is expired
                    if (serializableCookie.Expires > DateTime.Now)
                    { 
                        var uri = new Uri($"https://{serializableCookie.Domain}"); // I assume domain is always secure
                        cookieContainer.Add(uri, serializableCookie.ToCookie());
                    }
                }
            }
        }
    }


    public static bool IsAnyCookieNotExpired()
    {
        var allCookies = cookieContainer.GetAllCookies();

        // Check if at least one cookie is not expired
        return allCookies.Cast<Cookie>().Any(cookie => !cookie.Expired);
    }

    public static CookieCollection GetCookies(Uri baseUrl)
    {
        return cookieContainer.GetCookies(baseUrl);
    }

    private class SerializableCookie
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Domain { get; set; }
        public string Path { get; set; }
        public DateTime Expires { get; set; }

        // Parameterless constructor for deserialization
#pragma warning disable CS8618 
        public SerializableCookie() { }
#pragma warning restore CS8618

        // Constructor to initialize from a Cookie object
        public SerializableCookie(Cookie cookie)
        {
            Name = cookie.Name;
            Value = cookie.Value;
            Domain = cookie.Domain;
            Path = cookie.Path;
            Expires = cookie.Expires;
        }

        public Cookie ToCookie()
        {
            return new Cookie(Name, Value, Path, Domain)
            {
                Expires = Expires,
            };
        }
    }

}