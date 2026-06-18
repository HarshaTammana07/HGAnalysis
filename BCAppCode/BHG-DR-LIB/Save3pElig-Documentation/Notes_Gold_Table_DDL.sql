-- Gold warehouse tables for the Notes pipeline.
-- Run this once in the bhg_gold Fabric Data Warehouse before enabling
-- the truncate + copy activities in Execute_Notes.

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'pats')
    EXEC('CREATE SCHEMA pats');
GO

CREATE TABLE pats.gd_3p_arnote
(
    SiteCode varchar(25) NOT NULL,
    arnID int NOT NULL,
    arnLIID int NULL,
    arnNOTE varchar(8000) NULL,
    arnUSER varchar(50) NULL,
    arnDATE datetime2(6) NULL,
    arnDtRemoved datetime2(6) NULL,
    arnStrRemovedReason varchar(8000) NULL,
    arnStrRemovedUser varchar(100) NULL,
    bid int NULL,
    arnDBnotes varchar(250) NULL,
    globalBatchId bigint NULL,
    RowChkSum int NULL,
    LastModAt datetime2(6) NULL,
    RowState bit NULL
);
GO

CREATE TABLE pats.gd_3p_claim_note
(
    SiteCode varchar(25) NOT NULL,
    tpcn int NOT NULL,
    tpcnTPCID int NULL,
    tpcnDtmAdded datetime2(6) NULL,
    tpcnStrAdded varchar(100) NULL,
    tpcnStrNote varchar(1000) NULL,
    tpcnStrType varchar(10) NULL,
    tpcnDtTickler datetime2(6) NULL,
    tpcnDtTicklerRemoved varchar(8000) NULL,
    tpcnStrTicklerRemovedNote varchar(8000) NULL,
    tpcnStrTicklerRemovedUser varchar(100) NULL,
    tpcnStrTicklerType varchar(500) NULL,
    globalBatchId bigint NULL,
    RowChkSum int NULL,
    LastModAt datetime2(6) NULL,
    RowState bit NULL
);
GO
