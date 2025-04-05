using System;

namespace Unity.Assets.Scripts.Objects
{
    /// <summary>
    /// This corresponds to a CharacterClass ScriptableObject data object, containing the core gameplay data for
    /// a given class.
    /// </summary>
    public enum CharacterTypeEnum
    {
        //heroes
        Hero,
        None,
        Tank,
        Archer,
        Mage,
        Rogue,

        //monsters
        Imp,
        ImpBoss,
        VandalImp,
        Monster,
        green_slime
    }
}
