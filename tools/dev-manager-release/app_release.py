"""Publish only a verified Dev Manager Windows installer to GitHub Releases.

Signature: OpenAI-Codex
"""

import base64
from datetime import datetime
import hashlib
import io
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import urllib.error
import urllib.request
from urllib.parse import quote
import zipfile


REPOSITORY = "KingsleyHK0323/Cities_Skylines_1_Modpack"
APP_BRANCH = "Cities_Skylines_1_Modpack_Dev_Manager"
ASSET_NAME = "Cities_Skylines_1_Modpack_Dev_Manager_Setup.exe"
NODE_SHA = "1c331340fa6cac5e4294725959070a143fa0507195a78ad77eed7943f719aac9"
NODE_LICENSE_SHA = "5888dbb9a1d2b18f2c3e6c5f6af1b39de658372b402a0577b002777f14c62ace"
THIRD_PARTY_NOTICE_SHA = "5767c6fbab782df8d7983972d71eae1ee12e04423098380565e246621a350c93"
PACKAGE_SIGNATURE = "OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager"
REQUIRED_SOURCE_FILES = {
    "LICENSE", "README.md", "USER-GUIDE.md", "ARTIFACT-CONTRACT.md",
    "artifact-source.mjs", "desktop-backend.mjs", "desktop-service.mjs", "registry.mjs",
}
# The package manifest is untrusted: every distributable source file must be
# explicitly approved, and every listed file is required for this App build.
ALLOWED_SOURCE_FILES = {
    "core.mjs", "filesystem.mjs", "github-source.mjs", "download.mjs",
    "https-source.mjs", "module-policy.mjs", "release-index.mjs", "bundle-source.mjs",
    "update-order.mjs", "asset-policy.mjs", "desktop-service.mjs", "desktop-backend.mjs",
    "registry.mjs", "LICENSE", "README.md", "USER-GUIDE.md", "PUBLISHER-CONTRACT.md",
    "SOURCE-CONTRACT.md", "Change Log.md", "artifact-source.mjs", "ARTIFACT-CONTRACT.md",
    "preferences.mjs", "localization.mjs", "translations.json", "settings-store.mjs", "TEST-READINESS.md",
}
FIXED_FILES = {"DevManager.exe", "runtime/node.exe", "runtime/LICENSE_NODE.txt", "THIRD-PARTY.md", "package-manifest.json"}


class PublicationError(Exception):
    """The package, source or remote Release state could not be verified."""


class NoRedirect(urllib.request.HTTPRedirectHandler):
    """Never forward the upload credential to a redirected endpoint."""

    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None


def require(condition, message):
    if not condition:
        raise PublicationError(message)


def valid_sha(value, length=40):
    return isinstance(value, str) and re.fullmatch(r"[a-f0-9]{" + str(length) + r"}", value) is not None


def _unique_object(pairs):
    result = {}
    for key, value in pairs:
        require(key not in result, "Duplicate JSON key")
        result[key] = value
    return result


def _safe_path(value):
    require(isinstance(value, str) and 0 < len(value) <= 240 and not value.startswith("/") and
            not re.search(r'[\x00-\x1f<>:"\\|?*]', value) and
            all(part and part not in (".", "..") and not part.endswith((".", " ")) and
                not re.fullmatch(r"(?i:CON|PRN|AUX|NUL|COM[1-9¹²³]|LPT[1-9¹²³])", part.split(".", 1)[0].rstrip(" "))
                for part in value.split("/")), "Unsafe package path")
    return value


