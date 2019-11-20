# log4cplus 修改改说明

```
Github:https://github.com/log4cplus/log4cplus
修改 Log4jUdpAppender 类，可选输出 xml 固定格式，和自定义 layout 格式
DailyRollingFileAppender 也是会输出很多日志文件，但还未下手修改
```

## 修改 Log4jUdpAppender 增加支持输出 xml 格式和 Layout 自定义格式
```
原 Log4jUdpAppender 只支持输出 xml 格式日志，后面修改为可选输出格式
```
```XML
# 当使用 xml 编码输出时，layout 属性无效
log4cplus.appender.UDP_SOCKET.Appender.XMLFormat=false
log4cplus.appender.UDP_SOCKET.Appender.layout=log4cplus::PatternLayout
log4cplus.appender.UDP_SOCKET.Appender.layout.ConversionPattern=[%D][%5p][%5t|%x] [%l][%c] - %m%n
```
```C++
//源码
//Log4jUdpAppender.h
// ... codes
protected:
    bool xml = false;
// ... codes

//Log4jUdpAppender.cxx
Log4jUdpAppender::Log4jUdpAppender(const helpers::Properties & properties)
    : Appender(properties)
    , port(5000)
{
    // ... codes
    properties.getBool(xml, LOG4CPLUS_TEXT("XMLFormat"));
    // ... codes
}

void
Log4jUdpAppender::append(const spi::InternalLoggingEvent& event)
{
  // ... codes
  tstring & str = formatEvent (event);

  internal::appender_sratch_pad & appender_sp  = internal::get_appender_sp ();
  tostringstream & buffer = appender_sp.oss;
  detail::clear_tostringstream (buffer);
	
	if (xml)
	{
		buffer << LOG4CPLUS_TEXT("<log4j:event logger=\"")
			<< outputXMLEscaped(event.getLoggerName())
			<< LOG4CPLUS_TEXT("\" level=\"")
			// TODO: Some escaping of special characters is needed here.
			<< outputXMLEscaped(getLogLevelManager()
				.toString(event.getLogLevel()))
			<< LOG4CPLUS_TEXT("\" timestamp=\"")
			<< helpers::getFormattedTime(LOG4CPLUS_TEXT("%s%q"),
				event.getTimestamp())
			<< LOG4CPLUS_TEXT("\" thread=\"") << event.getThread()
			<< LOG4CPLUS_TEXT("\">")

			<< LOG4CPLUS_TEXT("<log4j:message>")
			// TODO: Some escaping of special characters is needed here.
			<< outputXMLEscaped(str)
			<< LOG4CPLUS_TEXT("</log4j:message>")

			<< LOG4CPLUS_TEXT("<log4j:NDC>")
			// TODO: Some escaping of special characters is needed here.
			<< outputXMLEscaped(event.getNDC())
			<< LOG4CPLUS_TEXT("</log4j:NDC>")

			<< LOG4CPLUS_TEXT("<log4j:locationInfo class=\"\" file=\"")
			// TODO: Some escaping of special characters is needed here.
			<< outputXMLEscaped(event.getFile())
			<< LOG4CPLUS_TEXT("\" method=\"")
			<< outputXMLEscaped(event.getFunction())
			<< LOG4CPLUS_TEXT("\" line=\"")
			<< event.getLine()
			<< LOG4CPLUS_TEXT("\"/>")
			<< LOG4CPLUS_TEXT("</log4j:event>");
	}
	else
	{
		layout->formatAndAppend(buffer, event);  //这里
	}
  appender_sp.chstr = LOG4CPLUS_TSTRING_TO_STRING (buffer.str ());

  bool ret = socket.write(appender_sp.chstr);
  if (!ret)
  {
      helpers::getLogLog().error(
          LOG4CPLUS_TEXT(
              "Log4jUdpAppender::append()- Cannot write to server"));
  }
}
```

