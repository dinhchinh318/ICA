using UnityEngine;
namespace LumaReef.Data
{
    [CreateAssetMenu(menuName="Luma Reef/Catalog")]
    public sealed class ReefCatalog : ScriptableObject
    {
        public RoomData room;
        public SkillData[] skills;
        public QuestData[] quests;
        public AudioLibrary audio;
        public Sprite disk,barrel,background,ring;
        public Material particleMaterial;
        public Font font;
        public Sprite lobbyBackground;
        public Sprite[] uiIcons=new Sprite[0];
        public Sprite[] cannonSprites=new Sprite[0];
        public Sprite[] effectSprites=new Sprite[0];
        public Sprite[] uiSkins=new Sprite[0];
    }
}
