CREATE TABLE IF NOT EXISTS "Stores" ("Id" uuid PRIMARY KEY,"IsActive" boolean NOT NULL);
CREATE TABLE IF NOT EXISTS "Products" ("Id" uuid PRIMARY KEY,"StoreId" uuid NOT NULL REFERENCES "Stores"("Id"),"Name" varchar(256) NOT NULL,"VariantName" varchar(256),"UnitPrice" numeric(18,2) NOT NULL,"IsOrderable" boolean NOT NULL);
CREATE TABLE IF NOT EXISTS "Carts" ("Id" uuid PRIMARY KEY,"CustomerId" uuid NOT NULL,"StoreId" uuid NOT NULL REFERENCES "Stores"("Id"),"IsActive" boolean NOT NULL);
CREATE TABLE IF NOT EXISTS "CartLines" ("Id" uuid PRIMARY KEY,"CartId" uuid NOT NULL REFERENCES "Carts"("Id") ON DELETE CASCADE,"ProductId" uuid NOT NULL REFERENCES "Products"("Id"),"VariantName" varchar(256),"Quantity" integer NOT NULL CHECK ("Quantity" > 0));
CREATE TABLE IF NOT EXISTS "Orders" ("Id" uuid PRIMARY KEY,"StoreId" uuid NOT NULL REFERENCES "Stores"("Id"),"OrderNumber" varchar(64) NOT NULL UNIQUE,"Status" varchar(64) NOT NULL,"CreatedAt" timestamptz NOT NULL,"UpdatedAt" timestamptz NOT NULL);
CREATE TABLE IF NOT EXISTS "OrderItems" ("Id" uuid PRIMARY KEY,"OrderId" uuid NOT NULL REFERENCES "Orders"("Id") ON DELETE CASCADE,"ProductId" uuid NOT NULL,"StoreId" uuid NOT NULL,"ProductName" varchar(256) NOT NULL,"VariantName" varchar(256),"UnitPrice" numeric(18,2) NOT NULL,"Quantity" integer NOT NULL CHECK ("Quantity" > 0),"LineDiscount" numeric(18,2) NOT NULL,"LineTotal" numeric(18,2) NOT NULL);
CREATE TABLE IF NOT EXISTS "IdempotencyRecords" ("Id" uuid PRIMARY KEY,"CustomerId" uuid NOT NULL,"Operation" varchar(128) NOT NULL,"IdempotencyKey" varchar(256) NOT NULL,"RequestFingerprint" varchar(128) NOT NULL,"OrderId" uuid REFERENCES "Orders"("Id"),"OrderNumber" varchar(64),"CreatedAt" timestamptz NOT NULL,"CompletedAt" timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_IdempotencyRecords_Customer_Operation_Key" ON "IdempotencyRecords" ("CustomerId","Operation","IdempotencyKey");
CREATE TABLE IF NOT EXISTS "Deliveries" ("Id" uuid PRIMARY KEY,"OrderId" uuid NOT NULL REFERENCES "Orders"("Id"),"StoreId" uuid NOT NULL REFERENCES "Stores"("Id"),"Status" varchar(64) NOT NULL,"DriverId" uuid,"CreatedAt" timestamptz NOT NULL,"UpdatedAt" timestamptz NOT NULL,"AssignedAt" timestamptz,"PickedUpAt" timestamptz,"OutForDeliveryAt" timestamptz,"DeliveredAt" timestamptz,"FailedAt" timestamptz,"FailureCode" varchar(128),"FailureReason" varchar(1024));
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Deliveries_Active_Order" ON "Deliveries" ("OrderId") WHERE "Status" IN ('Unassigned','Assigned','PickedUp','OutForDelivery');
CREATE TABLE IF NOT EXISTS "DeliveryStatusHistory" ("Id" uuid PRIMARY KEY,"DeliveryId" uuid NOT NULL REFERENCES "Deliveries"("Id") ON DELETE CASCADE,"ActorType" varchar(64) NOT NULL,"ActorId" uuid,"OccurredAt" timestamptz NOT NULL,"PreviousStatus" varchar(64) NOT NULL,"NewStatus" varchar(64) NOT NULL,"CommandName" varchar(128) NOT NULL,"CorrelationId" varchar(128) NOT NULL,"FailureCode" varchar(128),"FailureReason" varchar(1024));
CREATE INDEX IF NOT EXISTS "IX_DeliveryStatusHistory_Delivery_Occurred" ON "DeliveryStatusHistory" ("DeliveryId","OccurredAt");
CREATE OR REPLACE FUNCTION prevent_delivery_history_mutation() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    RAISE EXCEPTION 'DeliveryStatusHistory is append-only';
END;
$$;
DROP TRIGGER IF EXISTS "TR_DeliveryStatusHistory_AppendOnly" ON "DeliveryStatusHistory";
CREATE TRIGGER "TR_DeliveryStatusHistory_AppendOnly" BEFORE UPDATE OR DELETE ON "DeliveryStatusHistory" FOR EACH ROW EXECUTE FUNCTION prevent_delivery_history_mutation();
