# log4net 修改说明
```
log4net 是 Apache 下的开源日志项目中的子项，最新版本是2.0.8.0，多年未更新;
Apache:http://logging.apache.org/log4net/index.html 
Githut:https://github.com/apache/logging-log4net

对 v2.080 版本做以下修改，重新输出版本号 v2.081
v2.080存在以下问题：
1.修改 RemoteSyslogAppender 不支持中文，协议版本低的问题
2.修改 RollingFileAppender 不支持设置保留最多文件数量
```

## 修改 RemoteSyslogAppender 增加支持中文输出
```
RemoteSyslogAppender 默认是 ASCII 编码，即使外配置改为 GB2312 也不会支持中文
源码只支持 syslog RFC 3164 4.1.3 协议，对 ASCII 的支持范围在 char(32 - 126) 之间的字符
```
```C#
// RemoteSyslogAppender.cs 部份源码 385行左右
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
// 修改后的代码支持中文，参考 syslog RFC 5424 协议信息格式（未严格按协议开发完整）
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

## 修改 RollingFileAppender 增加支持 设置保留目录文件数量
```
日志文件滚动保存文件数量，会随时间的推移日志文件越来越多；增加保留最近创建的文件数量，删除其余旧的日志文件

新增配置属性 MaxReserveFileCount 保留最新的日志文件数量，配置示例：
<param name="MaxReserveFileCount" value="30"/> <!-- -1表示全部保留不删除任何文件 -->

新增加配置属性 MaxReserveFileDays 保留最新的日志文件天数，配置示例：
<param name="MaxReserveFileDays" value="30"/> <!-- -1表示全部保留不删除任何文件 -->
```
```C#
// RollingFileAppender.cs 代码修改部份
/// <summary>
/// 保留目录中的文件数量
/// <para>跟据文件创建日期排序，保留 count 个最新文件，超出 count 数量的文件删除</para>
/// <para>注意：该函数是比较文件的创建日期</para>
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

/// <summary>
/// 保留目录中的文件天数
/// <para>跟据文件上次修时间起计算，保留 days 天的文件，超出 days 天的文件删除</para>
/// <para>注意：该函数是比较文件的上次修改日期</para>
/// </summary>
/// <param name="days">保留天数</param>
/// <param name="path">文件夹目录</param>
/// <param name="searchPattern">文件匹配类型</param>
public static void ReserveFileDays(int days, string path, string searchPattern = null)
{
   if (days < 0 || String.IsNullOrWhiteSpace(path))
   {
      LogLog.Debug(declaringType, "ReserveFileDays 参数错误");
      return;
   }

   DirectoryInfo dir = new DirectoryInfo(path);
   FileInfo[] files = searchPattern == null ? dir.GetFiles() : dir.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);
   if (files.Length == 0) return;

   IEnumerable<FileInfo> removes =
         from file in files
         where file.LastWriteTime < DateTime.Today.AddDays(-days)
         select file;

   foreach (var file in removes)
   {
      file.Delete();
      LogLog.Debug(declaringType, $"Delete File ... LastWriteTime:{file.LastWriteTime}\t Name:{file.Name}");
   }
}

// ... codes

private int m_maxReserveFileCount = -1;
public int MaxReserveFileCount
{
   get { return m_maxReserveFileCount; }
   set { m_maxReserveFileCount = value; }
}

private int m_maxReserveFileDays = -1;
public int MaxReserveFileDays
{
   get { return m_maxReserveFileDays; }
   set { m_maxReserveFileDays = value; }
}

// ... codes
        
