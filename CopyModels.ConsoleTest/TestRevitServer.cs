using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CopyModels.Plugin.Services;

namespace CopyModels.ConsoleTest
{
    internal class TestRevitServer
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== RevitServerService Unit Tests ===\n");

            try
            {
                // ТЕСТ 1: Парсинг RSN пути (без сетевого запроса)
            }
            catch (Exception ex)
            {

            }

            Console.WriteLine("\n Нажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        /// <summary>
        /// ТЕСТ 1: Проверяем парсинг RSN пути
        /// </summary>
        static void TestExtractServerMethod()
        {
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine("ТЕСТ 1: Парсинг RSN пути");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            var service = new RevitServerService("2022");

            // Тестовые пути из RealProject.json
            var testPaths = new[]
            {
                "RSN://k-2133.atptlp.local/20175_INARCTICA/Architecture/Design.rvt",
                "RSN://revit2019-mo/ProjectA/Design.rvt",
                "RSN://server-name/folder|subfolder|model.rvt"
            };

            foreach (var path in testPaths)
            {
                try
                {
                    // Используем рефлексию чтобы вызвать private
                    var method = service.GetType()
                        .GetMethod("ExtractServer",
                            System.Reflection.BindingFlags.NonPublic |
                            System.Reflection.BindingFlags.Static);

                    var result = method?.Invoke(null, new object[] { path });
                    Console.WriteLine($"✓ {path} -> {result}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ {path} → ОШИБКА: {ex.InnerException?.Message}");
                }
            }
            Console.WriteLine();
        }

        /// <summary>
        /// ТЕСТ 2: Проверяем парсинг URL
        /// </summary>
        static void TestBuildUrlMethod()
        {
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine("ТЕСТ 2: Построение URL для REST API");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            var service = new RevitServerService("2022");

            var testServers = new[] { "k-2133.atptlp.local", "revit2019-mo", "server-name" };

            foreach (var server in testServers)
            {
                var expectedUrl = $"http://{server}/RevitServerAdminRESTService2022/AdminRESTService.svc/";
                Console.WriteLine($"✓ {server} → {expectedUrl}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// ТЕСТ 3: Логирование callback-ов работает
        /// </summary>
        static void TestLoggingCallbaks()
        {
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine("ТЕСТ 3: Логирование callback-ов");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            var logs = new List<string>();

            var service = new RevitServerService(
                "2022",
                msg => logs.Add($"[INFO] {msg}"),
                msg => logs.Add($"[WARN] {msg}"),
                msg => logs.Add($"[ERROR] {msg}")
            );

            Console.WriteLine("✓ Service создан с callback-ами");
            Console.WriteLine($"✓ Logs список инициализирован (count: {logs.Count})");
            Console.WriteLine();
        }

        /// <summary>
        /// ТЕСТ 4: Реальный запрос к Revit Server
        /// </summary>
        static void TestReadModelsFromRealServer()
        {
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine("ТЕСТ 4: Чтение моделей с реального сервера");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            // Заменить на ревльный путь
            string testPath = "RSN://k-2133.atptlp.local/20175_INARCTICA";

            var logs = new List<string>();
            var service = new RevitServerService(
                "2023",
                msg => { logs.Add($"[INFO] {{msg}}\"); Console.WriteLine($\"ℹ  {{msg}}"); },
                msg => { logs.Add($"[WARN] {msg}"); Console.WriteLine($"⚠  {msg}"); },
                msg => { logs.Add($"[ERROR] {msg}"); Console.WriteLine($"❌ {msg}"); }
                );

            Console.WriteLine($"Читаем модели из: {testPath}");

            try
            {
                var models = service.ReadRevitServerModels(testPath);

                Console.WriteLine($"✓ Найдено {models.Count} моделей:");
                foreach (var model in models) Console.WriteLine($" - {model}");
            }
            catch ( Exception ex )
            {
                Console.WriteLine($"❌ Ошибка: {ex.Message}");
            }

            Console.WriteLine($"\nВсего логов: {logs.Count}");
            Console.WriteLine();

        }
    }
}

