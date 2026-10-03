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
- Runs are serialized per ref: at most one run per ref is in progress, and a
  newly queued run replaces any pending one. Each run publishes the commit it
  was started for, and the site shows whichever run deployed last. The workflow
  does not check that this commit is still the tip of `main`.
- Pushes to `main` run in order, so without manual re-runs the newest push is
  the last to deploy, provided its run succeeds.
- Re-running an older `main` run redeploys that run's commit. This is the
  intended rollback path, and a tip-of-`main` check would remove it. To return
  to the latest version, re-run the newest run or push to `main`.
- A new push to a pull request cancels that pull request's in-progress run.

## Fonts

The page embeds Chakra Petch and IBM Plex Mono, both licensed under the SIL Open
Font License 1.1. The license texts are in `licenses/OFL-ChakraPetch.txt` and
`licenses/OFL-IBMPlexMono.txt`.

## Capture hooks

The page intentionally keeps the `window.seek(t)` hook and its related globals
(`__seam`, `__ready`, `render`). They allow deterministic frame-by-frame capture
of the page animation for screenshots and recordings. Do not remove them.
