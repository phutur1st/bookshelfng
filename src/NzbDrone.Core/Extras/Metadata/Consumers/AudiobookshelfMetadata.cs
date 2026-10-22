using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using System.Text;
using System.Xml;
using NzbDrone.Core.Books;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Extras.Metadata.Consumers
{
    public class AudiobookshelfMetadata : MetadataBase<LibraryOpfSettings>
    {
        private const string OpfNamespace = "http://www.idpf.org/2007/opf";
        private const string DcNamespace = "http://purl.org/dc/elements/1.1/";

        private readonly IMetadataTagService _metadataTagService;
        private readonly Logger _logger;

        public AudiobookshelfMetadata(IMetadataTagService metadataTagService, Logger logger)
        {
            _metadataTagService = metadataTagService;
            _logger = logger;
        }

        public override string Name => "Library OPF";

        public override MetadataFile FindMetadataFile(Author author, string path)
        {
            if (!string.Equals(Path.GetExtension(path), ".opf", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return new MetadataFile
            {
                AuthorId = author.Id,
                Consumer = GetType().Name,
                Type = MetadataType.BookMetadata,
                RelativePath = Path.GetRelativePath(author.Path, path),
                Extension = Path.GetExtension(path)
            };
        }

        public override MetadataFileResult AuthorMetadata(Author author)
        {
            return null;
        }

        public override MetadataFileResult BookMetadata(Author author, BookFile bookFile)
        {
            if (bookFile.Part > 1 ||
                string.IsNullOrWhiteSpace(bookFile.Path) ||
                !MediaFileExtensions.AllExtensions.Contains(Path.GetExtension(bookFile.Path)))
            {
                return null;
            }

            var edition = bookFile.Edition?.Value;
            var book = edition?.Book?.Value;
            if (edition == null || book == null)
            {
                return null;
            }

            if (Settings.VerifySourceTitle && !HasMatchingSourceTitle(bookFile, edition.Title ?? book.Title))
            {
                _logger.Warn("Skipping OPF export for {0}: selected catalog title does not match source title evidence", bookFile.Path);
                return null;
            }

            var authorName = bookFile.Author?.Value?.Name;
            if (string.IsNullOrWhiteSpace(authorName))
            {
                authorName = book.Author?.Value?.Name;
            }

            var relativePath = Path.GetRelativePath(author.Path, Path.ChangeExtension(bookFile.Path, ".opf"));
            return new MetadataFileResult(relativePath, CreateOpf(edition, book, authorName));
        }

        public override List<ImageFileResult> AuthorImages(Author author)
        {
            return new List<ImageFileResult>();
        }

        public override List<ImageFileResult> BookImages(Author author, BookFile bookFile)
        {
            return new List<ImageFileResult>();
        }

        private bool HasMatchingSourceTitle(BookFile file, string catalogTitle)
        {
            var expected = NormalizeTitle(catalogTitle);
            if (string.IsNullOrWhiteSpace(expected))
            {
                return false;
            }

            string taggedTitle;
            try
            {
                taggedTitle = _metadataTagService.ReadTags((FileInfoBase)new FileInfo(file.Path))?.BookTitle;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Could not verify source tags for OPF export: {0}", file.Path);
                return false;
            }

            var names = new[] { taggedTitle, Path.GetFileNameWithoutExtension(file.Path), Path.GetFileName(Path.GetDirectoryName(file.Path)) };
            return names.Any(name => NormalizeTitle(name) == expected);
        }

        private static string NormalizeTitle(string title)
        {
            var value = Regex.Replace(title ?? string.Empty, @"\s*\(\d+\)$", string.Empty);
            value = Regex.Replace(value, @"\s*\((?:unabridged|abridged)\)", string.Empty, RegexOptions.IgnoreCase);
            return Regex.Replace(value.Normalize(NormalizationForm.FormD), @"[^\p{L}\p{Nd}]", string.Empty).ToLowerInvariant();
        }

        private static string CreateOpf(Edition edition, Book book, string authorName)
        {
            var output = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = true
            };

            using (var writer = XmlWriter.Create(output, settings))
            {
                writer.WriteStartElement("package", OpfNamespace);
                writer.WriteAttributeString("version", "2.0");
                writer.WriteAttributeString("unique-identifier", "BookId");

                writer.WriteStartElement("metadata", OpfNamespace);
                writer.WriteAttributeString("xmlns", "dc", null, DcNamespace);
                writer.WriteAttributeString("xmlns", "opf", null, OpfNamespace);

                WriteDcElement(writer, "title", edition.Title ?? book.Title);
                WriteCreator(writer, authorName, "aut");
                WriteCreator(writer, edition.Narrator, "nrt");

                var isbn = edition.Isbn13;
                var asin = edition.Asin;
                var primaryScheme = !string.IsNullOrWhiteSpace(isbn)
                    ? "ISBN"
                    : (!string.IsNullOrWhiteSpace(asin) ? "ASIN" : "URI");
                var primaryIdentifier = !string.IsNullOrWhiteSpace(isbn)
                    ? isbn
                    : (!string.IsNullOrWhiteSpace(asin)
                        ? asin
                        : $"urn:bookshelfng:book:{book.ForeignBookId ?? book.Id.ToString(CultureInfo.InvariantCulture)}");

                WriteIdentifier(writer, primaryIdentifier, primaryScheme, true);
                if (!string.IsNullOrWhiteSpace(isbn) && !string.IsNullOrWhiteSpace(asin))
                {
                    WriteIdentifier(writer, asin, "ASIN", false);
                }

                WriteDcElement(writer, "description", edition.Overview);
                WriteDcElement(writer, "publisher", edition.Publisher);
                WriteDcElement(writer, "language", edition.Language);

                var releaseDate = edition.ReleaseDate ?? book.ReleaseDate;
                if (releaseDate.HasValue)
                {
                    WriteDcElement(writer, "date", releaseDate.Value.ToString("yyyy", CultureInfo.InvariantCulture));
                }

                foreach (var genre in book.Genres ?? new List<string>())
                {
                    WriteDcElement(writer, "subject", genre);
                }

                WriteSeries(writer, book);

                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            return output.ToString();
        }

        private static void WriteDcElement(XmlWriter writer, string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            writer.WriteElementString("dc", name, DcNamespace, value);
        }

        private static void WriteCreator(XmlWriter writer, string name, string role)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            writer.WriteStartElement("dc", "creator", DcNamespace);
            writer.WriteAttributeString("opf", "role", OpfNamespace, role);
            writer.WriteString(name);
            writer.WriteEndElement();
        }

        private static void WriteIdentifier(XmlWriter writer, string value, string scheme, bool isPrimary)
        {
            writer.WriteStartElement("dc", "identifier", DcNamespace);
            if (isPrimary)
            {
                writer.WriteAttributeString("id", "BookId");
            }

            writer.WriteAttributeString("opf", "scheme", OpfNamespace, scheme);
            writer.WriteString(value);
            writer.WriteEndElement();
        }

        private static void WriteSeries(XmlWriter writer, Book book)
        {
            var preferredSeries = book.Author?.Value?.MetadataProfile?.Value?.PreferredSeries;
            var seriesLink = book.SeriesLinks?.Value.GetPreferredSeriesLink(preferredSeries);

            var seriesTitle = seriesLink?.Series?.Value?.Title;
            if (string.IsNullOrWhiteSpace(seriesTitle))
            {
                return;
            }

            writer.WriteStartElement("meta", OpfNamespace);
            writer.WriteAttributeString("name", "calibre:series");
            writer.WriteAttributeString("content", seriesTitle);
            writer.WriteEndElement();

            var seriesIndex = seriesLink.Position;
            if (string.IsNullOrWhiteSpace(seriesIndex) && seriesLink.SeriesPosition > 0)
            {
                seriesIndex = seriesLink.SeriesPosition.ToString(CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrWhiteSpace(seriesIndex))
            {
                writer.WriteStartElement("meta", OpfNamespace);
                writer.WriteAttributeString("name", "calibre:series_index");
                writer.WriteAttributeString("content", seriesIndex);
                writer.WriteEndElement();
            }
        }
    }
}
