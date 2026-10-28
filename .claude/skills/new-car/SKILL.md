---
name: new-car
description: Add a new, fabricated car to the inventory store (SunnysCars.Inventory.Api/Data/cars.json), run the tests, and show the new car as a table.
disable-model-invocation: true
---

# Add a new car to the inventory

Adds one made-up car to `SunnysCars.Inventory.Api/Data/cars.json` by editing the file directly (not through the running API).

Copy this checklist into your progress tracking and tick each item off as you go:

- [ ] **1. Read the store.** Read all of `SunnysCars.Inventory.Api/Data/cars.json` and note every existing `model` + `colour` pair, and the `id` of the last car in the array.
- [ ] **2. Check the current shape of a car.** Read `SunnysCars.Inventory.Api/Models/Car.cs` and `Models/CarRequest.cs` so the new entry has exactly the current fields and satisfies the validation rules. At the time of writing:
  - `make`, `model`: 1–50 characters
  - `colour`: 1–30 characters
  - `price`: 0–10,000,000, written with two decimals (e.g. `8995.00`)
  - `year`: 1886–2100
  - `mileage`: 0 or more
  - `transmission`: `"automatic"` or `"manual"` (lowercase)
- [ ] **3. Fabricate the car.** Invent a realistic make, model, colour, price, year, mileage and transmission. The **model + colour combination must not already exist** in the store (compare case-insensitively). Keep price and mileage plausible for the year.
- [ ] **4. Assign the id.** Set `id` to the last car's `id` + 1.
- [ ] **5. Append it.** Add the new object at the end of the array, matching the file's existing formatting: 2-space indentation, camelCase keys, same key order as the other entries, no trailing comma.
- [ ] **6. Run the tests.** From the repo root run `dotnet test`. All tests must pass. If any fail, fix the cause (usually a malformed or invalid entry) and re-run; do not finish with failing tests.
- [ ] **7. Show the result.** End by showing the new car as a Markdown table:

  | Field        | Value |
  |--------------|-------|
  | Id           | …     |
  | Make         | …     |
  | Model        | …     |
  | Colour       | …     |
  | Price        | …     |
  | Year         | …     |
  | Mileage      | …     |
  | Transmission | …     |

  Follow the table with the test result (e.g. "All N tests passed").

Do not commit the change unless the user asks.
