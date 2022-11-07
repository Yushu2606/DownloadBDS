using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace bdsdler;

internal class Program
{
    private static readonly List<string> s_bdsWindowsVersions = new();
    private static readonly List<string> s_bdsLinuxVersions = new();
    private static readonly List<int> s_version = new()
    {
        1,
        6,
        0,
        0
    };
    private static readonly List<string> s_platforms = new()
    {
        "win",
        "linux"
    };
    private static void Main()
    {
        List<Task> tasks = new();
        foreach (string platform in s_platforms)
        {
            string fileName = $"bds_ver_{platform}.json";
            if (!File.Exists(fileName))
            {
                File.WriteAllText(fileName, JsonSerializer.Serialize(platform switch
                {
                    "win" => s_bdsWindowsVersions,
                    "linux" => s_bdsLinuxVersions,
                    _ => throw new NotImplementedException(platform)
                }));
            }
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
        foreach (string platform in s_platforms)
        {
            _ = Directory.CreateDirectory(platform);
        }
        for (int i = 0; i < Environment.ProcessorCount; i++)
        {
            int index = i;
            Task task = new(() =>
            {
                Output(index, index.ToString(), "空闲");
                while (s_version[1] <= 20)
                {
                    Download(index);
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
        Console.SetCursorPosition(0, 16);
        Console.WriteLine("下载完毕");

    }

    private static void Download(int index)
    {
        string versionstr = $"{s_version[0]}.{s_version[1]}.{s_version[2]}.{(((s_version[1] >= 16 && s_version[2] >= 1) || s_version[1] >= 17) && s_version[3] is < 10 and > 0 ? $"0{s_version[3]}" : s_version[3])}";
        s_version[3]++;
        if (s_version[3] > 35)
        {
            s_version[3] = 0;
            s_version[2]++;
        }
        if (s_version[2] > (s_version[1] <= 13 ? 5 : 222))
        {
            s_version[2] = 0;
            s_version[1]++;
        }
        if (s_version[1] > 20)
        {
            s_version[1]++;
            return;
        }
        foreach (string platform in s_platforms)
        {
            try
            {
                Output(index, index.ToString(), platform, versionstr);
                HttpClient httpClient = new();
                File.WriteAllBytes(Path.Combine(platform, $"bedrock-server-{versionstr}.zip"), httpClient.GetByteArrayAsync($"https://minecraft.azureedge.net/bin-{platform}/bedrock-server-{versionstr}.zip").Result);
                List<string> vers = platform switch
                {
                    "win" => s_bdsWindowsVersions,
                    "linux" => s_bdsLinuxVersions,
                    _ => throw new NotImplementedException(platform)
                };
                vers.Add(versionstr);
                File.WriteAllText($"bds_ver_{platform}.json", JsonSerializer.Serialize(vers));
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("404"))
                {
                    continue;
                }
                Download(index);
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
                Console.Write($"{messages}    ");
            }
            Console.WriteLine();
            Console.SetCursorPosition(0, 0);
        }
    }
}