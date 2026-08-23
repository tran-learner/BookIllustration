# Development Harness
After completing a code change, run the relevant validation checks for the affected part of the project, including type checking and any applicable build or compile checks. For backend changes, run the relevant .NET checks; for frontend changes, run the relevant Next.js checks.

## When a Check Fails

If a validation step fails:

1. Stop before making another change.
2. Report the error.
3. Briefly explain the likely cause.
4. Propose the next step.
5. Wait for approval before making another change.
