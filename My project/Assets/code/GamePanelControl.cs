using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePanelControl : MonoBehaviour
{
    public GameObject shopPanel;
    public GameObject gamePanel;
    public GameObject backpackPanel;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void TurnToBackPack()
    {
        backpackPanel.SetActive(true);
        gamePanel.SetActive(false);
    }
    public void TurnToShop()
    {
        shopPanel.SetActive(true);
        gamePanel.SetActive(false);
    }
    public void BagReturnToGamePanel()
    {
        gamePanel.SetActive(true);
        backpackPanel.SetActive(false );
    }
}
