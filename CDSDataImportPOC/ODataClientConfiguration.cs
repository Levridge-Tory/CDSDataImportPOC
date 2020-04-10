using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Text;

namespace CDSDataImportPOC
{
    class ODataClientConfiguration
    {
        private const String SectionName = "DynamicsCRM";

        public ODataClientConfiguration(IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (configuration is IConfigurationSection configSection)
            {
                this.Configuration = configuration;
            }
            else
            {
                this.Configuration = configuration.GetSection(SectionName);
            }

            this.ChangeToken = configuration.GetReloadToken();
        }

        public IConfiguration Configuration
        {
            get;
        }

        public IChangeToken ChangeToken
        {
            get;
            private set;
        }

        public String ActiveDirectoryClientAppId
        {
            get
            {
                return this.Configuration[nameof(this.ActiveDirectoryClientAppId)];
            }
        }

        public String ActiveDirectoryClientAppSecret
        {
            get
            {
                return this.Configuration[nameof(this.ActiveDirectoryClientAppSecret)];
            }
        }
        public String ActiveDirectoryResource
        {
            get
            {
                return this.Configuration[nameof(this.ActiveDirectoryResource)];
            }
        }
        public String ActiveDirectoryTenant
        {
            get
            {
                return this.Configuration[nameof(this.ActiveDirectoryTenant)];
            }
        }
        public String TLSVersion
        {
            get
            {
                return this.Configuration[nameof(this.TLSVersion)];
            }
        }
        public String UriString
        {
            get
            {
                return this.Configuration[nameof(this.UriString)];
            }
        }

        public String ODataEntityPath
        {
            get
            {
                return this.Configuration[nameof(this.ODataEntityPath)];
            }
        }

        public String AssemblyName
        {
            get
            {
                return this.Configuration[nameof(this.AssemblyName)];
            }
        }

        public String ClientClassesNameSpace
        {
            get
            {
                return this.Configuration[nameof(this.ClientClassesNameSpace)];
            }
        }

        public String MetadataResource
        {
            get
            {
                return this.Configuration[nameof(this.MetadataResource)];
            }
        }
    }
}
