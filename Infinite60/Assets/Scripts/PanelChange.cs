using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class PanelChange : MonoBehaviour
{
    public GameObject HomePanel;
    public GameObject UpgradePanel;
    public GameObject SkinPanel;
    public GameObject StartButton;
    public GameObject Title;



    public void OpenHomePanel()
    {
        Title.SetActive(true);
        StartButton.SetActive(true);
        HomePanel.SetActive(true);
        UpgradePanel.SetActive(false);
        SkinPanel.SetActive(false);
    }

    public void OpenUpgradePanel()
    {
        HomePanel.SetActive(false);
        UpgradePanel.SetActive(true);
        SkinPanel.SetActive(false);
        Title.SetActive(false);
        StartButton.SetActive(false);
    }
    public void OpenSkinPanel()
    {
        HomePanel.SetActive(false);
        UpgradePanel.SetActive(false);
        SkinPanel.SetActive(true);
        Title.SetActive(false);
        StartButton.SetActive(false);
    }
}
