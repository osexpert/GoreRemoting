using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using GoreRemoting.Serialization;
using GoreRemoting.Serialization.BinaryFormatter;
using GoreRemoting.Serialization.Json;
#if NET6_0_OR_GREATER
using GoreRemoting.Serialization.MemoryPack;
#endif
using GoreRemoting.Serialization.MessagePack;
using GoreRemoting.Serialization.Protobuf;
using GoreRemoting.Tests.ExternalTypes;
using GoreRemoting.Tests.Tools;


namespace GoreRemoting.Tests;

[TestClass]
public class AsyncTests
{
	#region Service with async method

	public interface IAsyncService
	{
		Task<string> ConvertToBase64Async(string text);

		Task NonGenericTask();

		(Version?, Version, Version[], Version[]?) TestMisc(Version? v, Version v2, Version[] versions, Version[]? versions2);
	}

	public class AsyncService : IAsyncService
	{
		public async Task<string> ConvertToBase64Async(string text)
		{
			var convertFunc = new Func<string>(() =>
			{
				var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
				return Convert.ToBase64String(stream.ToArray());
			});

			var base64String = await Task.Run(convertFunc);

			return base64String;
		}

		public Task NonGenericTask()
		{
			return Task.CompletedTask;
		}

		public (Version?, Version, Version[], Version[]?) TestMisc(Version? v, Version vs, Version[] versions, Version[]? versions2)
		{
			return (v, vs, versions, versions2);
		}
	}

	#endregion



	[TestMethod]
	[DataRow(Serializer.BinaryFormatter)]
	[DataRow(Serializer.Json)]
#if NET6_0_OR_GREATER
	[DataRow(Serializer.MemoryPack)]
#endif
	[DataRow(Serializer.MessagePack)]
	[DataRow(Serializer.Protobuf)]
	public async Task AsyncMethods_should_work(Serializer ser)
	{
		var serverConfig =
			new ServerConfig(Serializers.GetSerializer(ser));

		var port = Ports.GetNext();
		await using var server = new NativeServer(port, serverConfig);
		server.RegisterService<IAsyncService, AsyncService>();
		server.Start();

		await using var client = new NativeClient(port, new ClientConfig(Serializers.GetSerializer(ser)));

		var proxy = client.CreateProxy<IAsyncService>();

		var base64String = await proxy.ConvertToBase64Async("Yay");

		Assert.AreEqual("WWF5", base64String);

		var res = proxy.TestMisc(null, new Version(1, 42), new[] { new Version(1, 2), new Version(2, 3, 5) }, null);
		Assert.IsNull(res.Item1);
		Assert.IsNull(res.Item4);
		Assert.AreEqual(new Version(1, 42), res.Item2);
		Assert.HasCount(2, res.Item3);
		Assert.AreEqual(new Version(1, 2), res.Item3[0]);
		Assert.AreEqual(new Version(2, 3, 5), res.Item3[1]);
	}

	/// <summary>
	/// Awaiting for ordinary non-generic task method should not hangs. 
	/// </summary>
	//[Fact(Timeout = 15000)]
	[TestMethod()]//Timeout = 15000)]
	[DataRow(Serializer.BinaryFormatter)]
#if NET6_0_OR_GREATER
	[DataRow(Serializer.MemoryPack)]
#endif
	[DataRow(Serializer.Json)]
	[DataRow(Serializer.MessagePack)]
	[DataRow(Serializer.Protobuf)]
	public async Task AwaitingNonGenericTask_should_not_hang_forever(Serializer ser)
	{
		var serverConfig =
			new ServerConfig(Serializers.GetSerializer(ser));

		var port = Ports.GetNext();
		await using var server = new NativeServer(port, serverConfig);
		server.RegisterService<IAsyncService, AsyncService>();
		server.Start();

		await using var client = new NativeClient(port, new ClientConfig(Serializers.GetSerializer(ser)));

		var proxy = client.CreateProxy<IAsyncService>();

		await proxy.NonGenericTask();
	}

	public interface IExceptionTest
	{
		void TestSerializedExMistakeAndPrivate();
		void TestSerializedExMistake();
		void TestSerializedOk();

