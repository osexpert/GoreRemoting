using GoreRemoting.Serialization.BinaryFormatter;
using GoreRemoting.Tests.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GoreRemoting.Tests
{
	public interface IStreamTest
	{
		//Task StreamToClient(Action<byte[], int, int> write);
		//Task StreamToServer(Func<int, (byte[], int, int)> read);
		Task ServerPushDataToClient(Func<byte[], Task> push);

		Task ServerPullDataFromClient([StreamingFunc]Func<Task<byte[]>> pull);

		Task ServerPullStringDataFromClient(Func<Task<(string, bool)>> pull);
	}



	public class StreamTest : IStreamTest
	{
		public enum enTest
		{
			asd
		}

		public void Tesss(enTest a)
		{

		}
		public void Tesss2(int a)
		{

		}
		public async Task ServerPullDataFromClient(Func<Task<byte[]>> pull)
		{
			List<byte[]> ll = new List<byte[]>();
			while (true)
			{
				try
				{
					var d = await pull();

					ll.Add(d);
				}
				catch (StreamingDoneException)
				{
					break;
				}
			}
		}

		public Task ServerPullStringDataFromClient(Func<Task<(string, bool)>> pull)
		{
			throw new NotImplementedException();
		}

		//public async Task StreamToServer(Func<int, (byte[], int, int)> readFromClient)
		//{
		//	using Stream s = StreamAdapter.Consume(readFromClient);
		//	await s.CopyToAsync(Console.OpenStandardOutput());
		//}

		//public async Task StreamToClient(Action<byte[], int, int> writeToClient)
		//{
		//	using Stream s = StreamAdapter.Produce(writeToClient);
		//	await Console.OpenStandardInput().CopyToAsync(s);
		//}
		public Task ServerPushDataToClient(Func<byte[], Task> push)
		{
			throw new NotImplementedException();
		}
	}

	public class Lolz
	{
		//[Theory]
		//[InlineData(enSerializer.BinaryFormatter)]
		//[InlineData(enSerializer.MemoryPack)]
		//[InlineData(enSerializer.Json)]
		//[InlineData(enSerializer.MessagePack)]
		public async Task StreamTestt(enSerializer ser)
		{
			await using var server = new NativeServer(9198, new ServerConfig(new BinaryFormatterAdapter()));
			server.RegisterService<IStreamTest, StreamTest>();
			server.Start();

			await using var client = new NativeClient(9198, new ClientConfig(new BinaryFormatterAdapter()));
			
			var proxy = client.CreateProxy<IStreamTest>();

			int i = 0;
			await proxy.ServerPullDataFromClient(async () =>
			{

				i++;

				//				if (i == 10)
				//				throw new StreamingFuncStopException();

				if (i == 10)
					throw new StreamingDoneException();

				return new byte[] { 1, 2, 4 };// new StreamingDone(i == 10));

			});

			// data from server. but can we do the same to server?
			//await foreach (var i in AsyncEnumerableAdapter.Consume<string>(bb => proxy.ReadFromServer(x => bb(x))))
			//{
			//	//i2.Add(i);

			//	//Console.WriteLine(i);
			//	//OutputDebugString(i);
			//	//Debug.WriteLine(i);
			//}

			// TODO: dette kan være være internt? Mao AsyncEnumerableAdapter brukes inni ReadAsyncEnumerableStream?
			var rs = new ReadAsyncEnumerableStream(AsyncEnumerableAdapter.ClientConsume<byte[]>(bb => proxy.ServerPushDataToClient(x => bb(x))));


			await AsyncEnumerableAdapter.ClientProduce<string>(GetSomeStrings(), lol => proxy.ServerPullStringDataFromClient(() => lol()));


			var fileReadS = File.OpenRead("");

	//		await AsyncEnumerableAdapter.ClientProduce<byte[]>(GetDataFromFileStream(fileReadS), lol => proxy.ServerPullDataFromClient(() => lol()));

//			await StreamAdapter.ClientProduce(fileReadS, lol => proxy.ServerPullDataFromClient(() => lol()));
			//var writeTo = new WriteStreaam(cprod);
			// server kommer og gir klient data
			//Stream readstreamm = StreamAdapter.ClientConsume(bb => proxy.ServerPushDataToClient(x => bb(x)));




			// server kommer og spør klient om data



			//AsyncEnumerableAdapter.Produce<byte[]>(null, x => proxy.ServerPullDataFromClient(x));

			//Stream writeStream = StreamAdapter.ClientProduce(lol => proxy.ServerPullDataFromClient(() => lol()));

			//StreamAdapter.ToWriteStream(proxy.

			//using Stream s = StreamAdapter.Produce43(a => proxy.StreamToServer(a));
			//proxy.StreamToServer(StreamAdapter.)

			//await proxy.StreamToServer(a =>
			//{

			//});

			//await Console.OpenStandardInput().CopyToAsync(s);
			//await proxy.StreamToServer(StreamAdapter.Produce43)
		}

		private async IAsyncEnumerable<byte[]> GetDataFromFileStream(Stream fileReadS)
		{
			byte[] buff = new byte[0x1024];
			int i = 0;
			while ((i = await fileReadS.ReadAsync(buff, 0, buff.Length)) > 0)
			{
				if (buff.Length != i)
					Array.Resize(ref buff, i);
				yield return buff;
			}
		}

		private async static IAsyncEnumerable<string> GetSomeStrings()
		{
			yield return "a1";
			yield return "ss";
			yield return "ll";
		}

		public class StreamAdapter
		{
			public static Stream ClientConsume(Func<Func<byte[], Task>, Task> dataSource)
			{
				return new ReadAsyncEnumerableStream(AsyncEnumerableAdapter.ClientConsume<byte[]>(dataSource));
			}

			public static Task ClientProduce(Stream fileReadS, Func<Func<Task<(byte[], bool)>>, Task> dataProvider)
			{
				return AsyncEnumerableAdapter.ClientProduce<byte[]>(GetDataFromFileStream(fileReadS), dataProvider);
			}

			private static async IAsyncEnumerable<byte[]> GetDataFromFileStream(Stream fileReadS)
			{
				byte[] buff = new byte[81920];
				int i = 0;
				while ((i = await fileReadS.ReadAsync(buff, 0, buff.Length)) > 0)
				{
					if (buff.Length != i)
						Array.Resize(ref buff, i); // stream via IAsyncEnumerable er lite ideellt....
					// DET ER BEST HVIS SERVER SPØR OM DATA OG LENGDE!
					yield return buff;
				}
			}

			//internal static Stream ClientProduce(Func<Func<Task<(byte[], bool)>>, Task> dataProvider)
			//{
			//	return new WriteStreaam(dataProvider);
			//}

			//internal static Task<Stream> ClientProduce2(Func<Func<Task<byte[]>>, Task> dataProvider)
			//{
			//	return AsyncEnumerableAdapter.ClientProduce_ReturnWritableStream<byte[]>(dataProvider);
			//}
		}

		class WriteStreaam : Stream
		{
			Func<Func<Task<(byte[], bool)>>, Task> _writer;

			public WriteStreaam(Func<Func<Task<(byte[], bool)>>, Task> dataProvider)
			{
				_writer = dataProvider;

				//_writer(() => Task.FromResult(new byte[] { }));
			}

			public WriteStreaam(Task cprod)
			{
			}

			public override bool CanRead => throw new NotImplementedException();

			public override bool CanSeek => throw new NotImplementedException();

			public override bool CanWrite => throw new NotImplementedException();

			public override long Length => throw new NotImplementedException();

			public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

			public override void Flush()
			{
				throw new NotImplementedException();
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				throw new NotImplementedException();
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				throw new NotImplementedException();
			}

			public override void SetLength(long value)
			{
				throw new NotImplementedException();
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				
			}
		}

		public class ReadAsyncEnumerableStream : Stream
		{
			IAsyncEnumerator<byte[]> _asyncEnumerable;

			public ReadAsyncEnumerableStream(IAsyncEnumerable<byte[]> asyncEnumerable)
			{
				_asyncEnumerable = asyncEnumerable.GetAsyncEnumerator();
			}

			public override bool CanRead => throw new NotImplementedException();

			public override bool CanSeek => throw new NotImplementedException();

			public override bool CanWrite => throw new NotImplementedException();

			public override long Length => throw new NotImplementedException();

			public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

			public override void Flush()
			{
				throw new NotImplementedException();
			}

			byte[] _bytes;
			int _readPos;

			// TODO: where is complete??

			public override int Read(byte[] buffer, int offset, int count)
			{
				if (count <= 0)
					throw new ArgumentException("count <= 0");
				if (offset < 0)
					throw new ArgumentException("offset < 0");

				if (_bytes != null && _bytes.Length > 0)// && _readPos < _bytes.Length - 1)
				{
					int bytesLeft = _bytes.Length - _readPos - 1;
					if (bytesLeft > 0)
					{
						int bytesToCopy = Math.Min(count, bytesLeft);
						Buffer.BlockCopy(_bytes, _readPos, buffer, offset, bytesToCopy);
						_readPos += bytesToCopy;
						if (_readPos > _bytes.Length - 1)
							throw new Exception("readPos is past buffer end");
						return bytesToCopy;
					}
				}

				// If we get here _bytes is empty or has _bytes but _readPos is at end
				var assert = _bytes == null || _bytes.Length == 0 || _readPos == _bytes.Length - 1;
				if (!assert)
					throw new Exception("bad");

				_bytes = null;
				_readPos = 0;

				{
					if (!_asyncEnumerable.MoveNextAsync().GetAwaiter().GetResult())
					{
						return 0;
					}

					var b = _asyncEnumerable.Current;
					int bytesToCopy = Math.Min(count, b.Length);
					Buffer.BlockCopy(b, 0, buffer, offset, bytesToCopy);
					// if anythign left, store for later
					int bytesLeft = b.Length - bytesToCopy;
					if (bytesLeft > 0)
					{
						_bytes = b;
						_readPos = b.Length - bytesLeft;
					}

					return bytesToCopy;
				}
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				throw new NotImplementedException();
			}

			public override void SetLength(long value)
			{
				throw new NotImplementedException();
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				throw new NotImplementedException();
			}
		}
	}
}