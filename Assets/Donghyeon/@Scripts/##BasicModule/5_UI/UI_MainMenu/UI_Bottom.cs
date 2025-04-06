using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
[System.Serializable]
public class Panel
{
    public GameObject MainPanel;
    public Image PanelImage;
    public Animator animator;
}
public class Bottom_UIs : UI_Scene
{
    enum Texts
    {
    }
        
    enum Images
    {
    }
        
    enum GameObjects
    {

    }
    enum Buttons
    {
    Hero_B,
    Shop_B,
    Guild_B,
    Lock_B,
    Main_B
    }

    private List<Panel> Panels = new List<Panel>();
    [SerializeField] private GameObject[] mainPanels;
    [SerializeField] private Button[] buttons;
    [SerializeField] private Color ActiveColor, NoneActiveColor;

    private void Start(){}
    

    

    // Bottom_UIs.cs에 추가
    public void SetPanels(GameObject[] panels)
    {
        mainPanels = panels;
    }
     public override bool Init()
    {
        if(base.Init() == false)
            return false;

        BindTexts(typeof(Texts));
        BindImages(typeof(Images));
        BindObjects(typeof(GameObjects));
        BindButtons(typeof(Buttons));
        // 버튼과 패널을 연결

        return true;
    }


    public void SetupPanelsAndButtons()
    {
        if (mainPanels == null || mainPanels.Length == 0)
        {
            Debug.LogError("[Bottom_UIs] 패널이 설정되지 않았습니다. UI_MainMenu에서 SetPanels를 호출해야 합니다.");
            return;
        }


        ActiveColor = new Color(0.2f, 0.6f, 1f); // 어두운 파란색
        NoneActiveColor = new Color(0.2f, 0.2f, 0.2f); // 매우 어두운 회색
        // 바인딩된 버튼 가져오기
        Button heroButton = GetButton((int)Buttons.Hero_B);
        Button shopButton = GetButton((int)Buttons.Shop_B);
        Button guildButton = GetButton((int)Buttons.Guild_B);
        Button lockButton = GetButton((int)Buttons.Lock_B);
        Button mainButton = GetButton((int)Buttons.Main_B);

        for(int i = 0; i < mainPanels.Length; i++)
        {
            if(mainPanels[i] != null)
            {
                // 패널 이름에 따라 적절한 스크립트 추가
                if(mainPanels[i].name.Contains("Shop_P"))
                {
                    Util.GetOrAddComponent<UI_Shop>(mainPanels[i]);
                }
                else if(mainPanels[i].name.Contains("Hero_P"))
                {
                    // Hero 패널용 스크립트 추가
                    // mainPanels[i].GetOrAddComponent<HeroPanel>();
                }
                else if(mainPanels[i].name.Contains("Guild_P"))
                {
                    // Guild 패널용 스크립트 추가
                    // mainPanels[i].GetOrAddComponent<GuildPanel>();
                }
                // 기타 패널들...
            }
        }



        // 버튼 배열 생성
        buttons = new Button[] 
        { 
            mainButton, 
            heroButton, 
            shopButton, 
            guildButton, 
            lockButton 
        };

        // 각 버튼과 패널 연결
        for(int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && i < mainPanels.Length)
            {
                Panel panel = new Panel();
                panel.MainPanel = mainPanels[i];
                panel.PanelImage = buttons[i].GetComponent<Image>();
                panel.animator = buttons[i].GetComponent<Animator>();

                Panels.Add(panel);

                int index = i;
                buttons[i].onClick.AddListener(() => GetPanel(index));
            }
            else
            {
                Debug.LogWarning($"[Bottom_UIs] 버튼 또는 패널이 null입니다: 인덱스 {i}");
            }
        }

        // 기본적으로 첫 번째 패널 활성화 (Main)
        // GetPanel(0);

        // 색상 설정 (기본값)
    if (ActiveColor == Color.clear) ActiveColor = new Color(0.2f, 0.6f, 1f); // 어두운 파란색
    if (NoneActiveColor == Color.clear) NoneActiveColor = new Color(0.2f, 0.2f, 0.2f); // 매우 어두운 회색
    }

    public void GetPanel(int value)
    {
        for (int i = 0; i < Panels.Count; i++)
        {
            bool isActive = value == i;
            GameObject panelObj = Panels[i].MainPanel;
            
            panelObj.SetActive(isActive);


            AnimatorStateInfo stateInfo = Panels[i].animator.GetCurrentAnimatorStateInfo(0);
            bool isCurrentlyOn = stateInfo.IsName("Bottom_Panel_On");

            if(isActive)
            {
                // 패널 활성화 시 해당 스크립트의 초기화 메서드 호출
                if(panelObj.name.Contains("Shop_P"))
                {
                    // MainScene_Shop shopScript = panelObj.GetComponent<MainScene_Shop>();
                    // if(shopScript != null)
                    // {
                    //     // Shop 패널 초기화 메서드가 있다면 호출
                    //     // shopScript.Initialize();
                    // }
                }
                else if(panelObj.name.Contains("Hero_P"))
                {
                    // Hero 패널 초기화
                    // HeroPanel heroScript = panelObj.GetComponent<HeroPanel>();
                    // if(heroScript != null) heroScript.Initialize();
                }
                // 기타 패널들...
                
                Panels[i].animator.Play("Bottom_Panel_On");
            }
            else
            {
                // AnimatorStateInfo stateInfo = Panels[i].animator.GetCurrentAnimatorStateInfo(0);
                // bool isCurrentlyOn = stateInfo.IsName("Bottom_Panel_On");
                
                // if(isCurrentlyOn)
                // {
                //     Panels[i].animator.Play("Bottom_Panel_Down");
                // }
            }

            Panels[i].PanelImage.color = isActive == true ? ActiveColor : NoneActiveColor;
        }
    }

}
