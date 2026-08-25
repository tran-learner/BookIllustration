# Testing

## Testing strategies

1. Backend tests for pipeline steps:

   - Each test creates an isolated temporary SQLite database and seeds only the data required for that pipeline step.
   - The test signs in as the project owner to obtain authentication, then calls the endpoint that triggers the pipeline step.
   - Happy-path success is evaluated by confirming that the API returns its expected successful status code and that the expected data and pipeline state are persisted in the database.
   - Duplicate-request protection is evaluated by calling the endpoint again while the step is still in its valid processing period and confirming that the second request returns `409 Conflict`.

## Test Report

### 1. Tests for pipeline steps

---

#### Style step - 13:12 24/08/2026

Test file: `backend\BookIllustration_Backend.Tests\StyleTesting.cs`

- Happy path with mocked Gemini responses. Expected: an authenticated project owner receives `204 No Content`; the Style step becomes `Completed`; the generated style is saved to the project; and a pending Characters step is created.
- A duplicate request while the current Style step is still running. Expected: the second request receives `409 Conflict`, while the first request can continue and complete.

Test result:
[xUnit.net 00:00:01.83]   Finished:    BookIllustration_Backend.Tests
  BookIllustration_Backend.Tests test succeeded (2.5s)

Test summary: total: 2, failed: 0, succeeded: 2, skipped: 0, duration: 2.5s
Build succeeded in 4.3s

---

#### Character step - 19:24 24/08/2026

Test file: `backend\BookIllustration_Backend.Tests\CharacterTesting.cs`

- Happy path with mocked Gemini responses. Expected: an authenticated project owner receives `204 No Content`; the Characters step becomes `Completed`; parsed character records are saved; and a pending Portraits step is created with its character interaction ID and parsed character prompts in `StepData`.
- A duplicate request while the current Characters step is still running. Expected: the second request receives `409 Conflict`, while the first request can continue and complete.

Test result:
BookIllustration_Backend.Tests test succeeded (2.8s)

Test summary: total: 2, failed: 0, succeeded: 2, skipped: 0, duration: 2.8s
Build succeeded in 4.7s

---

#### Portrait step - 21:00 24/08/2026

Test file: `backend\BookIllustration_Backend.Tests\PortraitTesting.cs`

- Happy path with mocked Gemini responses. Expected: an authenticated project owner receives `204 No Content`; the Portraits step becomes `Completed`; each pending character receives an illustration path; the matching image files are saved in the isolated illustrations directory; and a pending Chapters step is created with the character and image interaction IDs in `StepData`.

Test result:
BookIllustration_Backend.Tests test succeeded (2.8s)

Test summary: total: 2, failed: 0, succeeded: 2, skipped: 0, duration: 2.8s
Build succeeded in 4.6s

---

#### Chapters step - 21:02 24/08/2026

Test file: `backend\BookIllustration_Backend.Tests\ChapterTesting.cs`

- Happy path with mocked Gemini responses. Expected: an authenticated project owner receives `204 No Content`; the Chapters step becomes `Completed`; the generated chapter prompt and chapter interaction ID are saved in `StepData`; the Chapter record is persisted; and a pending Illustrations step is created with the image interaction ID in `StepData`.

Test result:
BookIllustration_Backend.Tests test succeeded (2.3s)

Test summary: total: 2, failed: 0, succeeded: 2, skipped: 0, duration: 2.3s
Build succeeded with 2 warning(s) in 5.1s

---

#### Illustration step - 22:03 24/08/2026

Test file: `backend\BookIllustration_Backend.Tests\IllustrationTesting.cs`

- Happy path with mocked Gemini responses. Expected: an authenticated project owner receives `204 No Content`; the Illustrations step becomes `Completed`; each pending chapter receives an illustration path; and the matching image files are saved in the isolated illustrations directory.

Test result: 
BookIllustration_Backend.Tests test succeeded (3.0s)

Test summary: total: 2, failed: 0, succeeded: 2, skipped: 0, duration: 3.0s
Build succeeded with 2 warning(s) in 6.3s

#### All steps testing after splitting each step into a claim phase and an execute phase
Test dir: `backend\BookIllustration_Backend.Tests\
09:43 25/08/2026
  BookIllustration_Backend.Tests test succeeded (3.7s)

Test summary: total: 10, failed: 0, succeeded: 10, skipped: 0, duration: 3.7s
Build succeeded with 1 warning(s) in 7.5s