## 修改 FileAppenderBase 增加日志文件保留时间，以天为单位
```
原 FileAppenderBase 日志滚动文件数量会很多，增加属性进行控制
```
```XML
# 设置日志保留天数，按最后修改日期计算，默认为 -1 表示全部保留
log4cplus.appender.FILE.Appender.ReserveDays=30
```
```C++
void FileAppenderBase::ReserveDays(int days)
{
	if (days <= 0)return;
	if (filename.empty())return;

#if defined(_WIN32)
	tstring const dir_sep(LOG4CPLUS_TEXT("\\"));
#else
	tstring const dir_sep(LOG4CPLUS_TEXT("/"));
#endif

	//获取文件目录路径
	tstring directory;
	fs::directory_entry entry = fs::directory_entry(filename);
	log4cplus::tstring path = fs::absolute(entry).c_str();

	if (!fs::is_directory(entry))
	{
		const size_t last_idx = path.rfind(dir_sep);
		directory = path.substr(0, last_idx);
	}
	else
	{
		directory = path;
	}

	//获取当前时间
	time_t now = time(0);	

	//获取每个文件的最后修改日期
	for (auto& fn : fs::directory_iterator(directory))
	{
		if (fs::is_directory(fn)) continue;
		//if (fs::file_size(fn) == 0)
		//{
		//	fs::remove(fn);
		//	continue;
		//}

		auto lw_time = fs::last_write_time(fn);
		std::time_t f_time = decltype(lw_time)::clock::to_time_t(lw_time);

		int c_days = std::difftime(now, f_time) / (24 * 60 * 60) + 1;
		if (c_days > days)
		{
			fs::remove(fn);
		}
	}
}
```

