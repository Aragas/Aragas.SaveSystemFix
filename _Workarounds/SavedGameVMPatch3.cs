using HarmonyLib;

using ReflectionMagic;

using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem.Load;

namespace Aragas
{
	/// <summary>
	/// Tried to make it all in one class, but for some reason this Inquiry will not trigger its delegates.
	/// </summary>
    //[HarmonyPatch]
	public class SavedGameVMPatch3
	{
		public static dynamic Instance;
		public static LoadResult LoadResult;
		public static bool Track;

		static Assembly Assembly = Array.Find(AppDomain.CurrentDomain.GetAssemblies(), a => a.ManifestModule.Name == "SandBox.ViewModelCollection.dll");
		static MethodBase TargetMethod() => Assembly.GetType("SandBox.ViewModelCollection.SaveLoad.SavedGameVM").GetMethod("StartGame", BindingFlags.Instance | BindingFlags.NonPublic);

		public static bool Prefix(object __instance, LoadResult loadResult)
		{
			if (LoadContextPatch1.PostfixUsed && !Track)
			{
				Track = true;
				Instance = __instance.AsDynamic();
				LoadResult = loadResult;

				InformationManager.ShowInquiry(
					new InquiryData(
						new TextObject("{=gJtTUYkm}Module missing", null).ToString(),
						new TextObject("{=ld2gh1uF}The save file is loaded without a module that contains custom saved data. Ignore the data and proceed anyway?", null).ToString(),
						true,
						true,
						new TextObject("{=aeouhelq}Yes", null).ToString(),
						new TextObject("{=8OkPHu4f}No", null).ToString(),
						Yes,
						No, ""),
					false);

				return false;
			}
			else
			{
				// Reset
				//Instance = null;
				//LoadResult = null;
				return true;
			}
		}

		private static void Yes()
		{
			LoadContextPatch1.PostfixUsed = false; // reset flag first so vanilla StartGame will trigger.
			Instance.StartGame(LoadResult);
			Track = false;

			// Reset
			//Instance = null;
			//LoadResult = null;
		}
		private static void No()
		{
			// Reset
			LoadContextPatch1.PostfixUsed = false;
			Track = false;
			//Instance = null;
			//LoadResult = null;
		}
	}
}