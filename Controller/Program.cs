using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Controller
{
    /// <summary>
    /// Точка входа в программу контроллера технологического процесса.
    /// Контроллер генерирует значения температуры и давления и отправляет их диспетчеру.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Главный метод программы. Генерирует данные и отправляет их на сервер.
        /// </summary>
        /// <param name="args">Аргументы командной строки.</param>
        private static void Main(string[] args)
        {
            string serverIp = "127.0.0.1";
            int serverPort = 9002;

            Console.WriteLine("Контроллер запущен.");
            Console.WriteLine("Нажмите Enter для старта...");
            Console.ReadLine();

            Random random = new Random();

            while (true)
            {
                // Генерация температуры и давления.
                double temperature = random.NextDouble() * 100.0;
                double pressure = random.NextDouble() * 6.0;

                string message = $"TEMP={temperature:F2};PRESS={pressure:F2}";

                try
                {
                    using (TcpClient client = new TcpClient(serverIp, serverPort))
                    {
                        using (NetworkStream stream = client.GetStream())
                        {
                            byte[] data = Encoding.UTF8.GetBytes(message);
                            stream.Write(data, 0, data.Length);
                        }
                    }

                    Console.WriteLine($"Отправлено: {message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка: {ex.Message}");
                }

                Thread.Sleep(1000);
            }
        }
    }
}
