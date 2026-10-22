# Library metadata: ebook and audiobook OPF sidecars

BookshelfNG can write OPF metadata files beside ebooks and audiobooks for
BookOrbit, Audiobookshelf, and other OPF-aware readers. Enable **Library OPF**
under **Settings > Metadata**. Existing configurations retain the same provider
identity, even if their saved display name still says Audiobookshelf OPF.
The provider remains optional and disabled by default.

For each supported ebook or audiobook, the OPF file includes the title, author, available ISBN or ASIN,
description, publisher, language, release year, genres, narrator, and selected series
name and position. A metadata profile's preferred series is used when it
matches; otherwise BookshelfNG uses the provider's primary series and then
series position. For multi-part books, BookshelfNG writes one sidecar for the
first part. The OPF file follows the book file when BookshelfNG moves or
renames it.

## Narrator metadata

When a metadata provider supplies an edition narrator, BookshelfNG stores it
separately from the book author. Narrator values appear in book search and can
be used to filter the library. Provider coverage varies; Hardcover maps
narrator, reader, reading, and author/narrator contribution roles into the
narrator field.

During audiobook import identification, BookshelfNG compares narrator credits
read from the audio file with the candidate edition's narrator when both are
available. A matching narrator helps rank similar audiobook editions; missing
narrator data on either side does not count against a candidate.

Audio tag reading recognizes narrator fields in common audiobook tags,
including ID3, Vorbis comments, APE, ASF/WMA, MP4, and Audible metadata. Tag
writing is controlled by **Settings > Metadata > Write Audio Tags**. When
enabled, BookshelfNG writes the selected edition's narrator using the audio
format's tag fields.

For Vorbis comments, BookshelfNG reads all `PERFORMER` values as narrator
credits and falls back to the older `NARRATOR` field when no performer value
exists. When writing Vorbis tags, it writes narrator credits to `PERFORMER`
and clears the legacy `NARRATOR` field, leaving the author credit separate.

Point Audiobookshelf at the same media folders and run its normal library scan
or let its file watcher detect the sidecars. Audiobookshelf reads OPF metadata
from the library files; it does not need an API token or a direct connection to
BookshelfNG. If audio tags contain different values, check Audiobookshelf's
metadata precedence in the library settings.

See the [Audiobookshelf book library documentation](https://audiobookshelf.org/docs/documentation/libraries/book-library/directory-structure/)
for its supported directory layout and OPF sidecar fields.

## Ebooks and read-only readers

The OPF provider supports EPUB/KEPUB, MOBI, AZW3, PDF, and the supported audio
formats without a Calibre connection. It writes a sidecar with the media file's
stem, leaving ebook archives and audio streams unchanged. Embedded ebook tag
writing still uses the separate Calibre integration; this provider does not
change that behavior or enable audio tag writing.

Set BookOrbit's library metadata precedence to OPF before embedded tags, then
scan the affected library. BookOrbit prefers an existing `metadata.opf` over a
stem-matched OPF. Review and back up older sidecars before replacing or retiring
them; otherwise a stale Calibre sidecar can keep winning over a new export.
Provider-based Refresh Metadata in a reader is separate from loading source
file metadata. Preserve manual field locks when reconciling reader metadata.

Review catalog matches before exporting. Sidecars reflect the selected edition
and do not repair an incorrect edition match. Back up existing OPFs before the
first backfill, verify a representative ebook and audiobook, and keep readers
read-only. To roll back, disable the provider, restore the saved sidecars, and
rescan the readers; do not restore older reading-progress databases merely to
undo a metadata change. Download and seeding locations are not export targets.

### Auditing existing matches

Enable **Verify Source Title** during an existing-library backfill. The publisher
then requires the selected edition title to match embedded book tags, the file
stem, or the containing folder (ignoring punctuation, case, audiobook edition
markers and trailing Calibre numeric folder IDs). Unreadable tags fail closed.
A conflicting match is logged and no sidecar is written. This check does not
search for books, change assignments, download anything, or modify media.
It is optional and off by default to preserve existing provider behavior.
Review skipped and unmapped books in BookshelfNG before publishing them.