		void TestWithInnerException();
		void TestWithSqlException();
	}

	class ExceptionTest : IExceptionTest
	{
		public void TestSerializedExMistake()
		{
			throw new SerExMistakeNotPriv("The mess", "extra string");
		}

		public void TestSerializedExMistakeAndPrivate()
		{
			throw new SerExMistake("The mess", "extra string");
		}

		public void TestSerializedOk()
		{
			throw new SerExOk("The mess", "extra string");
		}

		public void TestWithInnerException()
		{
			var e = SqlExceptionTest.CreateException(new SqlError(41, 1, 1, "ser", "error", "proc", 42), "serv3", new SqlInternalConnectionTds(), new IOException("io exc"));

			throw new SerExOk("The mess", "extra string", e);
		}

		public void TestWithSqlException()
		{
			var e = SqlExceptionTest.CreateException(new SqlError(41, 1, 1, "ser", "error", "proc", 42), "serv3", new SqlInternalConnectionTds(), new IOException("io exc"));
			throw e;
		}
	}

	/// <summary>
	/// json can not find the type (private?). fails over to Exception..
	/// same with messpack? fails over to Exception..
	/// also binaryformatter withh throw bcause cant find property "Test"
	/// 
	/// the failover type here is not stable\consistent...
	/// 
	/// i would have liked:
	/// TheRealException
	/// SomeRemoteException with type and stack and serinfo in it
	/// SomeRemoteException with type and stack (assuming if serinfo fails to desser)
	/// 
	/// Mao: only 1 other exception to care about
	/// </summary>
	[Serializable]
	class SerExMistake : Exception
	{
		public string Test { get; }

		public SerExMistake(string theMess, string t) : base(theMess)
		{
			Test = t;
			Data.Add("teste", "foobar");
		}

		[Obsolete]
		public SerExMistake(SerializationInfo si, StreamingContext sc) : base(si, sc)
		{
			// Mistake: should be "test"
			Test = si.GetString("Test")!;
		}

#if NET8_0_OR_GREATER
		[Obsolete]
#endif
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("test", Test);
		}
	}

	[Serializable]
	public class SerExMistakeNotPriv : Exception
	{
		public string Test { get; }

		public SerExMistakeNotPriv(string theMess, string t) : base(theMess)
		{
			Test = t;
			Data.Add("teste", "foobar");
		}

		[Obsolete]
		public SerExMistakeNotPriv(SerializationInfo si, StreamingContext sc) : base(si, sc)
		{
			// Mistake: should be "test"
			Test = si.GetString("Test")!;
		}

#if NET8_0_OR_GREATER
		[Obsolete]
#endif
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("test", Test);
		}
	}

	[Serializable]
	public class SerExOk : Exception
	{
		public string Test { get; }

		public SerExOk(string theMess, string t) : base(theMess)
		{
			Test = t;
			Data.Add("teste", "foobar");
		}

		public SerExOk(string theMess, string t, Exception ie) : base(theMess, ie)
		{
			Test = t;
			Data.Add("teste", "foobar");
		}


		[Obsolete]
		public SerExOk(SerializationInfo si, StreamingContext sc) : base(si, sc)
		{
			Test = si.GetString("test")!;
		}

#if NET8_0_OR_GREATER
		[Obsolete]
#endif
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("test", Test);
		}
	}


	[TestMethod]
	[DataRow(Serializer.BinaryFormatter)]
	[DataRow(Serializer.Json)]
#if NET6_0_OR_GREATER
	[DataRow(Serializer.MemoryPack)]
