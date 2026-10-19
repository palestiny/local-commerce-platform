-- Additive and safe for existing databases. Legacy orders remain unowned until verified backfill data exists.
ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS "CustomerId" uuid NULL;
CREATE INDEX IF NOT EXISTS "IX_Orders_CustomerId" ON "Orders" ("CustomerId");
