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
		var ipAddress = IPAddress.Parse(ipString: "<SERVER_IP_ADDRESS>");
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

        while(true)
        {
	        var desktopClient = desktopClientSocket.Accept();
	        while(desktopClient.Connected is true)
	        {
		        try
		        {
			        var image = GetImage(receiveServer: ref raspberrySocket);
			        var rawImage = default(byte[]);

			        using (var stream = new MemoryStream()) {
				        image.Save(stream: stream, format: ImageFormat.Jpeg);
				        rawImage = stream.ToArray();
			        }

			        desktopClient.Send(buffer: rawImage);
		        }
		        catch (Exception e)
		        {
			        Console.WriteLine(e);
		        }
	        }
        }
	}
    private static Image GetImage(ref TcpListener receiveServer)
    {
	    try
	    {
		    using var client = receiveServer.AcceptTcpClient();
		    return Image.FromStream(stream: client.GetStream());
	    }
	    catch (Exception e)
	    {
		    Console.WriteLine(e);
	    }

	    return null;
    }
}
internal static class CoordsTransfer
{
    public static void Start()
    {
		var ipAddress = IPAddress.Parse(ipString: "178.159.39.175");
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

        while(true)
        {
	        var raspberryClient = raspberryClientSocket.Accept();
	        Console.WriteLine($"raspberry pi has been connected to server");

	        while(raspberryClient.Connected is true)
	        {
		        try
		        {
			        var coords = GetCoords(receiveServer: ref desktopClientSocket);
			        Console.WriteLine($"received coords: {coords}");

			        var rawCoords = Encoding.UTF8.GetBytes(coords);
			        raspberryClient.Send(buffer: rawCoords);
		        }
		        catch (Exception e)
		        {
			        Console.WriteLine(e);
		        }
	        }
        }
    }
    private static string GetCoords(ref TcpListener receiveServer)
    {
		using var client = receiveServer.AcceptTcpClient();
        using var binaryReader = new BinaryReader(input: client.GetStream());
        return binaryReader.ReadString();
    }
}
