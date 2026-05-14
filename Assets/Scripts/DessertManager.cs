using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class DessertManager : MonoBehaviour
{
    public List<GameObject> desserts = new();
    public DessertIndicator dessertIndicator;
    public DessertCollector dessertCollector;
    void Awake()
    {
        dessertCollector.OnDessertPickedUp += OnDessertPickedUp;
        dessertCollector.OnDessertDropped += OnDessertDropped;
        // disable all desserts
        foreach (GameObject dessert in desserts)
        {
            dessert.SetActive(false);
        }
        EnableDessert();


    }
    void EnableDessert()
    {
        if (desserts.Count > 0)
        {
            desserts[0].SetActive(true);
            dessertIndicator.dessertToLocate = desserts[0].transform;
        }
    }

    void OnDessertPickedUp()
    {
        dessertIndicator.dessertToLocate = null;
    }

    void OnDessertDropped()
    {
        desserts[0].SetActive(false);
        desserts.RemoveAt(0);
        EnableDessert();
    }
}
