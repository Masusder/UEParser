using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace UEParser;

public class API
{
    public class ApiResponse(bool success, string data, string errorMessage)
    {
        public bool Success { get; } = success;
        public string Data { get; } = data;
        public string? ErrorMessage { get; } = errorMessage;
    }

    public static async Task<ApiResponse> FetchUrl(string url)
    {
        using var client = new HttpClient();

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
        using var client = new HttpClient();

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
}