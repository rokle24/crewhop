# CrewHop Business Portal

Blazor WebAssembly business portal for coordinating crew, shifts, and venue promos.

## Run locally

```bash
dotnet run
```

Open the printed localhost URL. Sign in from the splash (demo only — no credentials), then use the sidebar for Dashboard, Crew, Shifts, and Potato promo.

Demo data lives in `wwwroot/data/seed.json` and is cached in browser `localStorage`.

## Static build

Release output for hosting is committed under `hosted/` (base href set for GitHub Pages project site `/crewhop/`).

```bash
dotnet publish -c Release -o ./publish-out
# then copy publish-out/wwwroot → hosted and set <base href="/crewhop/" />
```

## GitHub Pages

Pushing `main` to GitHub runs `.github/workflows/pages.yml`, which deploys the `hosted/` folder.

Live site: https://rokle24.github.io/crewhop/
