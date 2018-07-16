CREATE DATABASE IF NOT EXISTS `TradeData` DEFAULT CHARACTER SET latin1 COLLATE latin1_swedish_ci;

USE `TradeData`;

CREATE TABLE IF NOT EXISTS `ExchangeTrade` 
(
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  
`ExchId` varchar(50) NOT NULL,
 
`TradeBase` varchar(50) DEFAULT NULL,
  
`CurrencyId` varchar(50) DEFAULT NULL,
 
`TradeTimeStamp` timestamp NULL DEFAULT NULL,
  
`BidPrice` decimal(18,8) DEFAULT NULL,
  
`AskPrice` decimal(18,8) DEFAULT NULL,
  
`BidSize` decimal(18,8) DEFAULT NULL,
  
`AskSize` decimal(18,8) DEFAULT NULL,
  
`LastPrice` decimal(18,8) DEFAULT NULL,
 
`LastSize` decimal(18,8) DEFAULT NULL,
  
PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=332 DEFAULT CHARSET=latin1;