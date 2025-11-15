using System;
using ImageInfo.Services;

namespace ImageInfo;

class Program
{
    static int Main(string[] args)
    {
        var folder = args.Length > 0 ? args[0] : @"C:\\Users\\10374\\Desktop\\test";
        Console.WriteLine($"Scanning and converting images in: {folder}\n");

        ConversionService.ScanConvertAndReport(folder, openReport: true);

        return 0;
    }
}
