using System;
using System.Collections.Generic;
using System.Text;

namespace CDSDataImportPOC
{
    public class IntegrationJob
    {
        public String Name
        {
            get; set;
        }

        public String Description
        {
            get;
            set;
        }

        public String NotificationEmail
        {
            get;
            set;
        }

        public String SourceDataSource
        {
            get;
            set;
        }

        public String SourceEntityName
        {
            get;
            set;
        }

        public IntegrationSourceFile SourceFile
        {
            get;
            set;
        }

        /// <summary>
        /// Represents either a file path or a URI
        /// </summary>
        public String SourceFileLocation
        {
            get;
            set;
        }

        public String TargetDataSource
        {
            get;
            set;
        }

        public String TargetEntityName
        {
            get;
            set;
        }

        public EntityMap EntityMap
        {
            get;
            set;
        }
    }
}

//{
//  "name": null,
//  "description": null,
//  "sourceDataSource": null,
//  "sourceEntityName": null,
//  "sourceFile": null,
//  "sourceFileLocation": null,
//  "targetDataSource": null,
//  "targetEntityName": null,
//  "entityMap": null
//}
