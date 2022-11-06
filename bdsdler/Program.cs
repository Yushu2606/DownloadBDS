using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

List<int> version = new()
{
    1,
    6,
    0,
    0
};
List<string> platforms = new()
{
    "win",
    "linux"
};
List<Task> tasks = new();

Console.Write("开始自：");
string? input = Console.ReadLine();
Console.Clear();
if (!string.IsNullOrWhiteSpace(input))
{
    string[] temp = input.Split('.');
    for (int i = 0; i < temp.Length; i++)
    {
        version[i] = Convert.ToInt32(temp[i]);
    }
}
foreach (string platform in platforms)
{
    _ = Directory.CreateDirectory(platform);
}
for (int i = 0; i < 16; i++)
{
    int index = i;
    Task task = new(() =>
    {
        Output(index, index.ToString(), "空闲");
        while (version[1] <= 20)
        {
            Download(platforms, version, index);
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

static void Download(List<string> platforms, List<int> version, int index)
{
    string versionstr = $"{version[0]}.{version[1]}.{version[2]}.{(((version[1] >= 16 && version[2] >= 1) || version[1] >= 17) && version[3] is < 10 and > 0 ? $"0{version[3]}" : version[3])}";
    version[3]++;
    if (version[3] > 35)
    {
        version[3] = 0;
        version[2]++;
    }
    if (version[2] > (version[1] <= 13 ? 5 : 222))
    {
        version[2] = 0;
        version[1]++;
    }
    if (version[1] > 20)
    {
        version[1]++;
        return;
    }
    foreach (string platform in platforms)
    {
        try
        {
            Output(index, index.ToString(), platform, versionstr);
            File.WriteAllBytes(Path.Combine(platform, $"bedrock-server-{versionstr}.zip"), new HttpClient().GetByteArrayAsync($"https://minecraft.azureedge.net/bin-{platform}/bedrock-server-{versionstr}.zip").Result);
            File.AppendAllText($"bds_ver_{platform}.json", $"{versionstr}\n");
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("404"))
            {
                continue;
            }
            Download(platforms, version, index);
        }
    }
}

static void Output(int line, params string[] messages)
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