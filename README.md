# tSecret Version 2.0
Private Password Manager developped with Xamarin.Forms(C#) for iOS, Android and UWP projects. This manager can syncronize secret data to Azure Blob Storage. To specify user account, tSecret use Azure Active Directory authentication.

![](https://aqtono.com/tomarika/tsecret/tSecretIcon.png)  


## To use this repository, follow step below.


1. Clone this repository to your local environment.
1. Make MySecretParameter.cs in Xamarin Common folder(tSecret/tSecret)
1. Create a partial class MySecretParameter like below sample.

```C#
namespace tSecret
{
    public partial class MySecretParameter : SecretParameterBase
    {
        public override string KEY => "1234567890123456";
        public override string TEXTSET64 => "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz/+";
        public override int IVNPP => 16;
        public override string AzureStorageConnectionString => "DefaultEndpointsProtocol=https;AccountName=XXXXXXX;AccountKey=XXXXXX/XXX+XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX/XXXXXXXXXXXXXXXXX==;EndpointSuffix=core.windows.net";
        public override string IosKeychainSecurityGroups => "com.yourappname.tsecret";
        public override string AzureADClientId => "abcdef12-abcd-cdef-0123-1234567890ab";
        public const string RedirectUrlScheme = "msalabcdef12-abcd-cdef-0123-1234567890ab";
    }
}
```

Member|Remarks
-|-
KEY|Your secret key, 16 characters.
TEXTSET64|Shuffle these characters for your original base64
IVNPP|Set 16 here
AzureStorageConnectionString|Set your connection string of Azure Blob Storage
IosKeychainSecurityGroups|Set iOS key-chain group name
AzureADClientId|Set Azure Active Directory Client ID (Application ID)
RedirectUrlScheme|for Android Intent URL scheme name formatted "msal" + AzureADClientId

.  


## Azure Active Directory settings

1. App Registration  
Go to "Azure Portal" --> "Active Directory Tenant" --> App registrations
![](https://aqtono.com/tomarika/tsecret/ad01.png)  
Input application name and register.  
![](https://aqtono.com/tomarika/tsecret/ad02.png)  
1. Configure platform  
Select "Authentication" page then add new platform as "Mobile and desktop applications".  
![](https://aqtono.com/tomarika/tsecret/ad03.png)  
...Need two redirect URLs.  
![](https://aqtono.com/tomarika/tsecret/ad04.png)  
1. Get Client ID for your **MySecretParameter.AzureADClientId** value.
![](https://aqtono.com/tomarika/tsecret/ad05.png)  



