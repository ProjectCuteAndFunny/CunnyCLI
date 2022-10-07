using System.Net;
using System.Text.Json;
using System.CommandLine;

namespace GermanBread.cunnycli;

public static class Program {
    public static async Task<int> Main(string[] Args) {
        var booru = new Option<string>(
            name: "--booru",
            description: "One of <safebooru|gelbooru|danbooru|lolibooru|yandere|konachan>"
        ) {
            IsRequired = true
        };
        booru.AddValidator((val) => {
            if (!"safebooru|gelbooru|danbooru|lolibooru|yandere|konachan".Contains(val.GetValueOrDefault<string?>())) {
                Console.Error.WriteLine("Invalid booru.");
                Environment.Exit(2);
            }
        });
        var tags = new Option<string>(
            name: "--tags",
            description: "Tags to search for, separated by '+'"
        ) {
            IsRequired = true
        };
        var amount = new Option<int>(
            name: "--count",
            description: "Amount of images"
        );
        amount.SetDefaultValue(1);
        var showTags = new Option<bool>(
            name: "--show-tags",
            description: "Show tags in results"
        );
        showTags.SetDefaultValue(false);
        var outputJson = new Option<bool>(
            name: "--json",
            description: "Output JSON instead of pretty output"
        );
        outputJson.SetDefaultValue(false);
        var skip = new Option<int>(
            name: "--skip",
            description: "How many images should be skipped before images are enumerated."
        );
        skip.SetDefaultValue(0);
        var cunnyAPIUrl = new Option<string>(
            name: "--cunnyapi-url",
            description: "Full base URL to a CunnyAPI instance. See https://github.com/ProjectCuteAndFunny/CunnyApi for self-hosting instructions."
        );
        cunnyAPIUrl.AddValidator(async (val) => {
            var url = val.GetValueOrDefault<string?>();
            if (url is null) return;

            try {
                _ = await Globals.HttpClient.GetAsync($"{url}/api/alive");
            } catch (HttpRequestException ex) {
                if (ex.StatusCode != HttpStatusCode.NotFound && ex.StatusCode != HttpStatusCode.InternalServerError) {
                    Console.Error.WriteLine($"\"{url}\" is not a Cunny API instance");
                    Environment.Exit(2);
                } else {
                    Console.Error.WriteLine("Warning: Unable to verify if the URL is a CunnyAPI instance");
                }
            }
            val.ErrorMessage = "Invalid ";
        });
        var downloadPath = new Option<string>(
            name: "--path",
            description: "A valid path to a directory"
        );
        downloadPath.SetDefaultValue(Path.Combine(Environment.CurrentDirectory, "cunnycli-downloads"));

        var downloadCommand = new Command(
            name: "download",
            description: "Download images by tags from booru"
        ) {
            tags,
            skip,
            booru,
            amount,
            cunnyAPIUrl,
            downloadPath
        };
        var searchCommand = new Command(
            name: "search",
            description: "Search for images based on tags in a booru"
        ) {
            tags,
            skip,
            booru,
            amount,
            showTags,
            outputJson,
            cunnyAPIUrl,
            downloadPath
        };

        var rootCommand = new RootCommand(
            description: "Uooh CLI. Can query images and download images"
        ) {
            searchCommand,
            downloadCommand
        };

        downloadCommand.SetHandler(async (string booru, string tags, string downloadPath, string? cunnyapiUrl, int amount, int skip) => {
            var results = await CunnyAPIClient.Get(cunnyapiUrl ?? Globals.DefaultCunnyAPIURL, booru, tags, amount, skip);

            foreach (var item in results) {
                var _dirPath = Path.Combine(downloadPath, tags, new Uri(item.ImageURL).Host);
                var _filePath = Path.Combine(_dirPath, $"{item.Hash}-{item.Width}x{item.Height}{Path.GetExtension(item.ImageURL)}");
                var _filePathPart = Path.Combine(_dirPath, $"{item.Hash}-{item.Width}x{item.Height}{Path.GetExtension(item.ImageURL)}");

                if (File.Exists(_filePath)) {
                    Console.Error.WriteLine($"Skipping, because it is already downloaded: {_filePath}");
                    continue;
                }

                Directory.CreateDirectory(_dirPath);
                Console.Error.WriteLine($"Saving to: {_filePath}");

                using var _dlstrm = await Globals.HttpClient.GetStreamAsync(item.ImageURL);
                using var _wstrm = File.Open(_filePathPart, FileMode.Create);

                _dlstrm.CopyTo(_wstrm);

                File.Move(_filePathPart, _filePath);
            }
        }, booru, tags, downloadPath, cunnyAPIUrl, amount, skip);

        searchCommand.SetHandler(async (string booru, string tags, string? cunnyapiUrl, int amount, int skip, bool showTags, bool outputJson) => {
            var results = await CunnyAPIClient.Get(cunnyapiUrl ?? Globals.DefaultCunnyAPIURL, booru, tags, amount, skip);

            if (outputJson) {
                Console.Write(JsonSerializer.Serialize(results));
                return;
            }

            for (int i = 0; i < results.Count; i++) {
                var item = results[i];

                Console.ForegroundColor = i % 2 == 0 ? ConsoleColor.Cyan : ConsoleColor.Blue;
                Console.WriteLine($"{i}: {(showTags ? $"[ {string.Join(' ', item.Tags)} ]" : "")}({item.Width}x{item.Height}) - {item.PostURL}");
                Console.ResetColor();
            }
        }, booru, tags, cunnyAPIUrl, amount, skip, showTags, outputJson);

        return await rootCommand.InvokeAsync(Args);
    }
}