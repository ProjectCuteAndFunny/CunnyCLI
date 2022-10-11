using System.Net;
using System.Text.Json;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Net.Http.Json;
using GermanBread.CunnyCLI;

var booru = new Option<string>(new[] { "-b", "--booru" }) { 
    IsRequired = true
};

booru.AddCompletions("safebooru", "gelbooru", "danbooru", "lolibooru", "yandere", "konachan");
booru.GetCompletions();

var tags = new Option<string>(new[] {"-t", "--tags"},"Tags to search for, separated by '+'") {
    IsRequired = true
};

var amount = new Option<int>(new[] { "-c", "--count" },"Amount of images");
amount.SetDefaultValue(1);
        
var showTags = new Option<bool>("--show-tags","Show tags in results");
showTags.SetDefaultValue(false);
        
var outputJson = new Option<bool>("--json","Output JSON instead of pretty output");
outputJson.SetDefaultValue(false);
        
var skip = new Option<int>("--skip","How many images should be skipped before images are enumerated.");
skip.SetDefaultValue(0);
        
var cunnyApiUrl = new Option<string>("--cunnyapi-url",
    "Full base URL to a CunnyAPI instance. See https://github.com/ProjectCuteAndFunny/CunnyApi for self-hosting instructions.");
cunnyApiUrl.SetDefaultValue(Globals.DefaultCunnyApiurl);
cunnyApiUrl.AddValidator(CunnyApiUrlValidator);

var downloadPath = new Option<string>("--path","A valid path to a directory");
downloadPath.SetDefaultValue(Path.Combine(Environment.CurrentDirectory, "cunnycli-downloads"));

var downloadCommand = new Command("download", "Download images by tags from booru") { };

var searchCommand = new Command("search", "Search for images based on tags in a booru") { };

var rootCommand = new RootCommand("Uooh CLI. Can query images and download images") {
    searchCommand,
    downloadCommand
};

foreach (var option in new Option[] { booru, tags, amount, showTags, outputJson, skip, cunnyApiUrl, downloadPath })
    rootCommand.AddOption(option);

downloadCommand.SetHandler(DownloadHandler);

async Task DownloadHandler(InvocationContext invocationContext)
{
    var booruValue = invocationContext.ParseResult.GetValueForOption(booru);
    var tagsValue = invocationContext.ParseResult.GetValueForOption(tags);
    var amountValue = invocationContext.ParseResult.GetValueForOption(amount);
    var cunnyApiUrlValue = invocationContext.ParseResult.GetValueForOption(cunnyApiUrl);
    var downloadPathValue = invocationContext.ParseResult.GetValueForOption(downloadPath);
    var skipValue = invocationContext.ParseResult.GetValueForOption(skip);
    
    var results =
        await Globals.HttpClient.GetFromJsonAsync<List<CunnyJsonElement>>(
            $"{cunnyApiUrlValue}/api/v1/{booruValue}/{tagsValue}/{amountValue};{skipValue}");
    
    foreach (var item in results!) {
        var dirPath = Path.Combine(downloadPathValue!, tagsValue!, new Uri(item.ImageUrl).Host);
        var filePath = Path.Combine(dirPath, $"{item.Hash}-{item.Width}x{item.Height}{Path.GetExtension(item.ImageUrl)}");
        var filePathPart = Path.Combine(dirPath, $"{item.Hash}-{item.Width}x{item.Height}{Path.GetExtension(item.ImageUrl)}");

        if (File.Exists(filePath)) {
            await Console.Error.WriteLineAsync($"Skipping, because it is already downloaded: {filePath}");
            continue;
        }

        Directory.CreateDirectory(dirPath);
        await Console.Error.WriteLineAsync($"Saving to: {filePath}");
        
        var dlstrm = await Globals.HttpClient.GetStreamAsync(item.ImageUrl);
        var wstrm = File.Open(filePathPart, FileMode.Create);
        await dlstrm.CopyToAsync(wstrm);
        await wstrm.FlushAsync();
        dlstrm.Dispose();
        wstrm.Dispose();

        Console.WriteLine($"Downloading {item.ImageUrl}...");
        
        File.Move(filePathPart, filePath);
    }
}

searchCommand.SetHandler(SearchHandler);

async Task SearchHandler(InvocationContext invocationContext)
{
    var booruValue = invocationContext.ParseResult.GetValueForOption(booru);
    var tagsValue = invocationContext.ParseResult.GetValueForOption(tags);
    var amountValue = invocationContext.ParseResult.GetValueForOption(amount);
    var cunnyApiUrlValue = invocationContext.ParseResult.GetValueForOption(cunnyApiUrl);
    var skipValue = invocationContext.ParseResult.GetValueForOption(skip);
    var outputJsonValue = invocationContext.ParseResult.GetValueForOption(outputJson);
    
    var results =
        await Globals.HttpClient.GetFromJsonAsync<List<CunnyJsonElement>>(
            $"{cunnyApiUrlValue}/api/v1/{booruValue}/{tagsValue}/{amountValue};{skipValue}");
    
    if (outputJsonValue) {
        Console.Write(JsonSerializer.Serialize(results));
        return;
    }
    
    for (var i = 0; i < results!.Count; i++) {
        Console.ForegroundColor = i % 2 == 0 ? ConsoleColor.Cyan : ConsoleColor.Blue;
        Console.WriteLine($"{i}: ({results[i].Width}x{results[i].Height}) - {results[i].PostUrl}");
        Console.ResetColor();
    }

}

async void CunnyApiUrlValidator(OptionResult val)
{
    var url = val.GetValueOrDefault<string?>();
    if (url is null) return;

    try
    {
        _ = await Globals.HttpClient.GetAsync($"{url}/api/alive");
    }
    catch (HttpRequestException ex)
    {
        if (ex.StatusCode != HttpStatusCode.NotFound && ex.StatusCode != HttpStatusCode.InternalServerError)
        {
            await Console.Error.WriteLineAsync($"\"{url}\" is not a Cunny API instance");
            Environment.Exit(2);
        }
        else
        {
            await Console.Error.WriteLineAsync("Warning: Unable to verify if the URL is a CunnyAPI instance");
        }
    }

    val.ErrorMessage = "Invalid ";
}

await rootCommand.InvokeAsync(args);