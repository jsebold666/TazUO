using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

namespace ClassicUO.Dust765.Lobby.Networking
{
    public delegate void ConnectionEventHandler( ConnectionEventArgs e );

	public class ConnectionEventArgs : EventArgs
	{
        private IPAddress _address;
        public IPAddress Address => _address;
        
		private Socket _socket;
        public Socket AcceptedSocket => _socket;

		private Listener _host;
        public Listener GetListener() => _host;

		public ConnectionEventArgs( Listener host, Socket incoming ) {
			_host = host;
			_socket = incoming;
            _address = ( (IPEndPoint)incoming.RemoteEndPoint ).Address;
		}

		public bool Close { get; set; }
	}

	public class Listener : IDisposable
	{
		public static readonly Socket[] EMPTY_SOCKETS = new Socket[ 0 ];

		public static event ConnectionEventHandler OnClientConnect;

		readonly List<Socket> _connectionQueue;
		readonly object _syncObject;

		Socket _socket;
		AsyncCallback _acceptCallback;

		public Listener( IPEndPoint ipep ) {

			_connectionQueue = new List<Socket>();
			_syncObject = ( (ICollection)_connectionQueue ).SyncRoot;
			_socket = Bind( ipep );
            if ( _socket == null )
                throw new ArgumentNullException( "ipep", "Bind to endpoint failed." );

			Print();

            _acceptCallback = new AsyncCallback( OnAccept );
            try
            {
                _socket.BeginAccept( _acceptCallback, null );
            } catch ( SocketException ex )
            {
                TraceException( ex );
                Dispose();
            } catch ( ObjectDisposedException ) { }
		}

        public IEnumerable<Socket> Dequeue()
        {
            lock( _syncObject )
            {
                if ( _connectionQueue.Count == 0 )
                    yield break;

                try
                {
                    yield return _connectionQueue[0];
                }
                finally
                {
                    _connectionQueue.RemoveAt(0);
                }
            }
        }

		private void OnAccept( IAsyncResult ar ) {

            Socket socket;
            ConnectionEventArgs args;

            lock ( _syncObject )
            {
                try
                {
                    socket = _socket.EndAccept( ar );
                    args = new ConnectionEventArgs( this, socket );

                     if ( OnClientConnect != null )
                        OnClientConnect( args );

                    if ( args.Close )
                    {
                        try
                        {
                            socket.Shutdown( SocketShutdown.Both );
                        } catch ( SocketException ex )
                        {
                            TraceException( ex );
                        }
                        try
                        {
                            socket.Close();
                        } catch ( SocketException ex )
                        {
                            TraceException( ex );
                        }
                    } else
                    {
                        _connectionQueue.Add( socket );
                    }

                } catch ( Exception ex )
                {
                    if ( ex is SocketException )
                    {
                        TraceException( ex );
                    } else if ( ex is ObjectDisposedException )
                    {
                        return;
                    }
                }
            }

            try {
                _socket.BeginAccept( _acceptCallback, null );
            } catch ( SocketException ex ) {
                TraceException( ex );
            } 
            catch ( ObjectDisposedException ) { }
            catch( Exception ) { }
		}

        private void Print()
        {
            if ( _socket.LocalEndPoint is IPEndPoint ep && ep != null )
            {
                if ( ep.Address == IPAddress.Any || ep.Address == IPAddress.IPv6Any )
                {
                    foreach ( var unicast in
                        NetworkInterface.GetAllNetworkInterfaces()
                                        .Select( adapter => adapter.GetIPProperties() )
                                        .SelectMany( properties =>
                                            properties.UnicastAddresses
                                                      .Where( unicast =>
                                                        unicast.Address.AddressFamily == ep.AddressFamily ) ) )
                        Console.WriteLine( "Listening: {0}:{1}", unicast.Address, ep.Port );
                } else
                {
                    Console.WriteLine( "Listening: {0}:{1}", ep.Address, ep.Port );
                }
            }
        }

		bool _isClosed;

		public bool IsOpen { get { return !_isClosed; } }

		public void Dispose() {
			if ( _isClosed )
				return;

			Socket listener = Interlocked.Exchange( ref _socket, null );
            if ( listener != null )
				listener.Close();

            _connectionQueue.Clear();
			_isClosed = true;
		}

		private static Socket Bind( IPEndPoint ipep ) {

            if ( ipep == null )
                throw new ArgumentNullException( "ipep" );

			Socket s = new Socket( ipep.AddressFamily, SocketType.Stream, ProtocolType.Tcp );
			try {

                s.ReceiveTimeout = 3000;
				s.LingerState.Enabled = false;
				s.ExclusiveAddressUse = false;
				s.Bind( ipep );
				s.Listen( 8 );
				return s;
			} catch ( Exception e ) {
				if ( e is SocketException ) {
					var ex = e as SocketException;
					switch( ex.SocketErrorCode ) {

						case SocketError.AddressAlreadyInUse:
							Console.WriteLine( "Listener Error: {0}:{1} (In Use)", ipep.Address, ipep.Port );
							break;

						case SocketError.AddressNotAvailable:
							Console.WriteLine( "Listener Error: {0}:{1} (Unavailable)", ipep.Address, ipep.Port );
							break;

						default:
							Console.WriteLine( "Listener Exception:" );
							Console.WriteLine( ex );
							break;
				
					}
				}
			}
			return null;
		}

		private static void TraceException( Exception ex ) {
			try {
				using ( StreamWriter op = new StreamWriter( "network-errors.log", true ) ) {
					op.WriteLine( "# {0}", DateTime.UtcNow );
					op.WriteLine( ex );
					op.WriteLine();
					op.WriteLine();
				}
			} catch {
			}
			Console.WriteLine( ex );
		}
	}
}