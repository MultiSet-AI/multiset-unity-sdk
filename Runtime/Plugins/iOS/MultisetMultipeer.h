#import <Foundation/Foundation.h>
#import <MultipeerConnectivity/MultipeerConnectivity.h>

@interface MultisetMultipeer : NSObject

+ (instancetype)shared;

- (void)startHostingWithName:(NSString *)displayName;
- (void)startBrowsingWithName:(NSString *)displayName;
- (void)sendData:(NSData *)data reliable:(BOOL)reliable;
- (void)disconnect;

@property (nonatomic, readonly) BOOL isConnected;
@property (nonatomic, readonly) NSInteger connectedPeerCount;

@end

#ifdef __cplusplus
extern "C" {
#endif

void _MultisetStartHosting(const char *displayName);
void _MultisetStartBrowsing(const char *displayName);
void _MultisetSendData(const void *data, int length, bool reliable);
void _MultisetDisconnect(void);
bool _MultisetIsConnected(void);
int  _MultisetConnectedPeerCount(void);

#ifdef __cplusplus
}
#endif
