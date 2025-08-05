# HealthStatusFooter Component Documentation

## Overview

The `HealthStatusFooter` component provides real-time health monitoring for the EventForge application. It displays system health information in a compact, responsive footer that updates automatically every 30 seconds.

## Features

### 🔄 Periodic Health Monitoring
- Automatically calls the `/api/v1/health/detailed` endpoint every 30 seconds
- Manual refresh capability via refresh button
- Graceful error handling with fallback states

### 📊 Health Data Grouping

The component organizes health information into 7 thematic areas:

#### 1. **API**
- **Data Source**: `ApiStatus`, `Version`, `Timestamp`
- **Purpose**: Shows overall API health and version information
- **Status Indicators**: Healthy (green), Degraded (yellow), Unhealthy (red)

#### 2. **Database** 
- **Data Source**: `DatabaseStatus`, `DatabaseDetails`
- **Purpose**: Database connectivity and performance metrics
- **Details**: Connection status, response time, provider information

#### 3. **Authentication**
- **Data Source**: `AuthenticationStatus`, `AuthenticationDetails`
- **Purpose**: JWT configuration and authentication system health
- **Details**: JWT settings, password policies, lockout configuration

#### 4. **Migrations**
- **Data Source**: `AppliedMigrations`
- **Purpose**: Database migration status and history
- **Details**: Total applied migrations count, latest migration name

#### 5. **Performance**
- **Data Source**: `WorkingSet`, `Uptime`, `ProcessId`
- **Purpose**: Application performance and resource usage
- **Details**: Memory usage, uptime duration, process information
- **Status Logic**: 
  - Healthy: < 500MB memory
  - Warning: 500MB - 1GB memory
  - Error: > 1GB memory

#### 6. **Environment**
- **Data Source**: `Environment`, `MachineName`, `ProcessId`
- **Purpose**: Runtime environment information
- **Details**: Environment name (Development/Production), machine name

#### 7. **Dependencies**
- **Data Source**: `Dependencies` dictionary
- **Purpose**: External service health status
- **Details**: Status of all configured dependencies

## UI Design

### 🎨 Visual Components

#### Normal View
- **Health Chips**: Color-coded status indicators for each health area
- **Tooltips**: Detailed information on hover using MudBlazor `MudTooltip`
- **Controls**: Last update time, manual refresh button, minimize button

#### Minimized View
- **Status Dots**: Small colored dots showing critical health areas
- **Compact Text**: "Health Status" label
- **Expand Button**: Click to restore full view

### 📱 Responsive Design

#### Desktop (≥960px)
- Full component names displayed
- All health areas visible
- Last update timestamp shown
- Detailed tooltips with rich information

#### Tablet (600-959px)
- Abbreviated component names (API, DB, Auth, etc.)
- Condensed layout
- Simplified tooltips

#### Mobile (<600px)
- Icon-only display in minimized view
- Essential information only
- Touch-friendly interactions

### 🎯 Status Indicators

#### Color Coding
- **Green**: Healthy status
- **Yellow/Orange**: Warning or degraded status  
- **Red**: Error or unhealthy status
- **Gray**: Unknown or unavailable status

#### Icons
- **Healthy**: ✅ Check circle
- **Warning**: ⚠️ Warning triangle
- **Error**: ❌ Error circle
- **Unknown**: ❓ Help circle

## Integration

### Location
- **Position**: Fixed at bottom of viewport
- **Z-Index**: 1300 (above most content, below modals)
- **Visibility**: Only shown for authenticated users

### Layout Impact
- **Main Content Padding**: Added `pb-16` class to `MudMainContent` to prevent content overlap
- **Responsive Padding**: Automatically adjusts based on footer height

## Technical Implementation

### Component Structure
```
HealthStatusFooter.razor          # Main component markup and logic
HealthStatusFooter.razor.css      # Scoped styles for the component
```

### Dependencies
- `IHealthService`: Existing service for health API calls
- `ILogger<HealthStatusFooter>`: Logging functionality
- `MudBlazor`: UI components (tooltips, chips, buttons, etc.)

### Performance Considerations
- **Timer Management**: Proper disposal of refresh timer on component disposal
- **Error Recovery**: Continues operation even if health API calls fail
- **Memory Efficiency**: Minimal state management and efficient update cycles

### Accessibility Features
- **ARIA Labels**: Proper labeling for screen readers
- **Keyboard Navigation**: Full keyboard accessibility
- **High Contrast Support**: Compatible with high contrast modes
- **Reduced Motion**: Respects user's motion preferences

## Configuration Options

### Refresh Interval
- **Default**: 30 seconds
- **Configurable**: Can be modified in component code
- **Range**: Recommended 15-300 seconds

### Visibility Rules
- **Authentication Required**: Only visible for authenticated users
- **Role-Based**: Could be extended to show different information based on user roles

## Error Handling

### API Failures
- **Graceful Degradation**: Shows "Error" status when API is unavailable
- **Retry Logic**: Continues attempting updates on subsequent intervals
- **User Feedback**: Visual indication of connection issues

### Component Failures
- **Error Boundaries**: Wrapped in error handling to prevent app crashes
- **Logging**: Detailed error logging for debugging

## Future Enhancements

### Possible Extensions
1. **Configurable Refresh Rate**: User-adjustable update intervals
2. **Alert Thresholds**: Configurable warning/error thresholds
3. **Historical Data**: Trend indicators for health metrics
4. **Notification Integration**: Push notifications for critical issues
5. **Export Functionality**: Download health reports
6. **Advanced Filtering**: Hide/show specific health categories

### Integration Opportunities
1. **Dashboard Integration**: Embed health data in admin dashboards
2. **Monitoring Systems**: Integration with external monitoring tools
3. **Alerting Systems**: Connect to notification/email systems
4. **Performance Analytics**: Link to performance monitoring tools

## Maintenance

### Regular Updates Required
- **Health Categories**: Update grouping logic when new health data is added
- **Status Thresholds**: Adjust performance thresholds based on production experience
- **UI Responsiveness**: Test on new device sizes and update breakpoints

### Monitoring Points
- **API Response Times**: Monitor health endpoint performance
- **Client-Side Errors**: Track component error rates
- **User Interaction**: Monitor usage patterns for UX improvements