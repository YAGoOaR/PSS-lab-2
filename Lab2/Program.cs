
namespace Lab2
{
    internal class Program
    {
        // Вивід словника даних (наприклад, вхідних змінних)
        static void LogDict(Dictionary<string, object> dict) => Console.WriteLine(string.Join("\n", dict.Select(
                (kv, _) => kv.Value switch
                {
                    Matrix { shape: ( <= 10, <= 10) } matrix => $"{kv.Key} =\n{matrix.ToFullString()}",
                    _ => $"{kv.Key} = {kv.Value}",
                }
            )
        ));

        static void Main(string[] args)
        {
            // Вибір ім'я файла для запису часових результатів обчислень
            string timesFileName;
            int? runId = null;
            try
            {
                runId = int.Parse(args[0]);
            } catch (Exception)
            {
                runId = 1;
            }
            finally
            {
                timesFileName = $"timeResults{runId}.csv";
                Console.WriteLine($"Run time results will be saved to {timesFileName}");
            }

            (int, int, int) shapeRange = (100, 300, 10);
            string fileName = "input.json";
            string resultFileName = "output.json";

            // Завантаження вхідних даних з файла або їх генерація та запис, якщо файл відсутній
            var input = ProgramIO.LoadOrGenerate(shapeRange, fileName);

            List<(Dictionary<string, object>, (int, float))> results = new();

            Console.ForegroundColor = ConsoleColor.Blue;

            foreach (var variables in input)
            {
                // Хід циклу для певної розмірності даних
                // Вивід вхідних даних
                LogDict(variables);

                // Виконання обчислень (див. файл Calculations.сs) та вимірювання часу їх роботи
                var (res, time) = Utils.MeasureTime(() => Calculations.Run(variables));

                // Вивід часу роботи обчислень
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Run time: {time} ms");
                Console.ForegroundColor = ConsoleColor.Blue;

                // Словник з результатами
                Dictionary<string, object> resultsDict = new()
                {
                    ["E"] = res.Item1,
                    ["MA"] = res.Item2,
                };

                // Запис до всіх результатів
                results.Add((resultsDict, (res.Item1.shape.Item2, time)));
                Console.Write("\n");
            }

            // Перетворення результатів у json-формат і запис
            var stringResults = results.Select(x => x.Item1.ToDictionary(kv => kv.Key, kv => Matrix.MatrixToJson((Matrix)kv.Value))).ToList();
            ProgramIO.WriteToJson(stringResults, resultFileName);

            // Виокремлення часових результатів
            var times = results.Select(x => x.Item2).ToList();
            // Їх запис у csv-файл
            using (StreamWriter file = new(timesFileName))
            {
                file.Write("shape,time\n");
                file.Write(string.Join("\n", times.Select(x => $"{x.Item1},{x.Item2}").ToList()));
            }

            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
