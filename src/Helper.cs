using DownloadBDS.Types;
using System.Text;
using System.Text.Json;
using Version = DownloadBDS.Types.Version;

namespace DownloadBDS;

internal static class Helper
{
    private const string ProcessedDataSavePath = "ProcessedData.json";
    private static readonly Dictionary<string, PreviewVersion> s_processedData;

    static Helper()
    {
        if (File.Exists(ProcessedDataSavePath))
        {
            s_processedData =
                JsonSerializer.Deserialize<Dictionary<string, PreviewVersion>>(
                    File.ReadAllText(ProcessedDataSavePath))!;
        }

        s_processedData ??= [];
    }

    public static async Task<Result> DownloadAsync(Platform platform, bool isPreviewVersion, Version version)
    {
        string platformString = platform switch
        {
            Platform.Linux => "linux",
            Platform.Windows => "win",
            _ => throw new IndexOutOfRangeException()
        };
        if (isPreviewVersion)
        {
            platformString += "-preview";
        }

        HttpClient httpClient = new();
        string path = Path.Combine(platform.ToString(), isPreviewVersion ? "Preview" : string.Empty,
            $"bedrock-server-{version}.zip");
        byte[] result;
        try
        {
            result = await httpClient.GetByteArrayAsync(
                $"https://minecraft.azureedge.net/bin-{platformString}/bedrock-server-{version}.zip");
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("404"))
        {
            return Result.NotFound;
        }
        catch
        {
            return Result.Failed;
        }

        await File.WriteAllBytesAsync(path, result);
        return Result.Success;
    }

    public static void SaveData(Platform platform, bool isPreviewVersion, Version version)
    {
        if (!s_processedData.TryGetValue(version.ToString(), out PreviewVersion? data))
        {
            data = new();
        }

        switch (platform)
        {
            case Platform.Linux:
                {
                    data.Linux = isPreviewVersion;
                    break;
                }
            case Platform.Windows:
                {
                    data.Windows = isPreviewVersion;
                    break;
                }
            default:
                {
                    throw new IndexOutOfRangeException();
                }
        }

        string json = JsonSerializer.Serialize(s_processedData);
        lock (s_processedData)
        {
            File.WriteAllText(ProcessedDataSavePath, json);
        }
    }

    public static void Output(int line, params string[] messages)
    {
        lock (Console.Out)
        {
            Console.SetCursorPosition(0, line);
            foreach (string message in messages)
            {
                Console.Write($"{message,-10}");
            }

            StringBuilder trailingSpaces = new();
            int trailingSpacesCount =
                Console.WindowWidth - Console.GetCursorPosition().Left - Environment.NewLine.Length;
            for (int i = 0; i < trailingSpacesCount; i++)
            {
                trailingSpaces.Append(' ');
            }

            Console.WriteLine(trailingSpaces);
        }
    }
}