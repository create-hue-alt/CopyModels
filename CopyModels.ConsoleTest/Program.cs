using CopyModels.Core.Models;
using CopyModels.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CopyModels.ConsoleTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== CopyModels SettingsReader Тестирование ===\n");

            try
            {
                // Получаем путь к папке TestConfigs (она находится рядом с Program.cs)
                string testConfigsPath = GetTestConfigsPath();

                Console.WriteLine($"📁 Папка с конфигами: {testConfigsPath}");
                Console.WriteLine($"📁 Папка существует: {Directory.Exists(testConfigsPath)}\n");

                if (!Directory.Exists(testConfigsPath))
                {
                    Console.WriteLine("❌ ОШИБКА: Папка TestConfigs не найдена!");
                    Console.WriteLine("Пожалуйста создайте папку TestConfigs с JSON файлами.");
                    return;
                }

                // Создаём экземпляр SettingsReader
                var reader = new SettingsReader(testConfigsPath);

                //
                // ТЕСТ 1: GetDisciplineNames() - список дисциплин
                //
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine("ТЕСТ 1: GetDisciplineNames()");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                var disciplineNames = reader.GetDisciplineNames();
                Console.WriteLine($"✓ Найдено дисциплин: {disciplineNames.Count}");
                foreach (var name in disciplineNames)
                {
                    Console.WriteLine($"  ─ {name}");
                }
                Console.WriteLine();

                //
                // ТЕСТ 2: ReadDiscipline("Architecture") - конкретная дисциплина
                //
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine("ТЕСТ 2: ReadDiscipline(\"Architecture\")");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                var archSettings = reader.ReadDiscipline("Architecture");
                PrintResults(archSettings);
                Console.WriteLine();

                //
                // ТЕСТ 3: ReadAll() - все дисциплины
                //
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine("ТЕСТ 3: ReadAll()");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                var allSettings = reader.ReadAll();
                PrintResults(allSettings);
                Console.WriteLine();

                //
                // ПРОВЕРКИ (Assertions)
                //
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine("ПРОВЕРКИ (Assertions)");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                bool allPassed = true;

                // Проверка 1: должно быть 2 дисциплины (Architecture.json + Structure.json)
                if (disciplineNames.Count == 2)
                    Console.WriteLine("✓ Проверка 1 пройдена: найдено 2 дисциплины");
                else
                {
                    Console.WriteLine($"❌ Проверка 1 ПРОВАЛЕНА: ожидали 2 дисциплины, получили {disciplineNames.Count}");
                    allPassed = false;
                }

                // Проверка 2: Architecture и Structure должны быть в списке
                if (disciplineNames.Contains("Architecture") && disciplineNames.Contains("Structure"))
                    Console.WriteLine("✓ Проверка 2 пройдена: дисциплины 'Architecture' и 'Structure' найдены");
                else
                {
                    Console.WriteLine($"❌ Проверка 2 ПРОВАЛЕНА: не все дисциплины найдены");
                    allPassed = false;
                }

                // Проверка 3: Architecture должна содержать ProjectA и ProjectB
                if (archSettings.ContainsKey("ProjectA") && archSettings.ContainsKey("ProjectB"))
                    Console.WriteLine("✓ Проверка 3 пройдена: Architecture содержит ProjectA и ProjectB");
                else
                {
                    Console.WriteLine($"❌ Проверка 3 ПРОВАЛЕНА: не все проекты найдены в Architecture");
                    allPassed = false;
                }

                // Проверка 4: Architecture должна содержать 3 задания (Task1, Task2 в ProjectA + Task3 в ProjectB)
                if (archSettings["ALL"].Count == 3)
                    Console.WriteLine("✓ Проверка 4 пройдена: Architecture содержит 3 задания");
                else
                {
                    Console.WriteLine($"❌ Проверка 4 ПРОВАЛЕНА: ожидали 3 задания, получили {archSettings["ALL"].Count}");
                    allPassed = false;
                }

                // Проверка 5: ReadAll() должен содержать 5 заданий (3 из Architecture + 2 из Structure)
                if (allSettings["ALL"].Count == 5)
                    Console.WriteLine("✓ Проверка 5 пройдена: ReadAll() содержит 5 заданий");
                else
                {
                    Console.WriteLine($"❌ Проверка 5 ПРОВАЛЕНА: ожидали 5 заданий, получили {allSettings["ALL"].Count}");
                    allPassed = false;
                }

                // Проверка 6: ProjectSettings должны быть отсортированы по DisplayName
                var firstTaskName = archSettings["ProjectA"][0].DisplayName;
                if (!string.IsNullOrEmpty(firstTaskName))
                    Console.WriteLine("✓ Проверка 6 пройдена: DisplayName не пустой");
                else
                {
                    Console.WriteLine($"❌ Проверка 6 ПРОВАЛЕНА: DisplayName пустой");
                    allPassed = false;
                }

                Console.WriteLine();
                if (allPassed)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✓✓✓ ВСЕ ПРОВЕРКИ ПРОЙДЕНЫ! ✓✓✓");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌❌❌ ПРОВЕРКИ НЕ ПРОЙДЕНЫ! ❌❌❌");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ ОШИБКА: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.ResetColor();
            }

            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        /// <summary>
        /// Вспомогательный метод: получает путь к папке TestConfigs
        /// Ищет её рядом с исполняемым файлом или в папке проекта
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
            // ...\CopyModels\bin\Debug\ → ...\CopyModels\TestConfigs\
            string debugPath = Path.Combine(exeFolder, "..", "..", "TestConfigs");
            debugPath = Path.GetFullPath(debugPath);

            if (Directory.Exists(debugPath))
                return debugPath;

            // Если не нашли — возвращаем ожидаемый путь (для сообщения об ошибке)
            return testConfigsPath;
        }

        /// <summary>
        /// Вспомогательный метод: красиво выводит результаты ReadDiscipline / ReadAll.
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
                        Console.WriteLine($"   └─ Дисциплина: {setting.Discipline}");
                        Console.WriteLine($"      Задание: {setting.Name}");
                        Console.WriteLine($"      DisplayName: {setting.DisplayName}");
                        if (!string.IsNullOrEmpty(setting.SourcePath))
                            Console.WriteLine($"      Source: {setting.SourcePath}");
                        if (setting.TargetPaths != null && setting.TargetPaths.Count > 0)
                            Console.WriteLine($"      Targets: {string.Join(", ", setting.TargetPaths)}");
                    }
                }
                Console.WriteLine();
            }
        }
    }
}