override protected void OpenFile(string fileName, bool append)
{
   // ... cdoes
   if (m_maxReserveFileCount > 0)
   {
      FileInfo fi = new FileInfo(fileName);
      ReserveFileCount(m_maxReserveFileCount, fi.Directory.Name, "*" + fi.Extension);
   }
   
   if(m_maxReserveFileDays > 0)
   {
      FileInfo fi = new FileInfo(fileName);
      ReserveFileDays(m_maxReserveFileDays, fi.Directory.Name, "*" + fi.Extension);
   }
}
```

# 代码使用 示例
```C#
using System.Windows;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)] //或写在配置中，参考配置示例
namespace Space
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public static readonly log4net.ILog Log = log4net.LogManager.GetLogger("ApplicationLogger");

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Log.InfoFormat("Application OnStartup");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            Log.InfoFormat("Application OnExit");
        }
    }
}
```

# 参考配置 示例
```XML
<!-- Log4Net.Config -->
<?xml version="1.0" encoding="utf-8" ?>
<configuration>

    <!--
    <appSettings>
        <add key="log4net.Internal.Emit" value="False"/>
        <add key="log4net.Internal.Debug" value="False"/>
        <add key="log4net.Internal.Quiet" value="False"/>
        
        <add key="log4net.Config.Watch" value="True"/>
        <add key="log4net.Config" value="Log4Net.Config"/>
    </appSettings>
    -->

    <configSections>
        <section name="log4net" type="log4net.Config.Log4NetConfigurationSectionHandler, log4net"/>
    </configSections>

    <!-- Config Examples : http://logging.apache.org/log4net/release/config-examples.html -->
    <log4net debug="true">
        <!--Root Logger-->
        <root>
            <!-- 
            日志等级：OFF > FATAL(致命错误) > ERROR(一般错误) > WARN(警告) > INFO(一般信息) > DEBUG(调试信息) > ALL 
            跟据项目需求，开启/引用输出源名称
            -->
            <level value="INFO" />
            <appender-ref ref="UdpAppender" />
            <!--appender-ref ref="SmtpAppender" /-->
            <appender-ref ref="LogFileAppender" />
            <!--appender-ref ref="ColoredConsoleAppender"/-->

            <!--appender-ref ref="TraceAppender" /-->
            <appender-ref ref="ConsoleAppender" />
            <appender-ref ref="DebugStringAppender"/>
            <appender-ref ref="RemoteSyslogAppender"/>
            <param name="ConversionPattern" value="[%date{yyyy-MM-dd HH:mm:ss}] [%thread] [%level] [%method(%line)] %logger [%ndc] - %message (%r) %newline" />
        </root>

        <!-- 日志文本文件 输出 -->
        <appender  name="LogFileAppender" type="log4net.Appender.RollingFileAppender,log4net" >
            <param name="File" value="Logs/" />
            <!--是否是向文件中追加日志-->
            <param name="AppendToFile" value="true" />
            <!--单个文件最大的大小，单位:KB|MB|GB-->
            <param name="MaximumFileSize" value="8MB" />
            <!--当日志文件达到MaxFileSize大小，就自动创建备份文件-->
            <param name="MaxSizeRollBackups" value="10" />
            <!--按照何种方式产生多个日志文件(日期[Date],文件大小[Size],混合[Composite])-->
            <param name="RollingStyle" value="Composite" />
            <!--日志文件名格式 yyyy-MM-dd.log-->
            <param name="DatePattern" value="'logger_'yyyy-MM-dd'.log'" />
            <!--日志文件名是否是固定不变的-->
            <param name="StaticLogFileName" value="false" />
            <!--使用UTF-8编码-->
            <param name="Encoding" value="UTF-8" />
            <!--日志文件的保留数量及方式，-1表示不处理，或可使用叠加方式 -->
            <param name="MaxReserveFileDays" value="-1"/>
            <param name="MaxReserveFileCount" value="30"/>
            <!--记录日志写入文件时，不锁定文本文件，防止多线程时不能写Log,官方说线程非安全-->
            <lockingModel type="log4net.Appender.FileAppender+MinimalLock" />
            <!--定义输出布局风格-->
            <layout type="log4net.Layout.PatternLayout,log4net">
                <param name="ConversionPattern" value="[%date{HH:mm:ss}] [%thread] %level %logger [%method(%line)] - %message (%r) %newline" />
                <param name="Header" value="[Header]&#13;&#10;" />
                <param name="Footer" value="[Footer]&#13;&#10;&#13;&#10;" />
            </layout>
        </appender>

        <!-- 控制台程序 输出 -->
        <appender name="ColoredConsoleAppender" type="log4net.Appender.ColoredConsoleAppender">
            <mapping>
                <level value="ERROR" />
                <foreColor value="Red" />
            </mapping>
            <mapping>
                <level value="WARN" />
                <foreColor value="Yellow" />
            </mapping>
            <mapping>
                <level value="INFO" />
                <foreColor value="White" />
            </mapping>
            <mapping>
                <level value="DEBUG" />
                <foreColor value="Blue" />
            </mapping>
            <layout type="log4net.Layout.PatternLayout">
                <conversionPattern value="[%date{yyyy-MM-dd HH:mm:ss}] [%thread] %level %logger [%method(%line)] - %message (%r) %newline" />
            </layout>
            <filter type="log4net.Filter.LevelRangeFilter">
                <param name="LevelMin" value="Info" />
                <param name="LevelMax" value="Fatal" />
            </filter>
        </appender>

        <!-- UDP远程 输出 -->
        <appender name="UdpAppender" type="log4net.Appender.UdpAppender">
            <!--encoding value="gb2312" /-->
            <remotePort value="6666" />
            <remoteAddress value="127.0.0.1" />
            <layout type="log4net.Layout.PatternLayout, log4net">
                <conversionPattern value="[%date{yyyy-MM-dd HH:mm:ss}] [%property{log4net:HostName}] [%thread] [%level] %logger [%method(%line)] - %message (%r) %newline" />
            </layout>
        </appender>

        <!-- Stmp邮件 输出 -->
        <!-- (授权码) exmail.qq.com 设置—帐户—帐户安全(开启帐户安全)—微信绑定—客户端专用密码(生成客户端专用密码)-->
        <appender name="SmtpAppender" type="log4net.Appender.SmtpAppender,log4net">
            <to value="huangmin@spacecg.cn" />
            <from value="liaoyunhui@spacecg.cn" />
            <username value="liaoyunhui@spacecg.cn" />
            <!--授权码(非密码)-->
            <password value="9XZeuSzznTSBiENU" />
            <subject value="XX位置XX项目日志信息" />
            <!-- SmtpHost 使用SSL(具体看邮件服务器是否开启 SSL ) -->
            <enableSsl value="true" />
            <authentication value="Basic" />
            <smtpHost value="smtp.exmail.qq.com" />
            <!-- 未达到 bufferSize 大小信息将被丢弃 -->
            <lossy value="true" />
            <bufferSize value="1024" />
            <evaluator type="log4net.Core.LevelEvaluator,log4net">
                <threshold value="WARN"/>
            </evaluator>
            <layout type="log4net.Layout.PatternLayout">
                <conversionPattern value="[%date{yyyy-MM-dd HH:mm:ss}] [%thread] %level [%method(%line)] %logger [%ndc] - %message (%r) %newline" />
            </layout>
        </appender>

        <!-- SysLog 输出 -->
        <!-- 2019/11/06 修改了log4net RemoteSyslogAppender 部份源码 version: 2.0.8.1 -->
        <!-- 原版本 syslog 协议版本为 RFC3164 数据编码为 ACSII 码，不支持中文消息；后面将 syslog 版本修为 RFC5424 数据编码改为系统默认编码，可以支持中文了 -->
        <appender name="RemoteSyslogAppender" type="log4net.Appender.RemoteSyslogAppender,log4net">
            <remoteAddress value="127.0.0.1" />
            <remotePort value="514" />
            <layout type="log4net.Layout.PatternLayout, log4net">
                <conversionPattern value="[%property{log4net:HostName}] [%thread] [%level] %logger [%method(%line)] - %message (%r) %newline" />
            </layout>
        </appender>

        <!-- Trace 输出 -->
        <appender name="TraceAppender" type="log4net.Appender.TraceAppender,log4net">
            <layout type="log4net.Layout.PatternLayout, log4net">
                <conversionPattern value="Trace:[%date{yyyy-MM-dd HH:mm:ss}] [%thread] %level %logger [%method(%line)] - %message (%r) %newline" />
            </layout>
        </appender>
        <!-- Console 输出 -->
        <appender name="ConsoleAppender" type="log4net.Appender.ConsoleAppender,log4net">
            <layout type="log4net.Layout.PatternLayout, log4net">
                <conversionPattern value="[%date{yyyy-MM-dd HH:mm:ss}] [%thread] %level %logger [%method(%line)] - %message (%r) %newline" />
            </layout>
        </appender>
        <!-- Debug 输出 -->
        <appender name="DebugStringAppender" type="log4net.Appender.OutputDebugStringAppender,log4net" >
            <layout type="log4net.Layout.PatternLayout, log4net">
                <conversionPattern value="Debug:[%date{HH:mm:ss}] [%thread] %level %logger [%method(%line)] - %message (%r) %newline" />
            </layout>
        </appender>

    </log4net>
