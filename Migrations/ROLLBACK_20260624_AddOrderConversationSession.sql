-- ROLLBACK: 20260624_AddOrderConversationSession
-- Removes the OrderConversationSessions table.

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'OrderConversationSessions')
BEGIN
    DROP TABLE [dbo].[OrderConversationSessions];
    PRINT 'Dropped table OrderConversationSessions';
END
