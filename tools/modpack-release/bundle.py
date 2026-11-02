"""Collect verified cloud outputs without executing module source.

Signature: OpenAI-Codex
"""
import hashlib
import io
import json
import os
from pathlib import Path
import re
import subprocess
import zipfile
from datetime import datetime, timezone
from urllib.parse import quote

REPO = "KingsleyHK0323/Cities_Skylines_1_Modpack"
REFERENCES = "7b8a5e7c10f2db84388e8dd8af2084f239957b13"
PREFIX = "modpack-dev-"
ARCHIVE = "Cities Skylines_1_Modpack.zip"
REMOTE_ARCHIVE = "Cities.Skylines_1_Modpack.zip"
MODULES = (
    ("AI_Improve", "cloud-compile-test.yml", "AIImprove.dll", (
        "AIImprove.dll", "CitiesHarmony.API.dll", "README.md", "LICENSE",
        "CitiesHarmony-LICENSE.txt", "THIRD-PARTY-NOTICES.md",
        "Steam_Workshop_Link.txt", "build-info.json", "SHA256SUMS")),
    ("Automated_Public_Transport_Planning", "cloud-compile-test.yml",
     "AutomatedPublicTransportPlanning.dll", (
         "AutomatedPublicTransportPlanning.dll", "README.md", "LICENSE",
         "build-info.txt", "Steam_Workshop_Link.txt", "SHA256SUMS")),
    ("Automated_Public_Transport_Vehicle_Management", "cloud-build.yml",
     "AutomatedPublicTransportVehicleManagement.dll", (
         "AutomatedPublicTransportVehicleManagement.dll", "build-provenance.json")),
)


def gh(*args, binary=False):
    result = subprocess.run(["gh", *args], check=True, capture_output=True)
    return result.stdout if binary else json.loads(result.stdout)


def api(path):
    return gh("api", f"repos/{REPO}/{path}")


def write_api(path, method, body):
    result = subprocess.run(["gh", "api", f"repos/{REPO}/{path}",
                             "--method", method, "--input", "-"],
                            input=json.dumps(body).encode(), check=True, capture_output=True)
    return json.loads(result.stdout)


def digest(data):
    return hashlib.sha256(data).hexdigest()


def draft_by_tag(tag):
    # The tag endpoint may return 404 for an unpublished draft. Retain its ID.
    pages = gh("api", "--paginate", "--slurp", f"repos/{REPO}/releases?per_page=100")
    matches = [r for page in pages for r in page if r["tag_name"] == tag]
    if len(matches) != 1 or not matches[0]["draft"]:
        raise ValueError("Expected exactly one newly created draft")
    return api(f"releases/{matches[0]['id']}")


def strict_zip(data, allowed):
    with zipfile.ZipFile(io.BytesIO(data)) as archive:
        entries = archive.infolist()
        if (len(entries) != len(allowed) or
                {x.filename for x in entries} != set(allowed)):
            raise ValueError("Unexpected, duplicate, missing or nested artifact files")
        for entry in entries:
            if (entry.file_size < 1 or entry.file_size > 32 * 1024 * 1024 or
                    entry.external_attr >> 16 & 0o170000 == 0o120000):
                raise ValueError("Empty, oversized or symlink artifact entry")
        return {x.filename: archive.read(x) for x in entries}


