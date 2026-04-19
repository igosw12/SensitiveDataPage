# PR: Refactor and harden authentication flows

**Branch:** `feature/igosw/AddEditDeleteSensitiveData`  
**Target:** `master`  
**Date:** 2026-04-19

---

## Summary

This PR refactors and hardens the authentication and account-management flows: Login, Registration, Email Verification, Forgot Password, and Password Reset. It improves null-safety across models and page models, aligns the UI layout of the password-reset pages with the rest of the application, and applies minor code-quality fixes (property casing, removing dead code).

> **Note:** Despite the branch name `AddEditDeleteSensitiveData`, the sensitive-data CRUD pages (`Pages/SensitiveItems/`) are **not implemented** in this PR. That feature should be tracked separately or the branch renamed to avoid confusion.

---

## Changes

### Models
| File | Change |
|---|---|
| `Models/AuditLog.cs` | Made `Action`, `EntityType`, `IpAddress`, `UserAgent`, and `User` navigation property nullable to resolve compiler warnings and reflect optional database values. |
| `Models/SensitiveData.cs` | Marked `EncryptedData`, `EncryptionIV`, and `EncryptionTag` as `required`; made `User` navigation property nullable. |

### Pages - backend
| File | Change |
|---|---|
| `Pages/Login.cshtml.cs` | Added null guard for `user.PasswordHash` before splitting; added null guard for `user.Email` inside `SignInUser`; modernised `new Claim(...)` to target-typed `new(...)`; removed empty `OnGet()` stub. |
| `Pages/EmailVerification.cshtml.cs` | Added null guard for `dbToken.User` before accessing `User.IsVerified`. **See bug below.** |
| `Pages/PasswordRestart.cshtml.cs` | Renamed `tokenHash` to `TokenHash` (PascalCase for public property). |
| `Pages/Register.cshtml.cs` | Added developer comments noting that password hashing should be extracted to a service and that a stronger algorithm (Argon2 / bcrypt) is worth considering. |

### Pages - frontend
| File | Change |
|---|---|
| `Pages/ForgotPassword.cshtml` | Redesigned layout using `.wrapper` / `.left` / `.right` structure; added `ViewData["Title"]`; added client-side `required` attribute; added "Back to login" link; removed empty `onsubmit=""`. |
| `Pages/PasswordRestart.cshtml` | Redesigned layout to match ForgotPassword; added `ViewData["Title"]`; added client-side password pattern validation with custom `oninvalid` message; added confirmation field validation; added `_ValidationScriptsPartial`; added conditional display that hides the form when an error is present. |

---

## Issues Found

### Bug - Email verification is broken
**File:** `Pages/EmailVerification.cshtml.cs`

The `[BindProperty(SupportsGet = true)]` attribute was removed from the `Token` property:

```csharp
// Before (working)
[BindProperty(SupportsGet = true)]
public string? Token { get; set; }

// After (broken)
public string? Token { get; set; }
```

`OnGetAsync` still reads `Token` directly. Without `[BindProperty(SupportsGet = true)]`, the query-string value (`?token=xxx`) is never bound to the property and `Token` will always be `null`, so every verification attempt will fail with "Invalid token."

`PasswordRestart.cshtml.cs` correctly reads `Request.Query["token"]` directly and is unaffected. `EmailVerification.cshtml.cs` must either restore the attribute **or** switch to reading `Request.Query["token"]` explicitly.

### Placeholder text in views
Both `ForgotPassword.cshtml` and `PasswordRestart.cshtml` contain `Logo - TBD` inside the `.left` div. This should not reach production; replace with the actual logo element or remove the block.

### TempData double-read risk in PasswordRestart view
`PasswordRestart.cshtml` checks `TempData["ErrorMessage"] == null` twice - once to render the alert and once to conditionally show the form. TempData is consumed on the first read by default, so the second check may not behave as expected. Use `TempData.Peek("ErrorMessage")` for the read-without-consuming check, or assign the value to a `ViewData` entry in the page model before returning the page.

### No unit tests
`SensitiveDataPageTests/UnitTest1.cs` contains a single empty stub. None of the changed logic (null guards, password hash validation, email verification flow) is covered by automated tests.

### Password hashing algorithm
`Register.cshtml.cs` uses PBKDF2-HMACSHA256 (100,000 iterations). The added comment correctly flags that Argon2 or bcrypt would be a stronger choice. Consider acting on this before the feature ships.

### Positive observations
- `CryptographicOperations.FixedTimeEquals` is used correctly for hash comparison in Login - no timing-attack vector.
- Password complexity is enforced both client-side (HTML `pattern`) and server-side (`RegularExpression` data annotation) in PasswordRestart.
- `required` keyword added to encryption fields in `SensitiveData` prevents accidental creation of objects with missing encryption data.
- Target-typed `new(...)` used consistently in the updated Login code.

---

## Suggestions / Follow-ups

1. **Fix the email verification bug** (above) before merging.
2. Remove or implement the "Logo - TBD" placeholder in both views.
3. Add unit tests covering: null `PasswordHash` in Login, null `User` in email verification, expired/used token paths in PasswordRestart.
4. Extract password hashing into a dedicated `IPasswordHasher` service - the logic is duplicated between `Register.cshtml.cs` and `PasswordRestart.cshtml.cs`.
5. Rename the branch or open a separate branch for the actual Add/Edit/Delete sensitive-data CRUD feature.

---

## Review Checklist

- [ ] Email verification `[BindProperty(SupportsGet = true)]` bug resolved
- [ ] "Logo - TBD" placeholders removed
- [ ] TempData double-read in PasswordRestart view addressed
- [ ] Unit tests added for changed logic
- [x] Code builds cleanly targeting .NET 10
- [x] No secrets committed
- [x] Null guards added for nullable navigation properties
- [x] Password comparison uses constant-time equality
