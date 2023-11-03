using System;

namespace XRL.World.Parts
{
  [Serializable]
  public class LABYRINTHINETRAIL_RivalCritical : IPart
  {
    public int PenetrationBonus = 0;

    public override bool WantEvent(int ID, int cascade)
    {
      return base.WantEvent(ID, cascade)
      || ID == GetCriticalThresholdEvent.ID
      || ID == GetShortDescriptionEvent.ID
      || ID == EquipperEquippedEvent.ID
      || ID == UnequippedEvent.ID;
    }

    public override bool HandleEvent(EquipperEquippedEvent E)
    {
      if (
        E.Item.HasPart("MissileWeapon")
        && (E.Item.GetPart("MissileWeapon") as MissileWeapon ).Skill == "Pistol"
      ){
        E.Item.AddPart<LABYRINTHINETRAIL_RivalCriticalDamage>();
      }
      return base.HandleEvent(E);
    }

    public override bool HandleEvent(GetCriticalThresholdEvent E)
    {
      if (E.Attacker == this.ParentObject && E.Skill == "Pistol")
				E.Threshold -= this.ParentObject.Statistics["Agility"].Modifier;
      return base.HandleEvent(E);
    }

    public override bool HandleEvent(GetShortDescriptionEvent E)
    {
      int CriticalBonus = 5 * this.ParentObject.Statistics["Agility"].Modifier;
      E.Postfix.AppendRules(
        "This creature is "
        + CriticalBonus.ToString()
        + "% more likely to score critical hits with pistols and critical hits with pistols have a PV equal to their target's AV."
      );
      return base.HandleEvent(E);
    }

    public override void Register(GameObject Object)
    {
      Object.RegisterPartEvent((IPart) this, "MissileAttackerCriticalHit");
      base.Register(Object);
    }

    public override bool FireEvent(Event E)
    {
      if (E.ID == "MissileAttackerCriticalHit")
      {
        int AV = E.GetGameObjectParameter("Defender").GetStat("AV").Value;
        E.SetParameter("Penetrations", AV + this.PenetrationBonus);
				E.SetParameter("PenetrationCap", AV + this.PenetrationBonus);
      }
      return base.FireEvent(E);
    }
  }
}
