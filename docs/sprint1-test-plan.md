# BakeTrack Sprint 1 Test Plan

Prepared by: Celestine Ahfua
Task group: Group 4 — Validation and Testing

## Purpose

Check that staff can add, view, edit, delete, and permanently save ingredient records.

These tests are prepared for execution when the relevant features are available.

## Proposed Validation Rules

Confirm these rules with the ingredient storage and interface owners before implementation:

- Ingredient name is required and cannot contain only spaces.
- Unit of measurement is required.
- Quantity must be a number greater than or equal to zero.
- Minimum stock level must be a number greater than or equal to zero.
- Expiry date requirements are awaiting team confirmation.
- Invalid entries must show a clear message and must not be saved.

## Test Environment

Application version or commit: Not yet recorded
Browser: Not yet recorded
Database: Use test data only
Execution date: Not yet executed

## Test Scenarios

| ID | Test | Steps | Expected Result | Status |
|---|---|---|---|---|
| ING-01 | Add a valid ingredient | Add Test Flour with quantity 10, unit kg, minimum stock 2, and an expiry date 30 days after testing. Save. | The ingredient is saved and displayed with the entered values. | Not run |
| ING-02 | View ingredient details | Open the ingredient list after adding Test Flour. | The saved name, quantity, unit, expiry date, and minimum stock match the entry. | Not run |
| ING-03 | Edit an ingredient | Change Test Flour quantity from 10 to 15 and save. | The existing record shows 15 kg without creating a duplicate. | Not run |
| ING-04 | Delete an ingredient | Create Test Sugar, choose Delete, and confirm. | Test Sugar is removed from the list and storage. | Not run |
| ING-05 | Cancel deletion | Choose Delete for Test Flour, then cancel. | Test Flour remains unchanged. | Not run |
| ING-06 | Missing ingredient name | Leave the name empty, complete the other fields, and save. | A clear validation message appears and the record is not saved. | Not run |
| ING-07 | Name containing only spaces | Enter spaces as the name, complete the other fields, and save. | The entry is rejected and is not saved. | Not run |
| ING-08 | Missing unit | Enter valid values but leave the unit unselected or empty. Save. | The entry is rejected with a clear message. | Not run |
| ING-09 | Negative quantity | Enter -5 as the quantity and attempt to save. | The entry is rejected and no record is saved. | Not run |
| ING-10 | Zero quantity | Add Test Salt with quantity 0, unit kg, and minimum stock 1. | The record saves successfully because zero represents no stock available. | Not run |
| ING-11 | Decimal quantity | Add Test Butter with quantity 1.5, unit kg, and minimum stock 0.5. | The record saves with the decimal values preserved. | Not run |
| ING-12 | Nonnumeric quantity | Attempt to enter letters in the quantity field and save. | The field prevents the input or shows an error; no invalid record is saved. | Not run |
| ING-13 | Negative minimum stock | Enter -1 as the minimum stock level and attempt to save. | The entry is rejected with a clear message. | Not run |
| ING-14 | Zero minimum stock | Enter 0 as the minimum stock level with other valid values. | The record saves successfully. | Not run |
| ING-15 | Invalid edit | Edit a saved ingredient to have a negative quantity and save. | The change is rejected and the previously saved quantity remains unchanged. | Not run |
| ING-16 | Saved data survives restart | Add an ingredient, restart the application, and reopen the list. | The ingredient and its values remain saved. | Not run |
| ING-17 | Edits survive restart | Edit an ingredient, save, restart the application, and reopen the list. | The updated values remain saved. | Not run |
| ING-18 | Deletion survives restart | Delete a test ingredient, restart the application, and reopen the list. | The deleted ingredient does not reappear. | Not run |
| ING-19 | Empty ingredient list | Open ingredient management with no ingredient records in the test database. | The page loads without errors and provides a way to add an ingredient. | Not run |
| ING-20 | Smaller screen layout | Open the ingredient list and forms at a narrow browser width. | Labels, fields, messages, and action buttons remain readable and usable. | Not run |

## Execution Notes

When running a test, record:

- Test ID
- Application commit
- Actual result
- Pass, Fail, or Blocked
- Any related defect

Do not mark a test as passed until it has been executed.

## Defect Record

For each defect, record:

- Defect ID:
- Related test ID:
- Short title:
- Application commit:
- Steps to reproduce:
- Expected result:
- Actual result:
- Screenshot, if useful:
- Assigned owner:
- Retest result: