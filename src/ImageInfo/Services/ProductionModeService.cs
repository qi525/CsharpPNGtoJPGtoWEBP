using System;
using ImageInfo.Models;

namespace ImageInfo.Services
{
    /// <summary>生产模式：交互式菜单和转换流程。</summary>
    public static class ProductionModeService
    {
        public static int RunInteractiveMode(string folder)
        {
            var choice = GetConversionModeChoice();
            var mode = GetOutputDirectoryMode();
            Console.WriteLine($"开始转换（{GetFormatName(choice)}, {mode}）\n");
            var diagnosis = ConversionService.ScanConvertAndReport(folder, choice, mode, openReport: true);
            if (diagnosis != null)
                LogAnalyzer.PrintDiagnosisToConsole(diagnosis);
            return 0;
        }

        private static int GetConversionModeChoice()
        {
            int choice = 0;
            while (choice < 1 || choice > 3)
            {
                Console.WriteLine("请选择转换模式：");
                Console.WriteLine("  1) PNG -> JPG");
                Console.WriteLine("  2) PNG -> WEBP");
                Console.WriteLine("  3) JPG  -> WEBP");
                if (!int.TryParse(Console.ReadLine(), out choice))
                    choice = 0;
            }
            return choice;
        }

        private static OutputDirectoryMode GetOutputDirectoryMode()
        {
            int modeInput = 0;
            while (modeInput != 1 && modeInput != 2)
            {
                Console.WriteLine("请选择输出目录模式 (1: 兄弟目录, 2: 本地子目录): ");
                if (!int.TryParse(Console.ReadLine(), out modeInput))
                    modeInput = 0;
            }
            return modeInput == 1 ? OutputDirectoryMode.SiblingDirectoryWithStructure : OutputDirectoryMode.LocalSubdirectory;
        }

        private static string GetFormatName(int choice) => choice switch
        {
            1 => "JPG",
            2 => "WEBP",
            3 => "JPG -> WEBP",
            _ => "未知"
        };
    }
}
