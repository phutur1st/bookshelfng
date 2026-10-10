# Hom3lab fork

This personal fork follows snapetech/bookshelfng. Its initial patch is based on
upstream revision `9f8621daf4f217119fbedaf4cba05b62324d1953`, matching the deployed
Hardcover image. Upstream remotes are fetch-only in the working checkout.

The manual Hom3lab workflow retains the upstream backend/frontend build, complete
Linux unit test command, and required Chromium tests. After those succeed, it
publishes only `ghcr.io/phutur1st/bookshelfng:hom3lab` and a source SHA tag.
Deployment uses the immutable image digest. It does not sync external package
repositories or post announcements. The assembly version remains 0.4.21.65 for
this patch; the image revision identifies the fork changes.

The import fix checks format compatibility before applying destination-folder
safety checks. Other formats remain tracked in their own library folders. The
existing same-format upgrade and wrong-folder protection remain covered by tests.
Affected author folders must be rescanned to restore already detached records.

Changes and workflow preparation were assisted by OpenAI Codex.

ImageSharp 4.1.2 requires a Six Labors license key during compilation. Set the
`SIXLABORS_LICENSE_KEY` repository Actions secret; the workflow passes it as the
supported `SixLaborsLicenseKey` MSBuild property through the build environment.
The workflow supplies the key only to the build environment; do not commit it to
source. The direct consuming assembly is `Readarr.Core`. Six Labors offers
application-based community licensing at https://licensing.sixlabors.com/.
