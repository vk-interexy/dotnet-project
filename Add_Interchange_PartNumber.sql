ALTER TABLE `dotnet`.`parts` 
CHANGE COLUMN `syncCentricStatus` `syncCentricStatus` VARCHAR(255) NOT NULL DEFAULT ''


CREATE TABLE `part_interchange_numbers` (
  `PartInterchangeNumberId` bigint(20) NOT NULL AUTO_INCREMENT,
  `PartNumber` varchar(50) NOT NULL DEFAULT '',
  `InterchangeNumber` varchar(45) NOT NULL DEFAULT '',
  `InterchangeManufacturer` varchar(200) DEFAULT NULL,
  PRIMARY KEY (`PartInterchangeNumberId`),
  KEY `IX_PartNumberInterChangeNumber` (`PartNumber`,`InterchangeNumber`, `InterchangeManufacturer`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4_general_ci;