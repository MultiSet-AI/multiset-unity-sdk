// BackgroundUploader.h
#import <Foundation/Foundation.h>

@interface BackgroundUploader : NSObject

+ (instancetype)sharedInstance;
- (int)startBackgroundUpload:(NSString *)filePath toURL:(NSString *)uploadURL contentType:(NSString *)contentType;
- (float)getUploadProgress:(int)uploadId;
- (void)cancelUpload:(int)uploadId;
- (void)setupCompletionCallback:(void (*)(int, BOOL, const char *))callback;

@end