def verify_files(files, spec, sha, run):
    branch, _, dll, allowed = spec
    if set(files) != set(allowed) or any(not value for value in files.values()):
        raise ValueError("Incomplete module payload")
    if not files[dll].startswith(b"MZ"):
        raise ValueError("Module is not a PE assembly")
    attempt = str(run["run_attempt"])
    run_id = str(run["id"])
    if branch == "AI_Improve":
        info = json.loads(files["build-info.json"].decode("utf-8-sig"))
        expected = {"sourceSHA": sha, "sharedReferencesSHA": REFERENCES,
                    "runId": run_id, "runAttempt": attempt, "DevTools": True,
                    "EnableLateMergeProbe": False}
    elif branch == "Automated_Public_Transport_Planning":
        info = dict(line.split("=", 1) for line in
                    files["build-info.txt"].decode("utf-8-sig").splitlines()
                    if "=" in line)
        expected = {"sourceSHA": sha, "sharedrefsSHA": REFERENCES,
                    "runid": run_id, "runattempt": attempt, "DevTools": "true",
                    "repository": REPO, "ref": f"refs/heads/{branch}"}
    else:
        info = json.loads(files["build-provenance.json"].decode("utf-8-sig"))
        expected = {"commit": sha, "runId": run_id, "attempt": attempt,
                    "assembly": "AutomatedPublicTransportVehicleManagement",
                    "gameplayVerified": False}
        if str(info.get("sha256", "")).lower() != digest(files[dll]):
            raise ValueError("Vehicle DLL hash mismatch")
    for key, value in expected.items():
        actual = info.get(key)
        if isinstance(value, bool):
            matches = actual is value
        else:
            matches = str(actual) == value
        if not matches:
            raise ValueError(f"Invalid provenance: {branch}/{key}")
    if "SHA256SUMS" in files:
        checks = {}
        for line in files["SHA256SUMS"].decode("utf-8-sig").splitlines():
            match = re.fullmatch(r"([0-9A-Fa-f]{64})  ([^/\\]+)", line)
            if not match or match[2] in checks:
                raise ValueError("Malformed or duplicate checksum entry")
            checks[match[2]] = match[1].lower()
        if checks != {name: digest(data) for name, data in files.items()
                      if name != "SHA256SUMS"}:
            raise ValueError("Incomplete or incorrect module checksums")


def release_files(spec, sha, run):
    branch, _, _, allowed = spec
    tag = f"{branch}-dev-{run['id']}-{run['run_attempt']}"
    release = api(f"releases/tags/{tag}")
    if release["draft"] or release["target_commitish"] != sha:
        raise ValueError("Module release is a draft or has a different source")
    assets = release["assets"]
    if len(assets) != len(allowed) or {a["name"] for a in assets} != set(allowed):
        raise ValueError("Module release asset allowlist mismatch")
    files = {}
    for asset in assets:
        if not 0 < asset["size"] <= 32 * 1024 * 1024:
            raise ValueError("Invalid module asset size")
        data = gh("api", f"repos/{REPO}/releases/assets/{asset['id']}",
                  "-H", "Accept: application/octet-stream", binary=True)
        if len(data) != asset["size"]:
            raise ValueError("Incomplete asset download")
        files[asset["name"]] = data
    return files


def vehicle_files(run, allowed):
    artifacts = api(f"actions/runs/{run['id']}/artifacts?per_page=100")
    name = f"vehicle-management-{run['id']}-{run['run_attempt']}"
    matches = [a for a in artifacts["artifacts"] if a["name"] == name]
    if len(matches) != 1 or matches[0]["expired"]:
        raise ValueError("Required vehicle artifact missing or expired; no fallback")
    artifact = matches[0]
    if not 0 < artifact["size_in_bytes"] <= 64 * 1024 * 1024:
        raise ValueError("Invalid vehicle artifact size")
    binding = artifact.get("workflow_run", {})
    if binding.get("head_sha") != run["head_sha"]:
        raise ValueError("Artifact source does not match the selected run")
    data = gh("api", f"repos/{REPO}/actions/artifacts/{artifact['id']}/zip", binary=True)
    return strict_zip(data, allowed)


def source_identity(commits):
    # The collector and workflow are inputs, not just module commits.
    root = Path(__file__).resolve().parents[2]
    return digest(json.dumps({
        "modules": commits,
        "collector": digest(Path(__file__).read_bytes()),
        "workflow": digest((root / ".github/workflows/modpack-development-release.yml").read_bytes()),
    }, sort_keys=True).encode())


