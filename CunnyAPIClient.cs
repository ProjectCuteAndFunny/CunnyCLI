using System.Net.Http.Json;

namespace GermanBread.CunnyCLI;

public static class CunnyApiClient {
    public static async Task<List<CunnyJsonElement>> Get(string baseUrl, string booru, string tags, int amount,
        int skip)
    {
        var url = $"{baseUrl}/api/v1/{booru}/{tags}/{amount};{skip}";
        return (await Globals.Client.GetFromJsonAsync<List<CunnyJsonElement>>(url))!;
    }
}