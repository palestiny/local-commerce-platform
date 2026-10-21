# Baladna Runtime Upgrade and Release Readiness

**Last checked:** 2026-10-21  
**Purpose:** Keep runtime-upgrade evidence separate from deployment approval and authentication design approval.

## Current GitHub state

| Change | Current state | Evidence |
|---|---|---|
| Customer mobile + commerce integration, PR #22 | Open, Draft, not merged | Current head `abdf6d3ae6f2f3da6f59c58c9def7a90e04d7e87`; .NET run [39643375901](https://github.com/palestiny/local-commerce-platform/actions/runs/39643375901) and mobile run [39643375872](https://github.com/palestiny/local-commerce-platform/actions/runs/39643375872) completed successfully on this head. |
| .NET 10 foundation upgrade, PR #23 | Open, Draft, not merged | Head `edada1dc1636901a232001bc790e9a31f5df3e97`; workflow run [39617305844](https://github.com/palestiny/local-commerce-platform/actions/runs/39617305844) completed successfully. |
| Node 24-compatible GitHub Actions maintenance, PR #24 | Open, Draft, not merged | Head `ebefd29f831c85825a017ae128af0183ad760f79`; workflow run [39616239263](https://github.com/palestiny/local-commerce-platform/actions/runs/39616239263) completed successfully, including checkout@v7, setup-dotnet@v6, tests and PostgreSQL recovery smoke test. PR #24 targets the .NET 10 upgrade branch. |

These are separate branches/PRs. A successful run on the .NET 10 upgrade candidate does **not** mean the integration branch has been upgraded, and a successful combined-context run does **not** mean PR #24 is merged.

## What has been verified

- The .NET 10 candidate changes all six solution projects to `net10.0`, uses .NET SDK 10 in CI, and aligns EF Core/Npgsql EF provider packages to the 10.x family.
- The latest reported .NET 10 candidate CI run completed successfully. The related maintenance run reports 141 passing tests, zero build warnings/errors, and a PostgreSQL 16 backup/drop/recreate/restore verification.
- PR #22 current head `abdf6d3ae6f2f3da6f59c58c9def7a90e04d7e87` has successful .NET and customer-mobile CI runs (runs 39643375901 and 39643375872). The .NET job used `actions/checkout@v4` and .NET SDK 8, and the mobile job completed dependency installation, TypeScript checking, and Expo Doctor.
- The reviewed integration branch has no ASP.NET Core API host or live authentication implementation. The customer mobile auth screen remains a prototype.
- No PR in this sequence has been merged by this work; no production deployment has been performed.


## Additional branch/workflow evidence — 2026-10-21

- The workflow on `integration/baladna-foundation-mobile` still uses `actions/checkout@v4`, `actions/setup-dotnet@v4`, and `.NET 8.0.x`. Its latest verified run on integration head `abdf6d3ae6f2f3da6f59c58c9def7a90e04d7e87` passed, but this is evidence for the existing .NET 8 integration baseline, not the .NET 10 candidate.
- The workflow on `maintenance/node24-actions` uses `actions/checkout@v7`, `actions/setup-dotnet@v6`, and `.NET 10.0.x`; it also adds `upgrade/**` to the pull-request target filters. Those changes remain isolated to PR #24 and its base branch until the normal reviewed merge sequence.
- Earlier branch-divergence output was inconsistent with the later compare results and has been superseded by the latest direct checks below. Do not use the earlier `12 ahead / 2 behind` statement as current mergeability evidence.
- The repository/workflow evidence reviewed so far does not identify a production hosting provider or deployment target. This is a bounded finding from the inspected workflow and documented changed-file set, not proof that no external hosting configuration exists. The real environment owner must supply the hosting/runtime, backup and rollback facts.

## Latest verification and branch divergence — 2026-10-21 20:26 +03:00

- Rechecked PR #22 head `abdf6d3ae6f2f3da6f59c58c9def7a90e04d7e87`: both current .NET run [39643375901](https://github.com/palestiny/local-commerce-platform/actions/runs/39643375901) and mobile run [39643375872](https://github.com/palestiny/local-commerce-platform/actions/runs/39643375872) completed successfully. The .NET job's test and migration/recovery smoke-test steps both succeeded; the mobile dependency install, TypeScript check, and Expo Doctor steps all succeeded.
- Compare result for integration vs `upgrade/dotnet10-foundation`: `diverged`, upgrade is 12 commits ahead and 2 commits behind integration. Merge base remains `dcdf7319814792e39f7640bf376c63c8049f5251`. Do not merge or rebase blindly; review the two integration-only commits and check whether they affect the framework-upgrade changes.

