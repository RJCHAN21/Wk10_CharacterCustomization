using UnityEngine;

[CreateAssetMenu(
    fileName = "CharacterData", 
    menuName = "Custom Character/Character Data", 
    order = 0)]
public class CharacterData : ScriptableObject
{
    [Header("Sprite Indices")]
    public int hairIndex;
    public int faceHairIndex;
    public int clothIndex;
    public int pantsIndex;
    public int skinIndex;

    [Header("Colors")]
    public Color hairColor = Color.white;
    public Color eyeColor  = Color.white;
}
