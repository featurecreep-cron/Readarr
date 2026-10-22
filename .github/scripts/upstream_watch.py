#!/usr/bin/env python3
"""List new Sonarr, Lidarr and Radarr commits that touch code Readarr shares.

Opens one issue per run with a checklist of those commits. Triage ticks each
line once it has become a type/port issue or been judged not applicable. The
last commit seen in each repository is kept in a hidden comment in the issue,
so the next run starts where this one stopped.

    upstream_watch.py --repo featurecreep-cron/Readarr [--dry-run]

A commit counts as shared when one of its files maps to a path, or the folder
of a path, that exists in this checkout. Translation syncs, version bumps and
API doc updates are skipped, as is a cherry-pick of a commit already listed.
"""

import argparse
import json
import os
import re
import subprocess
import sys
import urllib.parse
import urllib.request
from datetime import datetime, timedelta, timezone
from pathlib import PurePosixPath

UPSTREAMS = ["Sonarr/Sonarr", "Lidarr/Lidarr", "Radarr/Radarr"]
BRANCH = "develop"
TITLE_PREFIX = "Upstream watch"
STATE_RE = re.compile(r"<!-- upstream-watch (\{.*?\}) -->")
PICK_RE = re.compile(r"cherry picked from commit ([0-9a-f]{7,40})")
SKIP_SUBJECTS = re.compile(
    r"^(Bump version|Multiple Translations updated|Translations? update|Automated API Docs update|Update translations)",
    re.IGNORECASE,
)
FIRST_RUN_DAYS = 7
MAX_COMMITS = 250

# Sibling project names in paths, mapped to ours.
PATH_MAP = [
    (re.compile(r"^src/(Sonarr|Lidarr|Radarr)\.Api\.V\d+/"), "src/Readarr.Api.V1/"),
    (re.compile(r"^src/(Sonarr|Lidarr|Radarr)\.Api\.V\d+\.Test/"), "src/Readarr.Api.V1.Test/"),
    (re.compile(r"^src/(Sonarr|Lidarr|Radarr)\."), "src/Readarr."),
]


def api(path: str, token: str):
    request = urllib.request.Request(
        f"https://api.github.com{path}",
        headers={"Accept": "application/vnd.github+json", "Authorization": f"Bearer {token}"},
    )
    with urllib.request.urlopen(request, timeout=60) as response:
        return json.load(response)


def local_paths() -> tuple[set[str], set[str]]:
    files = subprocess.run(["git", "ls-files"], capture_output=True, text=True, check=True).stdout.split("\n")
    files = {f for f in files if f}
    folders = {str(PurePosixPath(f).parent) for f in files}
    return files, folders


def map_path(path: str) -> str:
    for pattern, replacement in PATH_MAP:
        if pattern.search(path):
            return pattern.sub(replacement, path, count=1)
    return path


def is_shared(paths: list[str], files: set[str], folders: set[str]) -> bool:
    for path in paths:
        mapped = map_path(path)
        if mapped in files or str(PurePosixPath(mapped).parent) in folders:
            return True
    return False


def last_state(repo: str, token: str) -> dict:
    query = f'repo:{repo} is:issue in:title "{TITLE_PREFIX}"'
    found = api(f"/search/issues?q={urllib.parse.quote(query)}&sort=created&order=desc&per_page=5", token)
    for item in found.get("items", []):
        match = STATE_RE.search(item.get("body") or "")
        if match:
            return json.loads(match.group(1))
    return {}


def new_commits(upstream: str, since_sha: str | None, token: str) -> list[dict]:
    if since_sha:
        compared = api(f"/repos/{upstream}/compare/{since_sha}...{BRANCH}", token)
        return compared.get("commits", [])[-MAX_COMMITS:]

    since = (datetime.now(timezone.utc) - timedelta(days=FIRST_RUN_DAYS)).strftime("%Y-%m-%dT%H:%M:%SZ")
    commits = api(f"/repos/{upstream}/commits?sha={BRANCH}&since={since}&per_page=100", token)
    return list(reversed(commits))


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", required=True)
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()

    token = os.environ.get("GH_TOKEN") or os.environ.get("GITHUB_TOKEN")
    if not token:
        print("GH_TOKEN or GITHUB_TOKEN is required", file=sys.stderr)
        return 1

    files, folders = local_paths()
    state = last_state(args.repo, token)
    new_state = dict(state)
    listed: set[str] = set()
    sections = []

    for upstream in UPSTREAMS:
        commits = new_commits(upstream, state.get(upstream), token)
        if commits:
            new_state[upstream] = commits[-1]["sha"]

        lines = []
        for commit in commits:
            message = commit["commit"]["message"]
            subject = message.split("\n", 1)[0]
            if SKIP_SUBJECTS.search(subject) or len(commit.get("parents", [])) > 1:
                continue

            picked = PICK_RE.findall(message)
            if any(p[:7] in listed for p in picked):
                continue

            detail = api(f"/repos/{upstream}/commits/{commit['sha']}", token)
            if not is_shared([f["filename"] for f in detail.get("files", [])], files, folders):
                continue

            listed.add(commit["sha"][:7])
            url = f"https://github.com/{upstream}/commit/{commit['sha']}"
            lines.append(f"- [ ] [`{commit['sha'][:9]}`]({url}) {subject}")

        if lines:
            sections.append(f"### {upstream.split('/')[0]}\n\n" + "\n".join(lines))

    today = datetime.now(timezone.utc).strftime("%Y-%m-%d")
    title = f"{TITLE_PREFIX}: {today}"
    body = (
        "Commits to shared code since the last watch. Tick each once it has a `type/port` issue "
        "or is noted here as not applicable.\n\n"
        + ("\n\n".join(sections) if sections else "Nothing new touches shared code.")
        + f"\n\n<!-- upstream-watch {json.dumps(new_state, sort_keys=True)} -->\n"
    )

    if args.dry_run:
        print(title)
        print(body)
        return 0

    subprocess.run(
        ["gh", "issue", "create", "--repo", args.repo, "--title", title, "--label", "type/port", "--body", body],
        check=True,
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
