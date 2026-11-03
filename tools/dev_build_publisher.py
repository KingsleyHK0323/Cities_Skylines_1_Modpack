"""Publish verified module build files to an independent Git branch.

Signature: OpenAI-Codex
"""

import base64
import hashlib
import json
import os
from pathlib import Path
import re
import stat
import subprocess
import sys
from datetime import datetime, timezone
from urllib.parse import quote


REPOSITORY = "KingsleyHK0323/Cities_Skylines_1_Modpack"
MODULES = {
    "AI_Improve": {
        "module_id": "ai-improve", "assembly": "AIImprove",
        "workflow": ".github/workflows/cloud-compile-test.yml",
        "info": "build-info.json",
        "files": {
            "AIImprove.dll": "AIImprove", "CitiesHarmony.API.dll": "CitiesHarmony.API",
            "README.md": None, "LICENSE": None, "CitiesHarmony-LICENSE.txt": None,
            "THIRD-PARTY-NOTICES.md": None, "Steam_Workshop_Link.txt": None,
            "build-info.json": None,
        },
    },
    "Automated_Public_Transport_Planning": {
        "module_id": "public-transport-planning", "assembly": "AutomatedPublicTransportPlanning",
        "workflow": ".github/workflows/cloud-compile-test.yml",
        "info": "build-info.txt",
        "files": {
            "AutomatedPublicTransportPlanning.dll": "AutomatedPublicTransportPlanning",
            "README.md": None, "LICENSE": None, "Steam_Workshop_Link.txt": None,
            "build-info.txt": None,
        },
    },
}


class PublicationError(Exception):
    """A build or remote state could not be verified."""


def require(condition, message):
    if not condition:
        raise PublicationError(message)


def valid_sha(value):
    return isinstance(value, str) and re.fullmatch(r"[a-f0-9]{40}", value) is not None


def positive(value):
    return type(value) is int and 0 < value <= 2**53 - 1


def validate_workflow_attempt(build_attempt, workflow_attempt):
    require(positive(build_attempt) and positive(workflow_attempt) and build_attempt == workflow_attempt,
            "Compile and publish attempts differ; rerun all jobs")


def git_blob_sha(data):
    return hashlib.sha1(b"blob " + str(len(data)).encode("ascii") + b"\0" + data).hexdigest()


def _object_without_duplicate_keys(pairs):
    result = {}
    for key, value in pairs:
        require(key not in result, "Duplicate metadata key")
        result[key] = value
    return result


def _build_info(name, data):
    try:
        text = data.decode("utf-8-sig")
        if name.endswith(".json"):
            result = json.loads(text, object_pairs_hook=_object_without_duplicate_keys)
            require(isinstance(result, dict), "Invalid build metadata")
            return result
        result = {}
        for line in text.splitlines():
            if "=" not in line:
                continue
            key, value = line.split("=", 1)
            require(key not in result, "Duplicate build metadata")
            result[key] = value
        return result
    except (UnicodeError, ValueError) as exc:
        raise PublicationError("Invalid build metadata") from exc


def _context(ctx):
    require(isinstance(ctx, dict) and ctx.get("repository") == REPOSITORY, "Unexpected repository")
    branch = ctx.get("branch")
    require(branch in MODULES, "Unconfigured module branch")
    require(valid_sha(ctx.get("source_sha")), "Invalid source commit")
    require(positive(ctx.get("run_id")) and positive(ctx.get("attempt")), "Invalid build identity")
    return MODULES[branch]


