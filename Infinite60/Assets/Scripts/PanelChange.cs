using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class PanelChange : MonoBehaviour
{
    public GameObject UpgradePanel;
    public GameObject UpgradeTitle;
    public GameObject UpgradeButton1;
    public GameObject UpgradeButton2;
    public GameObject UpgradeButton3;
    public GameObject UpgradeButton4;


    public GameObject SkinPanel;
    public GameObject SkinTitle;

    public GameObject BallSkin1;
    public GameObject BallSkin2;
    public GameObject BallSkin3;

    public GameObject PannelSkin1;
    public GameObject PannelSkin2;
    public GameObject PannelSkin3;


    public GameObject StartButton;
    public GameObject Title;
    public GameObject HomePanel;



    public void OpenHomePanel()
    {
        Title.SetActive(true);
        StartButton.SetActive(true);
        HomePanel.SetActive(true);

        UpgradePanel.SetActive(false);
        UpgradeTitle.SetActive(false);
        UpgradeButton1.SetActive(false);
        UpgradeButton2.SetActive(false);
        UpgradeButton3.SetActive(false);
        UpgradeButton4.SetActive(false);

        SkinPanel.SetActive(false);
        SkinTitle.SetActive(false);
        BallSkin1.SetActive(false);
        BallSkin2.SetActive(false);
        BallSkin3.SetActive(false);
        PannelSkin1.SetActive(false);
        PannelSkin2.SetActive(false);
        PannelSkin3.SetActive(false);
    }

    public void OpenUpgradePanel()
    {
        HomePanel.SetActive(false);

        UpgradePanel.SetActive(true);
        UpgradeTitle.SetActive(true);
        UpgradeButton1.SetActive(true);
        UpgradeButton2.SetActive(true);
        UpgradeButton3.SetActive(true);
        UpgradeButton4.SetActive(true);

        SkinPanel.SetActive(false);
        SkinTitle.SetActive(false);

        Title.SetActive(false);
        StartButton.SetActive(false);

        BallSkin1.SetActive(false);
        BallSkin2.SetActive(false);
        BallSkin3.SetActive(false);
        PannelSkin1.SetActive(false);
        PannelSkin2.SetActive(false);
        PannelSkin3.SetActive(false);
    }
    public void OpenSkinPanel()
    {
        HomePanel.SetActive(false);

        UpgradePanel.SetActive(false);
        UpgradeTitle.SetActive(false);
        UpgradeButton1.SetActive(false);
        UpgradeButton2.SetActive(false);
        UpgradeButton3.SetActive(false);
        UpgradeButton4.SetActive(false);

        SkinPanel.SetActive(true);
        SkinTitle.SetActive(true);

        Title.SetActive(false);
        StartButton.SetActive(false);

        BallSkin1.SetActive(true);
        BallSkin2.SetActive(true);
        BallSkin3.SetActive(true);
        PannelSkin1.SetActive(true);
        PannelSkin2.SetActive(true);
        PannelSkin3.SetActive(true);
    }
}
