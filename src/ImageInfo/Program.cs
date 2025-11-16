using System;
using ImageInfo.Services;
using ImageInfo.Models;

namespace ImageInfo;

class Program
{
    static int Main(string[] args)
    {
        string folder;
        if (args.Length > 0)
        {
            folder = args[0];
        }
        else
        {
            Console.WriteLine("请输入要扫描和转换的根文件夹路径（按回车使用默认: C:\\Users\\10374\\Desktop\\test）：");
            var input = Console.ReadLine();
            folder = string.IsNullOrWhiteSpace(input) ? @"C:\\Users\\10374\\Desktop\\test" : input.Trim();
        }

        Console.WriteLine($"Scanning and converting images in: {folder}\n");

        // 检查开发模式环境变量
        bool isDevelopmentMode = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IMAGEINFO_DEV"));

        if (isDevelopmentMode)
        {
            // 开发模式：自动运行所有三种转换
            Console.WriteLine("[开发模式] 将自动批量运行所有三种转换模式...");
            var mode = OutputDirectoryMode.SiblingDirectoryWithStructure;

            Console.WriteLine("\n=== 模式 1: PNG -> JPG ===");
            ConversionService.ScanConvertAndReport(folder, 1, mode, openReport: false);

            Console.WriteLine("\n=== 模式 2: PNG -> WEBP ===");
            ConversionService.ScanConvertAndReport(folder, 2, mode, openReport: false);

            Console.WriteLine("\n=== 模式 3: JPG -> WEBP ===");
            ConversionService.ScanConvertAndReport(folder, 3, mode, openReport: false);

            Console.WriteLine("\n[开发模式完成] 所有三种转换已执行。");
        }
        else
        {
            // 生产模式：交互式菜单
            // 选择转换格式映射：1 = PNG -> JPG, 2 = PNG -> WEBP, 3 = JPG -> WEBP
            int choice = 0;
            while (choice < 1 || choice > 3)
            {
                Console.WriteLine("请选择转换模式：");
                Console.WriteLine("  1) PNG -> JPG");
                Console.WriteLine("  2) PNG -> WEBP");
                Console.WriteLine("  3) JPG  -> WEBP");
                var c = Console.ReadLine();
                if (!int.TryParse(c, out choice)) choice = 0;
            }

            // 选择输出目录模式
            int modeInput = 0;
            while (modeInput != 1 && modeInput != 2)
            {
                Console.WriteLine("请选择输出目录模式 (1: 兄弟目录并复刻结构, 2: 本地子目录): ");
                var m = Console.ReadLine();
                if (!int.TryParse(m, out modeInput)) modeInput = 0;
            }

            var mode = modeInput == 1 ? OutputDirectoryMode.SiblingDirectoryWithStructure : OutputDirectoryMode.LocalSubdirectory;

            Console.WriteLine($"开始转换（目标格式: {(choice==1?"JPG":"WEBP")}, 输出模式: {mode}）\n");

            ConversionService.ScanConvertAndReport(folder, choice, mode, openReport: true);
        }

        Console.WriteLine("按回车键退出...");
        Console.ReadLine();
        return 0;
    }
}
