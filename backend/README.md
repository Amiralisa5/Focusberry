# Focusberry Cloud API

ASP.NET Core + PostgreSQL backend for Focusberry account login and cross-device synchronization.

## Architecture

- ASP.NET Core 10 Minimal API
- PostgreSQL 17
- Entity Framework Core + Npgsql
- JWT access tokens
- BCrypt password hashing
- One JSONB document per user for the existing Focusberry local data model
- Last-write-wins versioning with a merge step in the PWA client

## Local development

```bash
cd backend
docker compose up --build
```

The API is then available at `http://localhost:8080`.

Before exposing the API publicly, replace the development JWT secret with a random secret of at least 32 characters. Never commit a production secret.

## Production deployment

Deploy `backend/Dockerfile` as a Docker Web Service and provide these environment variables:

```text
ConnectionStrings__Default=<PostgreSQL connection string>
Jwt__Issuer=Focusberry
Jwt__Audience=Focusberry
Jwt__Secret=<random 32+ character secret>
Cors__Origins__0=https://amiralisa5.github.io
```

The PWA's **Cloud** panel has an **API server** setting. Set it to the deployed API URL ending in `/api`, for example:

```text
https://focusberry-api.example.com/api
```

## API

- `GET /api/health`
- `POST /api/auth/register` `{ email, password }`
- `POST /api/auth/login` `{ email, password }`
- `GET /api/me` (JWT)
- `GET /api/sync` (JWT)
- `PUT /api/sync` (JWT) `{ data, version }`

## Security notes

The production database must use TLS and a strong unique password. JWT secrets must be stored as deployment secrets/environment variables. For a production release, add refresh-token rotation, email verification, password reset, rate limiting, audit logging, backups, and EF Core migrations before opening registration to a large public audience.
