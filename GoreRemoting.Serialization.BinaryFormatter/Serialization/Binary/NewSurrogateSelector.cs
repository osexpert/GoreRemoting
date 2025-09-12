/*
 * Based on code from Microsoft.Bot.Builder
 * https://github.com/CXuesong/BotBuilder.Standard
 * branch: netcore20+net45
 * BotBuilder.Standard/CSharp/Library/Microsoft.Bot.Builder/Fibers/NetStandardSerialization.cs
 * BotBuilder.Standard/CSharp/Library/Microsoft.Bot.Builder/Fibers/Serialization.cs
 */

using System;
//using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace GoreRemoting.Serialization.BinaryFormatter;

/// <summary>
/// Based on code from Microsoft.Bot.Builder on github
/// </summary>
public sealed class SafeSurrogateSelector : ISurrogateSelector
{
	//private static readonly IList<ISerializationSurrogateEx> _providers = GetProviders();
	private BinarySerializerOptions _options;

	[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
	public SafeSurrogateSelector(ISurrogateSelector next, BinarySerializerOptions options)
	{
		_options = options;
		if (next != null)
			throw new NotImplementedException("Next surrogate selector not allowed");
	}

	//[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
	//static IList<ISerializationSurrogateEx> GetProviders()
	//{
	//	var providers = new List<ISerializationSurrogateEx>();

	//	// These 2 are about safety during deserialize (does not alter serialization)
	//	providers.Add(new DataSetSurrogate());
	//	providers.Add(new WindowsIdentitySurrogate());

	//	if (BinaryFormatterAdapter.NetCore)
	//	{
	//		// These are about things that are no longer Serializable in net6
	//		// There is a lot more that is not Serializable in net6 (CollectionBase etc.)
	//		// but for some things its easier\better to change to somethign else than adding support for it here.
	//		providers.Add(new TypeSurrogate());
	//		providers.Add(new CultureInfoSurrogate());
	//	}

	//	return providers;
	//}

	[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
	void ISurrogateSelector.ChainSelector(ISurrogateSelector selector)
	{
		throw new NotImplementedException();
	}

	[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
	ISurrogateSelector ISurrogateSelector.GetNextSelector()
	{
		throw new NotImplementedException();
	}

	[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
	ISerializationSurrogate? ISurrogateSelector.GetSurrogate(Type type, StreamingContext context, out ISurrogateSelector? selector)
	{
		// Use a simpler logic at first, until we get conflicts (if ever)
		ISurrogate? found = null;
		foreach (var surr in _options.Surrogates)
		{
			if (surr.Handles(type, context))
			{
				if (found == null)
					found = surr;
				else
					throw new Exception("Myltiple surrogates can handle the same type: " + type);
			}
		}

		if (found != null)
		{
			selector = this;
			return found;
		}
		else
		{
			selector = null;
			return null;
		}
	}
}

/// <summary>
/// Extend <see cref="ISerializationSurrogate"/> with a "tester" method used by <see cref="SafeSurrogateSelector"/>.
/// </summary>
public interface ISurrogate : ISerializationSurrogate
{
	/// <summary>
	/// Determine whether this surrogate provider handles this type.
	/// </summary>
	/// <param name="type">The query type.</param>
	/// <param name="context">The serialization context.</param>
	/// <returns>True if this provider handles this type, false otherwise.</returns>
	bool Handles(Type type, StreamingContext context);
}
