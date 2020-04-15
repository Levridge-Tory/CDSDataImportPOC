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

        public String FileType
        {
            get;
            set;
        }

        public Boolean FirstRowIsHeader
        {
            get;
            set;
        }

        public Char DataDelimiter
        {
            get;
            set;
        }

        public Char FieldDelimiter
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