using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Sockets;
using System.Text;


// ReSharper disable ConvertToConstant.Local
[assembly: SuppressMessage(category: "Interoperability", checkId: "CA1416:Проверка совместимости платформы", Justification = "<Ожидание>")]


var videoTransferThread  = new Thread(VideoTransfer.Start);
var coordsTransferThread = new Thread(CoordsTransfer.Start);

videoTransferThread.Start();
coordsTransferThread.Start();

internal static class VideoTransfer  
{
    public static void Start() 
	{
		var ipAddress = IPAddress.Parse(ipString: "192.168.0.131");
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
        while(desktopClient.Connected is true) 
        {
            var image = GetImage(receiveServer: ref raspberrySocket);
            var rawImage = default(byte[]);

            using (var stream = new MemoryStream()) {
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
internal static class CoordsTransfer 
{
    public static void Start() 
    {
		var ipAddress = IPAddress.Parse(ipString: "192.168.0.131");
		var serverCoordsReceiverEndPointPort = 4343;
		var raspberryCoordsSenderEndPointPort = 4444;

        var serverCoordsReceiverEndPoint = new IPEndPoint(address: ipAddress, port: serverCoordsReceiverEndPointPort);
        var raspberryCoordsSenderEndPoint = new IPEndPoint(address: ipAddress, port: raspberryCoordsSenderEndPointPort);

        var desktopClientSocket = new TcpListener(
			localEP: serverCoordsReceiverEndPoint
		);

		var raspberryClientSocket = new Socket(
			addressFamily: AddressFamily.InterNetwork,
			socketType: SocketType.Stream,
			protocolType: ProtocolType.Tcp
		);

        desktopClientSocket.Start();

        raspberryClientSocket.Bind(localEP: raspberryCoordsSenderEndPoint);
        raspberryClientSocket.Listen(backlog: 1);

        var raspberryClient = raspberryClientSocket.Accept();
        while(raspberryClient.Connected is true) 
        {
            var coords = GetCoords(receiveServer: ref desktopClientSocket);
            var rawCoords = Encoding.UTF8.GetBytes(coords);
			raspberryClient.Send(buffer: rawCoords);
		}
    }
    private static string GetCoords(ref TcpListener receiveServer) 
    {
		using var client = receiveServer.AcceptTcpClient();
        using var binaryReader = new BinaryReader(input: client.GetStream());
        return binaryReader.ReadString();
    }
}