using Newtonsoft.Json;

namespace Lab2
{
    class Matrix
    {
        public readonly (int, int) shape;
        double[,] values;
        public double[,] Values { get => values; }

        // Кількість потоків для паралельного множення
        const int threads = 8;

        // Конструктор матриці за 2д-масивом
        public Matrix(double[,] array)
        {
            values = array;
            shape = (array.GetLength(0), array.GetLength(1));
        }

        // Алгоритм Кахана (тобто метод, що зображує одну ітерацію алгоритму)
        static (double, double) kahan_sum(double sum, double c, double val)
        {
            double y = val - c;
            double t = sum + y;
            c = (t - sum) - y;
            sum = t;
            return (sum, c);
        }

        // Функція для ділення проміжків на n частин
        // Потрібна для розділення паралельного множення рядків матриці на потоки
        // Тобто, 1,2,3...n інтервали відповідають 1,2,3...n потокам обчислень
        static List<(int, int)> SplitRange((int, int) range, int nparts)
        {
            int len = range.Item2 - range.Item1;
            (int k, int m) = Math.DivRem(len, nparts);
            List<(int, int)> result = new();
            
            for (int i = 0; i < nparts; i++)
            {
                int start = range.Item1 + i * k + Math.Min(i, m);
                int end = range.Item1 + (i + 1) * k + Math.Min(i + 1, m);
                result.Add((start, end));
            }

            return result;
        }

        // Послідовне додавання
        public static Matrix operator +(Matrix a, Matrix b)
        {
            double[,] matrixA = a.values;
            double[,] matrixB = b.values;

            int rowA = matrixA.GetLength(0);
            int colA = matrixA.GetLength(1);
            int rowB = matrixB.GetLength(0);
            int colB = matrixB.GetLength(1);

            if (rowA != rowB || colA != colB)
            {
                throw new ArgumentException("Invalid matrix dimensions for addition.");
            }

            double[,] resultMatrix = new double[rowA, colA];

            for (int i = 0; i < rowA; i++)
            {
                for (int j = 0; j < colA; j++)
                {
                    resultMatrix[i, j] = matrixA[i, j] + matrixB[i, j];
                }
            }

            return new(resultMatrix);
        }

        // Послідовне віднімання
        public static Matrix operator -(Matrix a, Matrix b)
        {
            double[,] matrixA = a.values;
            double[,] matrixB = b.values;

            int rowA = matrixA.GetLength(0);
            int colA = matrixA.GetLength(1);
            int rowB = matrixB.GetLength(0);
            int colB = matrixB.GetLength(1);

            if (rowA != rowB || colA != colB)
            {
                throw new ArgumentException("Invalid matrix dimensions for subtraction.");
            }

            double[,] resultMatrix = new double[rowA, colA];

            for (int i = 0; i < rowA; i++)
            {
                for (int j = 0; j < colA; j++)
                {
                    resultMatrix[i, j] = matrixA[i, j] - matrixB[i, j];
                }
            }

            return new(resultMatrix);
        }

        // Операція послідовного множення (без потоків. Див. паралельне множення далі)
        public static Matrix operator *(Matrix a, Matrix b)
        {
            double[,] matrixA = a.values;
            double[,] matrixB = b.values;

            int rowA = matrixA.GetLength(0);
            int colA = matrixA.GetLength(1);
            int rowB = matrixB.GetLength(0);
            int colB = matrixB.GetLength(1);

            if (colA != rowB)
            {
                throw new ArgumentException("Invalid matrix dimensions for multiplication.");
            }

            double[,] resultMatrix = new double[rowA, colB];

            for (int i = 0; i < rowA; i++)
            {
                for (int j = 0; j < colB; j++)
                {
                    double sum = 0.0;
                    double c = 0.0;
                    for (int k = 0; k < colA; k++)
                    {
                        (sum, c) = kahan_sum(sum, c, matrixA[i, k] * matrixB[k, j]);
                    }
                    resultMatrix[i, j] = sum;
                }
            }

            return new(resultMatrix);
        }

        // Послідовне множення на число
        public static Matrix operator *(Matrix a, double scalar)
        {
            double[,] matrix = a.values;

            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            double[,] resultMatrix = new double[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    resultMatrix[i, j] = matrix[i, j] * scalar;
                }
            }