def verify_package(data, *, runtime_sha=NODE_SHA, license_sha=NODE_LICENSE_SHA):
    require(isinstance(data, bytes) and 0 < len(data) <= 160 * 1024 * 1024, "Invalid package size")
    require(valid_sha(runtime_sha, 64) and valid_sha(license_sha, 64), "Invalid runtime policy")
    try:
        with zipfile.ZipFile(io.BytesIO(data)) as archive:
            infos = archive.infolist()
            require(0 < len(infos) <= 256, "Invalid package entry count")
            entries = {}
            folded = set()
            total = 0
            for info in infos:
                directory = info.is_dir()
                name = _safe_path(info.filename[:-1] if directory else info.filename)
                require(name.casefold() not in folded, "Duplicate package entry")
                folded.add(name.casefold())
                kind = (info.external_attr >> 16) & 0o170000
                require(kind in ((0, 0o040000) if directory else (0, 0o100000)), "Unsafe package entry type")
                if directory:
                    require(info.file_size == 0, "Invalid directory entry")
                    require(name == "runtime", "Unapproved package directory")
                    continue
                limit = 128 * 1024 * 1024 if name == "runtime/node.exe" else 32 * 1024 * 1024
                require(0 < info.file_size <= limit, "Invalid package file size")
                total += info.file_size
                require(total <= 200 * 1024 * 1024, "Package expands beyond its limit")
                entries[name] = info
            require("package-manifest.json" in entries and entries["package-manifest.json"].file_size <= 1048576,
                    "Package manifest missing")
            manifest = json.loads(archive.read(entries["package-manifest.json"]).decode("utf-8-sig"),
                                  object_pairs_hook=_unique_object)
            require(isinstance(manifest, dict) and manifest.get("schema") == 1 and manifest.get("runtime") == "v24.22.2",
                    "Invalid package manifest")
            require(set(manifest) == {"schema", "runtime", "runtimeSHA256", "nativeSHA256", "signature", "files"}
                    and manifest.get("signature") == PACKAGE_SIGNATURE, "Unapproved package metadata")
            expected = manifest.get("files")
            require(isinstance(expected, dict) and REQUIRED_SOURCE_FILES.issubset(expected), "Required App files missing")
            require(set(expected) == ALLOWED_SOURCE_FILES, "Unapproved or missing App source files")
            require(all(_safe_path(name) == name and "/" not in name and valid_sha(str(digest).lower(), 64)
                        for name, digest in expected.items()), "Invalid App file policy")
            require(not (set(expected) & FIXED_FILES) and set(entries) == set(expected) | FIXED_FILES,
                    "Unexpected package contents")
            notice = archive.read(entries["THIRD-PARTY.md"])
            require(hashlib.sha256(notice.replace(b"\r\n", b"\n").replace(b"\r", b"\n")).hexdigest() == THIRD_PARTY_NOTICE_SHA,
                    "Third-party notice differs from the approved text")
            require(str(manifest.get("runtimeSHA256", "")).lower() == runtime_sha and
                    valid_sha(str(manifest.get("nativeSHA256", "")).lower(), 64), "Package executable policy mismatch")
            checks = {name: str(digest).lower() for name, digest in expected.items()}
            checks.update({"runtime/node.exe": runtime_sha, "runtime/LICENSE_NODE.txt": license_sha,
                           "DevManager.exe": str(manifest["nativeSHA256"]).lower()})
            for name, expected_sha in checks.items():
                digest = hashlib.sha256()
                first = b""
                received = 0
                with archive.open(entries[name]) as stream:
                    while True:
                        chunk = stream.read(1024 * 1024)
                        if not chunk:
                            break
                        if not received:
                            first = chunk[:2]
                        received += len(chunk)
                        require(received <= entries[name].file_size, "Package file expanded unexpectedly")
                        digest.update(chunk)
                require(received == entries[name].file_size and digest.hexdigest() == expected_sha,
                        "Package file checksum mismatch")
                if name in ("DevManager.exe", "runtime/node.exe"):
                    require(first == b"MZ", "Package executable is invalid")
    except (zipfile.BadZipFile, UnicodeError, ValueError, KeyError) as exc:
        raise PublicationError("Invalid portable package") from exc
    return hashlib.sha256(data).hexdigest()


def verify_setup(data):
    require(isinstance(data, bytes) and 2 < len(data) <= 200 * 1024 * 1024 and data.startswith(b"MZ"),
            "Invalid Windows installer")
    return hashlib.sha256(data).hexdigest()


def verify_build_identity(ctx, identity, payload, setup):
    require(identity == {**ctx, "payloadSha256": hashlib.sha256(payload).hexdigest(),
                         "setupSha256": hashlib.sha256(setup).hexdigest()},
            "Build identity does not match this release")