def prepare_publication(ctx, files):
    config = _context(ctx)
    require(isinstance(files, dict), "Missing artifact files")
    expected = set(config["files"]) | {"SHA256SUMS"}
    require(set(files) == expected, "Artifact file list differs from allowlist")
    require(all(isinstance(data, bytes) and 0 < len(data) <= 32 * 1024 * 1024 for data in files.values()),
            "Invalid artifact size or content")
    require(sum(len(data) for data in files.values()) <= 64 * 1024 * 1024, "Artifact exceeds total size")
    try:
        lines = files["SHA256SUMS"].decode("ascii").splitlines()
    except UnicodeError as exc:
        raise PublicationError("Invalid checksum file") from exc
    checks = {}
    for line in lines:
        match = re.fullmatch(r"([a-fA-F0-9]{64})  ([A-Za-z0-9][A-Za-z0-9._-]{0,119})", line)
        require(match is not None and match.group(2) not in checks, "Invalid or duplicate checksum entry")
        checks[match.group(2)] = match.group(1).lower()
    require(set(checks) == set(config["files"]), "Incomplete checksum list")
    for name, digest in checks.items():
        require(hashlib.sha256(files[name]).hexdigest() == digest, "Artifact checksum mismatch")
    info = _build_info(config["info"], files[config["info"]])
    require(info.get("sourceSHA") == ctx["source_sha"], "Build source mismatch")
    require(str(info.get("runId", info.get("runid"))) == str(ctx["run_id"]), "Build run mismatch")
    require(str(info.get("runAttempt", info.get("runattempt"))) == str(ctx["attempt"]), "Build attempt mismatch")
    devtools = info.get("DevTools")
    require(devtools is True if config["info"].endswith(".json") else devtools == "true",
            "Development build evidence missing")
    require(info.get("repository", REPOSITORY) == REPOSITORY, "Build repository mismatch")
    require(info.get("ref", "refs/heads/" + ctx["branch"]) == "refs/heads/" + ctx["branch"], "Build branch mismatch")
    prefix = f"builds/{ctx['run_id']}-{ctx['attempt']}/"
    outputs = {prefix + name: files[name] for name in config["files"]}
    manifest = {
        "schema": 1, "sourceKind": "artifact-branch", "moduleId": config["module_id"],
        "repository": REPOSITORY, "sourceBranch": ctx["branch"],
        "artifactBranch": "Development_Build/" + ctx["branch"], "sourceSha": ctx["source_sha"],
        "assembly": config["assembly"], "workflowPath": config["workflow"],
        "runId": ctx["run_id"], "attempt": ctx["attempt"],
        "buildId": f"{ctx['branch']}-dev-{ctx['run_id']}-{ctx['attempt']}",
        "devTools": True,
        "files": [{"path": prefix + name, "name": name, "size": len(files[name]),
                   "sha256": hashlib.sha256(files[name]).hexdigest(), "assemblyName": assembly}
                  for name, assembly in config["files"].items()],
        "signature": "OpenAI-Codex",
    }
    return manifest, outputs


def _parse_time(value):
    try:
        parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
        require(parsed.tzinfo is not None, "Missing run timezone")
        return parsed.astimezone(timezone.utc)
    except (AttributeError, ValueError) as exc:
        raise PublicationError("Invalid run time") from exc


def _check_previous(previous, manifest, gateway):
    require(isinstance(previous, dict) and valid_sha(previous.get("head")) and valid_sha(previous.get("tree")),
            "Invalid existing artifact branch")
    old = previous.get("manifest")
    require(isinstance(old, dict), "Existing artifact manifest is missing")
    for key in ("schema", "sourceKind", "moduleId", "repository", "sourceBranch", "artifactBranch", "assembly", "workflowPath"):
        require(old.get(key) == manifest[key], "Existing artifact branch identity differs")
    require(valid_sha(old.get("sourceSha")) and positive(old.get("runId")) and positive(old.get("attempt")),
            "Invalid previous build identity")
    require(old.get("buildId") == f"{old['sourceBranch']}-dev-{old['runId']}-{old['attempt']}" and
            old.get("devTools") is True, "Invalid previous build declaration")
    require(isinstance(previous.get("paths"), dict), "Existing artifact tree is unavailable")
    if old.get("buildId") == manifest["buildId"]:
        require(old == manifest, "Existing build identity has different contents")
        return "unchanged"
    if old["sourceSha"] != manifest["sourceSha"]:
        require(gateway.compare(old["sourceSha"], manifest["sourceSha"]) == "ahead", "Source is older or divergent")
    elif old["runId"] == manifest["runId"]:
        require(manifest["attempt"] > old["attempt"], "Build attempt is not newer")
    else:
        previous_run = gateway.run(old["runId"], old["attempt"])
        current_run = gateway.run(manifest["runId"], manifest["attempt"])
        require(_parse_time(current_run.get("started")) > _parse_time(previous_run.get("started")),
                "Build order cannot be verified")
    return "publish"


