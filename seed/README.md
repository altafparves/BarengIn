# Seed Data

Committed static reference data, generated once and then treated as source.

The main artefact here is the OSM Overpass output for campus pickup points around UGM. It is queried once by hand, saved here, and committed so every teammate and the CI pipeline get identical data without calling the Overpass API. Regenerate it deliberately, in its own pull request, and say why in the commit message.

Anything ending in `.local.json` is gitignored scratch output and must not be committed.
