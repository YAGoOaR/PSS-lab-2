using Newtonsoft.Json;
using System.Globalization;

namespace Lab2
{
    internal class ProgramIO
    {
        const int MAX_NUM = 10000;
        static Random random = new Random();

        // Генерація матриць
        static Matrix genMatrix((int, int) shape)
        {
            (int n, int m) = shape;
            double[,] array = new double[n, m];

            for (int i = 0; i < n; ++i)
                for (int j = 0; j < m; ++j)
                    array[i, j] = random.NextDouble() * MAX_NUM;
            return new Matrix(array);
        }
        // Генерація всіх змінних.
        // Вважаємо, що вектор - це матриця з 1 рядком.
        public static Dictionary<string, object> generateVariables((int, int) shape)
        {
            return new Dictionary<string, object>
            {
                ["B"] = genMatrix((1, shape.Item1)),
                ["D"] = genMatrix((1, shape.Item1)),
                ["MC"] = genMatrix(shape),
                ["MD"] = genMatrix(shape),
                ["MX"] = genMatrix(shape),
                ["b"] = random.NextDouble() * MAX_NUM,
            };
        }
        // Перетворення змінних у json-формат
        public static Dictionary<string, string> VariablesToJson(Dictionary<string, object> variables) => variables.ToDictionary(x => x.Key,
            x => x.Value switch
            {
                Matrix m => Matrix.MatrixToJson(m),
                _ => JsonConvert.SerializeObject(x.Value, Formatting.None),
            }
        );

        // Завантаження вхідних даних з файла або їх генерація та запис, якщо файл відсутній
        public static List<Dictionary<string, object>> LoadOrGenerate((int, int, int) shapeRange, string fileName, bool rewrite = false)
        {
            if (!File.Exists(fileName) || rewrite)
            {
                Console.WriteLine("Generating input...");
                List<Dictionary<string, string>> jsonVariables = new();

                for (int s = shapeRange.Item1; s < shapeRange.Item2; s += shapeRange.Item3)
                {
                    var variables = generateVariables((s, s));
                    var variablesString = VariablesToJson(variables);
                    jsonVariables.Add(variablesString);
                }

                WriteToJson(jsonVariables, fileName);
            } else
            {
                Console.WriteLine("Input file exists. Reading...");
            }

            return ReadFromJson(fileName);
        }

        // Запис даних у json
        public static void WriteToJson(List<Dictionary<string, string>> dict, string fileName)
        {
            string json = JsonConvert.SerializeObject(dict, Formatting.Indented);
            File.WriteAllText(fileName, json);
        }
        
        // Читання даних з json
        public static List<Dictionary<string, object>> ReadFromJson(string fileName)
        {
            string json = File.ReadAllText(fileName);
            List<Dictionary<string, string>>? variablesStringList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(json);

            if (variablesStringList is null) throw new FileLoadException();

            return variablesStringList.Select(e => e.ToDictionary(x => x.Key, x => (object)(
                    double.TryParse(x.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double d) ? d : Matrix.MatrixFromJson(x.Value)
            ))).ToList();
        }
    }
}
