## 0.0.3 (next)
* Use nuget stakx.DynamicProxy.AsyncInterceptor instead of copying the code
* Use nuget AsyncReaderWriterLockSlim instead of copying the code
* Remove change detect logic from CallContext again

## 0.0.2
* Not compatible with 0.0.1, not in code and not on the wire. So breaking.
* Added Protobuf
* Security: changed serializers (except for BinaryFormatter) to not send type information. Constructing generic types and the current limit is 20 method arguments. Generic method support is removed. Method overload support is removed.
* Exceptions: now more unified and uses ISerializable.GetObjectData\ctor(SerializationInfo, StreamingContext). Uses json under the hood.
* CallContext: changed to use json under the hood.
* StreamResponseQueue: fixed rare hang if serializer crashed
* Fix sending byte arguments with json serializer
* Json: use UnsafeRelaxedJsonEscaping

## 0.0.1
* First release.
