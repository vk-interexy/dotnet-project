CREATE DEFINER=`root`@`localhost` TRIGGER `partextendedpricingproperties_AFTER_UPDATE` AFTER UPDATE ON `partextendedpricingproperties` FOR EACH ROW BEGIN

	IF EXISTS (SELECT 1 FROM `dotnet`.`parts` WHERE PartNumber = NEW.PartNumber) THEN
			
		/* Update Fields in Database */
		UPDATE `dotnet`.`parts` 
		SET ExtendedPriceUpdateDate = SYSDATE()
		WHERE PartNumber = NEW.PartNumber;
		
	END IF;
END