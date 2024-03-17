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
		private static readonly ConcurrentDictionary<string, AsyncLocal<(string?, bool)>> State =
			new ConcurrentDictionary<string, AsyncLocal<(string?, bool)>>();

		/// <summary>
		/// Stores a given object and associates it with the specified name.
		/// </summary>
		/// <param name="name">The name with which to associate the new item in the call context.</param>
		/// <param name="data">The object to store in the call context.</param>
		public static void SetData(string name, string? data) =>
			State.GetOrAdd(name, _ => new AsyncLocal<(string?, bool)>()).Value = (data, true);

		/// <summary>
		/// Retrieves an object with the specified name from the <see cref="CallContext"/>.
		/// </summary>
		/// <param name="name">The name of the item in the call context.</param>
		/// <returns>The object in the call context associated with the specified name, or <see langword="null"/> if not found.</returns>
		public static string? GetData(string name) =>
			State.TryGetValue(name, out AsyncLocal<(string?, bool)> data) ? data.Value.Item1 : null;

		/// <summary>
		/// Gets a serializable snapshot of the current call context.
		/// </summary>
		/// <returns>Array of call context entries</returns>
		internal static CallContextEntry[] GetChangedSnapshot()
		{
			var stateSnaphsot = State.ToArray();

			var changedEntries = stateSnaphsot.Where(s => s.Value.Value.Item2).ToArray();

			var result = new CallContextEntry[changedEntries.Length];

			for (int i = 0; i < changedEntries.Length; i++)
			{
				var entry = changedEntries[i];

				result[i] =
					new CallContextEntry()
					{
						Name = entry.Key,
						Value = entry.Value.Value.Item1
					};
			}

			return result;
		}


		/// <summary>
		/// Stores a given object and associates it with the specified name.
		/// </summary>
		/// <param name="name">The name with which to associate the new item in the call context.</param>
		/// <param name="data">The object to store in the call context.</param>
		private static void SetData_NotChanged(string name, string? data) =>
			State.GetOrAdd(name, _ => new AsyncLocal<(string?, bool)>()).Value = (data, true);


		/// <summary>
		/// Restore the call context from a snapshot.
		/// </summary>
		/// <param name="entries">Call context entries</param>
		internal static void RestoreFromSnapshot(IEnumerable<CallContextEntry> entries)
		{
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
				CallContext.SetData_NotChanged(entry.Name, entry.Value);
			}
		}
	}
}
