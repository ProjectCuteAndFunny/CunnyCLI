using System.Net;
using System.Text.Json;

namespace GermanBread.CunnyCLI;

public static class CunnyApiClient {
    public static async Task<List<CunnyJsonElement>?> Get(string baseUrl, string booru, string tags, int amount,
        int skip)
    {
        var url = $"{baseUrl}/api/v1/{booru}/{tags}/{amount};{skip}";
        var response = await Globals.Client.GetAsync(url);

        if (response.StatusCode is HttpStatusCode.OK)
            return JsonSerializer.Deserialize<List<CunnyJsonElement>>(await response.Content.ReadAsStringAsync());

        await Console.Error.WriteLineAsync($"Error: {response.StatusCode}");
        return null;
    }
}