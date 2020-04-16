using System;
using Levridge.ODataDataSources.DynamicsCRM;
using Levridge.EntityFramework;
using Levridge.ODataDataSources;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.IO;

namespace CDSDataImportPOC
{
    public class ImportHandlerService : IImportHandlerService
    {
        private CRMSimpleODataDataSource _crmDataSource;

        public ImportHandlerService(String name, 
            String description,
            String source,
            String target,
            ImportEntitiesPerFile entitiesperfile = ImportEntitiesPerFile.Single)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.Description = description ?? throw new ArgumentNullException(nameof(description));
            this.Source = source ?? throw new ArgumentNullException(nameof(source));
            this.Target = target ?? throw new ArgumentNullException(nameof(target));
            this.Entitiesperfile = entitiesperfile;
        }

        private String Name
        {
            get;
            set;
        }

        private String Description
        {
            get;
            set;
        }
        private String Source
        {
            get;
            set;
        }

        private String Target
        {
            get;
            set;
        }
        
        private ImportEntitiesPerFile Entitiesperfile
        {
            get;
            set;
        }

        private importmap ImportMap
        {
            get;
            set;
        }

        private Entity ImportMapEntity
        {
            get;
            set;
        }

        private import Import
        {
            get;
            set;
        }

        private Entity ImportEntity
        {
            get;
            set;
        }

        private importfile ImportFile
        {
            get;
            set;
        }

        private Entity ImportFileEntity
        {
            get;
            set;
        }

        public async Task<importmap> CreateImportMapAsync()
        {
            this.ImportMap = new importmap()
            {
                name = this.Name,
                source = this.Source,
                description = this.Description,
                entitiesperfile = (Int32)this.Entitiesperfile,
                isvalidforimport = true,
                importmaptype = (Int32)ImportMapType.Standard
            };

            this.ImportMapEntity = EntityHelper.CreateEntity<importmap>(this.ImportMap,
                new EntityFieldDefinition()
                {
                    DotNetType = typeof(importmap),
                    NameFieldName = nameof(importmap.name),
                    IdFieldName = nameof(importmap.importmapid),
                    SyncIdFieldName = nameof(importmap.name),
                    CreatedVersionFieldName = nameof(importmap.createdon),
                    ModifiedVersionFieldName = nameof(importmap.modifiedon)
                });

            // this.ImportMapEntity = await this.CreateEntityAsync(this.ImportMapEntity);
            this.ImportMapEntity = await this.ReadOrCreateEntityAsync(this.ImportMapEntity);

            this.ImportMap = this.ImportMapEntity.GetEntityAsDotNetType<importmap>();
            return this.ImportMap;
        }

        public async Task<import> CreateImportAsync(String emailAddress, ImportModeCode mode, Boolean sendNotification = false)
        {
            this.Import = new import()
            {
                name = Name,
                emailaddress = emailAddress,
                sendnotification = sendNotification,
                modecode = (Int32)mode,
                statuscode = (Int32)ImportStatus.Submitted
            };

            var keys = new List<IEnumerable<String>>();
            keys.Add(new List<String>()
            {
                nameof(import.name),
                nameof(import.modecode),
                nameof(import.statuscode)
            });

            this.ImportEntity = EntityHelper.CreateEntity<import>(this.Import,
            new EntityFieldDefinition()
            {
                DotNetType = typeof(import),
                NameFieldName = nameof(Import.name),
                IdFieldName = nameof(Import.importid),
                SyncIdFieldName = nameof(Import.importid),
                CreatedVersionFieldName = nameof(Import.createdon),
                ModifiedVersionFieldName = nameof(Import.modifiedon)
            }, keys);
            this._crmDataSource.Entity = this.ImportEntity;
            this.ImportEntity = await this.CreateEntityAsync(this.ImportEntity);
            this.Import = ImportEntity.GetEntityAsDotNetType<import>();
            return this.Import;
        }