def _context(ctx):
    require(isinstance(ctx, dict) and ctx.get("repository") == REPOSITORY, "Unexpected repository")
    require(valid_sha(ctx.get("sourceSha")), "Invalid App source commit")
    version = ctx.get("version")
    require(isinstance(version, str) and len(version) <= 64 and
            re.fullmatch(r"v(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?", version),
            "Invalid App version")
    require(type(ctx.get("runId")) is int and ctx["runId"] > 0 and type(ctx.get("attempt")) is int and ctx["attempt"] > 0,
            "Invalid App build identity")
    return "dev-manager-" + version


def _body(ctx, payload_digest, setup_digest):
    identity = {"sourceSha": ctx["sourceSha"], "runId": ctx["runId"], "attempt": ctx["attempt"],
                "payloadSha256": payload_digest, "setupSha256": setup_digest}
    marker = "<!-- dev-manager-build:" + json.dumps(identity, sort_keys=True, separators=(",", ":")) + " -->"
    return "\n".join([
        "## Dev Manager", "",
        "Preview the Dev Manager for discovering and managing available Development modules individually.", "",
        "> Development modules can be unstable and may damage a save. Back up saves and use a separate test city.", "",
        "## Preview limitations", "",
        "- Some or all module sources may be unavailable or unconfigured; unavailable modules cannot be installed until verified builds are published.",
        "- The App does not launch the game automatically. Workshop coexistence detection is not complete.",
        "- Live game compatibility and the complete desktop experience still require testing.", "",
        "## Install and use", "",
        "1. Download and run the Windows installer on Windows x64 with .NET Framework available.",
        "2. Choose an empty application folder and complete setup. No separate Node.js installation is needed.",
        "3. Select the existing Cities: Skylines Mods folder, then select the available Development modules.",
        "4. Close the game before installing, updating or removing modules. Read `USER-GUIDE.md` for details.",
        "5. GitHub also shows automatically generated source-code archives. They are not the installer.", "",
        "## Build information", "",
        "- App source commit: `" + ctx["sourceSha"] + "`",
        "- Cloud build run / attempt: `" + str(ctx["runId"]) + " / " + str(ctx["attempt"]) + "`",
        "- Installer SHA-256: `" + setup_digest + "`",
        "- Verified internal App payload SHA-256: `" + payload_digest + "`", "",
        "Cloud build checks do not establish subscribed-game compatibility or gameplay acceptance.", "", marker,
    ])


def _release_list(api):
    releases = api.list_releases()
    require(isinstance(releases, list), "Release list unavailable")
    seen = set()
    for release in releases:
        require(isinstance(release, dict) and type(release.get("id")) is int and release["id"] > 0 and
                type(release.get("draft")) is bool and isinstance(release.get("tag_name"), str), "Invalid Release entry")
        tag = release["tag_name"]
        require(tag not in seen and re.fullmatch(r"dev-manager-v(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?", tag),
                "A non-App or ambiguous Release needs manual review")
        assets = release.get("assets")
        require(isinstance(assets, list) and len(assets) <= 1 and (release["draft"] or len(assets) == 1),
                "An App Release contains unexpected assets")
        if assets:
            asset = assets[0]
            require(isinstance(asset, dict) and asset.get("name") == ASSET_NAME and asset.get("state") == "uploaded" and
                    type(asset.get("size")) is int and 2 < asset["size"] <= 200 * 1024 * 1024 and
                    isinstance(asset.get("digest"), str) and re.fullmatch(r"sha256:[a-f0-9]{64}", asset["digest"]),
                    "An App Release contains an invalid installer")
        seen.add(tag)
    return releases


def _asset_matches(asset, data):
    return (isinstance(asset, dict) and asset.get("name") == ASSET_NAME and asset.get("state") == "uploaded" and
            asset.get("size") == len(data) and asset.get("digest") == "sha256:" + hashlib.sha256(data).hexdigest())


def _latest_title(ctx):
    return "[LATEST APP] Cities Skylines 1 Modpack Dev Manager — " + ctx["version"]


def _pending_title(ctx):
    return "[PENDING APP] Cities Skylines 1 Modpack Dev Manager — " + ctx["version"]


