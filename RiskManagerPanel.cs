using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using TradingPlatform.PresentationLayer.Plugins;

namespace Risk_Manager
{
    public class RiskManagerPanel : Plugin
    {
        private RiskManagerControl _control;

        public static PluginInfo GetInfo()
        {
            return new PluginInfo
            {
                Name = "RiskManager.Panel",
                Title = loc.key("Risk Manager UI"),
                Group = PluginGroup.Portfolio,
                ShortName = "RMMUI",
                SortIndex = 34,
                // Request a host window (with chrome) instead of an embedded panel
                WindowParameters = new NativeWindowParameters(NativeWindowParameters.Window)
                {
                    BrowserUsageType = BrowserUsageType.None,
                    WindowStyle = NativeWindowStyle.SingleBorderWindow,
                    ResizeMode = NativeResizeMode.CanResizeWithGrip,
                    AllowActionsButton = true,
                    AllowCloseButton = true,
                    AllowMaximizeButton = true,
                    AllowFullScreenButton = true
                },
                CustomProperties = new System.Collections.Generic.Dictionary<string, object>
                {
                    { PluginInfo.Const.ALLOW_MANUAL_CREATION, true },
                    // Request larger default window (adjust Width/Height as needed)
                    { "DefaultWidth", 1200 },
                    { "DefaultHeight", 800 }
                }
            };
        }

        public override void Initialize()
        {
            base.Initialize();
            
            // Log window parameters to verify they are applied correctly
            try
            {
                var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                var log = System.IO.Path.Combine(desktop, "RiskManagerPanel_window_params_log.txt");
                var info = GetInfo();
                System.IO.File.AppendAllText(log, $"[{DateTime.Now:O}] Initialize called{Environment.NewLine}");
                System.IO.File.AppendAllText(log, $"WindowParameters Type: {info.WindowParameters?.GetType().Name ?? "null"}{Environment.NewLine}");
                
                if (info.WindowParameters is NativeWindowParameters nwp)
                {
                    System.IO.File.AppendAllText(log, $"  AllowCloseButton: {nwp.AllowCloseButton}{Environment.NewLine}");
                    System.IO.File.AppendAllText(log, $"  AllowMaximizeButton: {nwp.AllowMaximizeButton}{Environment.NewLine}");
                    System.IO.File.AppendAllText(log, $"  AllowFullScreenButton: {nwp.AllowFullScreenButton}{Environment.NewLine}");
                    System.IO.File.AppendAllText(log, $"  AllowActionsButton: {nwp.AllowActionsButton}{Environment.NewLine}");
                    System.IO.File.AppendAllText(log, $"  WindowStyle: {nwp.WindowStyle}{Environment.NewLine}");
                    System.IO.File.AppendAllText(log, $"  ResizeMode: {nwp.ResizeMode}{Environment.NewLine}");
                }
                System.IO.File.AppendAllText(log, Environment.NewLine);
            }
            catch { /* Ignore logging errors */ }
        }

        public override void Populate(PluginParameters args = null)
        {
            base.Populate(args);

            try
            {
                var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                var log = System.IO.Path.Combine(desktop, "RiskManagerPanel_attach_log.txt");
                System.IO.File.AppendAllText(log, $"Populate called at {DateTime.Now:O}{Environment.NewLine}");

                if (_control == null)
                    _control = new RiskManagerControl { Dock = DockStyle.Fill };

                bool attached = AttachControlToHostWithLogging(_control, log);
                System.IO.File.AppendAllText(log, $"Attach result: {attached}{Environment.NewLine}{Environment.NewLine}");

                try { MessageBox.Show("RiskManagerPanel.Populate executed. Check Desktop log: RiskManagerPanel_attach_log.txt", "Risk Manager - Debug"); }
                catch { /* hosts may prevent UI */ }
            }
            catch (Exception ex)
            {
                try
                {
                    var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    var log = System.IO.Path.Combine(desktop, "RiskManagerPanel_attach_log.txt");
                    System.IO.File.AppendAllText(log, $"Populate exception: {ex}{Environment.NewLine}");
                }
                catch { }
            }
        }