        public async Task<importfile> CreateImportFileAsync(String fileName, String path, Boolean firstRowHeader, ImportDataDelimiter dataDelimiter,
            ImportFieldDelimiter fieldDelimiter,
            ImportFileType fileType, Boolean enableDuplicateDetection, UpsertModeCode upsertMode)
        {
            this.ImportFile = new importfile()
            {
                datadelimitercode = (Int32)dataDelimiter,
                enableduplicatedetection = enableDuplicateDetection,
                fielddelimitercode = (Int32)fieldDelimiter,
                isfirstrowheader = firstRowHeader,
                filetypecode = (Int32)fileType,
                name = fileName,
                processcode = (Int32)ProcessCode.Process,
                upsertmodecode = (Int32)upsertMode,
                // importSourceFile.size = ?
                source = fileName,
                sourceentityname = this.Source,
                targetentityname = this.Target,
                importid = this.Import,
                importmapid = this.ImportMap,
                usesystemmap = false,
                processingstatus = (Int32)ImportFileProcessStatus.NotStarted
            };

            switch(fileType)
            {
                case ImportFileType.CSV:
                    ImportFile.content = await ImportHandlerService.ReadCsvFileAsync(
                        Path.Combine(path, ImportFile.source)).ConfigureAwait(false);
                    break;
                default:
                    throw new NotImplementedException($"File Type {fileType.ToString()} is not yet implemented.");
            }

            var keys = new List<IEnumerable<String>>();
            keys.Add(new List<String>()
            {
                nameof(importfile.source),
                nameof(importfile.sourceentityname),
                nameof(importfile.targetentityname),
                nameof(importfile.processingstatus)
            });

            this.ImportFileEntity = EntityHelper.CreateEntity<importfile>(this.ImportFile,
            new EntityFieldDefinition()
            {
                DotNetType = typeof(importfile),
                NameFieldName = nameof(importfile.name),
                IdFieldName = nameof(importfile.importfileid),
                SyncIdFieldName = nameof(importfile.entitykeyid),
                CreatedVersionFieldName = nameof(importfile.createdon),
                ModifiedVersionFieldName = nameof(importfile.modifiedon)
            }, keys);
            this._crmDataSource.Entity = this.ImportFileEntity;
            this.ImportFileEntity = await this.CreateEntityAsync(this.ImportFileEntity);

            this.ImportFile = this.ImportFileEntity.GetEntityAsDotNetType<importfile>();
            return ImportFile;
        }

        public async Task<columnmapping> CreateColumnMappingEntityAsync(
            String sourceAttributeName,
            String targetAttributeName)
        {
            var importMapDescriptionMapping = ImportMap.ColumnMapping_ImportMap?.Where(cm =>
                cm.sourceentityname == Source &&
                cm.sourceattributename == sourceAttributeName &&
                cm.targetentityname == Target &&
                cm.targetattributename == targetAttributeName);

            // either get the existing column mapping or create a new column mapping
            Boolean columnMappingExists = this.ImportMap.ColumnMapping_ImportMap?.Count > 0 &&
                importMapDescriptionMapping?.Count() > 0;

            var columnMapping = columnMappingExists ? importMapDescriptionMapping.First() :
                new columnmapping() // https://docs.microsoft.com/en-us/dynamics365/customer-engagement/web-api/columnmapping?view=dynamics-ce-odata-9
                {
                    sourceentityname = this.Source,
                    sourceattributename = sourceAttributeName,
                    targetentityname = this.Target,
                    targetattributename = targetAttributeName,
                    processcode = (Int32)ProcessCode.Process,
                    importmapid = this.ImportMap
                };
            var keys = new List<IEnumerable<String>>();
            keys.Add(new List<String>() { nameof(columnmapping.sourceattributename), nameof(columnmapping.targetattributename) });
            var entity = EntityHelper.CreateEntity<columnmapping>(columnMapping,
                        new EntityFieldDefinition()
                        {
                            DotNetType = typeof(columnmapping),
                            NameFieldName = nameof(columnmapping.sourceattributename),
                            IdFieldName = nameof(columnmapping.columnmappingid),
                            SyncIdFieldName = nameof(columnmapping.columnmappingid),
                            CreatedVersionFieldName = nameof(columnmapping.createdon),
                            ModifiedVersionFieldName = nameof(columnmapping.modifiedon)
                        }, keys);
            if (columnMappingExists == false)
            {
                this._crmDataSource.Entity = entity;
                // entity = await Program.ReadOrCreateEntityAsync(ds, entity);
                entity = await this.CreateEntityAsync(entity);
            }

            return entity.GetEntityAsDotNetType<columnmapping>();
        }