def _mark_older_historical(api, releases, current_id, changed=None):
    for old in releases:
        if old["id"] != current_id and old.get("draft") is False and str(old.get("name", "")).startswith("[LATEST APP]"):
            previous = old["name"]
            historical = previous.replace("[LATEST APP]", "[HISTORICAL APP]", 1)
            observed = api.release(old["id"])
            require(observed.get("id") == old["id"] and observed.get("draft") is False and
                    observed.get("tag_name") == old.get("tag_name") and observed.get("name") == previous and
                    observed.get("assets") == old.get("assets"), "Previous App Release changed before relabeling")
            if changed is not None:
                changed.append((old["id"], old["tag_name"], previous, historical))
            api.rename(old["id"], historical)


def _restore_unchanged_labels(api, changed):
    """Best-effort recovery; never intentionally overwrite an observed title edit."""
    for release_id, tag, previous, historical in changed:
        current = api.release(release_id)
        if current.get("id") == release_id and current.get("tag_name") == tag and current.get("draft") is False and current.get("name") == historical:
            api.rename(release_id, previous)


def _newest_published_id(releases):
    published = []
    for item in releases:
        if item.get("draft") is not False:
            continue
        value = item.get("published_at")
        require(isinstance(value, str), "Published App release time unavailable")
        try:
            instant = datetime.fromisoformat(value.replace("Z", "+00:00"))
        except ValueError as exc:
            raise PublicationError("Invalid App release time") from exc
        require(instant.tzinfo is not None, "App release time lacks a timezone")
        published.append((instant, item["id"]))
    require(published, "No published App release available")
    published.sort(reverse=True)
    require(len(published) == 1 or published[0][0] != published[1][0], "Newest App release is ambiguous")
    return published[0][1]


def prepare_draft(ctx, payload, setup, api, *, runtime_sha=NODE_SHA, license_sha=NODE_LICENSE_SHA):
    tag = _context(ctx)
    payload_digest = verify_package(payload, runtime_sha=runtime_sha, license_sha=license_sha)
    setup_digest = verify_setup(setup)
    require(api.source_head() == ctx["sourceSha"], "App source branch moved")
    releases = _release_list(api)
    current = next((item for item in releases if item["tag_name"] == tag), None)
    marker = _body(ctx, payload_digest, setup_digest)
    if current:
        require(current.get("target_commitish") == ctx["sourceSha"] and current.get("body") == marker and
                current.get("prerelease") is True and type(current.get("draft")) is bool,
                "Existing App Release does not match this build")
        require(not current["draft"] or current.get("name") == _pending_title(ctx), "Draft App title changed")
        ref = api.tag_commit(tag)
        require(ref is None or ref == ctx["sourceSha"], "Existing App tag points elsewhere")
        return current
    require(api.tag_commit(tag) is None, "An existing tag needs manual review")
    return api.create_release({
        "tag_name": tag, "target_commitish": ctx["sourceSha"], "draft": True, "prerelease": True,
        "make_latest": "false", "name": _pending_title(ctx),
        "body": marker,
    })


def _verify_draft(release, release_id, ctx, payload, setup, api, *, allow_empty=False):
    tag = _context(ctx)
    require(isinstance(release, dict) and release.get("id") == release_id and release.get("tag_name") == tag and
            release.get("target_commitish") == ctx["sourceSha"] and release.get("name") == _pending_title(ctx) and
            release.get("body") == _body(ctx, hashlib.sha256(payload).hexdigest(), hashlib.sha256(setup).hexdigest()) and
            release.get("draft") is True and release.get("prerelease") is True and
            isinstance(release.get("assets"), list) and
            ((allow_empty and len(release["assets"]) == 0) or
             (len(release["assets"]) == 1 and _asset_matches(release["assets"][0], setup))),
            "Draft App Release identity or installer changed")
    tag_commit = api.tag_commit(tag)
    require(tag_commit is None or tag_commit == ctx["sourceSha"], "Draft App tag points elsewhere")


