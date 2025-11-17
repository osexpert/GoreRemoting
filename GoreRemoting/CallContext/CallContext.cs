using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace GoreRemoting;

/// <summary>
/// Provides a way to set contextual data that flows with the call and 
/// async context of a invocation.
/// </summary>
public static class CallContext
{
	static JsonSerializerOptions _opt = new JsonSerializerOptions
	{
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};

	private static readonly ConcurrentDictionary<string, AsyncLocal<string>> State =
		new ConcurrentDictionary<string, AsyncLocal<string>>();

	/// <summary>
	/// Stores a given object and associates it with the specified name.
	/// </summary>
	/// <param name="name">The name with which to associate the new item in the call context.</param>
	/// <param name="data">The object to store in the call context.</param>
	private static void SetStringPrivate(string name, string data) =>
		State.GetOrAdd(name, _ => new AsyncLocal<string>()).Value = data;

	/// <summary>
	/// Retrieves an object with the specified name from the <see cref="CallContext"/>.
	/// </summary>
	/// <param name="name">The name of the item in the call context.</param>
	/// <returns>The object in the call context associated with the specified name, or <see langword="null"/> if not found.</returns>
	private static string? GetStringPrivate(string name) =>
		State.TryGetValue(name, out AsyncLocal<string> data) ? data.Value : null;

	public static T? GetValue<T>(string name)
	{
		var value = GetStringPrivate(name);
		if (value == null)
			return default;

		return JsonSerializer.Deserialize<T>(value, _opt);
	}

	public static void SetValue<T>(string name, T? value)
	{
		SetStringPrivate(name, JsonSerializer.Serialize<T?>(value, _opt));
	}

	/// <summary>
	/// Gets a serializable snapshot of the current call context.
	/// </summary>
	/// <returns>Array of call context entries</returns>
	internal static CallContextEntry[] CreateChangesSnapshot()
	{
		var stateSnaphsot = State.ToArray();

		var stateSnapshotNotNull = stateSnaphsot.Where(i => i.Value.Value != null).ToArray();

		var result = new CallContextEntry[stateSnapshotNotNull.Length];

		for (int i = 0; i < stateSnapshotNotNull.Length; i++)
		{
			var entry = stateSnapshotNotNull[i];

			result[i] =	new CallContextEntry()
			{
				Name = entry.Key,
				Value = entry.Value.Value
			};
		}

		return result;
	}

	/// <summary>
	/// Restore the call context from a snapshot.
	/// </summary>
	/// <param name="entries">Call context entries</param>
	internal static void RestoreFromChangesSnapshot(IEnumerable<CallContextEntry> entries)
	{
		foreach (var entry in entries)
		{
			SetStringPrivate(entry.Name, entry.Value);
		}
	}
}
