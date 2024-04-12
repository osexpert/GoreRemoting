# GoreRemoting
GoreRemoting is based on CoreRemoting
https://github.com/theRainbird/CoreRemoting  

GoreRemoting is (just like CoreRemoting) a way to migrate from .NET Remoting, but with Grpc instead of WebSockets\Sockets.

## General
Services are always stateless\single call. If you need to store state, store in a session.
You can send extra headers with every call from client to server, eg. a sessionId or a Token via BeforeMethodCall on client and CreateInstance on server (look in examples).
Clients create proxies from service interfaces (typically in shared assembly).
No support for MarshalByRef behaviour. Everything is by value.
GoreRemoting does not use .proto files (Protobuf).
Currently there is a limit of 20 method parameters. It is possible to increase it, possibly can increated to 30 if demand. But more than 30 won't happen.

## Callbacks from server to client
It is not possible to callback to clients directly, callbacks must happen during a call from client to server.
The server can callback via a delegate argument (look in examples).
Can have as many callback delegate arguments as you wish, but only one can return something from the client. 
Others must be void\Task\ValueTask and will be OneWay (no result or exception from client).
If you need to have a permanent open stream from server to client, have the client call a method that awaits forever and keeps an open stream,
and send callbacks via a delegate argument (look in examples).

## Cancellation
Support CancellationToken (via Grpc itself)

## IAsyncEnumerable
Does not support IEnumerable\IAsyncEnumerable as result.
Has AsyncEnumerableAdapter to adapt to IAsyncEnumerable providers\consumers via delegate.
But using delegate arguments may be just as easy\easier.

## IProgress
Does not support IProgress as argument.
Has ProgressAdapter to adapt to IProgress providers\consumers via delegate.
But using delegates arguments may be just as easy\easier.

## Grpc implementations
Can use both Grpc native and Grpc dotnet.
But Grpc dotnet is only fully compatible with itself, so I would strongly discourage mixing Grpc dotnet server and Grpc native clients.
Mixing Grpc native server and Grpc dotnet clients may work better.
But best to not mix Grpc dotnet with anything else.
Reason: under stress will get errors, specifically ENHANCE_YOUR_CALM

## Serializers
Currently has serializers for BinaryFormatter, System.Text.Json, MessagePack, MemoryPack, Protobuf.
Must set a default serializer. Can overide serializer per service\per method with SerializerAttribute.
This way migration from BinaryFormatter to eg. System.Text.Json can happen method by method\service by service.

### Exception handling
Exceptions thrown are marshalled based on a setting in ExceptionSerialization: ExceptionStrategy.
The default for all serializers (except BinaryFormatter) are ExceptionStrategy.Clone.
BinaryFormatter has its own ExceptionStrategy setting (override) and its default is ExceptionStrategy.BinaryFormatter.

### Exception strategies (GoreRemoring)
	public enum ExceptionStrategy
	{
		/// <summary>
		/// Same type as original, but some pieces may be missing (best effort).
		/// Uses ISerializable.GetObjectData\ctor(SerializationInfo, StreamingContext).
		/// </summary>
		Clone = 1,
		/// <summary>
		/// Always type RemoteInvocationException.
		/// Uses ISerializable.GetObjectData\ctor(SerializationInfo, StreamingContext).
		/// </summary>
		RemoteInvocationException = 2
	}

### Exception strategies (BinaryFormatter)
	public enum ExceptionStrategy
	{
		/// <summary>
		/// Use ExceptionSerialization.ExceptionStrategy setting
		/// </summary>
		Default = 0,

		/// <summary>
		/// BinaryFormatter used (if serializable, everything is preserved, else serialized as default)
		/// </summary>
		BinaryFormatter = 3
	}

## Compression
Has compressor for Lz4 and can also use GzipCompressionProvider that already exist in Grpc dotnet.
Can set default compressor, and like for serializers, can overide per service\per method with CompressorAttribute.
A NoCompressionProvider exist in case you want to use eg. Lz4 as default but want to ovveride some methods\services to not use compression.

## Task\async
Support Task\ValueTask in service methods result and in result from delegate arguments (but max one delegate with actual result).

## Limitations
Method that return IEnumerable and yield (crashes)  
Method that return IAsyncEnumerable and yield (crashes)  

