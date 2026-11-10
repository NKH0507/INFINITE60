using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class PanelChange : MonoBehaviour
{
    public GameObject HomePanel;
    public GameObject UpgradePanel;
    public GameObject UpgradeText;
    public GameObject UpgradeButton1;
    public GameObject UpgradeButton2;
    public GameObject UpgradeButton3;
    public GameObject UpgradeButton4;

    public GameObject SkinPanel;
    public GameObject SkinButton1;
    public GameObject SkinButton2;
    public GameObject SkinButton3;
    public GameObject SkinButton4;
    public GameObject SkinText;



    public GameObject StartButton;
    public GameObject Title;



    public void OpenHomePanel()
    {
        Title.SetActive(true);
        StartButton.SetActive(true);
        HomePanel.SetActive(true);
        UpgradePanel.SetActive(false);
        UpgradeButton1.SetActive(false);
        UpgradeButton2.SetActive(false);
        UpgradeButton3.SetActive(false);
        UpgradeButton4.SetActive(false);
        UpgradeText.SetActive(false);


        SkinPanel.SetActive(false);
        SkinText.SetActive(false);
        SkinButton1.SetActive(false);
        SkinButton2.SetActive(false);
        SkinButton3.SetActive(false);
        SkinButton4.SetActive(false);
    }

    public void OpenUpgradePanel()
    {
        HomePanel.SetActive(false);
        UpgradePanel.SetActive(true);
        SkinPanel.SetActive(false);
        Title.SetActive(false);
        StartButton.SetActive(false);

        UpgradePanel.SetActive(true);
        UpgradeButton1.SetActive(true);
        UpgradeButton2.SetActive(true);
        UpgradeButton3.SetActive(true);
        UpgradeButton4.SetActive(true);
        UpgradeText.SetActive(true);


        SkinText.SetActive(false);
        SkinPanel.SetActive(false);
        SkinButton1.SetActive(false);
        SkinButton2.SetActive(false);
        SkinButton3.SetActive(false);
        SkinButton4.SetActive(false);
    }
    public void OpenSkinPanel()
    {
        HomePanel.SetActive(false);
        UpgradePanel.SetActive(false);
        SkinPanel.SetActive(true);
        Title.SetActive(false);
        StartButton.SetActive(false);

        UpgradePanel.SetActive(false);
        UpgradeButton1.SetActive(false);
        UpgradeButton2.SetActive(false);
        UpgradeButton3.SetActive(false);
        UpgradeButton4.SetActive(false);
        UpgradeText.SetActive(false);


        SkinText.SetActive(true);
        SkinPanel.SetActive(true);
        SkinButton1.SetActive(true);
        SkinButton2.SetActive(true);
        SkinButton3.SetActive(true);
        SkinButton4.SetActive(true);
    }
}
