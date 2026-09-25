# Clipwell changelog film

This Remotion source uses **real Clipwell captures**. `prepare-assets.sh` downloads the
currently published light and dark picker screenshots and 3.6-second usage clips,
then checks their SHA-256 digests so a later asset change must be reviewed. The
captures and output files stay under ignored `public/` and `out/` paths; publish
approved files to the shared media host only after visual and audio review.

The 12-second composition has three acts: recall, live filtering, and the
privacy boundary added in source commit `d14aa3a`. On-screen annotation cards
identify what is shown. `ClipwellPosterLight` and `ClipwellPosterDark` are separately
art-directed stills, not arbitrary frames. Both themes share one narration.

This is a **candidate** composition using the older picker/usage captures.
The reviewed September 25 source-run web-picker images and silent footage are
published under `media/clipwell/media/i17-2026-09-25-db2d650/` with exact
hashes in its media manifest. Before a final film render, retarget the
composition to those current product pixels and review annotation placement in
both themes; the older boundary highlight must not be presented as proof of a
new MCP response. The existing paired poster drafts are source-run covers,
not frames from a finished narrated film.

```
npm ci
npm run prepare:assets
npm run typecheck
npm run compositions
```

For narration, choose and review a [Cartesia voice](https://docs.cartesia.ai/)
and set `CARTESIA_VOICE_ID`. Pass the API key through stdin with
`npm run narrate -- --stdin-key` (or through a protected `CARTESIA_API_KEY`
process environment); the script never logs the key. It makes a paid external
TTS call and saves `public/narration.wav`. Listen to the
result and check it fits the 12-second cut. The script intentionally refuses to
make a call without both values. No key, voice, or narration audio is committed.
`captions-draft.vtt` maps the approved wording to the three 12-second acts but
is **not synchronized** to a voice performance. Re-time it against the actual
reviewed WAV, as Dashcam did with editorial VTT timing, and mux/extract-check
captions in both final MP4s. Dashcam's localhost one-shot voice handoff can be
reused without copying or logging its secret; the missing inputs here are a
fresh key and a selected/reviewed Clipwell voice. Do not run the TTS request
until those inputs exist.

After review and resource coordination, render with the Remotion CLI:

Run these commands **from `media/changelog/`** so `staticFile()` resolves its
ignored `public/` directory.

```
npx remotion still src/index.ts ClipwellPosterLight out/poster-light.png
npx remotion still src/index.ts ClipwellPosterDark out/poster-dark.png
npx remotion render src/index.ts ClipwellFilmLight out/film-light.mp4 --concurrency=2
npx remotion render src/index.ts ClipwellFilmDark out/film-dark.mp4 --concurrency=2
```

Verify both themes visually, listen to the mixed film end to end, check captions
against footage, then upload as approved changelog media. The website currently
uses the existing silent themed WebMs and an annotated cover; it does not claim
the narrated film has been produced or published.