#endif
	[DataRow(Serializer.MessagePack)]
	[DataRow(Serializer.Protobuf)]
	public async Task ExceptionTests(Serializer ser)
	{
		var serverConfig = new ServerConfig(Serializers.GetSerializer(ser));

		var port = Ports.GetNext();
		await using var server = new NativeServer(port, serverConfig);
		server.RegisterService<IExceptionTest, ExceptionTest>();
		server.Start();

		await using var client = new NativeClient(port, new ClientConfig(Serializers.GetSerializer(ser)));

		var proxy = client.CreateProxy<IExceptionTest>();


		Exception e;
		if (ser == Serializer.BinaryFormatter)
		{
			e = Assert.ThrowsExactly<TaskCanceledException>(proxy.TestSerializedExMistakeAndPrivate);
			Assert.AreEqual("A task was canceled.", e.Message);
			Assert.IsNull(e.InnerException);

			AssertLines(e,
			[
				"System.Threading.Tasks.TaskCanceledException: A task was canceled.",
				"   at Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsThrowsFailing[TException](Action action, Boolean isStrictType, String assertMethodName)"
			]);
		}
		else
		{
			e = Assert.ThrowsExactly<TargetInvocationException>(proxy.TestSerializedExMistakeAndPrivate);
			Assert.AreEqual("Exception has been thrown by the target of an invocation.", e.Message);
			Assert.AreEqual("Member 'Test' was not found.", e.InnerException!.Message);

			AssertLines(e,
			[
				"System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocation.",
				" ---> System.Runtime.Serialization.SerializationException: Member 'Test' was not found.",
				"   --- End of inner exception stack trace ---",
				"   at Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsThrowsFailing[TException](Action action, Boolean isStrictType, String assertMethodName)"
			]);
		}

		Exception e2;
		if (ser == Serializer.BinaryFormatter)
		{
			e2 = Assert.ThrowsExactly<TaskCanceledException>(proxy.TestSerializedExMistake);

			Assert.AreEqual("A task was canceled.", e2.Message);
			Assert.IsNull(e2.InnerException);

			AssertLines(e,
			[
				"System.Threading.Tasks.TaskCanceledException: A task was canceled.",
				"   at Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsThrowsFailing[TException](Action action, Boolean isStrictType, String assertMethodName)"
			]);
		}
		else
		{
			e2 = Assert.ThrowsExactly<TargetInvocationException>(proxy.TestSerializedExMistake);
			Assert.AreEqual("Exception has been thrown by the target of an invocation.", e2.Message);
			Assert.AreEqual("Member 'Test' was not found.", e2.InnerException!.Message);

			AssertLines(e2,
			[
				"System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocation.",
				" ---> System.Runtime.Serialization.SerializationException: Member 'Test' was not found.",
				"   --- End of inner exception stack trace ---",
				"   at Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsThrowsFailing[TException](Action action, Boolean isStrictType, String assertMethodName)"
			]);
		}

		var e3 = Assert.ThrowsExactly<SerExOk>(proxy.TestSerializedOk);

		Assert.AreEqual("The mess", e3.Message);
		Assert.IsNull(e3.InnerException);

		AssertLines(e3,
		[
			"GoreRemoting.Tests.AsyncTests+SerExOk: The mess",
			"--- End of stack trace from previous location ---",
			"   at Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsThrowsFailing[TException](Action action, Boolean isStrictType, String assertMethodName)"
		]);
	}

	[TestMethod]
	[DataRow(Serializer.BinaryFormatter)]
	[DataRow(Serializer.Json)]
#if NET6_0_OR_GREATER
	[DataRow(Serializer.MemoryPack)]
