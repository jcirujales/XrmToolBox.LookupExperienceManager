using System;

namespace XrmToolBox.LookupExperienceManager.Model
{
    public class LookupInfo
    {
        public string SourceEntity { get; set; }
        public string Form { get; set; }
        public Guid FormId { get; set; }
        public string FormXml { get; set; }
        public string Label { get; set; }
        public string SchemaName { get; set; }
        public bool IsManaged { get; set; }
        public bool IsInlineNewEnabled { get; set; }
        public bool IsCustomizable { get; set; }
        public bool IsSourceEntityCustomizable { get; set; }
        public bool DisableMru { get; set; }
        public bool UseMainFormDialogForEdit { get; set; }
        public bool UseMainFormDialogForCreate { get; set; }
    }
}
