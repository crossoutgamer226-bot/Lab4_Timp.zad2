using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Dispatcher
{
    public partial class Form1 : Form
    {
        private TcpListener listener;
        private Thread listenThread;
        private bool running = true;

        private int timeIndex = 0;

        public Form1()
        {
            InitializeComponent();
            InitCharts();
            StartServer();
        }

        // ------------------------------------------------------------
        // ИНИЦИАЛИЗАЦИЯ ГРАФИКОВ
        // ------------------------------------------------------------
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

        // ------------------------------------------------------------
        // ЗАПУСК TCP-СЕРВЕРА
        // ------------------------------------------------------------
        private void StartServer()
        {
            listener = new TcpListener(IPAddress.Any, 9002);
            listener.Start();

            listenThread = new Thread(ListenLoop);
            listenThread.IsBackground = true;
            listenThread.Start();
        }

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
                    if (!running) break;
                }
            }
        }

        // ------------------------------------------------------------
        // ОБРАБОТКА ПОДКЛЮЧЕНИЯ КЛИЕНТА
        // ------------------------------------------------------------
        private void HandleClient(object state)
        {
            TcpClient client = state as TcpClient;
            if (client == null) return;

            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[256];
                    int bytes = stream.Read(buffer, 0, buffer.Length);
                    if (bytes <= 0) return;

                    string msg = Encoding.UTF8.GetString(buffer, 0, bytes).Trim();

                    double temp = 0;
                    double press = 0;

                    string[] parts = msg.Split(';');
                    foreach (string part in parts)
                    {
                        if (part.StartsWith("TEMP="))
                        {
                            string val = part.Substring(5).Replace(',', '.');
                            double.TryParse(val, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out temp);
                        }
                        else if (part.StartsWith("PRESS="))
                        {
                            string val = part.Substring(6).Replace(',', '.');
                            double.TryParse(val, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out press);
                        }
                    }

                    AddPoint(temp, press);
                }
            }
            catch
            {
                // Игнорируем ошибки
            }
        }

        // ------------------------------------------------------------
        // ДОБАВЛЕНИЕ ТОЧЕК НА ГРАФИКИ
        // ------------------------------------------------------------
        private void AddPoint(double temp, double press)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<double, double>(AddPoint), temp, press);
                return;
            }

            timeIndex++;

            chartTemp.Series[0].Points.AddXY(timeIndex, temp);
            chartPress.Series[0].Points.AddXY(timeIndex, press);

            if (chartTemp.Series[0].Points.Count > 100)
                chartTemp.Series[0].Points.RemoveAt(0);

            if (chartPress.Series[0].Points.Count > 100)
                chartPress.Series[0].Points.RemoveAt(0);

            chartTemp.ChartAreas[0].RecalculateAxesScale();
            chartPress.ChartAreas[0].RecalculateAxesScale();
        }

        // ------------------------------------------------------------
        // КОРРЕКТНОЕ ЗАВЕРШЕНИЕ СЕРВЕРА
        // ------------------------------------------------------------
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            running = false;
            try { listener.Stop(); } catch { }
        }

        // ------------------------------------------------------------
        // ПУСТЫЕ ОБРАБОТЧИКИ
        // ------------------------------------------------------------
        private void chartPress_Click(object sender, EventArgs e) { }
        private void chartTemp_Click_1(object sender, EventArgs e) { }
    }
}
