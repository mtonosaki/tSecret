*AzureAD
Add Redirect URL "msalabcdef12-1234-1234-1234-1234567890ab://auth"	"msal" + {ClientID} + "://auth"
	(Android catch intent of above scheme)

*Android Project
Manifest : ACCESS_NETWORK_STATE,  INTERNET,   USE_FINGERPRINT

*iOS Project
	Add below key-value into Info.plist in iOS project
	  <key>NSFaceIDUsageDescription</key>
	  <string>We will use FaceID to log you into tSecret.</string>


*UWP Project
