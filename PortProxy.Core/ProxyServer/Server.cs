namespace PortProxy.ProxyServer
{
	using System;
	using System.Net;
	using System.Net.Sockets;

	using Connection;

	using Microsoft.Extensions.DependencyInjection;
	using Microsoft.Extensions.Logging;

	using Statistics;

	public class Server
	{
		private TcpListener _listener;
        private TcpListener[] _listeners;
        private ILogger<Server> _logger;
		private int _port;
		private bool _local;
		private IServiceProvider _serviceProvider;

		public Server(ILogger<Server> logger, IServiceProvider serviceProvider)
		{
			_logger = logger;
			_serviceProvider = serviceProvider;

			var config = serviceProvider.GetService<ServerConfig>();
			_port = config.Port;
			_local = config.Local;
		}

		public async void Start()
		{
			_logger.LogInformation("正在启动服务器端监听...");
			_listener = new TcpListener(IPAddress.Any, _port);
            _listeners = new TcpListener[5000];
            _listener.Start();
            _logger.LogInformation($"服务器监听在端口 {_port}, 本地模式 {_local}...");

            _logger.LogInformation("等待客户端连接...");

			TcpClient client;
			while (true)
			{
				try
				{
					client = await _listener.AcceptTcpClientAsync();
                }
				catch (Exception)
				{
					break;
				}

				var ctx = new ConnectionContext(client);
				_serviceProvider.GetService<IStatistics>().Register(ctx);

				_logger.LogInformation($"[{ctx.Id}] 新的客户端连接 {client.Client.RemoteEndPoint} -> {client.Client.LocalEndPoint}");
				ProcessClientAsync(ctx);
			}
		}

        public async void Prepare()
        {
            _logger.LogInformation("创建5000端口...");
            _listeners = new TcpListener[5000];
        }

        public async void Start2(int i)
        {
            _logger.LogInformation("正在启动服务器端监听...");
            _listeners[i] = new TcpListener(IPAddress.Any, (_port + i));
            _listeners[i].Start();
            _logger.LogInformation($"服务器监听在端口 {_port + i}, 本地模式 {_local}...");

            _logger.LogInformation("等待客户端连接...");

            TcpClient client;
            while (true)
            {
                try
                {
                    client = await _listeners[i].AcceptTcpClientAsync();
                }
                catch (Exception)
                {
                    break;
                }

                var ctx = new ConnectionContext(client);
                _serviceProvider.GetService<IStatistics>().Register(ctx);

                _logger.LogInformation($"[{ctx.Id}] 新的客户端连接 {client.Client.RemoteEndPoint} -> {client.Client.LocalEndPoint}");
                ProcessClientAsync(ctx);
            }
        }

        async void ProcessClientAsync(ConnectionContext context)
		{
			using (var connection = _serviceProvider.GetService<IConnectionFactory>().GetConnection(context))
				await connection.ProcessClientAsync();
		}

		public void Stop()
		{
			_listener.Stop();
		}
	}
}
