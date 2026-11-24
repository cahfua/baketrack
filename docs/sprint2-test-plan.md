# BakeTrack Sprint 2 Test Plan

Prepared by: Celestine Ahfua
Task group: Group 4 — Dashboard Alerts and Sprint 2 QA

## Purpose

Verify dashboard alerts, stock replenishment, products, recipes,
data persistence, and Admin/Staff permissions.

Dashboard testing is complete for the checks recorded below.
Remaining feature tests have not been run.

## Test Environment

Application: Local BakeTrack
Branch: feature/dashboard-alerts
Application commit: Add tested commit URL
Database: Local PostgreSQL through Docker
Browser: Google Chrome on Windows
Dashboard test date: November 24, 2026

Use local test records only. Restore starting values between tests.
Do not delete shared data or change database schemas.

## Rules and Coordination

- Low Stock includes quantities at or below the minimum stock level.
- Expiring Soon currently includes today through the next 7 days,
  using UTC dates. Missing dates and already expired dates are excluded.
- Confirm the expiry window with Group 1.
- Dashboard information updates when the page is reopened or refreshed.
- Confirm replenishment and adjustment rules with Group 1.
- Confirm product and recipe validation with Groups 2 and 3.
- Confirm which operations Admin and Staff may perform before
  marking permission tests as passed or failed.

## Dashboard Test Data

| Ingredient | Starting Quantity | Unit | Minimum Stock | Expiry |
|---|---|---|---|---|
| Test Low Flour | 1 | kg | 2 | 2026-12-24 |
| Test Boundary Sugar | 2 | kg | 2 | 2026-12-24 |
| Test Soon Butter | 5 | kg | 1 | 2026-11-27 |
| Test Later Salt | 5 | kg | 1 | 2026-12-02 |

After testing, Flour quantity is 5 and Butter expiry is restored
to November 27, 2026.

For future tests, use dates relative to the application's UTC date.

## Completed Dashboard Tests

Results below were observed or confirmed during local testing.

| ID | Test | Expected and Actual Result | Status |
|---|---|---|---|
| DASH-01 | Open dashboard with no ingredients. | Zero counts and clear empty alert messages. | Pass |
| DASH-02 | Add the four starting test ingredients and reopen dashboard. | 4 ingredients, 2 low-stock alerts, and 1 expiring-soon alert. | Pass |
| DASH-03 | Check Sugar with quantity equal to its minimum. | Sugar appears in Low Stock. | Pass |
| DASH-04 | Change Flour quantity from 1 to 5 and reopen dashboard. | Flour disappears from Low Stock; Sugar remains; low-stock count becomes 1. | Pass |
| DASH-05 | Set Butter expiry to today and refresh. | Butter appears in Expiring Soon. | Pass |
| DASH-06 | Set Butter expiry to 7 days ahead and refresh. | Butter appears in Expiring Soon. | Pass |
| DASH-07 | Set Butter expiry to 8 days ahead and refresh. | Butter does not appear in Expiring Soon. | Pass |
| DASH-08 | Clear Butter expiry and refresh. | Butter does not appear in Expiring Soon. | Pass |
| DASH-09 | View dashboard as Admin and Staff. | Both roles see the expected dashboard counts and alerts. | Pass |
| DASH-10 | View dashboard at the tested narrow browser width. | Alerts and links remain readable; columns are tight. | Pass |
| DASH-11 | Click both ingredient alert links. | Both links open Ingredients. | Pass |
| DASH-12 | Restart application and reopen dashboard. | Saved records remain; final counts are 4 ingredients, 1 low-stock alert, and 1 expiring-soon alert. | Pass |

Build result: dotnet build src/BakeTrack succeeded with one warning
referring to the unchanged Login.razor file.

No defects were found in the dashboard checks performed.
Full mobile compatibility has not been verified.

## Remaining Tests

Run relevant workflows with Admin and Staff accounts.
Record separate results when permissions differ.

