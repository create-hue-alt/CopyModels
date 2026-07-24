namespace CopyModels.Settings
{
    public static class AppDefaults
    {
        public const string NavisworksViewName = "Navisworks";

        public const string EnvAutorun = "COPYMODELS_AUTORUN";
        public const string EnvProject = "COPYMODELS_PROJECT";
        public const string EnvDebug = "COPYMODELS_DEBUG";

        public const string AllProjectsKey = "ALL";

        public const double ActualityToleranceSeconds = 60; // 1 минута допуск, как в Python

        public const string RevitServerPrefix = "RSN";

        /// <summary>Проверяет, является ли путь адресом Revit Server (начинается с RSN).</summary>
        public static bool IsRevitServer(string path) =>
            path != null && path.StartsWith(RevitServerPrefix,System.StringComparison.OrdinalIgnoreCase);
    }
}
