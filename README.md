# tSecret
Private Password Manager developped with Xamarin.Forms(C#) for iOS, Android and UWP projects. This manager can syncronize secret data to Azure Blob Storage.

![](https://aqtono.com/tomarika/tsecret/tSecretIcon.png)  


To use this repository, follow step below.


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
    }
}
```

Member|Remarks
-|-
KEY|Your secret key, 16 characters.
TEXTSET64|Shuffle these characters for your original base64
IVNPP|Set 16 here
AzureStorageConnectionString|Set your connection string of Azure Blob Storage



