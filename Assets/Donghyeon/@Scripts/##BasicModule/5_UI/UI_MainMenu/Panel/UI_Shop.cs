using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class UI_Shop : UI_Scene
{
    [SerializeField] private TextMeshProUGUI T_Gem;

    [SerializeField] private TextMeshProUGUI T_OneGacha, T_TenGacha;
    [SerializeField] private TextMeshProUGUI T_ReGachaPrice;
    [SerializeField] private GameObject GachaPanel;

    [SerializeField] private GameObject CharacaterObject;
    [SerializeField] private Transform Gacha_Content;

    List<GameObject> Gorvage = new List<GameObject>();

    private int gachaValue = -1;
    private void Start()
    {
        InitalizeText();
        // Cloud_Mng.instance.onGetGemEvent += InitalizeText;
        // Cloud_Mng.instance.onUseGemEvent += InitalizeText;
    }

    private void OnEnable()
    {
        gachaValue = -1;
    }

    public void ReSummonBtn()
    {
        if (gachaValue == -1) return;

        Summon(gachaValue);
    }

    public void Summon(int value)
    {
        if(Gorvage.Count > 0)
        {
            for (int i = 0; i < Gorvage.Count; i++) Destroy(Gorvage[i]);
            Gorvage.Clear();
        }

        int price = value == 1 ? 50 : 450;
        // if (!Cloud_Mng.instance.UseGem(price)) return;
        // gachaValue = value;
        // T_ReGachaPrice.text = gachaValue == 1 ? "50" : "450";

        // T_ReGachaPrice.color = gachaValue == 1 ?
        //     Cloud_Mng.instance.m_Data.gem >= 50 ? Color.green : Color.red :
        //     Cloud_Mng.instance.m_Data.gem >= 450 ? Color.green : Color.red;

        // GachaPanel.SetActive(true);

        StartCoroutine(Gacha_Coroutine(value));   
    }

    IEnumerator Gacha_Coroutine(int value)
    {
        for (int i = 0; i < value; i++)
        {
            float roll = Random.Range(0.0f, 100.0f);
            string rarity = DetermineRarity(roll);
            // Hero_Scriptable hero = GetRandomheroFromRarity(rarity);
            // int RandomValue = Random.Range(1, 11);
            // if (hero != null)
            // {
            //     var go = Instantiate(CharacaterObject, Gacha_Content);
            //     go.transform.Find("Shadow").GetComponent<Image>().color
            //         = Utils.RarityColor((Rarity)System.Enum.Parse(typeof(Rarity), rarity));
            //     go.transform.Find("Icon").GetComponent<Image>().sprite = Utils.GetAtlas(hero.Name);
            //     go.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = RandomValue.ToString();
            //     Gorvage.Add(go);
            // }
            // Cloud_Mng.instance.GetHero(hero, RandomValue);
        }

        yield return new WaitForSeconds(0.1f);
        
        for(int i = 0; i < Gacha_Content.childCount; i++)
        {
            Gacha_Content.GetChild(i).gameObject.SetActive(true);
            yield return new WaitForSeconds(0.1f);
        }

    }

    private string DetermineRarity(float roll)
    {
        // float commonChance = Utils.Setting.Rarity_Percentage[0];
        // float unCommonChance = Utils.Setting.Rarity_Percentage[1];
        // float rareChance = Utils.Setting.Rarity_Percentage[2];
        // float heroChance = Utils.Setting.Rarity_Percentage[3];
        // float legendaryChance = Utils.Setting.Rarity_Percentage[4];

        // if (roll < commonChance) return "Common";
        // else if (roll < commonChance + unCommonChance) return "UnCommon";
        // else if (roll < commonChance + unCommonChance + rareChance) return "Rare";
        // else if (roll < commonChance + unCommonChance + rareChance + heroChance) return "Hero";
        // else if (roll < 100) return "Legendary";
        return "";
    }

    // private Hero_Scriptable GetRandomheroFromRarity(string rarity)
    // {
    //     Hero_Scriptable[] heroes = Resources.LoadAll<Hero_Scriptable>($"Character_Scriptable/{rarity}");
    //     if (heroes.Length == 0) return null;

    //     return heroes[Random.Range(0, heroes.Length)];
    // }



    private void InitalizeText()
    {
        // T_Gem.text = Cloud_Mng.instance.m_Data.gem.ToString();
        // T_OneGacha.color = Cloud_Mng.instance.m_Data.gem >= 50 ? Color.green : Color.red;
        // T_TenGacha.color = Cloud_Mng.instance.m_Data.gem >= 450 ? Color.green : Color.red;
    }

    public void BuyGem(int value)
    {
        // Cloud_Mng.instance.GetGem(value);
    }

    private void OnDestroy()
    {
        // Cloud_Mng.instance.onGetGemEvent -= InitalizeText;
    }
}
