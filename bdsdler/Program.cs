using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace bdsdler;

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
    private static void Main()
    {
        List<Task> tasks = new();
        foreach (KeyValuePair<string, List<string>> platform in s_platforms)
        {
            string fileName = $"bds_ver_{platform.Key}.json";
            if (!File.Exists(fileName))
            {
                File.WriteAllText(fileName, JsonSerializer.Serialize(platform.Value));
                continue;
            }
            s_platforms[platform.Key] = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(fileName));
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
        foreach (KeyValuePair<string, List<string>> platform in s_platforms)
        {
            _ = Directory.CreateDirectory(platform.Key);
        }
        for (int i = 0; i < Environment.ProcessorCount; i++)
        {
            int index = i;
            Task task = new(() =>
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
                    Download(index, versionstr);
                    Output(index, index.ToString(), "空闲");
                }
            });
            task.Start();
            tasks.Add(task);
        }
        foreach (Task task1 in tasks)
        {
            task1.Wait();
        }
        Console.SetCursorPosition(0, Environment.ProcessorCount);
        Console.WriteLine("下载完毕");
    }

    private static void Download(int index, string versionstr)
    {
        foreach (KeyValuePair<string, List<string>> platform in s_platforms)
        {
            try
            {
                Output(index, index.ToString(), platform.Key, versionstr);
                HttpClient httpClient = new();
                File.WriteAllBytes(Path.Combine(platform.Key, $"bedrock-server-{versionstr}.zip"), httpClient.GetByteArrayAsync($"https://minecraft.azureedge.net/bin-{platform.Key}/bedrock-server-{versionstr}.zip").Result);
                platform.Value.Add(versionstr);
                File.WriteAllText($"bds_ver_{platform.Key}.json", JsonSerializer.Serialize(platform.Value));
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("404"))
                {
                    continue;
                }
                Download(index, versionstr);
            }
        }
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
            int trailingSpacesCount = Console.WindowWidth - Console.GetCursorPosition().Left;
            for (int i = 0; i < trailingSpacesCount; i++)
            {
                _ = trailingSpaces.Append(' ');
            }
            Console.WriteLine(trailingSpaces);
            Console.SetCursorPosition(0, 0);
        }
    }
}