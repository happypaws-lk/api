-- Script to safely clear all data from the HappyPaws database
-- Execution order is structured from leaf tables to root tables to avoid foreign key violations.

BEGIN;

-- 1. Tables dependent on Messages & Conversations
DELETE FROM public.conversation_participants;
DELETE FROM public.messages;
DELETE FROM public.conversations;

-- 2. Tables dependent on Animal Listings & Rescue Cases
DELETE FROM public.listing_photos;
DELETE FROM public.adoption_applications;
DELETE FROM public.transport_tasks;
DELETE FROM public.case_updates;
DELETE FROM public.pledges;

-- 3. Animal Listings (Depends on Rescue Cases and Users)
DELETE FROM public.animal_listings;

-- 4. Rescue Cases (Depends on Users)
DELETE FROM public.rescue_cases;

-- 5. Remaining direct dependents of Users
DELETE FROM public.identity_documents;
DELETE FROM public.lifestyle_profiles;
DELETE FROM public.moderation_actions;
DELETE FROM public.notifications;
DELETE FROM public.otp_codes;
DELETE FROM public.refresh_tokens;
DELETE FROM public.reputation_events;
DELETE FROM public.user_badges;
DELETE FROM public.user_devices;
DELETE FROM public.user_roles;

-- 6. Root Entities
DELETE FROM public.users;

-- 7. Singleton / Independent Tables
-- Uncomment the line below if you also wish to clear the system configuration
-- DELETE FROM public.system_configs;

-- Note: spatial_ref_sys is a PostGIS system table. Do not clear it.
-- Note: __EFMigrationsHistory tracks EF Core migrations. Do not clear it unless you intend to reset database migrations completely.

COMMIT;
