using System;
using System.IO;
using ImageInfo.Services;

namespace ImageInfo;

class Program
{
    static int Main(string[] args)
    {
        // 检查快速启动命令行参数（--1, --2, --3, --4）
        if (args.Length > 0 && args[0].StartsWith("--"))
        {
            string quickLaunch = args[0].Substring(2).ToLowerInvariant();
            string folder = @"C:\stable-diffusion-webui\outputs\txt2img-images";
            
            return quickLaunch switch
            {
                "1" => LaunchFunction(folder, "scan", "功能1：不清洗正向关键词"),
                "2" => LaunchFunction(folder, "scan2", "功能2：清洗正向关键词"),
                "3" => LaunchFunction(folder, "verify1", "功能3：同时运行三种转换模式"),
                "4" => LaunchFunction(folder, "mode4", "功能4：选择性转换"),
                _ => RunNormalMode(args)
            };
        }

        // 读取模式选择：开发模式或生产模式
        return RunNormalMode(args);
    }

    private static int LaunchFunction(string folder, string devMode, string description)
    {
        Console.WriteLine($"[快速启动] {description}\n");
        return HandleDevelopmentMode(folder, devMode);
    }

    private static int RunNormalMode(string[] args)
    {
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
            return @"C:\stable-diffusion-webui\outputs\txt2img-images";

        Console.WriteLine("请输入要扫描和转换的根文件夹路径（按回车使用默认: C:\\stable-diffusion-webui\\outputs\\txt2img-images）：");
        var input = Console.ReadLine();
        return string.IsNullOrWhiteSpace(input) ? @"C:\stable-diffusion-webui\outputs\txt2img-images" : input.Trim();
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
            Console.WriteLine("开发功能1： [开发模式-只读模式1] 不清洗正向关键词\n");
            DevelopmentModeService.RunScanMode(folder);
            return 0;
        }

        if (devMode?.ToLowerInvariant() == "scan2")
        {
            Console.WriteLine("开发功能2： [开发模式-只读模式2] 清洗正向关键词\n");
            DevelopmentModeService.RunScanMode2(folder);
            return 0;
        }

        if (devMode?.ToLowerInvariant() == "verify1")
        {
            Console.WriteLine("开发功能3： [开发模式-检查方案一] 同时运行三种转换模式，png-jpg / png-webp / jpg-webp，每种模式自动打开一份报告...\n");
            DevelopmentModeService.RunFullConversionMode(folder);
            return 0;
        }

        if (devMode?.ToLowerInvariant() == "mode4")
        {
            Console.WriteLine("开发功能4： [开发模式-选择性转换] 选择转换类型，png-jpg / png-webp / jpg-webp\n");
            ProductionModeService.RunInteractiveMode(folder);
            return 0;
        }

        DevelopmentModeService.RunFullConversionMode(folder);
        return 0;

    }
}