#endif
	[DataRow(Serializer.MessagePack)]
	[DataRow(Serializer.Protobuf)]
	public async Task ExceptionTests_InnerEx(Serializer ser)
	{
		var serverConfig = new ServerConfig(Serializers.GetSerializer(ser));

		var port = Ports.GetNext();
		await using var server = new NativeServer(port, serverConfig);
		server.RegisterService<IExceptionTest, ExceptionTest>();
		server.Start();

		await using var client = new NativeClient(port, new ClientConfig(Serializers.GetSerializer(ser)));

		var proxy = client.CreateProxy<IExceptionTest>();

		var e4 = Assert.ThrowsExactly<SerExOk>(proxy.TestWithInnerException);

		Assert.AreEqual("The mess", e4.Message);
		if (ser == Serializer.BinaryFormatter)
		{
			Assert.AreEqual("error", e4.InnerException!.Message);

#if NETFRAMEWORK
			AssertLines(e4, [
				"GoreRemoting.Tests.AsyncTests+SerExOk: The mess",
				" ---> GoreRemoting.Tests.ExternalTypes.SqlExceptionTest: error",
				" ---> System.IO.IOException: io exc",
				"   --- End of inner exception stack trace ---",
				"   --- End of inner exception stack trace ---",
				"--- End of stack trace from previous location ---",
				"   at Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsThrowsFailing[TException](Action action, Boolean isStrictType, String assertMethodName)",
			]);
#else
			AssertLines(e4, [
				"GoreRemoting.Tests.AsyncTests+SerExOk: The mess",
				" ---> GoreRemoting.Tests.ExternalTypes.SqlExceptionTest (0x80131904): error",
				" ---> System.IO.IOException: io exc",
				"ExClientConnectionId",
				"   --- End of inner exception stack trace ---",
				"--- End of stack trace from previous location ---",
				"   at Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsThrowsFailing[TException](Action action, Boolean isStrictType, String assertMethodName)",
			]);
#endif
		}
		else
		{
			Assert.IsNull(e4.InnerException);

			AssertLines(e4, [
				"GoreRemoting.Tests.AsyncTests+SerExOk: The mess",
				" ---> GoreRemoting.Tests.ExternalTypes.SqlExceptionTest (0x80131904): error",
				" ---> System.IO.IOException: io exc",
				"ExClientConnectionId",
				"ExErrorNumberStateClass",
				"--- End of stack trace from previous location ---",
				"   at Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsThrowsFailing[TException](Action action, Boolean isStrictType, String assertMethodName)",
			]);
		}

	}

	private void AssertNotLine(Exception e4, string v)
	{
		var lines = e4.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
		Assert.IsFalse(lines.Contains(v));
	}

	private void AssertLines(Exception e, string[] strings)
	{
		var strings_list = strings.ToList();

		var estr = e.ToString();
#if !NET6_0_OR_GREATER
		estr = estr.Replace(" ---> ", Environment.NewLine + " ---> ");
#endif
		var lines = estr.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

		foreach (var line in lines)
		{
			if (line.StartsWith(strings_list.First()))
			{
				strings_list.RemoveAt(0);
				if (!strings_list.Any())
					break;
				continue;
			}
		}

		Assert.IsEmpty(strings_list);
	}

	[TestMethod]
	[DataRow(Serializer.BinaryFormatter)]
	[DataRow(Serializer.Json)]
#if NET6_0_OR_GREATER
	[DataRow(Serializer.MemoryPack)]
#endif
	[DataRow(Serializer.MessagePack)]
	[DataRow(Serializer.Protobuf)]
	public async Task ExceptionTests_SqlException(Serializer ser)
	{
		var serverConfig = new ServerConfig(Serializers.GetSerializer(ser));

		var port = Ports.GetNext();
		await using var server = new NativeServer(port, serverConfig);
		server.RegisterService<IExceptionTest, ExceptionTest>();
		server.Start();

		await using var client = new NativeClient(port, new ClientConfig(Serializers.GetSerializer(ser)));

		var proxy = client.CreateProxy<IExceptionTest>();

		var e4 = Assert.ThrowsExactly<SqlExceptionTest>(proxy.TestWithSqlException);

		Assert.AreEqual("error", e4.Message);

		if (ser == Serializer.BinaryFormatter)
		{
			Assert.AreEqual("io exc", e4.InnerException!.Message);
		}
		else
		{
			Assert.IsNull(e4.InnerException);
		}


		AssertNotLine(e4, "   --- End of inner exception stack trace ---");
	}
}


public enum Serializer
{
	BinaryFormatter = 1,
#if NET6_0_OR_GREATER
	MemoryPack = 2,
#endif
	Json = 3,
	MessagePack = 4,
	Protobuf = 5
}

public static class Serializers
{
	public static ISerializerAdapter GetSerializer(Serializer ser)
	{
		return ser switch
		{
			Serializer.BinaryFormatter => new BinaryFormatterAdapter(),
#if NET6_0_OR_GREATER
			Serializer.MemoryPack => new MemoryPackAdapter(),
#endif
			Serializer.Json => new JsonAdapter(),
			Serializer.MessagePack => new MessagePackAdapter(),
			Serializer.Protobuf => new ProtobufAdapter(),
			_ => throw new NotImplementedException(),
		};
	}
}
