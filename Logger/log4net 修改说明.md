# log4net 修改说明
```
log4net 是 Apache 下的开源日志项目中的子项，最新版本是2.0.8.0，多年未更新;
Apache:http://logging.apache.org/log4net/index.html 
Githut:https://github.com/apache/logging-log4net

对 v2.080 版本做以下修改，重新输出版本号 v2.081
v2.080存在以下问题：
1.RemoteSyslogAppender 是ASCII
```

## 修改 RemoteSyslogAppender 不支持中文输出
```
RemoteSyslogAppender 默认是 ASCII 编码，即使外配置改为 GB2312 也不会支持中文
源码只支持 syslog RFC 3164 4.1.3 协议，对 ASCII 的支持范围在 char(32 - 126) 之间的字符
```
```C#
//部份源码 385行左右
// ... codes
for (; i < message.Length; i++)
{
   c = message[i];

   // Accept only visible ASCII characters and space. See RFC 3164 section 4.1.3
    if (((int)c >= 32) && ((int)c <= 126))
    {
        builder.Append(c);
    }
    
    // ... codes
}
```
```C#
// 修改后的代码支持中文，参考 syslog RFC 5424 协议信息格式
//RFC 5424
//SYSLOG-MSG = 优先级 版本 空格 时间戳 空格 主机名 空格 应用名 空格 进程id 空格 信息id

// PRI
builder.Append('<');
builder.Append(priority);
builder.Append('>');
// VERSION
builder.Append("1 ");
// TIMESTAMP
builder.Append(loggingEvent.TimeStamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ "));
// HOSTNAME
builder.Append(SystemInfo.HostName);
builder.Append(" ");
// APP-NAME
builder.Append(identity);
builder.Append(" ");
// PROCID
builder.Append(Process.GetCurrentProcess().Id);
builder.Append(" ");
// MSGID
builder.Append(loggingEvent.LoggerName);
builder.Append(" ");
// STRUCTURED-DATA
// no structured data, it could be whole MDC
builder.Append("- ");

// MSG
builder.Append(message);

// Grab as a byte array
buffer = this.Encoding.GetBytes(builder.ToString());

// ... codes
```

## 修改 RollingFileAppender 不支持设置最多文件数量
```
日志文件滚动保存文件数量，随时间会越来越多；增加保留最近创建的文件数量，删除其余旧的日志文件
新增配置属性 MaxReserveFileCount 保留最新的日志文件数量，配置示例：
<param name="MaxReserveFileCount" value="30"/> <!-- -1表示全部保留不删除任何文件 -->
```
```C#
// 代码修改部份
/// <summary>
/// 跟据文件创建日期，保留最近创建的文件数量，其余的删除掉
/// <para>注意：该函数是比较文件的创建日期，不是修改日期</para>
/// </summary>
/// <param name="count">要保留的数量</param>
/// <param name="path">文件目录，当前目录 "/" 表示，不可为空</param>
/// <param name="searchPattern">只在目录中(不包括子目录)，查找匹配的文件；例如："*.jpg" 或 "temp_*.png"</param>
public static void ReserveFileCount(int count, string path, string searchPattern = null)
{
   if (count < 0 || String.IsNullOrWhiteSpace(path)) throw new ArgumentException("参数错误");
            
   DirectoryInfo dir = new DirectoryInfo(path);
   FileInfo[] files = searchPattern == null ? dir.GetFiles() : dir.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);

   if (files.Length <= count) return;

   //按文件的创建时间，升序排序(最新创建的排在最前面)
    Array.Sort(files, (f1, f2) =>
    {
        return f2.CreationTime.CompareTo(f1.CreationTime);
    });

    for (int i = count; i < files.Length; i++)
    {
        files[i].Delete();
        Trace.TraceWarning("Delete File ... CreationTime:{0}\t Name:{1}", files[i].CreationTime, files[i].Name);
    }
}
   
private int m_maxReserveFileCount = -1;
public int MaxReserveFileCount
{
   get { return m_maxReserveFileCount; }
   set { m_maxReserveFileCount = value; }
}
        
override protected void OpenFile(string fileName, bool append)
{
   // ... cdoes
   if (m_maxReserveFileCount > 0)
   {
      FileInfo fi = new FileInfo(fileName);
      ReserveFileCount(m_maxReserveFileCount, fi.Directory.Name, "*" + fi.Extension);
   }
}
```
