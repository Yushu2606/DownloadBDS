using DownloadBDS;
using DownloadBDS.Types;
using System.Collections.Concurrent;
using Version = DownloadBDS.Types.Version;

Console.Write("开始自：");
string? input = Console.ReadLine();
Console.CursorVisible = false;
Console.Clear();
if (!Version.TryParse(input, out Version version))
{
    version = new(1, 6, default, default);
}

Task[] tasks = new Task[Environment.ProcessorCount * 2];
ConcurrentBag<(Platform Platform, bool IsPreviewVersion, Version Verson)> data = [];
Task task = Task.Run(() =>
{
    while (version.Minor < 21)
    {
        version.Revision++;
        if (version.Revision > 35)
        {
            version.Revision = 0;
            version.Build++;
        }

        if (version.Build > (version.Minor < 14 ? 5 : 222))
        {
            version.Build = 0;
            version.Minor++;
        }

        if (version.Minor > 20)
        {
            version.Major++;
            return;
        }

        data.Add((Platform.Linux, false, version));
        data.Add((Platform.Linux, true, version));
        data.Add((Platform.Windows, false, version));
        data.Add((Platform.Windows, true, version));
    }
});
Directory.CreateDirectory("Linux");
Directory.CreateDirectory("Windows");
Directory.CreateDirectory(Path.Combine("Linux", "Preview"));
Directory.CreateDirectory(Path.Combine("Windows", "Preview"));
for (int i = 0; i < tasks.Length; i++)
{
    int index = i;
    tasks[i] = Task.Factory.StartNew(() =>
    {
        while (!task.IsCompleted)
        {
            while (data.TryTake(out (Platform Platform, bool IsPreviewVersion, Version Verson) datum))
            {
                Helper.Output(index, index.ToString(), datum.Platform.ToString(), datum.IsPreviewVersion.ToString(),
                    datum.Verson.ToString());
                switch (Helper.DownloadAsync(datum.Platform, datum.IsPreviewVersion, datum.Verson).Result)
                {
                    case Result.Success:
                        {
                            Helper.SaveData(datum.Platform, datum.IsPreviewVersion, datum.Verson);
                            break;
                        }
                    case Result.Failed:
                        {
                            data.Add(datum);
                            break;
                        }
                    case Result.NotFound:
                        {
                            break;
                        }
                    default:
                        {
                            throw new IndexOutOfRangeException();
                        }
                }
            }

            Helper.Output(index, index.ToString(), "空闲");
        }
    }, TaskCreationOptions.LongRunning);
}

Task.WaitAll(tasks);
Console.SetCursorPosition(0, tasks.Length);
Console.WriteLine("下载完毕");