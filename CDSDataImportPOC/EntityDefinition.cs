using System;
using System.Collections.Generic;
using System.Text;

namespace CDSDataImportPOC
{
    public class EntityDefinition
    {
        public String Name
        {
            get;
            set;
        }

        public String PrimaryKeyField
        {
            get;
            set;
        }

        public String NaturalKeyField
        {
            get;
            set;
        }

        public IEnumerable<EntityAttribute> Attributes
        {
            get;
            set;
        }
    }
}
