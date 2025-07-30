# MultiSet SDK for Unity

The MultiSet SDK provides powerful localization, navigation, and AR tracking capabilities for Unity applications. This package enables developers to integrate advanced spatial computing features with minimal setup.

## 📋 Prerequisites

Before getting started, ensure you have:

- **Unity 6000.0.36f1 or later** (minimum: Unity 2022.3.36+)
- **Valid MultiSet credentials** (Client ID and Client Secret)
- **Map/MapSet/ModelSet codes** from your MultiSet dashboard
- **iOS/Android development tools** for mobile deployment
- **Basic Unity development experience**

## 🚀 Installation

### Method 1: Git URL (Recommended)

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
- **Unity Cloud - Draco** (5.1.7)
- **Unity Cloud - glTFast** (6.8.0) 
- **AR Foundation** (6.0.3)
- **AI Navigation** (2.0.5)

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
| **Localization.unity** | Basic localization functionality | `Localization/` |
| **Single Frame Localization.unity** | Single-shot localization | `Localization/` |
| **ModelSetTracking.unity** | 3D model tracking | `ModelSetTracking/` |
| **Navigation.unity** | AR navigation features | `Navigation/` |
| **Training.unity** | Training mode capabilities | `Training/` |

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

2. Select the **MultisetSdkManager** GameObject in the Hierarchy

3. In the Inspector, locate the **MapLocalizationManager** component

4. Configure your localization method:
   - **Map**: For single, specific locations
   - **MapSet**: For multiple related locations or larger coverage areas
   - **ModelSet**: For 3D model tracking scenarios in **ModelTrackingManager**

5. Enter your corresponding code:
   - **Map Code**: For individual location mapping
   - **MapSet Code**: For grouped location sets
   - **ModelSet Code**: For 3D model tracking

> **💡 Tip:** You can obtain these codes from your MultiSet dashboard after creating maps.

### Step 3: Platform-Specific Setup

#### For iOS:
- Ensure **iOS Build Support** is installed
- Set **Target SDK** to iOS 12.0 or later
- Enable **Camera Usage Description** in Player Settings
- ARKit XR-Plugin Provider must be enabled.

#### For Android:
- Ensure **Android Build Support** is installed
- Set **Minimum API Level** to 26 or higher
- ARCore XR-Plugin Provider must be enabled


## 🏗️ Build and Deploy

### Build Settings

1. Navigate to **File → Build Settings**
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

- **`MultiSetSdkManager`**: Main SDK manager and entry point
- **`MapLocalizationManager`**: Handles localization operations
- **`NavigationController`**: Manages AR navigation features
- **`ModelSetTrackingManager`**: Controls 3D model tracking


## 📱 Platform Requirements

### iOS
- **iOS 13.0+**
- **ARKit compatible device** (iPhone 10x or newer)
- **Camera access permission**

### Android
- **Android API level 26 or higher**
- **ARCore supported device**
- **Camera access permission**

## 🔍 Troubleshooting

### Common Issues

#### Package Installation Issues
- **Error**: "Package not found"
- **Solution**: Ensure Unity version is 6000.0.36f1 or later, verify Git URL is correct

#### Credential Errors
- **Error**: "Authentication failed" 
- **Solution**: Verify Client ID and Secret in `MultiSetConfig.asset`

#### AR Foundation Issues
- **Error**: "AR Foundation not initialized"
- **Solution**: Ensure AR Foundation packages are properly installed and updated

#### Build Errors
- **Error**: Platform-specific compilation errors
- **Solution**: Check platform requirements, update XR settings


## 📞 Support

For technical support and questions:

- **Email**: [support@multiset.ai](mailto:support@multiset.ai)
- **Documentation**: [https://www.multiset.ai/docs](https://docs.multiset.ai/)
- **Website**: [https://www.multiset.ai](https://www.multiset.ai)

## 📄 License

This package is proprietary software. See the LICENSE file for detailed terms and conditions.

## 🔄 Updates

To update the SDK:

1. Open **Package Manager**
2. Find **MultiSet-SDK** 
3. Click **"Update"** if available
4. Or remove and re-add with latest Git URL

For manual updates, use:
```
https://github.com/MultiSet-AI/multiset-unity-sdk.git#latest
```

---

**Version**: 1.8.0  
**Unity Compatibility**: 6000.0.36f1+  
**Last Updated**: July 2025
