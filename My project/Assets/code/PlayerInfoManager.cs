using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerInfoManager : MonoBehaviour
{

   public TMP_Text id;
   public TMP_Text level;
    public TMP_Text attackT;
    public TMP_Text defenceT;
    public TMP_Text healthT;
    public TMP_Text criticalHitT;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        id.text = $"UID:{GameManager.CurrentUser.userId}";
        level.text = $"Level:{GameManager.CurrentUser.level}";
        attackT.text = $"{GameManager.CurrentUser.attack}";
        defenceT.text = $"{GameManager.CurrentUser.defence}";
        healthT.text = $"{GameManager.CurrentUser.health}";
        criticalHitT.text = $"{GameManager.CurrentUser.criticalHit}";
    }
}