            return new(resultMatrix);
        }
        // Послідовне множення числа на матрицю
        public static Matrix operator *(double a, Matrix b) => b * a;

        // Операція паралельного множення матриць
        // (вирішив обрати для неї оператор % бо в C# він має той самий приорітет, як і "*")
        public static Matrix operator %(Matrix a, Matrix b)
        {
            double[,] matrixA = a.values;
            double[,] matrixB = b.values;

            int rowA = matrixA.GetLength(0);
            int colA = matrixA.GetLength(1);
            int rowB = matrixB.GetLength(0);
            int colB = matrixB.GetLength(1);

            if (colA != rowB)
            {
                throw new ArgumentException("Invalid matrix dimensions for multiplication.");
            }

            double[,] resultMatrix = new double[rowA, colB];

            object testLocker = new();

            // Операція, яка буде виконана в різних потоках для визначеного проміжку рядків матриці
            var calculate = (int rangeStart, int rangeEnd) =>
            {
                for (int i = rangeStart; i < rangeEnd; i++)
                {
                    for (int j = 0; j < colB; j++)
                    {
                        double sum = 0.0;
                        double c = 0.0;
                        for (int k = 0; k < colA; k++)
                        {
                            (sum, c) = kahan_sum(sum, c, matrixA[i, k] * matrixB[k, j]);
                        }
                        // Використання lock при записі у спільну для потоків матричну змінну
                        // Насправді, lock тут не потрібен, бо потоки звертаються кожен до своїх, окремих, індексів матриці,
                        // і тому не спричиняють memory consistency помилок.
                        // Lock я додав з тієї причини, що нам потрібно протестувати lock elision.
                        // Тобто, оскільки блокування можна обійти, це і повинен (в теорії) зробити механізм lock elision. Докладніше в звіті.
                        lock (testLocker)
                        {
                            resultMatrix[i, j] = sum;
                        }
                    }
                }
            };

            (int, int) rowRange = (0, rowA);
            // Запуск потоків, їх поділ на проміжки за рядками матриць (див. пояснення над визначенням методу SplitRange вище)
            var tasks = SplitRange(rowRange, threads).Select(range => Task.Factory.StartNew(() => calculate(range.Item1, range.Item2))).ToList();

            // Синхронізація потоків
            foreach (Task t in tasks)
            {
                t.Wait();
            }

            return new(resultMatrix);
        }

        // Повний вивід матриці
        public string ToFullString()
        {
            return "[" + string.Join(",\n ", values.OfType<double>()
                .Select((value, index) => new { value, index })
                .GroupBy(x => x.index / values.GetLength(1), x => x.value,
                    (i, floats) => $"[{string.Join(", ", floats)}]")) + "]";
        }

        // Скорочений вивід матриці
        public override string? ToString()
        {
            return $"Matrix {values.GetLength(0)}x{values.GetLength(1)}";
        }

        // Мінімальне значення серед елементів
        internal double Min()
        {
            int rows = values.GetLength(0);
            int cols = values.GetLength(1);

            double min = values[0, 0];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (values[i, j] < min)
                    {
                        min = values[i, j];
                    }
                }
            }

            return min;
        }

        // Перетворення матриці в строку JSON-формату
        public static string MatrixToJson(Matrix m)
        {
            double[,] matrix = m.Values;
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            double[][] matrixArray = new double[rows][];

            for (int i = 0; i < rows; i++)
            {
                matrixArray[i] = new double[cols];

                for (int j = 0; j < cols; j++)
                {
                    matrixArray[i][j] = matrix[i, j];
                }
            }

            return JsonConvert.SerializeObject(matrixArray, Formatting.None);
        }

        // Перетворення строки JSON-формату в матрицю
        public static Matrix MatrixFromJson(string json)
        {
            double[][]? matrixArray = JsonConvert.DeserializeObject<double[][]>(json);

            if (matrixArray is null) throw new FileLoadException();

            int rows = matrixArray.Length;
            int cols = matrixArray[0].Length;

            double[,] matrix = new double[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    matrix[i, j] = matrixArray[i][j];
                }
            }

            return new(matrix);
        }
    }
}
