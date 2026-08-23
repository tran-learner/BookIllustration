The one command to start, the one command to test, prerequisites, env vars, and a short architecture overview 

# Book Illustration

A web app that turns a book's text into character portraits and a chapter illustration, using the Gemini API.

## Project Structure

- `frontend/`: Built with Next.js and provides the complete user interface.
- `backend/`: Built with ASP.NET Core; handles application logic and the core Gemini integration for generating illustrations.
- `AGENTS.md`: Defines the rules the agent must follow when collaborating with me.
- `HARNESS.md`: Describes the code verification process to run after each code change.

## Database Conceptual Model

![Database conceptual model](design/exported_files/cdm.png)