def publish(ctx, files, gateway):
    manifest, outputs = prepare_publication(ctx, files)
    require(gateway.source_head(ctx["branch"]) == ctx["source_sha"], "Source branch moved")
    state = gateway.artifact_state(manifest["artifactBranch"])
    if state and _check_previous(state, manifest, gateway) == "unchanged":
        for path, data in outputs.items():
            require(state["paths"].get(path) == git_blob_sha(data), "Existing build files differ")
        return "unchanged"
    prior_paths = state["paths"] if state else {}
    for path, data in outputs.items():
        if path in prior_paths:
            require(prior_paths[path] == git_blob_sha(data), "Existing build file would be overwritten")
    entries = []
    manifest_data = (json.dumps(manifest, ensure_ascii=True, indent=2) + "\n").encode("utf-8")
    for path, data in list(outputs.items()) + [("manifest.json", manifest_data)]:
        blob = gateway.write_blob(data)
        require(blob == git_blob_sha(data), "Uploaded blob checksum mismatch")
        entries.append({"path": path, "mode": "100644", "type": "blob", "sha": blob})
    tree = gateway.write_tree(state["tree"] if state else None, entries)
    require(valid_sha(tree), "Invalid created tree")
    commit = gateway.write_commit(tree, state["head"] if state else None)
    require(valid_sha(commit), "Invalid created commit")
    require(gateway.source_head(ctx["branch"]) == ctx["source_sha"], "Source branch moved before publication")
    if state:
        gateway.update_ref(manifest["artifactBranch"], commit, False)
    else:
        gateway.create_ref(manifest["artifactBranch"], commit)
    return "published"


class GitHubGateway:
    def __init__(self, ctx):
        self.ctx = ctx
        self.base = "repos/" + REPOSITORY

    def _call(self, method, suffix, body=None, allow_missing=False):
        cmd = ["gh", "api", "--method", method, "-H", "Accept: application/vnd.github+json", self.base + suffix]
        encoded = None
        if body is not None:
            cmd += ["--input", "-"]
            encoded = json.dumps(body)
        try:
            result = subprocess.run(cmd, input=encoded, capture_output=True, text=True, timeout=60, check=False)
        except (OSError, subprocess.TimeoutExpired) as exc:
            raise PublicationError("GitHub request failed") from exc
        if result.returncode:
            if allow_missing and "HTTP 404" in result.stderr:
                return None
            raise PublicationError("GitHub request failed")
        try:
            return json.loads(result.stdout)
        except ValueError as exc:
            raise PublicationError("GitHub returned invalid data") from exc

    def _ref(self, branch):
        response = self._call("GET", "/git/ref/heads/" + quote(branch, safe="/"), allow_missing=True)
        if response is None:
            return None
        require(response.get("ref") == "refs/heads/" + branch and response.get("object", {}).get("type") == "commit",
                "Unexpected Git reference")
        value = response["object"].get("sha")
        require(valid_sha(value), "Invalid reference commit")
        return value

    def source_head(self, branch):
        return self._ref(branch)

    def artifact_state(self, branch):
        head = self._ref(branch)
        if head is None:
            return None
        commit = self._call("GET", "/git/commits/" + head)
        tree = commit.get("tree", {}).get("sha")
        require(valid_sha(tree), "Artifact tree is unavailable")
        raw = self._call("GET", "/contents/manifest.json?ref=" + head)
        require(raw.get("encoding") == "base64" and type(raw.get("size")) is int and 0 < raw["size"] <= 1048576,
                "Artifact manifest is unavailable")
        try:
            data = base64.b64decode(raw["content"], validate=False)
            require(len(data) == raw["size"] and git_blob_sha(data) == raw.get("sha"), "Artifact manifest blob mismatch")
            manifest = json.loads(data.decode("utf-8-sig"), object_pairs_hook=_object_without_duplicate_keys)
        except (KeyError, UnicodeError, ValueError) as exc:
            raise PublicationError("Artifact manifest is invalid") from exc
        listing = self._call("GET", "/git/trees/" + tree + "?recursive=1")
        require(listing.get("truncated") is False and isinstance(listing.get("tree"), list), "Artifact tree is incomplete")
        paths = {}
        for item in listing["tree"]:
            if item.get("type") == "tree" and item.get("mode") == "040000":
                continue
            require(item.get("type") == "blob" and item.get("mode") == "100644" and
                    isinstance(item.get("path"), str) and valid_sha(item.get("sha")), "Unsafe artifact tree entry")
            require(item["path"] not in paths, "Duplicate artifact tree entry")
            paths[item["path"]] = item["sha"]
        return {"head": head, "tree": tree, "manifest": manifest, "paths": paths}

    def compare(self, old, new):
        response = self._call("GET", "/compare/" + old + "..." + new)
        if (response.get("status") == "ahead" and response.get("behind_by") == 0 and
                type(response.get("ahead_by")) is int and response["ahead_by"] > 0 and
                response.get("base_commit", {}).get("sha") == old):
            return "ahead"
        return "unknown"

    def run(self, run_id, attempt):
        response = self._call("GET", f"/actions/runs/{run_id}/attempts/{attempt}")
        config = MODULES[self.ctx["branch"]]
        require(response.get("id") == run_id and response.get("run_attempt") == attempt and
                response.get("head_branch") == self.ctx["branch"] and
                response.get("head_sha") == self.ctx["source_sha"] and
                response.get("repository", {}).get("full_name") == REPOSITORY and
                response.get("head_repository", {}).get("full_name") == REPOSITORY and
                response.get("event") in ("push", "workflow_dispatch") and
                response.get("path") in (config["workflow"], config["workflow"] + "@" + self.ctx["branch"],
                                         config["workflow"] + "@refs/heads/" + self.ctx["branch"]),
                "Build run identity differs")
        return {"started": response.get("run_started_at") or response.get("created_at")}

    def write_blob(self, data):
        return self._call("POST", "/git/blobs", {"content": base64.b64encode(data).decode("ascii"), "encoding": "base64"}).get("sha")

    def write_tree(self, base_tree, entries):
        body = {"tree": entries}
        if base_tree:
            body["base_tree"] = base_tree
        return self._call("POST", "/git/trees", body).get("sha")

    def write_commit(self, tree, parent):
        body = {"message": "Publish verified Development build", "tree": tree, "parents": [parent] if parent else []}
        return self._call("POST", "/git/commits", body).get("sha")

    def create_ref(self, branch, sha):
        self._call("POST", "/git/refs", {"ref": "refs/heads/" + branch, "sha": sha})

    def update_ref(self, branch, sha, force):
        require(force is False, "Force updates are forbidden")
        self._call("PATCH", "/git/refs/heads/" + quote(branch, safe="/"), {"sha": sha, "force": False})


