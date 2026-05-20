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
            throw new NotImplementedException();
        }

        /// <summary>
        /// ТЕСТ 3: Логирование callback-ов работает
        /// </summary>
        static void TestLoggingCallbaks()
        {
            throw new NotImplementedException();
        }
    }
}

