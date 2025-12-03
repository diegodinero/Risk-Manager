// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using System;
using System.Collections.Generic;
using System.Drawing;
using TradingPlatform.BusinessLayer;
using TradingPlatform.PresentationLayer.Plugins;

namespace Risk_Manager
{
    /// <summary>
    /// An example of blank indicator. Add your code, compile it and use on the charts in the assigned trading terminal.
    /// Information about API you can find here: http://api.quantower.com
    /// Code samples: https://github.com/Quantower/Examples
    /// </summary>
	public class Risk_Manager : Plugin
    {
        /// <summary>
        /// Indicator's constructor. Contains general information: name, description, LineSeries etc. 
        /// </summary>
        public static PluginInfo GetInfo()
        {
            return new PluginInfo
            {
                Name = "RiskManagerPlugin",
                Title = loc.key("Risk Manager Overview"),
                Group = PluginGroup.Portfolio,
                ShortName = "RMMBS",
                SortIndex = 35,
                WindowParameters = new NativeWindowParameters(NativeWindowParameters.Panel)
                {
                    BrowserUsageType = BrowserUsageType.None
                },
                CustomProperties = new Dictionary<string, object>
                {
                    { PluginInfo.Const.ALLOW_MANUAL_CREATION, true }
                }
            };
        }

        /// <summary>
        /// Initialize called once on plugin creation
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
        }

        public override void Populate(PluginParameters args = null)
        {
            base.Populate(args);
            //PerformFullUpdate();
        }
    }
}
