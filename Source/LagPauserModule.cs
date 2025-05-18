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
    public static bool lagPaused = false;

    private const double frameTime = 50.0 / 3.0; // 16.6666666666667ms
    public static double timeSincePause = 0;
    public static double timeSinceDeath = 0;
    public static double timeSinceTransition = 0;

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
        Everest.Events.Level.OnUnpause += Level_Unpause;
        Everest.Events.Level.OnLoadLevel += OnLoadLevel;
        On.Celeste.Dialog.Clean += Dialog_Clean;

        hook_Player_orig_Die = new Hook(
                typeof(Player).GetMethod("orig_Die", BindingFlags.Public | BindingFlags.Instance),
                typeof(LagPauserModule).GetMethod("OnPlayerDie"));
    }

    public override void Unload()
    {
        On.Monocle.Engine.Update -= EngineUpdateHook;
        Everest.Events.Level.OnUnpause -= Level_Unpause;
        Everest.Events.Level.OnLoadLevel -= OnLoadLevel;
        On.Celeste.Dialog.Clean -= Dialog_Clean;

        hook_Player_orig_Die?.Dispose();
        hook_Player_orig_Die = null;

        f_Game_accumulatedElapsedTime = null;
    }

    public static void EngineUpdateHook(On.Monocle.Engine.orig_Update orig, Engine engine, GameTime gameTime)
    {
        if (Engine.Scene is Level level && level.CanPause)
        {
            TimeSpan deltaTime = (TimeSpan)f_Game_accumulatedElapsedTime.GetValue(engine);

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
                    lagPaused = true;
                    level.CanRetry = false;
                    level.Pause();
                }
            }
        }

        orig(engine, gameTime);
    }

    public static void Level_Unpause(Level level)
    {
        if (lagPaused)
        {
            timeSincePause = 0;
            lagPaused = false;
            level.CanRetry = true;
        }
    }

    public static string Dialog_Clean(On.Celeste.Dialog.orig_Clean orig, string text, Language language)
    {
        if (lagPaused)
        {
            if (text == "menu_pause_title")
            {
                return orig("LAGPAUSER_PAUSED", language);
            }
        }

        return orig(text, language);
    }

    public static PlayerDeadBody OnPlayerDie(Func<Player, Vector2, bool, bool, PlayerDeadBody> orig, Player self, Vector2 direction, bool ifInvincible, bool registerStats)
    {
        timeSinceDeath = 0;

        return orig(self, direction, ifInvincible, registerStats);
    }

    private void OnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader)
    {
        Logger.Log(LogLevel.Verbose, "LagPauserModule", $"OnLoadLevel: {Settings.TransitionCooldownMs}ms {timeSinceTransition}ms");
        timeSinceTransition = 0;
    }
}
