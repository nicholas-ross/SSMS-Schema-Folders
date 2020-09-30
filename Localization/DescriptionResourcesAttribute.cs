namespace SsmsSchemaFolders.Localization
{
    using System.ComponentModel;

    internal class DescriptionResourcesAttribute : DescriptionAttribute
    {
        private bool isLocalized;

        public DescriptionResourcesAttribute(string resourcesNameSuffix)
            : base(resourcesNameSuffix)
        { }

        public override string Description
        {
            get
            {
                if (!isLocalized)
                {
                    isLocalized = true;
                    var localizedString = GetLocalizedString(DescriptionValue);
                    if (localizedString != null)
                        DescriptionValue = localizedString;
                }
                return DescriptionValue;
            }
        }

        protected virtual string GetLocalizedString(string value)
        {
            return ResourcesAccess.GetString("PropertyDescription" + value);
        }
    }
}