</configuration>

```

# 扩展 AppenderSkeleton 示例(WPF TextBoxBase)
```C#
// TextBoxBaseAppender.cs
using System;
using log4net.Core;
using log4net.Layout;
using log4net.Appender;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;

namespace SpaceCG.Log4Net
{
    /// <summary>
    /// Log4Net WPF TextBoxBase Appender
    /// </summary>
    public class TextBoxBaseAppender : AppenderSkeleton
    {
        protected static readonly SolidColorBrush InfoColor = new SolidColorBrush(Color.FromArgb(0x7F, 0xFF, 0xFF, 0xFF));
        protected static readonly SolidColorBrush WarnColor = new SolidColorBrush(Color.FromArgb(0x7F, 0xFF, 0xFF, 0x00));
        protected static readonly SolidColorBrush ErrorColor = new SolidColorBrush(Color.FromArgb(0x7F, 0xFF, 0x00, 0x00));
        protected static readonly SolidColorBrush FatalColor = new SolidColorBrush(Color.FromArgb(0xBF, 0xFF, 0x00, 0x00));

        /// <summary>
        /// 获取或设置最大可见行数
        /// </summary>
        protected uint MaxLines = 512;
        protected TextBoxBase TextBox;
        protected Action<String, Level> AppendTextDelegate;

