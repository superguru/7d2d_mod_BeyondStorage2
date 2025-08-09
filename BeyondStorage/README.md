# Beyond Storage 2

## Regression Testing Checklist

### Core Storage Functionality
- [ ] **Storage Discovery**: Verify nearby storage containers (chests, crates) are detected within configured range
- [ ] **Vehicle Storage**: Test that vehicle storage is accessible when `pullFromVehicleStorage` is enabled
- [ ] **Drone Storage**: Confirm drone storage integration works with owned drones
- [ ] **Workstation Output**: Verify workstation output stacks are pulled when `pullFromWorkstationOutputs` is enabled
- [ ] **Dew Collector**: Test dew collector integration when `pullFromDewCollectors` is enabled
- [ ] **Range Limiting**: Confirm storage sources outside configured range are ignored
- [ ] **Ownership Checks**: Verify only player-owned/accessible storage is used

### Item Operations
- [ ] **Item Crafting**: Test that crafting pulls required materials from storage
- [ ] **Recipe Display**: Verify ingredient counts show storage + inventory totals
- [ ] **Max Craftable**: Confirm max craftable calculations include storage items
- [ ] **Item Repair**: Test tool/weapon repair pulls repair materials from storage
- [ ] **Item Removal**: Verify correct items are removed from storage during operations

### Block Operations  
- [ ] **Block Repair**: Test that block repairs pull required materials from storage
- [ ] **Block Upgrade**: Verify block upgrades use materials from storage
- [ ] **Block Texturing**: Test that painting/texturing operations pull paint from storage
- [ ] **Paint Counting**: Verify paint usage is accurately calculated and limited by available paint

### Vehicle Operations
- [ ] **Vehicle Repair**: Test vehicle repairs pull parts from storage
- [ ] **Vehicle Refuel**: Verify vehicle refueling uses fuel from storage
- [ ] **Fuel Type Detection**: Confirm correct fuel type is identified for each vehicle

### Weapon/Ranged Operations
- [ ] **Weapon Reload**: Test that weapon reloading pulls ammo from storage
- [ ] **Ammo Count Display**: Verify ammo counts show storage + inventory totals
- [ ] **Ammo Type Matching**: Confirm correct ammo type is used for each weapon
- [ ] **Magazine vs Individual**: Test both magazine-fed and individual round weapons

### Power Source Operations
- [ ] **Generator Refuel**: Test generator refueling pulls fuel from storage
- [ ] **Fuel Consumption**: Verify fuel is properly removed when consumed
- [ ] **Multiple Fuel Types**: Test different generator fuel types

### User Interface Integration
- [ ] **Recipe Lists**: Verify crafting UI shows updated ingredient availability
- [ ] **Ingredient Entries**: Test ingredient count displays include storage
- [ ] **Recipe Tracker**: Confirm recipe tracker shows accurate availability
- [ ] **Workstation Windows**: Test workstation UI updates when storage changes
- [ ] **HUD Elements**: Verify any HUD integrations work correctly

### Configuration Management
- [ ] **Client Config**: Test client-side configuration loading and validation
- [ ] **Server Config**: Verify server configuration sync when enabled
- [ ] **Feature Toggles**: Test each enable/disable option works correctly
- [ ] **Range Settings**: Verify range configuration (-1 for unlimited, positive values)
- [ ] **Storage Type Filters**: Test `onlyStorageCrates` setting

### Performance & Caching
- [ ] **Cache Invalidation**: Test cache clears appropriately when storage changes
- [ ] **Context Expiration**: Verify storage contexts expire and refresh correctly
- [ ] **Performance Profiling**: Check that performance tracking works without errors
- [ ] **Memory Usage**: Monitor for memory leaks during extended play

### Multiplayer Compatibility
- [ ] **Server-Client Sync**: Test functionality works in multiplayer
- [ ] **Permission Checks**: Verify players can only access their own storage
- [ ] **Network Performance**: Check for excessive network traffic
- [ ] **Concurrent Access**: Test multiple players using storage simultaneously

### Error Handling & Edge Cases
- [ ] **Null Reference Prevention**: Verify no null reference exceptions occur
- [ ] **Invalid Items**: Test handling of corrupted/invalid items
- [ ] **Missing Storage**: Confirm graceful handling when storage is removed
- [ ] **World Loading**: Test functionality during world load/save operations
- [ ] **Mod Conflicts**: Verify compatibility with other common mods

### Validation & Data Integrity
- [ ] **Item Stack Validation**: Test that invalid item stacks are properly handled
- [ ] **Context Validation**: Verify storage context validation works correctly
- [ ] **Feature Flag Validation**: Test that disabled features are properly ignored
- [ ] **Data Store Integrity**: Confirm storage data remains consistent

### Logging & Debugging
- [ ] **Debug Logging**: Verify debug logs provide useful information when enabled
- [ ] **Error Logging**: Test that errors are properly logged with context
- [ ] **Performance Metrics**: Check performance profiler output is accurate
- [ ] **Code Quality**: Run code quality checker and verify clean results

### Regression-Specific Tests
- [ ] **Previous Bug Fixes**: Re-test any previously fixed bugs to ensure no regression
- [ ] **API Changes**: Verify all public method signatures remain compatible
- [ ] **Configuration Compatibility**: Test that old config files still work
- [ ] **Save Game Compatibility**: Confirm existing save games load without issues

