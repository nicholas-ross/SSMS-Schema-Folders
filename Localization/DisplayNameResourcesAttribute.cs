namespace SsmsSchemaFolders.Localization
{
    using System.ComponentModel;

    internal class DisplayNameResourcesAttribute : DisplayNameAttribute
    {
        private bool isLocalized;

        public DisplayNameResourcesAttribute(string resourcesNameSuffix)
            : base(resourcesNameSuffix)
        { }

        public override string DisplayName
        {
            get
            {
                if (!isLocalized)
                {
                    isLocalized = true;
                    var localizedString = GetLocalizedString(DisplayNameValue);
                    if (localizedString != null)
                        DisplayNameValue = localizedString;
                }
                return DisplayNameValue;
            }
        }

        protected virtual string GetLocalizedString(string value)
        {
            return ResourcesAccess.GetString("PropertyDisplayName" + value);
        }
    }
}
