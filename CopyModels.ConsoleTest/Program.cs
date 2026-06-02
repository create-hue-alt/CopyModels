using CopyModels.Core.Models;
using CopyModels.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CopyModels.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== CopyModels SettingsReader Тестирование ===\n");

            try
            {
                string testConfigsPath = GetTestConfigsPath();
                if (!Directory.Exists(testConfigsPath))
                {
                    Console.WriteLine($"❌ ОШИБКА: Папка TestConfigs не найдена! ({testConfigsPath})");
                    return;
                }

                var reader = new SettingsReader(testConfigsPath);

                Console.WriteLine("--- СТАТУС ПРОВЕРОК ---");
                bool allPassed = true;

                // Локальная функция для быстрого тестирования
                void Check(bool condition, string successMsg, string failMsg)
                {
                    if (condition) Console.WriteLine($"✓ {successMsg}");
                    else { Console.WriteLine($"❌ {failMsg}"); allPassed = false; }
                }

                // ЕДИНСТВЕННЫЙ МЕТОД ДЛЯ ЧТЕНИЯ ТЕПЕРЬ:
                var allSettings = reader.ReadAll();

                Check(allSettings != null && allSettings.Count > 0,
                    "Метод ReadAll() успешно прочитал настройки", "ReadAll() вернул пустой или null результат");

                if (allSettings != null && allSettings.Count > 0)
                {
                    Check(allSettings.ContainsKey("ALL"),
                        "Сгруппированный список 'ALL' успешно сформирован", "Ключ 'ALL' отсутствует в словаре");

                    if (allSettings.ContainsKey("ALL"))
                    {
                        var allTasks = allSettings["ALL"];

                        Check(allTasks.Count > 0,
                            $"В группе 'ALL' найдено {allTasks.Count} заданий", "Группа 'ALL' пуста");

                        Check(allSettings.Keys.Count > 1,
                            $"Найдено {allSettings.Keys.Count - 1} отдельных проектов", "Отдельные проекты (кроме ALL) не найдены");

                        Check(allTasks.All(t => !string.IsNullOrEmpty(t.DisplayName)),
                            "Все задания имеют DisplayName", "Есть задания без DisplayName");

                        Check(allTasks.All(t => !string.IsNullOrEmpty(t.SourcePath)),
                            "Все задания имеют SourcePath", "Есть задания без Source Path");

                        Check(allTasks.All(t => t.TargetPaths?.Count > 0),
                            "Все задания имеют TargetPaths", "Есть задания без Target Paths");

                        if (allTasks.Any(t => t.SourcePath != null && t.SourcePath.Contains("{DATE}")))
                            Console.WriteLine("⚠ ВНИМАНИЕ: Найдена переменная {DATE} (это нормально, заменяется в рантайме)");
                    }
                }

                Console.ForegroundColor = allPassed ? ConsoleColor.Green : ConsoleColor.Red;
                Console.WriteLine(allPassed ? "\n✓✓✓ ВСЕ КРИТИЧЕСКИЕ ПРОВЕРКИ ПРОЙДЕНЫ! ✓✓✓\n" : "\n❌ НЕКОТОРЫЕ ПРОВЕРКИ НЕ ПРОЙДЕНЫ\n");
                Console.ResetColor();

                // --- СТАТИСТИКА ---
                if (allSettings != null && allSettings.ContainsKey("ALL"))
                {
                    Console.WriteLine("📊 СТАТИСТИКА:");
                    Console.WriteLine($"   Всего проектов: {allSettings.Keys.Count(k => k != "ALL")}");
                    Console.WriteLine($"   Всего уникальных заданий: {allSettings["ALL"].Count}");
                }

                // --- REVIT SERVER ---
                Console.WriteLine("\n--- ТЕСТЫ RevitServer ---");
                TestRevitServer.TestExtractServerMethod();
                TestRevitServer.TestBuildUrlMethod();
                TestRevitServer.TestLoggingCallbaks();
                TestRevitServer.TestReadModelsFromRealServer();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ ОШИБКА: {ex.Message}\n{ex.StackTrace}");
                Console.ResetColor();
            }

            Console.WriteLine("\nНажми любую клавишу для выхода...");
            Console.ReadKey();
        }

        private static string GetTestConfigsPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string debugPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "TestConfigs"));
            return Directory.Exists(debugPath) ? debugPath : Path.Combine(baseDir, "TestConfigs");
        }
    }
}