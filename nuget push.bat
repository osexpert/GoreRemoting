set myKey=
set ver=0.0.1

nuget push bin\Packages\Release\NuGet\GoreRemoting.%ver%.nupkg -s https://api.nuget.org/v3/index.json -k %myKey%
nuget push bin\Packages\Release\NuGet\GoreRemoting.Compression.Lz4.%ver%.nupkg -s https://api.nuget.org/v3/index.json -k %myKey%
nuget push bin\Packages\Release\NuGet\GoreRemoting.Serialization.BinaryFormatter.%ver%.nupkg -s https://api.nuget.org/v3/index.json -k %myKey%
nuget push bin\Packages\Release\NuGet\GoreRemoting.Serialization.Json.%ver%.nupkg -s https://api.nuget.org/v3/index.json -k %myKey%
nuget push bin\Packages\Release\NuGet\GoreRemoting.Serialization.MemoryPack.%ver%.nupkg -s https://api.nuget.org/v3/index.json -k %myKey%
nuget push bin\Packages\Release\NuGet\GoreRemoting.Serialization.MessagePack.%ver%.nupkg -s https://api.nuget.org/v3/index.json -k %myKey%