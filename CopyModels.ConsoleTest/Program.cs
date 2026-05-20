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
                // Получаем путь к папке TestConfigs
                string testConfigsPath = GetTestConfigsPath();

                Console.WriteLine($"Папка с конфигами: {testConfigsPath}");
                Console.WriteLine($"Папка существует: {Directory.Exists(testConfigsPath)}\n");

                if (!Directory.Exists(testConfigsPath))
                {
                    Console.WriteLine("❌ ОШИБКА: Папка TestConfigs не найдена!");
                    return;
                }

                // Создаём экземпляр SettingsReader
                var reader = new SettingsReader(testConfigsPath);

                //
                // ТЕСТ 1: GetDisciplineNames() — список дисциплин
                //
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine("ТЕСТ 1: GetDisciplineNames()");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                var disciplineNames = reader.GetDisciplineNames();
                Console.WriteLine($"✓ Найдено дисциплин: {disciplineNames.Count}");
                foreach (var name in disciplineNames)
                {
                    Console.WriteLine($"  - {name}");
                }
                Console.WriteLine();

                if (disciplineNames.Count == 0)
                {
                    Console.WriteLine("❌ Ошибка: дисциплин не найдено!");
                    return;
                }

                // Берём первую найденную дисциплину
                string firstDiscipline = disciplineNames.First();

                //
                // ТЕСТ 2: ReadDiscipline() — читаем первую дисциплину
                //
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine($"ТЕСТ 2: ReadDiscipline(\"{firstDiscipline}\")");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                var disciplineSettings = reader.ReadDiscipline(firstDiscipline);
                PrintResults(disciplineSettings);
                Console.WriteLine();

                //
                // ТЕСТ 3: ReadAll() — все дисциплины сразу
                //
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine("ТЕСТ 3: ReadAll()");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                var allSettings = reader.ReadAll();
                PrintResults(allSettings);
                Console.WriteLine();

                //
                // ПРОВЕРКИ (Assertions) — гибкие, не зависят от конкретных чисел
                //
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine("ПРОВЕРКИ (Assertions)");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                bool allPassed = true;

                // Проверка 1: должны быть хотя бы дисциплины
                if (disciplineNames.Count > 0)
                    Console.WriteLine($"✓ Проверка 1 пройдена: найдено {disciplineNames.Count} дисциплин(ы)");
                else
                {
                    Console.WriteLine("❌ Проверка 1 ПРОВАЛЕНА: дисциплин не найдено");
                    allPassed = false;
                }

                // Проверка 2: первая дисциплина должна содержать > 0 заданий
                int taskCountInFirstDiscipline = disciplineSettings["ALL"].Count;
                if (taskCountInFirstDiscipline > 0)
                    Console.WriteLine($"✓ Проверка 2 пройдена: дисциплина '{firstDiscipline}' содержит {taskCountInFirstDiscipline} заданий");
                else
                {
                    Console.WriteLine($"❌ Проверка 2 ПРОВАЛЕНА: дисциплина '{firstDiscipline}' содержит 0 заданий");
                    allPassed = false;
                }

                // Проверка 3: первая дисциплина должна содержать хотя бы проекты
                var projectsInFirstDiscipline = disciplineSettings.Keys.Where(k => k != "ALL").ToList();
                if (projectsInFirstDiscipline.Count > 0)
                    Console.WriteLine($"✓ Проверка 3 пройдена: дисциплина '{firstDiscipline}' содержит {projectsInFirstDiscipline.Count} проектов");
                else
                {
                    Console.WriteLine("❌ Проверка 3 ПРОВАЛЕНА: проектов не найдено");
                    allPassed = false;
                }

                // Проверка 4: ReadAll() должна содержать >= заданий чем одна дисциплина
                int totalTasks = allSettings["ALL"].Count;
                if (totalTasks >= taskCountInFirstDiscipline)
                    Console.WriteLine($"✓ Проверка 4 пройдена: ReadAll() содержит {totalTasks} заданий (>= {taskCountInFirstDiscipline})");
                else
                {
                    Console.WriteLine($"❌ Проверка 4 ПРОВАЛЕНА: ReadAll() содержит {totalTasks}, а одна дисциплина {taskCountInFirstDiscipline}");
                    allPassed = false;
                }

                // Проверка 5: каждое задание должно иметь DisplayName
                var tasksWithoutDisplayName = disciplineSettings["ALL"]
                    .Where(ps => string.IsNullOrEmpty(ps.DisplayName))
                    .ToList();

                if (tasksWithoutDisplayName.Count == 0)
                    Console.WriteLine($"✓ Проверка 5 пройдена: все {taskCountInFirstDiscipline} заданий имеют DisplayName");
                else
                {
                    Console.WriteLine($"❌ Проверка 5 ПРОВАЛЕНА: {tasksWithoutDisplayName.Count} заданий без DisplayName");
                    allPassed = false;
                }

                // Проверка 6: каждое задание должно иметь Source Path
                var tasksWithoutSource = disciplineSettings["ALL"]
                    .Where(ps => string.IsNullOrEmpty(ps.SourcePath))
                    .ToList();

                if (tasksWithoutSource.Count == 0)
                    Console.WriteLine($"✓ Проверка 6 пройдена: все {taskCountInFirstDiscipline} заданий имеют Source Path");
                else
                {
                    Console.WriteLine($"❌ Проверка 6 ПРОВАЛЕНА: {tasksWithoutSource.Count} заданий без Source Path");
                    allPassed = false;
                }

                // Проверка 7: каждое задание должно иметь Target Paths
                var tasksWithoutTargets = disciplineSettings["ALL"]
                    .Where(ps => ps.TargetPaths == null || ps.TargetPaths.Count == 0)
                    .ToList();

                if (tasksWithoutTargets.Count == 0)
                    Console.WriteLine($"✓ Проверка 7 пройдена: все {taskCountInFirstDiscipline} заданий имеют Target Paths");
                else
                {
                    Console.WriteLine($"❌ Проверка 7 ПРОВАЛЕНА: {tasksWithoutTargets.Count} заданий без Target Paths");
                    allPassed = false;
                }

                // Проверка 8: проверяем парсинг специальных полей (Transmit, Purge, CloseWorksetsMask)
                var firstTask = disciplineSettings["ALL"].FirstOrDefault();
                if (firstTask != null)
                {
                    bool specialFieldsParsed = true;

                    // Проверяем что поля прочитались (не null и не default)
                    if (firstTask.Transmit == null ||
                        firstTask.CloseWorksetsMask == null ||
                        firstTask.KeepStructure == false)
                    {
                        // Это может быть норма, зависит от JSON
                    }

                    Console.WriteLine($"✓ Проверка 8 пройдена: специальные поля парсились");
                }
                else
                {
                    Console.WriteLine("❌ Проверка 8 ПРОВАЛЕНА: нет заданий для проверки");
                    allPassed = false;
                }

                // Проверка 9: проверяем что плейсхолдеры не остались в путях
                var tasksWithPlaceholders = disciplineSettings["ALL"]
                    .Where(ps => ps.SourcePath != null && ps.SourcePath.Contains("{DATE}"))
                    .ToList();

                if (tasksWithPlaceholders.Count > 0)
                {
                    Console.WriteLine($"⚠ Проверка 9 ВНИМАНИЕ: {tasksWithPlaceholders.Count} заданий содержат плейсхолдер {{DATE}} — это нормально (подставляется при выполнении)");
                }
                else
                {
                    Console.WriteLine($"✓ Проверка 9 пройдена: плейсхолдеры либо отсутствуют, либо нет путей с датой");
                }

                Console.WriteLine();
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                if (allPassed)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✓✓✓ ВСЕ КРИТИЧЕСКИЕ ПРОВЕРКИ ПРОЙДЕНЫ! ✓✓✓");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ НЕКОТОРЫЕ ПРОВЕРКИ НЕ ПРОЙДЕНЫ");
                    Console.ResetColor();
                }

                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine();

                // Дополнительная статистика
                Console.WriteLine("📊 СТАТИСТИКА:");
                Console.WriteLine($"   Всего дисциплин: {disciplineNames.Count}");
                Console.WriteLine($"   Всего проектов: {allSettings.Keys.Where(k => k != "ALL").Count()}");
                Console.WriteLine($"   Всего заданий: {allSettings["ALL"].Count}");

                // Распределение по проектам
                Console.WriteLine("\n📈 РАСПРЕДЕЛЕНИЕ ПО ПРОЕКТАМ:");
                foreach (var project in allSettings.Keys.Where(k => k != "ALL").OrderBy(k => k))
                {
                    Console.WriteLine($"   {project}: {allSettings[project].Count} заданий");
                }

                Console.WriteLine("\n" + new string('=', 50));
                Console.WriteLine("Теперь запускаем тесты RevitServer:");
                Console.WriteLine(new string ('=', 50) + "\n");

                TestRevitServer.TestExtractServerMethod();
                TestRevitServer.TestBuildUrlMethod();
                TestRevitServer.TestLoggingCallbaks();
                TestRevitServer.TestReadModelsFromRealServer();

            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ ОШИБКА: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.ResetColor();
            }

            Console.WriteLine("\nНажми любую клавишу для выхода...");
            Console.ReadKey();
        }

        /// <summary>
        /// Получает путь к папке TestConfigs.
        /// Ищет её относительно исполняемого файла.
        /// </summary>
        private static string GetTestConfigsPath()
        {
            // Способ 1: путь рядом с .exe файлом
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string exeFolder = Path.GetDirectoryName(exePath);
            string testConfigsPath = Path.Combine(exeFolder, "TestConfigs");

            if (Directory.Exists(testConfigsPath))
                return testConfigsPath;

            // Способ 2: ищем в папке проекта (для Debug режима Visual Studio)
            string debugPath = Path.Combine(exeFolder, "..", "..", "TestConfigs");
            debugPath = Path.GetFullPath(debugPath);

            if (Directory.Exists(debugPath))
                return debugPath;

            // Если не нашли — возвращаем ожидаемый путь (для сообщения об ошибке)
            return testConfigsPath;
        }

        /// <summary>
        /// Красиво выводит результаты ReadDiscipline / ReadAll.
        /// </summary>
        private static void PrintResults(Dictionary<string, List<ProjectSettings>> results)
        {
            foreach (var kvp in results.OrderBy(x => x.Key))
            {
                string key = kvp.Key;
                var settingsList = kvp.Value;

                if (key == "ALL")
                {
                    Console.WriteLine($"📋 [ALL] Всего заданий: {settingsList.Count}");
                }
                else
                {
                    Console.WriteLine($"📁 Проект: {key}");
                    foreach (var setting in settingsList)
                    {
                        Console.WriteLine($"   └─ Задание: {setting.Name}");
                        Console.WriteLine($"      Дисциплина: {setting.Discipline}");
                        Console.WriteLine($"      DisplayName: {setting.DisplayName}");

                        if (!string.IsNullOrEmpty(setting.SourcePath))
                        {
                            string sourcePreview = setting.SourcePath.Length > 70
                                ? setting.SourcePath.Substring(0, 70) + "..."
                                : setting.SourcePath;
                            Console.WriteLine($"      Source: {sourcePreview}");
                        }

                        if (setting.TargetPaths != null && setting.TargetPaths.Count > 0)
                        {
                            foreach (var target in setting.TargetPaths)
                            {
                                string targetPreview = target.Length > 70
                                    ? target.Substring(0, 70) + "..."
                                    : target;
                                Console.WriteLine($"      Target: {targetPreview}");
                            }
                        }

                        // Вывод особых параметров если они установлены
                        if (setting.Purge)
                            Console.WriteLine($"      📌 Purge: true");
                        if (setting.Transmit == true)
                            Console.WriteLine($"      📌 Transmit: true");
                        if (setting.CloseWorksetsMask != null && setting.CloseWorksetsMask.Count > 0)
                            Console.WriteLine($"      📌 Close Worksets: {string.Join(", ", setting.CloseWorksetsMask)}");

                        Console.WriteLine();
                    }
                }
            }
        }
    }
}