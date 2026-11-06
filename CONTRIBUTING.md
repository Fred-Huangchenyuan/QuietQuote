# Contributing

Please describe the problem, expected behavior, and Windows build before proposing a change. For visual issues, include the selected shape/theme/language and a screenshot without private content.

1. Branch from `main`.
2. Keep changes focused and run `scripts/test.ps1`.
3. For rendering or interaction changes, also run `scripts/test.ps1 -Desktop` on Windows and inspect the results. Use `-Live` only when the change requires real public market data.
4. Update the bilingual documentation for user-facing behavior.
5. Open a pull request with a description and the checks actually performed.

Do not commit API keys, local preferences, live caches, compiled binaries or build directories. Contributions are accepted under the project's MIT license.
