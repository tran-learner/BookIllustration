# Testing

## Testing strategies

1. Backend tests for pipeline steps: test the happy path for each step, then test a new request arriving while that step is still within its valid processing period.


## Style step — 13:12 24/08/2026

- Happy path with mocked Gemini responses. Expected: an authenticated project owner receives `204 No Content`; the Style step becomes `Completed`; the generated style is saved to the project; and a pending Characters step is created.
- A duplicate request while the current Style step is still running. Expected: the second request receives `409 Conflict`, while the first request can continue and complete.

Test result: 
[xUnit.net 00:00:01.83]   Finished:    BookIllustration_Backend.Tests
  BookIllustration_Backend.Tests test succeeded (2.5s)

Test summary: total: 2, failed: 0, succeeded: 2, skipped: 0, duration: 2.5s
Build succeeded in 4.3s