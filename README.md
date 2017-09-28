# GroupDocs.Viewer .NET AmazonS3

[Amazon S3](https://aws.amazon.com/s3/) IInputDataHandler and ICacheDataHandler provider for [GroupDocs.Viewer for .NET](https://www.nuget.org/packages/groupdocs-viewer-dotnet/)
 which allows you to keep files and cache in the cloud. 

## Installation & Configuration

Install via [nuget.org](http://nuget.org)

```powershell
Install-Package groupdocs-viewer-dotnet-amazons3
```

If you're hosting your project inside EC2 instance IAM access keys already exist inside instance via environment variables.
Please check [AWS Access Keys best practices article](http://docs.aws.amazon.com/general/latest/gr/aws-access-keys-best-practices.html) for more 
information about keeping your access keys sucure. 

For the test purposes you can add [IAM access keys](http://docs.aws.amazon.com/IAM/latest/UserGuide/ManagingCredentials.html) to your AppSettings in app.config or web.config.
(Do not commit your access keys to the source control).

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <appSettings>
    <add key="AWSAccessKey" value="***"/>
    <add key="AWSSecretKey" value="***"/>
    <add key="AWSRegion" value="us-west-2" />
  </appSettings>
</configuration>
```

## How to use

```csharp

var amazonS3Config = new AmazonS3Config { RegionEndpoint = RegionEndpoint.USWest2 };
var amazonS3Client = new AmazonS3Client(amazonS3Config);
var amazonBucketName = "my-bucket";

var amazonS3FileManager = new AmazonS3FileManager(amazonS3Client, amazonBucketName);

var viewerDataHandler = new ViewerDataHandler(amazonS3FileManager);

var viewerConfig = new ViewerConfig
{
    UseCache = true
};

var viewerHtmlHandler = new ViewerHtmlHandler(viewerConfig, viewerDataHandler, viewerDataHandler);

var pages = viewerHtmlHandler.GetPages("document.docx");
```


## License

GroupDocs.Viewer .NET AmazonS3 is Open Source software released under the [MIT license](https://github.com/harumburum/groupdocs-viewer-net-amazons3/blob/master/LICENSE.md).