# xcore
Wlniao.Aliyun for Wlniao.XCore

## License
![GitHub License](https://img.shields.io/github/license/wlniao/xcore)  
![Package Version](https://img.shields.io/nuget/v/Wlniao.Aliyun) 
![Pull Count](https://img.shields.io/nuget/dt/Wlniao.Aliyun) 

### Add Package
```
dotnet add package Wlniao.Aliyun -v 8.0.0
```

### SLS日志服务
设置Logger实例
```
Wlniao.Log.Loger.SetLogger(new Wlniao.Aliyun.AliyunLoger(Log.LogLevel.Debug));
```
配置Aliyun参数
```
WLN_LOG_SERVER=日志服务服务接入点
WLN_LOG_SLS_PROJECT=日志项目名称
WLN_LOG_SLS_STORE=日志存储库名称
WLN_LOG_KEYID=阿里云AccessKey ID
WLN_LOG_KEYSECRET=阿里云AccessKey Secret
```