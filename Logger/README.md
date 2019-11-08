# 日志基本规范
***

参考：https://blog.csdn.net/lk142500/article/details/80424945

## 概述
```
一切无法追溯的错误，无法查找的根源，原因就是没有日志
养成良好的日志撰写习惯，并且应在实际的开发工作中为写日志预留时间

程序运行之后，一旦发生异常，第一件事就应该弄清楚当时发生了什么，
用户当时做了什么操作，环境有无影响，数据有什么变化，是不是反复发生等，
然后再进一步的确定大致是哪个方面的问题，日志就给我们提供了信息。
```

## 基本原则
* ##### 不影响程序正常运行，不影响程序运行性能
* ##### 不产生程序安全问题，不输出敏感信息
* ##### 可追踪程序执行的过程，快速定位问题的根源 
* ##### 可供监控/维护人员分析，回溯上下文场景重现
* ##### 可配置日志输出级别，不输出无意义信息

### 基本要求
* ##### [强制] 可外部配置日志输出
* ##### [强制] 支持格式定义配置
* ##### [强制] 支持系统输出
* ##### [强制] 支持本地文件存储
* ##### [强制] 支持 UDP 输出
* ##### [建议] 支持 Syslog RFC5424 格式
* ##### [建议] 支持邮件输出
* ##### [建议] 支持数据库输出
* #### 硬件日志信息特殊处理

## 日志级别
```
OFF > FATAL(致命错误) > ERROR(一般错误) > WARN(警告信息) > INFO(一般信息) > DEBUG(调试信息) > TRACE(跟踪信息) > ALL 
```
* #### FATAL [致命错误]
```
出现严重错误、影响程序运行、影响业务演示、严重影响程序性能。例如：出现死循环，业务逻辑错误，内存溢出等。
```
* #### ERROR [一般错误]
```
程序出现错误，能继续正常运行，但影响到业务演示体验，部分功能出现混乱，或功能失效的错误。
例如，操作失败，函数调用失败，捕获异常等。
```
*  #### WARN [警告信息]
```
不影响程序继续运行，但不符合运行正常条件，参数不在预期的运行范围内，有可能引起程序运行错误的信息。
例如：用户输入错误，未按流程操作等，配置参数错误(但存在默认值)。
```
* #### INFO [一般信息]
```
在运行中应该让用户知道的基本信息、关键信息、关键运行指标等；
开发人员可以将初始化配置/参数、业务状态变化信息，或者业务流程中的核心处理记录到INFO日志中；
为方便维护，或后期回溯时上下文场景复现。例如：功能启动/停止，业务运行状态/流程位置/业务参数等。
```
* #### DEBUG/TRACE [调试/跟踪信息]
```
记录系统用于调试的一切信息，内容或者是一些关键数据内容的输出
例如：参数信息，调试细节信息，返回值信息等等，主要用于开发过程中输出的信息。
```

## 日志格式 [提高可读性、统一性]
```
[logger]
Logger-MSG = [Timestamp] SP [LogLevel] SP [Process/Thread ID] SP [LogSrc] SP [Model/Func Name (line)] SP - SP Msg
Logger-MSG = 时间戳 空格 日志级别 空格 进程/线程ID 空格 日志源名称 空格 模块/函数名称(行数位置) 空格 - 空格 信息

[Syslog RFC5424] 
SYSLOG-MSG = <PRI>Version SP Timestamp SP HostName SP APPName SP ProcID SP MsgID
SYSLOG-MSG = <PRI>版本号 空格 时间戳 空格 主机名 空格 应用名 空格 进程id 空格 信息id
```

## 日志规则
* #### 建议使用库 [推荐]
|Language|Library|
|-----|-----|
|Java|log4j
|C#|log4net
|C++|log4cplus
|AS|...
|硬件|...

* #### 禁用系统输出
```
禁用开发语言自带的输出，例如：
```
```C#
//C#
Console.WriteLine("Hello");
```
```C++
//C++
printf("Hello");
cout << "Hello" << endl;
```

* #### if..else判断
```
对于 else 是非正常的情况，需要根据情况选择打印 warn 或 error 日志。
对于只有 if 没有 else 的地方，如果 else 的路径是不可能的，应当加上 else 语句，并打印 error 日志。
```
```C#
if(true)
{
  // ...
}else{
 Log.Error("这是不可能出现 false , 但也在输出信息保留");
}
```

* #### try..catch捕获
```
catch中的异常记录必须打印堆栈信息
无论是否发生异常，都不要在不同地方重复记录针对同一事件的日志消息
不要日志后又抛出异常，因为这样会多次记录日志，只允许记录一次日志，但可以弹出提示框，或强制退出
```
```C#
try{
 // ...
}catch(Exception e){
 Log.Error("Exception:{0}", e.Message); //错误
 throw e; //错误
 
 Log.Error("Exception:{0}", e);  //正确
 MessageBox("xxxx");
 //Or
 //Exit(0);
}
```

* #### 重要函数出入口
```
建议记录方法调用、入参、返回值，对于排查问题会有很大帮助；但级别不得高于 INFO 级，建议使用小于 DEBUG 级别
```
```C#
public void Start()
{
  // ... codes
  if(Log.isInfoEnabled()) Log.Info("start ...");  //正确
}
protected void Analyse(String str)
{
  if(Log.isDebugEnabled())  Log.Debug("analyse::{0}", str); //正确
}
```


* #### 注意 示例
```C#
// 字符拼接，建议使用占位符
Log.Info("a:" + b); //不建议
Log.Info("a:{0}", b); //正确

// 低于 INFO 级别必需做判断
Log.Debug("debug message"); //绝对不可以
if(Log.isInfoEnabled()) Log.Info("info message"); //正确
if(Log.isDebugEnabled()) Log.Debug("debug message"); //正确

//循环体内不要打印 INFO 级别日志
for(int i = 0; i < 100; i ++)
{
 if(Log.isInfoEnabled()) Log.Info("info message::{0}", i); //不建议
 if(Log.isDebugEnabled()) Log.Debug("debug message::{0}", i);//正确
 if(Log.isTraceEnabled()) Log.Trace("trace message::{0}", i);//正确
}

//打印日志的代码任何情况下都不允许失败
int a = 10;
Log.Info("a:{0}", b);  //错误，输出的信息绝对不可出错

//不输出无意义信息
Log.Info("========================"); //不建议，没意义
Log.Info("++++++++++++++++++++++++"); //不建议，没意义

```
