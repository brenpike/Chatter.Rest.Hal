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
`.github/workflows/pages.yml` publishes the committed `index.html` and the
`licenses/` texts. Before publishing it reinstalls the pinned font packages
(`npm ci`), runs `build.js`, and fails if the output differs from the committed
`index.html`, so rebuild and commit `index.html` before merging.

- A push to `main` that touches `site/**` or the workflow file runs `pages-build`
  and then `pages-deploy`.
- A pull request against `main` that touches the same paths runs only
  `pages-build`. It never deploys.
- A manual workflow dispatch deploys only when run from `main`. From any other
  branch or tag, `pages-deploy` is skipped and the published site does not change.
- Runs are serialized per ref, so at most one run per ref is in progress.
  GitHub does not guarantee the order in which queued runs start, so an older
  commit can occasionally deploy after a newer one.
- Each run publishes the commit it was started for, and the last deploy to
  finish wins. The workflow does not check that this commit is still the tip
  of `main`.
- Re-running an older `main` run redeploys that run's commit. This is the
  intended rollback path.
- If the published site looks stale, re-run the newest `main` run or push to
  `main`.
- A new push to a pull request cancels that pull request's in-progress run.

## Fonts

The page embeds Chakra Petch and IBM Plex Mono, both licensed under the SIL Open
Font License 1.1. The license texts are in `licenses/OFL-ChakraPetch.txt` and
`licenses/OFL-IBMPlexMono.txt`.

## Capture hooks

The page intentionally keeps the `window.seek(t)` hook and its related globals
(`__seam`, `__ready`, `render`). They allow deterministic frame-by-frame capture
of the page animation for screenshots and recordings. Do not remove them.

## Review checklist

- **Accessibility:** the landing copy is in the accessibility tree at every width.
  `.mcopy` is the text source and is never `display:none`. The animated stages stay `aria-hidden`.
- **Motion:** every environment input (width, reduced motion, resize) goes through
  `applyPresentation()`. Never branch on `.matches` elsewhere or decide presentation once at load.
  Reduced motion shows the end-state frame with no animation. Toggling it at runtime stops or
  resumes the driver. After `seek()`, environment changes re-layout and re-render but never restart the driver.
- **HAL fidelity:** the Act-2 JSON pane must be token-identical to real serializer output (whitespace aside)
  for the Act-2 builder code. Verify against real Chatter.Rest.Hal output when either changes.
- **Render parity:** unrelated edits change no pixels.
