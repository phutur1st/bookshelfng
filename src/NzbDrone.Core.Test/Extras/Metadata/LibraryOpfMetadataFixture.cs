using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Extras.Metadata.Consumers;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Test.Extras.Metadata
{
    [TestFixture]
    public class LibraryOpfMetadataFixture
    {
        private readonly AudiobookshelfMetadata _subject = new AudiobookshelfMetadata();
        private Author _author;
        private BookFile _file;
        private Edition _edition;
        private static readonly XNamespace Dc = "http://purl.org/dc/elements/1.1/";
        private static readonly XNamespace Opf = "http://www.idpf.org/2007/opf";

        [SetUp]
        public void Setup()
        {
            _author = new Author { Name = "An Author", Path = Path.Combine(Path.GetTempPath(), "library", "An Author") };
            var book = new Book { Id = 7, ForeignBookId = "catalog-7", Title = "A Book", Author = _author, Genres = new List<string> { "Science & History" } };
            _edition = new Edition { Title = "A Book & More", Book = book, Isbn13 = "9781234567897", Asin = "B012345678", Narrator = "A Reader", Overview = "A <description>", ReleaseDate = new DateTime(2020, 6, 3) };
            _file = new BookFile { Id = 1, Path = Path.Combine(_author.Path, "A Book", "A Book.epub"), Part = 1, Author = _author, Edition = _edition };
        }

        [TestCase(".epub")]
        [TestCase(".kepub")]
        [TestCase(".mobi")]
        [TestCase(".azw3")]
        [TestCase(".pdf")]
        [TestCase(".m4b")]
        [TestCase(".mp3")]
        [TestCase(".EPUB")]
        public void supported_files_receive_a_sidecar_beside_the_media(string extension)
        {
            _file.Path = Path.ChangeExtension(_file.Path, extension);
            var result = _subject.BookMetadata(_author, _file);
            Assert.That(result, Is.Not.Null);
            Assert.That(Path.GetFullPath(Path.Combine(_author.Path, result.RelativePath)), Is.EqualTo(Path.ChangeExtension(_file.Path, ".opf")));
            var xml = XDocument.Parse(result.Contents);
            Assert.That(xml.Descendants(Dc + "title").Single().Value, Is.EqualTo("A Book & More"));
            Assert.That(xml.Descendants(Dc + "description").Single().Value, Is.EqualTo("A <description>"));
            Assert.That(xml.Descendants(Dc + "subject").Single().Value, Is.EqualTo("Science & History"));
            Assert.That(xml.Descendants(Dc + "identifier").Count(), Is.EqualTo(2));
            Assert.That(xml.Descendants(Dc + "creator").Single(e => (string)e.Attribute(Opf + "role") == "aut").Value, Is.EqualTo("An Author"));
            Assert.That(xml.Descendants(Dc + "creator").Single(e => (string)e.Attribute(Opf + "role") == "nrt").Value, Is.EqualTo("A Reader"));
        }

        [Test]
        public void later_audio_parts_do_not_create_duplicate_sidecars()
        {
            _file.Path = Path.ChangeExtension(_file.Path, ".mp3");
            _file.Part = 2;
            Assert.That(_subject.BookMetadata(_author, _file), Is.Null);
        }

        [TestCase(".jpg")]
        [TestCase(".opf")]
        [TestCase(".txt")]
        public void unrelated_files_are_ignored(string extension)
        {
            _file.Path = Path.ChangeExtension(_file.Path, extension);
            Assert.That(_subject.BookMetadata(_author, _file), Is.Null);
        }

        [Test]
        public void incomplete_catalog_records_are_not_exported()
        {
            _file.Edition = new LazyLoaded<Edition>((Edition)null);
            Assert.That(_subject.BookMetadata(_author, _file), Is.Null);
        }
    }
}
