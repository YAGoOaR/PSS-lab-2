
namespace Lab2
{
    internal class Utils
    {
        // Вимірювання часу роботи певної функції
        public static (T, float) MeasureTime<T>(Func<T> func)
        {
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            T res = func();
            watch.Stop();
            return (res, watch.ElapsedMilliseconds);
        }
    }
}
