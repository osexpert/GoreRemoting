using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace GoreRemoting
{
	/// <summary>
	/// Provides a way to set contextual data that flows with the call and 
	/// async context of a invocation.
	/// </summary>
	public static class CallContext
	{
		private static readonly ConcurrentDictionary<string, AsyncLocal<string?>> State =
			new ConcurrentDictionary<string, AsyncLocal<string?>>();

		/// <summary>
		/// Stores a given object and associates it with the specified name.
		/// </summary>
		/// <param name="name">The name with which to associate the new item in the call context.</param>
		/// <param name="data">The object to store in the call context.</param>
		public static void SetData(string name, string? data) =>
			State.GetOrAdd(name, _ => new AsyncLocal<string?>()).Value = data;

		/// <summary>
		/// Retrieves an object with the specified name from the <see cref="CallContext"/>.
		/// </summary>
		/// <param name="name">The name of the item in the call context.</param>
		/// <returns>The object in the call context associated with the specified name, or <see langword="null"/> if not found.</returns>
		public static string? GetData(string name) =>
			State.TryGetValue(name, out AsyncLocal<string?> data) ? data.Value : null;

		/// <summary>
		/// Gets a serializable snapshot of the current call context.
		/// </summary>
		/// <returns>Array of call context entries</returns>
		internal static CallContextEntry[] GetSnapshot()
		{
			var stateSnaphsot = State.ToArray();

			var result = new CallContextEntry[stateSnaphsot.Length];

			for (int i = 0; i < stateSnaphsot.Length; i++)
			{
				var entry = stateSnaphsot[i];

				result[i] =
					new CallContextEntry()
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
		internal static void RestoreFromSnapshot(IEnumerable<CallContextEntry> entries)
		{
			// This logic is weird... Why set everything to null in this case???
			// Also...will it ever be null?
			// And if we have empty collection, suddenly the logic switch completely...to do nothing at all.
			// This make no sense IMO.
			if (entries == null)
			{
				foreach (var entry in State)
				{
					SetData(entry.Key, null);
				}
				return;
			}

			foreach (var entry in entries)
			{
				CallContext.SetData(entry.Name, entry.Value);
			}
		}
	}
}
