using System;
using System.Collections.Generic;
using XRL.UI;

namespace XRL.World.Parts
{
  [Serializable]
  public class LABYRINTHINETRAIL_PingAbility : IPart
  {
		public static readonly string NAME = "ping";
    public static readonly string COMMAND_NAME = "LABYRINTHINETRAIL_ActivateFindingFork";
    public Guid ActivatedAbilityID = Guid.Empty;
    [NonSerialized]
    private static List<GameObject> Forks = new List<GameObject>(32);

    public override void Initialize()
    {
      this.ActivatedAbilityID = this.AddMyActivatedAbility(
				Name: LABYRINTHINETRAIL_PingAbility.NAME,
				Command: LABYRINTHINETRAIL_PingAbility.COMMAND_NAME,
				Class: "Items",
				IsWorldMapUsable: true
			);
      base.Initialize();
    }

    public override void Remove()
    {
      this.RemoveMyActivatedAbility(ref this.ActivatedAbilityID);
      base.Remove();
    }

    public override bool WantEvent(int ID, int cascade) => base.WantEvent(ID, cascade) || ID == PooledEvent<CommandEvent>.ID;

    public override bool HandleEvent(CommandEvent E)
    {
      if (E.Command == LABYRINTHINETRAIL_PingAbility.COMMAND_NAME)
      {
				// TODO: For now, this is implemented without using a custom event as
        // I suspect changes to that system will be made in the future. When changes
        // are made to remove the necessity of overriding Dispatch as a workaround,
        // consider implementing this with events.
        // Searching for the part instead of the tag because we need objects that
        // have the FindingFork functionality.
        Predicate<GameObject> has_fork_part = item => item.HasPart<LABYRINTHINETRAIL_FindingFork>();
        // Only show the menu if the player doesn't have a fork equipped.
        if (!E.Actor.HasEquippedItem(has_fork_part))
        {
          List<GameObject> collection = E.Actor.Inventory.GetObjects(has_fork_part);
          if (collection == null || collection.Count <= 0)
            return E.Actor.Fail("You do not have any finding forks.");
          LABYRINTHINETRAIL_PingAbility.Forks.Clear();
          LABYRINTHINETRAIL_PingAbility.Forks.AddRange((IEnumerable<GameObject>) collection);
          if (LABYRINTHINETRAIL_PingAbility.Forks.Count > 1)
            LABYRINTHINETRAIL_PingAbility.Forks.Sort(new Comparison<GameObject>(this.SortForks));
          int DefaultSelected = 0;
          bool flag;
          do
          {
            GameObject gameObject = Popup.PickGameObject(
  						"Use which finding fork?",
  						(IList<GameObject>) LABYRINTHINETRAIL_PingAbility.Forks,
  						true,
  						ExtraLabels: new Func<GameObject, string>(this.LabelFork),
  						DefaultSelected: DefaultSelected
  					);
            if (gameObject == null)
              return false;
            DefaultSelected = LABYRINTHINETRAIL_PingAbility.Forks.IndexOf(gameObject);
            flag = gameObject.Twiddle();
            if (flag)
              return false;

            for (int index = LABYRINTHINETRAIL_PingAbility.Forks.Count - 1; index >= 0; --index)
            {
              if (LABYRINTHINETRAIL_PingAbility.Forks[index] == null || LABYRINTHINETRAIL_PingAbility.Forks[index].IsNowhere())
              {
                LABYRINTHINETRAIL_PingAbility.Forks.RemoveAt(index);
                if (DefaultSelected >= index)
                  --DefaultSelected;
              }
            }
          }
          while (!flag);
        }
      }
      return base.HandleEvent(E);
    }

    public override bool AllowStaticRegistration() => true;

    private string LabelFork(GameObject Object)
    {
      string str = Object.GetPart<LABYRINTHINETRAIL_FindingFork>()?.GetStatusSummary();
      if (str == "EMP" || str == "warming up")
        str = (string) null;
      return str;
    }

    private int SortForks(GameObject A, GameObject B)
    {
      int oneEquipped = (A.Equipped == null).CompareTo(B.Equipped == null);
      if (oneEquipped != 0)
        return oneEquipped;
			LABYRINTHINETRAIL_FindingFork forkPartA = A.GetPart<LABYRINTHINETRAIL_FindingFork>();
			LABYRINTHINETRAIL_FindingFork forkPartB = B.GetPart<LABYRINTHINETRAIL_FindingFork>();
			int oneNotNull = (forkPartA != null).CompareTo(forkPartB != null);
			if (oneNotNull != 0)
				return oneNotNull;
      int oneGaveReward = (forkPartA.GaveReward).CompareTo(forkPartB.GaveReward);
      if (oneGaveReward != 0)
        return oneGaveReward;
			return A.GetCachedDisplayNameForSort().CompareTo(B.GetCachedDisplayNameForSort());
    }
  }
}
