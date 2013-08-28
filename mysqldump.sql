/*
Navicat MySQL Data Transfer

Source Server         : localhost
Source Server Version : 50532
Source Host           : localhost:3306
Source Database       : test

Target Server Type    : MYSQL
Target Server Version : 50532
File Encoding         : 65001

Date: 2013-07-24 11:58:19
*/

SET FOREIGN_KEY_CHECKS=0;

-- ----------------------------
-- Table structure for `exception_log`
-- ----------------------------
DROP TABLE IF EXISTS `exception_log`;
CREATE TABLE `exception_log` (
  `timestamp` int(55) NOT NULL,
  `class` varchar(55) NOT NULL,
  `method` varchar(55) NOT NULL,
  `text` varchar(500) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- ----------------------------
-- Records of exception_log
-- ----------------------------

-- ----------------------------
-- Table structure for `mylog`
-- ----------------------------
DROP TABLE IF EXISTS `mylog`;
CREATE TABLE `mylog` (
  `loglevel` varchar(55) NOT NULL,
  `class` varchar(55) NOT NULL,
  `method` varchar(55) NOT NULL,
  `text` varchar(55) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- ----------------------------
-- Records of mylog
-- ----------------------------
