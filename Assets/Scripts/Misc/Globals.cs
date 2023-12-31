﻿/// <summary>
/// Saves information about e.g can player currently move etc. which gets disabled when player
/// dies, or is in a menu.
/// </summary>
static class Globals
{
    //LOGGING
    public delegate void DebugChanged(bool state);
    public static event DebugChanged onDebugChanged;
    public delegate void DebugStateLogChanged(bool state);
    public static event DebugStateLogChanged onLogStatesChange;
    static bool _debugOn = false;
    static bool _logStates = false;


    //GAMEPLAY VARIABLES
    //17 ms for single character on text box
    static bool _easyModeOn = false;
    static float characterTextSpeed = 0.012f;
    static bool _movementControlsAreEnabled = true;
    static bool _controlsAreEnabled = true;
    static float likelinessOfItemDroppingInRoom = 100f;
    static float likelinessOfMookDroppingHp = 20f;
    static float likelinessOfContainerDroppingHp = 7.5f;
    static GenerationSettings generationSettings;
    public static string playerType;

    //PUBLIC PROPERTIES

    public static bool ControlsAreEnabled { get => _controlsAreEnabled; set => _controlsAreEnabled = value; }
    public static bool MovementControlsAreEnabled { get => _movementControlsAreEnabled; set => _movementControlsAreEnabled = value; }
    public static float CharacterTextSpeed { get => characterTextSpeed; set => characterTextSpeed = value; }
    public static float LikelinessOfItemDroppingInRoom { get => likelinessOfItemDroppingInRoom; set => likelinessOfItemDroppingInRoom = value; }
    public static float LikelinessOfMookDroppingHp
    {
        get 
        { 
            return likelinessOfMookDroppingHp + (Player.Singleton.BuffModifiers[StatType.ItemDiscovery]*100); 
        }
        set
        {
            likelinessOfMookDroppingHp = value;
        }
    }
    public static float LikelinessOfContainerDroppingHp 
    { get
        {
            //50% of item discovery is buffing container drops
            return likelinessOfContainerDroppingHp + (Player.Singleton.BuffModifiers[StatType.ItemDiscovery]*50); 
        } 
        set => likelinessOfContainerDroppingHp = value; 
    }

    public static bool DebugOn
    {
        get => _debugOn; set
        {
            _debugOn = value;
            onDebugChanged?.Invoke(value);
        }
    }
    internal static GenerationSettings GenerationSettings { get => generationSettings; set => generationSettings = value; }
    public static bool LogStates
    {
        get 
        { 
            return _logStates; 
        }
        set
        {
            _logStates = value;
            onLogStatesChange?.Invoke(value);
        }
    }

    public static bool EasyModeOn { get => _easyModeOn; set => _easyModeOn = value; }
}
