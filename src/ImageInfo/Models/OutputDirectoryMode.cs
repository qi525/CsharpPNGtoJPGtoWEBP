namespace ImageInfo.Models
{
    /// <summary>
    /// 输出目录模式枚举。
    /// 控制转换后的图片输出到哪里。
    /// </summary>
    public enum OutputDirectoryMode
    {
        /// <summary>
        /// 模式 1: 兄弟目录 + 复刻源目录结构
        /// 
        /// 示例：
        ///   源: D:/Pictures/folder/subfolder/photo.png
        ///   输出: D:/PNG转JPG/folder/subfolder/photo.jpg
        /// 
        /// 优点：与源文件夹分离，结构清晰
        /// 缺点：输出文件夹可能散开
        /// </summary>
        SiblingDirectoryWithStructure = 1,

        /// <summary>
        /// 模式 2: 本地子目录
        /// 
        /// 示例：
        ///   源: D:/Pictures/folder/subfolder/photo.png
        ///   输出: D:/Pictures/folder/subfolder/PNG转JPG/photo.jpg
        /// 
        /// 优点：输出文件夹紧贴原始位置
        /// 缺点：会在原目录下创建多个输出子目录
        /// </summary>
        LocalSubdirectory = 2
    }
}
