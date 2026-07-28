using System;
using System.IO;

namespace CopyModels.Settings
{
    /// <summary>
    /// Общие пути приложения (конфиги, логи) — единственный источник правды,
    /// чтобы не дублировать Path.Combine(...) в CopyModelsCommand и CopyModelsApplication.
    /// </summary>
    public static class AppPaths
    {
        public static string ConfigDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "000_CopyModels");

        public static string LogDir => Path.Combine(ConfigDir, "log");
    }
}
