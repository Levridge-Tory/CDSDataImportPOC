using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CDSDataImportPOC
{

    public enum LookupSourceCode
    {
        Source = 0,
        System =1
    }
    public enum MapType
    {
        Column = 0,
        ListValue = 1,
        Lookup = 2
    }

    [System.Runtime.Serialization.DataContractAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("CrmSvcUtil", "8.1.0.239")]
    public enum AsyncOperationStatusCode
    {

        [System.Runtime.Serialization.EnumMemberAttribute()]
        WaitingForResources = 0,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Waiting = 10,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        InProgress = 20,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Pausing = 21,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Canceling = 22,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Succeeded = 30,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Failed = 31,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Canceled = 32,
    }

    [System.Runtime.Serialization.DataContractAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("CrmSvcUtil", "7.0.0001.0117")]
    public enum AsyncOperationState
    {

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Ready = 0,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Suspended = 1,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Locked = 2,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Completed = 3,
    }

    enum ImportEntityMappingDedupe
    {
        Ignore = 1,
        Eliminate = 2
    }

    enum ImportEntityMappingComponentState
    {
        Published = 0,
        Unpublished = 1,
        Deleted = 2,
        DeletedUnpublished = 3
    }

    enum ImportStatus
    {
        Submitted = 0,
        Parsing = 1,
        Transforming = 2,
        Importing = 3,
        Completed = 4,
        Failed = 5
    }

    enum ImportFileProcessStatus
    {
        NotStarted = 1,
        Parsing = 2,
        ParsingComplete = 3,
        ComplexTransformation = 4,
        LookupTransformation = 5,
        PicklistTransformation = 6,
        OwnerTransformation = 7,
        TransformationComplete = 8,
        ImportPass_1 = 9,
        ImportPass_2 = 10,
        ImportComplete = 11,
        PrimaryKeyTransformation = 12
    }

    public enum ImportEntitiesPerFile
    {
        Single = 1,
        Multiple = 2
    }

    enum ImportMapType
    {
        Standard = 1,
        OutofBox = 2,
        InProcess = 3
    }

    enum ImportSourceType
    {
        SalesForceFullDataExport = 1,
        SalesForceReportExport = 2,
        SalesForceContactAndAccountReportExport = 3,
        Outlook2010BusinessContactManager = 4,
        GenericContactAndAccount = 5
    }

    enum ProcessCode
    {
        Process = 1,
        Ignore = 2,
        Internal = 3
    }

    public enum ImportDataDelimiter
    {
        DoubleQuote = 1,
        None = 2,
        SingleQuote
    }

    public enum ImportFieldDelimiter
    {
        Colon = 1,
        Comma = 2,
        Tab = 3,
        Semicolon = 4
    }

    public enum ImportFileType
    {
        CSV = 0,
        XMLSpreadsheet2003 = 1,
        Attachment = 2,
        XLSX = 3
    }

    public enum UpsertModeCode
    {
        Create = 0,
        Update = 1,
        Ignore = 2
    }

    public enum ImportModeCode
    {
        Create = 0,
        Update = 1
    }
}
