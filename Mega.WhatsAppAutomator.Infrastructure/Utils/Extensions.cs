using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Raven.Client.Documents;

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
            return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        }

        /// <summary>
        /// Converts datetime from Utc to Brazilia time
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns>The converted datetime.</returns>
        public static DateTime ToBraziliaDateTime(this DateTime dateTime)
        {
            var timeZone = GetBraziliaTimeZone();
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime, timeZone);
        }

        private static TimeZoneInfo GetBraziliaTimeZone()
        {
            TimeZoneInfo braziliaTimeZone = null;
            try
            {
                braziliaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Brazil/East");
            }
            catch (Exception)
            {
                braziliaTimeZone = TimeZoneInfo.CreateCustomTimeZone("Brazilia", TimeSpan.FromHours(-3), "BraziliaTimeZone", "BraziliaTimeZoneStd");
            }

            return braziliaTimeZone;
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
            where T: class, new()
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
    }
}
