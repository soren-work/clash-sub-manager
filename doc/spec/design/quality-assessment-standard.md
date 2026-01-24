# Service Quality Assessment Standard Specification

## Overview
Establish unified standards for Cloudflare IP quality assessment in the ClashSubManager system to ensure consistent quality evaluation results across all pages (/Admin/Index, /admin/clash-template, /admin/user-config).

## Assessment Dimensions

### 1. Network Performance Metrics
- **Packet Loss**: Data transmission reliability indicator
- **Latency**: Network response speed indicator  
- **Download Speed**: Data transfer rate indicator

### 2. Quality Level Definitions

#### 🟢 Excellent
- **Packet Loss**: 0%
- **Latency**: ≤ 150ms
- **Download Speed**: > 5 MB/s or no data (no penalty)
- **Use Case**: Ideal network connection, suitable for high-quality requirements

#### 🟡 Good
- **Packet Loss**: 0%
- **Latency**: 151ms - 250ms
- **Download Speed**: > 2 MB/s or no data (no penalty)
- **Use Case**: Usable network connection, meets general requirements

#### 🟠 Fair
- **Packet Loss**: 0.1% - 1%
- **Latency**: 251ms - 400ms
- **Download Speed**: > 1 MB/s or no data (no penalty)
- **Use Case**: Basically usable network connection, may have slight impact

#### 🔴 Poor
- **Packet Loss**: > 1%
- **Latency**: > 400ms
- **Download Speed**: ≤ 1 MB/s
- **Use Case**: Poor network connection quality, affects user experience

## Assessment Algorithm Implementation

### Core Logic
```csharp
public (string Quality, string CssClass) CalculateQuality(decimal packetLoss, decimal latency, string downloadSpeed)
{
    // 1. Packet loss hard indicator check
    if (packetLoss > 1)
        return ("Poor", "bg-danger");
    
    if (packetLoss > 0)
        return ("Fair", "bg-warning");
    
    // 2. Download speed parsing
    decimal speed = ParseDownloadSpeed(downloadSpeed);
    
    // 3. Latency evaluation
    if (latency <= 150)
    {
        // Low latency range
        if (speed > 5) return ("Excellent", "bg-success");
        if (speed > 2) return ("Good", "bg-primary");
        return ("Good", "bg-primary"); // No data, no penalty
    }
    else if (latency <= 250)
    {
        // Medium latency range
        if (speed > 2) return ("Good", "bg-primary");
        if (speed > 1) return ("Fair", "bg-warning");
        return ("Fair", "bg-warning"); // No data, no penalty
    }
    else if (latency <= 400)
    {
        // Higher latency range
        if (speed > 1) return ("Fair", "bg-warning");
        return ("Poor", "bg-danger");
    }
    else
    {
        // High latency range
        return ("Poor", "bg-danger");
    }
}
```

### Download Speed Parsing
```csharp
private decimal ParseDownloadSpeed(string speedStr)
{
    if (string.IsNullOrWhiteSpace(speedStr)) return 0;
    
    var cleanStr = speedStr.Trim();
    
    // Handle MB/s format
    if (cleanStr.EndsWith("MB/s", StringComparison.OrdinalIgnoreCase))
    {
        cleanStr = cleanStr.Substring(0, cleanStr.Length - 4).Trim();
        return decimal.TryParse(cleanStr, out var speed) ? speed : 0;
    }
    
    // Handle MB format
    if (cleanStr.EndsWith("MB", StringComparison.OrdinalIgnoreCase))
    {
        cleanStr = cleanStr.Substring(0, cleanStr.Length - 2).Trim();
        return decimal.TryParse(cleanStr, out var speed) ? speed : 0;
    }
    
    // Parse number directly
    return decimal.TryParse(cleanStr, out var directSpeed) ? directSpeed : 0;
}
```

## Display Standards

### CSS Style Classes
- **Excellent**: `bg-success` (green)
- **Good**: `bg-primary` (blue)  
- **Fair**: `bg-warning` (yellow)
- **Poor**: `bg-danger` (red)

### Display Format
```html
<span class="badge bg-success">Excellent</span>
<span class="badge bg-primary">Good</span>
<span class="badge bg-warning">Fair</span>
<span class="badge bg-danger">Poor</span>
```

## Application Scope

### Applicable Pages
1. **/Admin/Index** - Administrator dashboard page
2. **/admin/clash-template** - Clash template management page  
3. **/admin/user-config** - User configuration management page

### Applicable Components
- All Cloudflare IP preview tables
- IP quality assessment displays
- Data statistics panels

## Implementation Requirements

### 1. Consistency Requirements
- All pages must use the same assessment algorithm
- Quality level definitions must be completely consistent
- Display styles must remain unified

### 2. Data Accuracy Requirements
- Must correctly parse latency, packet loss, and download speed from CSV data
- Support multiple data formats (with/without units)
- Robust handling of abnormal data

### 3. User Experience Requirements
- Quality assessment should be intuitive and easy to understand
- Color coding should be intuitive
- Provide clear visual feedback

## Testing and Validation

### Unit Test Coverage
- Boundary value testing for quality calculation algorithm
- Accuracy testing for data parsing functionality
- Exception input handling testing

### Integration Test Coverage
- Complete page data display testing
- Multi-page consistency validation testing
- User interface interaction testing

## Maintenance Guidelines

### Algorithm Updates
- Any modifications to quality assessment standards must be updated across all related pages simultaneously
- Algorithm changes require re-running the complete test suite
- Version changes must be recorded in the changelog

### Standard Evolution
- Quality standard adjustments should be based on actual usage feedback
- New assessment dimensions should consider backward compatibility
- Standard changes require team review and approval
