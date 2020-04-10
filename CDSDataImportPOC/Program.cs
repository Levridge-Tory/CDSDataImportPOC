using System;
using Levridge.ODataDataSources.DynamicsCRM;
using Levridge.Microsoft.Dynamics.DataEntities;
using Levridge.ODataDataSources;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Levridge.EntityFramework;
using System.Net.Http;
using Levridge.EntityFramework.AuthenticationUtility;
using Levridge.AuthenticationUtility;
using Simple.OData.Client;

namespace CDSDataImportPOC
{



    class Program
    {
        enum ImportEntitiesPerFile
        {
            Single = 1,
            Multiple = 2
        }

        enum ImportMapType
        {
            Standard = 1,
            OutofBox = 2,
            InProcess = 3
        }

        enum ImportSourceType
        {
            SalesForceFullDataExport = 1,
            SalesForceReportExport = 2,
            SalesForceContactAndAccountReportExport = 3,
            Outlook2010BusinessContactManager = 4,
            GenericContactAndAccount = 5
        }

        enum ProcessCode
        {
            Process = 1,
            Ignore = 2,
            Internal = 3
        }

        enum ImportDataDelimiter
        {
            DoubleQuote = 1,
            None = 2,
            SingleQuote
        }

        enum ImportFieldDelimiter
        {
            Colon = 1,
            Comma = 2,
            Tab = 3,
            Semicolon = 4
        }

        enum ImportFileType
        {
            CSV = 0,
            XMLSpreadsheet2003 = 1,
            Attachment = 2,
            XLSX = 3
        }

        enum UpsertModeCode
        {
            Create = 0,
            Update = 1,
            Ignore = 2
        }

        enum ImportModeCode
        {
            Create = 0,
            Update = 1
        }