| ID | Test and Steps | Expected Result | Status |
|---|---|---|---|
| DASH-13 | Give a test ingredient yesterday's expiry date and refresh. | Already expired ingredient is excluded from Expiring Soon. | Not run |
| DASH-14 | Set a test ingredient's quantity and minimum to 0 and refresh. | Ingredient appears in Low Stock under the current rule. | Not run |
| STK-01 | Replenish a test ingredient from 5 kg by 3 kg. | Quantity becomes 8 kg and one matching inventory transaction is saved. | Not run |
| STK-02 | Attempt a negative delivery and invalid numeric input. | Invalid delivery is rejected without changing stock or creating a transaction. | Not run |
| STK-03 | Open a delivery form, enter values, then cancel. | Stock and transactions remain unchanged. | Not run |
| STK-04 | Save a delivery and inspect its transaction using an agreed read-only method. | Ingredient, quantity, reason, and RecordedByUserId are correct. | Not run |
| STK-05 | Restart after saving a delivery. | Stock quantity and transaction remain saved. | Not run |
| STK-06 | Compare Ingredients badges and dashboard alerts at stock and expiry boundaries. | Both follow the team's agreed rules. | Not run |
| PRD-01 | Create Test Recipe Bread with valid agreed field values. | Product saves and displays the entered values. | Not run |
| PRD-02 | Edit the test product and save. | Existing product updates without an unintended duplicate. | Not run |
| PRD-03 | Separately try a blank name, invalid batch yield, negative price, and nonnumeric input. | Entries violating agreed rules are rejected; rejected edits preserve saved values. | Not run |
| REC-01 | Add test Flour and Sugar to a recipe with valid per-batch quantities. | Correct ingredient references and quantities save. | Not run |
| REC-02 | Edit a recipe quantity and remove one recipe ingredient. | Recipe updates without deleting inventory ingredients. | Not run |
| REC-03 | Separately try zero/negative recipe quantities, missing ingredients, and duplicate ingredients. | Behavior follows agreed recipe rules; invalid data is not saved. | Not run |
| REC-04 | Attempt to delete a test ingredient used in a recipe. | Deletion is prevented and related records remain intact. | Not run |
| PRD-04 | Cancel product deletion, then confirm deletion as an authorized role. | Cancel preserves the product; confirmed deletion follows agreed relationship rules without deleting inventory ingredients. | Not run |
| PER-01 | Repeat product and recipe operations as Admin and Staff. | Allowed operations succeed; forbidden operations are rejected by the server. | Not run |
| PER-02 | Sign out and directly open protected application routes. | Authentication is required to access protected data. | Not run |
| PER-03 | As Staff, attempt an agreed Admin-only route or action. | Unauthorized access is denied without changing data. | Not run |
| E2E-01 | Add a test ingredient, replenish it, and create a product and recipe. | Related records stay consistent; creating a recipe does not consume stock. | Not run |
| E2E-02 | Restart after product/recipe edits and an authorized deletion. | Edits remain saved and deleted records do not return. | Not run |
| UI-01 | Inspect replenishment, product, and recipe forms at narrow width. | Labels, fields, validation messages, and buttons remain usable. | Not run |

## Recording Results

- Pass: Actual result matches the agreed expected result.
- Fail: Actual result differs from the agreed expected result.
- Blocked: Test was attempted but cannot proceed; record the reason.
- Not run: Test has not been attempted.

For each remaining test execution, record:

Test ID:
Date and application commit:
Account role:
Test data and steps:
Actual result:
Status:
Evidence or related defect:

## Defect Record

No dashboard defects have been identified in the tests completed.

For any defect found, record:

Defect ID:
Related test ID:
Short title:
Affected role:
Application commit:
Steps to reproduce:
Expected result:
Actual result:
Evidence:
Owner, once agreed:
Fix commit:
Retest date and result:

## Current Completion Status

- Dashboard implementation and recorded checks: Complete; PR awaiting review.
- Sprint 2 test plan: Prepared; shared rules require team confirmation.
- Replenishment, product, recipe, and broader permission testing: Not run.
- Remaining defect tracking and retesting: Pending test execution.

Only mark remaining tests as passed after executing them.
Feature code and database changes remain with their assigned owners.