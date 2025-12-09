#import <Foundation/Foundation.h>

// Check if multiset_ios framework is available - only required for on device localization
#if __has_include(<multiset_ios/main.h>)
    #import <multiset_ios/main.h>
    #define MULTISET_IOS_AVAILABLE 1
#else
    #define MULTISET_IOS_AVAILABLE 0
#endif

// MultiSet Framework wrapper - iOS plugin interface
extern "C" {

    // Simple data structures
    struct LocalizationResult {
        bool poseFound;
        float positionX;
        float positionY;
        float positionZ;
        float rotationX;
        float rotationY;
        float rotationZ;
        float rotationW;
        char mapIds[1024];
        float confidence;
    };

    // Plugin function declarations
    bool _LoadModels();
    bool _LoadMaps(const char* dbDirPath, const char* mapIdentifier);
    LocalizationResult _Localize(const char* mapCode, const char* mapSetCode, bool isRightHanded,
                                bool convertToGeoCoordinates,
                                float fx, float fy, float px, float py,
                                const char* imageData, int width, int height,
                                const char* hintMapCodes,
                                const char* hintPosition,
                                const char* geoCoordinates);
    bool _IsOnDeviceLocalizationAvailable();
}

// Implementation
extern "C" {

    bool _IsOnDeviceLocalizationAvailable() {
        #if MULTISET_IOS_AVAILABLE
            return true;
        #else
            return false;
        #endif
    }

#if MULTISET_IOS_AVAILABLE

    bool _LoadModels() {
        try {
            bool result = loadModels();
            // NSLog(@"loadModels() result: %s", result ? "SUCCESS" : "FAILED");
            return result;
        } catch (const std::exception& e) {
            NSLog(@"exception: %s", e.what());
            return false;
        } catch (...) {
            NSLog(@"unknown exception");
            return false;
        }
    }

    bool _LoadMaps(const char* dbDirPath, const char* mapIdentifier) {
        // NSLog(@"Calling loadMaps() with path: %s", dbDirPath);

        if (dbDirPath == nullptr) {
            NSLog(@"received null db dir path");
            return false;
        }

        if (mapIdentifier == nullptr) {
            NSLog(@"received null map identifier");
            return false;
        }

        try {
            std::string pathStr(dbDirPath);
            std::string mapIdentifierStr(mapIdentifier);
            bool result = loadMaps(pathStr, mapIdentifierStr);
            return result;
        } catch (const std::exception& e) {
            NSLog(@"exception: %s", e.what());
            return false;
        } catch (...) {
            NSLog(@"unknown exception");
            return false;
        }
    }

    LocalizationResult _Localize(const char* mapCode, const char* mapSetCode, bool isRightHanded,
                                bool convertToGeoCoordinates,
                                float fx, float fy, float px, float py,
                                const char* imageData, int width, int height,
                                const char* hintMapCodes,
                                const char* hintPosition,
                                const char* geoCoordinates) {

        LocalizationResult result = {}; // float hintX, float hintY, float hintZ, double geoLat, double geoLon, double geoAlt
        result.poseFound = false;

        // Validate: Either mapCode OR mapSetCode must be provided (not both empty)
        bool hasMapCode = (mapCode != nullptr && strlen(mapCode) > 0);
        bool hasMapSetCode = (mapSetCode != nullptr && strlen(mapSetCode) > 0);

        if (!hasMapCode && !hasMapSetCode) {
            return result;
        }

        if (imageData == nullptr) {
            NSLog(@"received null image data");
            return result;
        }

        try {
            // Convert const char* to int* for the library (which expects int*)
            int* imageDataInt = (int*)imageData;

            // Call the framework function with string parameters
            ImageQueryResponse response = Localize(
                mapCode,
                mapSetCode ? mapSetCode : "",
                isRightHanded,
                convertToGeoCoordinates,
                fx, fy, px, py,
                width, height,
                imageDataInt,      // int* q_image
                hintMapCodes,      // const char* (comma-separated string or empty)
                hintPosition,      // const char* (comma-separated string or empty)
                geoCoordinates     // const char* (comma-separated string or empty)
            );

            // Convert response to friendly structure
            result.poseFound = response.poseFound;

            if (response.poseFound) {
                // Convert double arrays to individual float fields
                result.positionX = (float)response.position[0];
                result.positionY = (float)response.position[1];
                result.positionZ = (float)response.position[2];
                result.rotationX = (float)response.rotation[0];
                result.rotationY = (float)response.rotation[1];
                result.rotationZ = (float)response.rotation[2];
                result.rotationW = (float)response.rotation[3];
                result.confidence = (float)response.confidence;
                strcpy(result.mapIds, response.mapIds);

            } else {
                NSLog(@"Localize FAILED!");
            }

        } catch (const std::exception& e) {
            NSLog(@"Localization exception: %s", e.what());
        } catch (...) {
            NSLog(@"Localization unknown exception");
        }
        return result;
    }

#else
    // Stub implementations when multiset_ios framework is not available

    bool _LoadModels() {
        NSLog(@"MultiSet iOS framework not available - _LoadModels() returning false");
        return false;
    }

    bool _LoadMaps(const char* dbDirPath, const char* mapIdentifier) {
        NSLog(@"MultiSet iOS framework not available - _LoadMaps() returning false");
        return false;
    }

    LocalizationResult _Localize(const char* mapCode, const char* mapSetCode, bool isRightHanded,
                                bool convertToGeoCoordinates,
                                float fx, float fy, float px, float py,
                                const char* imageData, int width, int height,
                                const char* hintMapCodes,
                                const char* hintPosition,
                                const char* geoCoordinates) {
        NSLog(@"MultiSet iOS framework not available - _Localize() returning empty result");
        LocalizationResult result = {};
        result.poseFound = false;
        return result;
    }

#endif
}