        public static async Task Main(string[] args)
        {
            Console.WriteLine("Create Payment Terms map");

            var importMap = new importmap()
            {
                name = "PaymentTerm->lev_paymentterm",
                source = "Terms of payment.xlsx",
                description = "FinOps PaymentTerm to CDS lev_paymentterm",
                entitiesperfile = (Int32)ImportEntitiesPerFile.Single,
                isvalidforimport = true
                //importmapid = Guid.NewGuid(),
                //importmaptype = (Int32) ImportMapType.Standard,
                //iswizardcreated = false,
                //sourcetype = (Int32)ImportSourceType.GenericContactAndAccount
            };
            // create to get GUID on importMap

            var entity = Program.CreateEntity<importmap>(importMap,
                new EntityFieldDefinition()
                {
                    DotNetType = typeof(importmap),
                    NameFieldName = nameof(importmap.name),
                    IdFieldName = nameof(importmap.importmapid),
                    SyncIdFieldName = nameof(importmap.name),
                    CreatedVersionFieldName = nameof(importmap.createdon),
                    ModifiedVersionFieldName = nameof(importmap.modifiedon)
                });

            var ds = await Program.CreateDataSourceAsync(entity).ConfigureAwait(false);
            entity =  await Program.ReadOrCreateEntity(ds, entity);
            importMap = entity.GetEntityAsDotNetType<importmap>();

            entity = await CreateColumnMappingEntity(ds, importMap,
                nameof(PaymentTerm),
                nameof(PaymentTerm.Description),
                nameof(lev_paymentterm),
                nameof(lev_paymentterm.lev_description));

            entity = await CreateColumnMappingEntity(ds, importMap,
                nameof(PaymentTerm),
                nameof(PaymentTerm.Name),
                nameof(lev_paymentterm),
                nameof(lev_paymentterm.lev_name));

            entity = await CreateColumnMappingEntity(ds, importMap,
                nameof(PaymentTerm),
                nameof(PaymentTerm.Name),
                nameof(lev_paymentterm),
                nameof(lev_paymentterm.lev_paymenttermcode));

            var importEntity = new import()
            {
                name = "lev_paymentterm import",
                emailaddress = "tory@levridge.com",
                sendnotification = true,
                modecode = (Int32)ImportModeCode.Create
            };

            entity = Program.CreateEntity<import>(importEntity,
            new EntityFieldDefinition()
            {
                DotNetType = typeof(import),
                NameFieldName = nameof(importEntity.name),
                IdFieldName = nameof(importEntity.importid),
                SyncIdFieldName = nameof(importEntity.name),
                CreatedVersionFieldName = nameof(importEntity.createdon),
                ModifiedVersionFieldName = nameof(importEntity.modifiedon)
            });
            ds.Entity = entity;
            entity = await Program.ReadOrCreateEntity(ds, entity);
            importEntity = entity.GetEntityAsDotNetType<import>();

            var importSourceFile = new importfile()
            {
                datadelimitercode = (Int32)ImportDataDelimiter.None,
                enableduplicatedetection = true,
                fielddelimitercode = (Int32)ImportFieldDelimiter.Comma,
                isfirstrowheader = true,
                filetypecode = (Int32)ImportFileType.CSV,
                name = "Terms of payment.csv",
                processcode = (Int32)ProcessCode.Process,
                upsertmodecode = (Int32)UpsertModeCode.Create,
                // importSourceFile.size = ?
                source = "Terms of payment.csv",
                sourceentityname = nameof(PaymentTerm),
                targetentityname = nameof(lev_paymentterm),
                importid = importEntity,
                importmapid = importMap,
                usesystemmap = false
            };
            importSourceFile.content = await ReadCsvFileAsync(
                Path.Combine(Directory.GetCurrentDirectory(), importSourceFile.source)).ConfigureAwait(false);

            entity = Program.CreateEntity<importfile>(importSourceFile,
            new EntityFieldDefinition()
            {
                DotNetType = typeof(importfile),
                NameFieldName = nameof(importfile.name),
                IdFieldName = nameof(importfile.importfileid),
                SyncIdFieldName = nameof(importfile.importfileid),
                CreatedVersionFieldName = nameof(importfile.createdon),
                ModifiedVersionFieldName = nameof(importfile.modifiedon)
            });
            ds.Entity = entity;
            entity = await Program.ReadOrCreateEntity(ds, entity);

            // entity = await ds.CreateEntityAsync(entity).ConfigureAwait(false);
            Console.WriteLine("Created Import Job.");
            Console.WriteLine("Begin Parsing ...");

            // Create client
            Uri uri = new Uri(Configuration.GetConnectionString("ODataEntityPath"), UriKind.Absolute);
            String connectionString = uri.ToString();

            var clientSettings = new ODataClientSettings(uri)
            {
                IncludeAnnotationsInResults = true,
                BeforeRequest = Program.BeforeRequest,
                IgnoreUnmappedProperties = true,
            };

            var client = new ODataClient(clientSettings);
            await client.For<import>()
                .Key(importEntity.importid)
                .Action("ParseImport")
                .Set(importEntity)
                .ExecuteAsSingleAsync();
            

            Console.WriteLine("completed processing.");
            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
        }

        private static ClientConfiguration CreateClientConfiguration(IConfiguration configuration)
        {
            var odataConfiguration = new ODataClientConfiguration(configuration);
            return new ClientConfiguration()
            {
                UriString = odataConfiguration.UriString,
                ActiveDirectoryResource = odataConfiguration.ActiveDirectoryResource,
                ActiveDirectoryTenant = odataConfiguration.ActiveDirectoryTenant,
                ActiveDirectoryClientAppId = odataConfiguration.ActiveDirectoryClientAppId,
                ActiveDirectoryClientAppSecret = odataConfiguration.ActiveDirectoryClientAppSecret,
                ODataEntityPath = odataConfiguration.ODataEntityPath
            };
        }

