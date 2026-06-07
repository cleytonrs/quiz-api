# Quiz App - Backend API

REST API for a quiz application that allows users to practice programming knowledge through multiple-choice questions on various topics (HTML, CSS, JavaScript, Python, PHP, etc.).

## Technology Stack

| Layer | Technology |
|-------|-----------|
| Framework | .NET 9 / ASP.NET Core Web API |
| Database | SQL Server + Entity Framework Core |
| Authentication | ASP.NET Core Identity + JWT Bearer |
| Validation | FluentValidation |
| Documentation | Swagger / OpenAPI |
| Testing | xUnit + FsCheck (property-based testing) |

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SQL Server](https://www.microsoft.com/sql-server) (LocalDB, Express, or Developer Edition)
- [EF Core CLI Tools](https://learn.microsoft.com/ef/core/cli/dotnet)

### Installing EF Core CLI

```bash
dotnet tool install --global dotnet-ef
```

## Configuration

### Connection String

Edit `src/QuizApp/appsettings.json` with your SQL Server connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=QuizAppDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

### JWT (Authentication)

JWT settings are in `appsettings.json`. For production, change the secret key:

```json
{
  "Jwt": {
    "Key": "YourSecretKeyHere_AtLeast32Characters!",
    "Issuer": "QuizApp",
    "Audience": "QuizApp",
    "ExpirationInMinutes": 60
  }
}
```

### CORS

The Angular frontend is allowed by default at `http://localhost:4200`:

```json
{
  "Cors": {
    "FrontendUrl": "http://localhost:4200"
  }
}
```

## Getting Started

### 1. Create the database

From the `backend/src/QuizApp` directory:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 2. Run the application

```bash
dotnet run --project src/QuizApp
```

The API will be available at `https://localhost:7013` (or `http://localhost:5276`).

### 3. Access Swagger

Open in your browser: `https://localhost:7013/swagger`

## Running Tests

From the `backend` directory:

```bash
dotnet test
```

Tests include:
- **Unit tests** — Controllers, Services, Validators
- **Property-based tests (FsCheck)** — 13 formal correctness properties

## Project Structure

```
backend/
├── QuizApp.sln
├── src/
│   └── QuizApp/
│       ├── Controllers/       # API Controllers (Auth, Quiz, Session, Dashboard)
│       ├── Data/              # DbContext and EF Core configurations
│       ├── DTOs/              # Data Transfer Objects
│       ├── Exceptions/        # Custom exceptions (404, 409, 401, 400)
│       ├── Middleware/        # Global Exception Handler
│       ├── Models/            # Domain entities
│       ├── Services/          # Service layer (business logic)
│       ├── Validators/        # FluentValidation validators
│       └── Program.cs         # Configuration and startup
└── tests/
    └── QuizApp.Tests/
        ├── Controllers/       # Controller tests
        ├── Middleware/        # Middleware tests
        ├── PropertyTests/     # Property-based tests (FsCheck)
        ├── Services/          # Service tests
        └── Validators/        # Validation tests
```

## API Endpoints

### Authentication

| Method | Route | Description | Auth Required |
|--------|-------|-------------|---------------|
| POST | `/api/auth/register` | Register a new user | No |
| POST | `/api/auth/login` | Authenticate user | No |

### Quizzes

| Method | Route | Description | Auth Required |
|--------|-------|-------------|---------------|
| GET | `/api/quizzes` | List all quizzes | No |
| GET | `/api/quizzes/{id}` | Get quiz details | No |
| POST | `/api/quizzes` | Create a quiz | Yes |
| PUT | `/api/quizzes/{id}` | Update a quiz | Yes |
| DELETE | `/api/quizzes/{id}` | Delete a quiz | Yes |

### Quiz Sessions

| Method | Route | Description | Auth Required |
|--------|-------|-------------|---------------|
| POST | `/api/quizzes/{quizId}/sessions` | Start a session | No |
| POST | `/api/sessions/{sessionId}/answers` | Submit an answer | No |
| POST | `/api/sessions/{sessionId}/complete` | Complete session | No |
| GET | `/api/sessions/{sessionId}/result` | Get result | No |

### Dashboard

| Method | Route | Description | Auth Required |
|--------|-------|-------------|---------------|
| GET | `/api/dashboard` | Get user's quiz history | Yes |

## Features

- **Authentication & Registration** — JWT with Identity, minimum 8-character password
- **Quiz CRUD** — Create, read, update, and delete quizzes (protected by authentication)
- **Quiz Sessions** — Start, submit answers, complete with result calculation
- **Result Calculation** — Correct answer count, elapsed time, pass/fail determination (≥70%)
- **Guest Access** — Users can take quizzes without creating an account
- **Dashboard** — Complete history of all attempts for authenticated users
- **Validation** — Each question must have exactly 4 options with exactly 1 correct
- **Error Handling** — Global middleware with consistent JSON error responses
- **Duplicate Prevention** — Cannot answer the same question twice in a session

## Correctness Properties (Property-Based Tests)

| # | Property | Description |
|---|----------|-------------|
| 1 | Question Structure Invariant | Every question has exactly 4 options with 1 correct |
| 4 | Answer Persistence Round-Trip | Stored answers contain the exact IDs submitted |
| 5 | Elapsed Time Calculation | Elapsed time = completedAt - startedAt |
| 6 | Correct Answer Count | Correct count = number of answers where isCorrect=true |
| 7 | Pass/Fail Threshold | Pass if and only if correctAnswers/total ≥ 0.70 |
| 8 | Quiz List Completeness | List returns all quizzes with correct question counts |
| 10 | Password Length Validation | Passwords < 8 chars rejected, ≥ 8 chars accepted |
| 11 | Dashboard Chronological Ordering | Sessions sorted by completion date descending |
| 12 | Repeat Attempt Preservation | K completions = K records on dashboard |
| 13 | Guest Session Non-Association | Guest sessions have null userId and calculable results |

## License

This project is licensed under the MIT License.