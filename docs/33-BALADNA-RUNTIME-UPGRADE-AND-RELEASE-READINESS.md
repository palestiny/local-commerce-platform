# Baladna Runtime Upgrade and Release Readiness

**Last checked:** 2026-10-21  
**Purpose:** Keep runtime-upgrade evidence separate from deployment approval and authentication design approval.

## Current GitHub state

| Change | Current state | Evidence |
|---|---|---|
| Customer mobile + commerce integration, PR #22 | Open, Draft, not merged | Current head `0a18831c88f6a2c82eab3ffc2dc4053caa14eab5`; .NET run [39642785748](https://github.com/palestiny/local-commerce-platform/actions/runs/39642785748) and mobile run [39642785733](https://github.com/palestiny/local-commerce-platform/actions/runs/39642785733) completed successfully on this head. |
| .NET 10 foundation upgrade, PR #23 | Open, Draft, not merged | Head `edada1dc1636901a232001bc790e9a31f5df3e97`; workflow run [39617305844](https://github.com/palestiny/local-commerce-platform/actions/runs/39617305844) completed successfully. |
| Node 24-compatible GitHub Actions maintenance, PR #24 | Open, Draft, not merged | Head `ebefd29f831c85825a017ae128af0183ad760f79`; workflow run [39616239263](https://github.com/palestiny/local-commerce-platform/actions/runs/39616239263) completed successfully, including checkout@v7, setup-dotnet@v6, tests and PostgreSQL recovery smoke test. PR #24 targets the .NET 10 upgrade branch. |

These are separate branches/PRs. A successful run on the .NET 10 upgrade candidate does **not** mean the integration branch has been upgraded, and a successful combined-context run does **not** mean PR #24 is merged.

## What has been verified

- The .NET 10 candidate changes all six solution projects to `net10.0`, uses .NET SDK 10 in CI, and aligns EF Core/Npgsql EF provider packages to the 10.x family.
- The latest reported .NET 10 candidate CI run completed successfully. The related maintenance run reports 141 passing tests, zero build warnings/errors, and a PostgreSQL 16 backup/drop/recreate/restore verification.
- PR #22 current head `0a18831c88f6a2c82eab3ffc2dc4053caa14eab5` has successful .NET and customer-mobile CI runs (runs 39642785748 and 39642785733). The .NET job used `actions/checkout@v4` and .NET SDK 8, and the mobile job completed dependency installation, TypeScript checking, and Expo Doctor.
- The reviewed integration branch has no ASP.NET Core API host or live authentication implementation. The customer mobile auth screen remains a prototype.
- No PR in this sequence has been merged by this work; no production deployment has been performed.


## Additional branch/workflow evidence — 2026-10-21

- The workflow on `integration/baladna-foundation-mobile` still uses `actions/checkout@v4`, `actions/setup-dotnet@v4`, and `.NET 8.0.x`. Its latest verified run on the current integration head passed, but this is evidence for the existing .NET 8 integration baseline, not the .NET 10 candidate.
- The workflow on `maintenance/node24-actions` uses `actions/checkout@v7`, `actions/setup-dotnet@v6`, and `.NET 10.0.x`; it also adds `upgrade/**` to the pull-request target filters. Those changes remain isolated to PR #24 and its base branch until the normal reviewed merge sequence.
- PR #23 is based on integration commit `dcdf7319814792e39f7640bf376c63c8049f5251`, while integration has since advanced to `0a18831c88f6a2c82eab3ffc2dc4053caa14eab5`. Recheck the merge base/out-of-date status and review any resulting diff before promoting the upgrade; no automatic rebase was performed.
- The repository/workflow evidence reviewed so far does not identify a production hosting provider or deployment target. This is a bounded finding from the inspected workflow and documented changed-file set, not proof that no external hosting configuration exists. The real environment owner must supply the hosting/runtime, backup and rollback facts.

## Release gates still open

1. **Hosting/runtime confirmation — OPEN.** The repository review did not identify a definitive production hosting target or deployment manifest. Confirm the actual hosting provider, deployment mechanism, supported .NET 10 runtime, environment variables/secrets configuration, health-check routing, and rollback mechanism from the real hosting configuration. Do not infer these from CI.
2. **Operational database recovery — OPEN.** CI's PostgreSQL 16 smoke test is useful evidence, but does not replace a backup/restore rehearsal using the actual deployment environment and a representative protected backup. Confirm backup ownership, retention, restore access, data-loss window, and verification procedure.
3. **Rollback and schema compatibility — OPEN.** Document who can execute rollback, the exact application artifact/configuration rollback steps, and how to recover if database changes or writes are not backward-compatible. Code rollback alone is not sufficient after an incompatible migration.
4. **PR review and merge order — OPEN.** Keep PRs Draft until their respective review gates are satisfied. Review the workflow maintenance and framework upgrade as separate changes; do not assume one PR's green checks merge the other.
5. **Authentication/API design gate — OPEN.** Runtime upgrade evidence does not authorize an API host, Identity/OpenIddict packages, endpoints, schema changes, mobile credential storage, or live sign-in. The auth design gate remains **NOT READY FOR IMPLEMENTATION** pending its remaining decisions and explicit owner approval.

## Next safe action

Obtain the factual deployment target and its runtime/backup/rollback details, then record them here with evidence. Until those facts are available, do not select a hosting platform by assumption and do not promote the upgrade to production. Once the deployment gate is evidenced, review PR #23 and PR #24 in the normal reviewed merge sequence; only then continue the separately gated authentication/API design.
