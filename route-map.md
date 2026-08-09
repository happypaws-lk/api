# Route Map: API Routes to User Stories

This document maps every backend route to the user stories it satisfies from `docs/user-stories.md`.

## Auth (`/api/v1/auth`)

| Route | Method | User Story |
|-------|--------|------------|
| `/auth/signup/send-code` | POST | Registration (Adopter/Owner) |
| `/auth/signup/verify-code` | POST | Registration (Adopter/Owner) |
| `/auth/signup/complete` | POST | Registration (Adopter/Owner) |
| `/auth/login` | POST | Login and authentication (all roles) |
| `/auth/refresh` | POST | Login and authentication (session management) |
| `/auth/revoke` | POST | Login and authentication (logout) |
| `/auth/otp/send` | POST | Login and authentication (Admin/Vet elevated access) |
| `/auth/otp/verify` | POST | Login and authentication (Admin/Vet elevated access) |
| `/auth/forgot-password` | POST | Login and authentication (password recovery) |
| `/auth/verify-reset-code` | POST | Login and authentication (password recovery) |
| `/auth/reset-password` | POST | Login and authentication (password recovery) |
| `/auth/change-password` | POST | Login and authentication (password management) |

## Users (`/api/v1/users`)

| Route | Method | User Story |
|-------|--------|------------|
| `/users/me` | GET | Login and authentication (access dashboard and data) |
| `/users/me` | PUT | Login and authentication (profile management) |
| `/users/me/profile` | GET | Login and authentication (full account profile) |
| `/users/me/profile` | PUT | Login and authentication (profile management) |
| `/users/me/avatar` | POST | Login and authentication (profile management) |
| `/users/me/change-password` | POST | Login and authentication (password management) |
| `/users/me/roles` | POST | Role assignment (assign additional roles from dashboard) |
| `/users/me/kyc` | POST | Identity verification (KYC) (all roles) |
| `/users/me/lifestyle-profile` | GET | Lifestyle profile and matching (Adopter) |
| `/users/me/lifestyle-profile` | POST | Lifestyle profile and matching (Adopter) |
| `/users/me/devices` | GET | Notifications (device management) |
| `/users/me/devices` | POST | Notifications (FCM token registration) |
| `/users/me/devices/{id}` | DELETE | Notifications (device removal) |
| `/users/{id}` | GET | Reputation and trust badges (view public profile) |

## Rescue Cases (`/api/v1/rescues`)

| Route | Method | User Story |
|-------|--------|------------|
| `/rescues` | POST | Rescue reporting (all roles: Adopter, Foster, Transporter, Vet) |
| `/rescues` | GET | Browse rescue cases (Sponsor), Live case map (Admin) |
| `/rescues/{id}` | GET | Track sponsored case progress (Sponsor), Foster case handling |
| `/rescues/{id}/accept` | POST | Foster placement acceptance and case handling |
| `/rescues/{id}/updates` | POST | Foster case handling, Provide medical guidance (Vet), Case updates |
| `/rescues/{id}/updates` | GET | Track sponsored case progress (Sponsor), Foster case handling |
| `/rescues/{id}/resolve` | POST | Foster placement completion, Reputation (foster points) |
| `/rescues/{id}/urgency` | PUT | AI triage oversight (Admin), Review AI photo triage (Vet) |

## Listings (`/api/v1/listings`)

| Route | Method | User Story |
|-------|--------|------------|
| `/listings` | POST | Adoption listing (Owner), Transition to adoption listing (Foster) |
| `/listings` | GET | Browse and search adoption listings (Adopter, Sponsor) |
| `/listings/{id}` | GET | Browse and search adoption listings (detail view) |
| `/listings/{id}` | PUT | Adoption listing management (Owner) |
| `/listings/{id}` | DELETE | Adoption listing management (Owner) |
| `/listings/{id}/status` | PUT | Adoption completion, Reputation (adoption points) |
| `/listings/matches` | GET | Lifestyle profile and matching (Adopter) |
| `/listings/{id}/photos` | GET | Browse and search adoption listings (photos) |
| `/listings/{id}/photos` | POST | Adoption listing (Owner, photo upload) |
| `/listings/{id}/photos/{photoId}` | DELETE | Adoption listing management (Owner) |
| `/listings/{id}/applications` | GET | Submit and track adoption application (Owner view) |

## Applications (`/api/v1/applications`)

| Route | Method | User Story |
|-------|--------|------------|
| `/applications` | POST | Submit and track an adoption application (Adopter) |
| `/applications/me` | GET | Submit and track an adoption application (Adopter tracking) |
| `/applications/{id}/accept` | PUT | Submit and track adoption application (Owner accepts) |
| `/applications/{id}/decline` | PUT | Submit and track adoption application (Owner declines) |

## Conversations (`/api/v1/conversations`)

