using System.Net;
using System.Net.Sockets;


// ReSharper disable ConvertToConstant.Local
// ReSharper disable LocalizableElement


namespace Ipai.UnderwaterDepthResearch.Communication.VideoReceiver;
public partial class Form1 : Form 
{
	private event Action<Image> NewFrameReceived;

	public Form1() 
	{
		InitializeComponent();
		base.Text = "Ipai.UnderwaterDepthResearch.Communication.VideoReceiver";
		this.NewFrameReceived = null!;
	}

	private void Form1_Load(object sender, EventArgs e) 
	{
		NewFrameReceived += OnNewFrameReceived;
		new Thread(ReceiveRepeating).Start();
	}

	private void OnNewFrameReceived(Image image) 
	{
		pictureBox1.Image = image;
	}

	private void ReceiveRepeating() 
	{
		var ipAddress = IPAddress.Parse(ipString: "192.168.1.2");
		var port = 4242;

		var endPoint = new IPEndPoint(address: ipAddress, port: port);
		var client = new Socket(
			addressFamily: AddressFamily.InterNetwork,
			socketType: SocketType.Stream,
			protocolType: ProtocolType.Tcp
		);

		client.Connect(remoteEP: endPoint);

		while(true) 
		{
			try 
			{
				var rawLength = new byte[sizeof(int)];
				client.Receive(buffer: rawLength);

				var length = BitConverter.ToInt32(rawLength);
				var rawImage = new byte[length];
				client.Receive(buffer: rawImage);

				var stream = new MemoryStream(buffer: rawImage);
				var image = Image.FromStream(stream: stream);

				NewFrameReceived.Invoke(obj: image);
			}
			catch(Exception) 
			{
				// ignored
			}
		}

		// ReSharper disable once FunctionNeverReturns
	}
}