using System.CommandLine;
using System.Net;
using System.Text.Json;
using GermanBread.CunnyCLI;

Option<string> booru = new(new []{"--booru", "-b"},
    "One of <safebooru|gelbooru|danbooru|lolibooru|yandere|konachan>") {
     IsRequired = true
 };
booru.AddValidator(val =>
{
    if ("safebooru|gelbooru|danbooru|lolibooru|yandere|konachan"
        .Contains(val.GetValueOrDefault<string>() ?? string.Empty))
        return;
    Console.Error.WriteLine("Invalid booru.");
    Environment.Exit(2);
});

Option<string> tags = new(new []{"--tags", "-t"}, "Tags to search for, separated by '+'") {
    IsRequired = true
};

Option<int> amount = new(new []{"--count", "-c"}, "Amount of images") {
    IsRequired = true
 };
amount.SetDefaultValue(1);

Option<int> threads = new(new []{"--threads", "-th"}, "Maximum amount of download threads") {
    IsRequired = true
 };
threads.AddValidator(val =>
{
    if (val.GetValueOrDefault<int>() <= 0)
        val.Option.SetDefaultValue(1);
});
threads.SetDefaultValue(Environment.ProcessorCount);

Option<bool> showTags = new(new []{"--show-tags", "-st"}, "Show tags in results");
showTags.SetDefaultValue(false);

Option<bool> outputJson = new(new []{"--json", "-j"}, "Output JSON instead of pretty output");
outputJson.SetDefaultValue(false);

Option<int> skip = new(new []{"--skip", "-s"}, "How many images should be skipped before images are enumerated.") {
    IsRequired = true
};
skip.SetDefaultValue(0);

Option<string> cunnyApiUrl = new(new[] { "--cunnyapi-url", "-cau" },
    "Full base URL to a CunnyAPI instance. See https://github.com/ProjectCuteAndFunny/CunnyApi for self-hosting instructions.")
{
    IsRequired = true
};
cunnyApiUrl.AddValidator(val => {
    var url = val.GetValueOrDefault<string?>();
    if (url is null)
         return;

    var response = Globals.Client.GetAsync($"{url}/api/v1/safebooru/1girl/1").Result;
    if (response.StatusCode is not (HttpStatusCode.NotFound or HttpStatusCode.InternalServerError))
        return;

    Console.Error.WriteLineAsync($"\"{url}\" is not a Cunny API instance");
    val.ErrorMessage = "Invalid";
});
cunnyApiUrl.SetDefaultValueFactory(Globals.DefaultCunnyApiurl.ToString);

Option<string> downloadPath = new(new []{"--path", "-p"}, "A valid path to a directory") {
    IsRequired = true
};
downloadPath.SetDefaultValue(Path.Combine(Environment.CurrentDirectory, "cunnycli-downloads"));

Option<string[]?> excludeTags = new(new[] { "--exclude-tags", "-et" }, "Tags to exclude, separated by space")
{
    AllowMultipleArgumentsPerToken = true,
    IsRequired = false
};

Command downloadCommand = new("download", "Download images by tags from booru")
{
    tags,
    skip,
    booru,
    amount,
    threads,
    cunnyApiUrl,
    downloadPath
};

Command searchCommand = new("search", "Search for images based on tags in a booru")
{
    tags,
    skip,
    booru,
    amount,
    showTags,
    outputJson,
    cunnyApiUrl,
    excludeTags,
    downloadPath
};

RootCommand rootCommand = new("Uooh CLI. Can query images and download images")
{
    searchCommand,
    downloadCommand
};

foreach (var option in new Option[]
         {
             booru,
             tags,
             amount,
             threads,
             showTags,
             outputJson,
             skip,
             cunnyApiUrl,
             downloadPath
         })
    rootCommand.AddOption(option);

var progress = 0;

downloadCommand.SetHandler(
    async (booruValue, tagsValue, downloadPathValue, cunnyapiUrl, amountValue, skipValue, excludeTagsValue, maxThreads) =>
    {
        var results = await CunnyApiClient.Get(cunnyapiUrl, booruValue, tagsValue, amountValue, skipValue);

        if (excludeTagsValue is not null)
            foreach (var element in results.Where(element => element.Tags.Any(excludeTagsValue.Contains)).ToList())
                results.Remove(element);

        Globals.Logs.CollectionChanged += (_, _) =>
        Console.WriteLine(
            $"\u001b[s\u001b[22;37m[\u001b[32m{progress}\u001b[37m/\u001b[0m{amountValue}\u001b[37m]\u001b[0m {Globals.Logs[^1]}\u001b[0J\u001b[u");

        Parallel.ForEach(results.ToList(), new ParallelOptions { MaxDegreeOfParallelism = maxThreads }, itemValue =>
        {
            var dirPath = Path.Combine(downloadPathValue, tagsValue, new Uri(itemValue.ImageURL).Host);
            var filePath = Path.Combine(dirPath, $"{itemValue.ID}-{itemValue.Hash}-{itemValue.Width}x{itemValue.Height}{Path.GetExtension(itemValue.ImageURL)}");
            var filePathPart = $"{filePath}.part";

            if (File.Exists(filePath))
            {
                Globals.Logs.Add($"Skipping \u001b[22;37m{Path.GetFileName(filePath)}\u001b[0m");
                progress++;
                return;
            }

            if(!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            Globals.Logs.Add($"Downloading \u001b[22;37m{Path.GetFileName(filePath)}\u001b[0m");
            using var downloadStream = Globals.Client.GetStreamAsync(itemValue.ImageURL).Result;
            using var writeStream = File.Open(filePathPart, FileMode.Create);
            downloadStream.CopyTo(writeStream);
            File.Move(filePathPart, filePath);
            Globals.Logs.Add($"Saved \u001b[22;37m{Path.GetFileName(filePath)}\u001b[0m");

            progress++;
        });

}, booru, tags, downloadPath, cunnyApiUrl, amount, skip, excludeTags, threads);

searchCommand.SetHandler(async (booruValue, tagsValue, cunnyapiUrl, amountValue, skipValue, showTagsValue,
    excludeTagsValue, outputJsonValue) =>
{
    var results = await CunnyApiClient.Get(cunnyapiUrl, booruValue, tagsValue, amountValue, skipValue);

    if (excludeTagsValue is not null)
        foreach (var element in results.Where(element => element.Tags.Any(excludeTagsValue.Contains)).ToList())
            results.Remove(element);

    if (outputJsonValue)
    {
        Console.Write(JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
        return;
    }

    var i = 0;
    foreach (var item in results.ToList())
    {
        Console.ForegroundColor = i % 2 == 0 ? ConsoleColor.Cyan : ConsoleColor.Blue;
        Console.WriteLine($"{i}: {(showTagsValue ? $"[ {string.Join(' ', item.Tags)} ]" : "")}({item.Width}x{item.Height}) - {item.PostURL}");
        Console.ResetColor();
        i++;
    }
}, booru, tags, cunnyApiUrl, amount, skip, showTags, excludeTags, outputJson);

await rootCommand.InvokeAsync(args);