using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ATL;
using ATL.AudioData;
using static MoraAudioModify.Logger;

namespace MoraAudioModify;

partial class Program
{
    private static readonly string[] DelTagList =
    {
        "45b1d925-1448-5784-b4da-b89901050a13", "be242671-3d48-5ac8-b762-7d2db4f584b8", "ff8ca75f-2d68-52eb-85d6-1580486025a4",
        "93a74bea-ce97-5571-a56a-c5084dba9873", "8e90f26b-372a-5c8c-bb05-1ec0f36ee60c", "07f42305-3c75-529c-ba48-09435e88980d",
        "50dbf5a2-f864-5c17-be00-c36dfd3df7b4", "MOOD", "GENRENUMBER", "PERFORMER"
    };

    [JsonSerializable(typeof(MyOption))]
    private partial class MyOptionJsonContext : JsonSerializerContext
    {
    }

    public static async Task<int> Main(params string[] args)
    {
        var rootCommand = CommandLineInvoker.GetRootCommand(DoWorkAsync);
        rootCommand.Description = $"MoraAudioModify是一个免费且便捷对Mora上下载的音频进行单个或批量的重命名并删掉多余Tag的软件.";
        rootCommand.TreatUnmatchedTokensAsErrors = true;
        var newArgsList = new List<string>();
        var commandLineResult = rootCommand.Parse(args);
        if (!string.Equals(commandLineResult.CommandResult.Command.Name, Path.GetFileNameWithoutExtension(Environment.ProcessPath)!,
                StringComparison.CurrentCultureIgnoreCase))
        {
            newArgsList.Add(commandLineResult.CommandResult.Command.Name);
            return await rootCommand.InvokeAsync(newArgsList.ToArray());
        }

        foreach (var item in commandLineResult.CommandResult.Children)
        {
            switch (item)
            {
                case ArgumentResult a:
                    newArgsList.Add(a.Tokens[0].Value);
                    break;
                case OptionResult o:
                    newArgsList.Add("--" + o.Option.Name);
                    newArgsList.AddRange(o.Tokens.Select(t => t.Value));
                    break;
            }
        }

        return await rootCommand.InvokeAsync(newArgsList.ToArray());
    }

