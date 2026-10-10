---
category: changed
audience: operators
area: release-pipeline
action: Set the SIXLABORS_LICENSE_KEY repository Actions secret before building.
breaking: false
---
The personal Bookshelf image build accepts its ImageSharp license through a GitHub Actions secret, allowing patched dependency builds without storing the key in source.
