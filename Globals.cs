using System.Collections.ObjectModel;

namespace GermanBread.CunnyCLI;

public static class Globals
{
    public static HttpClient Client { get; set; } = new();
    public static ObservableCollection<string> Logs { get; } = new();
    public static string DefaultCunnyApiUrl { get; } = "https://cunnyapi.breadwas.uber.space";
}