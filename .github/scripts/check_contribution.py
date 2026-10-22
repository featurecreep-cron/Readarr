#!/usr/bin/env python3
"""Enforce the Servarr contribution rules that a machine can check.

The rules come from https://wiki.servarr.com/sonarr/contributing and Sonarr's
CONTRIBUTING.md / AGENTS.md, which this fork holds itself to so that its work
can go back upstream. Each subcommand prints every violation it finds, then
exits 1 if there was any.

    check_contribution.py commits --base origin/develop --head HEAD
    check_contribution.py pr --event "$GITHUB_EVENT_PATH"
    check_contribution.py localization

commits
  - Rebase, don't merge: no merge commits.
  - Make meaningful commits or squash them: no fixup!/squash!/amend! commits.
  - Subjects are Servarr style. A user-facing change starts with `New:` or
    `Fixed:`; anything else (chores, refactors, CI, dependency bumps) has no
    prefix at all. Conventional-commit prefixes (`fix:`, `chore(ci):`) are
    rejected, because upstream's changelog only understands New:/Fixed:.
  - No AI attribution footers. Upstream treats them as evidence that nobody
    reviewed the submission.
  - *nix line endings: no CRLF introduced, no whitespace errors.

pr
  - Branch is a feature branch with a meaningful name, not develop/main/patch,
    and not an AI agent's default prefix.
  - Pull requests target develop.

localization
  - Every key the frontend passes to translate() and the backend passes to
    GetLocalizedString() exists in en.json, the source for all translations.
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from pathlib import Path

USER_FACING_PREFIXES = ("New", "Fixed")
SUBJECT_MAX = 120

# `fix: x`, `chore(ci): x`, `feat!: x`
CONVENTIONAL_RE = re.compile(r"^[a-z]+(?:\([^)]*\))?!?: ")
# `Word: x` with a capitalised word: only New: and Fixed: are allowed.
PREFIX_RE = re.compile(r"^([A-Z][A-Za-z]+): ")
AUTOSQUASH_RE = re.compile(r"^(?:fixup|squash|amend)! ")
AI_FOOTER_RES = [
    re.compile(r"^Co-Authored-By:.*\b(?:Claude|Copilot|Cursor|Codex|Gemini|Devin|anthropic\.com|openai\.com)\b", re.I),
    re.compile(r"Generated with \[?(?:Claude|Copilot|Cursor|Codex|Gemini)", re.I),
    re.compile(r"^\s*🤖"),
]

SIBLING_RE = re.compile(r"\b(?:Lidarr|Sonarr|Radarr)\w*")
# Real references to the sibling apps: packages, import lists and links that
# name them on purpose.
ALLOWED_SIBLING_RE = re.compile(r"TagLibSharp-Lidarr|ImportLists[./\\](?:Lidarr|Sonarr|Radarr)|github\.com/(?:Lidarr|Sonarr|Radarr)/|wiki\.servarr\.com")

AGENT_BRANCH_PREFIXES =("claude", "codex", "copilot", "cursor", "devin", "gemini", "worktree")
BAD_BRANCH_NAMES = {"develop", "main", "master", "patch", "fix", "feature", "update"}

EN_JSON = Path("src/NzbDrone.Core/Localization/Core/en.json")
FRONTEND_KEY_RE = re.compile(r"""\btranslate\(\s*['"]([A-Za-z0-9]+)['"]""")
BACKEND_KEY_RE = re.compile(r"""\bGetLocalizedString\(\s*"([A-Za-z0-9]+)"\s*[,)]""")
# Test fixtures use made-up keys on purpose.
TEST_PROJECT_RE = re.compile(r"^src/(?:[^/]+\.Test|NzbDrone\.Test\.Common)/")


def git(*args: str) -> str:
    result = subprocess.run(["git", *args], capture_output=True, text=True, check=False)
    if result.returncode != 0:
        sys.exit(f"::error::git {' '.join(args)} failed: {result.stderr.strip()}")
    return result.stdout


def report(problems: list[str], ok: str) -> int:
    if not problems:
        print(ok)
        return 0
    for problem in problems:
        print(f"::error::{problem}")
    print(f"\n{len(problems)} problem(s). See CONTRIBUTING.md.")
    return 1


def check_commits(base: str, head: str) -> int:
    problems: list[str] = []
    shas = git("rev-list", "--reverse", f"{base}..{head}").split()
    for sha in shas:
        short = sha[:9]
        parents = git("rev-list", "--parents", "-n", "1", sha).split()[1:]
        message = git("log", "-1", "--format=%B", sha)
        subject = message.splitlines()[0] if message.strip() else ""

        if len(parents) > 1:
            problems.append(f"{short} is a merge commit. Rebase on develop instead of merging.")
        if not subject:
            problems.append(f"{short} has an empty commit message.")
            continue
        if AUTOSQUASH_RE.match(subject):
            problems.append(f"{short} '{subject}' is a fixup/squash commit. Squash it before review.")
        if CONVENTIONAL_RE.match(subject):
            problems.append(
                f"{short} '{subject}' uses a conventional-commit prefix. Use 'New: ' or 'Fixed: ' for a "
                "user-facing change, and no prefix for anything else."
            )
        elif (match := PREFIX_RE.match(subject)) and match.group(1) not in USER_FACING_PREFIXES:
            problems.append(
                f"{short} '{subject}' starts with '{match.group(1)}:'. Only 'New:' and 'Fixed:' are prefixes; "
                "drop it for a change that is not user facing."
            )
        if len(subject) > SUBJECT_MAX:
            problems.append(f"{short} subject is {len(subject)} characters; keep it under {SUBJECT_MAX}.")
        if subject.endswith("."):
            problems.append(f"{short} '{subject}' ends with a period.")
        for line in message.splitlines():
            if any(pattern.search(line) for pattern in AI_FOOTER_RES):
                problems.append(f"{short} carries an AI attribution line: '{line.strip()}'. Remove it.")

    # Whitespace errors and CRLF in what the branch adds, all commits together.
    check = subprocess.run(["git", "diff", "--check", f"{base}...{head}"], capture_output=True, text=True, check=False)
    if check.returncode != 0:
        problems.append("whitespace errors in the diff:\n" + check.stdout.strip())
    diff = subprocess.run(["git", "diff", "--text", f"{base}...{head}"], capture_output=True, check=False).stdout
    current = None
    crlf: set[str] = set()
    for raw in diff.split(b"\n"):
        if raw.startswith(b"+++ b/"):
            current = raw[6:].decode("utf-8", "replace")
        elif raw.startswith(b"+") and not raw.startswith(b"+++") and raw.endswith(b"\r") and current:
            crlf.add(current)
    for path in sorted(crlf):
        problems.append(f"{path} adds lines with CRLF endings. Commit with *nix line endings.")

    # Ports from the sibling apps apply cleanly with the sibling's names still
    # in them: a config folder called "Lidarr", a LidarrStartupException.
    sibling = subprocess.run(
        ["git", "diff", "--unified=0", f"{base}...{head}", "--", "src", "frontend/src"],
        capture_output=True, text=True, check=False,
    ).stdout
    current = None
    for line in sibling.splitlines():
        if line.startswith("+++ b/"):
            current = line[6:]
        elif line.startswith("+") and not line.startswith("+++") and current:
            for match in SIBLING_RE.finditer(line):
                if not ALLOWED_SIBLING_RE.search(line):
                    problems.append(f"{current} adds '{match.group(0)}': {line[1:].strip()[:120]}. A port should use Readarr's names.")

    return report(problems, f"{len(shas)} commit(s) follow the contribution rules.")



def check_pr(event_path: str) -> int:
    event = json.loads(Path(event_path).read_text(encoding="utf-8"))
    pr = event["pull_request"]
    problems: list[str] = []

    branch = pr["head"]["ref"]
    if branch.lower() in BAD_BRANCH_NAMES:
        problems.append(f"Branch '{branch}' is not a feature branch. Use a meaningful name, like 'fix-bug-description'.")
    if branch.lower().startswith(AGENT_BRANCH_PREFIXES):
        problems.append(f"Branch '{branch}' starts with an AI agent's default prefix. Rename it for what it changes.")

    # The release workflow's develop -> main promotion is the only PR main takes.
    base = pr["base"]["ref"]
    if base in ("main", "master") and branch != "develop":
        problems.append(f"Pull requests go to develop, not {base}.")


    return report(problems, "The pull request follows the contribution rules.")


def check_localization() -> int:
    keys = set(json.loads(EN_JSON.read_text(encoding="utf-8-sig")))
    problems: list[str] = []
    sources = [(p, FRONTEND_KEY_RE) for p in Path("frontend/src").rglob("*") if p.suffix in (".js", ".ts", ".tsx")]
    sources += [(p, BACKEND_KEY_RE) for p in Path("src").rglob("*.cs") if not TEST_PROJECT_RE.match(p.as_posix())]
    for path, pattern in sources:
        text = path.read_text(encoding="utf-8-sig", errors="replace")
        for match in pattern.finditer(text):
            if match.group(1) not in keys:
                line = text.count("\n", 0, match.start()) + 1
                problems.append(f"{path}:{line} uses '{match.group(1)}', which is not in {EN_JSON}.")
    return report(problems, f"All localization keys exist in {EN_JSON}.")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="command", required=True)
    commits = sub.add_parser("commits")
    commits.add_argument("--base", required=True)
    commits.add_argument("--head", default="HEAD")
    pr = sub.add_parser("pr")
    pr.add_argument("--event", required=True, help="path to the pull_request event JSON")
    sub.add_parser("localization")
    args = parser.parse_args()

    if args.command == "commits":
        return check_commits(args.base, args.head)
    if args.command == "pr":
        return check_pr(args.event)
    return check_localization()


if __name__ == "__main__":
    sys.exit(main())
