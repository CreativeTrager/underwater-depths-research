using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Sockets;

namespace Ipai.UnderwaterDepthResearch.Clients.EndClient.DroneOperator
{
    internal static class VideoTransfer
    {
        public static void Start()
        {
            var ipAddress = IPAddress.Parse(ipString: "127.0.0.1");
            var serverVideoSenderEndPointPort = 4242;
            var raspberryVideoReceiveEndPointPort = 4141;

            var serverVideoSenderEndPoint = new IPEndPoint(address: ipAddress, port: serverVideoSenderEndPointPort);
            var raspberryVideoReceiveEndPoint = new IPEndPoint(address: ipAddress, port: raspberryVideoReceiveEndPointPort);

            var desktopClientSocket = new Socket(
                addressFamily: AddressFamily.InterNetwork,
                socketType: SocketType.Stream,
                protocolType: ProtocolType.Tcp
            );

            var raspberrySocket = new TcpListener(
                localEP: raspberryVideoReceiveEndPoint
            );

            raspberrySocket.Start();            

            desktopClientSocket.Bind(localEP: serverVideoSenderEndPoint);
            desktopClientSocket.Listen(backlog: 1);

            var desktopClient = desktopClientSocket.Accept();
            while (desktopClient.Connected is true)
            {
                var image = GetImage(receiveServer: ref raspberrySocket);
                var rawImage = default(byte[]);

                using (var stream = new System.IO.MemoryStream())
                {
                    image.Save(stream: stream, format: ImageFormat.Jpeg);
                    rawImage = stream.ToArray();
                }

                desktopClient.Send(buffer: BitConverter
                    .GetBytes(rawImage.Length)
                    .Concat(rawImage)
                    .ToArray()
                );
            }
        }
        private static Image GetImage(ref TcpListener receiveServer)
        {
            using var client = receiveServer.AcceptTcpClient();
            return Image.FromStream(stream: client.GetStream());
        }
    }
}
