using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace DownloadBDS;

internal class Program
{
    private static Version s_version;
    private static readonly ConcurrentBag<(Platform Platform, bool IsPreviewVersion, Version Verson)> ts_data;
    private static Dictionary<Version, PreviewVersion> s_processedData;
    private static bool s_finished;

    public struct Version
    {
        public Version(int major, int minor, int build, int revision)
        {
            Major = major;
            Minor = minor;
            Build = build;
            Revision = revision;
        }

        public int Major { get; set; }
        public int Minor { get; set; }
        public int Build { get; set; }
        public int Revision { get; set; }

        public override readonly string ToString()
        {
            return $"{Major}.{Minor}.{Build}.{((Minor > 15 && Build > 0 || Minor > 16) && Revision is > 0 and < 10 ? "0" : string.Empty)}{Revision}";
        }
    }
    public class PreviewVersion
    {
        public bool? Linux { get; set; }
        public bool? Windows { get; set; }
    }

    private enum Platform
    {
        Linux,
        Windows
    }

    static Program()
    {
        s_version = new(1, 6, default, default);
        ts_data = new();
        s_processedData = new();
        s_finished = false;
    }

    private static async Task Main()
    {
        List<Task> tasks = new();
        if (File.Exists("ProcessedData.json"))
        {
            s_processedData =
                JsonSerializer.Deserialize<Dictionary<Version, PreviewVersion>>(File.ReadAllText("ProcessedData.json")) ?? s_processedData;
        }
        Console.Write("开始自：");
        string? input = Console.ReadLine();
        Console.Clear();
        if (!string.IsNullOrWhiteSpace(input))
        {
            string[] temp = input.Split('.');
            for (int i = 0; i < temp.Length; i++)
            {
                switch (i)
                {
                    case 0:
                        s_version.Major = Convert.ToInt32(temp[i]);
                        break;
                    case 1:
                        s_version.Minor = Convert.ToInt32(temp[i]);
                        break;
                    case 2:
                        s_version.Build = Convert.ToInt32(temp[i]);
                        break;
                    case 3:
                        s_version.Revision = Convert.ToInt32(temp[i]);
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }
        Directory.CreateDirectory("Linux");
        Directory.CreateDirectory("Linux/Preview");
        Directory.CreateDirectory("Windows");
        Directory.CreateDirectory("Windows/Preview");
        _ = Task.Run(() =>
        {
            while (s_version.Minor < 21)
            {
                s_version.Revision++;
                if (s_version.Revision > 35)
                {
                    s_version.Revision = 0;
                    s_version.Build++;
                }
                if (s_version.Build > (s_version.Minor < 14 ? 5 : 222))
                {
                    s_version.Build = 0;
                    s_version.Minor++;
                }
                if (s_version.Minor > 20)
                {
                    s_version.Major++;
                    return;
                }
                ts_data.Add((Platform.Linux, false, s_version));
                ts_data.Add((Platform.Linux, true, s_version));
                ts_data.Add((Platform.Windows, false, s_version));
                ts_data.Add((Platform.Windows, true, s_version));
            }
            s_finished = true;
        });
        for (int i = 0; i < Environment.ProcessorCount * 2; i++)
        {
            int index = i;
            async Task @this()
            {
                while (!s_finished)
                {
                    while (ts_data.TryTake(out (Platform Platform, bool IsPreviewVersion, Version Verson) data))
                    {
                        await Download(index, data.Platform, data.IsPreviewVersion, data.Verson);
                    }
                    Output(index, index.ToString(), "空闲");
                }
            }
            tasks.Add(@this());
        }
        foreach (Task task in tasks)
        {
            await task;
        }
        Console.SetCursorPosition(0, Environment.ProcessorCount);
        Console.WriteLine("下载完毕");
    }

    private static async Task Download(int index, Platform platform, bool isPreviewVersion, Version version)
    {
        Output(index, index.ToString(), platform.ToString(), isPreviewVersion.ToString(), version.ToString());
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
        try
        {
            File.WriteAllBytes(Path.Combine(platform.ToString(), isPreviewVersion ? "Preview" : string.Empty, $"bedrock-server-{version}.zip"),
                await httpClient.GetByteArrayAsync($"https://minecraft.azureedge.net/bin-{platformString}/bedrock-server-{version}.zip"));
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("404"))
        {
            return;
        }
        catch
        {
            await Download(index, platform, isPreviewVersion, version);
            return;
        }
        if (!s_processedData.ContainsKey(version))
        {
            s_processedData[version] = new();
        }
        switch (platform)
        {
            case Platform.Linux:
                s_processedData[version].Linux = isPreviewVersion;
                break;
            case Platform.Windows:
                s_processedData[version].Windows = isPreviewVersion;
                break;
        }
        File.WriteAllText("ProcessedData.json",
            JsonSerializer.Serialize(s_processedData));
    }

    private static void Output(int line, params string[] messages)
    {
        lock (Console.Out)
        {
            Console.SetCursorPosition(0, line);
            foreach (string message in messages)
            {
                Console.Write($"{message,-16}");
            }
            StringBuilder trailingSpaces = new();
            int trailingSpacesCount = Console.WindowWidth -
                Console.GetCursorPosition().Left -
                Environment.NewLine.Length;
            for (int i = 0; i < trailingSpacesCount; i++)
            {
                trailingSpaces.Append(' ');
            }
            Console.WriteLine(trailingSpaces);
        }
    }
}
