using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO.Ports;
using GMap.NET;
using GMap.NET.MapProviders;
using System.ComponentModel;
using System.Drawing;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System.Net;
using System.Net.Sockets;
using Microsoft.Maps.MapControl.WPF;
using Microsoft.Maps.MapControl.WPF.Design;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Location = Microsoft.Maps.MapControl.WPF.Location;

namespace Ipai.UnderwaterDepthResearch.Clients.EndClient.DroneOperator
{
    public record struct Sensor(string SensorName, string SensorValue); // Структура представляющая датчик
    public partial class MainWindow : Window
    {

        string dataGPS;
        double longitude;
        double latitude;

        SerialPort mySerialPort = new SerialPort();
        private event Action<System.Drawing.Image> NewFrameReceived;


        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            this.Closing += Window_Closing;

            this.NewFrameReceived = null!;            
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) 
        {
            if (mySerialPort.IsOpen) // При закрытии программы, закрываем порт
            {
                mySerialPort.Close();
            }
        }



        /*public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }*/

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            List<Sensor> shipSensors = new List<Sensor> // Список всех датчиков
            {
                new Sensor("Speed","0000 km/h"),
                new Sensor("Power engine","0000 W"),
                new Sensor("Battery charge","0000 V"),
                new Sensor("Charge current","0000 A"),
                new Sensor("Packages","0000"),
                new Sensor("Speed packages","0000"),
                new Sensor("Power","0000 W"),
                new Sensor("Magnetometer","0000"),
                new Sensor("GPS","0000")
            };
            List<Sensor> droneSensors = new List<Sensor> // Список всех датчиков
            {
                new Sensor("Gyroscope_1","0000"),
                new Sensor("Gyroscope_2","0000"),
                new Sensor("Accelerometer_1","0000"),
                new Sensor("Accelerometer_2","0000"),
                new Sensor("Battery","0000 mAh"),
                new Sensor("Battery charge","0000 V"),
                new Sensor("Charge current","0000 A"),
                new Sensor("Power engine_1","0000 W"),
                new Sensor("Power engine_2","0000 W"),
                new Sensor("Power engine_3","0000 W"),
                new Sensor("Power engine_4","0000 W"),
            };
            List<Sensor> carSensors = new List<Sensor> // Список всех датчиков
            {
                new Sensor("Speed","0000 km/h"),
                new Sensor("Accelerometer_1","0000"),
                new Sensor("Accelerometer_2","0000"),
                new Sensor("Power engine","0000 W"),
                new Sensor("Battery_1","0000 A"),
                new Sensor("Battery_2","0000 A"),
                new Sensor("GPS","0000"),
            };
            listSensorsShip.ItemsSource = shipSensors; // Заполнение таблицы датчиками
            listSensorsDrone.ItemsSource = droneSensors; // Заполнение таблицы датчиками
            listSensorsCar.ItemsSource = carSensors; // Заполнение таблицы датчиками         

            string[] ports = SerialPort.GetPortNames(); // Массив строк имён доступных ком портов
            try
            {
                mySerialPort.PortName = ports[1]; // Порт с которого считываются данные
                mySerialPort.Open(); // Открываем порт
                mySerialPort.ReadTimeout = 500;
                mySerialPort.DataReceived += mySerialPort_DataReceived; // Обработчик приёма данных
                indicatorArduino.Fill = System.Windows.Media.Brushes.LimeGreen;
            }
            catch
            {
                indicatorArduino.Fill = System.Windows.Media.Brushes.Firebrick;
            }

            new Thread(VideoTransfer.Start).Start();
            NewFrameReceived += OnNewFrameReceived;
            new Thread(ReceiveRepeating).Start();

            object? data = null;
            SerialDataReceivedEventArgs args = null;
            GpsDataReseived(data, args); 

            myMap.Focus();
            MapLayer imageLayer = new MapLayer();
            System.Windows.Controls.Image image = new System.Windows.Controls.Image();
            image.Height = 150;
            BitmapImage myBitmapImage = new BitmapImage();
            myBitmapImage.BeginInit();
            myBitmapImage.UriSource = new Uri($"{Directory.GetCurrentDirectory()}/plane.jfif");
            myBitmapImage.DecodePixelHeight = 50;
            myBitmapImage.EndInit();
            image.Source = myBitmapImage;
            image.Opacity = 1;
            image.Stretch = System.Windows.Media.Stretch.None;

            Location location = new Location(latitude, longitude);
            PositionOrigin position = PositionOrigin.Center;

