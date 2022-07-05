using System.Net;
using System.Net.Sockets;


// ReSharper disable ConvertToConstant.Local


var ipAddress = IPAddress.Parse(ipString: "<SERVER_IP_ADDRESS>");
var port = 4343;

var endPoint = new IPEndPoint(address: ipAddress, port: port);
var client = new TcpClient();

Console.Write(value: $"Press <Enter> to start sending direction vector... ");
Console.ReadLine();

while(true)
{
	client = new TcpClient();
	Console.Clear();
	Console.Write($"Press one of the [Left], [Right], [Up], [Down] arrows to send direction vector: ");
	var enteredKey = Console.ReadKey().Key;

	var data = enteredKey switch {
		ConsoleKey.LeftArrow  => $"{-50}|{ 0};",
		ConsoleKey.RightArrow => $"{ 50}|{ 0};",
		ConsoleKey.DownArrow  => $"{ 0}|{-50};",
		ConsoleKey.UpArrow    => $"{ 0}|{ 50};",
		//ConsoleKey.Spacebar	  => $"{ 0}|{ 0};",
		_ => $"{0}|{0};"
	};

	/*if(data.Contains($"{0}|{0};"))
	{
		Console.WriteLine($"You've entered a wrong key. Please try again.");
		continue;
	}*/

	var stream = Connect(client, endPoint)!;
	SendData(stream, data);
	Disconnect(client);
}

static NetworkStream? Connect(TcpClient client, IPEndPoint endPoint)
{
	client.Connect(remoteEP: endPoint);
	return client.GetStream();
}
static void Disconnect(TcpClient client)
{
	client.Close();
}
static void SendData(Stream stream, string data)
{
	using var writer = new BinaryWriter(output: stream);
	writer.Write(value: data);
}
