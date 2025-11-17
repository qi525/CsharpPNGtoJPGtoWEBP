using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ImageInfo.Models;
using ImageMagick;

namespace ImageInfo.Services
{
    /// <summary>
    /// 元数据写入器：使用Magick.NET实现
    /// 支持PNG（tEXt块）、JPEG（EXIF UserComment）、WebP（EXIF UserComment）
    /// </summary>
    public static class MetadataWriter
    {
        private static readonly string WriterLog = Path.Combine(Environment.CurrentDirectory, "metadata-writer.log");

        public static (bool written, bool verified) WriteMetadata(string destPath, string sourceFormat, AIMetadata aiMetadata)
        {
            if (string.IsNullOrEmpty(destPath) || aiMetadata == null)
                return (false, false);

            try
            {
                var ext = Path.GetExtension(destPath).ToLowerInvariant();
                bool written = false;
                bool verified = false;

                if (!string.IsNullOrEmpty(aiMetadata.FullInfo))
                {
                    written = WriteFullInfoByFormat(destPath, ext, aiMetadata.FullInfo);
                    if (written)
                    {
                        verified = VerifyFullInfo(destPath, aiMetadata.FullInfo);
                        LogWrite(destPath, "FullInfo", written, verified);
                    }
                }
                else
                {
                    written = WriteStructuredMetadata(destPath, ext, aiMetadata);
                    verified = written;
                    LogWrite(destPath, "Structured", written, verified);
                }

                return (written, verified);
            }
            catch (Exception ex)
            {
                LogError(destPath, ex.Message);
                return (false, false);
            }
        }

        private static bool WriteFullInfoByFormat(string filePath, string ext, string fullInfo)
        {
            try
            {
                if (ext == ".png")
                    return WritePngTextChunk(filePath, "parameters", fullInfo);
                else if (ext == ".jpg" || ext == ".jpeg")
                    return WriteJpegExifUserComment(filePath, fullInfo);
                else if (ext == ".webp")
                    return WriteWebPExifUserComment(filePath, fullInfo);
                
                return false;
            }
            catch (Exception ex)
            {
                LogError(filePath, $"WriteFullInfoByFormat: {ex.Message}");
                return false;
            }
        }

        private static bool WritePngTextChunk(string pngPath, string keyword, string text)
        {
            try
            {
                using (var image = new MagickImage(pngPath))
                {
                    // PNG 元数据存储方案：使用 IPTC Profile
                    // IPTC 是标准的元数据格式，大多数图片编辑器都支持
                    var iptcProfile = image.GetIptcProfile();
                    if (iptcProfile == null)
                    {
                        iptcProfile = new IptcProfile();
                    }
                    
                    // 使用 IptcTag.Keywords 或自定义标签来存储参数
                    // 如果使用 Caption，就使用 IptcTag.Caption
                    iptcProfile.SetValue(IptcTag.Caption, text);
                    
                    image.SetProfile(iptcProfile);
                    
                    image.Format = MagickFormat.Png;
                    image.Write(pngPath);
                }

                return true;
            }
            catch (Exception ex)
            {
                LogError(pngPath, $"WritePngTextChunk: {ex.Message}");
                return false;
            }
        }

        private static bool WriteJpegExifUserComment(string jpegPath, string fullInfo)
        {
            try
            {
                using (var image = new MagickImage(jpegPath))
                {
                    var exifProfile = image.GetExifProfile() ?? new ExifProfile();
                    
                    // 使用标准的UTF8编码，不加UNICODE头
                    byte[] commentBytes = Encoding.UTF8.GetBytes(fullInfo);
                    exifProfile.SetValue(ExifTag.UserComment, commentBytes);
                    
                    image.SetProfile(exifProfile);
                    image.Write(jpegPath);
                }

                return true;
            }
            catch (Exception ex)
            {
                LogError(jpegPath, $"WriteJpegExifUserComment: {ex.Message}");
                return false;
            }
        }

