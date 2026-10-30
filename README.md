# MultiSet SDK for Unity

The MultiSet SDK provides powerful localization, navigation, and AR tracking capabilities for Unity applications. This package enables developers to integrate advanced spatial computing features with minimal setup.

## ✨ What's New in 2.0.0

- **Non-LiDAR mapping**: create maps with any ARKit or ARCore phone, no depth sensor needed (`Mapping(Non-LiDAR)` sample scene)
- **Deep Search for multi-frame localization**: set `MapLocalizationManager.queryMode` to `Vps2` for better results in visually difficult scenes
- **False-positive detection**: enable `poseConsistencyCheck` to discard localization results that contradict the device's own AR tracking
- **Fixes**: access tokens renew before they expire, and the AR view is no longer rotated on iOS 27 and later

See [CHANGELOG.md](CHANGELOG.md) for the full list, including API changes.

## 📋 Prerequisites

Before getting started, ensure you have:

- **Unity 6000.3.11f1 or later** (this release is tested with 6000.3.27f1)
- **Valid MultiSet credentials** (Client ID and Client Secret)
- **Map, MapSet or Object codes** from the MultiSet Developer Portal
- **iOS/Android development tools** for mobile deployment
- **Basic Unity development experience**

## 🚀 Installation

### Git URL (Recommended)

1. Open Unity and create a new **3D project** or open your existing project
2. Navigate to **Window → Package Manager**
3. Click the **"+"** button in the top-left corner
4. Select **"Add package from git URL"**
5. Enter the following URL and click **Add**:

```
https://github.com/MultiSet-AI/multiset-unity-sdk.git
```

### Verify Installation

After installation, you should see **"MultiSet-SDK"** listed in the Package Manager under **"In Project"**.

## 📦 Dependencies

The SDK automatically installs these required packages:
- **Unity Cloud - Draco** (5.5.0)
- **Unity Cloud - glTFast** (6.15.1)
- **AR Foundation** (6.3.5)
- **Google ARCore XR Plugin** (6.3.5)
- **Apple ARKit XR Plugin** (6.3.5)
- **AI Navigation** (2.0.16)
- **Netcode for GameObjects** (2.13.3)
- **Multiplayer Center** (1.0.1)

## 🎯 Sample Scenes

The SDK includes comprehensive sample scenes to demonstrate key features.

### Import Sample Scenes

1. Open **Window → Package Manager**
2. Find **"MultiSet-SDK"** in the package list
3. Click on the package to view details
4. Navigate to the **"Samples"** tab
5. Click **"Import"** next to **"Sample Scenes"**

The samples will be imported to:
```
Assets/Samples/MultiSet-SDK/[version]/Sample Scenes/
```

### Available Sample Scenes

| Scene | Purpose | Location |
|-------|---------|----------|
| **Mapping.unity** | Scan an area and create a map on a LiDAR device | `Mapping/` |
| **Mapping(Non-LiDAR).unity** | Scan an area and create a map on any ARKit or ARCore phone | `Mapping/` |
| **Localization.unity** | Multi-frame localization against a map or mapset | `Localization/` |
| **Single Frame Localization.unity** | Single-shot localization | `Localization/` |
| **ObjectTracking.unity** | Track up to 10 objects by object code | `ObjectTracking/` |
| **Navigation.unity** | AR navigation to points of interest | `Navigation/` |
| **VPS-GPS-Navigation.unity** | GPS places you near the map, then VPS localizes you when you enter it | `VPS-GPS-Navigation/` |
| **Training.unity** | Training mode capabilities | `Training/` |
| **MapSetAlignment.unity** | Align Mapsets within Unity Editor | `AlignMapSet/` |
| **SimulationDataCapture.unity** | Simulation Data Capture | `SimulationData/` |
| **GlobalToLocal.unity** | Coordinates from global to local | `GlobalToLocal/` |
| **MultiPlayerSample.unity** | Shared AR session between nearby devices | `MultiplayerSample/` |
| **NavMeshExport.unity** | Export NavMesh data for MultiSet Wearable VPS | `NavMeshExport/` |

> **Note:** The Non-LiDAR mapping scene runs in Landscape Left; the SDK sets that orientation for you. A scan lasts from 10 seconds to 10 minutes and needs 1.5 GB of free storage.

## ⚙️ Configuration

### Step 1: Configure API Credentials

1. Navigate to the configuration file:
   ```
   Assets/Samples/MultiSet-SDK/[version]/Sample Scenes/Resources/MultiSetConfig.asset
   ```

2. Open the ScriptableObject file in the Inspector

3. Replace the placeholder values with your actual credentials:
   ```csharp
   Client Id = "YOUR_CLIENT_ID_HERE"
   Client Secret = "YOUR_CLIENT_SECRET_HERE"
   ```

### Step 2: Configure Map Settings

1. Open your desired sample scene (e.g., `Localization.unity`)

2. Select the **MapLocalizationManager** GameObject in the Hierarchy

3. In the Inspector, locate the **MapLocalizationManager** component

4. Set **Localization Type**:
   - **Map**: For single, specific locations
   - **Mapset**: For multiple related locations or larger coverage areas

5. Enter the matching code in **Map Or Mapset Code**

