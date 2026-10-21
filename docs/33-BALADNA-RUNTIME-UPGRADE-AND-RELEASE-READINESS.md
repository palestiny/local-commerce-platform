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

- Re-fetched PR #23 multiple times. GitHub's `mergeable` field changed between `true` and `false` across consecutive reads while head `edada1dc1636901a232001bc790e9a31f5df3e97` and base SHA `dcdf7319814792e39f7640bf376c63c8049f5251` stayed the same. The field is therefore currently **inconsistent / not a reliable diagnosis**; do not infer that conflicts are or are not present from this metadata alone. The PR remains open, Draft, and unmerged.
- Re-ran direct compare from the PR base SHA to `integration/baladna-foundation-mobile`: integration is 3 commits ahead and 0 behind that base; the compare endpoint lists only this readiness document as changed on the integration side. Compare from the same base to `upgrade/dotnet10-foundation` reports 12 commits ahead and 0 behind, with eight changed files in the upgrade diff. The previous statement that the two branches currently diverged is therefore not supported by these latest direct compare results and is superseded.
- The PR #23 change set is limited to the workflow, six project files, and the infrastructure persistence test. The review found no direct overlap with the readiness document update. This is a filename/diff review, not a full semantic guarantee about every inherited commit.
- PR #24 remains open Draft and mergeable=true; its sole file change adds `upgrade/**` to the workflow pull-request base-branch filter. It is still separate and unmerged.
- Newly checked workflow runs for documentation commit `700eef583a728014aa585aaa6b1439ca2cf3033b` both completed successfully: [.NET run 39643882569](https://github.com/palestiny/local-commerce-platform/actions/runs/39643882569) and [customer-mobile run 39643882879](https://github.com/palestiny/local-commerce-platform/actions/runs/39643882879). These results apply to that commit, not to any later documentation update.
- Review of PR #23 diff: all six projects are retargeted from `net8.0` to `net10.0`; EF Core and Npgsql provider packages are aligned to 10.x in infrastructure and its tests; CI installs .NET 10 and upgrades checkout/setup-dotnet actions; the infrastructure test validates generated schema identifiers before interpolating them into schema DDL. No API/auth host or migration source is introduced by this PR.
- Current safe status: the .NET 10 candidate has a successful CI run, but mergeability metadata is inconsistent across repeated reads and has not been resolved into a verified conflict diagnosis. Keep merge/deployment and auth implementation gated on owner review and the open hosting/recovery/rollback questions above.


- Documentation commit `86ea218557a0b28523a319230a1c0ea927b2a2ba` has been verified on the integration branch. At the time of the post-write check, no PR-triggered workflow runs were returned for this new commit; the successful runs cited above belong to the preceding commit `700eef5...` only.


## Post-merge revalidation — 2026-10-21

This section supersedes earlier status statements in this document that describe PRs #23 and #24 as open/unmerged or state that the .NET 10 upgrade has not reached the integration branch.

## Current verified GitHub state

| Change | Current state | Evidence |
|---|---|---|
| Customer mobile + commerce integration, PR #22 | Open, Draft, not merged | Head `73f9953132659d23bba077f22e88c70747095607`; the current .NET and customer-mobile workflows both completed successfully after the runtime upgrade merge. |
| .NET 10 foundation upgrade, PR #23 | Closed as superseded; not merged | Replaced by PR #26. |
| Node 24-compatible Actions maintenance, PR #24 | Closed as superseded; not merged | Its workflow updates were included in PR #26. |
| Module-boundary documentation, PR #25 | Merged | Merge commit `c11c33706a0589d60e46e8609525bbffa9ee6c91`. |
| .NET 10 foundation upgrade, PR #26 | Merged | Merge commit `73f9953132659d23bba077f22e88c70747095607`. |
| Post-merge .NET CI | Passed | [Run 39647068937](https://github.com/palestiny/local-commerce-platform/actions/runs/39647068937): restore, tests, and PostgreSQL migration/recovery smoke test all succeeded. |
| Post-merge customer-mobile CI | Passed | [Run 39647068934](https://github.com/palestiny/local-commerce-platform/actions/runs/39647068934): dependency installation, TypeScript check, and Expo Doctor succeeded. |

## What this evidence proves — and does not prove

- The current integration head includes the .NET 10 upgrade and Node 24-compatible workflow actions.
- The current head passed the configured .NET/PostgreSQL and customer-mobile CI workflows.
- CI does not prove a physical Android/iOS runtime session, production hosting readiness, or a real deployment-environment backup/restore rehearsal.
- The customer mobile app still uses illustrative demo data and has no live authenticated API connection.
- The authentication/first-API design gate remains **NOT READY FOR IMPLEMENTATION**. No auth/API implementation is authorized by the runtime-upgrade merge.
- PR #22 remains Draft. It targets `fix/replacement-delivery-store-consistency` and contains a broad integration change set (mobile app, HTML design references, and customer-order ownership persistence). Do not merge it merely because CI is green: review its target/base intent and the complete diff, and keep it unmerged until the intended branch integration and product/auth design gates are explicitly resolved.

## Next safe action

1. Perform a focused review of PR #22's customer-order ownership migration and initialization behavior against existing database states; preserve historical orders with unknown ownership as `CustomerId = NULL` and do not expose them as belonging to a customer.
2. Preserve the verified stacked-PR relationship: PR #22's base SHA is PR #14's head (`fde2ece976a02dc7e984173cf9b835a273e0cc84`), and PR #14 targets `feature/foundation-domain-architecture`; PR #1 then targets `main`. Do not retarget these PRs independently. Review and merge only in dependency order after each Draft gate is cleared.
3. Resolve the remaining authentication design decisions and get explicit owner approval before implementing auth packages, API routes, credential handling, or auth schema changes.
4. Keep production release blocked until the actual hosting/runtime, environment secrets/health checks, and operational backup/restore and rollback procedure are verified.


## Customer-order ownership migration test follow-up — 2026-10-21

- Added a regression assertion to the legacy-schema integration test: create a pre-existing order before the additive ownership migration, run the initializer, and verify the row still exists with `CustomerId = NULL`. This protects the rule that historical ownership must never be inferred.
- The first CI attempt caught a C# interpolated-string syntax error in this new test. The test source was corrected and re-run.
- The corrected source/test commit `88602500b971519847b9ef9140b1ebbd7854e0e0` passed .NET restore, the complete configured test suite, and the PostgreSQL migration/recovery smoke test in [run 39648554968](https://github.com/palestiny/local-commerce-platform/actions/runs/39648554968). The customer-mobile workflow also passed in [run 39648554927](https://github.com/palestiny/local-commerce-platform/actions/runs/39648554927).
- These green runs apply to commit `88602500b971519847b9ef9140b1ebbd7854e0e0`. Any later commit still needs its own CI result checked before claiming that exact head is green.


## Customer-mobile PR deduplication — 2026-10-21

- Closed PR #20 (`feature/customer-mobile-foundation`) and PR #21 (`fix/customer-mobile-sdk57-stability`) as superseded by PR #22. Neither was merged.
- This was verified by comparing the changed-file lists and Git blob SHAs, not by title alone: all files changed by PR #21 are present in PR #22 with matching blob SHAs; the PR #20-only foundation files (`.gitignore`, `README.md`, `app.json`, `expo-env.d.ts`, and theme tokens) also match exactly.
- No mobile source was deleted. PR #22 remains the single current integration candidate for the mobile tree.
- The exact PR #22 head `183eb619aee316b69800c7d33679634d94f45836` passed both configured workflows: [.NET/PostgreSQL run 39648916922](https://github.com/palestiny/local-commerce-platform/actions/runs/39648916922) and [customer-mobile run 39648916783](https://github.com/palestiny/local-commerce-platform/actions/runs/39648916783).
- PR #22 remains Draft and unmerged. Green CI verifies repository tests and static/mobile dependency checks; it does not close the replacement-delivery authorization blocker, authenticate the mobile app, or prove a physical-device runtime session.


## Integration-head verification and stack review — 2026-10-21

### Latest verified integration head before this documentation update

- PR #22 head `db963a7a82ecae22273e64368d02e49fd9a419f6` passed [.NET/PostgreSQL CI run 39649983524](https://github.com/palestiny/local-commerce-platform/actions/runs/39649983524) and [customer-mobile CI run 39649983477](https://github.com/palestiny/local-commerce-platform/actions/runs/39649983477). Duplicate push/PR-triggered runs also passed: [dotnet 39649975603](https://github.com/palestiny/local-commerce-platform/actions/runs/39649975603) and [mobile 39649975555](https://github.com/palestiny/local-commerce-platform/actions/runs/39649975555).
- These results verify that exact head, not this new documentation commit. The new commit must pass both workflows before it can be called green.
- PR #20 and #21 remain closed as superseded; their changed mobile files were compared against PR #22 and matched by Git blob SHA.

### Stacked PR review

- PR #1 remains Draft. Its current head `c11c33706a0589d60e46e8609525bbffa9ee6c91` has a successful .NET workflow ([run 39646589329](https://github.com/palestiny/local-commerce-platform/actions/runs/39646589329)); this is CI evidence, not approval to merge.
- PR #14 remains Draft and GitHub reports `mergeable: false`. The owner-approved replacement-delivery role policy exists, but a production trusted actor-role / Merchant-to-Store membership source is still missing. See the decision gate in `docs/32-CUSTOMER-AUTH-AND-FIRST-API-SLICE-DESIGN-GATE.md`.
- PR #22 remains Draft and unmerged. Do not merge the stack solely because CI passes. First resolve PR #14's base/merge conflict and trusted authorization source; preserve the chain #22 → #14 → #1.
- The PR #1 comparison reports substantial commit-history divergence from `main`, while the current `main` tree contains only the README and the core project-planning documents. This is a history/topology warning, not proof of a content conflict; verify the intended merge-base/history strategy before merging the foundation stack.
- No authentication provider, production authorization source, API endpoint, or deployment decision was selected by this review.
