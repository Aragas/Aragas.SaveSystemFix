using HarmonyLib;

using System;

using TaleWorlds.MountAndBlade;

namespace Aragas.MountAndBlade
{
	public class SaveMissingModuleFixSubModule : MBSubModuleBase
	{
		public SaveMissingModuleFixSubModule()
		{
			try
			{
				var harmony = new Harmony("org.aragas.bannerlord.savesystemfix");
				harmony.PatchAll(typeof(SaveMissingModuleFixSubModule).Assembly);
			}
			catch (Exception ex)
			{
				// TODO: Find a logger
			}
		}
	}
}