def main():
    require(len(sys.argv) == 2, "Expected one artifact directory")
    branch = os.environ.get("GITHUB_REF", "").removeprefix("refs/heads/")
    require(os.environ.get("GITHUB_REF") == "refs/heads/" + branch and
            os.environ.get("GITHUB_EVENT_NAME") in ("push", "workflow_dispatch"), "Unexpected workflow trigger")
    try:
        ctx = {"repository": os.environ.get("GITHUB_REPOSITORY"), "branch": branch,
               "source_sha": os.environ.get("GITHUB_SHA"), "run_id": int(os.environ.get("GITHUB_RUN_ID", "")),
               "attempt": int(os.environ.get("RELEASE_BUILD_ATTEMPT", ""))}
        workflow_attempt = int(os.environ.get("GITHUB_RUN_ATTEMPT", ""))
    except ValueError as exc:
        raise PublicationError("Invalid workflow identity") from exc
    validate_workflow_attempt(ctx["attempt"], workflow_attempt)
    config = _context(ctx)
    directory = Path(sys.argv[1])
    require(directory.is_dir() and not directory.is_symlink(), "Artifact directory is invalid")
    files = {}
    for entry in directory.iterdir():
        item = entry.lstat()
        require(stat.S_ISREG(item.st_mode) and not entry.is_symlink() and entry.name in set(config["files"]) | {"SHA256SUMS"},
                "Unexpected artifact entry")
        require(0 < item.st_size <= 32 * 1024 * 1024, "Invalid artifact size")
        files[entry.name] = entry.read_bytes()
    print("Development artifact branch:", publish(ctx, files, GitHubGateway(ctx)))


if __name__ == "__main__":
    try:
        main()
    except PublicationError as error:
        print("Development artifact publication stopped:", str(error), file=sys.stderr)
        sys.exit(1)
