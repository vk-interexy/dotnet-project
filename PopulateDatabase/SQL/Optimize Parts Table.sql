use dotnet;
alter table parts drop KEY `ix_partsnumber`;
alter table parts drop KEY `ix_category_subcategory`;
alter table parts drop KEY `ix_category`;
alter table parts drop KEY `ix_subcategory`;
alter table parts drop KEY `ix_quantitypricingupdateddate`;

optimize table parts;

alter table parts add KEY `ix_partsnumber` (`PartNumber`);
alter table parts add KEY `ix_category_subcategory` (`Category`,`SubCategory`);
alter table parts add KEY `ix_category` (`Category`);
alter table parts add KEY `ix_subcategory` (`SubCategory`);
alter table parts add KEY `ix_quantitypricingupdateddate` (`QuantityPricingUpdatedDate`);
