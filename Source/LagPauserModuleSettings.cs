namespace Celeste.Mod.LagPauser;

public class LagPauserModuleSettings : EverestModuleSettings
{
  [SettingName("LAGPAUSER_THRESHOLD")]
  [SettingSubText("LAGPAUSER_THRESHOLD_DESC")]
  [SettingRange(0, 3000, true)]
  public int ThresholdMs { get; set; } = 100;

  [SettingName("LAGPAUSER_COOLDOWN")]
  [SettingSubText("LAGPAUSER_COOLDOWN_DESC")]
  [SettingRange(0, 10000, true)]
  public int CooldownMs { get; set; } = 100;

  [SettingName("LAGPAUSER_RESPAWN_COOLDOWN")]
  [SettingSubText("LAGPAUSER_RESPAWN_COOLDOWN_DESC")]
  [SettingRange(0, 10000, true)]
  public int RespawnCooldownMs { get; set; } = 100;
}
