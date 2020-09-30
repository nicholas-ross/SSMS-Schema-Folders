namespace SsmsSchemaFolders.Localization
{
    using System.ComponentModel;

    internal class CategoryResourcesAttribute : CategoryAttribute
    {
        public CategoryResourcesAttribute(string resourcesNameSuffix)
            : base(resourcesNameSuffix)
        { }

        protected override string GetLocalizedString(string value)
        {
            var localizedString = ResourcesAccess.GetString("PropertyCategory" + value);
            if (localizedString != null)
                return localizedString;
            return value;
        }
    }
}
