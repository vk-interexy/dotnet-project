//MinPrice_AMZ_SC
dotnet.parts.MinPrice_AMZ_SC = 
(
	(
		dotnet.parts.Cost 
		*
		( 
		  1 + 
		  dotnet.partextendedpricingproperties.InboundShippingCostPercentage +
		  dotnet.partextendedpricingproperties.AverageShippingCostPercentage
		)
	)
    +
	dotnet.partextendedpricingproperties.LaborCost
	+
	(
		dotnet.parts.ListPrice *
		dotnet.partextendedpricingproperties.TransactionFeeAMZSCPercentage 
	)
	+
	dotnet.partextendedpricingproperties.OtherFee
)
*
dotnet.partextendedpricingproperties.MarginStandardPercentage;

//MinPrice_AMZ_VC
dotnet.parts.MinPrice_AMZ_SC = 
(
	(
		dotnet.parts.Cost 
		*
		( 
		  1 + 
		  dotnet.partextendedpricingproperties.InboundShippingCostPercentage +
		  dotnet.partextendedpricingproperties.AverageShippingCostPercentage
		)
	)
    +
	dotnet.partextendedpricingproperties.LaborCost
	+
	(
		dotnet.parts.ListPrice *
		dotnet.partextendedpricingproperties.TransactionFeeAMZVCPercentage 
	)
	+
	dotnet.partextendedpricingproperties.OtherFee
)
*
dotnet.partextendedpricingproperties.MarginWholesalePercentage


//MaxPrice
dotnet.parts.MaxPrice = (dotnet.parts.ListPrice + dotnet.parts.Core) * 
dotnet.partextendedpricingproperties.MarginStandardPercentage;

if(dotnet.parts.MaxPrice < dotnet.parts.MinPrice_AMZ_SC)
{
    dotnet.parts.MaxPrice = dotnet.parts.MinPrice_AMZ_SC * 1.5;
}

//WebPrice
dotnet.parts.WebPrice = 
(
	(
		dotnet.parts.Cost 
		*
		( 
		  1 + 
		  dotnet.partextendedpricingproperties.InboundShippingCostPercentage +
		  dotnet.partextendedpricingproperties.AverageShippingCostPercentage
		)
	) 
	+
	(
		dotnet.parts.ListPrice *
		dotnet.partextendedpricingproperties.TransactionFeeStandardPercentage 
	)
	+
	dotnet.partextendedpricingproperties.LaborCost
	+
	dotnet.partextendedpricingproperties.OtherFee
)
*
dotnet.partextendedpricingproperties.MarginStandardPercentage