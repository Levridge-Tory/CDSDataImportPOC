using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Levridge.EntityFramework;
using Levridge.ODataDataSources.DynamicsCRM;

namespace CDSDataImportPOC
{
    public interface IImportHandlerService
    {
        public Task<importmap> CreateImportMapAsync();
        public Task<import> CreateImportAsync(String emailAddress, ImportModeCode mode, Boolean sendNotification = false);
        public Task<importfile> CreateImportFileAsync(String fileName, String path, Boolean firstRowHeader, ImportDataDelimiter dataDelimiter,
            ImportFieldDelimiter fieldDelimiter,
            ImportFileType fileType, Boolean enableDuplicateDetection, UpsertModeCode upsertMode);
        public Task<Entity> CreateColumnMappingEntityAsync(
            String sourceAttributeName,
            String targetAttributeName);

    }
}
