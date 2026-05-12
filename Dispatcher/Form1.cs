using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Dispatcher
{
    /// <summary>
    /// Главная форма диспетчера, принимающая данные температуры и давления от контроллера.
    /// </summary>
    public partial class Form1 : Form
    {
        private TcpListener listener;
        private Thread listenThread;
        private bool running = true;

        private int timeIndex;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Form1"/>.
        /// </summary>
        public Form1()
        {
            InitializeComponent();
            InitCharts();
            StartServer();
        }

        /// <summary>
        /// Инициализирует графики температуры и давления.
        /// </summary>
        private void InitCharts()
        {
            chartTemp.Series.Clear();
            chartPress.Series.Clear();

            Series tempSeries = new Series("Температура");
            tempSeries.ChartType = SeriesChartType.Line;
            tempSeries.XValueType = ChartValueType.Int32;
            tempSeries.YValueType = ChartValueType.Double;

            Series pressSeries = new Series("Давление");
            pressSeries.ChartType = SeriesChartType.Line;
            pressSeries.XValueType = ChartValueType.Int32;
            pressSeries.YValueType = ChartValueType.Double;

            chartTemp.Series.Add(tempSeries);
            chartPress.Series.Add(pressSeries);

            chartTemp.ChartAreas[0].AxisX.Title = "Время (с)";
            chartTemp.ChartAreas[0].AxisY.Title = "Температура (°C)";

            chartPress.ChartAreas[0].AxisX.Title = "Время (с)";
            chartPress.ChartAreas[0].AxisY.Title = "Давление (атм)";
        }

        /// <summary>
        /// Запускает TCP‑сервер для приёма данных от контроллера.
        /// </summary>
        private void StartServer()
        {
            listener = new TcpListener(IPAddress.Any, 9002);
            listener.Start();

            listenThread = new Thread(ListenLoop);
            listenThread.IsBackground = true;
            listenThread.Start();
        }

        /// <summary>
        /// Основной цикл ожидания подключений.
        /// </summary>
        private void ListenLoop()
        {
            while (running)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(HandleClient, client);
                }
                catch
                {
                    if (!running)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Обрабатывает подключение клиента и получение данных.
        /// </summary>
        /// <param name="state">Объект клиента.</param>
        private void HandleClient(object state)
        {
            TcpClient client = state as TcpClient;

            if (client == null)
            {
                return;
            }

            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[256];
                    int bytes = stream.Read(buffer, 0, buffer.Length);

                    if (bytes <= 0)
                    {
                        return;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, bytes).Trim();

                    double temperature = 0.0;
                    double pressure = 0.0;

                    string[] parts = message.Split(';');

                    foreach (string part in parts)
                    {
                        if (part.StartsWith("TEMP="))
                        {
                            string value = part.Substring(5).Replace(',', '.');

                            double.TryParse(
                                value,
                                System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out temperature);
                        }
                        else if (part.StartsWith("PRESS="))
                        {
                            string value = part.Substring(6).Replace(',', '.');

                            double.TryParse(
                                value,
                                System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out pressure);
                        }
                    }

                    AddPoint(temperature, pressure);
                }
            }
            catch
            {
                // Ошибка игнорируется.
            }
        }

        /// <summary>
        /// Добавляет новую точку на графики температуры и давления.
        /// </summary>
        /// <param name="temperature">Температура.</param>
        /// <param name="pressure">Давление.</param>
        private void AddPoint(double temperature, double pressure)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<double, double>(AddPoint), temperature, pressure);
                return;
            }

            timeIndex++;

            chartTemp.Series[0].Points.AddXY(timeIndex, temperature);
            chartPress.Series[0].Points.AddXY(timeIndex, pressure);

            if (chartTemp.Series[0].Points.Count > 100)
            {
                chartTemp.Series[0].Points.RemoveAt(0);
            }

            if (chartPress.Series[0].Points.Count > 100)
            {
                chartPress.Series[0].Points.RemoveAt(0);
            }

            chartTemp.ChartAreas[0].RecalculateAxesScale();
            chartPress.ChartAreas[0].RecalculateAxesScale();
        }

        /// <summary>
        /// Обрабатывает закрытие формы и завершает работу сервера.
        /// </summary>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            running = false;

            try
            {
                listener.Stop();
            }
            catch
            {
                // Ошибка игнорируется.
            }
        }

        private void chartPress_Click(object sender, EventArgs e)
        {
        }

        private void chartTemp_Click_1(object sender, EventArgs e)
        {
        }
    }
}
