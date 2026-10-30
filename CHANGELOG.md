# Changelog

All notable changes to the MultiSet SDK for Unity (`com.multiset.sdk`). Dates are YYYY-MM-DD.

## [2.0.0] - 2026-10-27
### Added
- Non-LiDAR mapping: create maps with any ARKit or ARCore phone, no depth sensor needed
  - New sample scene `Mapping/Mapping(Non-LiDAR).unity`
  - Scans run from 10 seconds to 10 minutes; save a scan as a draft and upload it later
  - The scene runs in Landscape Left, and the SDK sets that orientation for you
- Deep Search for multi-frame localization: `MapLocalizationManager.queryMode` (`Vps1` standard, the default; `Vps2` for higher accuracy and recall in visually difficult scenes, with a slightly longer response).
- False-positive detection, off by default

### Changed
- Native plugins: new iOS `MultisetOrientationCompat.mm`
- Dependencies updated: AR Foundation, ARCore and ARKit 6.3.5, Draco 5.5.0, AI Navigation 2.0.16, Netcode for GameObjects 2.13.3

### Fixed
- Expired access tokens: the SDK now renews the token before it expires
- An authentication response without a token is now reported as `AuthSuccess = false` instead of success

## [1.15.2] - 2026-07-22
### Added
- Convert map coordinates from global to local (relative to origin)

## [1.15.1] - 2026-07-01
### Added
- Fixed GeoPose field in localization response

## [1.15.0] - 2026-06-22
### Added
- Refactored the Project Structure
- Added Navigation scene scripts as part of Sample scene now
- Bug fixes

## [1.14.3] - 2026-06-10
### Added
- Bug fixes


## [1.14.2] - 2026-06-04
### Added
- Support added for adding hintFloorHeight in query


## [1.14.1] - 2026-05-22
### Added
- Support added for Portrait Mapping


## [1.14.0] - 2026-05-13
### Added
- Map Versioning added
- Added MapPointInspector to get MapSpace coordinates

## [1.11.5] - 2026-04-21
### Added
- Multiplayer sample scene added

## [1.11.4] - 2026-04-14
### Added
- Optimized multi-frame flow, fixed hintPosition, and improved Single Frame navigation.
- Navigation issue fixes

## [1.11.3] - 2026-03-26
### Added
- Updated Unity version requirement to 6000.3.11f1

## [1.11.2] - 2026-02-18
### Added
- Fixed issues with multi-object tracking
- Added the ability to retrieve a MapCode after creating a new map

## [1.11.1] - 2026-02-10
### Added
- New scene added to export NavMesh data that can be used to Multiset Wearable VPS

## [1.11.0] - 2026-02-03
### Added
- Resolved issues in Multi-Object Tracking
- Fixed multiple memory leak problems to improve stability
- Added event-based callbacks for Localization and Object Tracking responses

## [1.10.1] - 2026-01-12
### Added
- On-Device Localization Public methods added
- Modular Mapping scene
- new Mesh Animation added

## [1.10.0] - 2025-12-08
### Added
- On-Device Localization for iOS
- MapSet Alignment Scene
- ModelSet scene with ability to track multiple object
- Simulation data capture sample scene


## [1.9.3] - 2025-11-11
### Added
- draco and gltfast package updated
- fixed mapping scene issues


## [1.9.2] - 2025-09-23
### Added
- Android build issue fixed

## [1.9.1] - 2025-09-20
### Added
- Simulation support in Editor
- Download and select simulation data to localize


## [1.9.0] - 2025-09-11
### Added
- Mapping Scene Added
- Bug Fixes

## [1.8.2] - 2025-08-22
### Added
- URP support Added
- Confidence check in ModelSet tracking
- FPS drop issue fixed


## [1.8.0] - 2025-07-30
### Added
- Initial release of MultiSet SDK
- Localization and Navigation sample scenes
- Model Tracking sample scene
- Training mode scene added
