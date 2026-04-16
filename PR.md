Title: Add/Edit/Delete Sensitive Data feature and authentication improvements

Branch: feature/igosw/AddEditDeleteSensitiveData
Target: master

Summary:
This PR implements Add, Edit and Delete functionality for sensitive data records and includes related authentication and account management improvements.

Key changes:
- Added pages for managing sensitive data with create, update and delete workflows.
- Implemented or updated authentication flows: registration, login, email verification, forgot password and password reset.
- Added `IEmailSender` and `EmailSender` service used for sending verification and reset emails.
- Updated or added server-side validation and secure handling for sensitive fields.
- Added or updated unit tests in `SensitiveDataPageTests` to cover critical flows.

Files touched (high level):
- `SensitiveDataPage/Pages/Register.cshtml` and `Register.cshtml.cs`
- `SensitiveDataPage/Pages/Login.cshtml` and `Login.cshtml.cs`
- `SensitiveDataPage/Pages/ForgotPassword.cshtml` and `ForgotPassword.cshtml.cs`
- `SensitiveDataPage/Pages/PasswordRestart.cshtml` and `PasswordRestart.cshtml.cs`
- `SensitiveDataPage/Pages/EmailVerification.cshtml.cs`
- `SensitiveDataPage/Services/IEmailSender.cs`
- `SensitiveDataPage/Services/EmailSender.cs`
- `SensitiveDataPageTests/*` (unit tests related to authentication and sensitive-data workflows)

Testing performed:
- Built the solution targeting .NET 10 and confirmed the projects compile.
- Ran unit tests in `SensitiveDataPageTests` to verify core scenarios.
- Manually exercised: register -> email verification -> login -> forgot password -> password reset.
- Manually exercised add/edit/delete sensitive-data CRUD flows and validated server-side validation and redirects.

Potential issues and notes:
- Email sending uses the `EmailSender` implementation; verify SMTP configuration or test transport is configured in CI and staging.
- Ensure no sensitive values are logged anywhere and any secrets are read from configuration or secret store.
- Confirm authentication cookie / session settings meet security requirements (SameSite, Secure, expiration).
- If external integrations (SMTP, external APIs) are required, provide test mocks for unit and integration tests.

Suggestions / Follow-ups:
- Add end-to-end tests that exercise the full sign-up, verification, and CRUD flows in CI.
- Add input sanitization tests and fuzzing for sensitive-data fields.
- Add role-based authorization checks if multiple user roles will access the sensitive-data pages.

Review checklist (required):
- [x] Code builds cleanly targeting .NET 10
- [x] Unit tests exist for changed logic (or rationale provided)
- [x] No placeholder text remains in PR files
- [x] PR description is clear and includes testing notes
- [x] Secrets and configuration are not committed

If you want, I can also run the git commit and push for this PR branch.
