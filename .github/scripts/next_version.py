#!/usr/bin/env python3
"""Work out the next release version from what has changed since the last tag.

Servarr commits say what changed for users with a `New:` or `Fixed:` prefix,
on the subject or on a line of the body, and leave everything else unprefixed.
Dependency updates are recognised from what the commit changes, not from how
its message is worded:

    BREAKING CHANGE: footer             major  (minor while the version is 0.x)
    New: ...                            minor
    Fixed: ...                          patch
    a runtime dependency changes        patch  (it reaches every user)
    anything else                       no release

A runtime dependency is:
  - a package in src/Directory.Packages.props that a non-test project
    references (test frameworks and analyzers are referenced only by test
    projects or Directory.Build.props, so bumping them releases nothing),
  - an entry under "dependencies" (not "devDependencies") in package.json,
  - the .NET SDK in global.json, which carries the runtime that ships.

Prints the version (X.Y.Z, no "v") on stdout and writes the release notes to
--notes. Prints nothing and exits 0 when nothing since the last tag warrants a
release.

    next_version.py --notes notes.md [--force patch|minor|major]
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from pathlib import Path

TAG_RE = re.compile(r"^v(\d+)\.(\d+)\.(\d+)$")
NEW_RE = re.compile(r"^New:\s*(.+)$")
FIXED_RE = re.compile(r"^Fixed:\s*(.+)$")
BREAKING_RE = re.compile(r"^BREAKING[ -]CHANGE:\s*(.+)$", re.M)
PACKAGE_VERSION_RE = re.compile(r'<PackageVersion\s+Include="([^"]+)"\s+Version="([^"]+)"')
PACKAGE_REFERENCE_RE = re.compile(r'<PackageReference\s+Include="([^"]+)"')
TEST_PROJECT_RE = re.compile(r"^src/(?:[^/]+\.Test|NzbDrone\.Test\.Common|NzbDrone\.Test\.Dummy)/")
LEVELS = {"patch": 1, "minor": 2, "major": 3}

PACKAGES_PROPS = "src/Directory.Packages.props"
PACKAGE_JSON = "package.json"
GLOBAL_JSON = "global.json"


def git(*args: str, check: bool = True) -> str:
    result = subprocess.run(["git", *args], capture_output=True, text=True, check=False)
    if check and result.returncode != 0:
        raise SystemExit(f"git {' '.join(args)} failed: {result.stderr.strip()}")
    return result.stdout if result.returncode == 0 else ""


def last_tag() -> tuple[str | None, tuple[int, int, int]]:
    for tag in git("tag", "--merged", "HEAD", "--sort=-v:refname").split():
        if match := TAG_RE.match(tag):
            return tag, tuple(int(part) for part in match.groups())  # type: ignore[return-value]
    return None, (0, 0, 0)


def runtime_packages(rev: str) -> set[str]:
    """Packages a non-test project references at `rev`."""
    names: set[str] = set()
    for path in git("ls-tree", "-r", "--name-only", rev, "--", "src").splitlines():
        if path.endswith(".csproj") and not TEST_PROJECT_RE.match(path):
            names.update(PACKAGE_REFERENCE_RE.findall(git("show", f"{rev}:{path}")))
    return names


def package_versions(rev: str) -> dict[str, str]:
    return dict(PACKAGE_VERSION_RE.findall(git("show", f"{rev}:{PACKAGES_PROPS}", check=False)))


def json_at(rev: str, path: str) -> dict:
    text = git("show", f"{rev}:{path}", check=False)
    return json.loads(text) if text.strip() else {}


def dependency_changes(sha: str, files: set[str]) -> list[str]:
    """Runtime dependencies whose version this commit changes, as 'name a -> b'."""
    parent = f"{sha}^"
    if not git("rev-parse", "--verify", "--quiet", parent, check=False).strip():
        return []
    changes: list[str] = []

    if PACKAGES_PROPS in files:
        before, after = package_versions(parent), package_versions(sha)
        runtime = runtime_packages(sha) | runtime_packages(parent)
        for name in sorted(set(before) | set(after)):
            old, new = before.get(name), after.get(name)
            if old != new and name in runtime:
                changes.append(f"{name} {old or '(new)'} -> {new or '(removed)'}")

    if PACKAGE_JSON in files:
        before = json_at(parent, PACKAGE_JSON).get("dependencies", {})
        after = json_at(sha, PACKAGE_JSON).get("dependencies", {})
        for name in sorted(set(before) | set(after)):
            if before.get(name) != after.get(name):
                changes.append(f"{name} {before.get(name, '(new)')} -> {after.get(name, '(removed)')}")

    if GLOBAL_JSON in files:
        old = json_at(parent, GLOBAL_JSON).get("sdk", {}).get("version")
        new = json_at(sha, GLOBAL_JSON).get("sdk", {}).get("version")
        if old != new:
            changes.append(f".NET SDK {old or '(none)'} -> {new}")

    return changes


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--notes", required=True, help="where to write the release notes")
    parser.add_argument("--force", choices=sorted(LEVELS), help="bump at least this much, even with no releasable commit")
    args = parser.parse_args()

    tag, (major, minor, patch) = last_tag()
    revision_range = f"{tag}..HEAD" if tag else "HEAD"

    new: list[str] = []
    fixed: list[str] = []
    deps: list[str] = []
    breaking: list[str] = []
    for sha in git("rev-list", "--no-merges", "--reverse", revision_range).split():
        message = git("log", "-1", "--format=%B", sha)
        short = sha[:9]
        for line in (line.strip() for line in message.splitlines()):
            if match := NEW_RE.match(line):
                new.append(f"{match.group(1)} ({short})")
            elif match := FIXED_RE.match(line):
                fixed.append(f"{match.group(1)} ({short})")
        breaking += [f"{text} ({short})" for text in BREAKING_RE.findall(message)]
        files = set(git("diff-tree", "--no-commit-id", "--name-only", "-r", sha).split())
        deps += [f"{change} ({short})" for change in dependency_changes(sha, files)]

    level = 0
    if deps or fixed:
        level = LEVELS["patch"]
    if new:
        level = LEVELS["minor"]
    if breaking:
        level = LEVELS["major"]
    if args.force:
        level = max(level, LEVELS[args.force])

    if level == 0:
        print(f"Nothing releasable since {tag or 'the first commit'}.", file=sys.stderr)
        return 0

    # Like python-semantic-release's major_on_zero = false: 0.x stays 0.x.
    if level == LEVELS["major"] and major == 0:
        level = LEVELS["minor"]
    if level == LEVELS["major"]:
        major, minor, patch = major + 1, 0, 0
    elif level == LEVELS["minor"]:
        minor, patch = minor + 1, 0
    else:
        patch += 1

    sections = [("Breaking changes", breaking), ("New", new), ("Fixed", fixed), ("Dependencies", deps)]
    notes = "\n\n".join(f"## {title}\n\n" + "\n".join(f"- {item}" for item in items) for title, items in sections if items)
    Path(args.notes).write_text(notes + "\n", encoding="utf-8")
    print(f"{major}.{minor}.{patch}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
