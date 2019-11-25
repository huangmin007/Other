# WMI（Windows Management Instrumentation,Windows 管理规范）

## 之前的代码
```
https://github.com/huangmin007/ConsoleShellApplication/blob/master/WMI.Shell/WMIShell.cs
```

## 串口热插拔的应用示例
```C#
// ... codes
protected override void OnInitialized(EventArgs e)
{
    base.OnInitialized(e);
    App.Log.InfoFormat("OnInitialized.");
    ManagementExtension.ListenPnPEntityEvent(PnPEntityChangedHandler, App.Log);
}
protected override void OnClosing(CancelEventArgs e)
{
    base.OnClosing(e);
    App.Log.InfoFormat("OnClosing.");
    ManagementExtension.RemovePnPEntityEvent();
}
protected void PnPEntityChangedHandler(ManagementBaseObject obj)
{
    ManagementExtension.ToDebug(obj);
    ManagementBaseObject instance = (ManagementBaseObject)obj["TargetInstance"];
    //if (instance.ClassPath.RelativePath != "Win32_PnPEntity") return;

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
