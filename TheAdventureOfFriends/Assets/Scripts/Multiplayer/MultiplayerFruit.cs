using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public enum MultiplayerFruitType { Apple, Banana, Cherry, Kiwi, Melon, Orange, Pineapple, Strawberry }

public class MultiplayerFruit : MonoBehaviour
{
    [SerializeField] private MultiplayerFruitType fruitType;
    [SerializeField] private GameObject pickupVFX;

    private MultiplayerGameManager multiplayerGameManager;
    protected Animator anim;
    protected SpriteRenderer sr;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    protected virtual void Start()
    {
        multiplayerGameManager = MultiplayerGameManager.instance;

        SetRandomLookIfNeeded();
    }

    private void SetRandomLookIfNeeded()
    {
        if (multiplayerGameManager.FruitHaveRandomLook() == false)
        {
            UpdateFruitVisuals();
            return;
        }
        int randomIndex = Random.Range(0, 8);
        anim.SetFloat("fruitIndex", randomIndex);
    }

    private void UpdateFruitVisuals() => anim.SetFloat("fruitIndex", (int)fruitType);

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.GetComponent<Player>();

        if (player != null)
        {
            multiplayerGameManager.AddFruit();
            AudioManager.instance.PlaySFX(8);
            Destroy(gameObject);

            GameObject newFx = Instantiate(pickupVFX, transform.position, Quaternion.identity);
        }
    }
}