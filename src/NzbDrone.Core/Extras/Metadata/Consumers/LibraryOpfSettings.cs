using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Extras.Metadata.Consumers
{
    public class LibraryOpfSettings : NullConfig
    {
        [FieldDefinition(0, Label = "Verify Source Title", Type = FieldType.Checkbox, HelpText = "Skip exports unless the selected catalog title matches embedded tags, the filename, or the book folder. Use when auditing existing library matches.")]
        public bool VerifySourceTitle { get; set; }

        [FieldDefinition(1, Label = "Excluded Paths", Type = FieldType.Textbox, Advanced = true, HelpText = "Absolute file or folder paths to leave untouched during metadata review, separated by newlines. Remove an exclusion after verifying the catalog assignment.")]
        public string ExcludedPaths { get; set; }
    }
}
