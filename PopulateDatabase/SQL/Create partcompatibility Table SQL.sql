use dotnet;
CREATE TABLE `partcompatibility` (
  `PartCompatibilityID` int(11) NOT NULL AUTO_INCREMENT,
  `CompatibilityKey` varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `PartNumber` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `Make` varchar(200) CHARACTER SET utf8 COLLATE utf8_general_ci DEFAULT NULL,
  `Model` varchar(200) CHARACTER SET utf8 COLLATE utf8_general_ci DEFAULT NULL,
  `Engine` varchar(1000) CHARACTER SET utf8 COLLATE utf8_general_ci DEFAULT NULL,
  `StartYear` int(11) DEFAULT NULL,
  `EndYear` int(11) DEFAULT NULL,
  PRIMARY KEY (`PartCompatibilityID`),
  UNIQUE KEY `idx_partcompatibility_compatibilityKey` (`CompatibilityKey`),
  KEY `idx_partcompatibility_PartNumber` (`PartNumber`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;