## Removed from CoreRemoting
CoreRemoting use WebSockets while GoreRemoting is a rewrite (sort of) to use Grpc instead.  
Encryption, authentication, session management, DependencyInjection, Linq expression arguments removed (maybe some can be added back if demand).

## Delegate arguments
Delegates that return void, Task, ValueTask are all threated as OneWay. Then it will not wait for any result and any exceptions thrown are eaten.
You can have max one delegate with result (eg. int, Task\<int\>, ValueTask\<int\>) else will get runtime exception.
If you need to force a delegate to be non-OneWay, then just make it return something (eg. a bool or Task\<bool\>). But again, max one delegate with result.

### Advanced streaming
StreamingFuncAttribute\StreamingDoneException can be used to make streaming from client to server faster.
Normally there will be one delegate call from server to client for every delegate call that pull data from client.
With StreamingFuncAttribute\StreamingDoneException there will only be one delegate call from server to client, to start the streaming.
Streaming from server to client is always fast (one way delegate).

## Methods
OneWay methods not supported. Methods always wait for result\exception.

## Other Rpc framework maybe of interest
StreamJsonRpc (Json or MessagePack over streams & WebSockets)
https://github.com/microsoft/vs-streamjsonrpc  

ServiceModel.Grpc (code-first support, gRPC)
https://github.com/max-ieremenko/ServiceModel.Grpc  

protobuf-net.Grpc (code-first support, gRPC)
https://github.com/protobuf-net/protobuf-net.Grpc  

SignalR.Strong (strongly-typed hub methods)
https://github.com/mehmetakbulut/SignalR.Strong  

MagicOnion (RPC, gRPC)
https://github.com/Cysharp/MagicOnion

SharpRemote (RPC, TCP/IP)
https://github.com/Kittyfisto/SharpRemote

ServiceWire (RPC, Named Pipes or TCP/IP)
https://github.com/tylerjensen/ServiceWire

AdvancedRpc (TCP and Named Pipes)
https://github.com/fsdsabel/AdvancedRpc

SimpleRpc (gRPC)
https://github.com/netcore-jroger/SimpleRpc

## Examples
Client and Server in .NET Framework 4.8 using Grpc.Core native.
Client and Server in .NET 6.0 using Grpc.Net managed.

## BinaryFormatter interop
BinaryFormatter does not work well between .NET Framework and .NET bcause types are different,
eg. string in .NET is "System.String,System.Private.CoreLib" while in .NET Framework "System.String,mscorlib"

There exists hacks (links may not be relevant):
https://programmingflow.com/2020/02/18/could-not-load-system-private-corelib.html  
https://stackoverflow.com/questions/50190568/net-standard-4-7-1-could-not-load-system-private-corelib-during-serialization/56184385#56184385  

You will need to add some hacks yourself if using BinaryFormatter across .NET Framework and .NET

PS: why won't this be a problem for all serializers?

## Performance
Performance (1MB package size):
The file copy test:
.NET 4.8 server\client:  
File sent to server and written by server: 18 seconds (why so slow?)  
File read from server and written by client: 11 seconds  

.NET 6.0 server\client:  
File sent to server and written by server: 31 seconds (oh noes...)  
File read from server and written by client: 13 seconds  

Update, when using StreamingFuncAttribute\StreamingDoneException (but also using smaller package size, 8KB instead of 1MB):
.NET 6.0 native server\client: 
File sent to server and written by server: 16 seconds (better)
File read from server and written by client: 15 seconds

.NET 6.0 dotnet server\client:
File sent to server and written by server: 22 seconds (dotnet still slower than native)  
File read from server and written by client: 23 seconds (faster before...)

.NET 4.8 server\client:  
File sent to server and written by server: 15 seconds
File read from server and written by client: 15 seconds  

Conclusion: StreamingFuncAttribute\StreamingDoneException does even out the numbers from and to, but Grpc dotnet is still slower.

## grpc-dotnet problems
When calling the grpc-dotnet server too fast(?), I get ENHANCE_YOUR_CALM\ResourceExhausted\RST_STREAM or similar:
Bug filed: https://github.com/grpc/grpc-dotnet/issues/2010
Workaround added: use a hangup sequence.
But still, this only workaround the problem when grpc-dotnet is used as both server and client.
If grpc-dotnet is mixed with grpc-native, the problem still exist, specially when using grpc-native client agains grpc-dotnet server.
