using System;

namespace SsmsSchemaFolders.Localization
{
    using System.Globalization;
    using System.Resources;
    using System.Threading;

    internal sealed class ResourcesAccess
    {
        private static ResourcesAccess loader;
        private ResourceManager resources;

        internal ResourcesAccess()
        {
            this.resources = new ResourceManager("SsmsSchemaFolders.Resources.Resources", this.GetType().Assembly);
        }

        private static ResourcesAccess GetLoader()
        {
            if (ResourcesAccess.loader == null)
            {
                var sr = new ResourcesAccess();
                Interlocked.CompareExchange<ResourcesAccess>(ref ResourcesAccess.loader, sr, (ResourcesAccess)null);
            }
            return ResourcesAccess.loader;
        }

        private static CultureInfo Culture
        {
            get
            {
                return (CultureInfo)null;
            }
        }

        public static ResourceManager Resources
        {
            get
            {
                return ResourcesAccess.GetLoader().resources;
            }
        }

        public static string GetString(string name, params object[] args)
        {
            var loader = ResourcesAccess.GetLoader();
            if (loader == null)
                return (string)null;
            string format = loader.resources.GetString(name, ResourcesAccess.Culture);
            if (args == null || args.Length == 0)
                return format;
            for (int index = 0; index < args.Length; ++index)
            {
                string str = args[index] as string;
                if (str != null && str.Length > 1024)
                    args[index] = (object)(str.Substring(0, 1021) + "...");
            }
            return string.Format((IFormatProvider)CultureInfo.CurrentCulture, format, args);
        }

        public static string GetString(string name)
        {
            return ResourcesAccess.GetLoader()?.resources.GetString(name, ResourcesAccess.Culture);
        }

        public static string GetString(string name, out bool usedFallback)
        {
            usedFallback = false;
            return ResourcesAccess.GetString(name);
        }

        public static object GetObject(string name)
        {
            return ResourcesAccess.GetLoader()?.resources.GetObject(name, ResourcesAccess.Culture);
        }
    }
}
