# GroupDocs.Viewer .NET AmazonS3

[Amazon S3](https://aws.amazon.com/s3/) IInputDataHandler, ICacheDataHandler, IFileDataStore provider for [GroupDocs.Viewer for .NET](https://www.nuget.org/packages/groupdocs-viewer-dotnet/)
 which allows you to keep files and cache in the cloud. 

## Installation & Configuration

Install via [nuget.org](http://nuget.org)

```powershell
Install-Package groupdocs-viewer-dotnet-amazons3
```

Add "BucketName" to your AppSettings in app.config or web.config

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <appSettings>
    <add key="BucketName" value="your-bucket-name" />
  </appSettings>
</configuration>
```

If you're hosting your project inside EC2 instance IAM access keys already exist inside instance via environment variables.
Please check [AWS Access Keys best practices article](http://docs.aws.amazon.com/general/latest/gr/aws-access-keys-best-practices.html) for more 
information about keeping your access keys sucure. 

For the test purposes you can add [IAM access keys](http://docs.aws.amazon.com/IAM/latest/UserGuide/ManagingCredentials.html) to your AppSettings in app.config or web.config.
(Do not commit your access keys to the source control).

## How to use

```csharp
var viewerConfig = new ViewerConfig
{
    StoragePath = "/storage-path",
    CachePath = "/storage-path/cache",
    UseCache = true
};

var amazonS3Config = new AmazonS3Config {RegionEndpoint = RegionEndpoint.Your-Region-Endpoint};
var amazonS3Client = new AmazonS3Client(config);

var inputDataHandler = new InputDataHandler(viewerConfig, amazonS3Client);
var cacheDataHandler = new CacheDataHandler(viewerConfig, amazonS3Client);
var fileDataStore = new FileDataStore(viewerConfig, amazonS3Client);

var viewerHtmlHandler = new ViewerHtmlHandler(viewerConfig, inputDataHandler, cacheDataHandler, fileDataStore);

var pages = viewerHtmlHandler.GetPages("your-document.docx");
```


## License

GroupDocs.Viewer .NET AmazonS3 is Open Source software released under the [MIT license](https://github.com/harumburum/groupdocs-viewer-net-amazons3/blob/master/LICENSE.md).