        private bool AttachControlToHostWithLogging(Control control, string logPath)
        {
            try
            {
                var baseType = this.GetType().BaseType;
                System.IO.File.AppendAllText(logPath, $"Plugin base type: {baseType?.FullName}{Environment.NewLine}");

                // Try native window first
                try
                {
                    var windowProp = baseType?.GetProperty("Window", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (windowProp != null)
                    {
                        var nativeWindow = windowProp.GetValue(this);
                        System.IO.File.AppendAllText(logPath, $"Native window instance: {nativeWindow?.GetType().FullName ?? "null"}{Environment.NewLine}");

                        if (nativeWindow != null)
                        {
                            var nwType = nativeWindow.GetType();

                            // If native window exposes a writable Content property, try to set a WindowsFormsHost
                            var contentProp = nwType.GetProperty("Content", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            if (contentProp != null && contentProp.CanWrite)
                            {
                                System.IO.File.AppendAllText(logPath, "NativeWindow.Content is writable - attempting WindowsFormsHost wrapper..." + Environment.NewLine);

                                try
                                {
                                    var host = new WindowsFormsHost();
                                    host.Child = control;
                                    contentProp.SetValue(nativeWindow, host);
                                    System.IO.File.AppendAllText(logPath, "Attached via NativeWindow.Content = WindowsFormsHost(Child = control)" + Environment.NewLine);
                                    return true;
                                }
                                catch (Exception ex)
                                {
                                    System.IO.File.AppendAllText(logPath, $"WindowsFormsHost attach failed: {ex.GetType().Name}: {ex.Message}{Environment.NewLine}");
                                }
                            }
                            else
                            {
                                System.IO.File.AppendAllText(logPath, "NativeWindow.Content property not writable or not found." + Environment.NewLine);
                            }

                            var addChild = nwType.GetMethod("AddChild", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            if (addChild != null)
                            {
                                System.IO.File.AppendAllText(logPath, "Attempting nativeWindow.AddChild with WindowsFormsHost..." + Environment.NewLine);
                                try
                                {
                                    var host = new WindowsFormsHost();
                                    host.Child = control;
                                    addChild.Invoke(nativeWindow, new object[] { host });
                                    System.IO.File.AppendAllText(logPath, "Attached via NativeWindow.AddChild(WindowsFormsHost)" + Environment.NewLine);
                                    return true;
                                }
                                catch (Exception ex)
                                {
                                    System.IO.File.AppendAllText(logPath, $"NativeWindow.AddChild attach failed: {ex.GetType().Name}: {ex.Message}{Environment.NewLine}");
                                }
                            }
                        }
                    }
                    else
                    {
                        System.IO.File.AppendAllText(logPath, "Window property not found on plugin base type." + Environment.NewLine);
                    }
                }
                catch (Exception ex)
                {
                    System.IO.File.AppendAllText(logPath, $"NativeWindow attach attempt error: {ex.GetType().Name}: {ex.Message}{Environment.NewLine}");
                }

                // Fallbacks: try plugin methods/properties that accept object
                var candidateMethodNames = new[]
                {
                    "SetContent", "SetControl", "AddControl", "AddPanel", "CreatePanel",
                    "CreateControl", "SetView", "SetContentControl", "SetContentPanel", "SetPanel"
                };

                foreach (var name in candidateMethodNames)
                {
                    try
                    {
                        var mi = baseType?.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (mi == null)
                        {
                            System.IO.File.AppendAllText(logPath, $"Method not found: {name}{Environment.NewLine}");
                            continue;
                        }

                        var parameters = mi.GetParameters();
                        System.IO.File.AppendAllText(logPath, $"Trying method {name} with {parameters.Length} params...{Environment.NewLine}");

                        if (parameters.Length == 1 &&
                            (parameters[0].ParameterType.IsAssignableFrom(typeof(object))))
                        {
                            try
                            {
                                var host = new WindowsFormsHost();
                                host.Child = control;
                                mi.Invoke(this, new object[] { host });
                                System.IO.File.AppendAllText(logPath, $"Attached via {name}(WindowsFormsHost){Environment.NewLine}");
                                return true;
                            }
                            catch (Exception ex)
                            {
                                System.IO.File.AppendAllText(logPath, $"Method {name} invocation failed with WindowsFormsHost: {ex.GetType().Name}: {ex.Message}{Environment.NewLine}");
                                try
                                {
                                    mi.Invoke(this, new object[] { control });
                                    System.IO.File.AppendAllText(logPath, $"Attached via {name}(raw Control){Environment.NewLine}");
                                    return true;
                                }
                                catch (Exception ex2)
                                {
                                    System.IO.File.AppendAllText(logPath, $"Method {name} invocation failed with raw control: {ex2.GetType().Name}: {ex2.Message}{Environment.NewLine}");
                                }
                            }
                        }

                        if (parameters.Length == 2 && parameters[0].ParameterType.IsAssignableFrom(typeof(object)))
                        {
                            object second = parameters[1].ParameterType == typeof(bool) ? (object)true : null;
                            try
                            {
                                var host = new WindowsFormsHost();
                                host.Child = control;
                                mi.Invoke(this, new object[] { host, second });
                                System.IO.File.AppendAllText(logPath, $"Attached via {name}(WindowsFormsHost, second){Environment.NewLine}");
                                return true;
                            }
                            catch (Exception ex)
                            {
                                System.IO.File.AppendAllText(logPath, $"Method {name} (2 params) failed: {ex.GetType().Name}: {ex.Message}{Environment.NewLine}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.IO.File.AppendAllText(logPath, $"Method {name} invocation error: {ex.GetType().Name}: {ex.Message}{Environment.NewLine}");
                    }
                }

                var candidatePropNames = new[] { "Content", "Control", "Panel", "View", "ContentControl" };
                foreach (var pname in candidatePropNames)
                {
                    try
                    {
                        var pi = baseType?.GetProperty(pname, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (pi == null)
                        {
                            System.IO.File.AppendAllText(logPath, $"Property not found: {pname}{Environment.NewLine}");
                            continue;
                        }

                        System.IO.File.AppendAllText(logPath, $"Trying property {pname} (CanWrite={pi.CanWrite})...{Environment.NewLine}");
                        if (pi.CanWrite && (pi.PropertyType.IsAssignableFrom(typeof(object))))
                        {
                            try
                            {
                                var host = new WindowsFormsHost();
                                host.Child = control;
                                pi.SetValue(this, host);
                                System.IO.File.AppendAllText(logPath, $"Attached via property {pname} (WindowsFormsHost){Environment.NewLine}");
                                return true;
                            }
                            catch (Exception ex)
                            {
                                System.IO.File.AppendAllText(logPath, $"Property {pname} set failed with host: {ex.GetType().Name}: {ex.Message}{Environment.NewLine}");
                                try
                                {
                                    pi.SetValue(this, control);
                                    System.IO.File.AppendAllText(logPath, $"Attached via property {pname} (raw control){Environment.NewLine}");
                                    return true;
                                }
                                catch (Exception ex2)
                                {
                                    System.IO.File.AppendAllText(logPath, $"Property {pname} set failed with raw control: {ex2.GetType().Name}: {ex2.Message}{Environment.NewLine}");
                                }
                            }
                        }
                        System.IO.File.AppendAllText(logPath, $"Property {pname} exists but not writable or wrong type.{Environment.NewLine}");
                    }
                    catch (Exception ex)
                    {
                        System.IO.File.AppendAllText(logPath, $"Property {pname} set error: {ex.GetType().Name}: {ex.Message}{Environment.NewLine}");
                    }
                }

                System.IO.File.AppendAllText(logPath, "No suitable attach method/property found." + Environment.NewLine);
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(logPath, $"AttachControlToHostWithLogging exception: {ex}{Environment.NewLine}");
            }

            return false;
        }
    }
}