        private static void BeforeRequest(HttpRequestMessage request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            Program.HeaderDelegate.AddRequestAuthenticationHeader(request);

            // Add preference for annotations
            if (request.Headers.TryGetValues("Prefer", out IEnumerable<String> headerValues) == true)
            {
                // If Simple.OData is asking for annoations (Prefer: return= representation)
                // then use the CRM odata.include-annotations method for getting annotations
                if ((headerValues.Contains("return=representation") == true)
                    && (headerValues.Contains("odata.include-annotations=\"*\"") == false))
                {
                    request.Headers.Add("Prefer", "odata.include-annotations=\"*\"");
                }
            }

            // Add Hack for non-support for quoted key values
            String path = System.Web.HttpUtility.UrlDecode(request.RequestUri.AbsolutePath)
                .Replace("('", "(")
                .Replace("')", ")");
            var parsedQuery = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query);
            var uriBuilder = new UriBuilder(request.RequestUri)
            {
                Path = path,
                Query = parsedQuery.ToString()
            };
            request.RequestUri = uriBuilder.Uri;

            Console.WriteLine($"request being sent to:\n{request.RequestUri.ToString()}:");
            Console.WriteLine($"request content being sent:\n{request.Content}:");
        }

        private static async Task<Entity> CreateColumnMappingEntity(CRMSimpleODataDataSource ds,
        importmap importMap, 
        String sourceEntityName,
        String sourceAttributeName,
        String targetEntityName,
        String targetAttributeName)
        {
            var importMapDescriptionMapping = importMap.ColumnMapping_ImportMap.Where(cm =>
                cm.sourceentityname == sourceEntityName &&
                cm.sourceattributename == sourceAttributeName &&
                cm.targetentityname == targetEntityName &&
                cm.targetattributename == targetAttributeName);

            // either get the existing column mapping or create a new column mapping
            Boolean columnMappingExists = importMap.ColumnMapping_ImportMap.Count > 0 &&
                importMapDescriptionMapping.Count() > 0;

            var columnMapping = columnMappingExists ? importMapDescriptionMapping.First() :
                new columnmapping() // https://docs.microsoft.com/en-us/dynamics365/customer-engagement/web-api/columnmapping?view=dynamics-ce-odata-9
                {
                    sourceentityname = sourceEntityName,
                    sourceattributename = sourceAttributeName,
                    targetentityname = targetEntityName,
                    targetattributename = targetAttributeName,
                    processcode = (Int32)ProcessCode.Process,
                    importmapid = importMap
                };
            var keys = new List<IEnumerable<String>>();
            keys.Add(new List<String>() { nameof(columnmapping.sourceattributename), nameof(columnmapping.targetattributename) });
            var entity = Program.CreateEntity<columnmapping>(columnMapping,
                        new EntityFieldDefinition()
                        {
                            DotNetType = typeof(columnmapping),
                            NameFieldName = nameof(columnmapping.sourceattributename),
                            IdFieldName = nameof(columnmapping.columnmappingid),
                            SyncIdFieldName = nameof(columnmapping.columnmappingid),
                            CreatedVersionFieldName = nameof(columnmapping.createdon),
                            ModifiedVersionFieldName = nameof(columnmapping.modifiedon)
                        }, keys);
            if(columnMappingExists == false)
            {
                ds.Entity = entity;
                entity = await Program.ReadOrCreateEntity(ds, entity);
            }

            return entity;
        }

