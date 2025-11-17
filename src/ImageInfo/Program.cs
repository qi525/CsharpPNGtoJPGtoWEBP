using System;
using System.IO;
using ImageInfo.Services;

namespace ImageInfo;

class Program
{
    static int Main(string[] args)
    {
        // 读取模式选择：开发模式或生产模式
        string folder = GetInputFolder(args);
        Console.WriteLine($"Scanning and converting images in: {folder}\n");

        // 开发模式处理
        if (IsDevelopmentMode(out string? devMode))
        {
            return HandleDevelopmentMode(folder, devMode);
        }

        // 生产模式处理
        return ProductionModeService.RunInteractiveMode(folder);
    }

    private static string GetInputFolder(string[] args)
    {
        if (args.Length > 0)
            return args[0];

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IMAGEINFO_DEV")))
            return @"C:\Users\10374\Desktop\test";

        Console.WriteLine("请输入要扫描和转换的根文件夹路径（按回车使用默认: C:\\Users\\10374\\Desktop\\test）：");
        var input = Console.ReadLine();
        return string.IsNullOrWhiteSpace(input) ? @"C:\Users\10374\Desktop\test" : input.Trim();
    }

    private static bool IsDevelopmentMode(out string? devMode)
    {
        devMode = Environment.GetEnvironmentVariable("IMAGEINFO_DEV");
        return !string.IsNullOrEmpty(devMode);
    }

    private static int HandleDevelopmentMode(string folder, string? devMode)
    {
        if (devMode?.ToLowerInvariant() == "scan")
        {
            Console.WriteLine("[开发模式-扫描] 读取元数据并生成 Excel 报告...\n");
            DevelopmentModeService.RunScanMode(folder);
            return 0;
        }

        if (devMode?.ToLowerInvariant() == "verify")
        {
            Console.WriteLine("[检查方案一] 运行三种转换模式，每种模式自动打开一份报告...\n");
            DevelopmentModeService.RunFullConversionMode(folder);
            return 0;
        }

        DevelopmentModeService.RunFullConversionMode(folder);
        return 0;
    }
}