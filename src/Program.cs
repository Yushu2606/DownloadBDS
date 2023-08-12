using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace DownloadBDS;

internal class Program
{
    private static (int Major, int Minor, int Build, int Revision) s_version;
    private static readonly Dictionary<string, List<string>> s_platforms;
    private static readonly ConcurrentQueue<(string Platform, string Verson)> ts_data;
    private static bool s_finished;

    static Program()
    {
        s_version = (1, 6, default, default);
        s_platforms = new()
        {
            ["win"] = new(),
            ["linux"] = new(),
            ["win-preview"] = new(),
            ["linux-preview"] = new()
        };
        ts_data = new();
        s_finished = false;
    }

    private static async Task Main()
    {
        List<Task> tasks = new();
        foreach ((string platform, List<string> versions) in s_platforms)
        {
            string fileName = $"bds_ver_{platform}.json";
            if (!File.Exists(fileName))
            {
                File.WriteAllText(fileName, JsonSerializer.Serialize(versions));
                continue;
            }
            s_platforms[platform] =
                JsonSerializer.Deserialize<List<string>>(File.ReadAllText(fileName));
        }
        Console.Write("开始自：");
        string input = Console.ReadLine();
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
        foreach (string platform in s_platforms.Keys)
        {
            Directory.CreateDirectory(platform);
        }
        _ = Task.Run(() =>
        {
            while (s_version.Minor < 21)
            {
                string version = $"{s_version.Major}.{s_version.Minor}.{s_version.Build}.{((s_version.Minor > 15 && s_version.Build > 0 || s_version.Minor > 16) && s_version.Revision is > 0 and < 10 ? "0" : string.Empty)}{s_version.Revision}";
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
                foreach (string platform in s_platforms.Keys)
                {
                    ts_data.Enqueue((platform, version));
                }
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
                    while (ts_data.TryDequeue(out (string Platform, string Verson) data))
                    {
                        await Download(index, data.Platform, data.Verson);
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

    private static async Task Download(int index, string platform, string version)
    {
        Output(index, index.ToString(), platform, version);
        HttpClient httpClient = new();
        try
        {
            File.WriteAllBytes(Path.Combine(platform, $"bedrock-server-{version}.zip"),
                await httpClient.GetByteArrayAsync($"https://minecraft.azureedge.net/bin-{platform}/bedrock-server-{version}.zip"));
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("404"))
        {
            return;
        }
        catch
        {
            await Download(index, platform, version);
            return;
        }
        s_platforms[platform].Add(version);
        File.WriteAllText($"bds_ver_{platform}.json",
            JsonSerializer.Serialize(s_platforms[platform]));
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