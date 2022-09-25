using System.Net;

int interval = 200;
Directory.CreateDirectory("dlbds");
Directory.CreateDirectory("dlbds/bds-win");
Directory.CreateDirectory("dlbds/bds-linux");
Console.Write("开始自");
string input = Console.ReadLine() ?? string.Empty;
bool first = true;
List<string> key = new();
switch (input)
{
    case "relog":
        {
            List<string> file = File.ReadAllLines("log.log").ToList();
            foreach (string line in new List<string>(file))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                string pt = line.Split('-')[0];
                string filename = line.Split("下载失败")[0][(pt.Length + 1)..];
                _ = Task.Run(() =>
                {
                    _ = dl(pt, filename);
                });
                _ = file.Remove(line);
                File.WriteAllLines("log.log", file);
                Thread.Sleep(interval);
            }
            Console.Out.WriteLine("任务创建完毕");
            while (true)
            {
                _ = Console.ReadKey(false);
            }
        }

    default:
        if (input != string.Empty)
        {
            key.AddRange(input.Split('.'));
        }
        else
        {
            first = false;
        }

        break;
}
for (int i2 = 6; i2 <= 20; ++i2)
{
    if (first) { i2 = Convert.ToInt32(key[0]); }
    for (int i3 = 0; i3 <= (i2 <= 13 ? 5 : 222); ++i3)
    {
        if (first) { i3 = Convert.ToInt32(key[1]); }
        for (int i4 = 0; i4 <= 35; ++i4)
        {
            if (first) { i4 = Convert.ToInt32(key[2]); first = false; }
            string ver = $"1.{i2}.{i3}.{(((i2 >= 16 && i3 >= 1) || i2 >= 17) && i4 is < 10 and > 0 ? $"0{i4}" : i4)}";
            _ = Task.Run(() =>
            {
                _ = dl("win", ver);
            });
            Thread.Sleep(interval);
            _ = Task.Run(() =>
            {
                _ = dl("linux", ver);
            });
            Thread.Sleep(interval);
        }
    }
}
Console.Out.WriteLine("任务创建完毕");
while (true)
{
    _ = Console.ReadKey(false);
}

static bool dl(string pt, string ver)
{
    try
    {
        Console.Out.WriteLine($"开始下载{pt}-{ver}");
        new WebClient().DownloadFile($"https://minecraft.azureedge.net/bin-{pt}/bedrock-server-{ver}.zip", $"dlbds/bds-{pt}/bedrock-server-{ver}.zip");
        File.AppendAllLines($"dlbds/bds_ver_{pt}.json", new string[] { ver });
        Console.Out.WriteLine($"{pt}-{ver}下载成功");
        return true;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"{pt}-{ver}下载失败：{ex.Message}");
        if (ex.Message.Contains("404"))
        {
            return true;
        }
        File.AppendAllLines("log.log", new string[] { $"{pt}-{ver}下载失败：{ex.Message}" });
    }
    return false;
}