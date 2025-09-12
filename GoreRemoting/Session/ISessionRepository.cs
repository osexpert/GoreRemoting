using System;
using System.Collections.Generic;

namespace GoreRemoting;

/// <summary>
/// Interface to be implemented by CoreRemoting session repository classes.
/// </summary>
public interface ISessionRepository : IDisposable
{
	/// <summary>
	/// Creates a new session.
	/// </summary>
	/// <param name="server">Server instance</param>
	/// <returns>The newly created session</returns>
	Session CreateSession(RemotingServer server);

	/// <summary>
	/// Gets a specified session by its ID.
	/// </summary>
	/// <param name="sessionId">Session ID</param>
	/// <returns>The session correlating to the specified session ID</returns>
	Session GetSession(Guid sessionId);

	/// <summary>
	/// Gets a list of all sessions.
	/// </summary>
	IEnumerable<Session> Sessions { get; }

	/// <summary>
	/// Removes a specified session by its ID.
	/// </summary>
	/// <param name="sessionId">Session ID</param>
	void RemoveSession(Guid sessionId);
}
