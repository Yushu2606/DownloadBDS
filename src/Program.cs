using System.Text;
using System.Text.Json;

namespace DownloadBDS;

internal class Program
{
    private static readonly List<int> s_version = new()
    {
        1,
        6,
        0,
        0
    };
    private static readonly Dictionary<string, List<string>> s_platforms = new()
    {
        ["win"] = new(),
        ["linux"] = new(),
        ["win-preview"] = new(),
        ["linux-preview"] = new()
    };
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
            s_platforms[platform] = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(fileName));
        }
        Console.Write("开始自：");
        string input = Console.ReadLine();
        Console.Clear();
        if (!string.IsNullOrWhiteSpace(input))
        {
            string[] temp = input.Split('.');
            for (int i = 0; i < temp.Length; i++)
            {
                s_version[i] = Convert.ToInt32(temp[i]);
            }
        }
        foreach ((string platform, List<string> _) in s_platforms)
        {
            _ = Directory.CreateDirectory(platform);
        }
        for (int i = 0; i < Environment.ProcessorCount; i++)
        {
            int index = i;
            async Task @this()
            {
                Output(index, index.ToString(), "空闲");
                while (s_version[1] < 21)
                {
                    string versionstr = string.Empty;
                    lock (s_version)
                    {
                        versionstr = $"{s_version[0]}.{s_version[1]}.{s_version[2]}.{(((s_version[1] > 15 && s_version[2] > 0) || s_version[1] > 16) && s_version[3] is > 0 and < 10 ? "0" : string.Empty)}{s_version[3]}";
                        s_version[3]++;
                        if (s_version[3] > 35)
                        {
                            s_version[3] = 0;
                            s_version[2]++;
                        }
                        if (s_version[2] > (s_version[1] < 14 ? 5 : 222))
                        {
                            s_version[2] = 0;
                            s_version[1]++;
                        }
                        if (s_version[1] > 20)
                        {
                            s_version[0]++;
                            return;
                        }
                    }
                    foreach ((string platform, List<string> _) in s_platforms)
                    {
                        await Download(index, versionstr, platform);
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

    private static async Task Download(int index, string versionstr, string platformName)
    {
        Output(index, index.ToString(), platformName, versionstr);
        HttpClient httpClient = new();
        try
        {
            File.WriteAllBytes(Path.Combine(platformName, $"bedrock-server-{versionstr}.zip"), await httpClient.GetByteArrayAsync($"https://minecraft.azureedge.net/bin-{platformName}/bedrock-server-{versionstr}.zip"));
        } catch (HttpRequestException ex) when (ex.Message.Contains("404"))
        {
            return;
        } catch
        {
            await Download(index, versionstr, platformName);
            return;
        }
        s_platforms[platformName].Add(versionstr);
        File.WriteAllText($"bds_ver_{platformName}.json", JsonSerializer.Serialize(s_platforms[platformName]));
    }

    private static void Output(int line, params string[] messages)
    {
        lock (Console.Out)
        {
            Console.SetCursorPosition(0, line);
            foreach (string message in messages)
            {
                Console.Write($"{message}    ");
            }
            StringBuilder trailingSpaces = new();
            int trailingSpacesCount = Console.WindowWidth - Console.GetCursorPosition().Left - Environment.NewLine.Length;
            for (int i = 0; i < trailingSpacesCount; i++)
            {
                _ = trailingSpaces.Append(' ');
            }
            Console.WriteLine(trailingSpaces);
        }
    }
}