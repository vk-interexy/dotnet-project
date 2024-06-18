CREATE DEFINER=`root`@`localhost` TRIGGER `parts_BEFORE_UPDATE` BEFORE UPDATE ON `parts` FOR EACH ROW BEGIN

	DECLARE minPrice_AMZ_SC DECIMAL(10,2);
    DECLARE minPrice_AMZ_VC DECIMAL(10,2);
    DECLARE maxPrice DECIMAL(10,2);
    DECLARE webPrice DECIMAL(10,2);
    
    /*****************  BEGIN - BOOT STRAP CODE TO LOAD BASE VALUES ****************************/
	IF EXISTS (SELECT 1 FROM `dotnet`.`partextendedpricingproperties` WHERE PartNumber = NEW.PartNumber) THEN
		
		/* Get Cost, ListPrice and Core */
		SET @Cost = IFNULL(NEW.Cost,0);
		SET @ListPrice = IFNULL(NEW.ListPrice,0);
		SET @Core = IFNULL(NEW.Core,0); 
		
		/* Get extended Properties */
		SELECT 
		InboundShippingCostPercentage,
		AverageShippingCostPercentage,
		LaborCost,
		TransactionFeeAMZSCPercentage,
		TransactionFeeAMZVCPercentage,
		TransactionFeeStandardPercentage,
		OtherFee,
		MarginStandardPercentage,
		MarginWholesalePercentage,
		AMZVCFreightCost,
		MaxPriceCoeff
		INTO 
		@InboundShippingCostPercentage,
		@AverageShippingCostPercentage,
		@LaborCost,
		@TransactionFeeAMZSCPercentage,
		@TransactionFeeAMZVCPercentage,
		@TransactionFeeStandardPercentage,
		@OtherFee,
		@MarginStandardPercentage,
		@MarginWholesalePercentage,
		@AMZVCFreightCost,
		@MaxPriceCoeff
		from `dotnet`.`partextendedpricingproperties` 
		WHERE PartNumber = NEW.PartNumber LIMIT 1;
        
        /*****************  END - BOOT STRAP CODE TO LOAD BASE VALUES ****************************/
			
		/*********************************************************************************************
			Calculate MinPrice_AMZ_SC 
		
			MinPriceAMZSC=
				((Cost+(Cost*Inbound Shipping Cost %)+(Cost*Average Shipping Cost %)+Labor Cost+ Other Fee) + 
				((Cost+(Cost*Inbound Shipping Cost %)+(Cost*Average Shipping Cost %)+Labor Cost+ Other Fee)*Transaction Fee AMZSC))
                *(1+Margin Standard) 
		
        *********************************************************************************************/
        
		SET minPrice_AMZ_SC = 
			(
				(@Cost + (@Cost * @InboundShippingCostPercentage) + (@Cost * @AverageShippingCostPercentage) + @LaborCost + @OtherFee) + 
				(@Cost + (@Cost * @InboundShippingCostPercentage) + (@Cost * @AverageShippingCostPercentage) + @LaborCost + @OtherFee) 
                *  @TransactionFeeAMZSCPercentage
			) * 
            (1 + @MarginStandardPercentage);
					
		
        /*********************************************************************************************
			Calculate MinPrice_AMZ_VC 
		
			MinPriceAMZVC=
				(@Cost + @LaborCost + @OtherFee) * 
				(@Cost * @AverageShippingCostPercentage) * 
				(1 + @MarginWholeSalePercentage) * 
				(1 + @TransactionFeeAMZVCPercentage)
		
        *********************************************************************************************/
		SET minPrice_AMZ_VC = 
			(@Cost + @LaborCost + @OtherFee) * 
			(@Cost * @AverageShippingCostPercentage) * 
			(1 + @MarginWholeSalePercentage) * 
			(1 + @TransactionFeeAMZVCPercentage);	
            
            
		/*********************************************************************************************
			Calculate MaxPrice 
		
			MaxPrice = MAX((ListPrice + Core) * (1 + Margin Standard %) , (MinPriceAMZSC * Max Price Coefficient))
		
        *********************************************************************************************/
		SET maxPrice = (@ListPrice + @Core) * (1 + @MarginStandardPercentage);
        /* Determine maxPrice is Maximum or not */
		IF maxPrice < MinPrice_AMZ_SC THEN
			SET maxPrice = MinPrice_AMZ_SC * @MaxPriceCoeff;
		END IF;
		
		/*********************************************************************************************
			Calculate WebPrice 
		
			WebPrice=((Cost+(Cost*Inbound Shipping Cost %)+(Cost*Average Shipping Cost %)+Labor Cost+Other Fee) +
					 ((Cost+(Cost*Inbound Shipping Cost %)+(Cost*Average Shipping Cost %)+Labor Cost+Other Fee) *Transaction Fee Standard))
                     *(1+Margin Standard)
		
        *********************************************************************************************/
        SET webPrice = 
			(
				(@Cost + (@Cost * @InboundShippingCostPercentage) + (@Cost * @AverageShippingCostPercentage) + @LaborCost + @OtherFee) + 
				(@Cost + (@Cost * @InboundShippingCostPercentage) + (@Cost * @AverageShippingCostPercentage) + @LaborCost + @OtherFee) 
                *  @TransactionFeeStandardPercentage
			) * 
            (1 + @MarginStandardPercentage);
							
			/* Update Fields in Database */
			SET NEW.MinPrice_AMZ_SC = minPrice_AMZ_SC;
			SET NEW.MinPrice_AMZ_VC = minPrice_AMZ_VC;
			SET NEW.MaxPrice = maxPrice;
			SET NEW.WebPrice = webPrice;
			
		END IF;
	/*END IF; /* updating */
END