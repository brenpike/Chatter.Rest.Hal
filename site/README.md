# Chatter.Rest.Hal landing site

This directory holds the source for the project landing page, published at
https://brenpike.github.io/Chatter.Rest.Hal/.

## Files

- `index.src.html` is the hand-edited page source.
- `build.js` inlines the web fonts into `index.src.html` and writes `index.html`.
- `index.html` is committed build output. Never edit it by hand.
- `licenses/` contains the font license texts.

## Rebuilding

```sh
cd site
npm ci
node build.js
```

Commit the changed source and the regenerated `index.html` together.

## Deployment

The GitHub Pages source for this repository is set to GitHub Actions.
`.github/workflows/pages.yml` deploys the site on every push to `main` that touches
`site/**`, and can also be run manually through workflow dispatch.

## Fonts

The page embeds Chakra Petch and IBM Plex Mono, both licensed under the SIL Open
Font License 1.1. The license texts are in `licenses/OFL-ChakraPetch.txt` and
`licenses/OFL-IBMPlexMono.txt`.

## Capture hooks

The page intentionally keeps the `window.seek(t)` hook and its related globals
(`__seam`, `__ready`, `render`). They allow deterministic frame-by-frame capture
of the page animation for screenshots and recordings. Do not remove them.