def main():
    if os.environ.get("GITHUB_REPOSITORY") != REPO:
        raise ValueError("Unexpected repository")
    if os.environ.get("GITHUB_REF") != "refs/heads/Modpack_Website":
        raise ValueError("Only the default website branch may publish bundles")
    commits = {spec[0]: api(f"commits/{spec[0]}")["sha"] for spec in MODULES}
    identity = source_identity(commits)
    tag = PREFIX + identity
    # A failed draft is deliberately not considered a completed publication.
    releases = gh("api", "--paginate", "--slurp", f"repos/{REPO}/releases?per_page=100")
    releases = [item for page in releases for item in page]
    if any(r["tag_name"] == tag and not r["draft"] for r in releases):
        print("Unchanged sources and packaging rules: skipping duplicate publication.")
        return
    if any(r["tag_name"] == tag for r in releases):
        raise ValueError("An incomplete draft exists; inspect it before retrying")
    payload = {}
    manifest = {"sourceIdentity": identity, "modules": [], "signature": "OpenAI-Codex"}
    for spec in MODULES:
        branch, workflow, dll, allowed = spec
        runs = api(f"actions/workflows/{workflow}/runs?branch={branch}&head_sha={commits[branch]}&event=push&per_page=100")
        candidates = [r for r in runs["workflow_runs"] if r["head_sha"] == commits[branch]
                      and r["head_branch"] == branch and r["event"] == "push"]
        if not candidates:
            raise ValueError(f"No push build exists for current source: {branch}")
        run = max(candidates, key=lambda r: r["run_number"])
        if run["status"] != "completed" or run["conclusion"] != "success":
            raise ValueError(f"Latest current-source build has not passed: {branch}")
        files = (vehicle_files(run, allowed) if branch.endswith("Vehicle_Management")
                 else release_files(spec, commits[branch], run))
        verify_files(files, spec, commits[branch], run)
        if branch.endswith("Vehicle_Management"):
            license_data = gh("api", f"repos/{REPO}/contents/LICENSE?ref={commits[branch]}",
                              "-H", "Accept: application/vnd.github.raw+json", binary=True)
            if not license_data.strip():
                raise ValueError("Vehicle module license is missing")
            payload[f"Documentation/{branch}/LICENSE"] = license_data
        # Runtime dependencies stay alongside their owning module. Standalone
        # DLLs remain at the root; their documentation uses a separate namespace.
        for name, data in files.items():
            destination = (f"{branch}/{name}" if branch == "AI_Improve" else
                           name if name == dll else f"Documentation/{branch}/{name}")
            if destination in payload:
                raise ValueError("Duplicate package destination")
            payload[destination] = data
        manifest["modules"].append({"branch": branch, "commit": commits[branch],
                                    "runId": run["id"], "attempt": run["run_attempt"]})
    # Freeze the collected snapshot; never publish after a branch moved mid-run.
    if any(api(f"commits/{b}")["sha"] != sha for b, sha in commits.items()):
        raise ValueError("A module branch moved during collection; retry later")
    payload["manifest.json"] = json.dumps(manifest, indent=2).encode()
    payload["SHA256SUMS"] = "".join(f"{digest(data)}  {name}\n" for name, data in sorted(payload.items())).encode()
    output = Path(os.environ["RUNNER_TEMP"]) / "modpack-public"
    output.mkdir(exist_ok=False)
    archive = output / ARCHIVE
    with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED) as bundle:
        for name, data in sorted(payload.items()):
            bundle.writestr(name, data)
    with zipfile.ZipFile(archive) as bundle:
        if bundle.testzip() is not None or set(bundle.namelist()) != set(payload):
            raise ValueError("Final ZIP verification failed")
    notes = output / "release-notes.md"
    instructions = "\n\n".join(
        f"## Included installation guide: {name}\n\n{data.decode('utf-8-sig')}"
        for name, data in payload.items() if name.endswith("/README.md"))
    notes.write_text(release_notes(manifest) + "\n\n" + instructions, encoding="utf-8")
    date = datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M UTC")
    title = f"[LATEST DEV] Cities Skylines 1 Modpack — Development Bundle — {date} — {identity[:12]}"
    release = write_api("releases", "POST", {
        "tag_name": tag, "target_commitish": os.environ["GITHUB_SHA"],
        "draft": True, "prerelease": True, "make_latest": "false",
        "name": title, "body": notes.read_text(encoding="utf-8")})
    release_id = release["id"]
    upload_url = release["upload_url"].split("{")[0]
    if upload_url != f"https://uploads.github.com/repos/{REPO}/releases/{release_id}/assets":
        raise ValueError("Unexpected upload destination")
    gh("api", upload_url + "?name=" + quote(ARCHIVE) + "&label=" + quote(ARCHIVE),
       "--method", "POST", "-H", "Content-Type: application/zip",
       "--input", str(archive), binary=True)
    release = api(f"releases/{release_id}")
    assets = release["assets"]
    if len(assets) != 1 or assets[0]["name"] not in (ARCHIVE, REMOTE_ARCHIVE):
        raise ValueError("Draft asset allowlist mismatch")
    downloaded = gh("api", f"repos/{REPO}/releases/assets/{assets[0]['id']}",
                    "-H", "Accept: application/octet-stream", binary=True)
    if digest(downloaded) != digest(archive.read_bytes()):
        raise ValueError("Uploaded ZIP hash mismatch; draft retained")
    write_api(f"releases/{release_id}", "PATCH",
              {"draft": False, "prerelease": True, "make_latest": "false"})
    if api(f"releases/{release['id']}")["draft"]:
        raise ValueError("Publication was not confirmed")
    # Only this channel's old titles change, after the new asset is verified.
    for old in releases:
        if old["tag_name"].startswith(PREFIX) and not old["draft"] and old["name"].startswith("[LATEST DEV]"):
            gh("release", "edit", old["tag_name"], "--repo", REPO, "--title",
               old["name"].replace("[LATEST DEV]", "[HISTORICAL]", 1), binary=True)