        public async Task<lookupmapping> CreateLookupMappingEntityAsync(
            String sourceAttributeName,
            String targetAttributeName,
            string lookupEntity,
            string lookupAttribute)
        {
            var columnMapping = await this.CreateColumnMappingEntityAsync(sourceAttributeName, targetAttributeName);
            var existingLookupMap = columnMapping?.LookUpMapping_ColumnMapping?.Where(lm =>
            lm.lookupattributename == lookupAttribute &&
            lm.lookupentityname == lookupEntity);
            Boolean lookupMappingExists = this.ImportMap.ColumnMapping_ImportMap?.Count > 0 &&
                existingLookupMap?.Count() > 0;

            var lookupMapping = new lookupmapping() 
            {
                lookupentityname = lookupEntity,
                lookupattributename = lookupAttribute,
                lookupsourcecode = (Int32)LookupSourceCode.System,
                processcode = (Int32)ProcessCode.Process,
                columnmappingid = columnMapping
            };

            var keys = new List<IEnumerable<String>>();
            keys.Add(new List<String>() { nameof(lookupmapping.lookupentityname), 
                nameof(lookupmapping.lookupattributename)
            });
            var entity = EntityHelper.CreateEntity<lookupmapping>(lookupMapping,
                new EntityFieldDefinition()
                {
                    DotNetType = typeof(lookupmapping),
                    NameFieldName = nameof(lookupmapping.lookupattributename),
                    IdFieldName = nameof(lookupmapping.lookupmappingid),
                    SyncIdFieldName = nameof(lookupmapping.lookupattributename),
                    CreatedVersionFieldName = nameof(lookupmapping.createdon),
                    ModifiedVersionFieldName = nameof(lookupmapping.modifiedon)
                }, keys);
            if (lookupMappingExists == false)
            {
                this._crmDataSource.Entity = entity;
                // entity = await Program.ReadOrCreateEntityAsync(ds, entity);
                entity = await this.CreateEntityAsync(entity);
            }

            return entity.GetEntityAsDotNetType<lookupmapping>();
        }


        private static async Task<string> ReadCsvFileAsync(string filePath)
        {
            // StringBuilder data = new StringBuilder();
            using (StreamReader reader = new StreamReader(filePath))
            {
                return await reader.ReadToEndAsync();
            }
        }

        private static async Task<CRMSimpleODataDataSource> CreateDataSourceAsync(Entity entity)
        {
            var ds = new CRMSimpleODataDataSource(entity, Program.Configuration);
            await ds.ConnectAsync().ConfigureAwait(false);
            return ds;
        }


        private async Task<Entity> CreateEntityAsync(Entity entity)
        {
            if (entity is null)
            {
                throw new ArgumentNullException(nameof(entity));
            }
            if (this._crmDataSource is null)
            {
                this._crmDataSource = await ImportHandlerService.CreateDataSourceAsync(this.ImportMapEntity).ConfigureAwait(false);
            }

            entity = await this._crmDataSource.CreateEntityAsync(entity).ConfigureAwait(false);
            return entity;
        }

        private async Task<Entity> ReadOrCreateEntityAsync(Entity entity)
        {
            if (entity is null)
            {
                throw new ArgumentNullException(nameof(entity));
            }
            if (this._crmDataSource is null)
            {
                this._crmDataSource = await ImportHandlerService.CreateDataSourceAsync(this.ImportMapEntity).ConfigureAwait(false);
            }

            try
            {
                Entity readEntity = null;
                if (null != entity.ID || null != entity.SyncID)
                {
                    readEntity = this._crmDataSource.ReadEntity(entity);
                }
                else if (entity.Keys.Count() > 1 &&
                    entity.Keys.First(k => k.KeyName.Contains("PK") == false)
                    .KeyValues.All(v => null != v.Value && !ImportHandlerService.ValueIsDefault(v.Value)))
                {
                    readEntity = this._crmDataSource.ReadEntityByKey(entity, entity.Keys.First(k => k.KeyName.Contains("PK") == false).KeyName);
                }
                if (null != readEntity)
                {
                    entity = readEntity;
                }
                else
                {
                    entity = await this._crmDataSource.CreateEntityAsync(entity).ConfigureAwait(false);
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

    }
}
