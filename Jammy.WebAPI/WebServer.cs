using Jammy.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
	}

	public class WebServer : IWebServer
	{
		private readonly ILogger<WebServer> logger;
		private readonly HttpListener httpListener;
		private readonly Dictionary<string, Func<object>> getAction = new Dictionary<string, Func<object>>();
		private readonly Dictionary<string, Action<object>> putAction = new Dictionary<string, Action<object>>();
		private const string rootUrl = "jammy";

		public WebServer(ILogger<WebServer> logger, IServiceProvider serviceProvider)
		{
			this.logger = logger;

			ThreadPool.GetMaxThreads(out var workerThreads, out var completionPortThreads);
			logger.LogTrace($"ThreadPool size {workerThreads} {completionPortThreads}");

			Assembly assembly = Assembly.GetExecutingAssembly();

			var matchingClasses = assembly
				.GetTypes()
				.Where(t => t.IsClass)
				.Where(t => t.GetCustomAttributes(typeof(UrlPathAttribute), inherit: true).Any())
				.ToList();
			foreach (var clas in matchingClasses)
			{
				var path = clas.GetCustomAttribute<UrlPathAttribute>();
				var methods = clas
					.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
					.Where(m => m.GetCustomAttributes(typeof(UrlActionAttribute), false).Any())
					.ToList();

				var instance = ActivatorUtilities.CreateInstance(serviceProvider, clas);
				foreach (var method in methods)
				{
					var action = method.GetCustomAttribute<UrlActionAttribute>();

					string fullPath = $"/{rootUrl}/{path.Path}/{action.Path}";
					logger.LogTrace(fullPath);

					if (action.Action == "GET")
						getAction.Add(fullPath, () => method.Invoke(instance, null));
					else if (action.Action == "PUT")
						putAction.Add(fullPath, (o) => method.Invoke(instance, [o]));
				}
			}

			//netsh http add urlacl url=http://+:8080/ user=Everyone

			httpListener = new HttpListener();
			httpListener.Prefixes.Add("http://+:8080/");
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

		private void ProcessRequest(HttpListenerContext context, ILogger logger)
		{
			try
			{
				context.Response.AddHeader("Access-Control-Allow-Origin", "*");
				context.Response.AddHeader("Access-Control-Allow-Methods", "GET, PUT, POST, OPTIONS");
				context.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

				var serializer = new JsonSerializer();

				logger.LogTrace($"Request {context.Request.HttpMethod,8} {context.Request.Url.AbsolutePath}");

				if (context.Request.HttpMethod == "GET")
				{
					var action = this.getAction.GetValueOrDefault(context.Request.Url.AbsolutePath.ToLower());
					if (action == null)
					{
						context.Response.StatusCode = 404;
						context.Response.Close();
						return;
					}
					using var streamWriter = new StreamWriter(context.Response.OutputStream, Encoding.UTF8);// context.Response.ContentEncoding);
					using var jsonWriter = new JsonTextWriter(streamWriter);
					dynamic result = action();
					serializer.Serialize(jsonWriter, result);
					context.Response.StatusCode = 200;
					context.Response.ContentType = "application/json";
				}
				else if (context.Request.HttpMethod == "PUT")
				{
					var action = this.putAction.GetValueOrDefault(context.Request.Url.AbsolutePath.ToLower());
					if (action == null)
					{
						context.Response.StatusCode = 404;
						context.Response.Close();
						return;
					}
					using var streamReader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
					using var jsonReader = new JsonTextReader(streamReader);
					dynamic request = serializer.Deserialize(jsonReader);
					action(request);
					context.Response.StatusCode = 200;
				}
				else if(context.Request.HttpMethod == "OPTIONS") 
				{
					context.Response.StatusCode = 200;
				}
			}
			catch (Exception ex)
			{
				context.Response.StatusCode = 500;
				context.Response.ContentType = "text/plain";
				var responseString = ex.ToString();
				context.Response.OutputStream.Write(Encoding.UTF8.GetBytes(responseString, 0, responseString.Length));
			}
			context.Response.Close();
		}
	}
}
