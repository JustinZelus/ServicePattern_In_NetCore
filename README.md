# ServicePattern_In_NetCore

```
在你的web api打造以服務為架構的設計模式
Craft your own service in web api by using serivce pattern.
```
 
## 前言

```
與微服務不同，但理念同，目的為盡量做到單一責任。
```

## 說明

```
利用依賴倒置原則，配合DI，將api設計成以服務為取向，達到低耦合的結果。
在Core專案中可以看到推播服務、檔案服務、S3服務等裡面有各式接口。
剛開始建置時，建議造通用的接口，如此一來其它專案可以直接使用。例如檔案服務，內有檔案的移動、刪除、計算大小等等。
```

## 重點

```csharp
//請關注Core專案及Services專案即可。
//在startup.cs可以看到注入的好處，隨插即用。
services.AddTransient<IIOService, IOService>(o => new IOService(this._logger));
services.AddTransient<IGCMService, GCMService>();
services.AddTransient<IS3Service, S3Service>(o => new S3Service
```
