
namespace Lab2
{
    internal class Calculations
    {
        const bool printResults = true; // Вивід результатів обчислень в консоль
        const bool fullMatrixOutput = false; // Вивід повного вигляду матриць в консоль

        static object consoleColorLocker = new();

        // Клас Matrix я визначив вручну, як і операції, пов'язані з ним. (Див. файл Matrix.cs) 
        static Matrix Calc_E(Dictionary<string, object> variables)
        {
            Matrix B = (Matrix)variables["B"];
            Matrix D = (Matrix)variables["D"];
            Matrix MC = (Matrix)variables["MC"];

            // E = В * МС + D * min(MC)
            
            // Символ % я встановив як операцію паралельного множення матриць (див. перевантаження операторів в класі Matrix)
            Matrix E = B % MC + D * MC.Min();

            if (printResults)
            {
                // Використання синхронізації потоків за допомогою lock.
                // Синхронізація тут потрібна для запобігання помилок з узгодженням пам'яті (Memory consistency error) змінної кольору виводу в термінал.
                // Тобто, якби синхронізація не була використана, потоки б плутали між собою попередній колір, і він перемикався б на зелений, і вивід поламався.
                // Щодо самого виводу в консоль, то в C# операція Console.WriteLine є потокобезпечною, тобто синхронізованою між потоками.
                // Міг би переплутатись лише порядок виконання виводу назви матриці та самої матриці (див. нижче)
                lock (consoleColorLocker)
                {
                    ConsoleColor prevColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("E:");
                    Console.WriteLine(fullMatrixOutput ? E.ToFullString() : E.ToString());
                    Console.ForegroundColor = prevColor;
                }
            }

            return E;
        }

        static Matrix Calc_MA(Dictionary<string, object> variables)
        {
            Matrix MC = (Matrix)variables["MC"];
            Matrix MD = (Matrix)variables["MC"];
            Matrix MX = (Matrix)variables["MC"];
            double b = (double)variables["b"];

            // MА = b * MD * (MC - MX) + MX * MC * b

            // Символ % я встановив як операцію паралельного множення матриць (див. перевантаження операторів в класі Matrix)
            var calc_res1 = () => MD % (MC - MX);
            var calc_res2 = () => MX % MC;

            var parallelTask1 = Task.Factory.StartNew(calc_res1);
            var parallelTask2 = Task.Factory.StartNew(calc_res2);

            parallelTask1.Wait();
            parallelTask2.Wait();

            //Matrix MA = b * MD % (MC - MX) + MX % MC * b;
            Matrix MA = b * parallelTask1.Result + parallelTask2.Result * b;


            if (printResults)
            {
                // Див. коментарі про попередній lock
                lock (consoleColorLocker)
                {
                    ConsoleColor prevColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("MA:");
                    Console.WriteLine(fullMatrixOutput ? MA.ToFullString() : MA.ToString());
                    Console.ForegroundColor = prevColor;
                }
            }

            return MA;
        }

        // Виконання програми з паралельними обчисленнями
        public static (Matrix, Matrix) Run(Dictionary<string, object> variables)
        {
            Task<Matrix> TaskE = Task.Factory.StartNew(() => Calc_E(variables));
            Task<Matrix> TaskMA = Task.Factory.StartNew(() => Calc_MA(variables));

            TaskE.Wait();
            TaskMA.Wait();

            Matrix E = TaskE.Result;
            Matrix MA = TaskMA.Result;

            return (E, MA);
        }
    }
}
