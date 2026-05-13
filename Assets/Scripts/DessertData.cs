using UnityEngine;

[CreateAssetMenu(fileName = "New Dessert Data", menuName = "Dessert Dealer/Dessert Data")]
public class DessertData : ScriptableObject
{
    public string id;
    public string dessertName;
    public int dessertLevel;
    public int dessertValue;
}
