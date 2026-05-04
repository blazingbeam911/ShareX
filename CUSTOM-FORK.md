# Custom local ShareX build

This is a fork of [ShareX/ShareX](https://github.com/ShareX/ShareX) that
integrates a few patches not yet merged upstream:

- [PR #8151](https://github.com/ShareX/ShareX/pull/8151) — WebP, AVIF, and
  AV1 image/video format support (cryptofyre + ooopus).
- Three small enhancements from [Delphox/ShareX](https://github.com/Delphox/ShareX)
  that build on PR #8151: AV1 CRF support, a fix for double-codec FFmpeg
  arguments, and a libavif update.
- An auto-updater repoint so future custom builds pull from this fork's
  releases instead of upstream's, avoiding accidental overwrite.

## Branch model

```
upstream/develop  (the source of truth, read-only)
        |
        v
develop  (mirrors upstream/develop, never committed to directly)
        |
        +-- feature/pr-8151-webp-avif-av1     [9 commits from cryptofyre+ooopus]
        |       |
        |       +-- feature/delphox-av1-crf            [1 commit]
        |       |       |
        |       |       +-- feature/delphox-codec-fix  [1 commit, chained on av1-crf]
        |       |
        |       +-- feature/delphox-libavif-update     [1 commit]
        |
        +-- feature/pr-8151-webp-avif-av1
        |       |
        |       +-- feature/webp-encoding-options       [5 commits]
        |
        +-- feature/repoint-updater                    [1 commit, independent]
        |
        +-- feature/build-deps-from-source             [1 commit, fork-native]
        |       (adds .github/workflows/build-deps.yml + BUNDLED-BINARIES.md)
        |
        +-- feature/replace-with-vcpkg-builds          [1 commit, fork-native]
                (replaces the third-party native DLLs with vcpkg-built ones)

custom  (develop + every feature/* branch merged with --no-ff)
```

`custom` is the branch that gets built and released. Its tip is rebuilt every
time the upstream-sync workflow runs; treat anything you do directly on
`custom` as ephemeral.

## Per-branch quick reference

| Branch                                     | Source                                                                | Purpose                                                               |
| ------------------------------------------ | --------------------------------------------------------------------- | --------------------------------------------------------------------- |
| `feature/pr-8151-webp-avif-av1`            | [PR #8151](https://github.com/ShareX/ShareX/pull/8151)                | WebP / AVIF / AV1 codec support, plus AVIF IQ tuning UI               |
| `feature/delphox-av1-crf`                  | [Delphox 643176fe5](https://github.com/Delphox/ShareX/commit/643176fe5) | Reuse the x264 tab for AV1 so CRF mode works                          |
| `feature/delphox-codec-fix`                | [Delphox 1246b2a0f](https://github.com/Delphox/ShareX/commit/1246b2a0f) | Fix double-codec-appending bug in FFmpeg argument builder             |
| `feature/delphox-libavif-update`           | [Delphox 45a1ce6a7](https://github.com/Delphox/ShareX/commit/45a1ce6a7) | Update bundled `avif.dll` to v1.4.0 + dshow regex fix for new FFmpeg  |
| `feature/webp-encoding-options`            | (us)                                                                  | Configurable WebP encoder: mode (lossy/near-lossless/lossless), quality, method, preset, near-lossless level, alpha quality, exact RGB |
| `feature/repoint-updater`                  | (us, based on Delphox 009a084d3 pattern)                              | Patch in-app updater to check `blazingbeam911/ShareX` instead of upstream |
| `feature/build-deps-from-source`           | (us)                                                                  | Adds `build-deps.yml` workflow and `BUNDLED-BINARIES.md`              |
| `feature/replace-with-vcpkg-builds`        | (us)                                                                  | Replaces the third-party `avif.dll`, `libwebp.dll`, `libsharpyuv.dll` with vcpkg-built versions |

## CI workflows

Located in `.github/workflows/`:

- **`build.yml`** — inherited from upstream + customized. Triggers on push to
  any branch, on tags matching `v[0-9]+.[0-9]+.[0-9A-Za-z.-]+` (loosened to
  accept our `v20.0.4-custom-...` style tags), and on manual dispatch. Builds
  the full matrix (Release/Debug/Steam/MicrosoftStore × x64/ARM64) and
  publishes Dev releases on push-to-`custom` and Stable releases on tag push.
  Uses the auto-provided `GITHUB_TOKEN` so no secrets need configuring.
- **`upstream-sync.yml`** — runs Mondays 06:00 UTC and on manual dispatch.
  Fetches `ShareX/ShareX@develop`, fast-forwards our `develop`, rebases each
  cherry-picked feature branch onto its base, and rebuilds `custom` by
  re-merging everything. Opens an issue if any rebase or merge conflicts.
- **`release.yml`** — manual dispatch only. Tags the current `custom` tip as `v<base-version>.<run-number>` (e.g.
  `v20.0.4.5` for the 5th time you run this workflow) and pushes the tag, which
  causes `build.yml` to publish a Stable release.
- **`build-deps.yml`** — manual dispatch only. Builds `libwebp` and
  `libavif` from upstream source via vcpkg (pinned ref) on a Windows runner
  and uploads the DLLs as a workflow artifact. Used to verify or refresh the
  bundled native binaries. See `BUNDLED-BINARIES.md` for details.

## Building locally

You need .NET 9 SDK, Windows 10 SDK 10.0.22621 (or newer), and a NuGet
source configured (`dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org`
if `dotnet nuget list source` says "No sources found.").

```powershell
cd C:\dev\ShareX
git checkout custom
dotnet restore --runtime win-x64 ShareX\ShareX.csproj
dotnet build --no-restore --configuration Release -p:Platform=x64 --self-contained true ShareX\ShareX.csproj
```

That produces `ShareX\bin\Release\win-x64\ShareX.exe`. To make it use a
portable config folder (recommended for testing alongside an installed
upstream copy), drop an empty file named `Portable` next to the .exe before
launching.

To build the proper installer, install [Inno Setup](https://jrsoftware.org/isdl.php)
and additionally:

```powershell
dotnet build --no-restore -c Release -p:Platform=x64 --self-contained true ShareX.Setup\ShareX.Setup.csproj
& "ShareX.Setup\bin\Release\win-x64\ShareX.Setup.exe" -silent -job Release -platform x64
```

The installer ends up in `Output\ShareX-<version>-setup-x64.exe`.

To build the Steam launcher (only matters if you're shipping to Steam):
```powershell
dotnet restore --runtime win-x86 ShareX.Steam\ShareX.Steam.csproj
dotnet build --no-restore -c Steam -p:Platform=x86 ShareX.Steam\ShareX.Steam.csproj
```

(ShareX.Steam targets `net48` and only supports x86, so building it as part
of the full solution with `-p:Platform=x64` will fail.)

## Adding a new patch

When you want to integrate a new upstream PR or a new fix that isn't yet in
upstream:

```powershell
cd C:\dev\ShareX

# Fetch the source (a PR ref, an upstream commit, a Delphox commit, etc.)
git fetch upstream pull/NNNN/head:pr-NNNN

# Branch from develop (for an independent patch) or from another feature
# branch (if it depends on one).
git checkout -b feature/short-description develop
git cherry-pick <commits>

# Resolve conflicts as needed, build to verify, then push.
git push -u origin feature/short-description

# Add it to the merge order in custom
git checkout custom
git merge --no-ff feature/short-description -m "Merge feature/short-description into custom"
git push origin custom --force-with-lease

# Update the Per-branch quick reference table in this file.
```

Then add the new branch to the rebase list in
`.github/workflows/upstream-sync.yml` (the `CHAIN` array for cherry-picked
patches, or the merge order in the "Rebuild custom branch" step for
fork-native ones).

## Removing a patch (e.g. when upstream merges it)

When upstream merges one of our patches (say upstream merges PR #8151), the
patch becomes redundant on the next upstream sync because the cherry-picked
commits will already be in `develop`. To remove the now-redundant branch:

```powershell
# Delete locally and on the fork
git branch -D feature/pr-8151-webp-avif-av1
git push origin --delete feature/pr-8151-webp-avif-av1

# Remove from upstream-sync.yml's CHAIN array and from custom's merge order.
# Update this file's Per-branch quick reference table.
# Commit the cleanup and push.
```

The next upstream-sync run will rebuild `custom` without it.

## Auto-updater behavior

The in-app updater (`ShareX.ShareXUpdateManager`) has been patched (via
`feature/repoint-updater`) to check `blazingbeam911/ShareX/releases` instead
of the upstream `ShareX/ShareX/releases`. So when you click "Check for
updates" inside ShareX, it'll find the latest tagged release on this fork —
not on upstream — and offer to install it. This prevents your custom build
from being silently overwritten by an upstream release.

## Bundled native binaries

`ShareX/avif.dll`, `ShareX/libwebp.dll`, and `ShareX/libsharpyuv.dll` are
pre-compiled DLLs that ship in the build output. They were built by our own
`build-deps.yml` workflow from vcpkg-vetted upstream source. See
`BUNDLED-BINARIES.md` for full provenance and how to verify or refresh them.

## License

Same as upstream: GPL v3. See `LICENSE.txt`. This fork is not a commercial
product, makes no warranty, and is not affiliated with the ShareX project.
