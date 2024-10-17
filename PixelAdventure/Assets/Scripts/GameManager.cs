using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public Player player;

    [Header("Fruit Management")]
    public bool fruitHaveRandomLook;
    public int fruitCollected;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else 
            Destroy(gameObject);
    }

    public void AddFruit() => fruitCollected++;

    public bool FruitHaveRandomLook() => fruitHaveRandomLook;
}
