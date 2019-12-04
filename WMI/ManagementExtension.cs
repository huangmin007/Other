using System;
using System.Linq;
using System.Management;

namespace SpaceCG.Extension
{
    /// <summary>
    /// 注意：以后不会在这里更新，这只是一个示例，更多应用需自已去开发测试
    /// <para>System.Management命名空间 扩展/实用/通用 函数</para>
    /// </summary>
    public static class ManagementExtension
    {
        #region "__InstanceCreationEvent" AND "__InstanceDeletionEvent"

        /// <summary> ManagementEventWatcher Object </summary>
        static ManagementEventWatcher InstanceCreationEvent;
        /// <summary> ManagementEventWatcher Object </summary>
        static ManagementEventWatcher InstanceDeletionEvent;

        /// <summary>
        /// 监听 "__InstanceCreationEvent" AND "__InstanceDeletionEvent" 事件
        /// <para>示例：$"TargetInstance ISA 'Win32_PnPEntity'"    //监听即插即用设备状态，有关 Win32_PnPEntity(WMI类) 属性参考：https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-pnpentity </para>
        /// <para>示例：$"TargetInstance ISA 'Win32_PnPEntity' AND TargetInstance.Name LIKE '%({Serial.PortName})'"    //监听即插即用设备状态，且名称为串口名称</para>
        /// <para>示例：$"TargetInstance ISA 'Win32_LogicalDisk' AND TargetInstance.DriveType = 2 OR TargetInstance.DriveType = 4"  //监听移动硬盘状态 </para>
        /// <para>更多 WMI 类，请参考：https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/computer-system-hardware-classes </para>
        /// </summary>
        /// <param name="wql_condition">WQL 条件语句，关于 WQL 参考：https://docs.microsoft.com/zh-cn/windows/win32/wmisdk/wql-sql-for-wmi?redirectedfrom=MSDN </param>
        /// <param name="changedCallback"></param>
        /// <param name="Log"></param>
        public static void ListenInstanceChange(String wql_condition, Action<ManagementBaseObject> changedCallback, log4net.ILog Log = null)
        {
            if (InstanceCreationEvent != null || InstanceDeletionEvent != null) return;
            ManagementScope scope = new ManagementScope(@"\\.\Root\CIMV2", new ConnectionOptions()
            {
                //Username = "",
                //Password = "",
                EnablePrivileges = true,
            });

            TimeSpan interval = new TimeSpan(0, 0, 1);

            //__InstanceCreationEvent 
            InstanceCreationEvent = new ManagementEventWatcher(scope, new WqlEventQuery("__InstanceCreationEvent", interval, wql_condition));
            InstanceCreationEvent.EventArrived += (s, e) =>
            {
                Log?.InfoFormat("Instance Creation Event :: {0}", e.NewEvent.ClassPath);
                changedCallback?.Invoke(e.NewEvent);
            };

            //__InstanceDeletionEvent
            InstanceDeletionEvent = new ManagementEventWatcher(scope, new WqlEventQuery("__InstanceDeletionEvent", interval, wql_condition));
            InstanceDeletionEvent.EventArrived += (s, e) =>
            {
                Log?.InfoFormat("Instance Deletion Event :: {0}", e.NewEvent.ClassPath);
                changedCallback?.Invoke(e.NewEvent);
            };

            InstanceCreationEvent.Start();
            InstanceDeletionEvent.Start();
        }

        public static void RemoveInstanceChange()
        {
            InstanceCreationEvent?.Stop();
            InstanceDeletionEvent?.Stop();

            InstanceCreationEvent?.Dispose();
            InstanceDeletionEvent?.Dispose();

            InstanceCreationEvent = null;
            InstanceDeletionEvent = null;
        }
        #endregion


        #region "__InstanceModificationEvent"

        /// <summary> ManagementEventWatcher Object </summary>
        static ManagementEventWatcher InstanceModificationEvent;

