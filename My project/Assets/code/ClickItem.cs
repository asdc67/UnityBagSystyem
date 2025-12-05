using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static System.Net.Mime.MediaTypeNames;

public class ClickItem : MonoBehaviour
{
    public UnityEngine.UI.Image icon;
    public UnityEngine.UI.Text text;
    public GameObject image;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void OnClick()
    {
        InventortManager.currentItemID = Convert.ToInt32(gameObject.name);
        Debug.Log($"{InventortManager.currentItemID}");
        image.SetActive(true);
        Invoke("Dselect", 1f);
    }
    public void SetIcon(int id,string type,int level,int quantity)
    {
        icon.sprite = Resources.Load<Sprite>($"Sprites/{id}");
        if (type == "weapon")
        {
            icon.transform.rotation = Quaternion.Euler(0, 0, -45);
            text.text = $"LV:{level}";
        }
        else
        {
            text.text = $"{quantity}";
            icon.transform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }
  void Dselect()
    {
        image.SetActive(false); 
    }
}