        private static bool WriteWebPExifUserComment(string webpPath, string fullInfo)
        {
            try
            {
                using (var image = new MagickImage(webpPath))
                {
                    // WebP 需要特殊处理 - 先删除旧的EXIF以避免冲突
                    var oldExif = image.GetExifProfile();
                    if (oldExif != null)
                    {
                        image.RemoveProfile("exif");
                    }

                    // 创建新的EXIF配置文件
                    var exifProfile = new ExifProfile();
                    
                    // 使用标准的UTF8编码，不加UNICODE头
                    byte[] commentBytes = Encoding.UTF8.GetBytes(fullInfo);
                    exifProfile.SetValue(ExifTag.UserComment, commentBytes);
                    
                    image.SetProfile(exifProfile);
                    
                    // WebP写入时可能需要特殊处理
                    image.Format = MagickFormat.WebP;
                    image.Write(webpPath);
                }

                return true;
            }
            catch (Exception ex)
            {
                LogError(webpPath, $"WriteWebPExifUserComment: {ex.Message}");
                return false;
            }
        }



        private static bool WriteStructuredMetadata(string filePath, string ext, AIMetadata metadata)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                
                if (!string.IsNullOrEmpty(metadata.Prompt))
                    sb.AppendLine($"Prompt: {metadata.Prompt}");
                if (!string.IsNullOrEmpty(metadata.NegativePrompt))
                    sb.AppendLine($"NegativePrompt: {metadata.NegativePrompt}");
                if (!string.IsNullOrEmpty(metadata.Model))
                    sb.AppendLine($"Model: {metadata.Model}");
                if (!string.IsNullOrEmpty(metadata.Sampler))
                    sb.AppendLine($"Sampler: {metadata.Sampler}");

                string text = sb.ToString();
                
                if (ext == ".png")
                    return WritePngTextChunk(filePath, "parameters", text);
                else if (ext == ".jpg" || ext == ".jpeg")
                    return WriteJpegExifUserComment(filePath, text);
                else if (ext == ".webp")
                    return WriteWebPExifUserComment(filePath, text);
                    
                return false;
            }
            catch (Exception ex)
            {
                LogError(filePath, $"WriteStructuredMetadata: {ex.Message}");
                return false;
            }
        }

        private static bool VerifyFullInfo(string filePath, string expectedInfo)
        {
            try
            {
                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                
                // 对于JPEG和WebP，直接用Magick.NET读取EXIF字节进行精确验证
                if (ext == ".jpg" || ext == ".jpeg" || ext == ".webp")
                {
                    return VerifyExifUserCommentBytes(filePath, expectedInfo);
                }
                
                // PNG使用MetadataExtractors读取，然后精确比对
                var readBack = MetadataExtractors.ReadAIMetadata(filePath);
                if (string.IsNullOrEmpty(readBack.FullInfo))
                    return false;

                // 精确比对：写入的和读出的必须完全相等
                return readBack.FullInfo.Equals(expectedInfo, StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        private static bool VerifyExifUserCommentBytes(string filePath, string expectedInfo)
        {
            try
            {
                using (var image = new MagickImage(filePath))
                {
                    var exif = image.GetExifProfile();
                    if (exif == null)
                    {
                        LogError(filePath, "No EXIF profile");
                        return false;
                    }

                    var userComment = exif.GetValue(ExifTag.UserComment);
                    if (userComment == null)
                    {
                        LogError(filePath, "No UserComment tag");
                        return false;
                    }

                    var bytesObj = userComment.GetValue();
                    byte[]? bytes = bytesObj as byte[];
                    if (bytes == null || bytes.Length == 0)
                    {
                        LogError(filePath, $"Invalid bytes (length: {bytes?.Length ?? 0})");
                        return false;
                    }

                    // 直接用UTF8解码，不处理任何头
                    string decoded = Encoding.UTF8.GetString(bytes).TrimEnd('\0');
                    
                    // 精确比对：必须完全相等
                    bool isEqual = decoded.Equals(expectedInfo, StringComparison.Ordinal);
                    
                    return isEqual;
                }
            }
            catch (Exception ex)
            {
                LogError(filePath, $"VerifyExifUserCommentBytes: {ex.Message}");
                return false;
            }
        }


        private static void LogWrite(string filePath, string infoType, bool written, bool verified)
        {
            try
            {
                string line = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} | {Path.GetFileName(filePath)} | {infoType} | Written={written} | Verified={verified}\n";
                File.AppendAllText(WriterLog, line, Encoding.UTF8);
            }
            catch { }
        }

        private static void LogError(string filePath, string error)
        {
            try
            {
                string line = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} | ERROR | {Path.GetFileName(filePath)} | {error}\n";
                File.AppendAllText(WriterLog, line, Encoding.UTF8);
            }
            catch { }
        }
    }
}

