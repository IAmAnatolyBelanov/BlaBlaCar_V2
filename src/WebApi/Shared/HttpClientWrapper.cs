namespace WebApi.Shared
{
	public class HttpClientWrapper
	{
		private ClientWithUsageCount clientWithUsageCount = default!;

		public HttpClientWrapper(TimeSpan clientLifeTime)
		{
			clientWithUsageCount = new()
			{
				HttpClient = new HttpClient()
			};

			_ = Task.Run(async () =>
			{
				while (true)
				{
					await Task.Delay(clientLifeTime);

					_ = Task.Run(async () =>
					{
						var client = clientWithUsageCount;
						clientWithUsageCount = new()
						{
							HttpClient = new HttpClient()
						};

						while (client.UsageCount != 0)
						{
							await Task.Delay(1000);
						}

						client.HttpClient.Dispose();
					});
				}
			});
		}

		public async ValueTask MakeCall(Func<HttpClient, ValueTask> action)
		{
			var client = clientWithUsageCount;

			try
			{
				Interlocked.Increment(ref client.UsageCount);
				await action(client.HttpClient);
			}
			finally
			{
				Interlocked.Decrement(ref client.UsageCount);
			}
		}

		public async ValueTask<T> MakeCall<T>(Func<HttpClient, ValueTask<T>> action)
		{
			var client = clientWithUsageCount;

			try
			{
				Interlocked.Increment(ref client.UsageCount);
				return await action(client.HttpClient);
			}
			finally
			{
				Interlocked.Decrement(ref client.UsageCount);
			}
		}

		public HttpClientWrap Start()
		{
			return new HttpClientWrap(clientWithUsageCount);
		}

		public struct HttpClientWrap : IDisposable
		{
			private readonly ClientWithUsageCount _client;

			public HttpClientWrap(HttpClientWrap copy)
			{
				_client = copy._client;
			}
			public HttpClientWrap(ClientWithUsageCount client)
			{
				_client = client;
				Interlocked.Increment(ref _client.UsageCount);
			}

			public HttpClient GetHttpClient()
			{
				return _client.HttpClient;
			}

			public void Dispose()
			{
				Interlocked.Decrement(ref _client.UsageCount);
			}
		}

		public class ClientWithUsageCount
		{
			public HttpClient HttpClient = default!;
			public ulong UsageCount = 0;
		}
	}
}
