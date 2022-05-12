namespace Ipai.UnderwaterDepthResearch.Communication.VideoReceiver;
internal static class Program 
{
	[STAThread] private static void Main() 
	{
		ApplicationConfiguration.Initialize();
		Application.Run(new Form1());
	}
}