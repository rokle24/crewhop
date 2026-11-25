# CrewHop Business Portal

Blazor WebAssembly portal for venue operators: sign in, review venues, create promotions, launch ad campaigns, and browse performance stats. Data is backed by a JSON seed file and browser storage for this slice of the product.

## Run locally

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet restore
dotnet run --urls http://127.0.0.1:3847
```

Open [http://127.0.0.1:3847](http://127.0.0.1:3847) and sign in with the demo profile:

- Email: `demo@crewhop.com`
- Password: `demo`
- Display name: `Demo User`

## What’s included

- Single demo login
- Dashboard with venues, promotions, and stats
- Venues overview with cover photos (changeable via upload)
- Promotions list + create form + performance metrics
- Ad campaign planner per venue
- CrewHop color palette, Plus Jakarta Sans, and official logo

## Brand assets

Official logo: `wwwroot/images/crewhop-logo-full.png` (plus `@2x` / `1x` sizes). Pin mark / favicon: `wwwroot/images/crewhop-mark.png`.

## Refresh static host build

```bash
dotnet publish -c Release -o /tmp/crewhop-publish
rm -rf hosted && mkdir -p hosted
cp -a /tmp/crewhop-publish/wwwroot/. hosted/
rm -f hosted/_framework/*.pdb*
sed -i 's|<base href="/" />|<base href="/crewhop/" />|' hosted/index.html
cp hosted/index.html hosted/200.html
cp hosted/index.html hosted/404.html
```
