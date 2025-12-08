#include <stdbool.h>
#include <stddef.h>

bool _LoadModels() { return false; }
bool _LoadMaps(const char* dbDirPath, const char* mapIdentifier) { return false; }

typedef struct {
  bool poseFound;
  float positionX, positionY, positionZ;
  float rotationX, rotationY, rotationZ, rotationW;
  int num_matches;
  char mapIds[1024];
  float confidence;
} LocalizationResult;

LocalizationResult _Localize(
  const char* mapCode, const char* mapSetCode,
  bool isRightHanded, bool convertToGeoCoordinates,
  float fx, float fy, float px, float py,
  void* imageData, int width, int height,
  const char* hintMapCodes, const char* hintPosition, const char* geoCoordinates) {
  LocalizationResult result = {0};
  return result;
}