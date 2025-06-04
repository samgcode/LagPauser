using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.LagPauser;

public class LagPauserModule : EverestModule
{
    public static LagPauserModule Instance { get; private set; }

    public override Type SettingsType => typeof(LagPauserModuleSettings);
    public static LagPauserModuleSettings Settings => (LagPauserModuleSettings)Instance._Settings;

    public override Type SessionType => typeof(LagPauserModuleSession);
    public static LagPauserModuleSession Session => (LagPauserModuleSession)Instance._Session;

    public override Type SaveDataType => typeof(LagPauserModuleSaveData);
    public static LagPauserModuleSaveData SaveData => (LagPauserModuleSaveData)Instance._SaveData;

    private static Hook hook_Player_orig_Die;

    private static FieldInfo f_Game_accumulatedElapsedTime = typeof(Game).GetField("accumulatedElapsedTime", BindingFlags.NonPublic | BindingFlags.Instance);

    private const double frameTime = 50.0 / 3.0; // 16.6666666666667ms
    public static double timeSincePause = 0;
    public static double timeSinceDeath = 0;
    public static double timeSinceTransition = 0;
    public static double timeSincePauseStart = 0;

    private static TextMenu.Item resume;

    public LagPauserModule()
    {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(LagPauserModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(LagPauserModule), LogLevel.Info);
#endif
    }

    public override void Load()
    {
        On.Monocle.Engine.Update += EngineUpdateHook;
        Everest.Events.Level.OnLoadLevel += OnLoadLevel;

        hook_Player_orig_Die = new Hook(
                typeof(Player).GetMethod("orig_Die", BindingFlags.Public | BindingFlags.Instance),
                typeof(LagPauserModule).GetMethod("OnPlayerDie"));
    }

    public override void Unload()
    {
        On.Monocle.Engine.Update -= EngineUpdateHook;
        Everest.Events.Level.OnLoadLevel -= OnLoadLevel;

        hook_Player_orig_Die?.Dispose();
        hook_Player_orig_Die = null;

        f_Game_accumulatedElapsedTime = null;
    }

    public static void EngineUpdateHook(On.Monocle.Engine.orig_Update orig, Engine engine, GameTime gameTime)
    {
        if (Settings.ToggleEnabled.Pressed)
        {
            Settings.Enable = !Settings.Enable;
        }

        if (!Settings.Enable)
        {
            orig(engine, gameTime);
            return;
        }

        if (Engine.Scene is Level level)
        {
            TimeSpan deltaTime = (TimeSpan)f_Game_accumulatedElapsedTime.GetValue(engine);

            timeSincePauseStart += deltaTime.TotalMilliseconds + frameTime;
            if (resume != null)
            {
                resume.Disabled = timeSincePauseStart <= Settings.InputCooldownMs;
            }

            if (level.CanPause)
            {
                timeSincePause += deltaTime.TotalMilliseconds + frameTime;
                timeSinceDeath += deltaTime.TotalMilliseconds + frameTime;
                timeSinceTransition += deltaTime.TotalMilliseconds + frameTime;

                if (
                    timeSincePause >= Settings.CooldownMs &&
                    timeSinceDeath >= Settings.RespawnCooldownMs &&
                    timeSinceTransition >= Settings.TransitionCooldownMs
                    )
                {
                    if (deltaTime.TotalMilliseconds >= Settings.ThresholdMs)
                    {
                        timeSincePauseStart = 0;
                        Pause();
                    }
                }
            }
        }

        orig(engine, gameTime);
    }

    public static void Pause()
    {
        Level level = Engine.Scene as Level;
        level.StartPauseEffects();
        level.Paused = true;

        level.PauseMainMenuOpen = true;
        TextMenu menu = new TextMenu();

        menu.Add(new TextMenu.Header(Dialog.Clean("LAGPAUSER_PAUSED")));

        menu.Add(resume = new TextMenu.Button(Dialog.Clean("menu_pause_resume")).Pressed(() =>
        {
            menu.OnCancel();
        }));

        menu.OnESC = menu.OnCancel = menu.OnPause = () =>
        {
            if (timeSincePauseStart > Settings.InputCooldownMs)
            {
                level.PauseMainMenuOpen = false;
                menu.RemoveSelf();
                level.Paused = false;
                Audio.Play("event:/ui/game/unpause");
                level.unpauseTimer = 0.15f;
                level.EndPauseEffects();

                OnUnpause(level);
            }
        };

        level.Add(menu);
    }

    public static void OnUnpause(Level level)
    {
        timeSincePause = 0;
        level.CanRetry = true;
    }

    public static PlayerDeadBody OnPlayerDie(Func<Player, Vector2, bool, bool, PlayerDeadBody> orig, Player self, Vector2 direction, bool ifInvincible, bool registerStats)
    {
        timeSinceDeath = 0;

        return orig(self, direction, ifInvincible, registerStats);
    }

    private void OnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader)
    {
        timeSinceTransition = 0;
    }
}
