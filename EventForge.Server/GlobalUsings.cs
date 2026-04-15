// Global using statements for the EventForge.Server project

global using Prym.DTOs.Audit;
global using Prym.DTOs.Auth;
global using Prym.DTOs.Calendar;
global using Prym.DTOs.Common;
global using Prym.DTOs.Events;
global using Prym.DTOs.Health;
global using Prym.DTOs.Performance;
// Logs DTOs are in SuperAdmin namespace
global using Prym.DTOs.SuperAdmin;
global using Prym.DTOs.Tenants;
global using EventForge.Server.Data;
global using EventForge.Server.Data.Entities.Audit;
global using EventForge.Server.Data.Entities.Auth;
global using EventForge.Server.Data.Entities.Business;
global using EventForge.Server.Data.Entities.Common;
global using EventForge.Server.Data.Entities.Configuration;
global using EventForge.Server.Data.Entities.Documents;
global using EventForge.Server.Data.Entities.Events;
global using EventForge.Server.Data.Entities.PriceList;
global using EventForge.Server.Data.Entities.Products;
global using EventForge.Server.Data.Entities.Promotions;
global using EventForge.Server.Data.Entities.StationMonitor;
global using EventForge.Server.Data.Entities.Store;
global using EventForge.Server.Data.Entities.Teams;
global using EventForge.Server.Data.Entities.Warehouse;
global using EventForge.Server.Data.Entities.Reports;
global using EventForge.Server.Extensions;
global using EventForge.Server.Hubs;
global using EventForge.Server.Services.Audit;
global using EventForge.Server.Services.Auth;
global using EventForge.Server.Services.Configuration;
global using EventForge.Server.Services.Performance;
global using EventForge.Server.Services.Tenants;

// ── Prym.Hardware shared types ──────────────────────────────────────────────
// Type aliases so existing code in the Communication namespace continues to
// compile without any using-statement changes.
global using FiscalPrinterCommunicationException = Prym.Hardware.Exceptions.FiscalPrinterCommunicationException;
global using ICustomPrinterCommunication = Prym.Hardware.Interfaces.ICustomPrinterCommunication;
global using IEpsonChannel = Prym.Hardware.Interfaces.IEpsonChannel;

