#!/usr/bin/env python3
"""Require every behaviour change to arrive with a test that proves it.

The gate runs the pull request's own tests against the pull request's *base*
source. At least one newly added test must fail there, and every added test
must pass once the patch is applied. A test that passes without the patch does
not demonstrate anything; it is the failure on base that turns a test into
evidence.

"Source" is every file under src/ that is not part of a test project. Test
projects (src/*.Test/, src/NzbDrone.Test.Common/, src/NzbDrone.Test.Dummy/) stay
at head in both runs, so the only variable is the production change.

Exit codes: 0 pass, 1 gate failure, 2 harness error.

Run it locally the same way CI does:

    python3 .github/scripts/pr_test_gate.py --base origin/develop --head HEAD

Escape hatch: a pull request that genuinely changes no behaviour (a pure
refactor, a rename, a dependency bump, a performance change covered by existing
tests) is exempt via the `no-test-gate` label. The label is checked by the
workflow, not here, so the reason is recorded on the pull request rather than
buried in a log.
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

SRC_DIR = "src"
SOLUTION = "src/Readarr.sln"
FRAMEWORK = "net10.0"
TESTS_DIR = Path("_tests") / FRAMEWORK
RESULTS_DIR = Path("_gate")

# Projects whose code is test code. Everything else under src/ is source.
TEST_PROJECT_RE = re.compile(r"^src/(?:[^/]+\.Test|NzbDrone\.Test\.Common|NzbDrone\.Test\.Dummy)/")
# Front-end and localization files under src/ are not compiled into the backend.
NON_BACKEND_RE = re.compile(r"^src/NzbDrone\.Core/Localization/")

TEST_ATTRIBUTE_RE = re.compile(r"^\s*\[(?:Test|TestCase|TestCaseSource|Theory)\b")
METHOD_RE = re.compile(r"^\s*public\s+(?:async\s+)?(?:void|Task)\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(")
HUNK_RE = re.compile(r"^@@ -\d+(?:,\d+)? \+(\d+)(?:,(\d+))? @@")


class GateError(RuntimeError):
    """The gate could not run. Distinct from the gate deciding to fail."""


def git(*args: str) -> str:
    result = subprocess.run(["git", *args], capture_output=True, text=True, check=False)
    if result.returncode != 0:
        raise GateError(f"git {' '.join(args)} failed: {result.stderr.strip()}")
    return result.stdout


def changed_files(base: str, head: str) -> list[str]:
    out = git("diff", "--name-only", "--diff-filter=ACMRD", base, head, "--", SRC_DIR)
    return [line for line in out.splitlines() if line.strip()]


def added_lines(base: str, head: str, path: str) -> set[int]:
    """Line numbers (in head) that the diff adds to `path`."""
    diff = git("diff", "--unified=0", base, head, "--", path)
    lines: set[int] = set()
    for line in diff.splitlines():
        if match := HUNK_RE.match(line):
            start, count = int(match.group(1)), int(match.group(2) or "1")
            lines.update(range(start, start + count))
    return lines


def added_tests(base: str, head: str, test_files: list[str]) -> set[str]:
    """Names of test methods whose declaration line is new in this diff."""
    names: set[str] = set()
    for path in test_files:
        if not path.endswith(".cs") or not Path(path).exists():
            continue
        new_lines = added_lines(base, head, path)
        if not new_lines:
            continue
        attributed = False
        for number, text in enumerate(Path(path).read_text(encoding="utf-8-sig").splitlines(), start=1):
            if TEST_ATTRIBUTE_RE.match(text):
                attributed = True
                continue
            if match := METHOD_RE.match(text):
                if attributed and number in new_lines:
                    names.add(match.group(1))
                attributed = False
            elif text.strip() and not text.strip().startswith(("[", "//")):
                attributed = False
    return names


def build() -> tuple[bool, str]:
    result = subprocess.run(
        [
            "dotnet", "msbuild", "-restore", SOLUTION,
            "-p:Configuration=Debug", "-p:Platform=Posix", "-p:RuntimeIdentifiers=linux-x64",
            "-v:q", "-nologo", "-clp:ErrorsOnly",
        ],
        capture_output=True,
        text=True,
        check=False,
    )
    return result.returncode == 0, result.stdout + result.stderr


def run_tests(names: set[str], label: str) -> tuple[set[str], set[str]]:
    """Run the named tests; return (passed, failed) method names."""
    assemblies = sorted(str(p) for p in TESTS_DIR.glob("Readarr.*.Test.dll"))
    if not assemblies:
        raise GateError(f"no test assemblies found in {TESTS_DIR}")

    name_filter = "|".join(f"Name~{name}" for name in sorted(names))
    results = RESULTS_DIR / label
    subprocess.run(
        [
            "dotnet", "test", *assemblies,
            "--filter", f"({name_filter})&TestCategory!=ManualTest&TestCategory!=MetadataServer&TestCategory!=WINDOWS",
            "--results-directory", str(results),
            "--logger", "trx",
        ],
        capture_output=True,
        text=True,
        check=False,
    )

    passed: set[str] = set()
    failed: set[str] = set()
    ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    for trx in results.glob("*.trx"):
        for result in ET.parse(trx).getroot().iterfind(".//t:UnitTestResult", ns):
            # "should_x(1,2)" -> "should_x"; the class is not part of a C# test name.
            method = result.get("testName", "").split("(")[0].split(".")[-1]
            if method not in names:
                continue
            outcome = result.get("outcome")
            if outcome == "Passed":
                passed.add(method)
            elif outcome in ("Failed", "Error", "Timeout"):
                failed.add(method)
    return passed, failed


def fail(reason: str, *, detail: str = "") -> int:
    print(f"\n::error::PR test gate: {reason}")
    print(f"\n  FAILED — {reason}\n")
    if detail:
        print(detail)
    print(
        "\n  A change to src/ has to arrive with a test that fails without it.\n"
        "  If this pull request genuinely changes no behaviour — a pure refactor,\n"
        "  a rename, a dependency bump — add the `no-test-gate` label and say why\n"
        "  in the pull request body. The exemption is recorded, not silent.\n"
    )
    return 1


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", required=True, help="base ref or SHA")
    parser.add_argument("--head", default="HEAD", help="head ref or SHA")
    args = parser.parse_args()

    if not Path(SOLUTION).is_file():
        raise GateError(f"expected {SOLUTION} in {Path.cwd()}")

    base, head = args.base, args.head
    changed = changed_files(base, head)
    source = [p for p in changed if not TEST_PROJECT_RE.match(p) and not NON_BACKEND_RE.match(p)]
    tests = [p for p in changed if TEST_PROJECT_RE.match(p)]

    print(f"  base {git('rev-parse', '--short', base).strip()}  head {git('rev-parse', '--short', head).strip()}")

    if not source:
        print("\n  SKIPPED — no backend source changes under src/. Nothing to demonstrate.\n")
        return 0

    print(f"\n  source files changed ({len(source)}):")
    for path in source:
        print(f"    {path}")

    if not tests:
        return fail(
            "src/ changed but no test changed",
            detail="  Changed without a test:\n" + "".join(f"    {p}\n" for p in source),
        )

    added = added_tests(base, head, tests)
    if not added:
        return fail(
            "test files changed but no new test was added",
            detail=f"  Changed test files: {', '.join(tests)}\n"
            "  The gate looks for added [Test]/[TestCase] methods. Editing an\n"
            "  existing test in place cannot show that the defect was ever present.\n",
        )

    print(f"\n  tests added ({len(added)}):")
    for name in sorted(added):
        print(f"    {name}")

    # --- Half one: the new tests must fail against the base source. ----------
    print("\n  [1/2] building the PR's tests against base source ...")
    at_base = [p for p in source if _exists_at(base, p)]
    at_head = [p for p in source if _exists_at(head, p)]
    if at_base:
        git("checkout", base, "--", *at_base)
    for path in source:
        if path not in at_base and Path(path).exists():
            Path(path).unlink()
    try:
        built, output = build()
        if not built:
            # The tests reference API that only exists with the patch. That is
            # the strongest possible evidence the tests require it.
            print("  base source does not compile with the PR's tests (new API referenced).")
            print("  accepted as evidence the tests require the patch.")
            proving: list[str] = []
        else:
            print("  running the PR's tests against base source ...")
            _, failed = run_tests(added, "base")
            proving = sorted(failed)
            if not proving:
                return fail(
                    "no added test fails without the patch",
                    detail="  The tests added by this pull request pass against the base\n"
                    "  source, so they do not demonstrate that anything was broken.\n"
                    "  Write a test that fails first, then make it pass.\n",
                )
            print(f"  {len(proving)} added test(s) fail without the patch:")
            for name in proving:
                print(f"    {name}")
    finally:
        if at_head:
            git("checkout", head, "--", *at_head)
        for path in source:
            if path not in at_head and Path(path).exists():
                Path(path).unlink()

    # --- Half two: everything must pass with the patch applied. -------------
    print("\n  [2/2] building and running the same tests against the PR ...")
    built, output = build()
    if not built:
        raise GateError(f"head does not build:\n{output[-3000:]}")
    passed, failed = run_tests(added, "head")
    missing = added - passed - failed
    if failed or missing:
        return fail(
            "the added tests do not all pass with the patch applied",
            detail=f"  Failing: {', '.join(sorted(failed)) or '-'}\n"
            f"  Not run (check the test name or category): {', '.join(sorted(missing)) or '-'}\n",
        )

    print("\n  PASSED — the defect is demonstrated and the patch closes it.\n")
    return 0


def _exists_at(rev: str, path: str) -> bool:
    return subprocess.run(["git", "cat-file", "-e", f"{rev}:{path}"], capture_output=True, check=False).returncode == 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except GateError as exc:
        print(f"::error::PR test gate could not run: {exc}", file=sys.stderr)
        sys.exit(2)
