#if false
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using CoreRemoting.Authentication;
using CoreRemoting.Channels;
using CoreRemoting.ClassicRemotingApi;
using CoreRemoting.DependencyInjection;
using CoreRemoting.RpcMessaging;
using CoreRemoting.RemoteDelegates;
using CoreRemoting.Encryption;
using CoreRemoting.Serialization;
using Serialize.Linq.Nodes;

namespace CoreRemoting
{
    /// <summary>
    /// Implements a CoreRemoting session, which controls the CoreRemoting protocol on application layer at server side.
    /// This is doing the RPC magic of CoreRemoting at server side.
    /// </summary>
    public class RemotingSession : IDisposable
    {
#region Fields

        private readonly IRemotingServer _server;


        private readonly Guid _sessionId;
        
        private readonly RemoteDelegateInvocationEventAggregator _remoteDelegateInvocationEventAggregator;
        private IDelegateProxyFactory _delegateProxyFactory;
        private ConcurrentDictionary<Guid, IDelegateProxy> _delegateProxyCache;
        private bool _isAuthenticated;
        private DateTime _lastActivityTimestamp;

        /// <summary>
        /// Event: Fired before the session is disposed to do some clean up.
        /// </summary>
        public event Action BeforeDispose;
        
#endregion

#region Construction

        /// <summary>
        /// Creates a new instance of the RemotingSession class.
        /// </summary>
        /// <param name="server">Server instance, that hosts this session</param>
        internal RemotingSession(IRemotingServer server
            )
        {
            _sessionId = Guid.NewGuid();
            _lastActivityTimestamp = DateTime.Now;
            _isAuthenticated = false;
            
            CreatedOn = DateTime.Now;
            _remoteDelegateInvocationEventAggregator = new RemoteDelegateInvocationEventAggregator();
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _delegateProxyFactory = _server.ServiceRegistry.GetService<IDelegateProxyFactory>();
            _delegateProxyCache = new ConcurrentDictionary<Guid, IDelegateProxy>();
        }

        /// <summary>
        /// Event procedure: Called if the ErrorOccured event is fired on the raw message transport component.
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        /// <param name="ex">Optional exception from the transport infrastructure</param>
        private void OnErrorOccured(string errorMessage, Exception ex)
        {
            var exception = new RemotingException(errorMessage, innerEx: ex); 
            
            ((RemotingServer)_server).OnError(exception);
        }

#endregion

#region Properties

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
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public DateTime CreatedOn { get; }

        /// <summary>
        /// Gets whether authentication was successful.
        /// </summary>
        public bool IsAuthenticated => _isAuthenticated;



        /// <summary>
        /// Gets the authenticated identity of this session.
        /// </summary>
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public RemotingIdentity Identity { get; private set; }

#endregion



     
#region IDisposable implementation

        /// <summary>
        /// Frees managed resources.
        /// </summary>
        public void Dispose()
        {
            BeforeDispose?.Invoke();
            
        
            _delegateProxyFactory = null;
            _delegateProxyCache.Clear();
            _delegateProxyCache = null;
        
        }

#endregion
    }
}
#endif