6. Optional settings:
   - **Query Mode**: `Vps1` is the standard engine and the fastest. `Vps2` (Deep Search) gives better accuracy and recall in visually difficult scenes, with a slightly longer response.
   - **Pose Consistency Check**: discards results that contradict the device's own AR tracking. Listen to `LocalizationFalsePositive` or `OnLocalizationFalsePositive` to tell the user.

For object tracking, open `ObjectTracking.unity`, select the **ObjectTrackingManager** GameObject and enter up to 10 codes in **Object Codes**.

> **💡 Tip:** You can obtain these codes from your MultiSet dashboard after creating maps.

### Step 3: Platform-Specific Setup

#### For iOS:
- Ensure **iOS Build Support** is installed
- Set **Target minimum iOS Version** to 16.0 or later
- Add a **Camera Usage Description** (and a **Location Usage Description** for the GPS samples) in Player Settings
- Enable **Requires ARKit support** and **Allow 'unsafe' Code**
- Enable **Apple ARKit** under **XR Plug-in Management**
- The package's iOS post-build step adds the Local Network entries the multiplayer sample needs

#### For Android:
- Ensure **Android Build Support** is installed
- Set **Minimum API Level** to 28 or higher
- Uncheck **Auto Graphics API** and use **OpenGLES3** only (Vulkan is not supported)
- Enable **Allow 'unsafe' Code**
- Enable **Google ARCore** under **XR Plug-in Management**


## 🏗️ Build and Deploy

### Build Settings

1. Navigate to **File → Build Profiles**
2. Select your target platform (**iOS** or **Android**)
3. Click **"Add Open Scenes"** to include your scene
4. Configure platform-specific settings:

#### iOS Settings:
- **Target Device**: iPhone & iPad
- **Target SDK**: Device SDK
- **Scripting Backend**: IL2CPP

#### Android Settings:
- **Scripting Backend**: IL2CPP
- **Target Architectures**: ARM64

### Testing

1. Click **"Build and Run"** to deploy to your device
2. Grant camera permissions when prompted
3. Test localization functionality in your target environment
4. Verify AR features work as expected

## 🛠️ API Reference

### Core Classes

- **`MultisetSdkManager`**: Main SDK manager and entry point; authenticates with the credentials in `MultiSetConfig`
- **`MultiSetConfig`**: Credentials and API base URL
- **`MapLocalizationManager`**: Multi-frame localization against a map or mapset
- **`SingleFrameLocalizationManager`**: Single-image localization
- **`ObjectTrackingManager`**: Tracks up to 10 objects by object code
- **`MappingManager`**: Scan an area and create a map on a LiDAR device
- **`NonLiDARMappingManager`**: Scan an area and create a map on any ARKit or ARCore phone

`NavigationController` is part of the Navigation sample scene, not the SDK. The full API reference is at [docs.multiset.ai](https://docs.multiset.ai/).


## 📱 Platform Requirements

### iOS
- **iOS 16.0+**
- **ARKit compatible device**
- **LiDAR device** (iPhone Pro or iPad Pro) for `Mapping.unity`; Non-LiDAR mapping works on any ARKit device
- **Camera access permission**

### Android
- **Android API level 28 or higher**
- **ARCore supported device**
- **Camera access permission**

## 🔍 Troubleshooting

### Common Issues

#### Package Installation Issues
- **Error**: "Package not found"
- **Solution**: Ensure Unity version is 6000.3.11f1 or later, verify Git URL is correct

#### Credential Errors
- **Error**: "Authentication failed"
- **Solution**: Verify Client ID and Secret in `MultiSetConfig.asset`

#### AR Foundation Issues
- **Error**: "AR Foundation not initialized"
- **Solution**: Ensure AR Foundation packages are properly installed and updated

#### Build Errors
- **Error**: Platform-specific compilation errors
- **Solution**: Check platform requirements, update XR settings

#### Non-LiDAR Mapping Will Not Start
- **Error**: "Not enough free space. Scanning needs at least 1.5 GB." or "Storage is almost full, so the scan was stopped."
- **Solution**: Free up at least 1.5 GB on the device before scanning

#### Localization Results Discarded
- **Error**: "False Positive detected" or "Similar area detected"
- **Solution**: Pose Consistency Check rejected a result that contradicts the device's AR tracking. Keep the camera on a mapped area and localize again; for "Similar area detected", move to a visually distinct spot.


## 📞 Support

For technical support and questions:

- **Email**: [support@multiset.ai](mailto:support@multiset.ai)
- **Documentation**: [https://docs.multiset.ai](https://docs.multiset.ai/)
- **Website**: [https://www.multiset.ai](https://www.multiset.ai)

## 📄 License

This package is proprietary software. See the LICENSE file for detailed terms and conditions.

## 🔄 Updates

To update the SDK:

1. Open **Package Manager**
2. Find **MultiSet-SDK**
3. Click **"Update"** if available
4. Or remove the package and add the Git URL again

Unity locks a Git package to the commit it installed (in `Packages/packages-lock.json`), so re-adding the URL fetches the latest version. To pin a specific version, append a tag or commit hash to the URL:
```
https://github.com/MultiSet-AI/multiset-unity-sdk.git#<tag-or-commit>
```

---

**Version**: 2.0.0
**Unity Compatibility**: 6000.3.11f1+
**Last Updated**: October 2026