        private static async Task<Entity> ReadOrCreateEntity(CRMSimpleODataDataSource ds, Entity entity)
        {
            try
            {
                Entity readEntity = null;
                if(null != entity.ID || null != entity.SyncID)
                {
                    readEntity = ds.ReadEntity(entity);
                }
                else if(entity.Keys.Count() > 1 && 
                    entity.Keys.ElementAt(1).KeyValues.All(v => null != v.Value && !Program.ValueIsDefault(v.Value)))
                {
                    readEntity = ds.ReadEntityByKey(entity, entity.Keys.ElementAt(1).KeyName);
                }
                if (null != readEntity)
                {
                    entity = readEntity;
                }
                else
                {
                    entity = await ds.CreateEntityAsync(entity).ConfigureAwait(false);
                }
                return entity;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.GetType().Name} exception thrown.");
                Console.WriteLine($"Message: {ex.Message}");
                throw;
            }            
        }

        private static Boolean ValueIsDefault<T>(T value)
        {
            // from https://stackoverflow.com/questions/1895761/test-for-equality-to-the-default-value
            return EqualityComparer<T>.Default.Equals(value, default(T));
        }


        public static IConfiguration Configuration
        {
            get;
        } = Program.LoadConfig();

        public static List<String> CommonEntitySystemFields
        {
            get;
        } = new List<String>()
        {
            "utcconversiontimezonecode",
            "owninguser",
            "modifiedonbehalfby",
            "owningteam",
            "createdonbehalfby",
            "owningbusinessunit",
            "recordsownerid_systemuser",
            "ownerid",
            "modifiedby",
            "createdby",
            "timezoneruleversionnumber"
        };


        public static List<String> EntitySpecificSystemFieldSuffixes
        {
            get;
        } = new List<String>()
        {
            "_AsyncOperations",
            "_BulkDeleteFailures",
            "_SyncErrors"
        };

        private static OAuthHeaderDelegate HeaderDelegate
        {
            get;
        } = new OAuthHeaderDelegate(Program.CreateClientConfiguration(Program.Configuration));


        private static IConfiguration LoadConfig()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("ODataConfig.json");

            return builder.Build();
        }

        private static async Task<CRMSimpleODataDataSource> CreateDataSourceAsync(Levridge.EntityFramework.Entity entity)
        {
            var ds = new CRMSimpleODataDataSource(entity, Program.Configuration);
            var awaiter = ds.ConnectAsync();
            await Task.WhenAll(awaiter).ConfigureAwait(false);
            return ds;
        }

        private static Levridge.EntityFramework.Entity CreateEntity<T>(T dotNetObject,
            EntityFieldDefinition entityFieldDefinition, IEnumerable<IEnumerable<String>> alternateKeys = null)
        {
            var entity = new Levridge.EntityFramework.Entity(entityFieldDefinition.DotNetType.Name)
            {
                DotNetType = entityFieldDefinition.DotNetType,
                IDFieldName = entityFieldDefinition.IdFieldName,
                NameFieldName = entityFieldDefinition.NameFieldName,
                SyncIDFieldName = entityFieldDefinition.SyncIdFieldName,
                CreatedVersionFieldName = entityFieldDefinition.CreatedVersionFieldName,
                ModifiedVersionFieldName = entityFieldDefinition.ModifiedVersionFieldName
            };

            var ExcludeFieldList = new List<String>(
                Program.CommonEntitySystemFields.Concat(
                Program.EntitySpecificSystemFieldSuffixes
                .Select(f => entityFieldDefinition.DotNetType.Name + f)));

            entity.CopyValuesFromODataEntity(typeof(CRMODataField<>), 
                dotNetObject, 
                entityFieldDefinition.DotNetType.GetProperties()
                .Where(p => p.Name.StartsWith('_') == false && ExcludeFieldList.Contains(p.Name, StringComparer.OrdinalIgnoreCase) == false)
                .Select(p => p.Name));

            if (alternateKeys?.Count() > 0)
            {
                foreach (var key in alternateKeys)
                {
                    entity.AddKey(new EntityKey(entity, String.Concat(key), key));
                }
            }

            return entity;
        }

        private static async Task<string> ReadCsvFileAsync(string filePath)
        {
            // StringBuilder data = new StringBuilder();
            using (StreamReader reader = new StreamReader(filePath))
            {
                return await reader.ReadToEndAsync();
            }
        }


    }

    struct EntityFieldDefinition
    {
        public System.Type DotNetType;
        public String IdFieldName;
        public String NameFieldName;
        public String SyncIdFieldName;
        public String CreatedVersionFieldName;
        public String ModifiedVersionFieldName;
    }
}
