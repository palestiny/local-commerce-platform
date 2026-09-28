# Local Commerce Platform

A local-first commerce platform combining digital storefronts, product discovery, ordering, and local delivery.

## Project Status
Phase: Foundation / Architecture

GitHub is the source of truth. The repository starts intentionally clean so architecture, decisions, verification, and documentation are established before feature development.

## Core Transaction
Customer -> Store -> Product -> Cart -> Order -> Merchant -> Preparation -> Driver -> Delivery -> Delivered

## Initial Architecture
- Modular Monolith
- ASP.NET Core / .NET backend
- PostgreSQL
- React + TypeScript for web surfaces
- Mobile stack decided through a Design Gate
- External infrastructure introduced only when justified by a real requirement

## Engineering Method
Understand -> Map -> Design -> Trade-offs -> Decide -> TDD/Tests -> Implement -> Verify -> Document

See docs/00-PROJECT-CONSTITUTION.md.