        private TextBox tb;
        private RichTextBox rtb;

        /// <summary>
        /// Log4Net Appender for WPF TextBoxBase 
        /// </summary>
        /// <param name="textBox"></param>
        public TextBoxBaseAppender(TextBoxBase textBox)
        {
            this.TextBox = textBox;
            this.AppendTextDelegate = TextBoxAppendText;
            this.Layout = new PatternLayout("[%date{yyyy-MM-dd HH:mm:ss}] [%thread] [%level] [%method(%line)] %logger - %message (%r) %newline");

            //Set Controls Default Config
            if(this.TextBox is TextBox)
            {
                tb = (TextBox)this.TextBox;
                tb.IsReadOnly = true;
                tb.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
            }
            else if(this.TextBox is RichTextBox)
            {
                rtb = (RichTextBox)this.TextBox;
                rtb.IsReadOnly = true;
                rtb.AcceptsReturn = true;
                rtb.Document.LineHeight = 2;
                rtb.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            }
            else
            {
                //...                
            }

            log4net.Config.BasicConfigurator.Configure(this);
        }

        /// <summary>
        /// Log4Net Appender for WPF TextBoxBase 
        /// </summary>
        /// <param name="textBox"></param>
        /// <param name="maxLines">最大行数为 1024 行，默认为 512 行</param>
        public TextBoxBaseAppender(TextBoxBase textBox, uint maxLines):this(textBox)
        {
            this.MaxLines = maxLines > 1024 ? 1024 : maxLines;
        }

        /// <summary>
        /// @override
        /// </summary>
        /// <param name="loggingEvent"></param>
        protected override void Append(LoggingEvent loggingEvent)
        {
            if (this.TextBox == null) return;
            if (!this.TextBox.IsLoaded) return;

            String text = string.Empty;
            PatternLayout patternLayout = this.Layout as PatternLayout;

            if (patternLayout != null)
            {
                text = patternLayout.Format(loggingEvent);
                if (loggingEvent.ExceptionObject != null)
                    text += loggingEvent.ExceptionObject.ToString() + Environment.NewLine;
            }
            else
            {
                text = loggingEvent.LoggerName + "-" + loggingEvent.RenderedMessage + Environment.NewLine;
            }
            
            this.TextBox.Dispatcher.BeginInvoke(this.AppendTextDelegate, text, loggingEvent.Level);
        }

        /// <summary>
        /// TextBox AppendText
        /// </summary>
        /// <param name="text"></param>
        /// <param name="level"></param>
        protected void TextBoxAppendText(String text, Level level)
        {
            if (tb != null)
            {
                tb.AppendText(text);
                tb.ScrollToEnd();

                if (tb.LineCount > MaxLines)
                    tb.Text = tb.Text.Remove(0, tb.GetCharacterIndexFromLineIndex(1));

                return;
            }

            if (rtb != null)
            {
                Paragraph paragraph = new Paragraph(new Run(text.Trim()));
                paragraph.Background = level == Level.Fatal ? FatalColor : level == Level.Error ? ErrorColor : level == Level.Warn ? WarnColor : InfoColor;

                rtb.Document.Blocks.Add(paragraph);
                rtb.ScrollToEnd();

                if (rtb.Document.Blocks.Count > MaxLines)
                    rtb.Document.Blocks.Remove(rtb.Document.Blocks.FirstBlock);

                return;
            }

        }
        
    }
}

```
