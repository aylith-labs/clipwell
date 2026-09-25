# Clipwell current release-media progress — 2026-09-25

## Product story and boundary

The September 24 source update (`d14aa3a`) makes flagged clipboard items absent
from both MCP recent/search reads and refuses direct MCP get-text. A trusted local
REST picker still sees the item so its owner can manage the flag. The film must
show both sides of that boundary; “sensitive stays yours” must never imply that
all local API clients are denied access.

There are no numbered release tags for this update. Call it a dated source
update. Existing hosted `picker-{light,dark}.png` and silent
`usage-{light,dark}.webm` show the product but do not prove the new MCP filter.
The 12-second Remotion composition and paired poster source in
`media/changelog/` are candidates. Cartesia audio and final films are absent.

## Authentic synthetic capture plan

Use an isolated `CLIPWELL_DATA_DIR` under `/var/tmp`, with
`CLIPWELL_NO_WATCH=1` and `CLIPWELL_ALLOW_SEED=1`; never read the real clipboard
history. The gated `POST /api/clipboard/_seed` path can add fictional rows
directly. Mark one fictional row sensitive through the actual local REST action,
then capture the browser-served `/app` Solid picker in both themes. Show the
flagged row masked in the picker, a search/filter state, and (if visually
demonstrable) the MCP omission as a clearly identified request/result, without
substituting a mock product screen. Identify the surface as Clipwell's web
picker, not the Windows Avalonia picker. Windows-only `bench/capture-*.ps1`
cannot run on this Linux host; this is a distinct real product capture.

Capture paired screenshots and product-motion footage with the same fixture and
matching dimensions. Record source commit, fixture text, browser/daemon flags,
dimensions, duration, bytes and SHA-256. Build an annotated, release-specific
light/dark poster from verified product pixels, with readable flagged row and
the precise MCP boundary. Stage new immutable paths in `aylith-media` for
independent visual/truth review before main or publication.

## Remaining gates

- Independent visual/truth review of the source-run capture candidates before
  merging or publishing them.
- Review a licensed Cartesia voice, generate and listen to the narration; no
  credential is persistently available, so no paid call or finished audio is
  claimed here.
- Render and inspect both Remotion films with clear timed captions and
  annotations, including the actual sensitive-row/MCP boundary.
- Verify media-host bytes and SHA-256 before linking new assets from either
  changelog. Current silent clips remain labeled silent until replacement.

## Source-run capture receipt

The bounded Linux capture used an official isolated .NET SDK 10.0.401 under
`/var/tmp`, not a system install. The daemon Release build passed with zero
warnings/errors and the frozen Bun web-picker install/build passed. With the
watcher and sweep disabled, seven fictional rows were seeded in an isolated
database; one was flagged sensitive through the normal API and one pinned.
The actual browser-served `/app` picker produced matched 1024×800 light/dark
overview and `SAMPLE-ONLY` sensitive-search PNGs, plus paired silent VP8/WebM
filtering footage (5.32 s light, 4.96 s dark). These are source clips, not the
finished 12-second narrated film. Two 1600×900 draft posters compose real
overview pixels with precise MCP-vs-picker annotations and an explicit
synthetic-fixture label. The daemon and browser were stopped after capture;
the real clipboard was not read.

All assets, exact hashes, source/fixture provenance, poster HTML, and a
storyboard are durable on the isolated `aylith-media` review branch
`i17-clipwell-web-picker-media` at `9d883aceb91e95dacb3b77c22231d1b67024671d`,
under `media/clipwell/media/i17-2026-09-25-db2d650/`. The media pair gate passed:
211 files, 81 matched themed pairs. The branch is not merged or hosted; final
poster and video reviews, Cartesia narration, Remotion film and captions remain
open.
