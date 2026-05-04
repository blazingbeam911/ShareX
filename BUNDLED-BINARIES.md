# Bundled native binaries

This fork bundles three pre-compiled Windows DLLs in `ShareX/` that the AVIF
and WebP image format support (PR #8151) depend on:

| File              | Purpose                                  |
| ----------------- | ---------------------------------------- |
| `avif.dll`        | libavif — AVIF encode/decode             |
| `libwebp.dll`     | Google libwebp — WebP encode/decode      |
| `libsharpyuv.dll` | YUV color-space helper (libwebp project) |

## Provenance of the currently-committed DLLs

Each DLL was added to the upstream `ShareX/ShareX` PR #8151 by a third-party
contributor and **was not built by us from source we audited**. Trust chain:

| File              | Originally added by    | Currently from commit                                     |
| ----------------- | ---------------------- | --------------------------------------------------------- |
| `libwebp.dll`     | `ooopus` (Apr 2025)    | `a8d6a1555` "feat: add WebP image format support"         |
| `libsharpyuv.dll` | `ooopus` (Apr 2025)    | `a8d6a1555` "feat: add WebP image format support"         |
| `avif.dll`        | `ooopus` (Apr 2025)    | `260f7ece9` "update avif.dll to v1.4.0-559c589" (Delphox) |

**As of this fork's `feature/replace-with-vcpkg-builds` branch (merged
into `custom`), the committed DLLs were built by our own
`.github/workflows/build-deps.yml` workflow** at vcpkg ref `2025.12.12`,
on a `windows-latest` GitHub Actions runner. The trust chain is now:
us → Microsoft's vcpkg ports → upstream libavif/libwebp source.

Re-running the workflow at the same vcpkg ref produces functionally
equivalent binaries with different SHA-256 hashes (MSVC's default builds
embed timestamps and other non-deterministic data). Provenance is
verifiable; bit-identity is not.

## How to verify or replace them with binaries we built ourselves

Use the GitHub Actions workflow `.github/workflows/build-deps.yml`:

1. On github.com, navigate to `Actions` → `Build native dependencies from source`.
2. Click `Run workflow`. Optionally change the `vcpkg_ref` input from the
   default (currently `2025.12.12`) to a different vcpkg release tag.
3. Wait ~15–25 minutes for the build to complete.
4. Download the `verified-dlls-<ref>` artifact from the workflow run.

The workflow:

- Clones [vcpkg](https://github.com/microsoft/vcpkg) at the pinned ref.
- Installs `libavif[core,aom,dav1d]` and `libwebp` from vcpkg's curated ports
  (which fetch from the official upstream sources at versions vcpkg has
  vetted).
- Builds them with MSVC on a `windows-latest` runner.
- Reports SHA-256 of both the freshly-built DLLs **and** the
  currently-committed ones, side-by-side.
- Uploads the freshly-built DLLs as an artifact.

## Why the hashes will differ

You'll see the freshly-built hashes differ from the committed hashes even
when the source is identical. MSVC embeds non-deterministic data (build
timestamps, PDB GUIDs, file path strings) into binaries by default. Two
clean builds of the same source produce different bytes. The verification
the workflow gives is **provenance** ("these DLLs come from a specific
upstream source commit, built with a known toolchain"), not bit-identity.

If you want bit-identity, that requires `/Brepro` linker flags and other
deterministic-build configuration that's beyond what vcpkg currently exposes.
We accept the trade-off.

## How to swap committed DLLs for the verified ones

If you want to replace the in-tree DLLs with the vcpkg-built ones:

1. Run the workflow as above. Download the `verified-dlls-<ref>` artifact.
2. From the artifact, copy `avif.dll`, `libwebp.dll`, and `libsharpyuv.dll`
   into `ShareX/` of a clean clone, replacing the existing files.
3. Commit on a feature branch (e.g. `feature/replace-with-vcpkg-builds`),
   merge into `custom`, push.
4. Rebuild ShareX. The new build will load the vcpkg-built DLLs.

After this, your `custom` branch's bundled binaries are auditable: anyone can
re-run the workflow with the same `vcpkg_ref` to verify the source is
upstream-clean.

## Updating to newer upstream versions

To pull in newer libavif or libwebp:

1. Update `vcpkg_ref` in `.github/workflows/build-deps.yml` to a newer vcpkg
   release tag (check https://github.com/microsoft/vcpkg/releases).
2. Run the workflow, get the artifact.
3. Swap the DLLs as above.

vcpkg's port versions advance independently of vcpkg release tags, so a newer
vcpkg release usually means newer libavif/libwebp by side effect.

## Caveats

- vcpkg is itself a Microsoft project that fetches third-party source. We're
  trusting Microsoft to vet vcpkg's ports, and trusting the upstream projects
  (Google's libwebp, AOMediaCodec's libavif) not to ship malicious source.
  This is a smaller and better-known trust set than the original chain
  (`ooopus`, `cryptofyre`, `Delphox`), but it's not zero-trust.
- The `x64-windows` triplet builds dynamic-runtime MSVC DLLs. ShareX's P/Invoke
  signatures are compatible with this. If we ever needed a static-runtime
  build (`x64-windows-static`), the DLLs would still work but would be larger.
- libavif's full Windows build with all encoders bundled is large (~8 MB).
  The committed `avif.dll` is 8.6 MB; vcpkg's tends to produce something in
  the same range.
