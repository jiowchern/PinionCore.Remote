# Chat Room
## Stride Demo
[![Unity-DEMO](doc/1648914064143.jpg)](doc/stride-win-x64.7z) 
## Unity Demo  
**Regulus.Samples\Chat1\Regulus.Samples.Chat1.Unity\Assets\Project\Scenes\Chat1.unity**
**Regulus.Samples\Chat1\Regulus.Samples.Chat1.Unity2022\Assets\Project\Scenes\Chat1.unity**  
[![Unity-DEMO](doc/Snipaste_2022-03-08_18-24-25.png)](http://114.34.92.204:52000/chat1/index.html) 



---
## Run Tcp Console 

#### 1. run server
```powershell
dotnet run --project .\Regulus.Samples.Chat1.Server\ -- --tcpport 53772 --webport 0
```
> Be sure to run the server before running the client.
#### 2. run client
  
```powershell
dotnet run --project .\Regulus.Samples.Chat1.Client\
```


