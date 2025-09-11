// BackgroundUploader.mm
#import "BackgroundUploader.h"

@interface BackgroundUploader () <NSURLSessionTaskDelegate, NSURLSessionDelegate>
@property (nonatomic, strong) NSURLSession *backgroundSession;
@property (nonatomic, strong) NSMutableDictionary<NSNumber *, NSURLSessionUploadTask *> *activeTasks;
@property (nonatomic, strong) NSMutableDictionary<NSNumber *, NSProgress *> *taskProgress;
@property (nonatomic, assign) int nextUploadId;
@property (nonatomic, assign) void (*completionCallback)(int, BOOL, const char *);
@end

@implementation BackgroundUploader

+ (instancetype)sharedInstance {
    static BackgroundUploader *instance = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        instance = [[BackgroundUploader alloc] init];
    });
    return instance;
}

- (instancetype)init {
    if (self = [super init]) {
        NSURLSessionConfiguration *config = [NSURLSessionConfiguration backgroundSessionConfigurationWithIdentifier:@"com.yourcompany.app.backgrounduploader"];
        config.sessionSendsLaunchEvents = YES;
        config.discretionary = NO;
        
        self.backgroundSession = [NSURLSession sessionWithConfiguration:config delegate:self delegateQueue:nil];
        self.activeTasks = [NSMutableDictionary new];
        self.taskProgress = [NSMutableDictionary new];
        self.nextUploadId = 1;
        self.completionCallback = NULL;
    }
    return self;
}

- (void)setupCompletionCallback:(void (*)(int, BOOL, const char *))callback {
    self.completionCallback = callback;
}

- (int)startBackgroundUpload:(NSString *)filePath toURL:(NSString *)uploadURL contentType:(NSString *)contentType {
    NSURL *fileURL = [NSURL fileURLWithPath:filePath];
    NSURL *url = [NSURL URLWithString:uploadURL];
    
    if (![[NSFileManager defaultManager] fileExistsAtPath:filePath]) {
        NSLog(@"File does not exist at path: %@", filePath);
        return -1;
    }
    
    NSMutableURLRequest *request = [NSMutableURLRequest requestWithURL:url];
    [request setHTTPMethod:@"PUT"];
    [request setValue:contentType forHTTPHeaderField:@"Content-Type"];
    
    NSURLSessionUploadTask *uploadTask = [self.backgroundSession uploadTaskWithRequest:request fromFile:fileURL];
    
    int uploadId = self.nextUploadId++;
    self.activeTasks[@(uploadId)] = uploadTask;
    
    NSProgress *progress = [NSProgress progressWithTotalUnitCount:100];
    self.taskProgress[@(uploadId)] = progress;
    
    [uploadTask resume];
    
    return uploadId;
}

- (float)getUploadProgress:(int)uploadId {
    NSProgress *progress = self.taskProgress[@(uploadId)];
    if (progress) {
        return progress.fractionCompleted;
    }
    return 0.0;
}

- (void)cancelUpload:(int)uploadId {
    NSURLSessionUploadTask *task = self.activeTasks[@(uploadId)];
    if (task) {
        [task cancel];
        [self.activeTasks removeObjectForKey:@(uploadId)];
        [self.taskProgress removeObjectForKey:@(uploadId)];
    }
}

#pragma mark - NSURLSessionTaskDelegate

- (void)URLSession:(NSURLSession *)session task:(NSURLSessionTask *)task didSendBodyData:(int64_t)bytesSent totalBytesSent:(int64_t)totalBytesSent totalBytesExpectedToSend:(int64_t)totalBytesExpectedToSend {
    
    // Find the upload ID for this task
    __block NSNumber *uploadId = nil;
    [self.activeTasks enumerateKeysAndObjectsUsingBlock:^(NSNumber * _Nonnull key, NSURLSessionUploadTask * _Nonnull obj, BOOL * _Nonnull stop) {
        if (obj == task) {
            uploadId = key;
            *stop = YES;
        }
    }];
    
    if (uploadId) {
        NSProgress *progress = self.taskProgress[uploadId];
        if (progress) {
            float fractionCompleted = (float)totalBytesSent / (float)totalBytesExpectedToSend;
            progress.completedUnitCount = fractionCompleted * 100;
        }
    }
}

- (void)URLSession:(NSURLSession *)session task:(NSURLSessionTask *)task didCompleteWithError:(NSError *)error {
    
    // Find the upload ID for this task
    __block NSNumber *uploadId = nil;
    [self.activeTasks enumerateKeysAndObjectsUsingBlock:^(NSNumber * _Nonnull key, NSURLSessionUploadTask * _Nonnull obj, BOOL * _Nonnull stop) {
        if (obj == task) {
            uploadId = key;
            *stop = YES;
        }
    }];
    
    if (!uploadId) {
        return;
    }
    
    BOOL success = (error == nil && ((NSHTTPURLResponse *)task.response).statusCode >= 200 && ((NSHTTPURLResponse *)task.response).statusCode < 300);
    
    // Call the Unity callback
    if (self.completionCallback != NULL) {
        NSString *errorMessage = error ? [error localizedDescription] : @"";
        const char *errorCStr = [errorMessage UTF8String];
        self.completionCallback([uploadId intValue], success, errorCStr);
    }
    
    // Clean up
    [self.activeTasks removeObjectForKey:uploadId];
    [self.taskProgress removeObjectForKey:uploadId];
}

@end

// Unity Plugin Interface
extern "C" {
    int StartBackgroundUpload(const char* filePath, const char* uploadUrl, const char* contentType) {
        NSString *filePathStr = [NSString stringWithUTF8String:filePath];
        NSString *uploadUrlStr = [NSString stringWithUTF8String:uploadUrl];
        NSString *contentTypeStr = [NSString stringWithUTF8String:contentType];
        
        return [[BackgroundUploader sharedInstance] startBackgroundUpload:filePathStr 
                                                                    toURL:uploadUrlStr 
                                                              contentType:contentTypeStr];
    }
    
    float GetUploadProgress(int uploadId) {
        return [[BackgroundUploader sharedInstance] getUploadProgress:uploadId];
    }
    
    void CancelUpload(int uploadId) {
        [[BackgroundUploader sharedInstance] cancelUpload:uploadId];
    }
    
    void SetupCompletionCallback(void (*callback)(int, BOOL, const char *)) {
        [[BackgroundUploader sharedInstance] setupCompletionCallback:callback];
    }
}