using System;
using Levridge.ODataDataSources.DynamicsCRM;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CDSDataImportPOC
{

    class Program
    {

        public static async Task Main(string[] args)
        {
            if(args.Length == 0)
            {
                Console.WriteLine("You must provide a JSON file name to load.");
                throw new ArgumentNullException(nameof(args));
            }
            Console.WriteLine("Create Payment Terms map");

            var builder = Host.CreateDefaultBuilder(args)
                // should load appsettings.json by default
                // .ConfigureAppConfiguration(builder => builder.AddConfiguration(Configuration))
                // should add console logging by default
                //.ConfigureLogging(logging =>
                //{
                //    logging.AddConsole();
                //})
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHttpClient();
                    services.AddSingleton<IODataClientService, ODataClientService>();
                }).UseConsoleLifetime();

            var host = builder.Build();

            using (var serviceScope = host.Services.CreateScope())
            {
                var services = serviceScope.ServiceProvider;

                try
                {
                    IntegrationJob integrationJob = null;
                    // Load file
                    using (FileStream fs = File.OpenRead(args[0]))
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        };
                        options.Converters.Add(new JsonStringEnumConverter());
                        integrationJob = await JsonSerializer.DeserializeAsync<IntegrationJob>(fs, options);
                    }
                    // importmap
                    ImportHandlerService importHandler = new ImportHandlerService($"PaymentTerm->lev_paymentterm{DateTime.Now}",
                        "FinOps PaymentTerm to CDS lev_paymentterm",
                        "Terms of payment",
                        nameof(lev_paymentterm),
                        ImportEntitiesPerFile.Single);

                    importmap importmap = await importHandler.CreateImportMapAsync();

                    // column mappings
                    await importHandler.CreateColumnMappingEntityAsync("NAME", nameof(lev_paymentterm.lev_name)).ConfigureAwait(false);
                    await importHandler.CreateColumnMappingEntityAsync("DESCRIPTION", nameof(lev_paymentterm.lev_description)).ConfigureAwait(false);
                    await importHandler.CreateColumnMappingEntityAsync("NAME", nameof(lev_paymentterm.lev_paymenttermcode)).ConfigureAwait(false);

                    // importentitymapping
                    //var importEntityMapping = new importentitymapping()
                    //{
                    //    targetentityname = "lev_paymentterm",
                    //    sourceentityname = "Terms of payment",
                    //    importmapid = importMap,
                    //    componentstate = (Int32)ImportEntityMappingComponentState.Published,
                    //    processcode = (Int32)ProcessCode.Process,
                    //    dedupe = (Int32)ImportEntityMappingDedupe.Ignore
                    //};
                    //entity = Program.CreateEntity<importentitymapping>(importEntityMapping,
                    //new EntityFieldDefinition()
                    //{
                    //    DotNetType = typeof(importentitymapping),
                    //    NameFieldName = nameof(importentitymapping.targetentityname),
                    //    IdFieldName = nameof(importentitymapping.importentitymappingid),
                    //    SyncIdFieldName = nameof(importentitymapping.importentitymappingid),
                    //    CreatedVersionFieldName = nameof(importentitymapping.createdon),
                    //    ModifiedVersionFieldName = nameof(importentitymapping.modifiedon)
                    //});
                    //ds.Entity = entity;
                    //entity = await Program.CreateEntityAsync(ds, entity);
                    //importEntityMapping = entity.GetEntityAsDotNetType<importentitymapping>();

                    var importEntity = await importHandler.CreateImportAsync("tory@levridge.com",
                        ImportModeCode.Create, true);

                    var importMap = await importHandler.CreateImportFileAsync(fileName: "Terms of payment.csv",
                        path: Directory.GetCurrentDirectory(),
                        firstRowHeader: true,
                        dataDelimiter: ImportDataDelimiter.None,
                        fieldDelimiter: ImportFieldDelimiter.Comma,
                        fileType: ImportFileType.CSV,
                        enableDuplicateDetection: true,
                        upsertMode: UpsertModeCode.Create
                        );

                    // entity = await ds.CreateEntityAsync(entity).ConfigureAwait(false);
                    Console.WriteLine("Created Import Job.");
                    Console.WriteLine("Begin Parsing ...");

                    var odataClientService = services.GetRequiredService<IODataClientService>();
                    // <Action Name = "ParseImport" IsBound = "true">
                    // <Parameter Name = "entity" Type = "mscrm.import" Nullable = "false" />
                    // <ReturnType Type = "mscrm.asyncoperation" Nullable = "false" />
                    // </Action>
                    var updatedImport = await odataClientService.ExecuteBoundActionAsync<import>(importEntity, importEntity.importid.Value, "ParseImport");

                    // validate that we completed succesfully

                    // Execute transformation
                    var parameters = new Dictionary<String, System.Object>();
                    parameters.Add("ImportId", importEntity.importid.Value);

                    await odataClientService.ExecuteUnboundActionAsync(parameters, "TransformImport");

                    // validate that transformation was succesful
                    
                    updatedImport = await odataClientService.ExecuteBoundActionAsync<import>(importEntity, importEntity.importid.Value, "ImportRecordsImport");

                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();

                    logger.LogError(ex, "An error occurred.");
                }
            }

            Console.WriteLine("completed processing.");
            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
        }


        //private static async Task<Entity> ReadOrCreateEntityAsync(CRMSimpleODataDataSource ds, Entity entity)
        //{
        //    try
        //    {
        //        Entity readEntity = null;
        //        if(null != entity.ID || null != entity.SyncID)
        //        {
        //            readEntity = ds.ReadEntity(entity);
        //        }
        //        else if(entity.Keys.Count() > 1 && 
        //            entity.Keys.First(k => k.KeyName.Contains("PK") == false)
        //            .KeyValues.All(v => null != v.Value && !Program.ValueIsDefault(v.Value)))
        //        {
        //            readEntity = ds.ReadEntityByKey(entity, entity.Keys.First(k => k.KeyName.Contains("PK") == false).KeyName);
        //        }
        //        if (null != readEntity)
        //        {
        //            entity = readEntity;
        //        }
        //        else
        //        {
        //            entity = await CreateEntityAsync(ds, entity);
        //        }
        //        return entity;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"{ex.GetType().Name} exception thrown.");
        //        Console.WriteLine($"Message: {ex.Message}");
        //        throw;
        //    }            
        //}


        //private static Boolean ValueIsDefault<T>(T value)
        //{
        //    // from https://stackoverflow.com/questions/1895761/test-for-equality-to-the-default-value
        //    return EqualityComparer<T>.Default.Equals(value, default(T));
        //}


        public static IConfiguration Configuration
        {
            get;
        } = Program.LoadConfig();

        private static IConfiguration LoadConfig()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            return builder.Build();
        }


    }

}
