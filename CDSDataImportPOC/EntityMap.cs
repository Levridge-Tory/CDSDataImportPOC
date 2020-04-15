using System;
using System.Collections.Generic;
using System.Text;

namespace CDSDataImportPOC
{
    public class EntityMap
    {
        public String SourceEntity
        {
            get;
            set;
        }

        public String TargetEntity
        {
            get;
            set;
        }

        public IEnumerable<AttributeMap> AttributeMaps
        { 
            get;
            set;
        }
    }
}

//{
//  "sourceEntity": null,
//  "targetEntity": null,
//  "attributeMaps": null
//}