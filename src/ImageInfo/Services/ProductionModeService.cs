using System;
using ImageInfo.Models;

namespace ImageInfo.Services
{
    /// <summary>
    /// 生产模式服务：处理交互式菜单和正常的转换流程
    /// </summary>
    public static class ProductionModeService
    {
        /// <summary>
        /// 运行生产模式的交互式流程
        /// </summary>
        public static int RunInteractiveMode(string folder)
        {
            int choice = GetConversionModeChoice();
            OutputDirectoryMode mode = GetOutputDirectoryMode();
            
            Console.WriteLine($"开始转换（目标格式: {GetFormatName(choice)}, 输出模式: {mode}）\n");

            var diagnosis = ConversionService.ScanConvertAndReport(folder, choice, mode, openReport: true);
            
            if (diagnosis != null)
            {
                LogAnalyzer.PrintDiagnosisToConsole(diagnosis);
            }
            
            return 0;
        }

        #region 私有辅助方法

        private static int GetConversionModeChoice()
        {
            int choice = 0;
            while (choice < 1 || choice > 3)
            {
                Console.WriteLine("请选择转换模式：");
                Console.WriteLine("  1) PNG -> JPG");
                Console.WriteLine("  2) PNG -> WEBP");
                Console.WriteLine("  3) JPG  -> WEBP");
                
                var input = Console.ReadLine();
                if (!int.TryParse(input, out choice)) 
                    choice = 0;
            }
            return choice;
        }

        private static OutputDirectoryMode GetOutputDirectoryMode()
        {
            int modeInput = 0;
            while (modeInput != 1 && modeInput != 2)
            {
                Console.WriteLine("请选择输出目录模式 (1: 兄弟目录并复刻结构, 2: 本地子目录): ");
                var input = Console.ReadLine();
                if (!int.TryParse(input, out modeInput)) 
                    modeInput = 0;
            }
            return modeInput == 1 ? 
                OutputDirectoryMode.SiblingDirectoryWithStructure : 
                OutputDirectoryMode.LocalSubdirectory;
        }

        private static string GetFormatName(int choice)
        {
            return choice switch
            {
                1 => "JPG",
                2 => "WEBP",
                3 => "JPG -> WEBP",
                _ => "未知"
            };
        }

        #endregion
    }
}
