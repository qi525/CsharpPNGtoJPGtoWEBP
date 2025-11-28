using System;
using System.IO;
using ImageInfo.Services;

namespace ImageInfo;

class Program
{
    static int Main(string[] args)
    {
        // 检查快速启动命令行参数（--1, --2, ... --7）
        if (args.Length > 0 && args[0].StartsWith("--"))
        {
            string quickLaunch = args[0].Substring(2).ToLowerInvariant();
            string folder = @"C:\stable-diffusion-webui\outputs\txt2img-images";

            switch (quickLaunch)
            {
                case "1": return LaunchFunction(folder, "scan", "功能1：不清洗正向关键词");
                case "2": return LaunchFunction(folder, "scan2", "功能2：清洗正向关键词");
                case "3": return LaunchFunction(folder, "tfidf", "功能3：自定义关键词标记");
                case "4": return LaunchFunction(folder, "scorer", "功能4：TF-IDF关键词提取");
                case "5": return LaunchFunction(folder, "predict", "功能5：个性化评分预测");
                case "6": return LaunchFunction(folder, "rename", "功能6：图片文件重命名");
                case "21": return LaunchFunction(folder, "verify1", "功能21：同时运行三种转换模式");
                case "22": return LaunchFunction(folder, "mode4", "功能22：选择性转换");
                case "7": return LaunchFunction(folder, "mode7", "功能7：分析词频");
                default:
                    return RunNormalMode(args);
            }
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
        if (devMode?.ToLowerInvariant() == "mode7")
        {
            Console.WriteLine("开发功能7： [开发模式-分析词频] 统计核心正向词词频\n");
            ImageInfo.Services.Mode7WordFrequencyAnalyzer.RunWordFrequencyAnalysis(folder);
            return 0;
        }
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

        if (devMode?.ToLowerInvariant() == "tfidf")
        {
            Console.WriteLine("开发功能3： [开发模式-自定义标记] 自定义关键词标记与文件原名称提取\n");
            DevelopmentModeService.RunScanMode3(folder);
            return 0;
        }

        if (devMode?.ToLowerInvariant() == "scorer")
        {
            Console.WriteLine("开发功能4： [开发模式-高级分析] TF-IDF区分度关键词提取\n");
            DevelopmentModeService.RunScanMode4(folder);
            return 0;
        }

        if (devMode?.ToLowerInvariant() == "predict")
        {
            Console.WriteLine("开发功能5： [开发模式-高级分析] 个性化评分预测\n");
            DevelopmentModeService.RunScanMode5(folder);
            return 0;
        }

        if (devMode?.ToLowerInvariant() == "rename")
        {
            Console.WriteLine("开发功能6： [开发模式-文件重命名] 根据合并后缀重命名图片文件\n");
            DevelopmentModeService.RunScanMode6(folder);
            return 0;
        }

        if (devMode?.ToLowerInvariant() == "verify1")
        {
            Console.WriteLine("开发功能21： [开发模式-检查方案一] 同时运行三种转换模式，png-jpg / png-webp / jpg-webp，每种模式自动打开一份报告...\n");
            DevelopmentModeService.RunFullConversionMode(folder);
            return 0;
        }

        if (devMode?.ToLowerInvariant() == "mode4")
        {
            Console.WriteLine("开发功能22： [开发模式-选择性转换] 选择转换类型，png-jpg / png-webp / jpg-webp\n");
            ProductionModeService.RunInteractiveMode(folder);
            return 0;
        }

        DevelopmentModeService.RunFullConversionMode(folder);
        return 0;
    }
}