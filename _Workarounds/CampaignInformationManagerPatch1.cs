using HarmonyLib;

using System.Collections.Generic;

using TaleWorlds.CampaignSystem;

namespace Aragas
{
	/// <summary>
	/// OLD WORKAROUND
	/// Keeping it as a reminder.
	///
	/// Any custom event will be written inside the global storage.
	/// If the event is no longer awailable, it will be listed as null
	/// </summary>
	//[HarmonyPatch(typeof(CampaignInformationManager), "OnGameLoaded")]
	public class CampaignInformationManagerPatch1
	{
		public static void Prefix(List<LogEntry> ____mapNotifications)
		{
			____mapNotifications.RemoveAll(m => m == null);
		}
	}
}