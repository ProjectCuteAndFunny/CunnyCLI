using System.CommandLine;
using System.Text.Json;
using System.Net;

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
            if (!"safebooru|gelbooru|danbooru|lolibooru|yandere|konachan".Contains(val.GetValueOrDefault<string>()!)) {
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
        var threads = new Option<int>(
            name: "--threads",
            description: "Maximum amount of download threads"
        );
        threads.SetDefaultValue(5);
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
            threads,
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

        var progress = 0;

        downloadCommand.SetHandler(async (string booru, string tags, string downloadPath, string? cunnyapiUrl, int amount, int skip, int maxThreads) => {
            var results = await CunnyAPIClient.Get(cunnyapiUrl ?? Globals.DefaultCunnyAPIURL, booru, tags, amount, skip);

            Globals.Logs.CollectionChanged += (_, e) => Console.Write($"\u001b[s\u001b[22;37m[\u001b[34m{downloadThreads.Count}\u001b[37m/\u001b[32m{progress}\u001b[37m/\u001b[0m{amount}\u001b[37m]\u001b[0m {Globals.Logs[^1]}\u001b[0J\u001b[u");

            foreach (var item in results) {
                var _dirPath = Path.Combine(downloadPath, tags, new Uri(item.ImageURL).Host);
                var _filePath = Path.Combine(_dirPath, $"{item.Hash}-{item.Width}x{item.Height}{Path.GetExtension(item.ImageURL)}");
                var _filePathPart = $"{_filePath}.part";

                if (File.Exists(_filePath)) {
                    lock (logsLock) {
                        Globals.Logs.Add($"Skipping, because it is already downloaded: {Path.GetFileName(_filePath)}");
                    }
                    continue;
                }

                Directory.CreateDirectory(_dirPath);
                lock (logsLock) {
                    Globals.Logs.Add($"Saving to: {Path.GetFileName(_filePath)}");
                }

                while (downloadThreads.Count > maxThreads) {}

                var _task = Task.Run(async () => {
                    using var _dlstrm = await Globals.HttpClient.GetStreamAsync(item.ImageURL);
                    using var _wstrm = File.Open(_filePathPart, FileMode.Create);

                    _dlstrm.CopyTo(_wstrm);

                    File.Move(_filePathPart, _filePath);
                    lock (logsLock) {
                        Globals.Logs.Add($"Saved {Path.GetFileName(_filePath)}");
                        progress++;
                    }
                });

                downloadThreads.Add(_task);
                _ = Task.Run(() => {
                    while (!_task.IsCompleted) {}

                    lock (downloadThreadsLock) {
                        downloadThreads.Remove(_task);
                    }
                });
            }
        }, booru, tags, downloadPath, cunnyAPIUrl, amount, skip, threads);

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
    private readonly static object logsLock = new();
    private readonly static object downloadThreadsLock = new();
    private static readonly List<Task> downloadThreads = new();
}