using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class PanelChange : MonoBehaviour
{
    [UnityEngine.Serialization.FormerlySerializedAs("HomePanel")]
    public GameObject homePanel;
    [UnityEngine.Serialization.FormerlySerializedAs("UpgradePanel")]
    public GameObject upgradePanel;
    [UnityEngine.Serialization.FormerlySerializedAs("UpgradeText")]
    public GameObject upgradeText;
    [UnityEngine.Serialization.FormerlySerializedAs("UpgradeButton1")]
    public GameObject upgradeButton1;
    [UnityEngine.Serialization.FormerlySerializedAs("UpgradeButton2")]
    public GameObject upgradeButton2;
    [UnityEngine.Serialization.FormerlySerializedAs("UpgradeButton3")]
    public GameObject upgradeButton3;
    [UnityEngine.Serialization.FormerlySerializedAs("UpgradeButton4")]
    public GameObject upgradeButton4;

    [UnityEngine.Serialization.FormerlySerializedAs("SkinPanel")]
    public GameObject skinPanel;
    [UnityEngine.Serialization.FormerlySerializedAs("SkinButton1")]
    public GameObject skinButton1;
    [UnityEngine.Serialization.FormerlySerializedAs("SkinButton2")]
    public GameObject skinButton2;
    [UnityEngine.Serialization.FormerlySerializedAs("SkinButton3")]
    public GameObject skinButton3;
    [UnityEngine.Serialization.FormerlySerializedAs("SkinButton4")]
    public GameObject skinButton4;
    [UnityEngine.Serialization.FormerlySerializedAs("SkinText")]
    public GameObject skinText;



    [UnityEngine.Serialization.FormerlySerializedAs("StartButton")]
    public GameObject startButton;
    [UnityEngine.Serialization.FormerlySerializedAs("Title")]
    public GameObject title;



    public void OpenHomePanel()
    {
        title.SetActive(true);
        startButton.SetActive(true);
        homePanel.SetActive(true);
        upgradePanel.SetActive(false);
        upgradeButton1.SetActive(false);
        upgradeButton2.SetActive(false);
        upgradeButton3.SetActive(false);
        upgradeButton4.SetActive(false);
        upgradeText.SetActive(false);


        skinPanel.SetActive(false);
        skinText.SetActive(false);
        skinButton1.SetActive(false);
        skinButton2.SetActive(false);
        skinButton3.SetActive(false);
        skinButton4.SetActive(false);
    }

    public void OpenUpgradePanel()
    {
        homePanel.SetActive(false);
        upgradePanel.SetActive(true);
        skinPanel.SetActive(false);
        title.SetActive(false);
        startButton.SetActive(false);

        upgradePanel.SetActive(true);
        upgradeButton1.SetActive(true);
        upgradeButton2.SetActive(true);
        upgradeButton3.SetActive(true);
        upgradeButton4.SetActive(true);
        upgradeText.SetActive(true);


        skinText.SetActive(false);
        skinPanel.SetActive(false);
        skinButton1.SetActive(false);
        skinButton2.SetActive(false);
        skinButton3.SetActive(false);
        skinButton4.SetActive(false);
    }
    public void OpenSkinPanel()
    {
        homePanel.SetActive(false);
        upgradePanel.SetActive(false);
        skinPanel.SetActive(true);
        title.SetActive(false);
        startButton.SetActive(false);

        upgradePanel.SetActive(false);
        upgradeButton1.SetActive(false);
        upgradeButton2.SetActive(false);
        upgradeButton3.SetActive(false);
        upgradeButton4.SetActive(false);
        upgradeText.SetActive(false);


        skinText.SetActive(true);
        skinPanel.SetActive(true);
        skinButton1.SetActive(true);
        skinButton2.SetActive(true);
        skinButton3.SetActive(true);
        skinButton4.SetActive(true);
    }
}
