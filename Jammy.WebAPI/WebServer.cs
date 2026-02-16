using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;

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
				.Where(t => t.GetCustomAttributes(typeof(UrlPathAttribute), inherit: true).Any());

			foreach (var clas in matchingClasses)
			{
				var path = clas.GetCustomAttribute<UrlPathAttribute>();
				var methods = clas
					.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
					.Where(m => m.GetCustomAttributes(typeof(UrlActionAttribute), false).Any());

				var instance = ActivatorUtilities.CreateInstance(serviceProvider, clas);
				foreach (var method in methods)
				{
					var action = method.GetCustomAttribute<UrlActionAttribute>();

					string fullPath = $"/{rootUrl}/{path.Path}/{action.Path}";
					logger.LogTrace(fullPath);

					if (action.Action == "GET")
						getActions.Add(fullPath, () => method.Invoke(instance, null));
					else if (action.Action == "PUT" || action.Action == "POST")
						putActions.Add(fullPath, (o) => method.Invoke(instance, [o]));
				}
			}

			httpListener.Start();
			httpListener.BeginGetContext(ListenerCallback, null);
		}

		private void ListenerCallback(IAsyncResult ar)
		{
			if (!httpListener.IsListening) return;

			HttpListenerContext context = null;
			try
			{
				context = httpListener.EndGetContext(ar);
			}
			catch (Exception ex)
			{
				logger.LogTrace($"EndGetContext crashed {ex}");
				return;
			}

			httpListener.BeginGetContext(ListenerCallback, null);
			ThreadPool.QueueUserWorkItem(_ => ProcessRequest(context, logger));
		}

		private bool ProcessRequest(HttpListenerContext context, ILogger logger)
		{
			try
			{
				//CORS
				context.Response.AddHeader("Access-Control-Allow-Origin", "*");
				context.Response.AddHeader("Access-Control-Allow-Methods", "GET, PUT, POST, OPTIONS");
				context.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

				//cache control
				context.Response.AddHeader("Cache-Control", "no-cache, no-store, must-revalidate");

				if (context.Request.HttpMethod == "GET")
				{
					var action = getActions.GetValueOrDefault(SanitizePath(context.Request.Url));
					if (action == null) return Response(context.Response, 404);

					using var streamWriter = new StreamWriter(context.Response.OutputStream, Encoding.UTF8);
					using (var jsonWriter = new JsonTextWriter(streamWriter))
					{
						serializer.Serialize(jsonWriter, action());
					}
					context.Response.ContentType = "application/json";
					return Response(context.Response, 200);
				}
				else if (context.Request.HttpMethod == "PUT" || context.Request.HttpMethod == "POST")
				{
					var action = putActions.GetValueOrDefault(SanitizePath(context.Request.Url));
					if (action == null) return Response(context.Response, 404);

					using var streamReader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
					
					switch (SanitizeContentType(context.Request.ContentType)) 
					{ 
						case "json":
							using (var jsonReader = new JsonTextReader(streamReader))
							{ 
								object request = serializer.Deserialize(jsonReader);
								action(request);
							}
							return Response(context.Response, 200);

						case "plain":
							var text  = streamReader.ReadToEnd();
							action(text);
							return Response(context.Response, 200);

						default:
							return Response(context.Response, 415);
					}
				}
				else if (context.Request.HttpMethod == "OPTIONS") 
				{
					return Response(context.Response, 200);
				}
				else
				{
					return Response(context.Response, 400);
				}
				throw new UnreachableException();
			}
			catch (Exception ex)
			{
				context.Response.ContentType = "text/plain";
				var responseString = ex.ToString();
				context.Response.OutputStream.Write(Encoding.UTF8.GetBytes(responseString, 0, responseString.Length));
				return Response(context.Response, 500);
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
			
			contentType = contentType.ToLower();

			var bits = contentType.Split('/',3,StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
			if (bits.Length == 0) return "plain";//no contentType, assume text
			if (bits[0] != "application" && bits[0] != "text") return "unknown";
			return bits[1];
		}

		private string SanitizePath(Uri uri)
		{
			return uri.AbsolutePath.ToLower();
		}
	}
}
