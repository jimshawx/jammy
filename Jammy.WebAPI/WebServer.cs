using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema.Generation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.WebAPI
{
	public interface IWebServer
	{
		void Start();
	}

	public class WebServer : IWebServer
	{
		private readonly ILogger<WebServer> logger;
		private readonly IServiceProvider serviceProvider;
		private readonly HttpListener httpListener;
		private readonly Dictionary<string, Func<object>> getActions = new Dictionary<string, Func<object>>();
		private readonly Dictionary<string, Action<object>> putActions = new Dictionary<string, Action<object>>();
		private readonly JsonSerializer serializer = new JsonSerializer();

		private const string rootUrl = "jammy";
		private string openApiJson = "{}";

		public WebServer(ILogger<WebServer> logger, IServiceProvider serviceProvider)
		{
			this.logger = logger;
			this.serviceProvider = serviceProvider;

			//netsh http add urlacl url=http://+:8080/ user=Everyone

			httpListener = new HttpListener();
			httpListener.Prefixes.Add("http://+:8080/");
		}

		public void Start()
		{ 
			ThreadPool.GetMaxThreads(out var workerThreads, out var completionPortThreads);
			logger.LogTrace($"ThreadPool size {workerThreads} {completionPortThreads}");

			Assembly assembly = Assembly.GetExecutingAssembly();

			var matchingClasses = assembly
				.GetTypes()
				.Where(t => t.IsClass)
				.Where(t => t.GetCustomAttributes(typeof(UrlPathAttributes), inherit: true).Any());

			var openApiPaths = new Dictionary<string, object>();
			var generator = new JSchemaGenerator();

			foreach (var clas in matchingClasses)
			{
				var path = clas.GetCustomAttribute<UrlPathAttributes>();
				var methods = clas
					.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
					.Where(m => m.GetCustomAttributes(typeof(UrlActionAttribute), false).Any());

				var instance = ActivatorUtilities.CreateInstance(serviceProvider, clas);
				foreach (var method in methods)
				{
					var action = method.GetCustomAttribute<UrlActionAttribute>();

					string fullPath = $"/v{action.Ver}/{rootUrl}/{path.Path}/{action.Path}";
					logger.LogTrace(fullPath);

					//add the actions to the verb handlers
					if (action.Action == "GET")
						getActions.Add(fullPath, () => method.Invoke(instance, null));
					else if (action.Action == "PUT" || action.Action == "POST")
						putActions.Add(fullPath, (o) => method.Invoke(instance, [o]));

					//add the actions to OpenAPI
					AddToOpenAPI(openApiPaths, generator, method, action, fullPath);
				}
			}

			var openApiDoc = new
			{
				openapi = "3.0.1",
				info = new { title = "Jammy API", version = "1.0.0" },
				paths = openApiPaths,
				servers = new [ ] { new { url = "http://localhost:8080"} }
			};

			openApiJson = JsonConvert.SerializeObject(openApiDoc, Formatting.Indented);

			try
			{ 
				httpListener.Start();
				_ = Task.Run(StartListeningAsync);
			}
			catch (HttpListenerException)
			{
				logger.LogTrace("It was not possible to start the webserver on port 8080");
				logger.LogTrace("Run this command as administrator to enable the webserver to start:");
				logger.LogTrace("\tnetsh http add urlacl url=http://+:8080/ user=Everyone");
			}
		}

		private static void AddToOpenAPI(Dictionary<string, object> openApiPaths, JSchemaGenerator generator, MethodInfo method, UrlActionAttribute action, string fullPath)
		{
			if (!openApiPaths.TryGetValue(fullPath, out var pathNode))
				openApiPaths[fullPath] = pathNode = new Dictionary<string, object>();

			((Dictionary<string, object>)pathNode)[action.Action.ToLower()] = new Dictionary<string, object>
			{
				["summary"] = action.Summary,
				["responses"] = new Dictionary<string, object>
				{
					["200"] = new Dictionary<string, object> { ["description"] = "Success" }
				}
			};

			if (method.ReturnType != typeof(void))
			{
				((dynamic)pathNode)[action.Action.ToLower()]["responses"]["200"]["content"] = new Dictionary<string, object>
				{
					["application/json"] = new Dictionary<string, object>
					{
						["schema"] = JObject.FromObject(generator.Generate(method.ReturnType))
					}
				};
			}
		}

		private async Task StartListeningAsync()
		{
			while (httpListener.IsListening)
			{
				try
				{
					var context = await httpListener.GetContextAsync();
					_ = Task.Run(() => ProcessRequest(context, logger));
				}
				catch (Exception ex)
				{
					logger.LogTrace($"Listener loop interrupted: {ex}");
				}
			}
		}

		private bool ProcessRequest(HttpListenerContext context, ILogger logger)
		{
			var req = context.Request;
			var res = context.Response;

			try
			{
				//CORS
				res.AddHeader("Access-Control-Allow-Origin", "*");
				res.AddHeader("Access-Control-Allow-Methods", "GET, PUT, POST, OPTIONS");
				res.AddHeader("Access-Control-Allow-Headers", "Content-Type");

				//cache control
				res.AddHeader("Cache-Control", "no-cache, no-store, must-revalidate");

				switch (req.HttpMethod)
				{
					case "GET":
						{
							if (req.Url.AbsolutePath == "/openapi.json")
							{
								res.ContentType = "application/json";
								using (var writer = new StreamWriter(res.OutputStream, Encoding.UTF8))
								{
									writer.Write(openApiJson);
								}
								return Response(res, 200);
							}

							var action = getActions.GetValueOrDefault(SanitizePath(req.Url));
							if (action == null) return Response(res, 404);

							res.ContentType = "application/json";
							using var streamWriter = new StreamWriter(res.OutputStream, Encoding.UTF8);
							using (var jsonWriter = new JsonTextWriter(streamWriter))
							{
								serializer.Serialize(jsonWriter, action());
							}
							return Response(res, 200);
						}

					case "PUT":
					case "POST":
						{
							var action = putActions.GetValueOrDefault(SanitizePath(req.Url));
							if (action == null) return Response(res, 404);

							using var streamReader = new StreamReader(req.InputStream, req.ContentEncoding);

							switch (SanitizeContentType(req.ContentType))
							{
								case "json":
									using (var jsonReader = new JsonTextReader(streamReader))
									{
										object request = serializer.Deserialize(jsonReader);
										action(request);
									}
									return Response(res, 200);

								case "plain":
									var text = streamReader.ReadToEnd();
									action(text);
									return Response(res, 200);

								default:
									return Response(res, 415);
							}
						}

					case "OPTIONS":
						return Response(res, 200);
					default:
						return Response(res, 400);
				}
				throw new UnreachableException();
			}
			catch (Exception ex)
			{
				logger.LogTrace($"Request crashed: {ex}");
				res.ContentType = "text/plain";
				var bytes = Encoding.UTF8.GetBytes("Guru Meditation - check the log for stack trace");
				res.OutputStream.Write(bytes, 0, bytes.Length);
				return Response(res, 500);
			}
			throw new UnreachableException();
		}

		private bool Response(HttpListenerResponse response, int statusCode)
		{
			response.StatusCode = statusCode;
			response.Close();
			return true;
		}

		private string SanitizeContentType(string contentType)
		{
			if (string.IsNullOrWhiteSpace(contentType)) return "plain";

			// Grab everything before the semicolon (if there is one)
			var mimeType = contentType.Split(';')[0].Trim().ToLower();

			var bits = mimeType.Split('/', 3, StringSplitOptions.RemoveEmptyEntries);
			if (bits.Length < 2) return "plain";

			if (bits[0] != "application" && bits[0] != "text") return "unknown";

			return bits[1];
		}

		private string SanitizePath(Uri uri)
		{
			return uri.AbsolutePath.ToLower();
		}
	}
}
