﻿using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Parsing;

namespace MoraAudioModify;

internal class CommandLineInvoker
{
    private static readonly Argument<string> Url = new("url", description: "Mora音频文件或文件夹路径");
    private static readonly Option<bool> SkipChangeFilename = new(new [] { "--skip-change-filename", "-scf" }, "跳过修改文件名");
    private static readonly Option<bool> SkipFileNameFiltering = new(new [] { "--skip-Filename-Filtering", "-sff" }, "跳过纯数字文件名过滤");
    private static readonly Option<bool> DebugLog = new(new [] { "--debug-log", "-debug" }, "启用Debug日志输出");
    class MyOptionBinder : BinderBase<MyOption>
        {
            protected override MyOption GetBoundValue(BindingContext bindingContext)
            {
                var option = new MyOption
                {
                    Url = bindingContext.ParseResult.GetValueForArgument(Url)
                };
                
                if (bindingContext.ParseResult.HasOption(SkipChangeFilename)) option.SkipChangeFilename = bindingContext.ParseResult.GetValueForOption(SkipChangeFilename)!;
                if (bindingContext.ParseResult.HasOption(SkipFileNameFiltering)) option.SkipFileNameFiltering = bindingContext.ParseResult.GetValueForOption(SkipFileNameFiltering)!;
                if (bindingContext.ParseResult.HasOption(DebugLog)) option.DebugLog = bindingContext.ParseResult.GetValueForOption(DebugLog)!;
                return option;
            }
        }

        public static RootCommand GetRootCommand(Func<MyOption, Task> action)
        {
            var rootCommand = new RootCommand
            {
                Url,
                SkipChangeFilename,
                SkipFileNameFiltering,
                DebugLog
            };

            rootCommand.SetHandler(async (myOption) => await action(myOption), new MyOptionBinder());

            return rootCommand;
        }
}