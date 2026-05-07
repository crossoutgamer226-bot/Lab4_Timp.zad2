using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Controller
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string ip = "127.0.0.1";
            int port = 9002;

            Console.WriteLine("Контроллер запущен.");
            Console.WriteLine("Нажмите Enter для старта...");
            Console.ReadLine();

            Random rnd = new Random();

            while (true)
            {
                double temp = rnd.NextDouble() * 100.0;
                double press = rnd.NextDouble() * 6.0;

                string msg = $"TEMP={temp:F2};PRESS={press:F2}";

                try
                {
                    using (TcpClient client = new TcpClient(ip, port))
                    {
                        using (NetworkStream stream = client.GetStream())
                        {
                            byte[] data = Encoding.UTF8.GetBytes(msg);
                            stream.Write(data, 0, data.Length);
                        }
                    }

                    Console.WriteLine("Отправлено: " + msg);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка: " + ex.Message);
                }

                Thread.Sleep(1000);
            }
        }
    }
}
