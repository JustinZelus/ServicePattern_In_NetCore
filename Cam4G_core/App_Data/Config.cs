using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cam4G_core.App_Data
{
	public class Config
	{
		private static IConfiguration _configure { get; set; }
		private static Config instance { get; set; }

		private Config()
		{

		}

		public static void Initial(IConfiguration configure)
		{
			_configure = configure;
		}

		public static Config GetInstance()
		{
			if (instance == null)
			{
				instance = new Config();
			}
			return instance;
		}

		public static string GetSetting(string ConfigName)
		{
			return _configure.GetValue<string>(ConfigName);
		}

		public static double GetSettingDVal(string ConfigName)
		{
			return _configure.GetValue<double>(ConfigName);
		}

		public static bool GetSettingBoolVal(string ConfigName)
		{
			return _configure.GetValue<bool>(ConfigName);
		}

		public static IConfigurationSection GetSection(string sectionName)
		{
			if (string.IsNullOrEmpty(sectionName)) return null;
			IConfigurationSection myArraySection = _configure.GetSection(sectionName);
			return myArraySection;
		}

		public static IConfiguration GetSession(string SessionName)
		{
			return _configure.GetSection(SessionName);
		}

	}
}
