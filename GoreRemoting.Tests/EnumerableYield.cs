using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GoreRemoting.Tests.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoreRemoting.Tests;

[TestClass]
public class EnumerableYield
{
	public interface IIenumera
	{
		IEnumerable<string> NonJild();

		IEnumerable<string> Jild();
		IAsyncEnumerable<string> Jild2();

		IEnumerable<Task<string>> Jild3();

		Task Jild4(Func<string, Task> outt, int t);

		Tuple<string, int> RetCom1();
		(string, int) RetCom2();
		ValueTuple<string, int> RetCom3();

		Task<Tuple<string, int>> RetACom1();
		Task<(string, int)> RetACom2();
		Task<ValueTuple<string, int>> RetACom3();


		//Task<IAsyncEnumerable<string>> Jild3();
		//Task<IEnumerable<string>> Jild4();


		Task TestCancel(Func<string, Task> outt, CancellationToken cancel);
		Task TestCancel2(CancellationToken c1, CancellationToken cancel);

		Task TestProg(Action<int> p);


		void TextNonserEx();
		void TextNonserEx2();
	}



	public class EnumeTest : IIenumera
	{
		public IEnumerable<string> NonJild()
		{
			return new[] { "1", "2" };
		}

		public IEnumerable<string> Jild()
		{
			yield return "1";
			yield return "2";
		}

		public async IAsyncEnumerable<string> Jild2()
		{
			await Task.CompletedTask;
			yield return "1";
			yield return "2";
		}



		public Task Jild4(Func<string, Task> outt, int t)
		{
			//return Jild3Int().ViaFuncAsync(outt);

			return AsyncEnumerableAdapter.Produce(Jild3Int(t), outt);
			//				return AsyncEnumerableAdapter.Produce(cancel => Jild3Int(t, cancel), outt, new CancellationToken());
		}

		private async IAsyncEnumerable<string> Jild3Int(int x)
		{
			await Task.CompletedTask;
			yield return "1";
			yield return "2";

			//while (true)
			//{
			//	yield return Random.Shared.Next().ToString();// "2";

			//	await Task.Delay(1000);
			//}

		}

		private async IAsyncEnumerable<string> Jild3Int(int x, [EnumeratorCancellation]CancellationToken cancel)
		{
			await Task.CompletedTask;
			yield return "1";
			yield return "2";

			//while (true)
			//{
			//	yield return Random.Shared.Next().ToString();// "2";

			//	await Task.Delay(1000);
			//}

		}


		public async Task<Tuple<string, int>> RetACom1()
		{
			await Task.CompletedTask;
			return new Tuple<string, int>("1", 2);
		}

		public async Task<(string, int)> RetACom2()
		{
			await Task.CompletedTask;
			return ("1", 2);
		}

		public async Task<(string, int)> RetACom3()
		{
			await Task.CompletedTask;
			return ("1", 2);
		}

		public Tuple<string, int> RetCom1()
		{
			return new Tuple<string, int>("1", 2);
		}

		public (string, int) RetCom2()
		{
			return ("1", 2);
		}

		public (string, int) RetCom3()
		{
			return ("1", 2);
		}

		public IEnumerable<Task<string>> Jild3()
		{
			throw new NotImplementedException();
		}

		static Random r = new Random();

		public async Task TestCancel(Func<string, Task> outt, CancellationToken cancel)
		{
			while (true)
			{
				await outt(r.Next().ToString());
				await outt(r.Next().ToString());
				await outt(r.Next().ToString());
				await Task.Delay(1000000, cancel);
			}
		}

		public Task TestCancel2(CancellationToken c1, CancellationToken cancel)
		{
			throw new NotImplementedException();
		}

		public async Task TestProg(Action<int> pReport)
		{
			await Task.CompletedTask;
			ProTe(ProgressAdapter.ServerProduce<int>(pReport));
		}

		private void ProTe(IProgress<int> pReport)
		{
			pReport.Report(1);
			pReport.Report(42);
		}

		public void TextNonserEx()
		{
			var e = new NonoEx();
			throw e;
		}

		public void TextNonserEx2()
		{
			var e = new NonoEx2(null, "mess");
			throw e;
		}
	}

	class NonoEx : Exception
	{

	}

	class NonoEx2 : Exception
	{
		public NonoEx2(object? t, string mess) : base(mess)
		{

		}
	}



