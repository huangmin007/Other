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

## 可以有更多应用
```
Examples
```
