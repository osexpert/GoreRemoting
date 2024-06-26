using System;
using System.IO;
using System.Threading.Tasks;
using GoreRemoting;
using GoreRemoting.Serialization.MessagePack;

namespace ClientShared
{
	public class ClientTest
	{
		public void Test(ITestService testServ)
		{

			//            testServ.OtherFormatter();

			Task.Run(() =>
			{
				testServ.GetMessages(m =>
				{
					Console.WriteLine("Message from server: " + m);
				});

				Console.WriteLine("Returned from GetMessages");
			});

			testServ.TestProgress(pro =>
			{
				Console.WriteLine("Progress: " + pro);
			}, async lol =>
			{
				Console.WriteLine("eye");
				return "eye";
			});




			Console.WriteLine("Enter 'CGM' to stop recieving messages");

			int i = 0;

			while (true)//i++ < 300)
			{
				var res = testServ.Echo("lol42");
				Console.WriteLine(res + "I: " + i++);
			}

			while (true)
			{
				Console.WriteLine("Write a line");
				var line = Console.ReadLine();
				var res = testServ.Echo(line);

				if (line == "CGM")
				{
					testServ.CompleteGetMessages();
					Console.WriteLine("Will not recieve any more messages");
				}

				Console.WriteLine($"Echo {i++} res: " + res);

				testServ.SendMessage("Send mess: " + line);
			}

			// Currently unreachable, change the while(true) -> while(false) to enable the test (and maybe use your own file:-)

			//Console.WriteLine(" press a key");
			//Console.ReadKey();


			Console.WriteLine("Send file to server test");
			var now = DateTime.Now;
			using (var f = File.OpenRead(@"e:\ubuntu-20.04.2.0-desktop-amd64.iso"))
			{
				byte[] bytes = null;// new byte[1024 * 1024];
				testServ.SendFile(@"e:\ubuntu-20.04.2.0-desktop-amd64 WRITTEN BY SERVER.iso", (buffLen_constant) =>
				{
					if (bytes == null)
						bytes = new byte[buffLen_constant];

					//                    if (bytes == null || bytes.Length < len)
					//                      bytes = new byte[len];

					//					Array.Resize(ref bytes, 1024 * 1024);

					var r = f.Read(bytes, 0, buffLen_constant);
					if (r == 0)
						throw new StreamingDoneException();

					//        Array.Resize(ref bytes, r);

					return (bytes, r);

				}, p => Console.WriteLine("Progress:" + p));
			}
			Console.WriteLine("Time used: " + (DateTime.Now - now));



			now = DateTime.Now;
			Console.WriteLine("Get file from server test");
			using (var f = File.OpenWrite(@"e:\ubuntu-20.04.2.0-desktop-amd64 WRITTEN BY CLIENT.iso"))
			{
				testServ.GetFile(@"e:\ubuntu-20.04.2.0-desktop-amd64.iso", (bytes, off, len) =>
				{
					f.Write(bytes, off, len);

				}, p => Console.WriteLine("Progress:" + p));
			}
			Console.WriteLine("Time used: " + (DateTime.Now - now));


			Console.WriteLine("done. press a key");
			Console.ReadKey();
		}





	}

	public interface ITestService
	{
		void SendMessage(string mess);
		string Echo(string s);
		Task<string> EchoAsync(string s);

		void TestProgress(Action<string> progress, Func<string, Task<string>> echo);
		Task GetMessages(Action<string> message);
		void CompleteGetMessages();
		void GetFile(string file, Action<byte[], int, int> write, Action<string> progress);
		void SendFile(string file, [StreamingFunc] Func<int, (byte[], int)> read, Action<string> progress);

		[Serializer(typeof(MessagePackAdapter))]
		void OtherFormatter();
	}

	public interface IOtherService
	{
		string Get();
	}

}