    private static async Task DoWorkAsync(MyOption myOption)
    {
        try
        {
            Config.DEBUG_LOG = myOption.DebugLog;
            var inputUrl = myOption.Url;
            var skipChangeName = !myOption.SkipChangeFilename;
            var skipFileNameFiltering = myOption.SkipFileNameFiltering;
            LogDebug("程序路径: {0}", Path.GetDirectoryName(Environment.ProcessPath)!);
            LogDebug("运行参数：{0}", JsonSerializer.Serialize(myOption, MyOptionJsonContext.Default.MyOption));
            if (File.Exists(inputUrl))
            {
                LogDebug("输入路径是文件");
                DelTag(inputUrl, skipChangeName,false);
                LogColor("文件处理完毕,按下任意按键退出程序");
            }
            else if (Directory.Exists(inputUrl))
            {
                LogDebug("输入路径是文件夹");
                var files = new List<string>();
                var finalFiles = new List<string>();
                files.AddRange(Directory.GetFiles(inputUrl));
                LogDebug($"文件夹有 {files.Count} 个文件：");
                if (skipFileNameFiltering)
                {
                    finalFiles.AddRange(files);
                }
                else
                {
                    foreach (var f in files)
                    {
                        LogDebug($"{Path.GetFileName(f)}");
                        if (IsNumber(Path.GetFileNameWithoutExtension(f))) finalFiles.Add(f);;
                    }
                }
                files.Clear();
                LogDebug($"待处理 {finalFiles.Count} 个文件：");
                Task.WaitAll(finalFiles.Select(file => Task.Run(() => { DelTag(file, skipChangeName,true); })).ToArray());
                LogColor("文件夹处理完毕，按下任意按键退出程序");
            }
            else
            {
                LogError($"非法路径: {inputUrl}");
                throw new Exception($"非法路径: {inputUrl}");
            }

            //等待三秒或按下按键立即退出
            var semaphore = new SemaphoreSlim(0);
            await Task.Run(() =>
            {
                Console.ReadKey();
                semaphore.Release();
            });
            await semaphore.WaitAsync(TimeSpan.FromSeconds(1));
        }
        catch (Exception e)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(Config.DEBUG_LOG ? e.ToString() : e.Message);
            Console.ResetColor();
            Console.WriteLine();
            Thread.Sleep(1);
            Environment.Exit(1);
        }
    }

    private static void DelTag(string url, bool skipChangeName, bool isBatch)
    {
        var theTrack = new Track(url);
        var fileName = Path.GetFileNameWithoutExtension(url);
        if (Config.DEBUG_LOG && !isBatch)
        {
            LogDebug($"{fileName}-Title : {theTrack.Title}");
            LogDebug($"{fileName}-Artist : {theTrack.Artist}");
            LogDebug($"{fileName}-Album : {theTrack.Album}");
            LogDebug($"{fileName}-Recording year : {theTrack.Year}");
            LogDebug($"{fileName}-Track number : {theTrack.TrackNumber}");
            LogDebug($"{fileName}-Disc number : {theTrack.DiscNumber}");
            LogDebug($"{fileName}-Comment : {theTrack.Comment}");
            LogDebug($"{fileName}-Duration (s) : {theTrack.Duration}");
            LogDebug($"{fileName}-Bitrate (KBps) : {theTrack.Bitrate}");
            LogDebug($"{fileName}-Number of channels : {theTrack.ChannelsArrangement.NbChannels}");
            LogDebug($"{fileName}-Channels arrangement : {theTrack.ChannelsArrangement.Description}");
            LogDebug($"{fileName}-Has variable bitrate audio : {(theTrack.IsVBR ? "yes" : "no")}");
            LogDebug($"{fileName}-Has lossless audio : {(AudioDataIOFactory.CF_LOSSLESS == theTrack.CodecFamily ? "yes" : "no")}");
        
            foreach (KeyValuePair<string, string> field in theTrack.AdditionalFields)
            {
                LogDebug($"{fileName}-Custom field {field.Key} : value = {field.Value}");
            }
        }
        
        LogColor($"{fileName}-正在删除Tag");
        
        theTrack.Comment = "";

        foreach (var s in DelTagList)
        {
            theTrack.AdditionalFields.Remove(s);
        }

        if (Config.DEBUG_LOG && !isBatch)
        {
            LogDebug($"{fileName}-删除后所保留的自定义的Tag: ");
            foreach (KeyValuePair<string, string> field in theTrack.AdditionalFields)
            {
                LogDebug($"{fileName}-Custom field {field.Key} : value = {field.Value}");
            }
        }
        
        var tilte = theTrack.Title;
        var artist = theTrack.Artist;
        
        LogColor($"{fileName}-正在保存修改");
        
        if (isBatch)
        {
            theTrack.Save();
        }
        else
        {
            theTrack.Save(DisplayProgress);
        }

        if (skipChangeName)
        {
            FileRename(url, tilte, artist);
        }

        Console.WriteLine();
    }
    
    private static void FileRename(string url, string title, string artist)
    {
        try
        {
            LogColor($"将文件 {Path.GetFileName(url)} 重命名为 {title}-{artist}{Path.GetExtension(url)}");
            File.Move(url, $"{Path.GetDirectoryName(url)!}\\{title}.{Path.GetExtension(url)}", true);
        }
        catch (Exception e)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(Config.DEBUG_LOG ? e.ToString() : e.Message);
            Console.ResetColor();
            Console.WriteLine();
            Thread.Sleep(1);
            Environment.Exit(1);
        }
    }
    
    private static void DisplayProgress(float progress)
    {
        LogColor($"保存进度: {Convert.ToInt16(progress * 100)}%");
    }
    
    /// <summary>
    /// 判断字符串是否是数字
    /// </summary>
    private static bool IsNumber(string s)
    {
        var rx = MyRegex();
        return rx.IsMatch(s);
    }

    [GeneratedRegex("^[0-9]*$")]
    private static partial Regex MyRegex();
}