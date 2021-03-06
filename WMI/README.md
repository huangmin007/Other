# WMI（Windows Management Instrumentation,Windows 管理规范）

## 之前的代码
```
https://github.com/huangmin007/ConsoleShellApplication/blob/master/WMI.Shell/WMIShell.cs
```

## WMI 之 串口热插拔的 应用示例
```C#
// ... codes
protected override void OnInitialized(EventArgs e)
{
    base.OnInitialized(e);
    App.Log.InfoFormat("OnInitialized.");
    
    //针对指定的串口监听
    string wql = $"TargetInstance isa 'Win32_PnPEntity' AND TargetInstance.Name LIKE '%({Serial.PortName})'";
    //OR
    //string wql = $"TargetInstance isa 'Win32_PnPEntity' AND TargetInstance.Name LIKE '%(COM_)' OR TargetInstance.Name LIKE '%(COM__)'";
    ManagementExtension.ListenInstanceChange(wql, PnPEntityChangedHandler, App.Log);
}
protected override void OnClosing(CancelEventArgs e)
{
    base.OnClosing(e);
    App.Log.InfoFormat("OnClosing.");
    ManagementExtension.RemoveInstanceChange();
}
protected void PnPEntityChangedHandler(ManagementBaseObject obj)
{
    ManagementExtension.ToDebug(obj);
    ManagementBaseObject instance = (ManagementBaseObject)obj.GetPropertyValue("TargetInstance");
    
    //这里也一样，可不需要做判断
    //if (instance.ClassPath.RelativePath != "Win32_PnPEntity") return;

    //针对定向查询，可不需要在做结节判断
    //例如下面判断与串口名称是否一致
    if (instance.GetPropertyValue("Name").ToString().ToLower().IndexOf(Serial.PortName.ToLower()) != -1)
    {
        if (obj.ClassPath.RelativePath == "__InstanceCreationEvent")
        {
            if (!Serial.IsOpen) Serial.Open();
        }

        if (obj.ClassPath.RelativePath == "__InstanceDeletionEvent")
        {
            if (Serial.IsOpen) Serial.Close();
        }
    }
}
// ... codes
```

## 可以有更多 WMI 的应用
```
Examples
```

## Window Message 方式监听设备热插拔事件
```C#
//C# WPF, WinFroms的也一样监听Window Message
private void Window_Loaded(object sender, RoutedEventArgs e)
{
    IntPtr Handle = new WindowInteropHelper(this).Handle;
    
    HwndSource hwndSource = HwndSource.FromVisual(this) as HwndSource;
    Console.WriteLine("{0} == {1} = true", Handle, hwndSource.Handle);
    if (hwndSource != null)
        hwndSource.AddHook(new HwndSourceHook(WindowProcHandler));//挂钩

    //HwndSource.FromHwnd(Handle).AddHook(WindowProcHandler);
    //Marshal.GetLastWin32Error();
}

protected IntPtr WindowProcHandler(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
{
    MessageType mt = (MessageType)msg;
    Console.WriteLine("MessageType:{0} {1}", mt, wParam.ToInt32());

    if (mt == MessageType.WM_DEVICECHANGE)
    {
        DeviceBroadcastType dbt = (DeviceBroadcastType)wParam.ToInt32();
        Console.WriteLine("DeviceBroadcastType: {0}", dbt);

        switch (dbt)
        {
            case DeviceBroadcastType.DBT_DEVICEARRIVAL:
            case DeviceBroadcastType.DBT_DEVICEREMOVECOMPLETE:
                Console.WriteLine(dbt == DeviceBroadcastType.DBT_DEVICEARRIVAL ? "Device Arrival" : "Device Move Complete");

                DEV_BROADCAST_HDR hdr = (DEV_BROADCAST_HDR)Marshal.PtrToStructure<DEV_BROADCAST_HDR>(lParam);
                Console.WriteLine("{0}", hdr);

                if (hdr.dbch_devicetype == DeviceType.DBT_DEVTYP_PORT)
                {
                    DEV_BROADCAST_PORT port = (DEV_BROADCAST_PORT)Marshal.PtrToStructure<DEV_BROADCAST_PORT>(lParam);
                    Console.WriteLine(port);
                }
                if (hdr.dbch_devicetype == DeviceType.DBT_DEVTYP_VOLUME)
                {
                    DEV_BROADCAST_VOLUME volume = (DEV_BROADCAST_VOLUME)Marshal.PtrToStructure<DEV_BROADCAST_VOLUME>(lParam);
                    Console.WriteLine(volume);
                }
                break;

            default:
                break;
        }

        handled = true;
    }

    return IntPtr.Zero;
}
```
