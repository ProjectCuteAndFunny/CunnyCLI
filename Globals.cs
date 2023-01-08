using System.Collections.ObjectModel;

namespace GermanBread.CunnyCLI;

public static class Globals {
    public static HttpClient Client { get; set; } = new();
    public readonly static ObservableCollection<string> Logs = new();
    public const string DefaultCunnyApiUrl = "https://cunnyapi.breadwas.uber.space";
}