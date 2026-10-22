# How to Contribute

We're always looking for people to help make Readarr even better, there are a number of ways to contribute.

## Documentation

Setup guides, [FAQ](https://wiki.servarr.com/readarr/faq), the more information we have on the [wiki](https://wiki.servarr.com/readarr) the better.

## Development

Readarr follows the [Servarr development guidelines](https://wiki.servarr.com/sonarr/contributing). The rules below are those guidelines, and most of them are enforced by CI (see [What CI checks](#what-ci-checks)).

### Tools required

- Visual Studio 2022 or higher (https://www.visualstudio.com/vs/), or [Jetbrains Rider](https://www.jetbrains.com/rider/)
- The .NET SDK pinned in [global.json](global.json)
- HTML/Javascript editor of choice (VS Code/Webstorm/etc)
- [Git](https://git-scm.com/downloads)
- [NodeJS](https://nodejs.org/en/download/), the version pinned under `volta` in [package.json](package.json)
- [Yarn](https://yarnpkg.com/)

### Getting started

1. Fork Readarr
2. Clone the repository into your development machine. [_info_](https://docs.github.com/en/get-started/quickstart/fork-a-repo)
3. Install the required Node Packages `yarn install`
4. Start webpack to monitor your dev environment for any frontend changes using the `yarn start` command
5. Build the project in Visual Studio, setting the startup project to `Readarr.Console` and the framework to `x86`, or from the command line:
   `dotnet msbuild -restore src/Readarr.sln -p:Configuration=Debug -p:Platform=Posix -t:PublishAllRids`
6. Debug the project in Visual Studio
7. Open http://localhost:8787

### Issues

All issues must follow the provided templates, these templates help us triage and review contributions efficiently issues that do not use the templates may be closed without notice.

- We expect that all issues are opened by a human selecting the appropriate issue template while opening the issue
- Issues opened automatically or by other means may be closed automatically
- We also expect discussions on issues to be conducted by humans, we are not interested in conversing with AI or triaging AI hallucinations
- Bug reports for issues that generate logs must contain a link to the appropriate trace logs. See the [Wiki](https://wiki.servarr.com/readarr/troubleshooting#logging-and-log-files) for more information

### How to Contribute

- We prefer small PRs that focus on a single issue
- Discuss before building large features. For major features or architectural changes, please open an issue first. Once we've agreed on a solution the work can begin
- Small bug fixes or improvements can usually go straight to PR, but we may ask for additional information, including logs before reviewing
- Understand the code before changing it. Follow existing code structure and standards
- Add tests (unit/integration). A change to backend code needs a test that fails without it
- Commit with \*nix line endings for consistency
- Test thoroughly, all code must be tested to ensure it compiles and works correctly
- You are responsible for all code and you should understand it's functionality, including reviewing it before submission
- Be sure to run `yarn lint --fix` on your code for any front end changes before committing, and `yarn stylelint-linux` for css changes
- New strings for the UI or backend go in `src/NzbDrone.Core/Localization/Core/en.json`; other languages are translated through Weblate

### Commits

- Rebase from Readarr's `develop` branch, do not merge
- Make meaningful commits, or squash them
- Commits for changes that users would notice are written as `New:` or `Fixed:`, for example `Fixed: Importing books with empty tags`. Everything else (refactors, tests, CI, translations, dependency bumps) has no prefix
- Conventional-commit prefixes (`fix:`, `chore(ci):`) are not used

The version is worked out from those prefixes when a release is cut ([next_version.py](.github/scripts/next_version.py)):

| Commit | Release |
|---|---|
| `New: ...` | minor |
| `Fixed: ...`, or a commit that updates a runtime dependency (whatever its message says) | patch |
| no prefix | none, and not in the release notes |
| a `BREAKING CHANGE:` footer | major |

A `New:` or `Fixed:` line in the commit body counts too, for a commit that does several user-facing things.

### Pull Requesting

- Only make pull requests to `develop`, never `main`
- You're probably going to get some comments or questions from us, they will be to ensure consistency and maintainability
- Each PR should come from its own feature branch, not `develop` in your fork, and it should have a meaningful branch name (what is being added or fixed)
  - `new-feature` (Good)
  - `fix-bug` (Good)
  - `patch` (Bad)
  - `develop` (Bad)

### What CI checks

Run these locally before pushing; CI runs the same scripts.

| Check | Locally | Rule |
|---|---|---|
| Build | `dotnet msbuild -restore src/Readarr.sln -p:Configuration=Debug -p:Platform=Posix` | Warnings are errors, including StyleCop and the analyzers |
| Frontend | `yarn lint && yarn stylelint-linux` | ESLint and Stylelint clean |
| Tests | `./test.sh Linux Unit` | Unit tests pass on Linux, macOS and Windows, and on Postgres 16, 17 and 18 |
| Coverage | | At least 80% of the lines a PR changes are covered |
| Test gate | `python3 .github/scripts/pr_test_gate.py --base origin/develop` | At least one added test fails without the change. A PR that changes no behaviour can be labelled `no-test-gate`, and the reviewer checks that claim |
| Commits | `python3 .github/scripts/check_contribution.py commits --base origin/develop` | Commit message style, no merge or fixup commits, no AI attribution, \*nix line endings |
| Localization | `python3 .github/scripts/check_contribution.py localization` | Every translation key exists in `en.json` |
| Pull request | | Feature branch, targets `develop` |

## AI tools

AI tools can be useful, but unreviewed AI slop or automated AI agents is not.

The standard is the same regardless of how the code was written: **you are responsible for every line of your contribution.** If you can't explain why a line is there and why it's correct, it shouldn't be in your PR.

**Specifically:**

- If you use AI to help write code, you must understand every line of what you're submitting
- Do not paste raw AI output into issues, PRs, or comments. If you use AI to help draft text, rewrite it in your own words
- Remove AI-generated footers, co-author attributions, and "Generated with..." signatures before submitting. Their presence tells us you didn't review your own submission carefully enough to notice them
- Automated submissions — bots or agents opening issues or posting PRs without meaningful human review — will be treated as spam

If you have any questions about any of this, please let us know.
