CREATE DEFINER=`root`@`localhost` TRIGGER `parts_AFTER_UPDATE_PRICE_HISTORY` AFTER UPDATE ON `parts` FOR EACH ROW BEGIN
	
    /* Determine if one of the cost/price metrics changed */
    SET @ChangeDetected = (OLD.Cost <> NEW.Cost OR OLD.ListPrice <> NEW.ListPrice OR OLD.Core <> NEW.Core);
    
    IF @ChangeDetected = TRUE THEN
		INSERT INTO `dotnet`.`partpricehistory`
        (
			PartNumber, 
			ChangeDate, 
			CostOriginal, 
			CostNew,
			CostDifference,
			ListOriginal,
			ListNew,
			ListDifference,
			CoreOriginal,
			CoreNew,
			CoreDifference
        )
        VALUES
        (
			OLD.PartNumber,
            current_timestamp(),
            OLD.Cost,
            NEW.Cost,
            (NEW.Cost - OLD.Cost),
            OLD.ListPrice,
            NEW.ListPrice,
            (NEW.ListPrice - OLD.ListPrice),
            OLD.Core,
            NEW.Core,
            (NEW.Core - OLD.Core)
        );
    END IF;
END