            imageLayer.AddChild(image, location, position);
            myMap.Children.Add(imageLayer);
        }



        private void CreatePushpin(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            System.Windows.Point touchPoint = e.GetPosition(this);
            //touchPoint.X -= myMap.Margin.Left;
            //touchPoint.Y -= myMap.Margin.Top;

            touchPoint.X -= 1180;
            touchPoint.Y -= 582;
            Location pinLocation = myMap.ViewportPointToLocation(touchPoint);

            Pushpin pin = new Pushpin();
            pin.Location = pinLocation;

            myMap.Children.Add(pin);
        }

        private void DeletePushpin(object sender, KeyEventArgs e)
        {
            if (myMap.Children.Count > 0 && myMap.Children[myMap.Children.Count - 1] is Pushpin)
            {
                myMap.Children.RemoveAt(myMap.Children.Count - 1);
            }
        }

        private void GpsDataReseived(object? data, SerialDataReceivedEventArgs eventArgs)
        {
            try
            {
                dataGPS = "GPRMC,073111.00,A,4645.88427,N,03647.50032,E,0.145,,140422,,,A*74";

                if (dataGPS != null)
                {
                    byte commaCounter = 0;
                    string gpsLong = "";
                    for (int i = 0; i < dataGPS.Length; i++)
                    {
                        if (dataGPS[i] == ',')
                        {
                            commaCounter++;
                            continue;
                        }

                        if (commaCounter == 5 || commaCounter == 6)
                        {
                            gpsLong += dataGPS[i];
                        }
                        else if (commaCounter > 6)
                        {
                            break;
                        }
                    }

                    commaCounter = 0;
                    string gpsLat = "";
                    for (int i = 0; i < dataGPS.Length; i++)
                    {
                        if (dataGPS[i] == ',')
                        {
                            commaCounter++;
                            continue;
                        }

                        if (commaCounter == 3 || commaCounter == 4)
                        {
                            gpsLat += dataGPS[i];
                        }
                        else if (commaCounter > 4)
                        {
                            break;
                        }
                    }
                    if (gpsLat.Length <= 6 && gpsLong.Length <= 7)
                    {
                        throw new Exception("gps exception");
                    }
                    longitude = double.Parse(gpsLong.Substring(0, 3)) + (double.Parse(gpsLong.Substring(3, 2)) / 60) + (double.Parse(gpsLong.Substring(6, 2)) / 3600);
                    latitude = double.Parse(gpsLat.Substring(0, 2)) + (double.Parse(gpsLat.Substring(2, 2)) / 60) + (double.Parse(gpsLat.Substring(5, 2)) / 3600);
                }
                else
                {
                    throw new Exception("dataGPS is null");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        
        private void OnNewFrameReceived(System.Drawing.Image image)
        {
            Bitmap bitmap = new Bitmap(image);
            this.Dispatcher.Invoke(new Action(
                delegate ()
                {
                    camera.Source = bitmap.ToBitmapImage();
                }));                      
        }

        private void ReceiveRepeating()
        {
            string ip = "127.0.0.1";
            var port = 4242;
            try
            {
                var ipAddress = IPAddress.Parse(ipString: ip);


                var endPoint = new IPEndPoint(address: ipAddress, port: port);
                var client = new Socket(
                    addressFamily: AddressFamily.InterNetwork,
                    socketType: SocketType.Stream,
                    protocolType: ProtocolType.Tcp
                );

                client.Connect(remoteEP: endPoint);

                while (true)
                {
                    try
                    {
                        var rawLength = new byte[sizeof(int)];
                        client.Receive(buffer: rawLength);

                        var length = BitConverter.ToInt32(rawLength);
                        var rawImage = new byte[length];
                        client.Receive(buffer: rawImage);

                        var stream = new MemoryStream(buffer: rawImage);
                        var image = System.Drawing.Image.FromStream(stream: stream);

                        NewFrameReceived.Invoke(obj: image);
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }

        }


        string data;
        public int x;
        public int y;
        private int Range(int x, int in_min, int in_max, int out_min, int out_max)
        {
            return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }

        private void mySerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e) // Edit when hardware is available!
        {
            data = mySerialPort.ReadLine(); // data хранит информацию о данных ком порта

            var mqttFactory = new MqttFactory();
            var client = mqttFactory.CreateMqttClient(); // Создание клиента mqtt
            var options = new MqttClientOptionsBuilder() // Параметры которые использует клиент для подключения к брокеру
                                                .WithClientId(Guid.NewGuid().ToString())
                                                .WithTcpServer("broker.hivemq.com", 1883) // Подключение к брокеру mqtt
                                                .WithCleanSession()
                                                .Build();

            Dispatcher.BeginInvoke(new ThreadStart(async delegate
            {
                try
                {
                    string[] arrData = data.Replace(";", "").Replace("\r", "").Split('|'); // массив данных

                    x = Range(int.Parse(arrData[0]), -100, 100, 0, 190);
                    y = Range(int.Parse(arrData[1]), -100, 100, 0, 190);

                    informationalConsole.Text = data;
                    System.Drawing.Point point = new System.Drawing.Point(x, y);

                    joystick.SetValue(Canvas.TopProperty, (Double)point.Y / 1.2); // Отображение джойстика в интерфейсе
                    joystick.SetValue(Canvas.LeftProperty, (Double)point.X / 1.2);

                    indicatorJoystick.Fill = System.Windows.Media.Brushes.LimeGreen;


                    client.UseConnectedHandler(e => // Событие при подключении к брокеру
                    {
                        informationalConsole.Text += "Connected to the broker successfully"; // Сообщение об успешном подключении к брокеру
                    });
                    client.UseDisconnectedHandler(e => // Событие при отключении от брокера
                    {
                        informationalConsole.Text += "Disconnected from the broker successfully"; // Сообщение об успешном отключении от брокера
                    });

                    await client.ConnectAsync(options); // Установка соеденения с брокером mqtt
                    await PublishMessageAsync(client, "illantal/commands", data); // Отправка данных брокеру
                    await client.DisconnectAsync();
                }
                catch
                {
                    indicatorJoystick.Fill = System.Windows.Media.Brushes.Firebrick;
                }
            }));
        }

        private static async Task PublishMessageAsync(IMqttClient client, string topic, string payloadMessage) // Метод отправки данных брокеру
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic) // Тема
                .WithPayload(payloadMessage) // Сообщение которое отправляется брокеру
                .WithAtLeastOnceQoS()
                .Build();

            if (client.IsConnected)
            {
                await client.PublishAsync(message);
            }
        }
    }
}