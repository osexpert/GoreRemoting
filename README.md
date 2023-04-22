# GoreRemoting

GoreRemoting is based on CoreRemoting  
https://github.com/theRainbird/CoreRemoting  

GoreRemoting is (just like CoreRemoting) a way to migrate from .NET Remoting, but with Grpc instead of Websockets\Sockets.

How it works:
Services are always stateless\single call. If you need to store state, store in a session etc.
You can send extra headers with every call from client to server, eg. a sessionId or a Token via BeforeMethodCall on client and CreateInstance on server (look in examples).
Clients create proxies from service interfaces (typically in shared assembly).
No support for MarshalByRef behaviour. Everything is by value.
It is not possible to callback to clients directly, callbacks must happen as part of a call from client to server that awaits forever and keeps a stream open. The server can callback via a delegate argument (look in examples).
Can have as many callback delegate arguments as you wish, but only one can return something from the client. Others must be void\Task\ValueTask and will be OneWay (no result or exception from client).
Support CancellationToken (uses native Grpc support)
AsyncEnumerableAdapter to adapt to IAsyncEnumerable providers\consumers via delegate.
ProgressAdapter to adapt to IProgress providers\consumers via delegate.
You can create own adapters based on same idea to emulate MarshalByRef behaviour via delegates (but only works for simple scenarios).
Can use both native Grpc and Grpc dotnet.
Currently har working serializers for BinaryFormatter, System.Text.Json, MemoryPack
Support Task\ValueTask in service methods result and in result from delegate arguments (but max one with actual result).
It is possible to specify serializer on a per service or method basis, so slowly can migrate away from BinaryFormatter, method by method, service by service.
GoreRemoting does not use .proto files but simply interfaces. Look at the examples for info, there is no documentation.  

Limitations:
Method that return IEnumerable and yield (crashes)  
Method that return IAsyncEnumerable and yield (crashes)  

Removed from CoreRemoting:
CoreRemoting use websockets while GoreRemoting is a rewrite (sort of) to use Grpc instead.  
Encryption, authentication, session management, DependencyInjection, Linq expression arguments removed (maybe some can be added back if demand).

Delegate arguments:
Delegates that return void, Task, ValueTask are all threated as OneWay. Then it will not wait for any result and any exceptions thrown are eaten.
You can have max one delegate with result (eg. int, Task\<int\>, ValueTask\<int\>) else will get runtime exception.
If you need to force a delegate to be non-OneWay, then just make it return something (eg. a bool or Task\<bool\>). But again, max one delegate with result.
Advanced: StreamingFuncAttribute\StreamingDoneException can be used to make streaming from server to client faster (normally there will be one call from server to client for every call that pull data from client).

Methods:
OneWay methods not supported. Methods always wait for result\exception.

TODO:
Maybe OneWay delegate could be an opt-in instead of the default (still only max one could be non-OneWay)
Instead of eating exceptions from delegates, maybe could have an optino to throw them or some way to get notified about them (via subscribe to delegate)
Update: there is an event OneWayException in RemotingClient\RemotingServer if you want to observe any eaten exceptions.
Add session management? (not in the core, but as example)

Other Rpc framework maybe of interest:

StreamJsonRpc  
https://github.com/microsoft/vs-streamjsonrpc  

ServiceModel.Grpc   
https://max-ieremenko.github.io/ServiceModel.Grpc/  
https://github.com/max-ieremenko/ServiceModel.Grpc  

protobuf-net.Grpc  
https://github.com/protobuf-net/protobuf-net.Grpc  

SignalR.Strong
https://github.com/mehmetakbulut/SignalR.Strong  

MagicOnion RPC framework based on gRPC
https://github.com/Cysharp/MagicOnion

The examples:

Client and Server in .NET Framework 4.8 using Grpc.Core native.

Client and Server in .NET 6.0 using Grpc.Net managed.

BinaryFormatter does not work well between .NET Framework and .NET bcause types are different,
eg. string in .NET is "System.String,System.Private.CoreLib" while in .NET Framework "System.String,mscorlib"

There exists hacks (links may not be relevant):
https://programmingflow.com/2020/02/18/could-not-load-system-private-corelib.html  
https://stackoverflow.com/questions/50190568/net-standard-4-7-1-could-not-load-system-private-corelib-during-serialization/56184385#56184385  

You will need to add some hacks yourself if using BinaryFormatter across .NET Framework and .NET

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

Conclusion: StreamingFuncAttribute\StreamingDoneException does even out the numbers from and to. grpc dotnet is still slow.


Grpc dotnet problems:

When calling the server too fast(?) with grpc-dotnet, I get ENHANCE_YOUR_CALM:
Bug filed: https://github.com/grpc/grpc-dotnet/issues/2010
Workaround added: use a hangup sequence
