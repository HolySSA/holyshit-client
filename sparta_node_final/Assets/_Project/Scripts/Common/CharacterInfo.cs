public class CharacterInfo
{
    public CharacterType characterType;
    public string name;
    public int hp;
    public string description;
    public bool owned;
    public int playCount;
    public int winCount;

    public CharacterInfo(CharacterInfoData data)
    {
        characterType = data.CharacterType;
        name = data.Name;
        hp = data.Hp;
        description = data.Description;
        owned = data.Owned;
        playCount = data.PlayCount;
        winCount = data.WinCount;
    }
}