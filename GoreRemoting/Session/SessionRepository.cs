using System.Collections.Concurrent;
using System.Timers;
using Timer = System.Timers.Timer;

namespace GoreRemoting;

/// <summary>
/// Default in-memory session repository.
/// </summary>
public class SessionRepository : ISessionRepository
{
	private readonly ConcurrentDictionary<Guid, Session> _sessions;
	private Timer? _inactiveSessionSweepTimer;
	private readonly int _maximumSessionInactivityTimeSeconds;

	/// <summary>
	/// Creates a new instance of the SessionRepository class.
	/// </summary>
	/// <param name="inactiveSessionSweepIntervalSeconds">Sweep interval for inactive sessions in seconds (No session sweeping, if set to 0)</param>
	/// <param name="maximumSessionInactivityTimeSeconds">Maximum session inactivity time in minutes</param>
	public SessionRepository(int inactiveSessionSweepIntervalSeconds, int maximumSessionInactivityTimeSeconds)
	{
		_sessions = new ConcurrentDictionary<Guid, Session>();

		_maximumSessionInactivityTimeSeconds = maximumSessionInactivityTimeSeconds;

		StartInactiveSessionSweepTimer(inactiveSessionSweepIntervalSeconds);
	}

	/// <summary>
	/// Starts the inactive session sweep timer.
	/// </summary>
	/// <param name="inactiveSessionSweepIntervalSeconds">Sweep interval for inactive sessions in seconds</param>
	private void StartInactiveSessionSweepTimer(int inactiveSessionSweepIntervalSeconds)
	{
		if (inactiveSessionSweepIntervalSeconds <= 0)
			return;

		_inactiveSessionSweepTimer =
			new Timer(Convert.ToDouble(inactiveSessionSweepIntervalSeconds * 1000));

		_inactiveSessionSweepTimer.Elapsed += InactiveSessionSweepTimerOnElapsed;
		_inactiveSessionSweepTimer.Start();
	}

	/// <summary>
	/// Event procedure: Called when the inactive session sweep timer elapses. 
	/// </summary>
	/// <param name="sender">Event sender</param>
	/// <param name="e">Event arguments</param>
	private void InactiveSessionSweepTimerOnElapsed(object sender, ElapsedEventArgs e)
	{
		if (_inactiveSessionSweepTimer == null)
			return;

		if (!_inactiveSessionSweepTimer.Enabled)
			return;

		var inactiveSessionIdList =
			_sessions
				.Where(item =>
					DateTime.UtcNow.Subtract(item.Value.LastActivityUtc).TotalSeconds > _maximumSessionInactivityTimeSeconds)
				.Select(item => item.Key);

		foreach (var inactiveSessionId in inactiveSessionIdList)
		{
			RemoveSession(inactiveSessionId);
		}
	}

	/// <summary>
	/// Creates a new session.
	/// </summary>
	/// <param name="server">Server instance</param>
	/// <returns>The newly created session</returns>
	public Session CreateSession(RemotingServer server)
	{
		if (server == null)
			throw new ArgumentException(nameof(server));

		var session = new Session(server);

		_sessions.TryAdd(session.SessionId, session);

		return session;
	}

	/// <summary>
	/// Gets a specified session by its ID.
	/// </summary>
	/// <param name="sessionId">Session ID</param>
	/// <returns>The session correlating to the specified session ID</returns>
	/// <exception cref="KeyNotFoundException">Thrown, if no session with the specified session ID is found</exception>
	public Session GetSession(Guid sessionId)
	{
		if (_sessions.TryGetValue(sessionId, out var session))
			return session;

		throw new KeyNotFoundException($"Session '{sessionId}' not found.");
	}

	/// <summary>
	/// Removes a specified session by its ID.
	/// </summary>
	/// <param name="sessionId">Session ID</param>
	public void RemoveSession(Guid sessionId)
	{
		if (_sessions.TryRemove(sessionId, out var session))
			session.Dispose();
	}

	/// <summary>
	/// Gets a list of all sessions.
	/// </summary>
	public IEnumerable<Session> Sessions => _sessions.Values.ToArray();

	/// <summary>
	/// Frees managed resources.
	/// </summary>
	public void Dispose()
	{
		if (_inactiveSessionSweepTimer != null)
		{
			_inactiveSessionSweepTimer.Stop();
			_inactiveSessionSweepTimer.Dispose();
			_inactiveSessionSweepTimer = null;
		}

		while (_sessions.Count > 0)
		{
			var sessionId = _sessions.First().Key;
			RemoveSession(sessionId);
		}
	}
}