        /// <summary>
        /// 监听 "__InstanceModificationEvent" 事件 （持续监听事件，按固定 1s 查询一次状态生成事件）
        /// <para>示例：$"TargetInstance ISA 'Win32_Battery'"    //持续监听电池状态，EstimatedChargeRemaining 表示电池电量表示电池电量；更多 Win32_Battery 类的属性，请参考：https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-battery </para>
        /// <para>更多 WMI 类，请参考：https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/computer-system-hardware-classes </para>
        /// </summary>
        /// <param name="wql_condition">WQL 条件语句，关于 WQL 参考：https://docs.microsoft.com/zh-cn/windows/win32/wmisdk/wql-sql-for-wmi?redirectedfrom=MSDN </param>
        /// <param name="changedCallback"></param>
        /// <param name="Log"></param>
        public static void ListenInstanceModification(String wql_condition, Action<ManagementBaseObject> changedCallback, log4net.ILog Log = null)
        {
            if (InstanceModificationEvent != null) return;

            ManagementScope scope = new ManagementScope(@"\\.\Root\CIMV2", new ConnectionOptions()
            {
                EnablePrivileges = true,
            });

            //__InstanceModificationEvent 
            InstanceModificationEvent = new ManagementEventWatcher()
            {
                Scope = scope,
                Query = new WqlEventQuery()
                {
                    Condition = wql_condition,
                    WithinInterval = TimeSpan.FromSeconds(1),
                    EventClassName = "__InstanceModificationEvent",
                }
            };
            InstanceModificationEvent.EventArrived += (s, e) =>
            {
                if(Log != null && Log.IsDebugEnabled)
                    Log.DebugFormat("Instance Modification Event :: {0}", e.NewEvent.ClassPath);
                changedCallback?.Invoke(e.NewEvent);
            };
            InstanceModificationEvent.Start();
        }
        /// <summary>
        /// 监听 "__InstanceModificationEvent" 事件 （持续监听事件，按自定义时间间隔查询）
        /// <para>示例：$"TargetInstance ISA 'Win32_Battery'"    //持续监听电池状态，EstimatedChargeRemaining 表示电池电量；更多 Win32_Battery 类的属性，请参考：https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-battery </para>
        /// <para>更多 WMI 类，请参考：https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/computer-system-hardware-classes </para>
        /// </summary>
        /// <param name="wql_condition">WQL 条件语句，关于 WQL 参考：https://docs.microsoft.com/zh-cn/windows/win32/wmisdk/wql-sql-for-wmi?redirectedfrom=MSDN </param>
        /// <param name="interval">按指定的间隔时间查询</param>
        /// <param name="changedCallback"></param>
        /// <param name="Log"></param>
        public static void ListenInstanceModification(String wql_condition, TimeSpan interval, Action<ManagementBaseObject> changedCallback, log4net.ILog Log = null)
        {
            if (InstanceModificationEvent != null) return;

            ManagementScope scope = new ManagementScope(@"\\.\Root\CIMV2", new ConnectionOptions()
            {
                EnablePrivileges = true,
            });

            //__InstanceModificationEvent 
            InstanceModificationEvent = new ManagementEventWatcher()
            {
                Scope = scope,
                Query = new WqlEventQuery()
                {
                    Condition = wql_condition,
                    WithinInterval = interval,
                    EventClassName = "__InstanceModificationEvent",
                }
            };
            InstanceModificationEvent.EventArrived += (s, e) =>
            {
                if (Log != null && Log.IsDebugEnabled)
                    Log.DebugFormat("Instance Modification Event :: {0}", e.NewEvent.ClassPath);
                changedCallback?.Invoke(e.NewEvent);
            };
            InstanceModificationEvent.Start();
        }
        /// <summary>
        /// 移除 "__InstanceModificationEvent" 监听事件
        /// </summary>
        public static void RemoveInstanceModification()
        {
            InstanceModificationEvent?.Stop();
            InstanceModificationEvent?.Dispose();
            InstanceModificationEvent = null;
        }
        #endregion


        /// <summary>
        /// 输出打印 PropertyDataCollection 属性名以及对应值
        /// </summary>
        /// <param name="collection"></param>
        public static void ToDebug(PropertyDataCollection collection)
        {
            if (collection == null || collection.Count <= 0) return;

            foreach (PropertyData pd in collection)
            {
                if (string.IsNullOrWhiteSpace(pd.Name) || pd.Value == null) continue;

                if (!pd.IsArray)
                    Console.WriteLine("\t{0} = \"{1}\"", pd.Name, pd.Value);
                else
                {
                    Console.WriteLine("\t{0} = \n\t{{", pd.Name);
                    foreach (var item in pd.Value as Array)
                        Console.WriteLine("\t\t\"{0}\",", item);
                    Console.WriteLine("\t}");
                }
            }
        }

        /// <summary>
        /// 输出打印 ManagementBaseObject 对象的属性名以及对应值
        /// </summary>
        /// <param name="obj"></param>
        public static void ToDebug(ManagementBaseObject obj)
        {
            Console.WriteLine("=====================================================Start");
            Console.WriteLine("ManagementBaseObject:\"{0}\"", obj.ClassPath);
            //SystemProperties
            ToDebug(obj.SystemProperties);
            //Properties
            ToDebug(obj.Properties);

            ManagementBaseObject instance = (ManagementBaseObject)obj.GetPropertyValue("TargetInstance");
            if (instance != null)
            {
                Console.WriteLine("TargetInstance:\"{0}\"", instance.ClassPath);
                //SystemProperties
                ToDebug(instance.SystemProperties);
                //Properties
                ToDebug(instance.Properties);
            }

            Console.WriteLine("=====================================================End");
        }

        /// <summary>
        /// 获取当前计算机的 串行端口 完整名称 的数组
        /// <para>与 <see cref="System.IO.Ports.SerialPort.GetPortNames"/> 不同，SerialPort.GetPortNames() 只输出类似"COM3,COM4,COMn"，该函数输出串口对象的名称或是驱动名，类似："USB Serial Port (COM59)" </para>
        /// <para>这只是示例函数代码，用于查询 WMI 信息。更多 WMI 应用需自行思考。</para>
        /// </summary>
        /// <returns></returns>
        public static string[] GetPortNames()
        {
            String queryString = "SELECT Name FROM Win32_PnPEntity WHERE Name LIKE '%(COM_)' OR Name LIKE '%(COM__)'";

            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(queryString))
            {
                ManagementObjectCollection collection = searcher.Get();

                var names = from ManagementObject obj in collection
                            from PropertyData pd in obj.Properties
                            where !string.IsNullOrWhiteSpace(pd.Name) && pd.Value != null
                            select pd.Value.ToString();

                return names.ToArray();
            }
        }

    }
}
