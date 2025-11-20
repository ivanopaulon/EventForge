-- =============================================
-- Migration: Increase Icon column length in DashboardMetricConfigs
-- Date: 2025-11-20
-- Description: Fix string truncation error when saving MudBlazor SVG icons
-- =============================================

USE [EventData];
GO

-- Increase Icon column length from NVARCHAR(100) to NVARCHAR(1000)
ALTER TABLE [dbo].[DashboardMetricConfigs]
ALTER COLUMN [Icon] NVARCHAR(1000) NULL;
GO

-- Add/Update comment
EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Icon SVG path (MudBlazor icon). Increased to 1000 chars to accommodate full SVG paths.', 
    @level0type = N'SCHEMA', @level0name = 'dbo',
    @level1type = N'TABLE', @level1name = 'DashboardMetricConfigs',
    @level2type = N'COLUMN', @level2name = 'Icon';
GO

PRINT 'Migration 20251120_IncreaseIconColumnLength applied successfully.';
GO
