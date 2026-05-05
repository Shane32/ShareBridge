# Copilot Instructions for .NET Projects

## Completion Requirements

Before completing any task or pull request, always run:

- `dotnet format`

If a `Unable to fix` message appears:

1. Run:
   - `dotnet format --verify-no-changes`
2. Review the reported issues.
3. Manually fix any remaining formatting or analyzer errors.
4. Re-run `dotnet format` until no issues remain.

If formatting changes are applied, include them in the same commit or pull request.

## Expectations

- Do not consider the task complete if formatting or analyzer issues remain.
- Ensure all modified files are properly formatted according to the repository’s configuration.

# Pull Request Creation

- After completing a task that involves code changes, always create a pull request using the `create_pull_request` tool.
- Do **not** create a pull request if the request was purely informational (e.g., asking a question, requesting a plan or analysis).
- Do **not** create a pull request if an open pull request already exists for the current branch.
