using System;
using System.Linq;
using System.Management;

namespace SpaceCG.Extension
{
    /// <summary>
    /// Management 扩展/实用/通用 函数
    /// </summary>
    public static class ManagementExtension
    {
        static ManagementEventWatcher USBInsertEvent;
        static ManagementEventWatcher USBRemoveEvent;

        /// <summary>
        /// Create Listener Win32_PnpEntity Instance Evnet
        /// <para>Win32_PnPEntity(WMI类) 表示即插即用设备的属性，有关属性参考：https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-pnpentity </para>
        /// </summary>
        /// <param name="changedCallback"></param>
        /// <param name="Log"></param>
        public static void ListenPnPEntityEvent(Action<ManagementBaseObject> changedCallback, log4net.ILog Log = null)
        {
            ManagementScope scope = new ManagementScope("root\\cimv2:Win32_PnPEntity");
            scope.Options.EnablePrivileges = true;

            TimeSpan interval = new TimeSpan(0, 0, 1);

            //Insert
            USBInsertEvent = new ManagementEventWatcher(scope, new WqlEventQuery("__InstanceCreationEvent", interval, "TargetInstance isa 'Win32_PnPEntity'"));
            USBInsertEvent.EventArrived += (s, e) =>
            {
                Log?.InfoFormat("Instance Creation Event :: {0}", e.NewEvent.ClassPath);
                changedCallback?.Invoke(e.NewEvent);
            };
            //Remove
            USBRemoveEvent = new ManagementEventWatcher(scope, new WqlEventQuery("__InstanceDeletionEvent", interval, "TargetInstance isa 'Win32_PnPEntity'"));
            USBRemoveEvent.EventArrived += (s, e) =>
            {
                Log?.InfoFormat("Instance Deletion Event :: {0}", e.NewEvent.ClassPath);
                changedCallback?.Invoke(e.NewEvent);
            };

            USBInsertEvent.Start();
            USBRemoveEvent.Start();
        }

        /// <summary>
        /// Remove Listener Win32_PnpEntity Instance Evnet
        /// <para>Win32_PnPEntity(WMI类) 表示即插即用设备的属性，有关属性参考：https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-pnpentity </para>
        /// </summary>
        public static void RemovePnPEntityEvent()
        {
            USBInsertEvent?.Stop();
            USBRemoveEvent?.Stop();

            USBInsertEvent?.Dispose();
            USBRemoveEvent?.Dispose();
        }

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
        /// 这只是示例函数代码，用于查询 WMI 信息。更多 WMI 应用需自行思考。
        /// <para>获取当前计算机的串行端口名的数组，请使用 SerialPort.GetPortNames(); </para>
        /// </summary>
        /// <returns></returns>
        public static string[] GetPortNames()
        {
            String queryString = "SELECT Name FROM Win32_PnPEntity WHERE Name LIKE '%(COM_)' OR Name LIKE '%(COM__)'";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(queryString);
            ManagementObjectCollection collection = searcher.Get();

            var names = from ManagementObject obj in collection
                        from PropertyData pd in obj.Properties
                        where !string.IsNullOrWhiteSpace(pd.Name) && pd.Value != null
                        select pd.Value.ToString();

            return names.ToArray();
        }

    }
}