	[TestMethod]
	[DataRow(enSerializer.BinaryFormatter)]
#if NET6_0_OR_GREATER
	[DataRow(enSerializer.MemoryPack)]
#endif
	[DataRow(enSerializer.Json)]
	[DataRow(enSerializer.MessagePack)]
	[DataRow(enSerializer.Protobuf)]
	public async Task YieldTest(enSerializer ser)
	{
		await using var server = new NativeServer(9198, new ServerConfig(Serializers.GetSerializer(ser)));
		server.RegisterService<IIenumera, EnumeTest>();
		server.Start();

		await using var client = new NativeClient(9198, new ClientConfig(Serializers.GetSerializer(ser)));

		var proxy = client.CreateProxy<IIenumera>();

		var n1 = proxy.NonJild().ToList();
		Assert.HasCount(2, n1);
		Assert.AreEqual("1", n1[0]);
		Assert.AreEqual("2", n1[1]);

		//List<string> i1 = new();
		//foreach (var i in proxy.Jild())
		//{
		//	i1.Add(i);
		//}
		//Assert.Equal(2, i1.Count);
		//Assert.Equal("1", i1[0]);
		//Assert.Equal("2", i1[1]);

		//List<string> i2 = new();
		//await foreach (var i in proxy.Jild2())
		//{
		//	i2.Add(i);
		//}
		//Assert.Equal(2, i2.Count);
		//Assert.Equal("1", i2[0]);
		//Assert.Equal("2", i2[1]);



		List<string> i2 = new();

		//await proxy.Jild4(async x => { i2.Add(x); }, 42);
		var res = AsyncEnumerableAdapter.Consume<string>(bb => proxy.Jild4(x => bb(x), 42));
		await foreach (var i in res)
		{
			i2.Add(i);

			//Console.WriteLine(i);
			//OutputDebugString(i);
			//Debug.WriteLine(i);
		}

		Assert.HasCount(2, i2);
		Assert.AreEqual("1", i2[0]);
		Assert.AreEqual("2", i2[1]);



		var r1 = proxy.RetCom1();
		var r2 = proxy.RetCom2();
		var r3 = proxy.RetCom3();
		var ra1 = await proxy.RetACom1();
		var ra2 = await proxy.RetACom2();
		var ra3 = await proxy.RetACom3();
		Assert.IsTrue(r1.Item1 == "1" && r1.Item2 == 2);
		Assert.IsTrue(r2.Item1 == "1" && r2.Item2 == 2);
		Assert.IsTrue(r3.Item1 == "1" && r3.Item2 == 2);
		Assert.IsTrue(ra1.Item1 == "1" && ra1.Item2 == 2);
		Assert.IsTrue(ra2.Item1 == "1" && ra2.Item2 == 2);
		Assert.IsTrue(ra3.Item1 == "1" && ra3.Item2 == 2);


		var t1 = DateTime.Now;
		var fiveSec = new CancellationTokenSource(5000);
		int hit1 = 0;
		bool wasC = false;
		try
		{
			await proxy.TestCancel(async s =>
			{
				await Task.CompletedTask;
				hit1++;

			}, fiveSec.Token);
		}
		catch (TaskCanceledException)
		{
			wasC = true;
		}

		var td = DateTime.Now - t1;
//			Assert.True(td.TotalSeconds < 10);
		Assert.AreEqual(3, hit1);
		Assert.IsTrue(wasC);

		Exception? cee = null;
		try
		{
			await proxy.TestCancel2(CancellationToken.None, CancellationToken.None);
		}
		catch (Exception e)
		{
			cee = e;
		}
		Assert.AreEqual("More than one CancellationToken", cee!.Message);

		List<int> l = new();
		var p = new GoodProgress<int>();
		p.ProCha += (a, b) =>
		{
			l.Add(b);
		};
		await proxy.TestProg(ProgressAdapter.ClientConsume(p));

		//weird hack: ProgressChanged is called on background thread?
		//await Task.Delay(100);

		Assert.HasCount(2, l);
		Assert.AreEqual(43, l.Sum());

		Exception? _ex1 = null;
		try
		{
			proxy.TextNonserEx();
		}
		catch (Exception e)
		{
			_ex1 = e;
		}

		Exception? _ex2 = null;
		try
		{
			proxy.TextNonserEx2();
		}
		catch (Exception e)
		{
			_ex2 = e;
		}

		Assert.AreEqual("Exception of type 'GoreRemoting.Tests.EnumerableYield+NonoEx' was thrown.", _ex1!.Message);
		Assert.AreEqual("mess", _ex2!.Message);

	}

	class GoodProgress<T> : IProgress<T>
	{
		public event EventHandler<T>? ProCha;

		public void Report(T value)
		{
			ProCha?.Invoke(this, value);

		}
	}
}
