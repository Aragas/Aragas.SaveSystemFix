//#define REMOVE_INVALID
//#define SKIP_INVALID

using HarmonyLib;

using ReflectionMagic;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace Aragas
{
	/// <summary>
	/// Will be executed if the original save fails
	/// Basically, here is the vanilla execution flow except applied patches
	/// While using IL injection would be faster and better, for both easier debugging and overall understanding of code
	/// ReflectionMagic was chosen
	/// While the method should be executing slower, in theory, user should cann it only once.
	/// When the save is loaded using this patch, the result will be a 'clean' vanilla save that won't reuqire this anymore.
	/// </summary>
	[HarmonyPatch]
	public class LoadContextPatch1
	{
		public static bool PostfixUsed;

		private static Type GetType(string type) => typeof(LoadData).Assembly.GetType($"TaleWorlds.SaveSystem.{type}");
		public static MethodBase TargetMethod() => GetType("Load.LoadContext").GetMethod("Load");

		public static void Postfix(object __instance, ref bool __result, LoadData loadData)
		{
			// If the save is loaded withou any problem by vanilla
			if (__result)
			{
				PostfixUsed = false;
				return;
			}

			try
			{
				var @dynamic = __instance.AsDynamic();

				using (new PerformanceTestBlock("LoadContext::Load Headers"))
				{
					using (new PerformanceTestBlock("LoadContext::Load And Create Header"))
					{
						var archiveDeserializer = Activator.CreateInstance(GetType("ArchiveDeserializer")).AsDynamic();
						archiveDeserializer.LoadFrom(loadData.GameData.Header);
						var headerRootFolder = archiveDeserializer.RootFolder;
						BinaryReader binaryReader = headerRootFolder.GetEntry(Activator.CreateInstance(GetType("EntryId"), new object[] { -1, Enum.ToObject(GetType("SaveEntryExtension"), 0x7) })).GetBinaryReader();
						@dynamic._objectCount = binaryReader.ReadInt();
						@dynamic._stringCount = binaryReader.ReadInt();
						@dynamic._containerCount = binaryReader.ReadInt();
						@dynamic._objectHeaderLoadDatas = Activator.CreateInstance(GetType("Load.ObjectHeaderLoadData[]"), new object[] { (int) DynamicHelper.Unwrap(@dynamic._objectCount) });
						@dynamic._containerHeaderLoadDatas = Activator.CreateInstance(GetType("Load.ContainerHeaderLoadData[]"), new object[] { (int) DynamicHelper.Unwrap(@dynamic._containerCount) });
						@dynamic._strings = new string[(int) DynamicHelper.Unwrap(@dynamic._stringCount)];
						Parallel.For(0, (int) DynamicHelper.Unwrap(@dynamic._objectCount), (int i) =>
						{
							var objectHeaderLoadData = Activator.CreateInstance(GetType("Load.ObjectHeaderLoadData"), new object[] { __instance, i }).AsDynamic();
							var childFolder = headerRootFolder.GetChildFolder(Activator.CreateInstance(GetType("FolderId"), new object[] { i, Enum.ToObject(GetType("SaveFolderExtension"), 0x1) }));
							objectHeaderLoadData.InitialieReaders(DynamicHelper.Unwrap(childFolder));
							((IList) DynamicHelper.Unwrap(@dynamic._objectHeaderLoadDatas))[i] = DynamicHelper.Unwrap(objectHeaderLoadData);
						});
						Parallel.For(0, (int) DynamicHelper.Unwrap(@dynamic._containerCount), (int i) =>
						{
							var containerHeaderLoadData = Activator.CreateInstance(GetType("Load.ContainerHeaderLoadData"), new object[] { __instance, i }).AsDynamic();
							var childFolder = headerRootFolder.GetChildFolder(Activator.CreateInstance(GetType("FolderId"), new object[] { i, Enum.ToObject(GetType("SaveFolderExtension"), 0x3) }));
							containerHeaderLoadData.InitialieReaders(DynamicHelper.Unwrap(childFolder));
							((IList) DynamicHelper.Unwrap(@dynamic._containerHeaderLoadDatas))[i] = DynamicHelper.Unwrap(containerHeaderLoadData);
						});
					}
					using (new PerformanceTestBlock("LoadContext::Create Objects"))
					{
#if REMOVE_INVALID
						// PATCH
						var set1 = new HashSet<object>();
						var set2 = new HashSet<object>();
						// PATCH
#endif
						foreach (object objectHeaderLoadData in @dynamic._objectHeaderLoadDatas)
						{
							var objectHeaderLoadDataDynamic = objectHeaderLoadData.AsDynamic();
							objectHeaderLoadDataDynamic.CreateObject();

							if (objectHeaderLoadDataDynamic.Id == 0)
								@dynamic.RootObject = DynamicHelper.Unwrap(objectHeaderLoadDataDynamic.Target);

#if REMOVE_INVALID
							// PATCH
							if (objectHeaderLoadDataDynamic.TypeDefinition == null)
								set1.Add(objectHeaderLoadData);
							// PATCH
#endif
						}
						foreach (object containerHeaderLoadData in @dynamic._containerHeaderLoadDatas)
						{
							var containerHeaderLoadDataDynamic = containerHeaderLoadData.AsDynamic();
							if (containerHeaderLoadDataDynamic.GetObjectTypeDefinition())
								containerHeaderLoadDataDynamic.CreateObject();

#if REMOVE_INVALID
							// PATCH
							if (containerHeaderLoadDataDynamic.TypeDefinition == null)
								set2.Add(containerHeaderLoadData);
							// PATCH
#endif
						}
#if REMOVE_INVALID
						// PATCH
						foreach (var item in set1)
							((IList) DynamicHelper.Unwrap(@dynamic._objectHeaderLoadDatas)).Remove(item);
						@dynamic._objectCount -= set1.Count;
						foreach (var item in set2)
							((IList) DynamicHelper.Unwrap(@dynamic._containerHeaderLoadDatas)).Remove(item);
						@dynamic._containerCount -= set2.Count;
						// PATCH
#endif
					}
				}
				GC.Collect();
				GC.WaitForPendingFinalizers();
				using (new PerformanceTestBlock("LoadContext::Load Strings"))
				{
					var archiveDeserializer2 = Activator.CreateInstance(GetType("ArchiveDeserializer")).AsDynamic();
					archiveDeserializer2.LoadFrom(loadData.GameData.Strings);
					for (var j = 0; j < (int) @dynamic._stringCount; j++)
					{
						var method = GetType("Load.LoadContext").GetMethod("LoadString", BindingFlags.NonPublic | BindingFlags.Static);
						((IList) DynamicHelper.Unwrap(@dynamic._strings))[j] = (string) method.Invoke(null, new object[] { DynamicHelper.Unwrap(archiveDeserializer2), j });
					}
				}
				GC.Collect();
				GC.WaitForPendingFinalizers();
				using (new PerformanceTestBlock("LoadContext::Load Object Datas"))
				{
					Parallel.For(0, (int) DynamicHelper.Unwrap(@dynamic._objectCount), (int i) =>
					{
						var archiveDeserializer2 = Activator.CreateInstance(GetType("ArchiveDeserializer")).AsDynamic();
						archiveDeserializer2.LoadFrom(loadData.GameData.ObjectData[i]);
						var rootFolder = archiveDeserializer2.RootFolder;
						var objectLoadData = Activator.CreateInstance(GetType("Load.ObjectLoadData"), new object[] { ((IList) DynamicHelper.Unwrap(@dynamic._objectHeaderLoadDatas))[i] }).AsDynamic();
						var childFolder = rootFolder.GetChildFolder(Activator.CreateInstance(GetType("FolderId"), new object[] { i, Enum.ToObject(GetType("SaveFolderExtension"), 0x1) }));
#if SKIP_INVALID
						// PATCH
						if (objectLoadData.TypeDefinition == null)
						{
							objectLoadData.Id = -1;
							return;
						}
						// PATCH
#endif
						try
						{
							objectLoadData.InitializeReaders(DynamicHelper.Unwrap(childFolder)); // bmountney: Added "z" to correct method name
						}
						catch (System.MissingMethodException) // bmountney: If method with new name doesn't exist, try the old name for e1.1.2
						{
							objectLoadData.InitialieReaders(DynamicHelper.Unwrap(childFolder));
						}
						objectLoadData.FillCreatedObject();
						objectLoadData.Read();
						objectLoadData.FillObject();
					});
				}
				using (new PerformanceTestBlock("LoadContext::Load Container Datas"))
				{
					Parallel.For(0, (int) DynamicHelper.Unwrap(@dynamic._containerCount), (int i) =>
					{
						var binaryArchive = loadData.GameData.ContainerData[i];
						var archiveDeserializer3 = Activator.CreateInstance(GetType("ArchiveDeserializer")).AsDynamic();
						archiveDeserializer3.LoadFrom(binaryArchive);
						var rootFolder = archiveDeserializer3.RootFolder;
						var containerLoadData = Activator.CreateInstance(GetType("Load.ContainerLoadData"), new object[] { ((IList) @dynamic._containerHeaderLoadDatas.RealObject)[i] }).AsDynamic();
						var childFolder = rootFolder.GetChildFolder(Activator.CreateInstance(GetType("FolderId"), new object[] { i, Enum.ToObject(GetType("SaveFolderExtension"), 0x3) }));
#if SKIP_INVALID
						// PATCH
						if (containerLoadData.TypeDefinition == null)
						{
							containerLoadData.Id = -1;
							return;
						}
						// PATCH
#endif

						try
						{
							containerLoadData.InitializeReaders(DynamicHelper.Unwrap(childFolder)); // bmountney: Added "z" to correct method name
						}
						catch (System.MissingMethodException) // bmountney: If method with new name doesn't exist, try the old name for e1.1.2
						{
							containerLoadData.InitialieReaders(DynamicHelper.Unwrap(childFolder));
						}
						containerLoadData.FillCreatedObject();
						containerLoadData.Read();
						ContainerLoadDataPatch1.Prefix(DynamicHelper.Unwrap(containerLoadData));
					});
				}
				using (new PerformanceTestBlock("LoadContext::Callbacks"))
				{
					try // bmountney: Added to deal with System.Reflection.TargetInvocationException in beta e1.3.0 of the game after removing some mods
					{
						foreach (object objectHeaderLoadData2 in @dynamic._objectHeaderLoadDatas)
						{
							var objectHeaderLoadData2Dynamic = objectHeaderLoadData2.AsDynamic();
							// PATCH
							if (objectHeaderLoadData2Dynamic.TypeDefinition == null)
								continue;
							// PATCH

							foreach (MethodInfo methodInfo in objectHeaderLoadData2Dynamic.TypeDefinition.InitializationCallbacks)
							{
								// bmountney: In e1.3.0 this method was throwing a System.Reflection.TargetInvocationExecption at some point
								//		during the loop, and once it started happening it seemed to repeat indefinitely, so attempting to catch
								//		it inside the either the inner or outer loop seemed to result in an infinite loop, or at least it was
								//		taking longer than I was willing to wait.  Jumping out of the loops on the first instance of the exception
								//		seemed to cause no issues in my tests after removing the "Tournaments XPanded-for-BL1.3.0" mod.
								methodInfo.Invoke(DynamicHelper.Unwrap(objectHeaderLoadData2Dynamic.Target), new object[]
								{
								loadData.MetaData
								});
							}
						}
					}
					catch (System.Reflection.TargetInvocationException)
					{
					}
				}

				GC.Collect();
				GC.WaitForPendingFinalizers();
				__result = true;

				PostfixUsed = true;
			}
			catch
			{
				__result = false;
				PostfixUsed = false;
			}
		}
	}
}