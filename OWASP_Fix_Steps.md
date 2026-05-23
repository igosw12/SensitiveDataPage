# ?? Plan naprawy – OWASP Top 10 / SensitiveDataPage

## ?? Priorytet WYSOKI

### 1. Weryfikacja reCAPTCHA po stronie serwera
- [ ] Zarejestruj klucze reCAPTCHA v3 w Google Admin Console
- [ ] Dodaj `RecaptchaSecret` do `appsettings.json` / user-secrets
- [ ] Wstrzyknij `IHttpClientFactory` do `RegisterModel`
- [ ] W `OnPostAsync()` wywo³aj `https://www.google.com/recaptcha/api/siteverify` z tokenem i kluczem
- [ ] Odrzuæ ¿¹danie jeœli `success == false` lub `score < 0.5`

---

### 2. Rate limiting na endpointach autoryzacji
- [ ] Zarejestruj `builder.Services.AddRateLimiter(...)` w `Program.cs`
- [ ] Zdefiniuj politykê `fixed` lub `sliding window` (np. 10 req/min per IP)
- [ ] Zastosuj `[EnableRateLimiting("auth")]` na stronach: `Login`, `Register`, `ForgotPassword`, `PasswordRestart`
- [ ] Dodaj `app.UseRateLimiter()` w potoku middleware (przed `UseRouting`)

---

## ?? Priorytet ŒREDNI

### 3. Nag³ówki bezpieczeñstwa HTTP
- [ ] Dodaj middleware w `Program.cs` ustawiaj¹cy nag³ówki odpowiedzi:
  - `X-Content-Type-Options: nosniff`
  - `X-Frame-Options: DENY`
  - `Referrer-Policy: strict-origin-when-cross-origin`
  - `Content-Security-Policy: default-src 'self'; ...`
- [ ] Przetestuj nag³ówki przez [securityheaders.com](https://securityheaders.com)

---

### 4. Konfiguracja cookie uwierzytelniania
- [ ] W `Program.cs` w bloku `.AddCookie(options => {...})` dodaj:
  - `options.Cookie.SameSite = SameSiteMode.Strict`
  - `options.Cookie.SecurePolicy = CookieSecurePolicy.Always`
  - `options.Cookie.HttpOnly = true`

---

### 5. Dodanie pola `Details` do modelu `AuditLog`
- [ ] Dodaj w³aœciwoœæ `public string? Details { get; set; }` do `Models/AuditLog.cs`
- [ ] Zaktualizuj konfiguracjê encji w `ApplicationDbContext.cs` (np. `HasMaxLength(1000)`)
- [ ] Przypisz wartoœæ `Details = details` w `AuditMechanism.LogAudit()`
- [ ] Wygeneruj i zastosuj migracjê EF Core: `Add-Migration AddAuditLogDetails` ? `Update-Database`

---

### 6. Zast¹pienie pustych bloków `catch {}` logowaniem
- [ ] Wstrzyknij `ILogger<DashboardModel>` do `DashboardModel`
- [ ] W `OnGetEntriesAsync()` zast¹p `catch { }` blokiem `catch (Exception ex) { _logger.LogError(ex, "..."); }`
- [ ] Przejrzyj pozosta³e puste bloki catch w ca³ym projekcie i zastosuj to samo

---

### 7. Usuniêcie user enumeration przy resecie has³a
- [ ] W `ForgotPasswordModel.OnPostAsync()` zwracaj **tê sam¹** odpowiedŸ sukcesu niezale¿nie od tego, czy email istnieje w bazie
- [ ] Loguj wewnêtrznie fakt braku konta (audit log / logger), ale nie ujawniaj tego klientowi
- [ ] Przyk³ad: zawsze zwracaj `new JsonResult(new { success = true, message = "forgot.emailSent" })`

---

### 8. Subresource Integrity (SRI) dla zewnêtrznych zasobów
- [ ] W `Pages/Shared/_Layout.cshtml` dla ka¿dego zewnêtrznego `<script>` i `<link>` z CDN:
  - Wygeneruj hash SRI na [srihash.org](https://www.srihash.org)
  - Dodaj atrybut `integrity="sha384-..."` i `crossorigin="anonymous"`

---

## ?? Priorytet NISKI

### 9. Polityka ról i uprawnieñ
- [ ] Zdefiniuj role (np. `User`, `Admin`) w bazie lub claimach
- [ ] Zarejestruj polityki: `builder.Services.AddAuthorization(o => o.AddPolicy("Admin", p => p.RequireRole("Admin")))`
- [ ] Zastosuj `[Authorize(Policy = "Admin")]` na stronach administracyjnych
- [ ] Rozwa¿ weryfikacjê tier-ów danych po stronie serwera (nie tylko jako parametr frontendowy)

---

### 10. Bezpieczne przechowywanie sekretów
- [ ] Przenieœ `EmailStrings:EmailPassword`, `Encryption:Key`, `ConnectionStrings:DefaultConnection` do:
  - **Development**: `dotnet user-secrets`
  - **Production**: Azure Key Vault / zmienne œrodowiskowe
- [ ] Upewnij siê, ¿e `appsettings.json` nie zawiera ¿adnych sekretów w repozytorium
- [ ] Dodaj `appsettings.*.json` do `.gitignore` jeœli zawieraj¹ wartoœci œrodowiskowe

---

### 11. Naprawa b³êdu logiki lockout konta
- [ ] W `Login.cshtml.cs` popraw warunek blokady przy 5. nieudanej próbie:
  - Zmieñ `user.LockoutUntil < DateTime.UtcNow` na `user.LockoutUntil == null || user.LockoutUntil < DateTime.UtcNow`
  - Zapewni to ustawienie blokady przy pierwszym wejœciu w ga³¹Ÿ `>= 5`

---

### 12. Usuniêcie `Task.Delay(500)` z `Program.cs`
- [ ] Usuñ trzy wywo³ania `await Task.Delay(500)` z `Program.cs`
- [ ] Zidentyfikuj i napraw rzeczywist¹ przyczynê potrzeby opóŸnienia (jeœli istnieje)

---

## ?? Podsumowanie

| Krok | Obszar | Priorytet | OWASP |
|------|--------|-----------|-------|
| 1 | Weryfikacja reCAPTCHA server-side | ?? Wysoki | A04 |
| 2 | Rate limiting | ?? Wysoki | A04, A07 |
| 3 | Nag³ówki HTTP | ?? Œredni | A05 |
| 4 | Konfiguracja cookie | ?? Œredni | A05 |
| 5 | Pole `Details` w `AuditLog` | ?? Œredni | A09 |
| 6 | Logowanie b³êdów (catch) | ?? Œredni | A09 |
| 7 | User enumeration – reset has³a | ?? Œredni | A07 |
| 8 | SRI dla CDN | ?? Œredni | A08 |
| 9 | Polityka ról | ?? Niski | A01 |
| 10 | Bezpieczne przechowywanie sekretów | ?? Niski | A02, A05 |
| 11 | B³¹d logiki lockout | ?? Niski | A07 |
| 12 | Usuniêcie `Task.Delay` | ?? Niski | A04 |
