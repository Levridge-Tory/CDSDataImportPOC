using System;
using System.Collections.Generic;
using System.Text;

namespace CDSDataImportPOC
{
    public class AttributeMap
    {
        public MapType MapType
        {
            get;
            set;
        }
        public String SourceAttribute
        {
            get;
            set;
        }

        public Boolean SourceAttributeRequired
        {
            get;
            set;
        }

        public String TargetAttribute
        {
            get;
            set;
        }

        public Object DefaultTargetAttributeValue
        {
            get;
            set;
        }

        public IEnumerable<AttributeTransformation> Transformations
        {
            get;
            set;
        }

        public String LookupEntity
        {
            get;
            set;
        }

        public String LookupAttribute
        {
            get;
            set;
        }
    }
}


//{
//  "sourceAttribute": null,
//  "sourceAttributeRequired": false,
//  "targetAttribute": null,
//  "defaultTargetAttributeValue": null,
//  "transformations": null
//}