using ReflectionMagic;

using System;
using System.Collections;
using System.Reflection;

using TaleWorlds.SaveSystem;

namespace Aragas
{
	/// <summary>
	/// Lets hope that the game is not expecting null values in a list.
	/// 
	/// </summary>
	//[HarmonyPatch] // We call it manually for now
	public class ContainerLoadDataPatch1
	{
		static MethodBase TargetMethod() => typeof(LoadData).Assembly.GetType("TaleWorlds.SaveSystem.Load.ContainerLoadData").GetMethod("FillObject");

		public static void Prefix(object __instance)
		{
			var @dynamic = __instance.AsDynamic();

			foreach (object objectLoadData in @dynamic._childStructs.Values)
			{
				var objectLoadDataDynamic = objectLoadData.AsDynamic();
				objectLoadDataDynamic.FillObject();
			}
			for (var i = 0; i < (int) DynamicHelper.Unwrap(@dynamic._elementCount); i++)
			{
				switch (Convert.ToInt32(DynamicHelper.Unwrap(@dynamic._containerType)))
				{
					case 1: // List
						{
							var list = (IList) DynamicHelper.Unwrap(@dynamic.Target);
							if (!IsNull(list)) // bmountney: Added test for NULL
							{
								var value = ((IList) DynamicHelper.Unwrap(@dynamic._values))[i].AsDynamic();
								if (Convert.ToInt32(DynamicHelper.Unwrap(value.SavedMemberType)) == 4) // CustomStruct
								{
									var objectLoadData = ((IDictionary) DynamicHelper.Unwrap(@dynamic._childStructs))[(int) DynamicHelper.Unwrap(value.Data)].AsDynamic();
									value.SetCustomStructData(DynamicHelper.Unwrap(objectLoadData.Target));
								}
								object valueData = DynamicHelper.Unwrap(value.GetDataToUse());
								// PATCH
								if (!IsNull(valueData))
								{
									try // bmountney: Added try...catch
									{
										list.Add(valueData);
									}
									catch (System.ArgumentException)
									{
									}
								}
							}
							break;
						}
					case 2: // Dictionary
						{
							var dictionary = (IDictionary) DynamicHelper.Unwrap(@dynamic.Target);
							if (!IsNull(dictionary)) // bmountney: Added test for NULL
							{
								var key = ((IList) DynamicHelper.Unwrap(@dynamic._keys))[i].AsDynamic();
								var value = ((IList) DynamicHelper.Unwrap(@dynamic._values))[i].AsDynamic();
								if (Convert.ToInt32(DynamicHelper.Unwrap(key.SavedMemberType)) == 4) // CustomStruct
								{
									var objectLoadData = ((IDictionary) DynamicHelper.Unwrap(@dynamic._childStructs))[(int) DynamicHelper.Unwrap(key.Data)].AsDynamic();
									key.SetCustomStructData(DynamicHelper.Unwrap(objectLoadData.Target));
								}
								if (Convert.ToInt32(DynamicHelper.Unwrap(value.SavedMemberType)) == 4) // CustomStruct
								{
									var objectLoadData = ((IDictionary) DynamicHelper.Unwrap(@dynamic._childStructs))[(int) DynamicHelper.Unwrap(value.Data)].AsDynamic();
									value.SetCustomStructData(DynamicHelper.Unwrap(objectLoadData.Target));
								}
								object keyData = DynamicHelper.Unwrap(key.GetDataToUse());
								object valueData = DynamicHelper.Unwrap(value.GetDataToUse());
								if (!IsNull(keyData) && !IsNull(valueData))
									dictionary.Add(keyData, valueData);
							}
							break;
						}
					case 3: // Array
						{
							var array = (Array) DynamicHelper.Unwrap(@dynamic.Target);
							var value = ((IList) DynamicHelper.Unwrap(@dynamic._values))[i].AsDynamic();
							if (Convert.ToInt32(DynamicHelper.Unwrap(value.SavedMemberType)) == 4) // CustomStruct
							{
								var objectLoadData = ((IDictionary) DynamicHelper.Unwrap(@dynamic._childStructs))[(int) DynamicHelper.Unwrap(value.Data)].AsDynamic();
								value.SetCustomStructData(DynamicHelper.Unwrap(objectLoadData.Target));
							}
							var valueData = DynamicHelper.Unwrap(value.GetDataToUse());
							if (!IsNull(valueData))
								array.SetValue(valueData, i);
							break;
						}
					case 4: // Queue
						{
							var collection = (ICollection) DynamicHelper.Unwrap(@dynamic.Target);
							var value = ((IList) DynamicHelper.Unwrap(@dynamic._values))[i].AsDynamic();
							if (Convert.ToInt32(DynamicHelper.Unwrap(value.SavedMemberType)) == 4) // CustomStruct
							{
								var objectLoadData = ((IDictionary) DynamicHelper.Unwrap(@dynamic._childStructs))[(int) DynamicHelper.Unwrap(value.Data)].AsDynamic();
								value.SetCustomStructData(DynamicHelper.Unwrap(objectLoadData.Target));
							}
							var valueData = DynamicHelper.Unwrap(value.GetDataToUse());
							if (!IsNull(valueData))
								collection.GetType().GetMethod("Enqueue").Invoke(collection, new object[] { valueData });
							break;
						}
				}
			}
		}

		private static bool IsNull(object x)
		{
			return x == null;
		}
	}
}