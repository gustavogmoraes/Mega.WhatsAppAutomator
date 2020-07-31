using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Mega.WhatsAppAutomator.Domain.Objects;
using Mega.WhatsAppAutomator.Infrastructure.Utils;
using Raven.Client.Documents;

namespace Mega.WhatsAppAutomator.Infrastructure.Persistence
{
    public static class Stores
    {
        private const string CertificateFileName = "free.gsoftware.client.certificate.with.password.pfx";
        private const string CertificatePassword = "8FF4A485E3D110558EF44DAA5347761E";

        private static string UrlCloud = EnvironmentConfiguration.DatabaseUrl;
        private static string MainDatabase = EnvironmentConfiguration.DatabaseName;

        private static string ClientDatabase => GetClientDatabaseName();

        private static string GetClientDatabaseName()
        {
            return string.Empty;
        }

        public static List<Client> Clients { get; set; }

        static Stores()
        {
            DocumentStores = new Dictionary<string, IDocumentStore>();
            
            using(var session = MegaWhatsAppApi.OpenSession())
            {
                Clients = session.Query<Client>().ToList();
            }
        }
        
        /// <summary>
        /// Gets main document store.
        /// </summary>
        /// <returns>Return this document store.</returns>
        public static IDocumentStore MegaWhatsAppApi =>
            DocumentStores.ContainsKey(MainDatabase)
                ? DocumentStores[MainDatabase]
                : CreateNewDocumentStore(MainDatabase);

        /// <summary>
        /// Gets automator document store for this client.
        /// </summary>
        /// <returns>Return this document store.</returns>
        public static IDocumentStore MegaWhatsAppAutomator => DocumentStores[MainDatabase] ?? CreateNewDocumentStore(ClientDatabase);

        /// <summary>
        /// The document stores.
        /// </summary>
        /// <value>Returns the document stores dictionaries.</value>
        private static Dictionary<string, IDocumentStore> DocumentStores { get; set; }

        #region Constructor
        
        #endregion

        #region Private Methods

        private static IDocumentStore CreateNewDocumentStore(string databaseName)
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var certificatePath = Path.Combine(baseDirectory, CertificateFileName);

            var documentStore = new DocumentStore
            {
                Urls = new[] { UrlCloud },
                Database = databaseName
            };

            documentStore.Certificate = new X509Certificate2(certificatePath, CertificatePassword);

            documentStore.Initialize();

            DocumentStores.Add(databaseName, documentStore);

            return documentStore;

        }

        #endregion
    }
}