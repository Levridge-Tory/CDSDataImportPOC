using System;
using System.Collections.Generic;
using System.Text;

namespace CDSDataImportPOC
{
    public class IntegrationSourceFile
    {
        public String Name
        {
            get;
            set;
        }

        public String Description
        {
            get;
            set;
        }

        public ImportFileType FileType
        {
            get;
            set;
        }

        public Boolean FirstRowIsHeader
        {
            get;
            set;
        }

        public ImportDataDelimiter DataDelimiter
        {
            get;
            set;
        }

        public ImportFieldDelimiter FieldDelimiter
        {
            get;
            set;
        }
    }
}

//{
//  "name": null,
//  "description": null,
//  "fileType": null,
//  "firstRowIsHeader": false,
//  "dataDelimiter": "\u0000",
//  "fieldDelimiter": "\u0000"
//}