def publish(ctx, payload, setup, api, *, runtime_sha=NODE_SHA, license_sha=NODE_LICENSE_SHA):
    release = prepare_draft(ctx, payload, setup, api, runtime_sha=runtime_sha, license_sha=license_sha)
    require(type(release.get("id")) is int and release["id"] > 0 and isinstance(release.get("assets"), list),
            "Invalid App Release response")
    release_id = release["id"]
    if release["draft"]:
        require(len(release["assets"]) <= 1, "Unexpected draft assets")
        if not release["assets"]:
            observed = api.release(release_id)
            _verify_draft(observed, release_id, ctx, payload, setup, api, allow_empty=True)
            require(not observed["assets"], "Draft gained an asset before upload")
            api.upload(release_id, setup)
        release = api.release(release_id)
        _verify_draft(release, release_id, ctx, payload, setup, api)
        require(api.source_head() == ctx["sourceSha"], "App source branch moved before publication")
        releases = _release_list(api)
        changed = []
        try:
            _mark_older_historical(api, releases, release_id, changed)
            require(api.source_head() == ctx["sourceSha"], "App source branch moved before publication")
            _verify_draft(api.release(release_id), release_id, ctx, payload, setup, api)
            api.publish_release(release_id, _latest_title(ctx))
            release = api.release(release_id)
        except Exception as exc:
            try:
                observed = api.release(release_id)
            except Exception as read_error:
                raise PublicationError("Publication outcome is unknown; inspect the existing Release before retrying") from read_error
            if observed.get("draft") is True:
                try:
                    _restore_unchanged_labels(api, changed)
                except Exception as restore_error:
                    raise PublicationError("App remains a draft; previous labels need manual review") from restore_error
                raise PublicationError("App remains a draft; previous labels were restored only where unchanged") from exc
            require(observed.get("draft") is False, "Publication outcome requires manual review")
            release = observed
        require(release.get("draft") is False and release.get("prerelease") is True and
                release.get("tag_name") == _context(ctx) and
                release.get("name") == _latest_title(ctx) and release.get("target_commitish") == ctx["sourceSha"] and
                release.get("body") == _body(ctx, hashlib.sha256(payload).hexdigest(), hashlib.sha256(setup).hexdigest()) and
                len(release.get("assets", [])) == 1 and _asset_matches(release["assets"][0], setup) and
                api.tag_commit(release["tag_name"]) == ctx["sourceSha"], "Published App installer could not be verified")
        outcome = "published"
    else:
        require(release.get("prerelease") is True and len(release["assets"]) == 1 and
                _asset_matches(release["assets"][0], setup) and api.tag_commit(release["tag_name"]) == ctx["sourceSha"],
                "Published App Release differs")
        outcome = "already-published"
        releases = _release_list(api)
        if _newest_published_id(releases) == release_id:
            require(release.get("name") == _latest_title(ctx), "Published App title changed; manual review required")
            _mark_older_historical(api, releases, release_id)
    return outcome


