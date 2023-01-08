using System.Collections.ObjectModel;

namespace GermanBread.CunnyCLI;

public static class Globals
{
    public static HttpClient Client = new();
    public static readonly ObservableCollection<string> Logs = new();
    public const string DefaultCunnyApiurl = "https://cunnyapi.breadwas.uber.space";
}