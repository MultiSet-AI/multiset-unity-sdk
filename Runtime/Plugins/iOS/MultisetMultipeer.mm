#import "MultisetMultipeer.h"
extern void UnitySendMessage(const char *obj, const char *method, const char *msg);
static NSString *const kServiceType = @"multiset-sdk";
static NSString *const kGameObjectName = @"MultisetMultipeerReceiver";
@interface MultisetMultipeer () <MCSessionDelegate, MCNearbyServiceAdvertiserDelegate, MCNearbyServiceBrowserDelegate>
@property (nonatomic, strong) MCPeerID *myPeerID;
@property (nonatomic, strong) MCSession *session;
@property (nonatomic, strong) MCNearbyServiceAdvertiser *advertiser;
@property (nonatomic, strong) MCNearbyServiceBrowser *browser;
@end
@implementation MultisetMultipeer
+ (instancetype)shared {
    static MultisetMultipeer *instance = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        instance = [[MultisetMultipeer alloc] init];
    });
    return instance;
}
- (void)setupWithName:(NSString *)displayName {
    self.myPeerID = [[MCPeerID alloc] initWithDisplayName:displayName];
    self.session = [[MCSession alloc] initWithPeer:self.myPeerID
                                   securityIdentity:nil
                               encryptionPreference:MCEncryptionNone];
    self.session.delegate = self;
}
- (void)startHostingWithName:(NSString *)displayName {
    [self setupWithName:displayName];
    self.advertiser = [[MCNearbyServiceAdvertiser alloc] initWithPeer:self.myPeerID
                                                        discoveryInfo:nil
                                                          serviceType:kServiceType];
    self.advertiser.delegate = self;
    [self.advertiser startAdvertisingPeer];
    NSLog(@"MultisetMPC >> Started hosting as %@", displayName);
}
- (void)startBrowsingWithName:(NSString *)displayName {
    [self setupWithName:displayName];
    self.browser = [[MCNearbyServiceBrowser alloc] initWithPeer:self.myPeerID
                                                    serviceType:kServiceType];
    self.browser.delegate = self;
    [self.browser startBrowsingForPeers];
    NSLog(@"MultisetMPC >> Started browsing as %@", displayName);
}
- (void)sendData:(NSData *)data reliable:(BOOL)reliable {
    if (self.session.connectedPeers.count == 0) return;

    MCSessionSendDataMode mode = reliable ? MCSessionSendDataReliable : MCSessionSendDataUnreliable;
    NSError *error = nil;
    [self.session sendData:data
                   toPeers:self.session.connectedPeers
                  withMode:mode
                     error:&error];
    if (error) {
        NSLog(@"MultisetMPC >> Send error: %@", error.localizedDescription);
    }
}
- (void)disconnect {
    [self.advertiser stopAdvertisingPeer];
    [self.browser stopBrowsingForPeers];
    [self.session disconnect];
    NSLog(@"MultisetMPC >> Disconnected");
}
- (BOOL)isConnected {
    return self.session.connectedPeers.count > 0;
}

- (NSInteger)connectedPeerCount {
    return self.session.connectedPeers.count;
}

#pragma mark - MCSessionDelegate

- (void)session:(MCSession *)session peer:(MCPeerID *)peerID didChangeState:(MCSessionState)state {
    NSString *stateStr = @"unknown";
    if (state == MCSessionStateConnected) stateStr = @"connected";
    else if (state == MCSessionStateNotConnected) stateStr = @"disconnected";
    else if (state == MCSessionStateConnecting) stateStr = @"connecting";

    NSLog(@"MultisetMPC >> Peer %@ state: %@", peerID.displayName, stateStr);

    NSString *json = [NSString stringWithFormat:@"{\"peer\":\"%@\",\"state\":\"%@\"}",
                      peerID.displayName, stateStr];
    UnitySendMessage([kGameObjectName UTF8String], "OnPeerStateChanged", [json UTF8String]);

    if (state == MCSessionStateConnected) {
        UnitySendMessage([kGameObjectName UTF8String], "OnPeerConnected", [peerID.displayName UTF8String]);
    }
}

- (void)session:(MCSession *)session didReceiveData:(NSData *)data fromPeer:(MCPeerID *)peerID {
    NSString *jsonString = [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
    if (jsonString) {
        UnitySendMessage([kGameObjectName UTF8String], "OnDataReceived", [jsonString UTF8String]);
    }
}

- (void)session:(MCSession *)session didReceiveStream:(NSInputStream *)stream
       withName:(NSString *)streamName fromPeer:(MCPeerID *)peerID {}
- (void)session:(MCSession *)session didStartReceivingResourceWithName:(NSString *)resourceName
       fromPeer:(MCPeerID *)peerID withProgress:(NSProgress *)progress {}
- (void)session:(MCSession *)session didFinishReceivingResourceWithName:(NSString *)resourceName
       fromPeer:(MCPeerID *)peerID atURL:(NSURL *)localURL withError:(NSError *)error {}

#pragma mark - MCNearbyServiceAdvertiserDelegate

- (void)advertiser:(MCNearbyServiceAdvertiser *)advertiser
didReceiveInvitationFromPeer:(MCPeerID *)peerID
       withContext:(NSData *)context
 invitationHandler:(void (^)(BOOL, MCSession *))invitationHandler {
    NSLog(@"MultisetMPC >> Accepting invitation from %@", peerID.displayName);
    invitationHandler(YES, self.session);
}

- (void)advertiser:(MCNearbyServiceAdvertiser *)advertiser
didNotStartAdvertisingPeer:(NSError *)error {
    NSLog(@"MultisetMPC >> Advertise failed: %@", error.localizedDescription);
}

#pragma mark - MCNearbyServiceBrowserDelegate

- (void)browser:(MCNearbyServiceBrowser *)browser
      foundPeer:(MCPeerID *)peerID
withDiscoveryInfo:(NSDictionary<NSString *,NSString *> *)info {
    NSLog(@"MultisetMPC >> Found peer: %@", peerID.displayName);
    [browser invitePeer:peerID toSession:self.session withContext:nil timeout:10];
}

- (void)browser:(MCNearbyServiceBrowser *)browser lostPeer:(MCPeerID *)peerID {
    NSLog(@"MultisetMPC >> Lost peer: %@", peerID.displayName);
}

- (void)browser:(MCNearbyServiceBrowser *)browser
didNotStartBrowsingForPeers:(NSError *)error {
    NSLog(@"MultisetMPC >> Browse failed: %@", error.localizedDescription);
}

@end

#pragma mark - C Bridge Functions

extern "C" {

void _MultisetStartHosting(const char *displayName) {
    [[MultisetMultipeer shared] startHostingWithName:[NSString stringWithUTF8String:displayName]];
}

void _MultisetStartBrowsing(const char *displayName) {
    [[MultisetMultipeer shared] startBrowsingWithName:[NSString stringWithUTF8String:displayName]];
}

void _MultisetSendData(const void *data, int length, bool reliable) {
    NSData *nsData = [NSData dataWithBytes:data length:length];
    [[MultisetMultipeer shared] sendData:nsData reliable:reliable];
}

void _MultisetDisconnect() {
    [[MultisetMultipeer shared] disconnect];
}

bool _MultisetIsConnected() {
    return [[MultisetMultipeer shared] isConnected];
}

int _MultisetConnectedPeerCount() {
    return (int)[[MultisetMultipeer shared] connectedPeerCount];
}

}