class GitHubGateway:
    def __init__(self):
        self.base = "repos/" + REPOSITORY

    def _call(self, method, endpoint, body=None, *, binary=None, missing=False, paginate=False):
        command = ["gh", "api", "--method", method, "-H", "Accept: application/vnd.github+json"]
        if paginate:
            command += ["--paginate", "--slurp"]
        payload = None
        if binary is not None:
            command += ["-H", "Content-Type: application/octet-stream", "--input", "-"]
            payload = binary
        elif body is not None:
            command += ["--input", "-"]
            payload = json.dumps(body).encode("utf-8")
        command.append(endpoint)
        try:
            result = subprocess.run(command, input=payload, capture_output=True, timeout=120, check=False)
        except (OSError, subprocess.TimeoutExpired) as exc:
            raise PublicationError("GitHub request failed") from exc
        if result.returncode:
            if missing and b"HTTP 404" in result.stderr:
                return None
            raise PublicationError("GitHub request failed")
        try:
            return json.loads(result.stdout.decode("utf-8"))
        except (UnicodeError, ValueError) as exc:
            raise PublicationError("Invalid GitHub response") from exc

    def source_head(self):
        ref = self._call("GET", self.base + "/git/ref/heads/" + APP_BRANCH)
        require(ref.get("ref") == "refs/heads/" + APP_BRANCH and ref.get("object", {}).get("type") == "commit", "Invalid App branch")
        return ref["object"].get("sha")

    def list_releases(self):
        pages = self._call("GET", self.base + "/releases?per_page=100", paginate=True)
        require(isinstance(pages, list) and all(isinstance(page, list) for page in pages), "Incomplete Release list")
        return [item for page in pages for item in page]

    def tag_commit(self, tag):
        ref = self._call("GET", self.base + "/git/ref/tags/" + quote(tag, safe=""), missing=True)
        if ref is None:
            return None
        require(ref.get("ref") == "refs/tags/" + tag, "Unexpected App tag")
        obj = ref.get("object", {})
        seen = set()
        for _ in range(5):
            value = obj.get("sha")
            require(valid_sha(value) and value not in seen, "Invalid App tag chain")
            seen.add(value)
            if obj.get("type") == "commit":
                return value
            require(obj.get("type") == "tag", "Invalid App tag type")
            obj = self._call("GET", self.base + "/git/tags/" + value).get("object", {})
        raise PublicationError("App tag chain exceeds limit")

    def create_release(self, body):
        return self._call("POST", self.base + "/releases", body)

    def upload(self, release_id, data):
        require(type(release_id) is int and release_id > 0, "Invalid App Release ID")
        verify_setup(data)
        token = os.environ.get("GH_TOKEN")
        require(isinstance(token, str) and bool(token) and not re.search(r"[\x00-\x20\x7f]", token),
                "GitHub upload credential is unavailable")
        endpoint = "https://uploads.github.com/" + self.base + "/releases/" + str(release_id) + "/assets?name=" + quote(ASSET_NAME, safe="")
        request = urllib.request.Request(endpoint, data=data, method="POST", headers={
            "Authorization": "Bearer " + token,
            "Content-Type": "application/octet-stream",
            "Content-Length": str(len(data)),
            "Accept": "application/vnd.github+json",
            "X-GitHub-Api-Version": "2022-11-28",
        })
        try:
            with urllib.request.build_opener(NoRedirect()).open(request, timeout=120) as response:
                require(response.status == 201, "GitHub upload returned an unexpected status")
        except urllib.error.HTTPError as exc:
            raise PublicationError("GitHub upload failed (HTTP " + str(exc.code) + ")") from exc
        except (urllib.error.URLError, OSError, TimeoutError) as exc:
            raise PublicationError("GitHub upload request failed") from exc

    def release(self, release_id):
        return self._call("GET", self.base + "/releases/" + str(release_id))

    def publish_release(self, release_id, title):
        self._call("PATCH", self.base + "/releases/" + str(release_id),
                   {"draft": False, "prerelease": True, "make_latest": "false", "name": title})

    def rename(self, release_id, title):
        self._call("PATCH", self.base + "/releases/" + str(release_id), {"name": title})


def main():
    require(len(sys.argv) == 4, "Expected installer, internal payload and build identity paths")
    require(os.environ.get("GITHUB_REPOSITORY") == REPOSITORY and os.environ.get("GITHUB_EVENT_NAME") == "workflow_dispatch" and
            os.environ.get("GITHUB_ACTOR", "").casefold() == "kingsleyhk0323" and
            os.environ.get("GITHUB_TRIGGERING_ACTOR", "").casefold() == "kingsleyhk0323", "Unexpected release authority")
    try:
        identity = json.loads(Path(sys.argv[3]).read_text(encoding="utf-8-sig"), object_pairs_hook=_unique_object)
        ctx = {"repository": REPOSITORY, "sourceSha": os.environ.get("APP_SOURCE_SHA"),
               "version": os.environ.get("APP_VERSION"), "runId": int(os.environ.get("GITHUB_RUN_ID", "")),
               "attempt": int(os.environ.get("BUILD_ATTEMPT", ""))}
    except (OSError, ValueError) as exc:
        raise PublicationError("Invalid App build identity") from exc
    setup_path, package = Path(sys.argv[1]), Path(sys.argv[2])
    require(package.is_file() and not package.is_symlink() and 0 < package.stat().st_size <= 160 * 1024 * 1024,
            "App package unavailable")
    require(setup_path.is_file() and not setup_path.is_symlink() and 2 < setup_path.stat().st_size <= 200 * 1024 * 1024,
            "App installer unavailable")
    payload, setup = package.read_bytes(), setup_path.read_bytes()
    verify_build_identity(ctx, identity, payload, setup)
    print("Dev Manager Release:", publish(ctx, payload, setup, GitHubGateway()))


if __name__ == "__main__":
    try:
        main()
    except PublicationError as error:
        print("Dev Manager publication stopped:", str(error), file=sys.stderr)
        sys.exit(1)
