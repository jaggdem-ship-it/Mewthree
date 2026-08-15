# GitHub Pages deployment for the Fable game

This repository now includes `.github/workflows/pages.yml`. Every push to `main`, or a manual workflow dispatch, restores the local Fable tool, restores the .NET project, compiles `phase1-fable/GameApp.fsproj`, assembles a static `_site` directory, and deploys it through the official GitHub Pages artifact workflow.

## Repository configuration

Open the repository on GitHub and go to **Settings → Pages**. Set **Source** to **GitHub Actions**. The workflow grants `pages: write` and `id-token: write`, which are required by the Pages deployment action. No personal access token or deploy key is needed for the GitHub Actions deployment itself.

The workflow uses .NET 10 because the repository's Fable 5.13.0 tool payload targets `net10.0`. It runs `dotnet tool restore` from `phase1-fable`, then invokes:

```bash
dotnet fable phase1-fable/GameApp.fsproj --outDir phase1-fable/build
```

The build copies `phase1-fable/index.html` to the artifact root and copies the complete Fable output, including `fable_modules`, beside it. The HTML entrypoint therefore loads `./App.js`, not `./dist/App.js`.

## Activation

Commit and push `.github/workflows/pages.yml`, `GITHUB_PAGES.md`, and the corrected `phase1-fable/index.html` to `main`. Open the repository's **Actions** tab and select **Build and deploy Fable game to GitHub Pages**. After the workflow completes, the deploy job exposes the published URL in its environment summary and in the workflow run's deployment section.

For a project repository, the default URL is normally:

```text
https://<owner>.github.io/<repository>/
```

The current repository is `jaggdem-ship-it/Mewthree`, so its expected Pages URL is:

```text
https://jaggdem-ship-it.github.io/Mewthree/
```

## Troubleshooting

If the page loads but the game is blank, inspect the browser console and Network panel. The most common cause is an incorrect relative script path or a missing `fable_modules` directory in the uploaded artifact. The workflow copies both the generated JavaScript and its module directory.

If the workflow is skipped, verify that the workflow file is on the `main` branch and that Actions are enabled for the repository. If deployment fails with a permissions error, confirm that Pages uses **GitHub Actions** as its source and that repository Actions are allowed to create Pages deployments.

If the game expects root-relative assets, change those references to relative paths such as `./textures/example.png`, or calculate paths from `import.meta.url`. This keeps asset loading compatible with the repository subpath used by GitHub Pages.

## Local reproduction

You can reproduce the build locally from the repository root with:

```bash
cd phase1-fable
dotnet tool restore
dotnet restore GameApp.fsproj
dotnet fable GameApp.fsproj --outDir build
rm -rf ../_site
mkdir -p ../_site
cp index.html ../_site/index.html
cp -R build/. ../_site/
cd ../_site
python3 -m http.server 8080
```

Then open `http://localhost:8080/` and check the browser console for module or CDN errors.
