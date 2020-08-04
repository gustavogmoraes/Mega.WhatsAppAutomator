using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Raven.Client.Documents;
using Raven.Client.Documents.Commands.Batches;
using Raven.Client.Documents.Session;
using Sparrow.Json;
using Sparrow.Json.Parsing;

namespace Mega.WhatsAppAutomator.Infrastructure.Utils
{
	public static class Extensions
	{
		/// <summary>
		/// Gets a string content to send a rest request.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static StringContent GetStringContent(this object obj)
		{
			var jsonContent = JsonConvert.SerializeObject(obj);

			var contentString = new StringContent(jsonContent, Encoding.UTF8, "application/json");
			contentString.Headers.ContentType = new MediaTypeHeaderValue("application/json");

			return contentString;
		}

		/// <summary>
		/// Gets a dynamic object from a string content result.
		/// </summary>
		/// <param name="resultString"></param>
		/// <returns></returns>
		public static dynamic GetStringContentResult(this string resultString)
		{
			return (dynamic)JsonConvert.DeserializeObject<ExpandoObject>(resultString, new ExpandoObjectConverter());
		}

		/// <summary>
		/// Returns a secure string.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static SecureString ToSecureString(this string str)
		{
			var secureString = new SecureString();
			str.ToList().ForEach(x => secureString.AppendChar(x));

			return secureString;
		}

		/// <summary>s        
		/// Check if enviroment is development.
		/// </summary>
		/// <returns></returns>
		public static bool EnvironmentIsDevelopment()
		{
			return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT").StartsWith("Development");
		}

		/// <summary>
		/// Converts datetime from Utc to Brazilia time
		/// </summary>
		/// <param name="dateTime"></param>
		/// <returns>The converted datetime.</returns>
		public static DateTime ToBraziliaDateTime(this DateTime dateTime)
		{
			return TimeZoneInfo.ConvertTimeFromUtc(dateTime, BraziliaTimeZone);
		}

		private static TimeZoneInfo BraziliaTimeZone { get; set; }

		static Extensions()
		{
			BraziliaTimeZone = TimeZoneInfo.CreateCustomTimeZone("Brazilia", TimeSpan.FromHours(-3), "BraziliaTimeZone", "BraziliaTimeZoneStd");
		}

		public static T[] ArrayAdd<T>(this T[] array, T item)
		{
			var list = array.ToList();
			list.Add(item);

			return list.ToArray();
		}

		public static IEnumerable<string> SplitOnChunks(this string str, int chunkSize)
		{
			return Enumerable.Range(0, str.Length / chunkSize)
				.Select(i => str.Substring(i * chunkSize, chunkSize));
		}

		public static void MassInsert<T>(this IDocumentStore store, IList<T> list, bool processLoopOnDatabase = false)
			where T : class, new()
		{
			if (processLoopOnDatabase)
			{
				using (var bulkInsert = store.BulkInsert())
				{
					foreach (var item in list)
					{
						bulkInsert.Store(item);
					}
				}
				return;
			}

			using (var session = store.OpenSession())
			{
				list.ToList().ForEach(item => session.Store(item));
				session.SaveChanges();
			}
		}

		public static T Random<T>(this IEnumerable<T> input)
		{
			var random = new Random();
			var list = input.ToList();

			return list.ElementAt(random.Next(0, list.Count));
		}

		public static string TimeSpanToReport(this TimeSpan ts)
		{
			return new DateTime(ts.Ticks).ToString("mm:ss");
		}

		public static void SaveStreamAsFile(this Stream inputStream, string filePath)
		{
			using FileStream outputFileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite);

			inputStream.CopyTo(outputFileStream);
		}

		public static string GetReadableFileSize(this long length)
		{
			string[] sizes = { "B", "KB", "MB", "GB", "TB" };
			double len = Convert.ToDouble(length);
			int order = 0;
			while (len >= 1024 && order < sizes.Length - 1)
			{
				order++;
				len = len / 1024;
			}

			// Adjust the format string to your preferences. For example "{0:0.#}{1}" would
			// show a single decimal place, and no space.
			return string.Format("{0:0.##} {1}", len, sizes[order]);
		}

		public static void BulkUpdate<T>(this IDocumentSession session, IList<T> items)
			where T : class, IRavenDbDocument, new()
		{
			var batchCommand = new BatchCommand(
				session.Advanced.DocumentStore.Conventions,
				JsonOperationContext.ShortTermSingleUse(),
				items.Select(x => new PutCommandData(
						x.Id,
						null,
						DynamicJsonValue.Convert(x.ToDictionary<object>())))
					.OfType<ICommandData>().ToList());

			session.Advanced.RequestExecutor.Execute(batchCommand, session.Advanced.Context);
		}

		public static Dictionary<string, TValue> ToDictionary<TValue>(this object obj)
		{
			var json = JsonConvert.SerializeObject(obj);
			var dictionary = JsonConvert.DeserializeObject<Dictionary<string, TValue>>(json);

			return dictionary;
		}
	}
}
