using System;
using XRL.World;

namespace LabyrinthineTrailExtensions
{
  public static class Extensions
  {
    public static GameObject FindCarriedItemWithTag(this GameObject go, string tag)
    {
      Predicate<GameObject> pred = item => item.HasTag(tag);
      if (go.Inventory.FindObject(pred) is GameObject item) return item;
      return go.FindEquippedItem(pred);
    }

    public static bool HasCarriedItemWithTag(this GameObject go, string tag) => go.FindCarriedItemWithTag(tag) != null;
  }
}
