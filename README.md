# Tofu Jobs API

[Open the hosted demo](https://tofu-jobs-demo.vercel.app) · [Frontend source](https://github.com/brennanbutler01/tofu-jobs) · [API source](https://github.com/brennanbutler01/tofu-jobs-api)

C#/.NET 10 and PostgreSQL backend for the Tofu Jobs organizer. The six record types are companies, application lists, jobs, interviews, activities and cover-letter links. Every route checks the authenticated owner, including related record ownership.

## Disposable local demo

```sh
export VISITOR_DEMO_SIGNING_KEY="$(python3 -c 'import secrets; print(secrets.token_urlsafe(48))')"
docker compose -f compose.visitor.yaml up --build -d
python3 tests/test_visitor.py
```

The API listens at http://127.0.0.1:5215. Start the companion client on port 5216. The database is local and contains only invented visitor data. Stop it with `docker compose -f compose.visitor.yaml down`; add `-v` only to discard its local demo volume.

## Visitor isolation

POST `/demo/session` creates an opaque visitor identity and a one-hour signed token. All record routes require authentication. Each request checks that the stored session still exists and has not expired. DELETE `/demo/session` revokes access and deletes all owned records transactionally. Reset and writes lock the same session row to prevent late writes leaving records behind.

The demo limits each visitor to 100 records, request bodies to 16 KiB, and active sessions to 100. Request limits are per server instance, not a distributed denial-of-service defense. Cleanup runs at startup and every five minutes while the process is awake. Expired sessions lose access immediately; physical cleanup can wait until a sleeping host resumes.

Uploads and Auth0 management endpoints are unavailable in the visitor demo. Never enter real job-search or personal information.

## Hosting

`vercel.json` explicitly selects the .NET container service. Use a dedicated Vercel project and PostgreSQL database. Set these server-side environment variables:

- `VisitorDemo__Enabled=true`
- `VisitorDemo__SigningKey`: random secret, at least 32 characters
- `ConnectionStrings__DefaultConnectionString`: dedicated PostgreSQL connection string
- `Cors__Origins__0`: exact frontend origin
- `AllowedHosts`: deployment hostnames, localhost and 127.0.0.1
- `ASPNETCORE_ENVIRONMENT=Production`
- `PORT=8080`

The container initializes the schema only for the isolated demo database. Do not point it at an existing production database. Normal authenticated mode requires your own Auth0 configuration and separate schema migration management.

`tests/test_visitor.py` validates ownership, forged owners, cross-visitor requests, foreign relationships, reset, simultaneous writes and database expiry. Direct database checks run only against the local disposable Docker database.