def release_notes(manifest):
    rows = "\n".join(f"| {m['branch']} | `{m['commit']}` | {m['runId']} / {m['attempt']} |"
                     for m in manifest["modules"])
    return f"""## Development bundle

AI Improve provides shared simulation infrastructure. Public Transport Planning
provides transport planning tools. Public Transport Vehicle Management currently
provides observation tools. This bundle collects verified independent cloud builds.

> **Development build warning:** These experimental modules can be unstable,
> fail to work, conflict with other mods, or damage a save. Back up your saves.
> Cloud build verification does not establish gameplay compatibility or stability.

## Manual installation

1. Close Cities: Skylines and back up your saves and existing mod files.
2. Download and extract the bundle to a temporary directory. GitHub normalizes
   its filename to `Cities.Skylines_1_Modpack.zip`; its display label is
   `Cities Skylines_1_Modpack.zip`.
3. Open `%LOCALAPPDATA%\\Colossal Order\\Cities_Skylines\\Addons\\Mods`.
4. Copy the entire `AI_Improve` folder there, keeping `AIImprove.dll` and
   `CitiesHarmony.API.dll` together. For each standalone DLL at the ZIP root,
   create a separate mod folder and copy that DLL into its folder.
5. Remove duplicate installations from your active mod setup before testing;
   do not enable a Workshop copy and a development copy of the same module together.
6. Start the game and enable each installed module in Content Manager > Mods.
7. AI Improve and Planning were compiled with `DevTools=true`; follow each
   module's included README for its exact development controls and prerequisites.
   Vehicle Management is an observation build and does not claim a Dev Tool switch.
8. Test with a separate save. To return to a stable setup, close the game, remove
   these development folders and restore your previous installation and backup.

Module licenses, installation instructions and dependency notices are included.
Game reference assemblies are excluded. Sources were unchanged at collection end.

## Build provenance

| Module branch | Source commit | Cloud run / attempt |
| --- | --- | --- |
{rows}

Packaging commit: `{os.environ.get('GITHUB_SHA', '')}`

Bundle identity: `{manifest['sourceIdentity']}`

Signature: OpenAI-Codex
"""


if __name__ == "__main__":
    main()