# 参考配置 示例
```XML
# 根 Logger 的配置
## 语法：log4cplus.rootLogger=[LogLevel], appenderName, appenderName ...

log4cplus.rootLogger=ALL, STDOUT


# 非根 Logger 的配置
## 语法：log4cplus.logger.logger_name=[LogLevel|INHERITED], appenderName, appenderName, ...
## 说明：INHERITED表示继承父Logger的日志级别

log4cplus.logger.global=TRACE, STDOUT, LOCAL_FILE, UDP_SOCKET, SYSLOG
# log4cplus.logger.global=TRACE, STDOUT, LOCAL_FILE, UDP_SOCKET, SYSLOG
log4cplus.additivity.global=false


# Appender配置
## 语法：log4cplus.appender.appenderName=fully.qualified.name.of.appender.class
## 语法：log4cplus.appender.appenderName.[property]=value



# ------------------------- AppenderName: STDOUT ---------------------------------
log4cplus.appender.STDOUT=log4cplus::ConsoleAppender
log4cplus.appender.STDOUT.layout=log4cplus::TTCCLayout
log4cplus.appender.STDOUT.layout.ConversionPattern=[%d{%H:%M:%S}] [%t] [%-5p] - %m [%M(%L)]%n
# ------------------------- AppenderName: STDOUT ---------------------------------


# ------------------------- AppenderName: UDP_SOCKET ---------------------------------
log4cplus.appender.UDP_SOCKET=log4cplus::AsyncAppender
log4cplus.appender.UDP_SOCKET.Appender=log4cplus::Log4jUdpAppender
log4cplus.appender.UDP_SOCKET.Appender.host=127.0.0.1
log4cplus.appender.UDP_SOCKET.Appender.port=6666
# 当使用 xml 编码输出时，layout 属性无效
log4cplus.appender.UDP_SOCKET.Appender.XMLFormat=false
log4cplus.appender.UDP_SOCKET.Appender.layout=log4cplus::PatternLayout
log4cplus.appender.UDP_SOCKET.Appender.layout.ConversionPattern=[%D][%5p][%5t|%x] [%l][%c] - %m%n
# ------------------------- AppenderName: UDP_SOCKET ---------------------------------


# ------------------------- AppenderName: SYSLOG ---------------------------------
log4cplus.appender.SYSLOG=log4cplus::AsyncAppender
log4cplus.appender.SYSLOG.Appender=log4cplus::SysLogAppender
log4cplus.appender.SYSLOG.Appender.host=127.0.0.1
log4cplus.appender.SYSLOG.Appender.port=514
log4cplus.appender.SYSLOG.Appender.udp=true
log4cplus.appender.SYSLOG.Appender.ident=APP_NAME
log4cplus.appender.SYSLOG.Appender.Locale=en_US.UTF-8
log4cplus.appender.SYSLOG.Appender.layout=log4cplus::PatternLayout
log4cplus.appender.SYSLOG.Appender.layout.ConversionPattern=[%D][%c][%-5p][%5t|%x] [%M:%L] - %m%n
# ------------------------- AppenderName: SYSLOG ---------------------------------



# ------------------------- AppenderName: LOCAL_FILE ---------------------------------
log4cplus.appender.LOCAL_FILE=log4cplus::AsyncAppender
log4cplus.appender.LOCAL_FILE.QueueLimit=1024

log4cplus.appender.LOCAL_FILE.Appender=log4cplus::DailyRollingFileAppender
log4cplus.appender.LOCAL_FILE.Appender.Append=true
log4cplus.appender.LOCAL_FILE.Appender.BufferSize=10240
log4cplus.appender.LOCAL_FILE.Appender.ImmediateFlush=true

log4cplus.appender.LOCAL_FILE.Appender.CreateDirs=true
# 关闭程序时是否滚动日志文件
# log4cplus.appender.LOCAL_FILE.Appender.RollOnClose=false

log4cplus.appender.LOCAL_FILE.Appender.File=Logs/logger
log4cplus.appender.LOCAL_FILE.Appender.DatePattern=%Y-%m-%d.log
log4cplus.appender.LOCAL_FILE.Appender.MaxFileSize=8MB
log4cplus.appender.LOCAL_FILE.Appender.MaxBackupIndex=16

# 指定滚动计划
# MONTHLY(每月)、WEEKLY(每周)、DAILY(默认每日)、TWICE_DAILY(每两天)、HOURLY(每时)、MINUTELY(每分)
log4cplus.appender.LOCAL_FILE.Appender.Schedule=DAILY

# log4cplus.appender.LOCAL_FILE.Appender.Locale=en_US.UTF-8
log4cplus.appender.LOCAL_FILE.Appender.layout=log4cplus::PatternLayout
log4cplus.appender.LOCAL_FILE.Appender.layout.ConversionPattern=[%D][%5p][%5t|%x] [%l][%c] - %m%n

# 指定日志消息的输出最低层次
log4cplus.appender.LOCAL_FILE.Threshold=TRACE
# ------------------------- AppenderName: LOCAL_FILE ---------------------------------



# ------------------------- AppenderName: TIME_FILE ---------------------------------
log4cplus.appender.TIME_FILE=log4cplus::AsyncAppender
log4cplus.appender.TIME_FILE.QueueLimit=1024

log4cplus.appender.TIME_FILE.Appender=log4cplus::TimeBasedRollingFileAppender
log4cplus.appender.TIME_FILE.Appender.Append=true
log4cplus.appender.TIME_FILE.Appender.BufferSize=10240
log4cplus.appender.TIME_FILE.Appender.ImmediateFlush=true

log4cplus.appender.TIME_FILE.Appender.MaxHistory=32
log4cplus.appender.TIME_FILE.Appender.CreateDirs=true
# 关闭程序时是否滚动日志文件
log4cplus.appender.TIME_FILE.Appender.RollOnClose=false

log4cplus.appender.TIME_FILE.Appender.FilenamePattern=Logs/logger_%d.log
log4cplus.appender.TIME_FILE.Appender.MaxFileSize=8MB

# 指定滚动计划
# MONTHLY(每月)、WEEKLY(每周)、DAILY(默认每日)、TWICE_DAILY(每两天)、HOURLY(每时)、MINUTELY(每分)
log4cplus.appender.TIME_FILE.Appender.Schedule=DAILY

# log4cplus.appender.TIME_FILE.Appender.Locale=en_US.UTF-8
log4cplus.appender.TIME_FILE.Appender.layout=log4cplus::PatternLayout
log4cplus.appender.TIME_FILE.Appender.layout.ConversionPattern=[%D][%5p][%5t|%x] [%l][%c] - %m%n

# 指定日志消息的输出最低层次
log4cplus.appender.TIME_FILE.Threshold=TRACE
# ------------------------- AppenderName: TIME_FILE ---------------------------------

```

```C++
//配置头文件 log4cplus.h 
#pragma once

#include <log4cplus/logger.h>
#include <log4cplus/initializer.h>
#include <log4cplus/configurator.h>
#include <log4cplus/loggingmacros.h>

#include <log4cplus/appender.h>
#include <log4cplus/fileappender.h>
#include <log4cplus/socketappender.h>
#include <log4cplus/syslogappender.h>
#include <log4cplus/consoleappender.h>
#include <log4cplus/log4judpappender.h>

using namespace log4cplus;

namespace spacecg
{
	/* global 日志记录对象 */
	static log4cplus::Logger Log;

	/* log4cplus 配置，读取 log4cplus.config 配置文件 */
	static void Log4CplusConfigure()
	{
		log4cplus::Initializer initializer;
		log4cplus::PropertyConfigurator property(LOG4CPLUS_TEXT("log4cplus.config"));
		property.configure();

		Log = log4cplus::Logger::getInstance(LOG4CPLUS_TEXT("global"));
		LOG4CPLUS_INFO(Log, "log4cplus conifgure complete.");

		//log4cplus::Logger::shutdown();
	}
}
```