| Route | Method | User Story |
|-------|--------|------------|
| `/conversations` | GET | Private in-app messaging (all roles) |
| `/conversations` | POST | Private in-app messaging (start conversation) |
| `/conversations/{id}/messages` | GET | Private in-app messaging (message history) |
| `/conversations/{id}/read` | PUT | Private in-app messaging (read receipts) |

SignalR Hub at `/hubs/chat`:
- `SendMessage` — Private in-app messaging (real-time send)
- `ReceiveMessage` — Private in-app messaging (real-time receive)
- `MessageRead` — Private in-app messaging (read receipts)

## Pledges (`/api/v1/pledge`)

| Route | Method | User Story |
|-------|--------|------------|
| `/pledge` | POST | Pledge support to a case or listing (Sponsor) |
| `/pledge/me` | GET | Track sponsored case progress (Sponsor) |

## Transports (`/api/v1/transports`)

| Route | Method | User Story |
|-------|--------|------------|
| `/transports` | POST | Foster placement (request transport) |
| `/transports` | GET | Accept and track a transport task (Transporter browse) |
| `/transports/{id}/claim` | POST | Accept and track a transport task (Transporter claim) |
| `/transports/{id}/status` | PUT | Accept and track a transport task (status progression) |

## Notifications (`/api/v1/notification`)

| Route | Method | User Story |
|-------|--------|------------|
| `/notification` | GET | Notifications (all roles, paginated list) |
| `/notification/{id}/read` | PUT | Notifications (mark read) |
| `/notification/read-all` | PUT | Notifications (mark all read) |
| `/notification/unread-count` | GET | Notifications (badge count) |

Push delivery (via FCM, triggered server-side):
- Geo-targeted rescue alerts (Foster, Transporter, Vet)
- Geo-targeted transport alerts (Transporter)
- Application status changes (Adopter)
- Case updates (Foster, Sponsor)
- KYC decisions (all roles)
- Moderation warnings (all roles)

## Admin (`/api/v1/admin`)

| Route | Method | User Story |
|-------|--------|------------|
| `/admin/dashboard` | GET | Admin dashboard and reporting |
| `/admin/cases` | GET | Live case map and rescue coordination |
| `/admin/users` | GET | User account management |
| `/admin/users/{id}` | GET | User account management (detail view) |
| `/admin/users/{id}/suspend` | PUT | User account management (suspend) |
| `/admin/users/{id}/unsuspend` | PUT | User account management (unsuspend) |
| `/admin/listings` | GET | Content moderation (listing management) |
| `/admin/moderation` | POST | Content moderation (action) |
| `/admin/moderation` | GET | Content moderation (audit log) |
| `/admin/reputation/{userId}` | PUT | Reputation and dispute handling |
| `/admin/kyc/pending` | GET | Identity verification review |
| `/admin/kyc/{id}/approve` | POST | Identity verification review (approve) |
| `/admin/kyc/{id}/reject` | POST | Identity verification review (reject) |

## Setup (`/api/v1/setup`)

| Route | Method | User Story |
|-------|--------|------------|
| `/setup/status` | GET | Admin dashboard (first-time setup check) |
| `/setup/complete` | POST | Admin dashboard (initial admin account creation) |

## Coverage Summary

All user stories from `docs/user-stories.md` are served by the routes above:

| Functionality | Routes |
|---------------|--------|
| Registration | `/auth/signup/*` |
| Role assignment | `/users/me/roles` |
| Login and authentication | `/auth/login`, `/auth/refresh`, `/auth/revoke`, `/auth/change-password` |
| Identity verification (KYC) | `/users/me/kyc`, `/admin/kyc/*` |
| Lifestyle profile and matching | `/users/me/lifestyle-profile`, `/listings/matches` |
| Browse and search listings | `/listings` (GET) |
| Adoption listing (Owner) | `/listings` (POST), `/listings/{id}/*` |
| Submit and track application | `/applications/*` |
| Rescue reporting | `/rescues` (POST) |
| Private in-app messaging | `/conversations/*`, SignalR `/hubs/chat` |
| Reputation and trust badges | Automatic via services, `/users/{id}` (view), `/admin/reputation/{id}` (adjust) |
| Notifications | `/notification/*`, FCM push delivery |
| Geo-targeted alerts | Server-side push on rescue creation (proximity query) |
| Foster placement and case handling | `/rescues/{id}/accept`, `/rescues/{id}/updates`, `/rescues/{id}/resolve` |
| Transition to adoption listing | `/listings` (POST with `RescueCaseId`) |
| Transport tasks | `/transports/*` |
| Pledge support | `/pledge/*` |
| AI triage oversight | `/rescues/{id}/urgency` |
| Content moderation | `/admin/moderation`, `/admin/listings` |
| User account management | `/admin/users/*` |
| Admin dashboard | `/admin/dashboard` |
