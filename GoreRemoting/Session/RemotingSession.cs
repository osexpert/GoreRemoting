using System;
using System.Collections.Concurrent;

namespace GoreRemoting
{
    /// <summary>
    /// Implements a CoreRemoting session, which controls the CoreRemoting protocol on application layer at server side.
    /// This is doing the RPC magic of CoreRemoting at server side.
    /// </summary>
    public class RemotingSession : IDisposable
    {
        private readonly RemotingServer _server;

		private readonly ConcurrentDictionary<object, object> _properties = new();

        private readonly Guid _sessionId;
        private bool _isAuthenticated;
        private DateTime _lastActivityTimestamp;

        /// <summary>
        /// Event: Fired before the session is disposed to do some clean up.
        /// </summary>
        public event Action? BeforeDispose;
        
        /// <summary>
        /// Creates a new instance of the RemotingSession class.
        /// </summary>
        /// <param name="server">Server instance, that hosts this session</param>
        internal RemotingSession(RemotingServer server)
        {
            _sessionId = Guid.NewGuid();
            _lastActivityTimestamp = DateTime.Now;
            _isAuthenticated = false;
            
            CreatedOn = DateTime.Now;
            _server = server ?? throw new ArgumentNullException(nameof(server));
        }

		public T? GetProperty<T>(object key)
		{
			if (_properties.TryGetValue(key, out var value))
			{
				return (T)value;
			}

			return default;
		}

		public void SetProperty<T>(object key, T value) where T : notnull
		{
			_properties[key] = value;
		}

        /// <summary>
        /// Gets the timestamp of the last activity of this session.
        /// </summary>
        public DateTime LastActivityTimestamp => _lastActivityTimestamp;

        /// <summary>
        /// Gets this session's unique session ID.
        /// </summary>
        public Guid SessionId => _sessionId;

        /// <summary>
        /// Gets the timestamp when this session was created.
        /// </summary>
        public DateTime CreatedOn { get; }

        /// <summary>
        /// Gets whether authentication was successful.
        /// </summary>
        public bool IsAuthenticated => _isAuthenticated;



        // <summary>
        // Gets the authenticated identity of this session.
        // </summary>
//        public RemotingIdentity Identity { get; private set; }


        /// <summary>
        /// Frees managed resources.
        /// </summary>
        public void Dispose()
        {
            BeforeDispose?.Invoke();
        }


    }
}
