using System.Collections.ObjectModel;

namespace GermanBread.cunnycli;

public static class Globals {
    public static readonly HttpClient HttpClient = new();
    public static readonly ObservableCollection<string> Logs = new();
    public static readonly string DefaultCunnyAPIURL = "https://cunnyapi.breadwas.uber.space";
}