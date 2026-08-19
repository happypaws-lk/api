-- =============================================================================
-- HappyPaws — Clear All Users and User-Associated Data
-- =============================================================================
-- Deletes every user and all data owned by or referencing them.
-- Tables with Restrict FKs must be cleared before their parent (users /
-- rescue_cases) to avoid FK violations. Tables with Cascade FKs clean up
-- automatically once their parent row is removed.
--
-- Safe to run on a PostgreSQL database (the engine used by HappyPaws).
-- Execute inside a transaction so the whole operation rolls back on error.
-- =============================================================================

BEGIN;

-- ── 1. Nullify self-referencing FKs on rescue_cases so rows can be deleted ──
--      (assigned_foster_id, urgency_overridden_by_id point back to users with
--      SetNull — PostgreSQL handles these automatically on user delete, but
--      we clear them explicitly here for clarity and safety.)

UPDATE rescue_cases SET assigned_foster_id      = NULL WHERE assigned_foster_id      IS NOT NULL;
UPDATE rescue_cases SET urgency_overridden_by_id = NULL WHERE urgency_overridden_by_id IS NOT NULL;

-- ── 2. Nullify self-referencing FK on identity_documents ─────────────────────
--      (reviewed_by_id → users, Restrict — must be cleared before user delete)

UPDATE identity_documents SET reviewed_by_id = NULL WHERE reviewed_by_id IS NOT NULL;

-- ── 3. Nullify conversation_participants.last_read_message_id ─────────────────
--      (→ messages, SetNull — clearing prevents circular FK issues when
--      messages are deleted as part of conversation cleanup)

UPDATE conversation_participants SET last_read_message_id = NULL WHERE last_read_message_id IS NOT NULL;

-- ── 4. Nullify optional FKs on pledges (case_id / listing_id, SetNull) ───────
UPDATE pledges SET case_id    = NULL WHERE case_id    IS NOT NULL;
UPDATE pledges SET listing_id = NULL WHERE listing_id IS NOT NULL;

-- ── 5. Nullify optional FKs on conversations (listing_id / case_id, SetNull) ──
UPDATE conversations SET listing_id = NULL WHERE listing_id IS NOT NULL;
UPDATE conversations SET case_id    = NULL WHERE case_id    IS NOT NULL;

-- ── 6. Nullify optional FK on animal_listings (rescue_case_id, SetNull) ──────
UPDATE animal_listings SET rescue_case_id = NULL WHERE rescue_case_id IS NOT NULL;

-- ── 7. Clear leaf tables that have Restrict FKs (no cascade from parent) ─────

-- Moderation actions (admin_id → users, Restrict)
DELETE FROM moderation_actions;

-- Transport tasks (transporter_id → users Restrict; case_id → rescue_cases Restrict)
DELETE FROM transport_tasks;

-- Case updates (user_id → users Restrict; case_id → rescue_cases Cascade)
DELETE FROM case_updates;

-- Messages (sender_id → users Restrict; conversation_id → conversations Cascade)
DELETE FROM messages;

-- Adoption applications (applicant_id → users Restrict; listing_id → animal_listings Cascade)
DELETE FROM adoption_applications;

-- Identity documents (user_id → users Restrict; reviewed_by_id nullified above)
DELETE FROM identity_documents;

-- Pledges (sponsor_id → users Restrict; case_id / listing_id nullified above)
DELETE FROM pledges;

-- ── 8. Clear tables that cascade from animal_listings ────────────────────────

-- Listing photos (listing_id → animal_listings, Cascade)
DELETE FROM listing_photos;

-- ── 9. Clear conversation participants and conversations ──────────────────────

-- conversation_participants (user_id → users Cascade; conversation_id → conversations Cascade)
DELETE FROM conversation_participants;

DELETE FROM conversations;

-- ── 10. Clear animal listings ─────────────────────────────────────────────────
DELETE FROM animal_listings;

-- ── 11. Clear rescue cases ────────────────────────────────────────────────────
DELETE FROM rescue_cases;

-- ── 12. Delete all users ──────────────────────────────────────────────────────
--       The following tables use Cascade and are cleaned up automatically:
--         • user_roles
--         • refresh_tokens
--         • otp_codes
--         • lifestyle_profiles
--         • user_devices
--         • notifications
--         • reputation_events
--         • user_badges

DELETE FROM users;

COMMIT;
