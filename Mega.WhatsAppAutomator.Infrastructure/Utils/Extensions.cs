using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Mega.WhatsAppAutomator.Domain.Interfaces;
using Mega.WhatsAppAutomator.Domain.Objects;
using Mega.WhatsAppAutomator.Infrastructure.DevOps;
using Mega.WhatsAppAutomator.Infrastructure.PupeteerSupport;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Raven.Client.Documents;
using Raven.Client.Documents.Commands.Batches;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Queries;
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
		
		public static string RemoveDateConvertingToString(this DateTime dateTime)
		{
			return dateTime.ToString("hh:mm:ss");
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
				using var bulkInsert = store.BulkInsert();
				foreach (var item in list)
				{
					bulkInsert.Store(item);
				}

				return;
			}

			using var session = store.OpenSession();
			foreach (var item in list.ToList())
			{
				session.Store(item);
			}
			
			session.SaveChanges();
		}

		public static T Random<T>(this IEnumerable<T> input)
		{
			var random = new Random();
			var list = input.ToList();

			return list.ElementAt(random.Next(0, list.Count));
		}

		private static string GetPaddedTimeSpan(this TimeSpan ts, int widht = 3)
		{
			return ts.Milliseconds < 2 
				? 1.ToString().PadLeft(widht, '0') 
				: ts.Milliseconds.ToString().PadLeft(widht, '0');
		}

		public static string TimeSpanToReport(this TimeSpan ts, bool considerMs = false)
		{
			var stringResult = new DateTime(ts.Ticks).ToString("mm:ss");
			var msToReport = considerMs ? $":{ts.GetPaddedTimeSpan()}" : string.Empty;
			
			return stringResult + msToReport;
		}

		public static void SaveStreamAsFile(this Stream inputStream, string filePath)
		{
			using var outputFileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite);

			inputStream.CopyTo(outputFileStream);
		}

		public static string GetReadableFileSize(this long length)
		{
			string[] sizes = { "B", "KB", "MB", "GB", "TB" };
			var len = Convert.ToDouble(length);
			var order = 0;
			
			while (len >= 1024 && order < sizes.Length - 1)
			{
				order++;
				len /= 1024;
			}

			// Adjust the format string to your preferences. For example "{0:0.#}{1}" would
			// show a single decimal place, and no space.
			return $"{len:0.##} {sizes[order]}";
		}

		public static void BulkUpdate<T>(this IDocumentStore documentStore, IList<T> items)
			where T : class, IRavenDbDocument, new()
		{
			throw new NotImplementedException();
		}
		
		public static void BulkUpdate<T>(this IDocumentStore documentStore, IList<T> items, Expression<Func<T, object>> propertyToChange, object valueToBePut)
			where T : class, IRavenDbDocument, new()
		{
			var collectionName = typeof(T).Name + "s";
			var propertyInfo = (PropertyInfo)GetPropertyFromExpression(propertyToChange);
			var ids = "'" + string.Join("', '", items.Select(x => x.Id)) + "'";
			
			var query =
				$@"from {collectionName} as x 
				   where id() in ({ids}) 
				   update 
				   {{	
						x.{propertyInfo.Name} = {valueToBePut.ToString()?.ToLowerInvariant()}; 
				   }}";
			
			var operation = documentStore
				.Operations
				.Send(new PatchByQueryOperation(new IndexQuery { Query = query }));

			operation.WaitForCompletion();
		}
		
		public static MemberInfo GetPropertyFromExpression(this LambdaExpression propertyLambda)
		{
			MemberExpression Exp = null;

			//this line is necessary, because sometimes the expression comes in as Convert(originalExpression)
			if (propertyLambda.Body is UnaryExpression)
			{
				var UnExp = (UnaryExpression)propertyLambda.Body;
				if (UnExp.Operand is MemberExpression)
				{
					Exp = (MemberExpression)UnExp.Operand;
				}
				else
					throw new ArgumentException();
			}
			else if (propertyLambda.Body is MemberExpression)
			{
				Exp = (MemberExpression)propertyLambda.Body;
			}
			else
			{
				throw new ArgumentException();
			}

			return Exp.Member;
		}

		public static Dictionary<string, TValue> ToDictionary<TValue>(this object obj)
		{
			var json = JsonConvert.SerializeObject(obj);
			var dictionary = JsonConvert.DeserializeObject<Dictionary<string, TValue>>(json);

			return dictionary;
		}

		public static async Task<T> ExecuteWithLogsAsync<T>(Func<Task<T>> function, string log = null, Func<string> delegated = null)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var result = await function.Invoke();
			stopwatch.Stop();
			
			WriteOnConsole(
				$"At {DateTime.UtcNow.ToBraziliaDateTime()} {log ?? string.Empty}"
				.Replace("{totalTime}", stopwatch.Elapsed.TimeSpanToReport()));
			
			return result;
		}

		public static void GetPermission(this DirectoryInfo baseDirectoryInfo)
		{
			if (DevOpsHelper.GetOsPlatform() == OSPlatform.Windows)
			{
				// Remove read only
				baseDirectoryInfo.Attributes &= ~FileAttributes.ReadOnly;
				
				// Get permission
				SetFullControlPermissionsToEveryone(baseDirectoryInfo.FullName);
				return;
			}
			
			DevOpsHelper.Bash($"chmod 755 {baseDirectoryInfo.FullName}");
		}
		
		private static void SetFullControlPermissionsToEveryone(string path)
		{
			const FileSystemRights rights = FileSystemRights.FullControl;

			var allUsers = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);

			// Add Access Rule to the actual directory itself
			var accessRule = new FileSystemAccessRule(
				allUsers,
				rights,
				InheritanceFlags.None,
				PropagationFlags.NoPropagateInherit,
				AccessControlType.Allow);

			var info = new DirectoryInfo(path);
			var security = info.GetAccessControl(AccessControlSections.Access);

			bool result;
			security.ModifyAccessRule(AccessControlModification.Set, accessRule, out result);

			if (!result)
			{
				throw new InvalidOperationException("Failed to give full-control permission to all users for path " + path);
			}

			// add inheritance
			var inheritedAccessRule = new FileSystemAccessRule(
				allUsers,
				rights,
				InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
				PropagationFlags.InheritOnly,
				AccessControlType.Allow);

			bool inheritedResult;
			security.ModifyAccessRule(AccessControlModification.Add, inheritedAccessRule, out inheritedResult);

			if (!inheritedResult)
			{
				throw new InvalidOperationException("Failed to give full-control permission inheritance to all users for " + path);
			}

			info.SetAccessControl(security);
		}

		public static void DeleteAllBut(this DirectoryInfo baseDirectoryInfo, IList<string> exceptions)
		{
			foreach (var fileInfo in baseDirectoryInfo.GetFiles())
			{
				if (!exceptions.Contains(fileInfo.FullName))
				{
					fileInfo.Delete();
				}
			}
			
			foreach (var directoryInfo in baseDirectoryInfo.GetDirectories())
			{
				if (!exceptions.Contains(directoryInfo.FullName))
				{
					directoryInfo.Delete(recursive: true);
				}
			}
		}
		
		public static void ExecuteWithElapsedTime(Action taskToExecute, out TimeSpan elapsedTime)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			taskToExecute.Invoke();
			stopwatch.Stop();

			elapsedTime = stopwatch.Elapsed;
		}
		
		public static T ExecuteWithElapsedTime<T>(Func<T> taskToExecute, out TimeSpan elapsedTime)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var result = taskToExecute.Invoke();
			stopwatch.Stop();
			
			elapsedTime = stopwatch.Elapsed;

			return result;
		}
		
		public static void ClearCurrentConsoleLine()
		{
			Console.SetCursorPosition(0, Console.CursorTop - 1);
				
			int currentLineCursor = Console.CursorTop;
			Console.SetCursorPosition(0, Console.CursorTop);
			Console.Write(new string(' ', Console.WindowWidth)); 
			Console.SetCursorPosition(0, currentLineCursor);
		}
		
		public static string LastWrittenLine { get; set; }
		
		public static DateTime LastTimeThatIdled { get; set; }
		
		public static void WriteOnConsole(string line)
		{
			LastWrittenLine = line;
			Console.WriteLine(line);
		}

		public static string NumberToReport(this string number)
		{
			number = number.Replace("-", string.Empty);
			number = number.Trim();
			
			var countryCode = number.Substring(0, 3);
			var areaCode = number.Substring(3, 2);
			
			var realNumber = number.Split(countryCode + areaCode).Last();
			if (number.ContainsBrazilian9ThDigit())
			{
				return $"{countryCode} {areaCode} {realNumber.Insert(5, "-")}";
			}

			number = number.InsertBrazilian9ThDigit();
			realNumber = number.Split(countryCode + areaCode).Last();
			return $"{countryCode} {areaCode} {realNumber.Insert(5, "-")}";
		}

		public static bool ContainsBrazilian9ThDigit(this string number)
		{
			return number.Length > 13 && number[6] == '9';
		}

		public static string RemoveBrazilian9ThDigit(this string number)
		{
			return number.Remove(5, 1);
		}
        
		public static string InsertBrazilian9ThDigit(this string number)
		{
			return number.Insert(5, "9");
		}
	}
}
