using Newtonsoft.Json;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace UEParser.Network;

public class API
{
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
                        // Add cookie to the container
                        cookieContainer.Add(new Uri(url), cookie);
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

    private static List<Cookie> ParseCookies(string cookieHeader, string baseUrl)
    {
        var cookies = new List<Cookie>();
        var cookieParts = cookieHeader.Split(';');

        // Extract name and value from each cookie part
        foreach (var part in cookieParts)
        {
            var kvp = part.Split('=');
            if (kvp.Length == 2)
            {
                var name = kvp[0].Trim();
                var value = kvp[1].Trim();

                // Create a new Cookie object
                var cookie = new Cookie(name, value)
                {
                    Domain = new Uri(baseUrl).Host, // Set the domain to the base URL's domain
                    Path = "/" // Set the path to root or a specific path if needed
                };

                cookies.Add(cookie);
            }
        }

        return cookies;
    }

    public static void SetCookie(Uri baseUrl, Cookie cookie)
    {
        cookieContainer.Add(baseUrl, cookie);
    }

    public static CookieCollection GetCookies(Uri baseUrl)
    {
        return cookieContainer.GetCookies(baseUrl);
    }
}