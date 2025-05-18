namespace Celeste.Mod.LagPauser;

public class LagPauserModuleSettings : EverestModuleSettings
{
  [SettingName("LAGPAUSER_THRESHOLD")]
  [SettingSubText("LAGPAUSER_THRESHOLD_DESC")]
  [SettingRange(0, 3000, true)]
  public int ThresholdMs { get; set; } = 100;

  public int CooldownMs { get; set; } = 0;
  public int RespawnCooldownMs { get; set; } = 0;
  public int TransitionCooldownMs { get; set; } = 0;


  public void CreateCooldownMsEntry(TextMenu menu, bool inGame)
  {
    TextMenu.Item menuItem;

    menu.Add(menuItem = new TextMenu.Slider(
      Dialog.Clean("LAGPAUSER_COOLDOWN"),
      i => $"{i * 100}",
      0, 100,
      CooldownMs / 100
    ).Change(i => CooldownMs = i * 100));

    menuItem.AddDescription(menu, Dialog.Clean("LAGPAUSER_COOLDOWN_DESC"));
  }

  public void CreateRespawnCooldownMsEntry(TextMenu menu, bool inGame)
  {
    TextMenu.Item menuItem;

    menu.Add(menuItem = new TextMenu.Slider(
      Dialog.Clean("LAGPAUSER_RESPAWN_COOLDOWN"),
      i => $"{i * 100}",
      0, 100,
      RespawnCooldownMs / 100
    ).Change(i => RespawnCooldownMs = i * 100));

    menuItem.AddDescription(menu, Dialog.Clean("LAGPAUSER_RESPAWN_COOLDOWN_DESC"));
  }

  public void CreateTransitionCooldownMsEntry(TextMenu menu, bool inGame)
  {
    TextMenu.Item menuItem;

    menu.Add(menuItem = new TextMenu.Slider(
      Dialog.Clean("LAGPAUSER_TRANSITION_COOLDOWN"),
      i => $"{i * 100}",
      0, 100,
      TransitionCooldownMs / 100
    ).Change(i => TransitionCooldownMs = i * 100));

    menuItem.AddDescription(menu, Dialog.Clean("LAGPAUSER_TRANSITION_COOLDOWN_DESC"));
  }
}
