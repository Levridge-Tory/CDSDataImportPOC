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
            Console.WriteLine($"Processing {args[0]}");

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
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            PropertyNameCaseInsensitive = true
                        };
                        options.Converters.Add(new JsonStringEnumConverter());
                        
                        //options.Converters.Add(new LevridgeEnumConverter<MapType>());
                        //options.Converters.Add(new LevridgeEnumConverter<ImportModeCode>());
                        //options.Converters.Add(new LevridgeEnumConverter<ImportFileType>());
                        //options.Converters.Add(new LevridgeEnumConverter<ImportDataDelimiter>());
                        //options.Converters.Add(new LevridgeEnumConverter<ImportFieldDelimiter>());
                        integrationJob = await JsonSerializer.DeserializeAsync<IntegrationJob>(fs, options);
                    }
                    // importmap
                    ImportHandlerService importHandler = new ImportHandlerService(integrationJob.Name,
                        integrationJob.Description,
                        integrationJob.SourceEntityName,
                        integrationJob.TargetEntityName,
                        ImportEntitiesPerFile.Single);

                    importmap importmap = await importHandler.CreateImportMapAsync();
                    // column mappings
                    foreach (var attributeMap in integrationJob.EntityMap.AttributeMaps)
                    {
                        switch(attributeMap.MapType)
                        {
                            case MapType.Column:
                                await importHandler.CreateColumnMappingEntityAsync(attributeMap.SourceAttribute, attributeMap.TargetAttribute).ConfigureAwait(false);
                                break;
                            case MapType.ListValue:
                                await importHandler.CreatePickListMappingEntityAsync(attributeMap.SourceAttribute, attributeMap.TargetAttribute).ConfigureAwait(false);
                                break;
                            case MapType.Lookup:
                                await importHandler.CreateLookupMappingEntityAsync(
                                    attributeMap.SourceAttribute, 
                                    attributeMap.TargetAttribute,
                                    attributeMap.LookupEntity,
                                    attributeMap.LookupAttribute).ConfigureAwait(false);
                                break;

                        }
                    }

                    //await importHandler.CreateColumnMappingEntityAsync("DESCRIPTION", nameof(lev_paymentterm.lev_description)).ConfigureAwait(false);
                    //await importHandler.CreateColumnMappingEntityAsync("NAME", nameof(lev_paymentterm.lev_paymenttermcode)).ConfigureAwait(false);
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

                    var importEntity = await importHandler.CreateImportAsync(integrationJob.NotificationEmail,
                        integrationJob.ImportMode, String.IsNullOrEmpty(integrationJob.NotificationEmail) == false);

                    var path = integrationJob.SourceFileLocation.Trim().StartsWith('[') &&
                        integrationJob.SourceFileLocation.Trim().EndsWith(']') ? ProcessCommand(integrationJob.SourceFileLocation)
                    : integrationJob.SourceFileLocation;

                    var importMap = await importHandler.CreateImportFileAsync(fileName: integrationJob.SourceFile.Name,
                        path: path,
                        firstRowHeader: integrationJob.SourceFile.FirstRowIsHeader,
                        dataDelimiter: integrationJob.SourceFile.DataDelimiter,
                        fieldDelimiter: integrationJob.SourceFile.FieldDelimiter,
                        fileType: integrationJob.SourceFile.FileType,
                        enableDuplicateDetection: true,
                        upsertMode: integrationJob.ImportMode == ImportModeCode.Create ? UpsertModeCode.Create : UpsertModeCode.Update
                        );

                    // entity = await ds.CreateEntityAsync(entity).ConfigureAwait(false);
                    Console.WriteLine($"Created Import Job {integrationJob.Name}.");
                    Console.WriteLine("Begin Parsing ...");

                    var odataClientService = services.GetRequiredService<IODataClientService>();
                    // <Action Name = "ParseImport" IsBound = "true">
                    // <Parameter Name = "entity" Type = "mscrm.import" Nullable = "false" />
                    // <ReturnType Type = "mscrm.asyncoperation" Nullable = "false" />
                    // </Action>
                    var updatedImport = await odataClientService.ExecuteBoundActionAsync<import>(importEntity, importEntity.importid.Value, "ParseImport");

                    // validate that we completed successfully

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

        private static String ProcessCommand(String location)
        {
            var command = location.Trim(new char[]{ '[', ']' });
            String result;
            switch (command)
            {
                // currently the only command we support
                case "CurrentDirectory":
                    result = Directory.GetCurrentDirectory();
                    break;
                default:
                    throw new InvalidDataException($"File location command {command} is not recognized.");
            }
            return result;
        }

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
