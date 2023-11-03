using System;

namespace XRL.World.Parts
{
  [Serializable]
  public class LABYRINTHINETRAIL_RivalCriticalDamage : IPart
  {
    public int PenetrationBonus = 0;

    public override bool WantEvent(int ID, int cascade) => base.WantEvent(ID, cascade) || ID == UnequippedEvent.ID;

    public override bool HandleEvent(UnequippedEvent E)
    {
      E.Item.RemovePart( this );
      return base.HandleEvent(E);
    }

    public override void Register(GameObject Object)
    {
      Object.RegisterPartEvent((IPart) this, "WeaponMissileWeaponHit");
      base.Register(Object);
    }

    public override bool FireEvent(Event E)
    {
      if (E.ID == "WeaponMissileWeaponHit" && E.HasFlag("Critical"))
      {
				int AV = E.GetGameObjectParameter("Defender").GetStat("AV").Value;
        E.SetParameter("Penetrations", AV + this.PenetrationBonus);
				E.SetParameter("PenetrationCap", AV + this.PenetrationBonus);
      }
      return base.FireEvent(E);
    }
  }
}
