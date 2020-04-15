using System;
using System.Collections.Generic;
using System.Linq;
using Levridge.ODataDataSources;
using Levridge.EntityFramework;
using System.Text;

namespace CDSDataImportPOC
{
    public static class EntityHelper
    {
        public static Levridge.EntityFramework.Entity CreateEntity<T>(T dotNetObject,
            EntityFieldDefinition entityFieldDefinition, IEnumerable<IEnumerable<String>> alternateKeys = null)
        {
            var entity = new Levridge.EntityFramework.Entity(entityFieldDefinition.DotNetType.Name)
            {
                DotNetType = entityFieldDefinition.DotNetType,
                IDFieldName = entityFieldDefinition.IdFieldName,
                NameFieldName = entityFieldDefinition.NameFieldName,
                SyncIDFieldName = entityFieldDefinition.SyncIdFieldName,
                CreatedVersionFieldName = entityFieldDefinition.CreatedVersionFieldName,
                ModifiedVersionFieldName = entityFieldDefinition.ModifiedVersionFieldName
            };

            var ExcludeFieldList = new List<String>(
                EntityHelper.CommonEntitySystemFields.Concat(
                EntityHelper.EntitySpecificSystemFieldSuffixes
                .Select(f => entityFieldDefinition.DotNetType.Name + f)));

            entity.CopyValuesFromODataEntity(typeof(CRMODataField<>),
                dotNetObject,
                entityFieldDefinition.DotNetType.GetProperties()
                .Where(p => p.Name.StartsWith('_') == false && ExcludeFieldList.Contains(p.Name, StringComparer.OrdinalIgnoreCase) == false)
                .Select(p => p.Name));

            if (alternateKeys?.Count() > 0)
            {
                foreach (var key in alternateKeys)
                {
                    entity.AddKey(new EntityKey(entity, String.Concat(key), key));
                }
            }

            return entity;
        }

        public static List<String> CommonEntitySystemFields
        {
            get;
        } = new List<String>()
        {
            "utcconversiontimezonecode",
            "owninguser",
            "modifiedonbehalfby",
            "owningteam",
            "createdonbehalfby",
            "owningbusinessunit",
            "recordsownerid_systemuser",
            "ownerid",
            "modifiedby",
            "createdby",
            "timezoneruleversionnumber"
        };

        public static List<String> EntitySpecificSystemFieldSuffixes
        {
            get;
        } = new List<String>()
        {
            "_AsyncOperations",
            "_BulkDeleteFailures",
            "_SyncErrors"
        };


    }

    public struct EntityFieldDefinition
    {
        public System.Type DotNetType;
        public String IdFieldName;
        public String NameFieldName;
        public String SyncIdFieldName;
        public String CreatedVersionFieldName;
        public String ModifiedVersionFieldName;
    }

}
