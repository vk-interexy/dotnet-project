use dotnet;
ALTER TABLE `dotnet`.`parts` 
ADD COLUMN `PartCompatibilityComplete` BIT(1) NULL DEFAULT 0 AFTER `QuantityPricingUpdatedDate`;