## Release gates still open

1. **Hosting/runtime confirmation — OPEN.** The repository review did not identify a definitive production hosting target or deployment manifest. Confirm the actual hosting provider, deployment mechanism, supported .NET 10 runtime, environment variables/secrets configuration, health-check routing, and rollback mechanism from the real hosting configuration. Do not infer these from CI.
2. **Operational database recovery — OPEN.** CI's PostgreSQL 16 smoke test is useful evidence, but does not replace a backup/restore rehearsal using the actual deployment environment and a representative protected backup. Confirm backup ownership, retention, restore access, data-loss window, and verification procedure.
3. **Rollback and schema compatibility — OPEN.** Document who can execute rollback, the exact application artifact/configuration rollback steps, and how to recover if database changes or writes are not backward-compatible. Code rollback alone is not sufficient after an incompatible migration.
4. **PR review and merge order — OPEN.** Keep PRs Draft until their respective review gates are satisfied. Review the workflow maintenance and framework upgrade as separate changes; do not assume one PR's green checks merge the other.
5. **Authentication/API design gate — OPEN.** Runtime upgrade evidence does not authorize an API host, Identity/OpenIddict packages, endpoints, schema changes, mobile credential storage, or live sign-in. The auth design gate remains **NOT READY FOR IMPLEMENTATION** pending its remaining decisions and explicit owner approval.

## Next safe action

Obtain the factual deployment target and its runtime/backup/rollback details, then record them here with evidence. Until those facts are available, do not select a hosting platform by assumption and do not promote the upgrade to production. Once the deployment gate is evidenced, review PR #23 and PR #24 in the normal reviewed merge sequence; only then continue the separately gated authentication/API design.


## Recheck after documentation commit — 2026-10-21

- Re-fetched PR #23. GitHub currently reports it **open, Draft, not merged, and mergeable=true** at head `edada1dc1636901a232001bc790e9a31f5df3e97`, targeting `integration/baladna-foundation-mobile` at base SHA `dcdf7319814792e39f7640bf376c63c8049f5251`. This supersedes the earlier `mergeable:false` observation; GitHub mergeability can be recalculated and is not itself approval to merge.
- Re-ran direct compare from the PR base SHA to `integration/baladna-foundation-mobile`: integration is 3 commits ahead and 0 behind that base; the compare endpoint lists only this readiness document as changed on the integration side. Compare from the same base to `upgrade/dotnet10-foundation` reports 12 commits ahead and 0 behind, with eight changed files in the upgrade diff. The previous statement that the two branches currently diverged is therefore not supported by these latest direct compare results and is superseded.
- The PR #23 change set is limited to the workflow, six project files, and the infrastructure persistence test. The review found no direct overlap with the readiness document update. This is a filename/diff review, not a full semantic guarantee about every inherited commit.
- PR #24 remains open Draft and mergeable=true; its sole file change adds `upgrade/**` to the workflow pull-request base-branch filter. It is still separate and unmerged.
- Newly checked workflow runs for documentation commit `700eef583a728014aa585aaa6b1439ca2cf3033b` both completed successfully: [.NET run 39643882569](https://github.com/palestiny/local-commerce-platform/actions/runs/39643882569) and [customer-mobile run 39643882879](https://github.com/palestiny/local-commerce-platform/actions/runs/39643882879). These results apply to that commit, not to any later documentation update.
- Review of PR #23 diff: all six projects are retargeted from `net8.0` to `net10.0`; EF Core and Npgsql provider packages are aligned to 10.x in infrastructure and its tests; CI installs .NET 10 and upgrades checkout/setup-dotnet actions; the infrastructure test validates generated schema identifiers before interpolating them into schema DDL. No API/auth host or migration source is introduced by this PR.
- Current safe status: the .NET 10 candidate has a successful CI run and GitHub currently reports it mergeable, but the PR remains Draft. Keep merge/deployment and auth implementation gated on owner review and the open hosting/recovery/rollback questions above.
