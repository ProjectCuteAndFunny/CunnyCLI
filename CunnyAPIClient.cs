using System.Net;
using System.Net.Http.Json;

namespace GermanBread.cunnycli;

public static class CunnyAPIClient {
    public async static Task<List<CunnyJSONElement>> Get(string baseUrl, string booru, string tags, int amount, int skip) {
        var url = $"{baseUrl}/api/v1/{booru}/{tags}/{amount};{skip}";
        return (await Globals.HttpClient.GetFromJsonAsync<List<CunnyJSONElement>>(url))!;
    }
}