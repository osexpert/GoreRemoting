using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;

namespace GoreRemoting
{
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

		// bool: true if changed
		private static readonly ConcurrentDictionary<string, AsyncLocal<(string, bool)>> State =
			new ConcurrentDictionary<string, AsyncLocal<(string, bool)>>();

		/// <summary>
		/// Stores a given object and associates it with the specified name.
		/// </summary>
		/// <param name="name">The name with which to associate the new item in the call context.</param>
		/// <param name="data">The object to store in the call context.</param>
		private static void SetStringPrivate(string name, string? data) =>
			State.GetOrAdd(name, _ => new AsyncLocal<(string, bool)>()).Value = (data, true);

		private static void SetStringNotChanged(string name, string data) =>
			State.GetOrAdd(name, _ => new AsyncLocal<(string, bool)>()).Value = (data, false);

		/// <summary>
		/// Retrieves an object with the specified name from the <see cref="CallContext"/>.
		/// </summary>
		/// <param name="name">The name of the item in the call context.</param>
		/// <returns>The object in the call context associated with the specified name, or <see langword="null"/> if not found.</returns>
		private static string? GetStringPrivate(string name) =>
			State.TryGetValue(name, out AsyncLocal<(string, bool)> data) ? data.Value.Item1 : null;//"null";

	//	public static void RemoveData(string name) => State.TryRemove(name, out AsyncLocal<string> _);

		public static T? GetValue<T>(string name)
		{
			var value = GetStringPrivate(name);
			if (value == null)
				return default;

			//if (value == null)
			//	return default;
			//else if (typeof(T) == typeof(string))
			//	return (T)(object)value;
			//else
			return JsonSerializer.Deserialize<T>(value, _opt);
		}

		public static void SetValue<T>(string name, T? value)
		{
			//if (value is null)
			//	SetStringPrivate(name, null);
			//else if (value is string s)
			//	SetStringPrivate(name, s);
			//else
			SetStringPrivate(name, JsonSerializer.Serialize<T?>(value, _opt));
		}

		/// <summary>
		/// Gets a serializable snapshot of the current call context.
		/// </summary>
		/// <returns>Array of call context entries</returns>
		internal static CallContextEntry[] GetChangesSnapshot()
		{
			var stateSnaphsot = State.ToArray();

			var stateSnaphsotChanged = stateSnaphsot.Where(en => en.Value.Value.Item2).ToArray();

			var result = new CallContextEntry[stateSnaphsotChanged.Length];

			for (int i = 0; i < stateSnaphsotChanged.Length; i++)
			{
				var entry = stateSnaphsotChanged[i];

				result[i] =	new CallContextEntry()
				{
					Name = entry.Key,
					Value = entry.Value.Value.Item1
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
			// This logic is weird... Why set everything to null in this case???
			// Also...will it ever be null?
			// And if we have empty collection, suddenly the logic switch completely...to do nothing at all.
			// This make no sense IMO.
			//if (entries == null)
			//{
			//if (removeExisting)
			//{
			//	foreach (var entry in State)
			//	{
			//		//RemoveData(entry.Key);
			//		//	SetData(entry.Key, null);
			//	}
			//}
			//	return;
			//}

			foreach (var entry in entries)
			{
				SetStringNotChanged(entry.Name, entry.Value);
			}
		}
	}
}
