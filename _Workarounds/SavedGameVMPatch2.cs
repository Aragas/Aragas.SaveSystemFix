using HarmonyLib;

using System;
using System.Reflection;

using TaleWorlds.SaveSystem.Load;

namespace Aragas
{
	/// <summary>
	/// If we loaded the save via out patch, delay actual save loading by the game
	/// This is needed because we can't show an UI while executing something in a method that is calling it,
	/// so we are bypassing it.
	/// Basically our new Inquiry will decide if the save should be loaded by the game, not vanilla's Module mismatch.
	/// </summary>
    [HarmonyPatch]
	public class SavedGameVMPatch2
	{
		public static bool UserRequestedSaveLoading = false;
		public static LoadResult? LoadResult;

		static Assembly Assembly = Array.Find(AppDomain.CurrentDomain.GetAssemblies(), a => a.ManifestModule.Name == "SandBox.ViewModelCollection.dll");
		static MethodBase TargetMethod() => Assembly.GetType("SandBox.ViewModelCollection.SaveLoad.SavedGameVM").GetMethod("StartGame", BindingFlags.Instance | BindingFlags.NonPublic);

		public static bool Prefix(object __instance, LoadResult loadResult)
		{
			if (LoadContextPatch1.PostfixUsed)
			{
				UserRequestedSaveLoading = true;
				LoadResult = loadResult;
				return false;
			}
			else
			{
				UserRequestedSaveLoading = false;
				LoadResult = null;
				return true;
			}
		}
	}
}