# 3. Separate the booking rules from storage

Date: 2026-11-08

## Status

Accepted

## Context

We want fast, reliable tests of the booking rules, and to run the app locally without a database.

## Decision

The rules live in the domain (`Course.Book`), with no framework dependencies. The application
depends on an `ICourseRepository` port. There are two adapters: in-memory (local development and
tests) and EF Core for Azure SQL. Configuration chooses one at start-up.

Overbooking is prevented in two places: `Course.Book` is thread-safe in memory, and in SQL every
booking updates the course row, which has a row version, so concurrent bookings conflict and are
retried. A unique index on (course, email) stops double bookings even if the code is wrong.

## Consequences

Domain tests run in milliseconds with no database. The in-memory adapter isn't for production.
