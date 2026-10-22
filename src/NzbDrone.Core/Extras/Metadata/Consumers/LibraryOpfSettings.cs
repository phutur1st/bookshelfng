using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Extras.Metadata.Consumers
{
    public class LibraryOpfSettings : NullConfig
    {
        [FieldDefinition(0, Label = "Verify Source Title", Type = FieldType.Checkbox, HelpText = "Skip exports unless the selected catalog title matches embedded tags, the filename, or the book folder. Use when auditing existing library matches.")]
        public bool VerifySourceTitle { get; set; }
    }
}
