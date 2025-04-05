using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
public class MainScene_Hero  : UI_Scene
{
    [SerializeField] private GameObject Hero_Object;
    [SerializeField] private Transform Content;

    [SerializeField] private GameObject HeroInformation;
    [SerializeField] private Image HeroIcon;
    [SerializeField] private Image HeroRarityIcon;

    List<GameObject> Gorvage = new List<GameObject>();
    private void OnEnable()
    {
        Initalize();
    }

    private void SetInformation(string HeroName)
    {
        HeroInformation.SetActive(true);
        // HeroIcon.sprite = Utils.GetAtlas(HeroName);
        // HeroRarityIcon.color = Utils.RarityColor(Cloud_Mng.instance.m_Data.heroData[HeroName].rarity);
    }

    public void Initalize()
    {
    //     if(Gorvage.Count > 0)
    //     {
    //         for (int i = 0; i < Gorvage.Count; i++) Destroy(Gorvage[i]);
    //         Gorvage.Clear();
    //     }
    //     var datas = Cloud_Mng.instance.m_Data.heroData;

    //     foreach(var data in datas)
    //     {
    //         var go = Instantiate(Hero_Object, Content);
    //         go.SetActive(true);

    //         go.transform.Find("Rarity").GetComponent<Image>().color = Utils.RarityColor(data.Value.rarity);
            
    //         var icon = go.transform.Find("Icon").GetComponent<Image>();
    //         icon.sprite = Utils.GetAtlas(data.Key);
    //         icon.color = data.Value.Count == 0 ? Color.black : Color.white;

    //         var countParent = go.transform.Find("Count");
    //         countParent.GetChild(0).GetChild(0).GetComponent<Image>().fillAmount = (float)data.Value.Count / 10;
    //         countParent.GetChild(1).GetComponent<TextMeshProUGUI>().text =
    //             string.Format("{0}/{1}", data.Value.Count, 10);

    //         go.transform.Find("LevelTxT").GetComponent<TextMeshProUGUI>().text = "Lv." + data.Value.Level.ToString();
    //         go.transform.Find("Mark").gameObject.SetActive(data.Value.Count >= 10);

    //         go.transform.Find("Button").GetComponent<Button>().onClick.AddListener(() =>
    //         {
    //             SetInformation(data.Key);
    //         });

    //         Gorvage.Add(go);
    //     }
    }
}
