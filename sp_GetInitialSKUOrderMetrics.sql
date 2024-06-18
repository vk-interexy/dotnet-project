DROP PROCEDURE `sp_GetInitialSKUOrderMetrics`;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_GetInitialSKUOrderMetrics`(IN `@maxSalesVelocityDays` INT) NOT DETERMINISTIC NO SQL SQL SECURITY DEFINER BEGIN
CREATE TEMPORARY TABLE IF NOT EXISTS dotnet.SKUSoldNonDistinct (SKU varchar(50));
CREATE TEMPORARY TABLE IF NOT EXISTS dotnet.SKUSoldDistinct (SKU varchar(50));

TRUNCATE TABLE dotnet.SKUSoldNonDistinct;
TRUNCATE TABLE dotnet.SKUSoldDistinct;

INSERT INTO dotnet.SKUSoldNonDistinct
SELECT item1Sku
FROM dotnet.ss_orders where item1Sku <> '' and item1Sku is not null and LENGTH(item1Sku) <= 50 and orderStatus in ('shipped','awaiting_shipment','on_hold') and orderDate >= DATE_SUB(current_date(), INTERVAL `@maxSalesVelocityDays` DAY);

INSERT INTO dotnet.SKUSoldNonDistinct
SELECT item2Sku
FROM dotnet.ss_orders where item2Sku <> '' and item2Sku is not null and LENGTH(item2Sku) <= 50 and orderStatus in ('shipped','awaiting_shipment','on_hold') and orderDate >= DATE_SUB(current_date(), INTERVAL `@maxSalesVelocityDays` DAY);

INSERT INTO dotnet.SKUSoldNonDistinct
SELECT item3Sku
FROM dotnet.ss_orders where item3Sku <> '' and item3Sku is not null and LENGTH(item3Sku) <= 50 and orderStatus in ('shipped','awaiting_shipment','on_hold') and orderDate >= DATE_SUB(current_date(), INTERVAL `@maxSalesVelocityDays` DAY);

INSERT INTO dotnet.SKUSoldNonDistinct
SELECT item4Sku
FROM dotnet.ss_orders where item4Sku <> '' and item4Sku is not null and LENGTH(item4Sku) <= 50 and orderStatus in ('shipped','awaiting_shipment','on_hold') and orderDate >= DATE_SUB(current_date(), INTERVAL `@maxSalesVelocityDays` DAY);

INSERT INTO dotnet.SKUSoldNonDistinct
SELECT item5Sku
FROM dotnet.ss_orders where item5Sku <> '' and item5Sku is not null and LENGTH(item5Sku) <= 50 and orderStatus in ('shipped','awaiting_shipment','on_hold') and orderDate >= DATE_SUB(current_date(), INTERVAL `@maxSalesVelocityDays` DAY);

INSERT INTO dotnet.SKUSoldNonDistinct
SELECT item6Sku
FROM dotnet.ss_orders where item6Sku <> '' and item6Sku is not null and LENGTH(item6Sku) <= 50 and orderStatus in ('shipped','awaiting_shipment','on_hold') and orderDate >= DATE_SUB(current_date(), INTERVAL `@maxSalesVelocityDays` DAY);

INSERT INTO dotnet.SKUSoldNonDistinct
SELECT item7Sku
FROM dotnet.ss_orders where item7Sku <> '' and item7Sku is not null and LENGTH(item7Sku) <= 50 and orderStatus in ('shipped','awaiting_shipment','on_hold') and orderDate >= DATE_SUB(current_date(), INTERVAL `@maxSalesVelocityDays` DAY);

INSERT INTO dotnet.SKUSoldNonDistinct
SELECT item8Sku
FROM dotnet.ss_orders where item8Sku <> '' and item8Sku is not null and LENGTH(item8Sku) <= 50 and orderStatus in ('shipped','awaiting_shipment','on_hold') and orderDate >= DATE_SUB(current_date(), INTERVAL `@maxSalesVelocityDays` DAY);


INSERT INTO dotnet.SKUSoldDistinct
SELECT DISTINCT SKU from dotnet.SKUSoldNonDistinct;


SELECT 
sqs.Sku, 
psm.PartNumber,
psm.PackageQuantity,
0e53 as ActualOnHand_SKU,
0e53 as ActualOnHand_Part,
pst.LeadTimeInDays,
0e53 as OnOrder_SKU,
0e53 as OnOrder_Part,
0e53 as InTransit_SKU,
0e53 as InTransit_Part,
0e53 as IncomingUnits_SKU,
0e53 as IncomingUnits_Part,
pst.DaysInStock,
0e53 as QuantitySoldInLastXDays_SKU,
0e53 as QuantitySoldInLastXDays_Part,
pst.SalesVelocityInDays,
pst.ForecastedGrowthPercentage,
0e53 as SalesVelocity_SKU,
0e53 as SalesVelocity_Part,
pst.ReorderBufferInDays,
0e53 as QuantityToOrder_SKU,
0e53 as QuantityToOrder_Part,
'None' as OrderingStatus,
CURRENT_DATE() as OrderDate,
CONVERT(1,SIGNED INTEGER) as AutoOrderEnabled
from dotnet.SKUSoldDistinct sqs
INNER JOIN dotnet.partskumaster psm on sqs.Sku = psm.SKU
INNER JOIN dotnet.partskuinventorytarget pst on sqs.Sku = pst.SKU;

END