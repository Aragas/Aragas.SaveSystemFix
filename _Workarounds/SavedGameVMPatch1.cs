using HarmonyLib;

using ReflectionMagic;

using System;
using System.Reflection;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Aragas
{
	/// <summary>
	/// After ExecuteSaveLoad is invoked, we check if we should display the message about missing modules
	/// And display it if needed
	/// And execute StartGame if user is willing to
	/// </summary>
	[HarmonyPatch]
	public class SavedGameVMPatch1
	{
		static Assembly Assembly = Array.Find(AppDomain.CurrentDomain.GetAssemblies(), a => a.ManifestModule.Name == "SandBox.ViewModelCollection.dll");
		static MethodBase TargetMethod() => Assembly.GetType("SandBox.ViewModelCollection.SaveLoad.SavedGameVM").GetMethod("ExecuteSaveLoad");

		public static void Postfix(object __instance)
		{
			var @dynamic = __instance.AsDynamic();

			if ((bool)DynamicHelper.Unwrap(@dynamic._isSaving))
				return;

			Task.Delay(1000).Wait();
			if (LoadContextPatch1.PostfixUsed)
			{
				InformationManager.ShowInquiry(
					new InquiryData(
						new TextObject("{=gJtTUYkm}Module missing", null).ToString(),
						new TextObject("{=ld2gh1uF}The save file is loaded without a module that contains custom saved data. Ignore the data and proceed anyway?", null).ToString(),
						true,
						true,
						new TextObject("{=aeouhelq}Yes", null).ToString(),
						new TextObject("{=8OkPHu4f}No", null).ToString(),
						() =>
						{
							if (SavedGameVMPatch2.UserRequestedSaveLoading == true)
							{
								LoadContextPatch1.PostfixUsed = false; // reset flag so StartGame will trigger.
								@dynamic.StartGame(SavedGameVMPatch2.LoadResult);
								// Flags will be reset by Prefix
							}

						},
						() =>
						{
							// Reset all flags
							LoadContextPatch1.PostfixUsed = false;
							SavedGameVMPatch2.UserRequestedSaveLoading = false;
							SavedGameVMPatch2.LoadResult = null;
						}, ""), false);
			}
		}
	}
}