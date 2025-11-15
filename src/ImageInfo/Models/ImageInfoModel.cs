using System;
using System.Collections.Generic;

namespace ImageInfo.Models
{
    /// <summary>
    /// 容器对象：表示单张图片的信息（路径、创建/修改时间、提取到的标签）。
    /// 由 MetadataService 填充并在程序/测试中传递使用。
    /// </summary>
    public class ImageInfoModel
    {
        /// <summary>图片文件完整路径。</summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>UTC 创建时间（从文件系统读取）。</summary>
        public DateTime CreatedUtc { get; set; }

        /// <summary>UTC 修改时间（从文件系统读取）。</summary>
        public DateTime ModifiedUtc { get; set; }

        /// <summary>从元数据或文件名中提取的标签列表（去重后）。</summary>
        public List<string> Tags { get; set; } = new List<string>();
    }
}
