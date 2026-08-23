## 01. Next.js for Frontend and ASP.NET Core for Backend

I chose Next.js for the frontend and ASP.NET Core for the backend because I am most familiar with both stacks. This choice will save time and help me work more efficiently. Codex did not push back on this choice. The trade-off is that I will need to work with two different languages and frameworks within the same project.

## 02. SQLite for Storage

For storage, I chose SQLite because it is simple and does not require a Docker image, a separate database server, or a cloud service. It still persists data across refreshes, sign-outs, and backend restarts. SQLite also provides proper relational data constraints and works well with EF Core, so I do not need to manually handle persistence concerns or spend extra time mapping application data to JSON files. Codex did not push back on this choice.

## 03. JWT in an HttpOnly Cookie for Session Representation

When designing the database, the session representation needs to be considered because it determines whether a corresponding table is required. Codex initially suggested a `Sessions` table as the conservative choice for a signed-in web app: the server retains control of each session, can revoke it immediately, set its expiry, and sign the user out across all devices.However, I proposed using a JWT in an `HttpOnly` cookie. For the scope of this assessment, it is sufficient to support sign-in and sign-out without requiring sign-out across every device. It also keeps the implementation simpler because the database does not need a separate `Sessions` table.Therefore, the database schema can stay minimal and focus on the application's core data.

## 04. Keep Step Name and Order as an Enum in PipelineSteps

I initially proposed a separate table for step names and their display order. Each `PipelineStep` record could reference that table instead of storing the step name and order itself, and adding or removing steps in the future would only require changing the references.

Codex pushed back on this approach. The five pipeline steps are fixed for this assessment, so the definition table would only contain static seed data while adding another relationship, migration, and query. Keeping the step type as an enum in `PipelineSteps`, with its name and display order mapped in code, is simpler for the current scope. I agreed with that opinion.
